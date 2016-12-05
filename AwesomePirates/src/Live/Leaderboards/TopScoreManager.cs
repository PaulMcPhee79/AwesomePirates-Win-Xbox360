using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using System.Diagnostics;
using System.IO;
using SpynDoctor;
using SparrowXNA;

namespace AwesomePirates
{
    class TopScoreManager : SPEventDispatcher
    {
        public const string CUST_EVENT_TYPE_ONLINE_SCORES_STOPPED = "onlineScoresStoppedEvent";

        private static volatile bool s_SyncManagerStopped = false;
        private static readonly object s_lock = new object();

        private const string kDataVersion = "Version_1.0";
        private const string kSaveFileName = "TopScores.dat";
        private const int kTopScoreMaxPosition = 500;
        private const int MILLIS_IN_MINUTE = 60000;

        public TopScoreManager()
        {
            mSyncManager = new OnlineDataSyncManager(0, GameController.GC);
            GameController.GC.Components.Add(mSyncManager);

            TopScoreEntry.SetupReusables();
            Load();

            if (mSyncTarget == null)
                mSyncTarget = new TopScoreListContainer(1, kTopScoreMaxPosition);

#if LIVE_SCORES_STRESS_TEST
            mSyncTarget.PreFill(kTopScoreMaxPosition, mSyncManager);
#endif
        }

        #region Fields
        private bool mSyncManagerStopped = false;
        private OnlineDataSyncManager mSyncManager;
        private TopScoreListContainer mSyncTarget;
        private Dictionary<PlayerIndex, SignedInGamer> mPotentialHosts = new Dictionary<PlayerIndex, SignedInGamer>(4, PlayerIndexComparer.Instance);
        private SPEvent mStoppedEvent = new SPEvent(CUST_EVENT_TYPE_ONLINE_SCORES_STOPPED);
        #endregion

        #region Properties
        public bool IsRunning { get { return mSyncManager != null && !mSyncManager.isStopping() && mSyncManager.Enabled; } }
        public bool IsStopping { get { return mSyncManager != null && mSyncManager.isStopping(); } }
        public bool IsStopped { get { return mSyncManager != null && !mSyncManager.isStopping() && !mSyncManager.Enabled; } }
        public bool ShouldSave { get { return mSyncTarget != null && mSyncTarget.ShouldSave; } }
        public int NumPotentialHosts { get { return mPotentialHosts == null ? 0 : mPotentialHosts.Count; } }
        public int NumTopScores { get { return mSyncTarget != null ? mSyncTarget.getFullListSize(0) : 0; } }
        #endregion

        #region Methods
        public void GoSlow(bool enable)
        {
            if (mSyncManager != null)
                mSyncManager.GoSlow(enable);
        }

        public int GlobalRankForScore(SignedInGamer gamer, int score)
        {
            int rank = -1;
#if SYSTEM_LINK_SESSION
            if (mSyncTarget != null && gamer != null)
#else
            if (mSyncTarget != null && gamer != null && gamer.Privileges.AllowOnlineSessions && !gamer.IsGuest)
#endif
                rank = mSyncTarget.positionFromFullListForScore(0, score);

            if (rank >= kTopScoreMaxPosition)
                rank = -1;
            return rank == -1 ? rank : rank + 1;
        }

        public int GlobalRankForGamer(SignedInGamer gamer)
        {
            int rank = -1;
#if SYSTEM_LINK_SESSION
            if (mSyncTarget != null && gamer != null)
#else
            if (mSyncTarget != null && gamer != null && gamer.Privileges.AllowOnlineSessions && !gamer.IsGuest)
#endif
                rank = mSyncTarget.positionFromFullListForGamertag(0, gamer.Gamertag);
            return rank == -1 ? rank : rank + 1;
        }

        public int FriendsRankForGamer(SignedInGamer gamer)
        {
            int rank = -1;
#if SYSTEM_LINK_SESSION
            if (mSyncTarget != null && gamer != null)
#else
            if (mSyncTarget != null && gamer != null && gamer.Privileges.AllowOnlineSessions && !gamer.IsGuest)
#endif
                rank = mSyncTarget.positionFromFilteredListForGamer(0, gamer);
            return rank == -1 ? rank : rank + 1;
        }

