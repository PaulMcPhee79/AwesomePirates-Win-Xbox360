using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class Ash
    {
        public const int ASH_KEY_COUNT = 6;

        // Non-procable
        public const uint ASH_DEFAULT = (1 << 0);
        public const uint ASH_DUTCHMAN_SHOT = (1 << 1);

        // Procable (pickups)
        public const uint ASH_NOXIOUS = (1 << 2);
        public const uint ASH_MOLTEN = (1 << 3);
        public const uint ASH_SAVAGE = (1 << 4);
        public const uint ASH_ABYSSAL = (1 << 5);

        public Ash(uint key, int rank = 1)
        {
            mKey = key;
            mRank = rank;
        }

        private uint mKey;
        private int mRank;

        public uint Key { get { return mKey; } set { mKey = value; } }
        public int Rank { get { return mRank; } set { mRank = Math.Min(Ash.MaxRankForKey(mKey), value); } }
        public int NextRank { get { return Math.Min(Ash.MaxRankForKey(mKey), mRank + 1); } }
        public bool IsMaxRank { get { return (mRank == Ash.MaxRankForKey(mKey)); } }
        public int SortOrder { get { return Ash.SortOrderForKey(mKey); } }
        public string KeyAsString { get { return mKey.ToString(); } }

        public static uint NumProcableAshes { get { return ASH_KEY_COUNT - 2; } }
        public static List<uint> AshKeys
        {
            get { return new List<uint>() { ASH_DEFAULT, ASH_NOXIOUS, ASH_MOLTEN, ASH_SAVAGE, ASH_ABYSSAL }; }
        }
        public static List<uint> ProcableAshKeys
        {
            get { return new List<uint>() { ASH_NOXIOUS, ASH_MOLTEN, ASH_SAVAGE, ASH_ABYSSAL }; }
        }
        public static List<Ash> NewAshList
        {
            get
            {
                return new List<Ash>()
                {
                    new Ash(ASH_DEFAULT),
                    new Ash(ASH_NOXIOUS),
                    new Ash(ASH_MOLTEN),
                    new Ash(ASH_SAVAGE),
                    new Ash(ASH_ABYSSAL)
                };
            }
        }
        public static List<Ash> NewProcableAshList
        {
            get
            {
                return new List<Ash>()
                {
                    new Ash(ASH_NOXIOUS),
                    new Ash(ASH_MOLTEN),
                    new Ash(ASH_SAVAGE),
                    new Ash(ASH_ABYSSAL)
                };
            }
        }
        public static List<string> AllTexturePrefixes
        {
            get { return new List<string>() { "single-shot_", "dutchman-shot_", "venom-shot_", "magma-shot_", "crimson-shot_", "abyssal-shot_" }; }
        }

        public static int MaxRankForKey(uint key)
        {
            return 1;
        }

        public static AshProc AshProcForAsh(Ash ash)
        {
            AshProc ashProc = new AshProc();
    
            ashProc.Proc = ash.Key;
            ashProc.ChanceToProc = 0;
	        ashProc.SpecialChanceToProc = 0;
	        ashProc.SpecialProcEventKey = null;
            ashProc.ChargesRemaining = ashProc.TotalCharges = (uint)(Ash.TotalChargesForAsh(ash) *
                Potion.LongevityBonusFactorForPotion(GameController.GC.GameStats.PotionForKey(Potion.POTION_LONGEVITY)));
	        ashProc.RequirementCount = 0;
	        ashProc.RequirementCeiling = 0;
	        ashProc.Addition = 0;
	        ashProc.Multiplier = Ash.InfamyFactorForAsh(ash);
	        ashProc.RicochetAddition = 0;
	        ashProc.RicochetMultiplier = 1;
	        ashProc.DeactivatesOnMiss = false;
            ashProc.SoundName = null;
            ashProc.TexturePrefix = Ash.TexturePrefixForKey(ash.Key);
    
            return ashProc;
        }

        public static AshProc SKAshProcForAsh(Ash ash)
        {
            AshProc ashProc = new AshProc();

            ashProc.Proc = ash.Key;
            ashProc.ChanceToProc = 0;
            ashProc.SpecialChanceToProc = 0;
            ashProc.SpecialProcEventKey = null;
            ashProc.ChargesRemaining = ashProc.TotalCharges = (uint)((ash.Key == Ash.ASH_MOLTEN) ? 10 : 20);
            ashProc.RequirementCount = 0;
            ashProc.RequirementCeiling = 0;
            ashProc.Addition = 0;
            ashProc.Multiplier = Ash.InfamyFactorForAsh(ash);
            ashProc.RicochetAddition = 0;
            ashProc.RicochetMultiplier = 1;
            ashProc.DeactivatesOnMiss = false;
            ashProc.SoundName = null;
            ashProc.TexturePrefix = Ash.TexturePrefixForKey(ash.Key);

            return ashProc;
        }

        public static Ash AshForKeyInArray(uint key, List<Ash> array)
        {
            Ash foundIt = null;
	
	        foreach (Ash ash in array) {
		        if (ash.Key == key) {
			        foundIt = ash;
			        break;
		        }
	        }
	
	        return foundIt;
        }

        public static uint TotalChargesForAsh(Ash ash)
        {
            uint totalCharges = 20;

            if (ash.Key == ASH_DUTCHMAN_SHOT)
                throw new ArgumentException("Don't use ASH_DUTCHMAN_SHOT directly.");

            if (ash.Key == ASH_NOXIOUS && (GameController.GC.MasteryManager.MasteryBitmap & CCMastery.CANNON_NOXIOUS_STRAITS) != 0)
                totalCharges += 10;
                
            return totalCharges;
        }

        public static int SortOrderForKey(uint key)
        {
            int sortOrder = 0;

            switch (key)
            {
                case ASH_DEFAULT: sortOrder = 0; break;
                case ASH_NOXIOUS: sortOrder = 1; break;
                case ASH_MOLTEN: sortOrder = 2; break;
                case ASH_SAVAGE: sortOrder = 3; break;
                case ASH_ABYSSAL: sortOrder = 4; break;
                default: break;
            }

            return sortOrder;
        }

        public static string HintForKey(uint key)
        {
            string hint = null;

            switch (key)
            {
                case ASH_NOXIOUS:
                    hint = "Venom Shot";
                    break;
                case ASH_MOLTEN:
                    hint = "Molten Shot";
                    break;
                case ASH_SAVAGE:
                    hint = "Crimson Shot";
                    break;
                case ASH_ABYSSAL:
                    hint = "Abyssal Shot";
                    break;
                case ASH_DUTCHMAN_SHOT:
                case ASH_DEFAULT:
                default:
                    hint = null;
                    break;
            }

            return hint;
        }

        public static string GameSettingForKey(uint key)
        {
            string settingKey = null;

            switch (key)
            {
                case ASH_NOXIOUS:
                    settingKey = GameSettings.PICKUP_VENOM_TIPS;
                    break;
                case ASH_MOLTEN:
                    settingKey = GameSettings.PICKUP_MOLTEN_TIPS;
                    break;
                case ASH_SAVAGE:
                    settingKey = GameSettings.PICKUP_CRIMSON_TIPS;
                    break;
                case ASH_ABYSSAL:
                    settingKey = GameSettings.PICKUP_ABYSSAL_TIPS;
                    break;
                case ASH_DUTCHMAN_SHOT:
                case ASH_DEFAULT:
                default:
                    settingKey = null;
                    break;
            }

            return settingKey;
        }

        public static string SoundNameForKey(uint key)
        {
            string texturePrefix = null;

            switch (key)
            {
                case ASH_NOXIOUS:
                    texturePrefix = "AshNoxious";
                    break;
                case ASH_MOLTEN:
                    texturePrefix = "AshMolten";
                    break;
                case ASH_SAVAGE:
                    texturePrefix = "AshSavage";
                    break;
                case ASH_ABYSSAL:
                    texturePrefix = "AshAbyssal";
                    break;
                case ASH_DUTCHMAN_SHOT:
                case ASH_DEFAULT:
                default:
                    texturePrefix = null;
                    break;
            }

            return texturePrefix;
        }

        public static string TexturePrefixForKey(uint key)
        {
            string texturePrefix = null;

            switch (key)
            {
                case ASH_DUTCHMAN_SHOT:
                    texturePrefix = "dutchman-shot_";
                    break;
                case ASH_NOXIOUS:
                    texturePrefix = "venom-shot_";
                    break;
                case ASH_MOLTEN:
                    texturePrefix = "magma-shot_";
                    break;
                case ASH_SAVAGE:
                    texturePrefix = "crimson-shot_";
                    break;
                case ASH_ABYSSAL:
                    texturePrefix = "abyssal-shot_";
                    break;
                case ASH_DEFAULT:
                default:
                    texturePrefix = "single-shot_";
                    break;
            }

            return texturePrefix;
        }

        public static uint InfamyFactorForAsh(Ash ash)
        {
            uint factor = 1;
            return factor;
        }
    }
}
