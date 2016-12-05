using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AwesomePirates
{
    class Idol
    {
        public const int IDOL_KEY_COUNT = 9;

        // 0-15 Gadgets
        public const uint GADGET_SPELL_BRANDY_SLICK = (1<<0);
        public const uint GADGET_SPELL_TNT_BARRELS = (1<<1);
        public const uint GADGET_SPELL_NET = (1<<2);
        public const uint GADGET_SPELL_CAMOUFLAGE = (1<<3);
        public const uint GADGET_MASK = (GADGET_SPELL_BRANDY_SLICK | GADGET_SPELL_TNT_BARRELS | GADGET_SPELL_NET | GADGET_SPELL_CAMOUFLAGE);

        // 16-31 Voodoo
        public const uint VOODOO_SPELL_WHIRLPOOL = (1<<16);
        public const uint VOODOO_SPELL_TEMPEST = (1<<17);
        public const uint VOODOO_SPELL_HAND_OF_DAVY = (1<<18);
        public const uint VOODOO_SPELL_FLYING_DUTCHMAN = (1<<19);
        public const uint VOODOO_SPELL_SEA_OF_LAVA = (1<<20);
        public const uint VOODOO_MASK = (VOODOO_SPELL_WHIRLPOOL | VOODOO_SPELL_TEMPEST | VOODOO_SPELL_HAND_OF_DAVY | VOODOO_SPELL_FLYING_DUTCHMAN | VOODOO_SPELL_SEA_OF_LAVA);

        public const float VOODOO_DESPAWN_DURATION = 3.0f;
        public const int kMaxIdolRank = 3;

        public Idol(uint key)
        {
            mKey = key;
            mRank = kMaxIdolRank; // Force it to max rank (now that Swindlers Alley is gone)
        }

        #region Fields
        private uint mKey;
        private int mRank;
        #endregion

        #region Properties
        public uint Key { get { return mKey; } set { mKey = value; } }
        public int Rank { get { return mRank; } set { mRank = Math.Min(kMaxIdolRank, value); } }
        public int NextRank { get { return Math.Min(kMaxIdolRank, mRank + 1); } }
        public bool IsMaxRank { get { return (mRank == kMaxIdolRank); } }
        public string KeyAsString { get { return mKey.ToString(); } }
        public static List<uint> VoodooKeys
        {
            get { return new List<uint>() { VOODOO_SPELL_WHIRLPOOL, VOODOO_SPELL_TEMPEST, VOODOO_SPELL_HAND_OF_DAVY, VOODOO_SPELL_FLYING_DUTCHMAN, VOODOO_SPELL_SEA_OF_LAVA }; }
        }
        public static List<uint> GadgetKeys
        {
            get { return new List<uint>() { GADGET_SPELL_BRANDY_SLICK, GADGET_SPELL_TNT_BARRELS, GADGET_SPELL_NET, GADGET_SPELL_CAMOUFLAGE }; }
        }
        public static List<uint> VoodooKeysLite
        {
            get { return new List<uint>() { VOODOO_SPELL_WHIRLPOOL, VOODOO_SPELL_TEMPEST }; }
        }
        public static List<uint> GadgetKeysLite
        {
            get { return new List<uint>() { GADGET_SPELL_TNT_BARRELS, GADGET_SPELL_NET }; }
        }
        public static List<uint> VoodooGadgetKeys
        {
            get
            {
                List<uint> keys = Idol.VoodooKeys;
                keys.AddRange(Idol.GadgetKeys);
                return keys;
            }
        }
        public static List<Idol> NewTrinketList
        {
            get
            {
                return new List<Idol>()
                {   
                    new Idol(VOODOO_SPELL_WHIRLPOOL),
                    new Idol(VOODOO_SPELL_TEMPEST),
                    new Idol(VOODOO_SPELL_HAND_OF_DAVY),
                    new Idol(VOODOO_SPELL_FLYING_DUTCHMAN),
                    new Idol(VOODOO_SPELL_SEA_OF_LAVA)
                };
            }
        }
        public static List<Idol> NewGadgetList
        {
            get
            {
                return new List<Idol>()
                {   
                    new Idol(GADGET_SPELL_BRANDY_SLICK),
                    new Idol(GADGET_SPELL_TNT_BARRELS),
                    new Idol(GADGET_SPELL_NET),
                    new Idol(GADGET_SPELL_CAMOUFLAGE)
                };
            }
        }

        #endregion

        #region Methods
        public virtual Idol Clone()
        {
            return MemberwiseClone() as Idol;
        }

        public virtual void DecodeWithReader(BinaryReader reader)
        {
            mKey = reader.ReadUInt32();
            mRank = reader.ReadInt32();
        }

        public virtual void EncodeWithWriter(BinaryWriter writer)
        {
            writer.Write(mKey);
            writer.Write(mRank);
        }

        public static bool IsMunition(uint key)
        {
            return ((key & GADGET_MASK) == key);
        }

        public static bool IsSpell(uint key)
        {
            return ((key & VOODOO_MASK) == key);
        }

        public static Idol IdolForKeyInArray(uint key, List<Idol> array)
        {
            Idol foundIt = null;
	
	        foreach (Idol idol in array)
            {
		        if (idol.Key == key)
                {
			        foundIt = idol;
			        break;
		        }
	        }
	
	        return foundIt;
        }

        public static List<Idol> SyncIdols(List<Idol> destIdols, List<Idol> srcIdols)
        {
            List<Idol> synced = new List<Idol>(destIdols.Count);

            foreach (Idol destIdol in destIdols)
            {
                foreach (Idol srcIdol in srcIdols)
                {
                    if (destIdol.Key == srcIdol.Key)
                    {
                        destIdol.Rank = srcIdol.Rank;
                        break;
                    }
                }
                synced.Add(destIdol);
            }

            return synced;
        }

        public static double DurationForIdol(Idol idol)
        {
            double duration = 0;
            bool potencyActive = GameController.GC.GameStats.PotionForKey(Potion.POTION_POTENCY).IsActive;
	
	        switch (idol.Key)
            {
		        case GADGET_SPELL_BRANDY_SLICK:
				        switch (idol.Rank)
                        {
					        case 1: duration = 20.0; break;
					        case 2: duration = 30.0; break;
					        case 3: duration = 40.0; break;
					        default: break;
				        }
            
                    if (potencyActive)
                        duration += 30.0;
			        break;
		        case GADGET_SPELL_TNT_BARRELS:
			        break;
		        case GADGET_SPELL_NET:
                    switch (idol.Rank)
                    {
                        case 1: duration = 20.0; break;
                        case 2: duration = 30.0; break;
                        case 3: duration = 40.0; break;
                        default: break;
                    }
            
                    if (potencyActive)
                        duration += 30.0;
			        break;
		        case GADGET_SPELL_CAMOUFLAGE:
			        switch (idol.Rank)
                    {
				        case 1: duration = 20.0; break;
				        case 2: duration = 30.0; break;
				        case 3: duration = 40.0; break;
				        default: break;
			        }
            
                    if (potencyActive)
                        duration += 30.0;
			        break;
		        case VOODOO_SPELL_WHIRLPOOL:
                    switch (idol.Rank)
                    {
				        case 1: duration = 12.0; break;
				        case 2: duration = 18.0; break;
				        case 3: duration = 24.0; break;
				        default: break;
			        }
            
                    if (potencyActive)
                        duration += 6.0;
			        break;
		        case VOODOO_SPELL_TEMPEST:
                    switch (idol.Rank)
                    {
				        case 1: duration = 14.0; break;
				        case 2: duration = 22.0; break;
				        case 3: duration = 30.0; break;
				        default: break;
			        }
            
                    if (potencyActive)
                        duration += 6.0;
			        break;
		        case VOODOO_SPELL_HAND_OF_DAVY:
                    switch (idol.Rank)
                    {
				        case 1: duration = 14.0; break;
				        case 2: duration = 22.0; break;
				        case 3: duration = 30.0; break;
				        default: break;
			        }
            
                    if (potencyActive)
                        duration += 10.0;
			        break;
		        case VOODOO_SPELL_FLYING_DUTCHMAN:
                    switch (idol.Rank)
                    {
				        case 1: duration = 12.0; break;
				        case 2: duration = 18.0; break;
				        case 3: duration = 24.0; break;
				        default: break;
			        }
            
                    if (potencyActive)
                        duration += 16.0;
			        break;
                case VOODOO_SPELL_SEA_OF_LAVA:
                    duration = 6.0;
                    break;
		        default:
			        break;
	        }

	        return duration;
        }

        public static float InfamyMultiplierForIdol(Idol idol)
        {
            return 1f;
        }

        public static int CountForIdol(Idol idol)
        {
            GameController gc = GameController.GC;
            int count = 0;
    
            if (idol == null)
                return count;
	
	        switch (idol.Key)
            {
		        case GADGET_SPELL_BRANDY_SLICK:
			        count = 1;
			        break;
		        case GADGET_SPELL_TNT_BARRELS:
                    count = (int)(12 * Potion.PotencyCountFactorForPotion(GameController.GC.GameStats.PotionForKey(Potion.POTION_POTENCY)));
			        break;
		        case GADGET_SPELL_NET:
			        count = 1;
			        break;
		        case GADGET_SPELL_CAMOUFLAGE:
			        count = 1;
			        break;
		        case VOODOO_SPELL_WHIRLPOOL:
			        count = 1;
			        break;
		        case VOODOO_SPELL_TEMPEST:
                    if ((gc.MasteryManager.MasteryBitmap & CCMastery.VOODOO_TWISTED_SISTERS) != 0)
                        count = 3;
                    else
                        count = 2;
			        break;
		        case VOODOO_SPELL_HAND_OF_DAVY:
                    if ((gc.MasteryManager.MasteryBitmap & CCMastery.VOODOO_DAVYS_FURY) != 0)
                        count = 3;
                    else
			            count = 2;
			        break;
		        case VOODOO_SPELL_FLYING_DUTCHMAN:
                    count = 0;
			        break;
                case VOODOO_SPELL_SEA_OF_LAVA:
                    count = 0;
			        break;
		        default:
			        break;
	        }
	
	        return count;
        }

        public static float ScaleForIdol(Idol idol)
        {
            return 1f;
        }

        public static string NameForIdol(Idol idol)
        {
            if (idol == null)
		        return null;
	
	        string desc = null;
	
	        switch (idol.Key)
            {
		        case GADGET_SPELL_BRANDY_SLICK: desc = "Brandy Slick"; break;
		        case GADGET_SPELL_TNT_BARRELS: desc = "Powder Kegs"; break;
		        case GADGET_SPELL_NET: desc = "Trawling Net"; break;
		        case GADGET_SPELL_CAMOUFLAGE: desc = "Navy Colors"; break;
		        case VOODOO_SPELL_WHIRLPOOL: desc = "Whirlpool"; break;
		        case VOODOO_SPELL_TEMPEST: desc = "Tornado Storm"; break;
		        case VOODOO_SPELL_HAND_OF_DAVY: desc = "Hand of Davy"; break;
		        case VOODOO_SPELL_FLYING_DUTCHMAN: desc = "Ghost Ship"; break;
                case VOODOO_SPELL_SEA_OF_LAVA: desc = "Sea of Lava"; break;
		        default: break;
	        }

	        return desc;
        }

        public static string DescForIdol(Idol idol)
        {
            if (idol == null)
                return null;

            string desc = null;

            switch (idol.Key)
            {
                case GADGET_SPELL_BRANDY_SLICK: desc = "Burns ships when ignited."; break;
                case GADGET_SPELL_TNT_BARRELS: desc = "Explosive barrels in the water."; break;
                case GADGET_SPELL_NET: desc = "Slows ships within its radius."; break;
                case GADGET_SPELL_CAMOUFLAGE: desc = "Stops enemies from firing at you."; break;
                case VOODOO_SPELL_WHIRLPOOL: desc = "Swallows ships in its vortex."; break;
                case VOODOO_SPELL_TEMPEST: desc = "Destroys ships in its path."; break;
                case VOODOO_SPELL_HAND_OF_DAVY: desc = "Drags ships underwater."; break;
                case VOODOO_SPELL_FLYING_DUTCHMAN: desc = "While under the effect of this spell, you can't be hit or get a red cross."; break;
                case VOODOO_SPELL_SEA_OF_LAVA: desc = "Turns the ocean to lava and spawns pools of magma."; break;
                default: break;
            }

            return desc;
        }

        public static string TextureNameForKey(uint key)
        {
            string textureName = null;
	
	        switch (key)
            {
		        case GADGET_SPELL_BRANDY_SLICK: textureName = "brandy-slick"; break;
		        case GADGET_SPELL_TNT_BARRELS: textureName = "powder-keg"; break;
		        case GADGET_SPELL_NET: textureName = "net"; break;
		        case GADGET_SPELL_CAMOUFLAGE: textureName = "navy"; break;
		        case VOODOO_SPELL_WHIRLPOOL: textureName = "whirlpool-idol"; break;
		        case VOODOO_SPELL_TEMPEST: textureName = "tempest-idol"; break;
		        case VOODOO_SPELL_HAND_OF_DAVY: textureName = "death-from-the-deep-idol"; break;
		        case VOODOO_SPELL_FLYING_DUTCHMAN: textureName = "flying-dutchman-idol"; break;
                case VOODOO_SPELL_SEA_OF_LAVA:
		        default: break;
	        }
	
	        return textureName;
        }

        public static string IconTextureNameForKey(uint key)
        {
            string textureName = null;

            switch (key)
            {
                case GADGET_SPELL_BRANDY_SLICK: textureName = "brandy-slick-icon"; break;
                case GADGET_SPELL_TNT_BARRELS: textureName = "powder-keg-icon"; break;
                case GADGET_SPELL_NET: textureName = "net-icon"; break;
                case GADGET_SPELL_CAMOUFLAGE: textureName = "camouflage-icon"; break;
                case VOODOO_SPELL_WHIRLPOOL: textureName = "whirlpool-icon"; break;
                case VOODOO_SPELL_TEMPEST: textureName = "tempest-icon"; break;
                case VOODOO_SPELL_HAND_OF_DAVY: textureName = "death-from-the-deep-icon"; break;
                case VOODOO_SPELL_FLYING_DUTCHMAN: textureName = "flying-dutchman-icon"; break;
                case VOODOO_SPELL_SEA_OF_LAVA: textureName = "sea-of-lava-icon"; break;
                default: break;
            }

            return textureName;
        }
        #endregion
    }
}
