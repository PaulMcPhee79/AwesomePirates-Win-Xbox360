using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class GameStats : SPEventDispatcher
    {
        public const string CUST_EVENT_TYPE_SHIP_TYPE_CHANGED = "shipTypeChangedEvent";
        public const string CUST_EVENT_TYPE_CANNON_TYPE_CHANGED = "cannonTypeChangedEvent";
        public const string CUST_EVENT_TYPE_PLAYER_CHANGED = "playerChangedEvent";

        private const string kDataVersion = "Version_2.0"; // "Version_1.0";

        public GameStats(string alias)
        {
            mDataVersion = kDataVersion;
            mAlias = alias;
            ResetAllStats();
		    mShipName = "Man o' War";
		    mCannonName = "Perisher";
            mShipNames = new List<string>() { "Man o' War", "Speedboat" };
            mCannonNames = new List<string>() { "Perisher" };
            mHiScore = 0;
            mTrinkets = Idol.NewTrinketList;
            mGadgets = Idol.NewGadgetList;
            mPotions = Potion.NewPotionDictionary;

            mObjectives = new List<ObjectivesRank>(ObjectivesRank.NUM_OBJECTIVES_RANKS);
            for (int i = 0; i < ObjectivesRank.NUM_OBJECTIVES_RANKS; ++i)
                mObjectives.Add(new ObjectivesRank((uint)i));

            mMasteries = new MasteryModel();
            CCMastery.PopulateModel(mMasteries);
        }

        #region Fields
        private string mDataVersion;     // To maintain data integrity between versions when restoring data from cloud and from disk.
        private string mAlias;           // The player's local alias/name (not GC)
        private string mShipName;
        private string mCannonName;
        private List<string> mShipNames;
        private List<string> mCannonNames;

        private List<Idol> mTrinkets;
        private List<Idol> mGadgets;

        private int mHiScore;
        private uint[] mAchievementBitmap = new uint[4]; // Overall achievements bitmap
        List<ObjectivesRank> mObjectives; // Overall objectives list

        private uint mCannonballsShot;
        private uint mCannonballsHit;
        private uint[] mRicochets = new uint[5];
        private uint mMerchantShipsSunk;
        private uint mPirateShipsSunk;
        private uint mNavyShipsSunk;
        private uint mEscortShipsSunk;
        private uint mSilverTrainsSunk;
        private uint mTreasureFleetsSunk;
        private uint mPlankings;
        private uint mHostages;
        private uint mSharkAttacks;
        private float mDaysAtSea;

        private uint mPowderKegSinkings;
        private uint mWhirlpoolSinkings;
        private uint mDamascusSinkings;
        private uint mBrandySlickSinkings;
        private uint mDavySinkings;
	
        // OpenFeint active challenge
        //CCOFChallenge *mOFChallenge;
    
        // GameCenter offline score
        //NSMutableArray *mOfflineScores;
    
        private Dictionary<uint, Potion> mPotions;
        private MasteryModel mMasteries;

        // v2.0 additions
        private uint mAcidPlankings;
        #endregion

        #region Properties
        public static string DefaultAlias { get { return "Unknown Swabby"; } }
        public string Alias { get { return mAlias; } set { mAlias = value; } }
        public string ShipName
        {
            get { return mShipName; }
            set
            {
                if (value == null || mShipName.Equals(value) || !mShipNames.Contains(value))
                    return;
                mShipName = value;
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SHIP_TYPE_CHANGED));
            }
        }
        public string CannonName { get { return mCannonName; } set { mCannonName = value; } }
        public int HiScore { get { return mHiScore; } set { if (value > mHiScore) mHiScore = value; } }
        public List<Idol> Trinkets { get { return new List<Idol>(mTrinkets); } }
        public List<Idol> Gadgets { get { return new List<Idol>(mGadgets); } }
        public Dictionary<uint, Potion> Potions { get { return mPotions; } }
        public uint Abilities { get { return TrinketAbilities | GadgetAbilities; } }
        public uint TrinketAbilities
        {
            get
            {
                uint abilities = 0;
	
	            foreach (Idol trinket in mTrinkets)
		            abilities |= trinket.Key;
	            return abilities;
            }
        }
        public uint GadgetAbilities
        {
            get
            {
                uint abilities = 0;

                foreach (Idol gadget in mGadgets)
		            abilities |= gadget.Key;
	            return abilities;
            }
        }
        public List<ObjectivesRank> Objectives { get { return mObjectives; } }
        public MasteryModel Masteries { get { return mMasteries; } }
        public uint CannonballsShot { get { return mCannonballsShot; } set { mCannonballsShot = value; } }
        public uint CannonballsHit { get { return mCannonballsHit; } set { mCannonballsHit = value; } }
        public float CannonballAccuracy
        {
            get
            {
                float accuracy = 0;

                if (mCannonballsShot != 0)
                    accuracy = mCannonballsHit / (float)mCannonballsShot;
                return accuracy;
            }
        }
        public uint MerchantShipsSunk { get { return mMerchantShipsSunk; } set { mMerchantShipsSunk = value; } }
        public uint PirateShipsSunk { get { return mPirateShipsSunk; } set { mPirateShipsSunk = value; } }
        public uint NavyShipsSunk { get { return mNavyShipsSunk; } set { mNavyShipsSunk = value; } }
        public uint EscortShipsSunk { get { return mEscortShipsSunk; } set { mEscortShipsSunk = value; } }
        public uint SilverTrainsSunk { get { return mSilverTrainsSunk; } set { mSilverTrainsSunk = value; } }
        public uint TreasureFleetsSunk { get { return mTreasureFleetsSunk; } set { mTreasureFleetsSunk = value; } }
        public uint Plankings { get { return mPlankings; } set { mPlankings = value; } }
        public uint Hostages { get { return mHostages; } set { mHostages = value; } }
        public uint SharkAttacks { get { return mSharkAttacks; } set { mSharkAttacks = value; } }
        public uint AcidPlankings { get { return mAcidPlankings; } set { mAcidPlankings = value; } }
        public float DaysAtSea { get { return mDaysAtSea; } set { mDaysAtSea = value; } }
        public int NumAchievementsCompleted
        {
            get
            {
                int count = 0;

                for (int i = 0, bitmapIndex = 0, bitMask = 0; i < AchievementManager.ACHIEVEMENT_COUNT; ++i)
                {
                    if ((mAchievementBitmap[bitmapIndex] & (1 << bitMask)) != 0)
                        ++count;

                    if (++bitMask == 30)
                    {
                        bitMask = 0;
                        ++bitmapIndex;
                    }
                }
                return count;
            }
        }
        public static int NumProfileStats { get { return 17; } }
        #endregion

        #region Methods
        public static uint AchievementBitForIndex(int index)
        {
            if (index < 30)
                return (uint)(1 << index);
            else
                return (uint)((1 << 30) | (1 << (index - 30)));
        }

        private int IdolIndexInArray(List<Idol> array, uint key)
        {
            int index = 0;
	
	        foreach (Idol idol in array)
            {
		        if (idol.Key == key)
			        return index;
		        ++index;
	        }
	        return -1;
        }

        public void AddRicochets(uint count, uint hops)
        {
            if (hops >= 1 && hops <= 5)
                mRicochets[hops - 1] += count;
        }

        public uint NumRicochetsForHops(uint hops)
        {
            uint count = 0;

            if (hops >= 1 && hops <= 5)
                count = mRicochets[hops - 1];
            return count;
        }

        public Idol IdolForKey(uint key)
        {
            Idol idol = null;
	
	        foreach (Idol trinket in mTrinkets)
            {
		        if (trinket.Key == key)
                {
			        idol = trinket;
			        break;
		        }
	        }
	
	        if (idol == null) {
		        foreach (Idol gadget in mGadgets)
                {
			        if (gadget.Key == key)
                    {
				        idol = gadget;
				        break;
			        }
		        }
	        }
	
	        return idol;
        }

        private Idol IdolAtSlot(int slot, List<Idol> idols)
        {
            Idol idol = null;

            if (slot < idols.Count)
                idol = idols[slot];
            return idol;
        }

        private void SetIdolAtSlot(uint idol, int slot, List<Idol> idols)
        {
            Idol idolObj = IdolForKey(idol);

            if (slot < idols.Count && idolObj != null)
            {
                int index = IdolIndexInArray(idols, idol);

                if (index != slot)
                {
                    idols.Insert(slot, idolObj);
                    idols.RemoveAt(slot + 1);
                }
            }
        }

        public Idol TrinketAtSlot(int slot)
        {
            return IdolAtSlot(slot, mTrinkets);
        }

        public void SetTrinketAtSlot(uint trinket, int slot)
        {
            SetIdolAtSlot(trinket, slot, mTrinkets);
        }

        public Idol GadgetAtSlot(int slot)
        {
            return IdolAtSlot(slot, mGadgets);
        }

        public void SetGadgetAtSlot(uint gadget, int slot)
        {
            SetIdolAtSlot(gadget, slot, mGadgets);
        }

        private void AddIdol(uint idol, List<Idol> idols)
        {
            Idol idolObj = IdolForKey(idol);
	
	        if (idolObj != null)
		        ++idolObj.Rank;
	        else
                idols.Add(new Idol(idol));
        }

        private void RemoveIdol(uint idol, List<Idol> idols)
        {
            int index = IdolIndexInArray(idols, idol);
	
	        if (index != -1)
                idols.RemoveAt(index);
        }

        public void AddTrinket(uint trinket)
        {
            AddIdol(trinket, mTrinkets);
        }

        public void RemoveTrinket(uint trinket)
        {
            RemoveIdol(trinket, mTrinkets);
        }

        public bool ContainsTrinket(uint trinket)
        {
            return (IdolForKey(trinket) != null);
        }

        public void AddGadget(uint gadget)
        {
            AddIdol(gadget, mGadgets);
        }

        public void RemoveGadget(uint gadget)
        {
            RemoveIdol(gadget, mGadgets);
        }

        public bool ContainsGadget(uint gadget)
        {
            return (IdolForKey(gadget) != null);
        }

        public Potion PotionForKey(uint key)
        {
            Potion potion;
            mPotions.TryGetValue(key, out potion);
            return potion;
        }

        public static List<Potion> ActivatedPotionsFromPotions(Dictionary<uint, Potion> potions)
        {
            if (potions == null || potions.Count == 0)
                return new List<Potion>();
    
            List<Potion> potionArray = new List<Potion>(potions.Count);
    
            foreach (KeyValuePair<uint, Potion> kvp in potions)
            {
                if (kvp.Value.IsActive)
                    potionArray.Add(kvp.Value);
            }

            potionArray.Sort(Potion.CompareByActivationIndex);
            return potionArray;
        }

        public void ActivatePotion(bool activate, uint key)
        {
            Potion potion = PotionForKey(key);
            potion.IsActive = activate;
    
            if (activate)
                EnforcePotionConstraints();
        }

        public void EnforcePotionConstraints()
        {
            ObjectivesRank objRank = ObjectivesRank.GetCurrentRankFromRanks(mObjectives);
            uint rank = (objRank != null) ? objRank.Rank : 0;
            List<Potion> potionArray = new List<Potion>(mPotions.Count);
    
            // Gather active potions and deactivate potions that shouldn't be active due to rank restrictions.
            foreach (KeyValuePair<uint, Potion> kvp in mPotions)
            {
                Potion potion = kvp.Value;

                if (potion.IsActive)
                {
                    if (Potion.RequiredRankForPotion(potion) > rank)
                        potion.IsActive = false;
                    else
                        potionArray.Add(potion);
                }
            }
    
            // Deactivate potions that exceed the limit of permitted active potions.
            int i = 0, limit = Potion.ActivePotionLimitForRank(rank);
            potionArray.Sort(Potion.CompareByActivationIndex);
    
            foreach (Potion potion in potionArray)
            {
                if (i >= limit)
                    potion.IsActive = false;
                ++i;
            }
        }

        public void EnforcePotionRequirements()
        {
            List<Potion> activatedPotions = GameStats.ActivatedPotionsFromPotions(Potions);
            ObjectivesRank objRank =  ObjectivesRank.GetCurrentRankFromRanks(mObjectives);
            uint rank = (objRank != null) ? objRank.Rank : 0;
            int limit = Potion.ActivePotionLimitForRank(rank);
            int activeCount = activatedPotions.Count;
    
            // Activate potions up to the expected level for this rank
            if (activeCount < limit) 
            {
                Potion potion;
                List<uint> unlockedPotionKeys = Potion.PotionKeysForRank(rank);
        
                foreach (uint key in unlockedPotionKeys)
                {
                    if (activeCount >= limit)
                        break;
                    
                    Potions.TryGetValue(key, out potion);
            
                    if (potion != null && !potion.IsActive)
                    {
                        potion.IsActive = true;
                        ++activeCount;
                    }
                }
            }
        }

        public uint GetAchievementBit(uint key)
        {
            uint index = key >> 30;
            return mAchievementBitmap[index] & (key & 0x3fffffff);
        }

        public void SetAchievementBit(uint key)
        {
            uint index = key >> 30;
            mAchievementBitmap[index] |= (key & 0x3fffffff);
        }

        public uint EarnedAchievementPoints(List<object> achievementDefs)
        {
            return AchievementPoints(achievementDefs, true);
        }

        public uint TotalAchievementPoints(List<object> achievementDefs)
        {
            return AchievementPoints(achievementDefs, false);
        }

        private uint AchievementPoints(List<object> achievementDefs, bool completed)
        {
            uint points = 0;
	
	        for (int i = 0, bitmapIndex = 0, bitMask = 0; i < AchievementManager.ACHIEVEMENT_COUNT; ++i)
            {
		        if (!completed || (mAchievementBitmap[bitmapIndex] & (1<<bitMask)) != 0)
                {
			        if (achievementDefs.Count > i)
                    {
                        Dictionary<string, object> dict = achievementDefs[i] as Dictionary<string, object>;
				
				        if (dict != null)
                            points += Convert.ToUInt32(dict["points"]);
			        }
		        }

		        if (++bitMask == 30)
                {
			        bitMask = 0;
			        ++bitmapIndex;
		        }
	        }
	        return points;
        }

        public void PrepareForNewGame()
        {
            // Do nothing
        }

        public void ResetObjectives()
        {
            mObjectives = new List<ObjectivesRank>(ObjectivesRank.NUM_OBJECTIVES_RANKS);

            for (int i = 0; i < ObjectivesRank.NUM_OBJECTIVES_RANKS; ++i)
                mObjectives.Add(new ObjectivesRank((uint)i));
        }

        public void ResetAchievements()
        {
            for (int i = 0; i < mAchievementBitmap.Length; ++i)
                mAchievementBitmap[i] = 0;

            mTreasureFleetsSunk = 0;
            mPlankings = 0;
            mPowderKegSinkings = 0;
            mWhirlpoolSinkings = 0;
            mDamascusSinkings = 0;
            mBrandySlickSinkings = 0;
            mDavySinkings = 0;
            mAcidPlankings = 0;
        }

        public void ResetAllStats()
        {
            ShipName = null;
	        CannonName = null;
            mHiScore = 0;
            mTrinkets = null;
            mGadgets = null;
	
	        mCannonballsShot = 0;
	        mCannonballsHit = 0;
	        mMerchantShipsSunk = 0;
	        mPirateShipsSunk = 0;
	        mNavyShipsSunk = 0;
	        mEscortShipsSunk = 0;
	        mSilverTrainsSunk = 0;
	        mTreasureFleetsSunk = 0;
	        mPlankings = 0;
            mHostages = 0;
            mSharkAttacks = 0;
            mAcidPlankings = 0;
            mDaysAtSea = 0;
    
            mPowderKegSinkings = 0;
            mWhirlpoolSinkings = 0;
            mDamascusSinkings = 0;
            mBrandySlickSinkings = 0;
            mDavySinkings = 0;

            for (int i = 0; i < mAchievementBitmap.Length; ++i)
                mAchievementBitmap[i] = 0;

            for (int i = 0; i < mRicochets.Length; ++i)
                mRicochets[i] = 0;
        }

        public void ShipSunkWithDeathBitmap(uint deathBitmap)
        {
            switch (deathBitmap)
            {
                case DeathBitmaps.POWDER_KEG: ++mPowderKegSinkings; break;
                case DeathBitmaps.WHIRLPOOL: ++mWhirlpoolSinkings; break;
                case DeathBitmaps.DAMASCUS: ++mDamascusSinkings; break;
                case DeathBitmaps.BRANDY_SLICK: ++mBrandySlickSinkings; break;
                case DeathBitmaps.HAND_OF_DAVY: ++mDavySinkings; break;
                default: break;
            }
        }

        public double PercentComplete(uint achievementBit)
        {
            double percentComplete = 0;

#if !GAME_STATS_DEBUG
	        switch (achievementBit)
            {
		        case AchievementManager.ACHIEVEMENT_BIT_MASTER_PLANKER: percentComplete = Math.Min(1, mPlankings / 500.0); break;
                case AchievementManager.ACHIEVEMENT_BIT_ROBBIN_DA_HOOD: percentComplete = mTreasureFleetsSunk / 250.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_BOOM_SHAKALAKA: percentComplete = mPowderKegSinkings / 500.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_LIKE_A_RECORD_BABY: percentComplete = mWhirlpoolSinkings / 500.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_ROAD_TO_DAMASCUS: percentComplete = mDamascusSinkings / 250.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_WELL_DONE: percentComplete = mBrandySlickSinkings / 250.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_DAVY_JONES_LOCKER: percentComplete = mDavySinkings / 500.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_BETTER_CALL_SAUL: percentComplete = mAcidPlankings / 100.0; break;
		        default: percentComplete = (GetAchievementBit(achievementBit) != 0) ? 1 : 0; break;
	        }
#else
            switch (achievementBit)
            {
		        case AchievementManager.ACHIEVEMENT_BIT_MASTER_PLANKER: percentComplete = Math.Min(1, mPlankings / 5.0); break;
                case AchievementManager.ACHIEVEMENT_BIT_ROBBIN_DA_HOOD: percentComplete = mTreasureFleetsSunk / 2.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_BOOM_SHAKALAKA: percentComplete = mPowderKegSinkings / 5.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_LIKE_A_RECORD_BABY: percentComplete = mWhirlpoolSinkings / 10.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_ROAD_TO_DAMASCUS: percentComplete = mDamascusSinkings / 3.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_WELL_DONE: percentComplete = mBrandySlickSinkings / 3.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_DAVY_JONES_LOCKER: percentComplete = mDavySinkings / 5.0; break;
                case AchievementManager.ACHIEVEMENT_BIT_BETTER_CALL_SAUL: percentComplete = mAcidPlankings / 3.0; break;
                default: percentComplete = (GetAchievementBit(achievementBit) != 0) ? 1 : 0; break;
	        }
#endif
            return Math.Min(100.0, 100 * percentComplete);
        }

        public void UpdateAchievementPercentComplete(double percentComplete, uint achievementBit, int achievementIndex)
        {
            bool earned = false;
	
#if !GAME_STATS_DEBUG
	        switch (achievementBit)
            {
		        case AchievementManager.ACHIEVEMENT_BIT_MASTER_PLANKER: earned = (mPlankings >= 500); break;
                case AchievementManager.ACHIEVEMENT_BIT_ROBBIN_DA_HOOD: earned = (mTreasureFleetsSunk >= 250); break;
                case AchievementManager.ACHIEVEMENT_BIT_BOOM_SHAKALAKA: earned = (mPowderKegSinkings >= 500); break;
                case AchievementManager.ACHIEVEMENT_BIT_LIKE_A_RECORD_BABY: earned = (mWhirlpoolSinkings >= 500); break;
                case AchievementManager.ACHIEVEMENT_BIT_ROAD_TO_DAMASCUS: earned = (mDamascusSinkings >= 250); break;
                case AchievementManager.ACHIEVEMENT_BIT_WELL_DONE: earned = (mBrandySlickSinkings >= 250); break;
                case AchievementManager.ACHIEVEMENT_BIT_DAVY_JONES_LOCKER: earned = (mDavySinkings >= 500); break;
                case AchievementManager.ACHIEVEMENT_BIT_BETTER_CALL_SAUL: earned = (mAcidPlankings >= 100); break;
		        default: earned = !SPMacros.SP_IS_DOUBLE_EQUAL(0,(float)percentComplete); break;
	        }
#else
            switch (achievementBit)
            {
		        case ACHIEVEMENT_BIT_MASTER_PLANKER: earned = (mPlankings >= 5); break;
                case ACHIEVEMENT_BIT_ROBBIN_DA_HOOD: earned = (mTreasureFleetsSunk >= 2); break;
                case ACHIEVEMENT_BIT_BOOM_SHAKALAKA: earned = (mPowderKegSinkings >= 5); break;
                case ACHIEVEMENT_BIT_LIKE_A_RECORD_BABY: earned = (mWhirlpoolSinkings >= 10); break;
                case ACHIEVEMENT_BIT_ROAD_TO_DAMASCUS: earned = (mDamascusSinkings >= 3); break;
                case ACHIEVEMENT_BIT_WELL_DONE: earned = (mBrandySlickSinkings >= 3); break;
                case ACHIEVEMENT_BIT_DAVY_JONES_LOCKER: earned = (mDavySinkings >= 5); break;
                case AchievementManager.ACHIEVEMENT_BIT_BETTER_CALL_SAUL: earned = (mAcidPlankings >= 3); break;
		        default: earned = !SP_IS_FLOAT_EQUAL(0,percentComplete); break;
	        }
#endif

            if (!earned)
		        earned = SPMacros.SP_IS_DOUBLE_EQUAL(PercentComplete(achievementBit), 100.0);
	
	        if (earned && GetAchievementBit(achievementBit) == 0)
            {
                SetAchievementBit(achievementBit);
                DispatchEvent(new AchievementEarnedEvent(achievementBit, achievementIndex));
	        }
        }

        public virtual GameStats Clone()
        {
            GameStats clone = MemberwiseClone() as GameStats;
            clone.mDataVersion = mDataVersion;

            clone.mShipNames = new List<string>(mShipNames.Count);
            foreach (string shipName in mShipNames)
                clone.mShipNames.Add(shipName);

            clone.mCannonNames = new List<string>(mCannonNames.Count);
            foreach (string cannonName in mCannonNames)
                clone.mCannonNames.Add(cannonName);

            clone.mTrinkets = new List<Idol>(mTrinkets.Count);
            foreach (Idol trinket in mTrinkets)
                clone.mTrinkets.Add(trinket.Clone());

            clone.mGadgets = new List<Idol>(mGadgets.Count);
            foreach (Idol gadget in mGadgets)
                clone.mGadgets.Add(gadget.Clone());

            clone.mAchievementBitmap = new uint[4];
            for (int i = 0; i < mAchievementBitmap.Length; ++i)
                clone.mAchievementBitmap[i] = mAchievementBitmap[i];

            clone.mObjectives = new List<ObjectivesRank>(mObjectives.Count);
            foreach (ObjectivesRank objRank in mObjectives)
                clone.mObjectives.Add(objRank.Clone());

            clone.mRicochets = new uint[5];
            for (int i = 0; i < mRicochets.Length; ++i)
                clone.mRicochets[i] = mRicochets[i];

            clone.mPotions = new Dictionary<uint, Potion>(mPotions.Count);
            foreach (KeyValuePair<uint, Potion> kvp in mPotions)
                clone.mPotions.Add(kvp.Key, kvp.Value);

            clone.mMasteries = mMasteries.Clone();

            return clone;
        }

        // Async access.
        private bool IsSupportedFileVersion(string version, float minVersion)
        {
            bool isSupported = false;
            try
            {
                if (version != null)
                {
                    int versionIndex = version.LastIndexOf('_') + 1;
                    if (versionIndex < version.Length)
                    {
                        float versionNumber;
                        string versionNumberString = version.Substring(versionIndex);
                        if (float.TryParse(versionNumberString, out versionNumber))
                        {
                            // Added in Version 1.1
                            if (versionNumber > minVersion || SPMacros.SP_IS_FLOAT_EQUAL(versionNumber, minVersion))
                                isSupported = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                isSupported = false;
            }

            return isSupported;
        }

        public void DecodeWithReader(BinaryReader reader)
        {
            int i, count;

            // Decrypt buffer
            count = reader.ReadInt32();

            if (count > 50000)
                throw new Exception("Saved game data length is invalid. Loading aborted.");

            byte[] buffer = new byte[count];
            int bufferLen = reader.Read(buffer, 0, count);

            if (bufferLen != count)
                throw new Exception("Saved game file could not be loaded due to file length inaccuracies.");
            FileManager.MaskUnmaskBuffer(0x10, buffer, bufferLen);

            BinaryReader br = new BinaryReader(new MemoryStream(buffer));

            // Read Saved Data
            mDataVersion = br.ReadString();
            mAlias = br.ReadString();
            mShipName = br.ReadString();
            mCannonName = br.ReadString();

            // Ship Names
            count = br.ReadInt32();

            if (count > 0)
                mShipNames = new List<string>(count);

            for (i = 0; i < count; ++i)
                mShipNames.Add(br.ReadString());

            // Cannon Names
            count = br.ReadInt32();

            if (count > 0)
                mCannonNames = new List<string>(count);

            for (i = 0; i < count; ++i)
                mCannonNames.Add(br.ReadString());

            // Trinkets
            count = Math.Min(br.ReadInt32(), mTrinkets.Count);

            for (i = 0; i < count; ++i)
                mTrinkets[i].DecodeWithReader(br);

            // Gadgets
            count = Math.Min(br.ReadInt32(), mGadgets.Count);

            for (i = 0; i < count; ++i)
                mGadgets[i].DecodeWithReader(br);

            mHiScore = br.ReadInt32();

            // Achievements
            for (i = 0; i < 4; ++i)
                mAchievementBitmap[i] = br.ReadUInt32();

            // Objectives
            count = Math.Min(br.ReadInt32(), mObjectives.Count);

            for (i = 0; i < count; ++i)
                mObjectives[i].DecodeWithReader(br);

            mCannonballsShot = br.ReadUInt32();
            mCannonballsHit = br.ReadUInt32();

            // Ricochets
            for (i = 0; i < 5; ++i)
                mRicochets[i] = br.ReadUInt32();

            mMerchantShipsSunk = br.ReadUInt32();
            mPirateShipsSunk = br.ReadUInt32();
            mNavyShipsSunk = br.ReadUInt32();
            mEscortShipsSunk = br.ReadUInt32();
            mSilverTrainsSunk = br.ReadUInt32();
            mTreasureFleetsSunk = br.ReadUInt32();
            mPlankings = br.ReadUInt32();
            mHostages = br.ReadUInt32();
            mSharkAttacks = br.ReadUInt32();
            mDaysAtSea = br.ReadSingle();
            mPowderKegSinkings = br.ReadUInt32();
            mWhirlpoolSinkings = br.ReadUInt32();
            mDamascusSinkings = br.ReadUInt32();
            mBrandySlickSinkings = br.ReadUInt32();
            mDavySinkings = br.ReadUInt32();

            // Potions
            try
            {
                count = br.ReadInt32();

                for (i = 0; i < count; ++i)
                {
                    uint potionKey = br.ReadUInt32();
                    Potion potion = null;

                    if (mPotions.ContainsKey(potionKey))
                        potion = mPotions[potionKey];
                    else
                        potion = new Potion(potionKey); // Invalid potion key, but need to read it out of the stream anyway.

                    potion.DecodeWithReader(br);
                }
            }
            catch (Exception)
            {
                // Ignore - no potions to load.
            }

            // Masteries
            try
            {
                mMasteries.DecodeWithReader(br);
            }
            catch (Exception)
            {
                // Initialize empty MasteryModel if nothing to load.
                if (mMasteries.TreeCount == 0)
                    CCMastery.PopulateModel(mMasteries);
            }

            // v2.0 additions
            try
            {
                if (IsSupportedFileVersion(mDataVersion, 2.0f))
                    mAcidPlankings = br.ReadUInt32();
            }
            catch (Exception)
            {
                mAcidPlankings = 0;
            }


            buffer = null;
            br = null;

#if false
            foreach (ObjectivesRank objRank in mObjectives)
                objRank.ForceCompletion();

            for (i = 0; i < 7; ++i)
            {
                mMasteries.AddXP(25000000);
                mMasteries.AttemptLevelUp();
            }
#endif
        }

        public void EncodeWithWriter(BinaryWriter writer)
        {
            int i;
            BinaryWriter bw = new BinaryWriter(new MemoryStream(2500));

            bw.Write(kDataVersion);
            bw.Write(mAlias);
            bw.Write(mShipName);
            bw.Write(mCannonName);

            bw.Write(mShipNames.Count);
            foreach (string shipName in mShipNames)
                bw.Write(shipName);

            bw.Write(mCannonNames.Count);
            foreach (string cannonName in mCannonNames)
                bw.Write(cannonName);

            bw.Write(mTrinkets.Count);
            foreach (Idol trinket in mTrinkets)
                trinket.EncodeWithWriter(bw);

            bw.Write(mGadgets.Count);
            foreach (Idol gadget in mGadgets)
                gadget.EncodeWithWriter(bw);

            bw.Write(mHiScore);

            for (i = 0; i < mAchievementBitmap.Length; ++i)
                bw.Write(mAchievementBitmap[i]);

            bw.Write(mObjectives.Count);
            foreach (ObjectivesRank objRank in mObjectives)
                objRank.EncodeWithWriter(bw);

            bw.Write(mCannonballsShot);
            bw.Write(mCannonballsHit);

            for (i = 0; i < mRicochets.Length; ++i)
                bw.Write(mRicochets[i]);

            bw.Write(mMerchantShipsSunk);
            bw.Write(mPirateShipsSunk);
            bw.Write(mNavyShipsSunk);
            bw.Write(mEscortShipsSunk);
            bw.Write(mSilverTrainsSunk);
            bw.Write(mTreasureFleetsSunk);
            bw.Write(mPlankings);
            bw.Write(mHostages);
            bw.Write(mSharkAttacks);
            bw.Write(mDaysAtSea);
            bw.Write(mPowderKegSinkings);
            bw.Write(mWhirlpoolSinkings);
            bw.Write(mDamascusSinkings);
            bw.Write(mBrandySlickSinkings);
            bw.Write(mDavySinkings);

            bw.Write(mPotions.Count);
            foreach (KeyValuePair<uint, Potion> kvp in mPotions)
            {
                bw.Write(kvp.Key);
                kvp.Value.EncodeWithWriter(bw);
            }

            mMasteries.EncodeWithWriter(bw);

            // v2.0 additions
            bw.Write(mAcidPlankings);

            // Perform basic encryption on buffer
            Stream stream = bw.BaseStream;
            stream.Position = 0;

            byte[] buffer = new byte[(int)stream.Length];
            int bufferLen = stream.Read(buffer, 0, (int)stream.Length);
            FileManager.MaskUnmaskBuffer(0x10, buffer, bufferLen);

            // Write encrypted buffer back to stream
            writer.Write(bufferLen);
            writer.Write(buffer, 0, bufferLen);

            buffer = null;
            bw = null;
        }
        #endregion
    }
}
