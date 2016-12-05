using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class DeathBitmaps
    {
        // Describes how a ship sunk
        public const uint ALIVE = 0x0;
        public const uint NPC_CANNON = 0x1;
        public const uint TOWN_CANNON = 0x2;
        // Space for one more
        public const uint NPC_MASK = 0x7;

        public const uint PLAYER_CANNON = 0x8;

        // Voodoo
        public const uint HAND_OF_DAVY = 0x10;
        public const uint WHIRLPOOL = 0x20;
        public const uint GHOSTLY_TEMPEST = 0x40;
        public const uint SEA_OF_LAVA = 0x80;
        // Space for 9 more

        // Munitions
        public const uint POWDER_KEG = 0x10000;
        public const uint BRANDY_SLICK = 0x20000;
        public const uint GADGET = (POWDER_KEG | BRANDY_SLICK);
        // Space for 6 more

        // Ashes
        public const uint ACID_POOL = 0x1000000;
        public const uint MAGMA_POOL = 0x2000000;
        public const uint ABYSSAL_SURGE = 0x4000000;
        // Space for 2 more

        // Misc
        public const uint SHARK = 0x20000000;
        public const uint DAMASCUS = 0x40000000;
        // Space for 2 more

        // Aggregate
        public const uint VOODOO = (HAND_OF_DAVY | WHIRLPOOL | GHOSTLY_TEMPEST | SEA_OF_LAVA | MAGMA_POOL);
    }
}