        public int FriendsRankForGamer(SignedInGamer gamer, int score)
        {
            int rank = -1;
#if SYSTEM_LINK_SESSION
            if (mSyncTarget != null && gamer != null)
#else
            if (mSyncTarget != null && gamer != null && gamer.Privileges.AllowOnlineSessions && !gamer.IsGuest)
#endif
                rank = mSyncTarget.positionFromFilteredListForGamer(0, score, gamer);

            if (rank >= 100)
                rank = -1;
            return rank == -1 ? rank : rank + 1;
        }

        public HiScoreTable TopScores(int pageNumber, int numScores)
        {
            // Populate score table
            HiScoreTable topScores = new HiScoreTable(SceneController.LeaderboardFontKey, numScores, pageNumber * numScores);
            if (mSyncTarget != null)
            {
                TopScoreEntry[] page = new TopScoreEntry[numScores];
                mSyncTarget.fillPageFromFullList(0, pageNumber, page);

                foreach (TopScoreEntry entry in page)
                {
                    if (entry == null)
                        break;
                    topScores.InsertScore(entry.Score, entry.Gamertag);
                }
            }

            return topScores;
        }
        
        public HiScoreTable TopFriendsScores(int numScores, SignedInGamer gamer)
        {
            // Populate score table
            HiScoreTable topScores = new HiScoreTable(SceneController.LeaderboardFontKey, numScores);
            if (mSyncTarget != null && gamer != null)
            {
                TopScoreEntry[] page = new TopScoreEntry[numScores];
                mSyncTarget.fillPageThatContainsGamertagFromFilteredList(0, page, gamer);

                foreach (TopScoreEntry entry in page)
                {
                    if (entry == null)
                        break;
                    topScores.InsertScore(entry.Score, entry.Gamertag);
                }

                topScores.PopulateRanks(delegate(int index)
                {
                    if (index < page.Length)
                    {
                        if (page[index] != null && page[index].RankAtLastPageFill > 0)
                            return page[index].RankAtLastPageFill-1;
                    }

                    if (mSyncTarget != null)
                    {
                        int fullListSize = mSyncTarget.getFullListSize(0);
                        return fullListSize > 0 ? fullListSize - 1 : 499;
                    }
                    else
                        return 499;
                });
            }

            return topScores;
        }

        public void AddPotentialHost(SignedInGamer gamer)
        {
#if SYSTEM_LINK_SESSION
            if (gamer != null && !gamer.IsGuest && mPotentialHosts != null && !mPotentialHosts.ContainsKey(gamer.PlayerIndex))
#else
            if (gamer != null && gamer.IsSignedInToLive && gamer.Privileges.AllowOnlineSessions && !gamer.IsGuest && mPotentialHosts != null && !mPotentialHosts.ContainsKey(gamer.PlayerIndex))
#endif
            {
                mPotentialHosts[gamer.PlayerIndex] = gamer;
                if (TopScore.IsLogging) TopScore.WriteLine(string.Format("Potential host added: [{0}]", gamer.Gamertag));
            }
        }

        public void RemovePotentialHost(PlayerIndex playerIndex)
        {
            if (mPotentialHosts != null && mPotentialHosts.ContainsKey(playerIndex))
            {
                mPotentialHosts.Remove(playerIndex);
                if (TopScore.IsLogging) TopScore.WriteLine(string.Format("Potential host removed: [{0}]", playerIndex));

                if (IsRunning && playerIndex == mSyncManager.HostGamer.PlayerIndex)
                    mSyncManager.stop(AfterStop, false);
            }
        }

        public void AddScore(int score, string gamerTag)
        {
            if (mSyncTarget != null) // && mSyncManager != null (let mSyncTarget take care of a null manager)
            {
                mSyncTarget.addEntry(0, score, gamerTag, mSyncManager);
                if (TopScore.IsLogging) TopScore.WriteLine(string.Format("Score of {0} added for [{1}]", score, gamerTag));
            }
        }

        public bool TryStart()
        {
            if (!IsStopped || mSyncManager == null || mSyncTarget == null || mPotentialHosts == null || mPotentialHosts.Count == 0)
                return false;

            for (PlayerIndex hostIndex = PlayerIndex.One; hostIndex <= PlayerIndex.Four; ++hostIndex)
            {
                if (mPotentialHosts.ContainsKey(hostIndex))
                {
                    mSyncManager.start(mPotentialHosts[hostIndex], mSyncTarget);
                    return true;
                }
            }

            return false;
        }

