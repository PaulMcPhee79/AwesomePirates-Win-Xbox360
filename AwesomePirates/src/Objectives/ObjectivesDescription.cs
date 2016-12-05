using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AwesomePirates
{
    class ObjectivesDescription
    {
        public const uint SHIP_TYPE_PLAYER_SHIP = 0x1;
        public const uint SHIP_TYPE_PIRATE_SHIP = 0x2;
        public const uint SHIP_TYPE_NAVY_SHIP = 0x4;
        public const uint SHIP_TYPE_MERCHANT_SHIP = 0x8;
        public const uint SHIP_TYPE_ESCORT_SHIP = 0x10;
        public const uint SHIP_TYPE_TREASURE_FLEET = 0x20;
        public const uint SHIP_TYPE_SILVER_TRAIN = 0x40;

        public ObjectivesDescription(uint key, int count = 0)
        {
            mKey = key;
            mCount = count;
            mFailed = false;
        }

        #region Fields
        private uint mKey;
        private int mCount;
        private bool mFailed;
        #endregion 

        #region Properties
        public int Count { get { return mCount; } set { mCount = Math.Min(value, Quota); } }
        public int Quota { get { return ObjectivesDescription.QuotaForKey(mKey); } }
        public uint Key { get { return mKey; } }
        public bool IsFailed
        {
            get { return mFailed; }
            set
            {
                // Don't set a completed objective to a failed state
                if (!value || !IsCompleted)
                    mFailed = value;
            }
        }
        public bool IsCompleted { get { return (mCount >= Quota); } }
        public String Description { get { return ObjectivesDescription.DescriptionTextForKey(mKey); } }
        public String LogbookDescription { get { return ObjectivesDescription.LogbookDescriptionTextForKey(mKey); } }
        #endregion

        #region Methods
        public virtual ObjectivesDescription Clone()
        {
            return MemberwiseClone() as ObjectivesDescription;
        }

        public virtual void DecodeWithReader(BinaryReader reader)
        {
            mKey = reader.ReadUInt32();
            mCount = reader.ReadInt32();
            mFailed = reader.ReadBoolean();

            mCount = Math.Min(mCount, ObjectivesDescription.QuotaForKey(mKey));
        }

        public virtual void EncodeWithWriter(BinaryWriter writer)
        {
            writer.Write(mKey);
            writer.Write(mCount);
            writer.Write(mFailed);
        }

        public void ForceCompletion()
        {
            Count = Quota;
            IsFailed = false;
        }

        public void Reset()
        {
            Count = 0;
            IsFailed = false;
        }

        public static String DescriptionTextForKey(uint key)
        {
            string desc = null;
            int value = ObjectivesDescription.ValueForKey(key), quota = ObjectivesDescription.QuotaForKey(key);

            switch (key)
            {
                // 0 Unranked
                case 1: desc = "Make a rival pirate walk the plank."; break;
                case 2: desc = "Sink " + quota + " ships with a Powder Keg deployment."; break;
                case 3: desc = "Survive until sunset on Day " + value + "."; break;
    
                // 1 Swabby
                case 4: desc = "Sink the Treasure Fleet."; break;
                case 5: desc = "Score a ricochet by shooting " + value + " ships with one cannonball."; break;
                case 6: desc = "Remove a red cross by sinking 10 ships."; break;
        
                // 2 Deckhand
                case 7: desc = "Sink " + quota + " navy ships."; break;
                case 8: desc = "Sink " + quota + " ships in a Whirlpool."; break;
                case 9: desc = "Score " + quota + " ricochets."; break;
        
                // 3 Jack Tar
                case 10: desc = "Shoot " + quota + " ships with a Molten Shot powerup."; break;
                case 11: desc = "Shoot " + quota + " ships without missing."; break;
                case 12: desc = "Survive until sunrise on Day " + value + "."; break;
        
                // 4 Old Salt
                case 13: desc = "Trap " + value + " ships in a Trawling Net."; break;
                case 14: desc = "Sink " + quota + " rival pirate ships."; break;
                case 15: desc = "Achieve a score of " + GuiHelper.CommaSeparatedValue(value) + " without getting any ricochets."; break;

                // 5 Helmsman
                case 16: desc = "Sink the Silver Train twice."; break;
                case 17: desc = "Survive until sunrise on Day " + value + " without getting a red cross."; break;
                case 18: desc = "Shoot " + value + " ships with one cannon-^ball."; break;
        
                // 6 Sea Dog
                case 19: desc = "Make " + quota + " rival pirates walk the plank."; break;
                case 20: desc = "Sink " + quota + " ships with the Hand of Davy."; break;
                case 21: desc = "Survive until midnight on Day " + value + "."; break;
        
                // 7 Villain
                case 22: desc = "Shoot " + value + " navy ships with one cannonball."; break;
                case 23: desc = "Score " + quota + " ricochets."; break;
                case 24: desc = "Knock " + quota + " enemy crew overboard with a Crimson Shot powerup."; break;
        
                // 8 Brigand
                case 25: desc = "Sink " + quota + " navy ships."; break;
                case 26: desc = "Sink " + quota + " ships in a Brandy Slick."; break;
                case 27: desc = "Achieve a score of " + GuiHelper.CommaSeparatedValue(value) + "."; break;
        
                // 9 Looter
                case 28: desc = "Shoot " + quota + " ships without missing."; break;
                case 29: desc = "Survive until noon on Day " + value + " without sinking a navy ship."; break;
                case 30: desc = "Shoot " + value + " rival pirate ships with one cannonball."; break;
        
                // 10 Gallows Bird
                case 31: desc = "Sink " + quota + " rival pirate ships."; break;
                case 32: desc = "Shoot " + quota + " ships while aboard the Ghost Ship."; break;
                case 33: desc = "Survive until sunrise on Day " + value + "."; break;
        
                // 11 Scoundrel
                case 34: desc = "Sink the Treasure Fleet " + quota + " times."; break;
                case 35: desc = "Sink " + quota + " ships in acid pools with a Venom Shot powerup."; break;
                case 36: desc = "Shoot " + value + " ships with one cannon-^ball."; break;
        
                // 12 Rogue
                case 37: desc = "Shoot " + quota + " ships while flying Navy Colors."; break;
                case 38: desc = "Make " + quota + " rival pirates walk the plank."; break;
                case 39: desc = "Achieve a score of " + GuiHelper.CommaSeparatedValue(value) + "."; break;
        
                // 13 Pillager
                case 40: desc = "Shoot " + quota + " ships without missing."; break;
                case 41: desc = "Survive until sunrise on Day " + value + " without being shot."; break;
                case 42: desc = "Score " + quota + " ricochets."; break;
        
                // 14 Plunderer
                case 43: desc = "Shoot a navy ship and a rival pirate ship with one cannonball."; break;
                case 44: desc = "Sink " + quota + " ships with a Tornado Storm."; break;
                case 45: desc = "Survive until midnight on Day " + value + "."; break;
        
                // 15 Freebooter
                case 46: desc = "Sink " + quota + " navy ships."; break;
                case 47: desc = "Knock " + quota + " enemy crew over-^board with a Crimson Shot powerup."; break;
                case 48: desc = "Achieve a score of " + GuiHelper.CommaSeparatedValue(value) + "."; break;
        
                // 16 Privateer
                case 49: desc = "Sink " + quota + " rival pirate ships."; break;
                case 50: desc = "Survive until midnight on Day " + value + " without getting a red cross."; break;
                case 51: desc = "Score " + quota + " ricochets in a row."; break;
        
                // 17 Corsair
                case 52: desc = "Remove " + quota + " red crosses."; break;
                case 53: desc = "Survive until sunrise on Day " + value + " without sinking a navy ship."; break;
                case 54: desc = "Achieve a score of " + GuiHelper.CommaSeparatedValue(value) + " without getting any ricochets."; break;

                // 18 Buccaneer
                case 55: desc = "Shoot " + quota + " ships with a Molten Shot powerup."; break;
                case 56: desc = "Sink the Treasure Fleet twice with a Powder Keg deployment."; break;
                case 57: desc = "Survive until sunrise on Day " + value + " without using spells or munitions."; break;
        
                // 19 Sea Wolf
                case 58: desc = "Sink " + quota + " ships in a row without firing a cannonball."; break;
                case 59: desc = "Achieve a score of " + GuiHelper.CommaSeparatedValue(value) + "."; break;
                case 60: desc = "Survive until dusk on Day " + value + " without sinking a pirate ship."; break;
    
                // 20 Swashbuckler
                case 61: desc = "Spawn " + quota + " pools of magma with a Sea of Lava."; break;
                case 62: desc = "Sink " + quota + " ships in abyssal surges with an Abyssal Shot powerup."; break;
                case 63: desc = "Survive until sunrise on Day " + value + "."; break;
        
                // 21 Calico Jack
                case 64: desc = "Achieve a score of " + GuiHelper.CommaSeparatedValue(5000000) + " before sunrise on Day " + value + "."; break;
                case 65: desc = "Sink the Silver Train twice with a Molten Shot powerup."; break;
                case 66: desc = "Sink " + quota + " rival pirate ships with a Powder Keg deployment."; break;
        
                // 22 Black Bart
                case 67: desc = "Survive until midnight on Day " + value + " without shooting a ship."; break;
                case 68: desc = "Remove " + quota + " red crosses while flying Navy Colors."; break;
                case 69: desc = "Shoot " + quota + " navy ships with a Crimson Shot powerup."; break;
        
                // 23 Barbarossa
                case 70: desc = "Score " + quota + " ricochets before sunset on Day " + value + "."; break;
                case 71: desc = "Achieve a score of " + GuiHelper.CommaSeparatedValue(value) + "."; break;
                case 72: desc = "Shoot " + value + " ships with one cannon-^ball."; break;
        
                // 24 Captain Kidd
                case 73:
                default: desc = null; break;
            }

            return desc;
        }

        public static String LogbookDescriptionTextForKey(uint key)
        {
            string desc = null;
            int value = ObjectivesDescription.ValueForKey(key);
            int quota = ObjectivesDescription.QuotaForKey(key);

            switch (key)
            {
                case 2: desc = "Sink " + quota + " ships with a Powder Keg deploy-^ment."; break;
                case 18: desc = "Shoot " + value + " ships with one cannonball."; break;
                case 30: desc = "Shoot " + value + " rival pirate ships with one cannon-^ball."; break;
                case 36: desc = "Shoot " + value + " ships with one cannonball."; break;
                case 47: desc = "Knock " + quota + " enemy crew overboard with a Crimson Shot powerup."; break;
                case 58: desc = "Sink " + quota + " ships in a^row without firing a cannonball."; break;
                case 60: desc = "Survive until dusk on^Day " + value + " without sinking a pirate ship."; break;
                case 72: desc = "Shoot " + value + " ships with one cannonball."; break;
                default: desc = ObjectivesDescription.DescriptionTextForKey(key); break;
            }

            return desc;
        }

        // For non-binary events
        public static int QuotaForKey(uint key)
        {
            int quota = 0;

            switch (key)
            {
                // Unranked
                case 1: quota = 1; break;
                case 2: quota = 3; break;
                case 3: quota = 1; break;

                // Swabby
                case 4: quota = 1; break;
                case 5: quota = 1; break;
                case 6: quota = 1; break;

                // Deckhand
                case 7: quota = 5; break;
                case 8: quota = 10; break;
                case 9: quota = 5; break;

                // Jack Tar
                case 10: quota = 10; break;
                case 11: quota = 10; break;
                case 12: quota = 1; break;

                // Old Salt
                case 13: quota = 1; break;
                case 14: quota = 10; break;
                case 15: quota = 1; break;

                // Helmsman
                case 16: quota = 2; break;
                case 17: quota = 1; break;
                case 18: quota = 1; break;

                // Sea Dog
                case 19: quota = 15; break;
                case 20: quota = 20; break;
                case 21: quota = 1; break;

                // Villain
                case 22: quota = 1; break;
                case 23: quota = 10; break;
                case 24: quota = 15; break;

                // Brigand
                case 25: quota = 10; break;
                case 26: quota = 10; break;
                case 27: quota = 1; break;

                // Looter
                case 28: quota = 30; break;
                case 29: quota = 1; break;
                case 30: quota = 1; break;

                // Gallows Bird
                case 31: quota = 20; break;
                case 32: quota = 20; break;
                case 33: quota = 1; break;

                // Scoundrel
                case 34: quota = 3; break;
                case 35: quota = 20; break;
                case 36: quota = 1; break;

                // Rogue
                case 37: quota = 30; break;
                case 38: quota = 25; break;
                case 39: quota = 1; break;

                // Pillager
                case 40: quota = 50; break;
                case 41: quota = 1; break;
                case 42: quota = 20; break;

                // Plunderer
                case 43: quota = 1; break;
                case 44: quota = 25; break;
                case 45: quota = 1; break;

                // Freebooter
                case 46: quota = 25; break;
                case 47: quota = 30; break;
                case 48: quota = 1; break;

                // Privateer
                case 49: quota = 40; break;
                case 50: quota = 1; break;
                case 51: quota = 10; break;

                // Corsair
                case 52: quota = 30; break;
                case 53: quota = 1; break;
                case 54: quota = 1; break;

                // Buccaneer
                case 55: quota = 40; break;
                case 56: quota = 2; break;
                case 57: quota = 1; break;

                // Sea Wolf
                case 58: quota = 100; break;
                case 59: quota = 1; break;
                case 60: quota = 1; break;

                // Swashbuckler
                case 61: quota = 12; break;
                case 62: quota = 60; break;
                case 63: quota = 1; break;

                // Calico Jack
                case 64: quota = 1; break;
                case 65: quota = 2; break;
                case 66: quota = 6; break;

                // Black Bart
                case 67: quota = 1; break;
                case 68: quota = 10; break;
                case 69: quota = 10; break;

                // Barbarossa
                case 70: quota = 25; break;
                case 71: quota = 1; break;
                case 72: quota = 1; break;

                // Captain Kidd
                default: quota = 1; break; // Make it 1 so that isCompleted returns NO
            }

            return quota;
        }

        // For binary events (you either score a 2x ricochet or you don't)
        public static int ValueForKey(uint key)
        {
            int value = 0;

            switch (key)
            {
                case 3: value = 1; break;
                case 5: value = 2; break;
                case 12: value = 2; break;
                case 13: value = 5; break;
                case 15: value = 500000; break;
                case 17: value = 2; break;
                case 18: value = 3; break;
                case 21: value = 2; break;
                case 22: value = 2; break;
                case 27: value = 1500000; break;
                case 29: value = 2; break;
                case 30: value = 2; break;
                case 33: value = 3; break;
                case 36: value = 4; break;
                case 39: value = 3000000; break;
                case 41: value = 3; break;
                case 45: value = 3; break;
                case 48: value = 4500000; break;
                case 50: value = 2; break;
                case 53: value = 3; break;
                case 54: value = 2500000; break;
                case 57: value = 3; break;
                case 59: value = 7000000; break;
                case 60: value = 2; break;
                case 63: value = 4; break;
                case 64: value = 3; break;
                case 67: value = 2; break;
                case 70: value = 2; break;
                case 71: value = 8000000; break;
                case 72: value = 5; break;
                default: value = 0; break;
            }

            return value;
        }

        public static uint RequiredNpcShipTypeForKey(uint key)
        {
            uint shipType = 0;
    
            switch (key)
            {
                case 4: shipType = SHIP_TYPE_TREASURE_FLEET; break;
                case 16: shipType = SHIP_TYPE_SILVER_TRAIN; break;
                case 34: shipType = SHIP_TYPE_TREASURE_FLEET; break;
                case 56: shipType = SHIP_TYPE_TREASURE_FLEET; break;
                case 65: shipType = SHIP_TYPE_SILVER_TRAIN; break;
                default: shipType = 0; break;
            }
    
            return shipType;
        }

        public static uint RequiredAshTypeForKey(uint key)
        {
            uint ashType = 0;
    
            switch (key)
            {
                case 10: ashType = Ash.ASH_MOLTEN; break;
                case 24: ashType = Ash.ASH_SAVAGE; break;
                case 35: ashType = Ash.ASH_NOXIOUS; break;
                case 47: ashType = Ash.ASH_SAVAGE; break;
                case 55: ashType = Ash.ASH_MOLTEN; break;
                case 62: ashType = Ash.ASH_ABYSSAL; break;
                case 65: ashType = Ash.ASH_MOLTEN; break;
                case 69: ashType = Ash.ASH_SAVAGE; break;
                default: ashType = 0; break;
            }
    
            return ashType;
        }
        #endregion
    }
}
