/*
Copyright (c) 2010-2011 Spyn Doctor Games (Johannes Hubert). All rights reserved.

Redistribution and use in binary forms, with or without modification, and for whatever
purpose (including commercial) are permitted. Atribution is not required. If you want
to give attribution, use the following text and URL (may be translated where required):
		Uses source code by Spyn Doctor Games - http://www.spyn-doctor.de

Redistribution and use in source forms, with or without modification, are permitted
provided that redistributions of source code retain the above copyright notice, this
list of conditions and the following disclaimer.

THIS SOFTWARE IS PROVIDED BY SPYN DOCTOR GAMES (JOHANNES HUBERT) "AS IS" AND ANY EXPRESS
OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
SPYN DOCTOR GAMES (JOHANNES HUBERT) OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Last change: 2011-01-20
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Diagnostics;

namespace SpynDoctor
{
	public delegate void AfterStopDelegate();

	public class OnlineDataSyncManager : GameComponent
	{
        private static readonly int[] ProcessorAffinity = new int[] { 3 };
        private static volatile bool s_GoSlowDisabled = false;

		private Action mAction;
		private NetworkSession mSession;
		private string mHostGamertag;
		private List<HostInfo> mRecentHosts;
		private Dictionary<string, string> mRecentHostsLookup;
		private double mServerMillisToLive;
		private Random mRandom;
		private IOnlineSyncTarget mSyncTarget;
		private PacketWriter mWriter;
		private PacketReader mReader;
		private bool mHasNewLocalEntry;
		private AfterStopDelegate mAfterStopDelegate;
		private LocalNetworkGamer mLocalGamerForSendingAndReceiving;
		private NetworkGamer mRemoteGamerForSending;
		private bool mGameExiting;

#if LIVE_SCORES_STRESS_TEST
    #if WINDOWS
        private bool mShouldIncrementScores = true;
        private int mClientRetries;
    #else
        private bool mShouldIncrementScores = false;
    #endif
#endif

        private readonly object SYNC = new object();

		private const int MILLIS_IN_MINUTE = 60000;
#if LIVE_SCORES_STRESS_TEST
        private const double WAIT_TIME_FOR_RECENT_HOST = 5;
#else
        private const double WAIT_TIME_FOR_RECENT_HOST = 30 * MILLIS_IN_MINUTE;
#endif
        private const int MINIMUM_SERVER_TIME = 4 * MILLIS_IN_MINUTE;
		private const int MAXIMUM_SERVER_TIME = 7 * MILLIS_IN_MINUTE;

		// Number of milliseconds that the worker thread sleeps in various key situations and
		// after each transferred data record.
		// A higher value means "longer sleep" -> "slower data exchange".
		// A lower value means "shorter sleep" -> "faster data exchange".
		// Tweak this value to make sure that the worker thread does not hog the CPU too much.
		// If you have a CPU intensive game and see stuttering, increase this value to leave more
		// CPU for the main thread (although this will make the data exchange slower).
		// If your game is not very CPU intensive, you may be able to decrease the value (for a
		// faster data exchange) without making your main thread stutter.
		private const int SLEEP_DURATION = 17; // Note: There's 16.67ms per frame @ 60fps

		// Define SYSTEM_LINK_SESSION as a compilation symbol to compile a version that
		// uses NetworkSessionType.SystemLink (good for debugging, and tests between Xbox and PC).
		// Or leave the symbol undefined to compile a version that uses standard Xbox LIVE
		// (i.e. NetworkSessionType.PlayerMatch).
#if SYSTEM_LINK_SESSION
		public const NetworkSessionType SESSION_TYPE = NetworkSessionType.SystemLink;
#else
		public const NetworkSessionType SESSION_TYPE = NetworkSessionType.PlayerMatch;
#endif

		public enum Mode { UNKNOWN, SERVER, CLIENT };
		public enum State { IDLE, SEND, RECEIVE, WAIT_FOR_END };
		private enum Action { NONE, PROCESS, EXIT_THREAD, DISABLE };
		private enum SessionProperty { VERSION };

		private List<SignedInGamer> mHostGamer;
		public SignedInGamer HostGamer
		{
			get
			{
				if (mHostGamer.Count > 0)
					return mHostGamer[0];
				else
					return null;
			}
		}

		private Mode mMode;
		public Mode MyMode
		{
			get { return mMode; }
		}

		private State mState;
		public State MyState
		{
			get { return mState; }
		}

		private int mVersion;
		public int MyVersion
		{
			get { return mVersion; }
		}
        
        private double mSessionInterval = 0;
        private double mSessionIntervalDelay = SLEEP_DURATION;
        public void GoSlow(bool enable)
        {
            lock (SYNC)
            {
                if (enable)
                    mSessionIntervalDelay = 1 * MILLIS_IN_MINUTE;
                else
                    mSessionIntervalDelay = SLEEP_DURATION;
                s_GoSlowDisabled = !enable;
            }
        }

		/* The "version" argument specifies the data version of the topscore data. This makes sure
		 * that if you have different versions of the same game deployed, that only games with the
		 * same version exchange data with each other.
		 * 
		 * For example: In the very first implementation, simply pass "0" as the version. Then assume
		 * that you have released this game to the marketplace. Now assume that after a while, you
		 * decide to make some changes to the structure of the topscore lists. For example, in the
		 * first version the list contained only the gamertag and score, now in the new version you
		 * also store the maximum level reached with each score. This means that you have a new field
		 * in your score entries.
		 * When you release this game to the Xbox marketplace, it would be fatal if old versions of the
		 * game (without the level field) would try to exchange scores with new versions of the game
		 * (with the level field). However, you can not guarantee that it will not happen that old and
		 * new versions of the game are running at the same time, because with XBLIG, the player is
		 * not required to upgrade to your latest version (he can upgrade, but is not forced to).
		 * Therefore, when you prepare the new version for release, make sure to use a different
		 * version number when calling this ctor. If in the old version you passed "0" as the version,
		 * now pass "1" as the version.
		 * If later you should have to make yet another incompatible change, increment the version
		 * to "2", and so on.
		 * 
		 * Of course you only have to do such a version increment in a new version of your game if
		 * the topscore data format actually changed. If the new version has the same data format,
		 * then you can just keep the previous version number. But if in doubt, it is better to
		 * increment the number when releasing a new version. */
		public OnlineDataSyncManager(int version, Game game)
			: base(game)
		{
			mVersion = version;
			mHostGamer = new List<SignedInGamer>();
			mRecentHosts = new List<HostInfo>();
			mRecentHostsLookup = new Dictionary<string, string>();
			mRandom = new Random();
			mWriter = new PacketWriter();
			mReader = new PacketReader();
			Enabled = false;
			mAction = Action.NONE;
		}

		/* The standard Update method of a GameComponent. As for all game components, this method
		 * is called once per frame by the main game thread.
		 * All the other methods in this class are instead called by the worker thread which is
		 * started in the "start" method (see below). */
		public override void Update(GameTime gameTime)
		{
			lock (SYNC) {
				if (mSession != null && !mSession.IsDisposed) {
					try {
						mSession.Update();
					}
					catch { }
				}

				if (mAction == Action.DISABLE && (mSession == null || mSession.IsDisposed || mSession.BytesPerSecondSent == 0)) {
					mAction = Action.NONE;
					Enabled = false;
					endSession();
                    if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": MANAGER DISABLED");
					if (mAfterStopDelegate != null)
						mAfterStopDelegate.Invoke();
				}
			}
		}

		public void start(SignedInGamer hostGamer, IOnlineSyncTarget syncTarget)
		{
			lock (SYNC) {
				if (mAction == Action.NONE && !mGameExiting) {
                    if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": MANAGER ENABLED");
					mAction = Action.PROCESS;
					Enabled = true;

					mHostGamer.Clear();
					mHostGamer.Add(hostGamer);
					mHostGamertag = hostGamer.Gamertag;
					mSyncTarget = syncTarget;

					mMode = Mode.UNKNOWN;
					mState = State.IDLE;

					new Thread(run).Start();
				}
			}
		}

		public void stop(AfterStopDelegate afterStopDelegate, bool gameExiting)
		{
			lock (SYNC) {
				mGameExiting = gameExiting;
				if (mAction == Action.PROCESS) {
                    if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Initiating stop");
					mAfterStopDelegate = afterStopDelegate;
					mAction = Action.EXIT_THREAD;
				}
				else {
					mAction = Action.NONE;
					Enabled = false;
					endSession();
                    if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": MANAGER DISABLED (via stop)");
					if (afterStopDelegate != null)
						afterStopDelegate.Invoke();
				}
			}
		}

		public bool isStopping()
		{
			return mAction == Action.EXIT_THREAD || mAction == Action.DISABLE;
		}

		public void notifyAboutNewLocalEntry()
		{
			mHasNewLocalEntry = true;
            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": New local entry");
		}

		public void run()
		{
#if XBOX360
            Thread.CurrentThread.SetProcessorAffinity(ProcessorAffinity);
#endif

			Stopwatch timer = new Stopwatch();
			timer.Start();
			TimeSpan now = timer.Elapsed;
			TimeSpan lastTime;
			
			while (mAction == Action.PROCESS) {
				try {
					lastTime = now;
					now = timer.Elapsed;
					double elapsedMillis = (now - lastTime).TotalMilliseconds;

					if (mHasNewLocalEntry) {
						mHasNewLocalEntry = false;
						mServerMillisToLive = 0;	// this wakes up the server, should it currently be waiting
						mRecentHosts.Clear();		// all recent hosts are now acceptable again
						mRecentHostsLookup.Clear();
                        if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Waking up server and again accepting all hosts");
					}
					else {
						for (int i = mRecentHosts.Count - 1; i >= 0; i--) {
							HostInfo hostInfo = mRecentHosts[i];
							hostInfo.mWaitTime -= elapsedMillis;
							if (hostInfo.mWaitTime <= 0) {
								mRecentHosts.RemoveAt(i);
								mRecentHostsLookup.Remove(hostInfo.mHostGamertag);
                                if (TopScore.IsLogging) TopScore.WriteLine(string.Format("{0}: Excluded host accepted again: [{1}]", mMode, hostInfo.mHostGamertag));
							}
						}
					}

					switch (mState) {
						case State.IDLE:
                            if (s_GoSlowDisabled)
                            {
                                lock (SYNC)
                                {
                                    if (s_GoSlowDisabled)
                                    {
                                        if (mSessionInterval > mSessionIntervalDelay)
                                            mSessionInterval = mSessionIntervalDelay;
                                        s_GoSlowDisabled = false;
                                    }
                                }
                            }

                            mSessionInterval -= elapsedMillis;
                            if (mSessionInterval > 0)
                                break;

							switch (mMode) {
								case Mode.UNKNOWN: handleIdleUnknown(); break;
								case Mode.SERVER: handleIdleServer(elapsedMillis); break;
								case Mode.CLIENT: handleIdleClient(); break;
							}
							break;

						case State.SEND:
							if (mSession != null)
								sendNextEntry();
							break;

						case State.RECEIVE:
							if (mSession != null)
								receiveNextEntry();
							break;

						case State.WAIT_FOR_END:
							if (mSession != null)
								Thread.Sleep(SLEEP_DURATION);
							break;
					}
				}
				catch {
					Thread.Sleep(SLEEP_DURATION);
				}
			}

			if (mAction == Action.EXIT_THREAD)
				mAction = Action.DISABLE;

            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": THREAD EXIT");
		}

		private bool findServer()
		{
            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Looking for server");

			NetworkSessionProperties searchProperties = new NetworkSessionProperties();
			searchProperties[(int)SessionProperty.VERSION] = mVersion;

			AvailableNetworkSessionCollection availableSessions;
			try {
				availableSessions = NetworkSession.Find(SESSION_TYPE, mHostGamer, searchProperties);
			}
			catch (Exception) {
				return false;
			}

			foreach (AvailableNetworkSession availableSession in availableSessions) {
                if (TopScore.IsLogging) TopScore.WriteLine(string.Format("{0}: Server found: [{1}]", mMode, availableSession.HostGamertag));

                if (!mRecentHostsLookup.ContainsKey(availableSession.HostGamertag)) {
                    NetworkSession session; // use local variable for sync reasons
                    try
                    {
                        session = NetworkSession.Join(availableSession);
                    }
                    catch (Exception)
                    {
                        if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Server went away during join");
                        Thread.Sleep(SLEEP_DURATION);
                        return false;
                    }
                    session.SessionEnded += new EventHandler<NetworkSessionEndedEventArgs>(clientSide_serverHasDisconnected);
                    session.LocalGamers[0].IsReady = true;
                    mMode = Mode.CLIENT;
                    mSession = session;	// only assign to mSession after all parameters are set
                    mLocalGamerForSendingAndReceiving = null;
                    mRemoteGamerForSending = null;
                    disableVoice();
                    if (TopScore.IsLogging) TopScore.WriteLine(string.Format("{0}: Server joined: [{1}]", mMode, availableSession.HostGamertag), Color.Yellow);
                    return true;
                } 
                else {
                    if (TopScore.IsLogging) TopScore.WriteLine(string.Format("{0}: Server skipped as excluded: [{1}]", mMode, availableSession.HostGamertag), Color.Orange);
                }
			}
			return false;
		}

		private void startServerSession()
		{
            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Starting session");

			NetworkSessionProperties sessionProperties = new NetworkSessionProperties();
			sessionProperties[(int)SessionProperty.VERSION] = mVersion;

			NetworkSession session; // use local variable for sync reasons
			try {
				session = NetworkSession.Create(SESSION_TYPE, mHostGamer, 2, 0, sessionProperties);
			}
			catch (Exception) {
				return;
			}
			session.AllowHostMigration = false;
			session.AllowJoinInProgress = false;
			session.GamerLeft += new EventHandler<GamerLeftEventArgs>(serverSide_clientHasDisconnected);
			session.LocalGamers[0].IsReady = true;

			mServerMillisToLive = mRandom.Next(MINIMUM_SERVER_TIME, MAXIMUM_SERVER_TIME);
			mMode = Mode.SERVER;
			mSession = session;	// only assign to mSession after all parameters are set
			mLocalGamerForSendingAndReceiving = null;
			mRemoteGamerForSending = null;
            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Server time to live: " + (mServerMillisToLive / 1000.0) + "sec", Color.SkyBlue);
		}

		private void handleIdleUnknown()
		{
			if (!findServer())
				startServerSession();
		}

		private void handleIdleServer(double elapsedMillis)
		{
			if (mSession != null) {
				switch (mSession.SessionState) {
					case NetworkSessionState.Lobby:
						mServerMillisToLive -= elapsedMillis;
						if (mServerMillisToLive > 0) {
							if (mSession.RemoteGamers.Count == 1 && mSession.IsEveryoneReady) {
                                if (TopScore.IsLogging) TopScore.WriteLine(string.Format("{0}: Client [{1}] has connected, starting transfer", mMode, mSession.RemoteGamers[0].Gamertag), Color.LawnGreen);
								mSyncTarget.startSynchronization();
								mState = State.RECEIVE;
								disableVoice();
								mSession.StartGame();
							}
							else
								Thread.Sleep(SLEEP_DURATION);
						}
						else {
                            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Server timed out");
							endSession();
						}
						break;
					case NetworkSessionState.Ended:
                        if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Session ended externally");
						endSession();
						break;
					//case NetworkSessionState.Playing:  Never happens
					//    break;
				}
			}
		}

		private void handleIdleClient()
		{
            if (mSession != null)
            {
                switch (mSession.SessionState)
                {
                    case NetworkSessionState.Lobby:
                        Thread.Sleep(SLEEP_DURATION);
                        break;
                    case NetworkSessionState.Playing:
                        if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Transfer started, switching to send mode");
                        mSyncTarget.startSynchronization();
                        mSyncTarget.prepareForSending();
                        mState = State.SEND;
                        break;
                    case NetworkSessionState.Ended:
                        if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Session ended externally");
                        endSession();
                        break;
                }
            }
#if LIVE_SCORES_STRESS_TEST
            else
    #if XBOX
                findServer();
    #else
            {
                if (!findServer())
                {
                    ++mClientRetries;

                    if (mClientRetries == 10)
                    {
                        mClientRetries = 0;
                        mMode = Mode.UNKNOWN;
                    }
                }
            }
    #endif
#endif
		}

		private void sendNextEntry()
		{
			if (mSession.BytesPerSecondSent == 0) {
				if (mLocalGamerForSendingAndReceiving == null) {
					GamerCollection<LocalNetworkGamer> localGamers = mSession.LocalGamers;
					if (localGamers.Count > 0)
						mLocalGamerForSendingAndReceiving = localGamers[0];
					return;
				}
				else if (mRemoteGamerForSending == null) {
					GamerCollection<NetworkGamer> remoteGamers = mSession.RemoteGamers;
					if (remoteGamers.Count > 0) {
						mRemoteGamerForSending = remoteGamers[0];
						disableVoice();
					}
					return;
				}
				else {
					bool wasLastRecord = mSyncTarget.writeTransferRecord(mWriter);
					mLocalGamerForSendingAndReceiving.SendData(mWriter, SendDataOptions.ReliableInOrder, mRemoteGamerForSending);
					Thread.Sleep(5);

					if (wasLastRecord) {
						if (mMode == Mode.SERVER) {
                            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Sending completed, waiting for end from client");
							mState = State.WAIT_FOR_END;
						}
						else {
                            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Sending completed, switching to receive mode");
							mState = State.RECEIVE;
						}
					}
				}
			}

			// Sleep before returning
			Thread.Sleep(SLEEP_DURATION);
		}

		private void receiveNextEntry()
		{
			if (mLocalGamerForSendingAndReceiving == null) {
				GamerCollection<LocalNetworkGamer> localGamers = mSession.LocalGamers;
				if (localGamers.Count > 0)
					mLocalGamerForSendingAndReceiving = localGamers[0];
				return;
			}
			else if (mLocalGamerForSendingAndReceiving.IsDataAvailable) {
				NetworkGamer sender;
				mLocalGamerForSendingAndReceiving.ReceiveData(mReader, out sender);
				Debug.Assert(!sender.IsLocal);
				bool wasLastRecord = mSyncTarget.readTransferRecord(mReader);
				if (wasLastRecord) {
                    if (TopScore.IsLogging) TopScore.WriteLine(string.Format("{0}: Excluding server [{1}] for the next {2} sec", mMode, sender.Gamertag, (WAIT_TIME_FOR_RECENT_HOST / 1000.0)));
					mRecentHosts.Add(new HostInfo(sender.Gamertag, WAIT_TIME_FOR_RECENT_HOST));
					mRecentHostsLookup[sender.Gamertag] = "";

					if (mMode == Mode.SERVER) {
                        if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Receiving completed, switching to send mode");
						mSyncTarget.prepareForSending();
						mState = State.SEND;
					}
					else {
                        if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Receiving completed, ending session on client side");
						endSession();
					}
				}
			}

			// Sleep before returning
			Thread.Sleep(SLEEP_DURATION);
		}

		private void endSession()
		{
			// used both by server and client
			lock (SYNC) {
				if (mSession != null && !mSession.IsDisposed) {
					mSession.Dispose();
					mSyncTarget.endSynchronization();
                    if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Session disposed");
				}
				mSession = null;
				
#if LIVE_SCORES_STRESS_TEST
                if (mShouldIncrementScores)
                    (mSyncTarget as TopScoreListContainer).IncrementScores(11, this);
                mShouldIncrementScores = !mShouldIncrementScores;
                mMode = mShouldIncrementScores ? Mode.CLIENT : Mode.UNKNOWN;
                mState = State.IDLE;
#else
                mMode = Mode.UNKNOWN;
				mState = State.IDLE;
#endif
                mSessionInterval = mSessionIntervalDelay;
            }
		}

		private void disableVoice()
		{
			foreach (LocalNetworkGamer localGamer in mSession.LocalGamers) {
				foreach (NetworkGamer remoteGamer in mSession.RemoteGamers)
					localGamer.EnableSendVoice(remoteGamer, false);
			}

            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ", " + mState + ": Voice disabled");
		}

		public void serverSide_clientHasDisconnected(object sender, GamerLeftEventArgs args)
		{
            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Client ended session, ending session on server side");
			endSession();
		}

		public void clientSide_serverHasDisconnected(object sender, NetworkSessionEndedEventArgs args)
		{
            if (TopScore.IsLogging) TopScore.WriteLine(mMode + ": Server ended session, ending session on client side");
			endSession();
		}

		private class HostInfo
		{
			public string mHostGamertag;
			public double mWaitTime;

			public HostInfo(string hostGamertag, double waitTime)
			{
				mHostGamertag = hostGamertag;
				mWaitTime = waitTime;
			}
		}
	}
}
