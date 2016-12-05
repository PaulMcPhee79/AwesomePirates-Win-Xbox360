/*
Copyright (c) 2010 Spyn Doctor Games (Johannes Hubert). All rights reserved.

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

Last change: 2010-09-16
*/

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using AwesomePirates;

namespace SpynDoctor
{
    public class TopScoreEntry : IReusable
	{
        public const int kMaxGamertagLen = 20;
        private const string kIllegalString = "?";
        private const char kIllegalChar = '?';

        private byte[] mGamertagBytes = new byte[kMaxGamertagLen];
		private StringBuilder mGamertag;
        private string mGamertagCache;
        public string Gamertag
		{
            get
            {
                if (mGamertagCache == null && mGamertag != null)
                    mGamertagCache = mGamertag.ToString();
                return mGamertagCache;
            }
		}

		private int mScore;
		public int Score
		{
			get { return mScore; }
		}

		// Local (true) means: This score was in the list before the latest score transfer.
		// Non-local (false) means: This score was freshly transferred into the list from another Xbox.
		private bool mIsLocalEntry;
		public bool IsLocalEntry
		{
			get { return mIsLocalEntry; }
			set { mIsLocalEntry = value; }
		}

		private int mRankAtLastPageFill;
		public int RankAtLastPageFill
		{
			get { return mRankAtLastPageFill; }
			set { mRankAtLastPageFill = value; }
		}

		public TopScoreEntry(string gamertag, int score)
		{
			// freshly created local entry
            mInUse = true;
            mPoolIndex = -1;
            ProcessDefaultConstructor(gamertag, score, true);
		}

		public TopScoreEntry(BinaryReader reader, bool isLocalEntry)
		{
			// isLocalEntry == true: local entry read from storage
			// isLocalEntry == false: entry from remote source read from online transfer
            mInUse = true;
            mPoolIndex = -1;

            if (reader != null)
            {
                mGamertag = new StringBuilder(kMaxGamertagLen);
                readGamerTag(reader, mGamertag, mGamertagBytes);
                mScore = reader.ReadInt32();
                mIsLocalEntry = isLocalEntry;
            }
            else
                ProcessDefaultConstructor(kIllegalString, 0, isLocalEntry);
		}

        private void ProcessDefaultConstructor(string gamertag, int score, bool isLocalEntry)
        {
            string temp = gamertag.Length >= kMaxGamertagLen ? kIllegalString : Locale.SanitizeText(gamertag, SceneController.LeaderboardFontKey, HiScoreTable.kLeaderboardFontSize);
            System.Text.Encoding.UTF8.GetBytes(temp, 0, Math.Min(temp.Length, mGamertagBytes.Length), mGamertagBytes, 0);
            mGamertag = new StringBuilder(temp, kMaxGamertagLen);
            mScore = score;
            mIsLocalEntry = isLocalEntry;
        }

        private static void readGamerTag(BinaryReader reader, StringBuilder sb, byte[] bytes)
        {
            Debug.Assert(reader != null && sb != null && bytes != null, "TopScoreEntry::readGamerTag - Bad parameters.");
            if (reader == null || sb == null || bytes == null)
                return;

            try
            {
                int tagLen = reader.ReadInt32();
                int readLen = Math.Min(bytes.Length, tagLen);
                reader.Read(bytes, 0, readLen);

                if (readLen < tagLen)
                    reader.BaseStream.Position += tagLen - readLen;
                sb.Length = 0;

                for (int i = 0; i < readLen; ++i)
                    sb.Append((char)bytes[i]);
            }
            catch (Exception)
            {
                sb.Length = 0;
            }

            Locale.SanitizeText(sb, SceneController.LeaderboardFontKey, HiScoreTable.kLeaderboardFontSize);
        }

        public int compareTo(TopScoreEntry other)
		{
            Debug.Assert(other != null, "TopScoreEntry::compareTo argument was null.");
            if (other == null)
                return 0;

			if (mScore < other.mScore)		// lower score is lower in rank
				return -1;
			else if (mScore > other.mScore) // higher score is higher in rank
				return 1;
			else							// same score, same rank
				return 0;
		}

		public void write(BinaryWriter writer)
		{
            if (writer == null)
                return;

            int len = mGamertag.Length;
            writer.Write(len);
            writer.Write(mGamertagBytes, 0, len);
			writer.Write(mScore);
			// mRankAtLastPageFill is not saved by design!
		}

