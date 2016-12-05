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
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;

namespace SpynDoctor
{
	public class TopScoreList : IDisposable
	{
        protected bool mIsDisposed = false;
		private int mMaxSize;
		private List<TopScoreEntry> mEntryList;
		private List<TopScoreEntry> mFilteredList;
		private Dictionary<TopScoreEntry, TopScoreEntry> mEntryMap;

		private readonly object SYNC = new object();

		public TopScoreList(int maxSize)
		{
			mMaxSize = Math.Max(1, maxSize);
			mEntryList = new List<TopScoreEntry>();
			mFilteredList = new List<TopScoreEntry>();
			mEntryMap = new Dictionary<TopScoreEntry, TopScoreEntry>(new TopScoreEntry.TopScoreEqualityComparer());
		}

		public TopScoreList(BinaryReader reader)
			: this(reader != null ? reader.ReadInt32() : 500)
		{
            if (reader != null)
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    TopScoreEntry entry = TopScoreEntry.GetTopScoreEntry(reader, true);
                    if (entry.isLegal())
                    {
                        mEntryMap[entry] = entry;
                        mEntryList.Add(entry);
                    }
                }
            }
		}

        internal int KeyScore
        {
            get
            {
                int i = 1, keyScore = 0;
                if (mEntryList != null)
                {
                    foreach (TopScoreEntry entry in mEntryList)
                        keyScore += entry.Score / (mMaxSize + i++);
                }

                return keyScore;
            }
        }

        public int positionFromFullList(int score)
        {
            lock (SYNC)
            {
                if (mEntryList != null)
                {
                    for (int i = 0; i < mEntryList.Count; ++i)
                    {
                        if (mEntryList[i].Score < score)
                            return i;
                    }

                    return mEntryList.Count;
                }
                return -1;
            }
        }

        public int positionFromFullList(string gamertag)
        {
            lock (SYNC)
            {
                if (gamertag != null && mEntryList != null)
                {
                    for (int i = 0; i < mEntryList.Count; ++i)
                    {
                        if (mEntryList[i].Gamertag == gamertag)
                            return i;
                    }
                }
                return -1;
            }
        }

        public int positionFromFilteredList(int score, SignedInGamer gamer)
        {
            lock (SYNC)
            {
                if (gamer != null)
                {
                    initFilteredList(gamer, false);

                    if (mFilteredList != null)
                    {
                        for (int i = 0; i < mFilteredList.Count; i++)
                        {
                            if (mFilteredList[i].Score < score)
                                return i;
                        }

                        return mFilteredList.Count;
                    }
                }
                return -1;
            }
        }

        public int positionFromFilteredList(SignedInGamer gamer)
        {
            lock (SYNC)
            {
                if (gamer != null)
                {
                    initFilteredList(gamer, false);

                    if (mFilteredList != null)
                    {
                        for (int i = 0; i < mFilteredList.Count; i++)
                        {
                            if (mFilteredList[i].Gamertag == gamer.Gamertag)
                                return i;
                        }
                    }
                }
                return -1;
            }
        }

		public bool containsEntryForGamertag(string gamertag)
		{
			lock (SYNC) {
                if (gamertag != null && mEntryList != null)
                {
                    for (int i = 0; i < mEntryList.Count; i++)
                    {
                        if (mEntryList[i].Gamertag == gamertag)
                            return true;
                    }
                }
				return false;
			}
		}

		public int getFullCount()
		{
			lock (SYNC) {
				return mEntryList.Count;
			}
		}

		public int getFilteredCount(SignedInGamer gamer)
		{
			lock (SYNC) {
				initFilteredList(gamer, false);
				return mFilteredList.Count;
			}
		}

		public void fillPageFromFullList(int pageNumber, TopScoreEntry[] page)
		{
			lock (SYNC) {
				fillPage(mEntryList, true, pageNumber, page);
			}
		}

		public void fillPageFromFilteredList(int pageNumber, TopScoreEntry[] page, SignedInGamer gamer)
		{
			lock (SYNC) {
				initFilteredList(gamer, true);
				fillPage(mFilteredList, false, pageNumber, page);
			}
		}

		public int fillPageThatContainsGamertagFromFullList(TopScoreEntry[] page, string gamertag)
		{
			lock (SYNC) {
				int indexOfGamertag = 0;
				for (int i = 0; i < mEntryList.Count; i++) {
					if (mEntryList[i].Gamertag == gamertag) {
						indexOfGamertag = i;
						break;
					}
				}
				int pageNumber = indexOfGamertag / page.Length;
				fillPage(mEntryList, true, pageNumber, page);
				return pageNumber;
			}
		}

		public int fillPageThatContainsGamertagFromFilteredList(TopScoreEntry[] page, SignedInGamer gamer)
		{
            if (page == null || page.Length == 0 || gamer == null)
                return -1;

			lock (SYNC) {
				initFilteredList(gamer, true);

				int indexOfGamertag = 0;
				for (int i = 0; i < mFilteredList.Count; i++) {
					if (mFilteredList[i].Gamertag == gamer.Gamertag) {
						indexOfGamertag = i;
						break;
					}
				}
				int pageNumber = indexOfGamertag / page.Length;
				fillPage(mFilteredList, false, pageNumber, page);
				return pageNumber;
			}
		}

		private void fillPage(List<TopScoreEntry> list, bool initRank, int pageNumber, TopScoreEntry[] page)
		{
            if (list == null || page == null)
                return;

			int index = pageNumber * page.Length;
			for (int i = 0; i < page.Length; i++) {
				if (index >= 0 && index < list.Count) {
					page[i] = list[index];
					if (initRank)
						page[i].RankAtLastPageFill = index + 1;
				}
				else
					page[i] = null;
				index++;
			}
		}

		private void initFilteredList(SignedInGamer gamer, bool initRank)
		{
            if (gamer == null)
            {
                if (mFilteredList != null)
                    mFilteredList.Clear();
                return;
            }

			string gamertag = gamer.Gamertag;
            FriendCollection friendsFilter = null;
            try { friendsFilter = gamer.GetFriends(); }
            catch (Exception) { }
			mFilteredList.Clear();
			for (int i = 0; i < mEntryList.Count; i++) {
				TopScoreEntry entry = mEntryList[i];
				if (entry.Gamertag == gamertag) {
					mFilteredList.Add(entry);
					if (initRank)
						entry.RankAtLastPageFill = i + 1;
				}
				else if (friendsFilter != null) {
					foreach (FriendGamer friend in friendsFilter) {
						if (entry.Gamertag == friend.Gamertag) {
							mFilteredList.Add(entry);
							if (initRank)
								entry.RankAtLastPageFill = i + 1;
							break;
						}
					}
				}
			}

            // I think this friends collection is managed by the XNA framework and should not be altered.
            //try {
            //    if (friendsFilter != null && !friendsFilter.IsDisposed)
            //        friendsFilter.Dispose();
            //}
            //catch (Exception) { }
		}

		public void write(BinaryWriter writer)
		{
            Debug.Assert(writer != null, "TopScoreList::write's writer was null.");
            if (writer == null)
                return;

			lock (SYNC) {
				writer.Write(mMaxSize);
				writer.Write(mEntryList.Count);
				foreach (TopScoreEntry entry in mEntryList)
					entry.write(writer);
			}
		}

		public bool addEntry(TopScoreEntry entry)
		{
			if (entry == null || !entry.isLegal())
			    return false;

			lock (SYNC) {
                if (mEntryMap.ContainsKey(entry)) {
					// Existing entry found for this gamertag
                    TopScoreEntry existingEntry = mEntryMap[entry];
					int compareValue = entry.compareTo(existingEntry);
					if (compareValue < 0) {
						// new entry is smaller: do not insert
						return false;
					}
					else if (compareValue == 0) {
						// both entries are equal: Keep existing entry but transfer "IsLocal" state
						existingEntry.IsLocalEntry = entry.IsLocalEntry;
						return false;
					}
					else {
						// existing entry is smaller: replace with new entry
						mEntryList.Remove(existingEntry);
                        mEntryMap.Remove(existingEntry); // *** MUST remove old key because keys are pooled and mutable! ***
						addNewEntry(entry);	// this also replaces existing entry in mEntryMap
                        existingEntry.Hibernate();
						return true;
					}
				}
				else
					return addNewEntry(entry);
			}
		}

		private bool addNewEntry(TopScoreEntry entry)
		{
            if (entry == null)
                return false;

			for (int i = 0; i < mEntryList.Count; i++) {
				if (entry.compareTo(mEntryList[i]) >= 0) {
					// Found existing smaller entry: Insert this one before
					mEntryList.Insert(i, entry);
					mEntryMap[entry] = entry;
					// Delete last entry if there are now too many
					if (mEntryList.Count > mMaxSize) {
						TopScoreEntry removedEntry = mEntryList[mMaxSize];
						mEntryList.RemoveAt(mMaxSize);
						mEntryMap.Remove(removedEntry);
                        removedEntry.Hibernate();
					}
					return true;
				}
			}

			// No existing smaller entry found, but still space in list: Add at end
			if (mEntryList.Count < mMaxSize) {
				mEntryList.Add(entry);
				mEntryMap[entry] = entry;
				return true;
			}

			// Entry added at end or No existing smaller entry found and list is full: Do not add
			return false;
		}

		public void initForTransfer()
		{
			lock (SYNC) {
				foreach (TopScoreEntry entry in mEntryList)
					entry.IsLocalEntry = true; // at the beginning of a transfer, all entries are local
			}
		}

		public int writeNextTransferEntry(PacketWriter writer, int myListIndex, int entryIndex)
		{
            Debug.Assert(writer != null, "TopScoreList::writeNextTransferEntry writer was null.");
            if (writer == null)
                return -1;

			lock (SYNC) {
				while (entryIndex < mEntryList.Count) {
					// While there are still more entries in the current list:
					// Find a local entry that needs transfer
					if (mEntryList[entryIndex].IsLocalEntry) {
                        if (TopScore.IsLogging) TopScore.Write("*"); // local entry transferred
						writer.Write(TopScoreListContainer.MARKER_ENTRY);
						writer.Write((byte)myListIndex);
						mEntryList[entryIndex].write(writer);
						return entryIndex + 1;
					}
					else {
                        if (TopScore.IsLogging) TopScore.Write("~"); // remote entry skipped
						entryIndex++;
						Thread.Sleep(1);
					}
				}
				return -1;
			}
		}

		public bool readTransferEntry(PacketReader reader)
		{
			lock (SYNC) {
                TopScoreEntry entry = TopScoreEntry.GetTopScoreEntry(reader, false);
				if (addEntry(entry))
                    return true;
                else
                {
                    entry.Hibernate();
                    return false;
                }
			}
		}

#if LIVE_SCORES_STRESS_TEST
        internal void IncrementScores(int amount)
        {
            foreach (TopScoreEntry entry in mEntryList)
                entry.IncrementScore(amount);
        }
#endif

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mEntryList != null)
                        {
                            foreach (TopScoreEntry entry in mEntryList)
                                entry.Hibernate();
                            mEntryList = null;
                        }

                        mEntryMap = null;
                        mFilteredList = null;
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
                finally
                {
                    mIsDisposed = true;
                }
            }
        }

        ~TopScoreList()
        {
            Dispose(false);
        }
	}
}
