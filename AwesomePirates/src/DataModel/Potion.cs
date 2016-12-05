using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using SparrowXNA;

namespace AwesomePirates
{
    class Potion
    {
        private const int kTwoPotionsRank = 10;
        private const int kNumPotions = 7;
        private static readonly uint[] kPotionUnlockRanks = new uint[] { 2, 5, 8, 12, 15, 17, 20 };

        // Valerie Potions
        public const uint POTION_POTENCY = (1 << 0);
        public const uint POTION_LONGEVITY = (1 << 1);
        public const uint POTION_RESURGENCE = (1 << 2);
        public const uint POTION_NOTORIETY = (1 << 3);
        public const uint POTION_BLOODLUST = (1 << 4);
        public const uint POTION_MOBILITY = (1 << 5);
        public const uint POTION_RICOCHET = (1 << 6);

        private static uint s_activationIndex = 0;

        public Potion(uint key)
        {
            mActive = false;
            mActivationIndex = 0;
            mKey = key;
            mRank = 1;
        }

        public static Potion PotencyPotion() { return new Potion(POTION_POTENCY); }
        public static Potion LongevityPotion() { return new Potion(POTION_LONGEVITY); }
        public static Potion ResurgencePotion() { return new Potion(POTION_RESURGENCE); }
        public static Potion NotorietyPotion() { return new Potion(POTION_NOTORIETY); }
        public static Potion BloodlustPotion() { return new Potion(POTION_BLOODLUST); }
        public static Potion MobilityPotion() { return new Potion(POTION_MOBILITY); }
        public static Potion RicochetPotion() { return new Potion(POTION_RICOCHET); }
        public static Potion PotionWithPotion(Potion potion)
        {
            Potion newPotion = null;

            if (potion != null) {
		        newPotion = new Potion(potion.Key);
		        newPotion.Rank = potion.Rank;
	        }
	        return newPotion;
        }

        #region Fields
        private bool mActive;
        private uint mActivationIndex;
        private uint mKey;
        private int mRank;
        #endregion

        #region Properties
        public bool IsActive
        {
            get { return mActive; }
            set
            {
                if (value)
                    mActivationIndex = ++s_activationIndex;
                mActive = value;
            }
        }
        public uint Key { get { return mKey; } set { mKey = value; } }
        public int Rank { get { return mRank; } set { mRank = Math.Min(Potion.MaxRankForKey(mKey), value); } }
        public uint ActivationIndex { get { return mActivationIndex; } }
        public int NextRank { get { return Math.Min(Potion.MaxRankForKey(mKey), mRank + 1); } }
        public bool IsMaxRank { get { return (mRank == Potion.MaxRankForKey(mKey)); } }
        public int SortOrder { get { return Potion.SortOrderForKey(mKey); } }
        public Color Color { get { return Potion.ColorForKey(mKey); } }
        public string KeyAsString { get { return mKey.ToString(); } }
        public string Name { get { return Potion.NameForKey(mKey); } }

        public static int NumPotions { get { return kNumPotions; } }
        public static uint MinPotionRank { get { return kPotionUnlockRanks[0]; } }
        public static int RequiredRankForTwoPotions { get { return kTwoPotionsRank; } }
        public static List<uint> PotionKeys
        {
            get { return new List<uint>() { POTION_POTENCY, POTION_LONGEVITY, POTION_RESURGENCE, POTION_NOTORIETY, POTION_BLOODLUST, POTION_MOBILITY, POTION_RICOCHET }; }
        }
        public static List<Potion> NewPotionList
        {
            get
            {
                return new List<Potion>()
                {   
                    Potion.PotencyPotion(),
                    Potion.LongevityPotion(),
                    Potion.ResurgencePotion(),
                    Potion.NotorietyPotion(),
                    Potion.BloodlustPotion(),
                    Potion.MobilityPotion(),
                    Potion.RicochetPotion()
                };
            }
        }
        public static Dictionary<uint, Potion> NewPotionDictionary
        {
            get
            {
                return new Dictionary<uint, Potion>()
                {
                    { POTION_POTENCY, Potion.PotencyPotion() },
                    { POTION_LONGEVITY, Potion.LongevityPotion() },
                    { POTION_RESURGENCE, Potion.ResurgencePotion() },
                    { POTION_NOTORIETY, Potion.NotorietyPotion() },
                    { POTION_BLOODLUST, Potion.BloodlustPotion() },
                    { POTION_MOBILITY, Potion.MobilityPotion() },
                    { POTION_RICOCHET, Potion.RicochetPotion() }
                };
            }
        }
        #endregion