		public bool isLegal()
		{
            if (mGamertag == null || mGamertag.Length < 1 || mGamertag.Length >= kMaxGamertagLen || mScore < 0)
				return false;
            mGamertagCache = null; // Mark cache as dirty.

            for (int i = 0; i < mGamertag.Length; ++i)
            {
                if (mGamertag[i] == '?')
                    return false;
            }

            return true;

            /*
			for (int i = 0; i < mGamertag.Length; i++) {
				char c = mGamertag[i];
				if (!(c == ' ' || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9'))
					return false;
			}
			return true;
            */
		}

        public class TopScoreEqualityComparer : IEqualityComparer<TopScoreEntry>
        {
            private int[] mPrimes = new int[] { 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163 };

            public bool Equals(TopScoreEntry x, TopScoreEntry y)
            {
                if (x == null && y == null)
                    return true;
                else if (x == null || y == null)
                    return false;
                else if (x.mGamertagBytes.Length != y.mGamertagBytes.Length)
                    return false;

                for (int i = 0; i < x.mGamertagBytes.Length; ++i)
                {
                    if (x.mGamertagBytes[i] != y.mGamertagBytes[i])
                        return false;
                }

                return true;
            }

            public int GetHashCode(TopScoreEntry x)
            {
                if (x == null)
                    return 0;

                int hashCode = 0, limit = Math.Min(x.mGamertagBytes.Length, mPrimes.Length);

                for (int i = 0; i < limit; ++i)
                    hashCode += (int)x.mGamertagBytes[i] ^ mPrimes[i];

                //Debug.WriteLine("HASHCODE for {0}: {1}", x.Gamertag, hashCode);
                return hashCode;
            }
        }


#if LIVE_SCORES_STRESS_TEST
        internal void IncrementScore(int amount)
        {
            mScore += amount;
        }
#endif

        // IResuable
        private const uint kTopScoreReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public uint ReuseKey { get { return kTopScoreReuseKey; } }
        private bool mInUse;
        public bool InUse { get { return mInUse; } }
        private int mPoolIndex;
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 1000;
            uint reuseKey = kTopScoreReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetTopScoreEntry("Unknown Swabby", 0);
                reusable.Hibernate();
                sCache.AddReusable(reusable);
            }

            sCache.VerifyCacheIntegrity();
            sCaching = false;
        }

        private static IReusable CheckoutReusable(uint reuseKey)
        {
            IReusable reusable = null;

            if (sCache != null && !sCaching)
                reusable = sCache.Checkout(reuseKey);

            return reusable;
        }

        private static void CheckinReusable(IReusable reusable)
        {
            if (sCache != null && !sCaching)
                sCache.Checkin(reusable);
        }

        public static TopScoreEntry GetTopScoreEntry(BinaryReader reader, bool isLocalEntry)
        {
            TopScoreEntry entry = CheckoutReusable(kTopScoreReuseKey) as TopScoreEntry;

            if (entry != null)
            {
                entry.Reuse();
                readGamerTag(reader, entry.mGamertag, entry.mGamertagBytes);
                entry.mScore = reader.ReadInt32();
                entry.IsLocalEntry = isLocalEntry;
            }
            else
            {
                entry = new TopScoreEntry(reader, isLocalEntry);
            }

            return entry;
        }

        public static TopScoreEntry GetTopScoreEntry(string gamertag, int score)
        {
            string temp = gamertag == null || gamertag.Length >= kMaxGamertagLen
                ? kIllegalString
                : Locale.SanitizeText(gamertag, SceneController.LeaderboardFontKey, HiScoreTable.kLeaderboardFontSize);
            TopScoreEntry entry = CheckoutReusable(kTopScoreReuseKey) as TopScoreEntry;

            if (entry != null)
            {
                entry.Reuse();

                int tagLen = Math.Min(temp.Length, entry.mGamertagBytes.Length);
                System.Text.Encoding.UTF8.GetBytes(temp, 0, tagLen, entry.mGamertagBytes, 0);
                entry.mGamertag.Length = 0;
                for (int i = 0; i < tagLen; ++i)
                    entry.mGamertag.Append((char)entry.mGamertagBytes[i]);
                entry.mScore = score;
                entry.IsLocalEntry = true;
            }
            else
            {
                entry = new TopScoreEntry(temp, score);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed TopScoreEntry ReusableCache.");
#endif
            }

            return entry;
        }

        public void Reuse()
        {
            if (InUse)
                return;
            Array.Clear(mGamertagBytes, 0, mGamertagBytes.Length);
            mGamertag.Length = 0;
            mGamertagCache = null;
            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;
            mInUse = false;
            CheckinReusable(this);
        }
	}
}
