using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AwesomePirates
{
    class ObjectivesRank
    {
        public const int kNumObjectivesPerRank = 3;

        public const uint RANK_UNRANKED = 0;
        public const uint RANK_SWABBY = 1;
        public const uint RANK_DECKHAND = 2;
        public const uint RANK_JACK_TAR = 3;
        public const uint RANK_OLD_SALT = 4;
        public const uint RANK_HELMSMAN = 5;
        public const uint RANK_SEA_DOG = 6;
        public const uint RANK_VILLAIN = 7;
        public const uint RANK_BRIGAND = 8;
        public const uint RANK_LOOTER = 9;
        public const uint RANK_GALLOWS_BIRD = 10;
        public const uint RANK_SCOUNDREL = 11;
        public const uint RANK_ROGUE = 12;
        public const uint RANK_PILLAGER = 13;
        public const uint RANK_PLUNDERER = 14;
        public const uint RANK_FREEBOOTER = 15;
        public const uint RANK_PRIVATEER = 16;
        public const uint RANK_CORSAIR = 17;
        public const uint RANK_BUCCANEER = 18;
        public const uint RANK_SEA_WOLF = 19;
        public const uint RANK_SWASHBUCKLER = 20;
        public const uint RANK_CALICO_JACK = 21;
        public const uint RANK_BLACK_BART = 22;
        public const uint RANK_BARBAROSSA = 23;
        public const uint RANK_CAPTAIN_KIDD = 24;

        public const uint MAX_OBJECTIVES_RANK = RANK_CAPTAIN_KIDD;
        public const uint MAX_OBJECTIVES_RANK_TRIAL = RANK_HELMSMAN;
        public const int NUM_OBJECTIVES_RANKS = 25;

        public ObjectivesRank(uint rank)
        {
            mRank = rank;
            mObjectiveDescs = ObjectivesRank.ObjectivesDescriptionsForRank(rank);
        }

        #region Fields
        private uint mRank;
        private List<ObjectivesDescription> mObjectiveDescs;
        #endregion

        #region Properties
        public bool IsCompleted
        {
            get
            {
                bool completed = true;
    
                foreach (ObjectivesDescription objDesc in mObjectiveDescs)
                    completed = completed && objDesc.IsCompleted;
    
                return completed;
            }
        }
        public bool IsMaxRank { get { return (mRank == ObjectivesRank.MaxRank); } }
        public uint Rank { get { return mRank; } }
        public uint DisplayRank { get { return mRank + 1; } }
        public string Title { get { return ObjectivesRank.TitleForRank(mRank); } }
        public uint RequiredNpcShipType
        {
            get
            {
                uint shipType = 0;
    
                foreach (ObjectivesDescription objDesc in mObjectiveDescs) {
                    if (!objDesc.IsCompleted)
                        shipType = ObjectivesDescription.RequiredNpcShipTypeForKey(objDesc.Key);
        
                    if (shipType != 0)
                        break;
                }
    
                return shipType;
            }
        }
        public uint RequiredAshType
        {
            get
            {
                uint ashType = 0;
    
                foreach (ObjectivesDescription objDesc in mObjectiveDescs) {
                    if (!objDesc.IsCompleted)
                        ashType = ObjectivesDescription.RequiredAshTypeForKey(objDesc.Key);
        
                    if (ashType != 0)
                        break;
                }
    
                return ashType;
            }
        }
        public static uint MaxRank { get { return GameController.GC.IsTrialMode ? MAX_OBJECTIVES_RANK_TRIAL : MAX_OBJECTIVES_RANK; } }
        #endregion

        #region Methods
        public virtual ObjectivesRank Clone()
        {
            ObjectivesRank clone = MemberwiseClone() as ObjectivesRank;

            clone.mObjectiveDescs = new List<ObjectivesDescription>(mObjectiveDescs.Count);
            foreach (ObjectivesDescription objDesc in mObjectiveDescs)
                clone.mObjectiveDescs.Add(objDesc.Clone());

            return clone;
        }

        public virtual void DecodeWithReader(BinaryReader reader)
        {
            mRank = reader.ReadUInt32();
            mObjectiveDescs = ObjectivesRank.ObjectivesDescriptionsForRank(mRank);

            foreach (ObjectivesDescription objDesc in mObjectiveDescs)
                objDesc.DecodeWithReader(reader);
        }

        public virtual void EncodeWithWriter(BinaryWriter writer)
        {
            writer.Write(mRank);

            foreach (ObjectivesDescription objDesc in mObjectiveDescs)
                objDesc.EncodeWithWriter(writer);
        }

        public void ForceCompletion()
        {
            foreach (ObjectivesDescription objDesc in mObjectiveDescs)
                objDesc.ForceCompletion();
        }

        public void PrepareForNewGame()
        {
            foreach (ObjectivesDescription objDesc in mObjectiveDescs)
            {
                if (!objDesc.IsCompleted)
                    objDesc.Reset();
            }
        }

        public ObjectivesDescription ObjectiveDescAtIndex(int index)
        {
            ObjectivesDescription objDesc = null;

            if (index < mObjectiveDescs.Count)
                objDesc = mObjectiveDescs[index];

            return objDesc;
        }

        public bool IsObjectiveCompletedAtIndex(int index)
        {
            bool completed = true;
    
            if (index < mObjectiveDescs.Count)
                completed = mObjectiveDescs[index].IsCompleted;
    
            return completed;
        }

        public bool IsObjectiveFailedAtIndex(int index)
        {
            bool failed = true;

            if (index < mObjectiveDescs.Count)
                failed = mObjectiveDescs[index].IsFailed;

            return failed;
        }

        public int ObjectiveCountAtIndex(int index)
        {
            int count = 0;
    
            if (index < mObjectiveDescs.Count)
                count = mObjectiveDescs[index].Count;
    
            return count;
        }

        public int ObjectiveQuotaAtIndex(int index)
        {
            int quota = 0;

            if (index < mObjectiveDescs.Count)
                quota = mObjectiveDescs[index].Quota;

            return quota;
        }

        public string ObjectiveTextAtIndex(int index)
        {
            string text = null;
    
            if (index < mObjectiveDescs.Count)
                text = mObjectiveDescs[index].Description;
    
            return text;
        }

        public string ObjectiveLogbookTextAtIndex(int index)
        {
            string text = null;

            if (index < mObjectiveDescs.Count)
                text = mObjectiveDescs[index].LogbookDescription;

            return text;
        }

        public void IncreaseObjectiveCountAtIndex(int index, int amount)
        {
            if (index < mObjectiveDescs.Count)
            {
                ObjectivesDescription objDesc = mObjectiveDescs[index];
        
                if (!objDesc.IsFailed)
                    objDesc.Count += amount;
            }
        }

        public void SetObjectiveCountAtIndex(int count, int index)
        {
            if (index < mObjectiveDescs.Count)
            {
                ObjectivesDescription objDesc = mObjectiveDescs[index];
                objDesc.Count = count;
            }
        }

        public void SetObjectiveFailedAtIndex(bool isFailed, int index)
        {
            if (index < mObjectiveDescs.Count)
            {
                ObjectivesDescription objDesc = mObjectiveDescs[index];
                objDesc.IsFailed = isFailed;
            }
        }

        public void SyncWithObjectivesRank(ObjectivesRank objRank)
        {
            if (objRank == null)
                throw new ArgumentNullException("Cannot sync ObjectivesRank with null.");

            int i = 0;
    
            foreach (ObjectivesDescription objDesc in mObjectiveDescs)
            {
                objDesc.Count = objRank.ObjectiveCountAtIndex(i);
                objDesc.IsFailed = objRank.IsObjectiveFailedAtIndex(i);
                ++i;
            }
        }

        public void UpgradeToObjectivesRank(ObjectivesRank objRank)
        {
            int i = 0;
    
            foreach (ObjectivesDescription objDesc in mObjectiveDescs) {
                int count = objRank.ObjectiveCountAtIndex(i);
        
                if (count > objDesc.Count)
                    objDesc.Count = count;

                bool isFailed = objRank.IsObjectiveFailedAtIndex(i);
        
                if (objDesc.IsFailed && !isFailed)
                    objDesc.IsFailed = isFailed;
        
                ++i;
            }
        }

        public static int MultiplierForRank(uint rank)
        {
            return (int)rank + 10;
        }

        public static string TitleForRank(uint rank)
        {
            string title = null;

            switch (rank)
            {
                case RANK_UNRANKED: title = "Unranked"; break;
                case RANK_SWABBY: title = "Swabby"; break;
                case RANK_DECKHAND: title = "Deckhand"; break;
                case RANK_JACK_TAR: title = "Jack Tar"; break;
                case RANK_OLD_SALT: title = "Old Salt"; break;
                case RANK_HELMSMAN: title = "Helmsman"; break;
                case RANK_SEA_DOG: title = "Sea Dog"; break;
                case RANK_VILLAIN: title = "Villain"; break;
                case RANK_BRIGAND: title = "Brigand"; break;
                case RANK_LOOTER: title = "Looter"; break;
                case RANK_GALLOWS_BIRD: title = "Gallows Bird"; break;
                case RANK_SCOUNDREL: title = "Scoundrel"; break;
                case RANK_ROGUE: title = "Rogue"; break;
                case RANK_PILLAGER: title = "Pillager"; break;
                case RANK_PLUNDERER: title = "Plunderer"; break;
                case RANK_FREEBOOTER: title = "Freebooter"; break;
                case RANK_PRIVATEER: title = "Privateer"; break;
                case RANK_CORSAIR: title = "Corsair"; break;
                case RANK_BUCCANEER: title = "Buccaneer"; break;
                case RANK_SEA_WOLF: title = "Sea Wolf"; break;
                case RANK_SWASHBUCKLER: title = "Swashbuckler"; break;
                case RANK_CALICO_JACK: title = "Calico Jack"; break;
                case RANK_BLACK_BART: title = "Black Bart"; break;
                case RANK_BARBAROSSA: title = "Barbarossa"; break;
                case RANK_CAPTAIN_KIDD: title = "Captain Kidd"; break;
                default: title = "Unranked"; break;
            }

            return title;
        }

        public static List<ObjectivesDescription> ObjectivesDescriptionsForRank(uint rank)
        {
            uint key = 3 * rank + 1;
            List<ObjectivesDescription> descs = new List<ObjectivesDescription>()
            {
                new ObjectivesDescription(key),
                new ObjectivesDescription(key+1),
                new ObjectivesDescription(key+2)
            };
            return descs;
        }

        public static ObjectivesRank GetCurrentRankFromRanks(List<ObjectivesRank> ranks)
        {
            if (ranks == null || ranks.Count == 0)
                return null;

            ObjectivesRank currentRank = null;
    
            foreach (ObjectivesRank rank in ranks) {
                if (!rank.IsCompleted || rank.IsMaxRank) {
                    currentRank = rank;
                    break;
                }
            }
    
            if (currentRank == null)
                currentRank = ranks[ranks.Count-1];
    
            return currentRank;
        }

        public static ObjectivesRank GetRankFromRanks(uint rank, List<ObjectivesRank> ranks)
        {
            ObjectivesRank objRank = null;

            if (ranks != null && rank < ranks.Count)
                objRank = ranks[(int)rank];
            return objRank;
        }
        #endregion
    }
}