        #region Methods
        public virtual Potion Clone()
        {
            return MemberwiseClone() as Potion;
        }

        public virtual void DecodeWithReader(BinaryReader reader)
        {
            mActive = reader.ReadBoolean();
            mActivationIndex = reader.ReadUInt32();
            mKey = reader.ReadUInt32();
            mRank = reader.ReadInt32();

            if (mActivationIndex >= s_activationIndex)
                s_activationIndex = mActivationIndex + 1;
        }

        public virtual void EncodeWithWriter(BinaryWriter writer)
        {
            writer.Write(mActive);
            writer.Write(mActivationIndex);
            writer.Write(mKey);
            writer.Write(mRank);
        }

        public static int CompareByActivationIndex(Potion a, Potion b)
        {
            if (a.ActivationIndex < b.ActivationIndex)
                return 1;
            else if (a.ActivationIndex > b.ActivationIndex)
                return -1;
            else
                return 0;
        }

        public static string NameForKey(uint key)
        {
            string name = null;

            switch (key)
            {
                case POTION_POTENCY: name = "Potency"; break;
                case POTION_LONGEVITY: name = "Longevity"; break;
                case POTION_RESURGENCE: name = "Resurgence"; break;
                case POTION_NOTORIETY: name = "Notoriety"; break;
                case POTION_BLOODLUST: name = "Bloodlust"; break;
                case POTION_MOBILITY: name = "Mobility"; break;
                case POTION_RICOCHET: name = "Ricochet"; break;
                default: break;
            }

            return name;
        }

        public static string PotionKeyAsString(uint key)
        {
            return key.ToString();
        }

        public static int MaxRankForKey(uint key)
        {
            return 1;
        }

        public static bool IsPotionUnlockedAtRank(int rank)
        {
            bool unlocked = false;
    
            for (int i = 0; i < Potion.NumPotions; ++i) {
                if (rank == kPotionUnlockRanks[i]) {
                    unlocked = true;
                    break;
                }
            }
    
            return unlocked;
        }

        public static uint UnlockedPotionKeyForRank(int rank)
        {
            uint key = 0;

            switch (rank)
            {
                case 2: key = POTION_POTENCY; break;
                case 5: key = POTION_LONGEVITY; break;
                case 8: key = POTION_RESURGENCE; break;
                case 12: key = POTION_NOTORIETY; break;
                case 15: key = POTION_BLOODLUST; break;
                case 17: key = POTION_MOBILITY; break;
                case 20: key = POTION_RICOCHET; break;
                default: key = 0; break;
            }

            return key;
        }

        public static List<uint> PotionKeysForRank(uint rank)
        {
            List<uint> allKeys = Potion.PotionKeys;
            List<uint> rankKeys = new List<uint>(Potion.NumPotions);
    
            for (int i = 0; i < Potion.NumPotions; ++i) {
                if (rank >= kPotionUnlockRanks[i]) {
                    if (i < allKeys.Count)
                        rankKeys.Add(allKeys[i]);
                }
            }
    
            return rankKeys;
        }

        public static int SortOrderForKey(uint key)
        {
            int sortOrder = 0;

            switch (key)
            {
                case POTION_POTENCY: sortOrder = 0; break;
                case POTION_LONGEVITY: sortOrder = 1; break;
                case POTION_RESURGENCE: sortOrder = 2; break;
                case POTION_NOTORIETY: sortOrder = 3; break;
                case POTION_BLOODLUST: sortOrder = 4; break;
                case POTION_MOBILITY: sortOrder = 5; break;
                case POTION_RICOCHET: sortOrder = 6; break;
                default: break;
            }

            return sortOrder;
        }

        public static Potion PotionForKeyInArray(uint key, List<Potion> array)
        {
            Potion foundIt = null;
	
	        foreach (Potion potion in array) {
		        if (potion.Key == key) {
			        foundIt = potion;
			        break;
		        }
	        }
	
	        return foundIt;
        }

        public static int ActivePotionLimitForRank(uint rank)
        {
            int limit = 1;

            if (rank >= Potion.RequiredRankForTwoPotions)
                limit = 2;
            return limit;
        }