        public void Stop(bool gameExiting)
        {
            if (mSyncManager == null || IsStopped)
                DispatchEvent(mStoppedEvent);
            else if (IsRunning)
                mSyncManager.stop(AfterStop, gameExiting);
            //else we're already stopping and the event will fire when we've stopped.

            // Note: As long as we're satisfied that our client (GameController) won't restart us when the game should be exiting, then
            //       there's no need to be concerned that a non-gameExiting stop in progress can block a game exiting one. The only thing
            //       that the gameExiting argument does is prevent future restarts.
        }

        public void AfterStop()
        {
            lock (s_lock)
            {
                s_SyncManagerStopped = true;
            }   
        }

        public void AdvanceTime(double time)
        {
            if (s_SyncManagerStopped)
            {
                lock (s_lock)
                {
                    s_SyncManagerStopped = false;
                }

                mSyncManagerStopped = true;
            }

            if (mSyncManagerStopped)
            {
                mSyncManagerStopped = false;
                DispatchEvent(mStoppedEvent);
            }
        }

        public void Load()
        {
            try
            {
                if (FileManager.FM.IsReadyGlobal() && FileManager.FM.FileExistsGlobal(FileManager.kSharedStorageContainerName, kSaveFileName))
                {
                    FileManager.FM.LoadGlobal(FileManager.kSharedStorageContainerName, kSaveFileName, stream =>
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                            DecodeWithReader(reader);
                        Debug.WriteLine("Top scores load completed.");
                    });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("An unexpected error occurred when attempting to load top scores. " + e.Message);
            }
        }

        public void Save(bool async = true)
        {
            if (mSyncTarget == null)
                return;

            try
            {
               if (async)
               {
                   FileManager.FM.QueueGlobalSaveAsync(FileManager.kSharedStorageContainerName, kSaveFileName, stream =>
                   {
                       try
                       {
                           using (BinaryWriter writer = new BinaryWriter(stream))
                               EncodeWithWriter(writer);
                           Debug.WriteLine("Top scores save(async) completed.");
                       }
                       catch (Exception eInner)
                       {
                           Debug.WriteLine(eInner.Message);
                       }
                   });
               }
               else if (FileManager.FM.IsReadyGlobal())
               {
                   FileManager.FM.SaveGlobal(FileManager.kSharedStorageContainerName, kSaveFileName, stream =>
                   {
                       try
                       {
                           using (BinaryWriter writer = new BinaryWriter(stream))
                               EncodeWithWriter(writer);
                           Debug.WriteLine("Top scores save(sync) completed.");
                       }
                       catch (Exception eInner)
                       {
                           Debug.WriteLine(eInner.Message);
                       }
                   });
               }
            }
            catch (Exception e)
            {
                Debug.WriteLine("An unexpected error occurred when attempting to save top scores. " + e.Message);
            }
        }

        private void DecodeWithReader(BinaryReader reader)
        {
            // Decrypt buffer
            int count = reader.ReadInt32();

            if (count > 250000)
                throw new Exception("Top score data length is invalid. Loading aborted.");

            byte[] buffer = new byte[count];
            int bufferLen = reader.Read(buffer, 0, count);

            if (bufferLen != count)
                throw new Exception("Top scores could not be loaded due to file length inaccuracies.");
            FileManager.MaskUnmaskBuffer(0x31, buffer, bufferLen);

            BinaryReader br = new BinaryReader(new MemoryStream(buffer));

            // Read Saved Data
            string dataVersion = br.ReadString();
            int keyScore = br.ReadInt32();

            if (mSyncTarget != null)
            {
                mSyncTarget.Dispose();
                mSyncTarget = null;
            }

            mSyncTarget = new TopScoreListContainer(br);

            if (mSyncTarget.KeyScore != keyScore)
            {
                mSyncTarget.Dispose();
                mSyncTarget = null;
                Debug.WriteLine("Invalid Top Score data. Discarding loaded scores.");
            }

            buffer = null;
            br = null;
        }

        private void EncodeWithWriter(BinaryWriter writer)
        {
            BinaryWriter bw = new BinaryWriter(new MemoryStream(50000));
            bw.Write(kDataVersion);
            mSyncTarget.save(bw);

            // Perform basic encryption on buffer
            Stream stream = bw.BaseStream;
            stream.Position = 0;

            byte[] buffer = new byte[(int)stream.Length];
            int bufferLen = stream.Read(buffer, 0, (int)stream.Length);
            FileManager.MaskUnmaskBuffer(0x31, buffer, bufferLen);

            // Write encrypted buffer back to stream
            writer.Write(bufferLen);
            writer.Write(buffer, 0, bufferLen);

            buffer = null;
            bw = null;
        }
        #endregion
    }
}