        public static uint RequiredRankForPotion(Potion potion)
        {
            uint rank = 0;

            switch (potion.Key)
            {
                case POTION_POTENCY: rank = kPotionUnlockRanks[0]; break;
                case POTION_LONGEVITY: rank = kPotionUnlockRanks[1]; break;
                case POTION_RESURGENCE: rank = kPotionUnlockRanks[2]; break;
                case POTION_NOTORIETY: rank = kPotionUnlockRanks[3]; break;
                case POTION_BLOODLUST: rank = kPotionUnlockRanks[4]; break;
                case POTION_MOBILITY: rank = kPotionUnlockRanks[5]; break;
                case POTION_RICOCHET: rank = kPotionUnlockRanks[6]; break;
                default: break;
            }

            return rank;
        }

        public static string RequiredRankStringForPotion(Potion potion)
        {
            return "[Requires rank " + Potion.RequiredRankForPotion(potion) + "]";
        }

        public static string DescForPotion(Potion potion)
        {
            string desc = null;
    
            switch (potion.Key) {
                case POTION_POTENCY: desc = "Increases the duration or quantity of spells and munitions."; break;
                case POTION_LONGEVITY: desc = "Increases the number of charges in pickups by 50%."; break;
                case POTION_RESURGENCE: desc = "Red crosses are removed by sinking 7 ships instead of 10."; break;
                case POTION_NOTORIETY: desc = "Increases score gained from all sources by 20%."; break;
                case POTION_BLOODLUST: desc = "Doubles the score gained from^shark attacks."; break;
                case POTION_MOBILITY: desc = "Getting shot only slows you for 1 second instead of 3."; break;
                case POTION_RICOCHET: desc = "Your ricochets receive an increasing score bonus for each hop."; break;
                default: break;
	        }
	
	        return desc;
        }

        public static Color ColorForKey(uint key)
        {
            uint color = 0xee2cee;

            switch (key)
            {
                case POTION_POTENCY: color = 0xee2cee; break;
                case POTION_LONGEVITY: color = 0x00ff00; break;
                case POTION_RESURGENCE: color = 0x126df5; break;
                case POTION_NOTORIETY: color = 0x00ffff; break;
                case POTION_BLOODLUST: color = 0xff100b; break;
                case POTION_MOBILITY: color = 0xdddddd; break;
                case POTION_RICOCHET: color = 0xffff00; break;
                default: break;
            }

            return SPUtils.ColorFromColor(color);
        }

        public static string SoundNameForKey(uint key)
        {
            return "PotionRankup";
        }

        public static List<Potion> SyncPotions(List<Potion> destPotions, List<Potion> srcPotions)
        {
            List<Potion> synced = new List<Potion>(destPotions.Count);

            foreach (Potion destPotion in destPotions)
            {
                foreach (Potion srcPotion in srcPotions)
                {
                    if (destPotion.Key == srcPotion.Key)
                    {
                        destPotion.Rank = srcPotion.Rank;
				        break;
                    }
		        }
                synced.Add(destPotion);
	        }
	
	        return synced;
        }

        public static float PotencyCountFactorForPotion(Potion potion)
        {
            return ((potion.IsActive) ? 1.5f : 1f);
        }

        public static float PotencyDurationFactorForPotion(Potion potion)
        {
            return ((potion.IsActive) ? 1.3f : 1.0f);
        }

        public static float LongevityBonusFactorForPotion(Potion potion)
        {
            return ((potion.IsActive) ? 1.5f : 1f);
        }

        public static float ResurgenceFactorForPotion(Potion potion)
        {
            return ((potion.IsActive) ? 1.43f : 1.0f);
        }

        public static float NotorietyFactorForPotion(Potion potion)
        {
            return ((potion.IsActive) ? 1.2f : 1.0f);
        }

        public static float BloodlustFactorForPotion(Potion potion)
        {
            return ((potion.IsActive) ? 2.0f : 1.0f);
        }

        public static float MobilityReductionDurationForPotion(Potion potion)
        {
            return ((potion.IsActive) ? 2.0f : 0.0f);
        }

        public static int RicochetBonusForPotion(Potion potion)
        {
            return ((potion.IsActive) ? 500 : 250);
        }
        #endregion
    }
}
