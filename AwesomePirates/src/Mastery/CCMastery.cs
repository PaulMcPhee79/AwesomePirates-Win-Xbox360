using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class CCMastery : IMasteryTemplate
    {
        // Mastery Trees
        public const uint TREE_CANNON_MASTERY                       = 1 << 0;
        public const uint TREE_VOODOO_MASTERY                       = 1 << 1;
        public const uint TREE_ROGUE_MASTERY                        = 1 << 2;

        // Cannon Mastery
        public const uint CANNON_MORTAL_IMPACT                      = 1 << 0;
        public const uint CANNON_TRAIL_OF_DESTRUCTION               = 1 << 1;

        public const uint CANNON_SCORCHED_HORIZON                   = 1 << 2;
        public const uint CANNON_NOXIOUS_STRAITS                    = 1 << 3;
        public const uint CANNON_DEPTH_CHARGE                       = 1 << 4;

        public const uint CANNON_CAPTAINS_REPRIEVE                  = 1 << 5;
        public const uint CANNON_CANNONEER                          = 1 << 6;

        public const uint CANNON_WRECKING_BALL                      = 1 << 7;

        public const uint CANNON_MASTERY_MASK                       = 0xff;

        // Voodoo Mastery
        public const uint VOODOO_DAVYS_FURY                         = 1 << 8;
        public const uint VOODOO_TWISTED_SISTERS                    = 1 << 9;

        public const uint VOODOO_GHOSTLY_AURA                       = 1 << 10;
        public const uint VOODOO_SPECTER_OF_SALVATION               = 1 << 11;

        public const uint VOODOO_MOLTEN_ARMY                        = 1 << 12;
        public const uint VOODOO_LEAGUES_OF_ANGUISH                 = 1 << 13;
        public const uint VOODOO_WITCH_DOCTOR                       = 1 << 14;

        public const uint VOODOO_ENCHANTED_REJUVENATION             = 1 << 15;

        public const uint VOODOO_MASTERY_MASK                       = 0xff00;

        // Rogue Mastery
        public const uint ROGUE_ENTANGLEMENT                        = 1 << 16;
        public const uint ROGUE_BLAZE_OF_GLORY                      = 1 << 17;

        public const uint ROGUE_FRIEND_OR_FOE                       = 1 << 18;
        public const uint ROGUE_THUNDERMAKER                        = 1 << 19;

        public const uint ROGUE_SHARK_BAIT                          = 1 << 20;
        public const uint ROGUE_THICK_AS_THIEVES                    = 1 << 21;
        public const uint ROGUE_ROYAL_PARDON                        = 1 << 22;

        public const uint ROGUE_SCOUNDRELS_WAGER                    = 1 << 23;

        public const uint ROGUE_MASTERY_MASK                        = 0xff0000;

        public CCMastery()
        {
            mMasteryBitmap = 0;
        }

        #region Fields
        private uint mMasteryBitmap;
        #endregion

        #region Properties
        public uint MasteryBitmap { get { return mMasteryBitmap; } set { mMasteryBitmap = value; } }
        #endregion

        #region Methods
        // Implement IMasteryTemplate Interface=
        public float ApplyScoreBonus(float score, ShipActor ship)
        {
            if (ship == null)
                return score;

            GameController gc = GameController.GC;
            PlayerShip playerShip = gc.PlayerShip;
            float adjustedScore = score;

            if (playerShip == null)
                return adjustedScore;

            if ((mMasteryBitmap & CANNON_MASTERY_MASK) != 0)
            {
                if ((ship.DeathBitmap & DeathBitmaps.PLAYER_CANNON) != 0)
                {
                    if ((mMasteryBitmap & CANNON_MORTAL_IMPACT) != 0)
                        adjustedScore *= 1.05f;

                    if (ship.RicochetCount > 0)
                    {
                        if ((mMasteryBitmap & CANNON_TRAIL_OF_DESTRUCTION) != 0)
                            adjustedScore = adjustedScore * 1.15f;

                        if ((mMasteryBitmap & CANNON_WRECKING_BALL) != 0)
                            adjustedScore *= 1.5f;
                    }
                }
                else if ((mMasteryBitmap & CANNON_DEPTH_CHARGE) != 0)
                {
                    if ((ship.DeathBitmap & DeathBitmaps.ABYSSAL_SURGE) != 0)
                        adjustedScore *= 1.5f;
                }
            }

            if ((mMasteryBitmap & VOODOO_MASTERY_MASK) != 0)
            {
                if ((mMasteryBitmap & VOODOO_GHOSTLY_AURA) != 0 && playerShip.IsFlyingDutchman)
                    adjustedScore = adjustedScore * 1.3f;

                if ((mMasteryBitmap & VOODOO_MOLTEN_ARMY) != 0 && (ship.DeathBitmap & (DeathBitmaps.SEA_OF_LAVA | DeathBitmaps.MAGMA_POOL)) != 0)
                    adjustedScore *= 3f;

                if ((mMasteryBitmap & VOODOO_WITCH_DOCTOR) != 0 && (ship.DeathBitmap & DeathBitmaps.VOODOO) != 0)
                    adjustedScore = adjustedScore * 1.25f;
            }

            if ((mMasteryBitmap & ROGUE_MASTERY_MASK) != 0)
            {
                if ((mMasteryBitmap & ROGUE_BLAZE_OF_GLORY) != 0 && (ship.DeathBitmap & DeathBitmaps.BRANDY_SLICK) != 0)
                    adjustedScore *= 3f;

                if ((mMasteryBitmap & ROGUE_FRIEND_OR_FOE) != 0 && playerShip.IsCamouflaged)
                    adjustedScore *= 1.3f;
                else if ((mMasteryBitmap & ROGUE_THUNDERMAKER) != 0 && (ship.DeathBitmap & DeathBitmaps.POWDER_KEG) != 0)
                    adjustedScore *= 3f;

                if ((mMasteryBitmap & ROGUE_THICK_AS_THIEVES) != 0)
                    adjustedScore = adjustedScore * 1.1f;

                if ((mMasteryBitmap & ROGUE_ROYAL_PARDON) != 0 && ship is NavyShip)
                    adjustedScore *= 1.5f;

                if ((mMasteryBitmap & ROGUE_SCOUNDRELS_WAGER) != 0 && gc.ThisTurn.Mutiny == 0 && gc.ThisTurn.MutinyCountdown.Counter == gc.ThisTurn.MutinyCountdown.CounterMax)
                    adjustedScore *= 1.3f;
            }

            return adjustedScore;
        }

        public float ApplyScoreBonus(float score, OverboardActor prisoner)
        {
            if (prisoner == null)
                return score;

            GameController gc = GameController.GC;
            PlayerShip playerShip = gc.PlayerShip;
            float adjustedScore = score;

            if (playerShip == null)
                return adjustedScore;

            if ((mMasteryBitmap & CANNON_MASTERY_MASK) != 0)
            {
                if ((prisoner.DeathBitmap & DeathBitmaps.PLAYER_CANNON) != 0)
                {
                    if ((mMasteryBitmap & CANNON_MORTAL_IMPACT) != 0)
                        adjustedScore = adjustedScore * 1.05f;
                }
            }

            if ((mMasteryBitmap & VOODOO_MASTERY_MASK) != 0)
            {
                if ((mMasteryBitmap & VOODOO_GHOSTLY_AURA) != 0 && playerShip.IsFlyingDutchman)
                    adjustedScore = adjustedScore * 1.3f;

                if ((mMasteryBitmap & VOODOO_MOLTEN_ARMY) != 0 && (prisoner.DeathBitmap & (DeathBitmaps.SEA_OF_LAVA | DeathBitmaps.MAGMA_POOL)) != 0)
                    adjustedScore *= 3f;
                else if ((mMasteryBitmap & VOODOO_LEAGUES_OF_ANGUISH) != 0 && (prisoner.DeathBitmap & DeathBitmaps.WHIRLPOOL) != 0)
                    adjustedScore *= 3f;

                if ((mMasteryBitmap & VOODOO_WITCH_DOCTOR) != 0 && (prisoner.DeathBitmap & DeathBitmaps.VOODOO) != 0)
                    adjustedScore *= 1.25f;
            }

            if ((mMasteryBitmap & ROGUE_MASTERY_MASK) != 0)
            {
                if ((mMasteryBitmap & ROGUE_BLAZE_OF_GLORY) != 0 && (prisoner.DeathBitmap & DeathBitmaps.BRANDY_SLICK) != 0)
                    adjustedScore *= 3f;

                if ((mMasteryBitmap & ROGUE_FRIEND_OR_FOE) != 0 && playerShip.IsCamouflaged)
                    adjustedScore *= 1.3f;

                if ((mMasteryBitmap & ROGUE_SHARK_BAIT) != 0 && (prisoner.DeathBitmap & DeathBitmaps.SHARK) != 0)
                    adjustedScore *= 1.5f;

                if ((mMasteryBitmap & ROGUE_THICK_AS_THIEVES) != 0)
                    adjustedScore *= 1.1f;

                if ((mMasteryBitmap & ROGUE_SCOUNDRELS_WAGER) != 0 && gc.ThisTurn.Mutiny == 0 && gc.ThisTurn.MutinyCountdown.Counter == gc.ThisTurn.MutinyCountdown.CounterMax)
                    adjustedScore *= 1.3f;
            }

            return adjustedScore;
        }

        public static void PopulateModel(MasteryModel model)
        {
            // ------- Build Cannon Mastery Tree -------
            MasteryTree tree = new MasteryTree(TREE_CANNON_MASTERY);

                // Tier 1
            MasteryRow row = new MasteryRow();
            row.AddNode(new MasteryNode(CANNON_MORTAL_IMPACT));
            row.AddNode(new MasteryNode(CANNON_TRAIL_OF_DESTRUCTION));
            tree.AddRow(row);

                // Tier 2
            row = new MasteryRow();
            row.AddNode(new MasteryNode(CANNON_SCORCHED_HORIZON));
            row.AddNode(new MasteryNode(CANNON_NOXIOUS_STRAITS));
            row.AddNode(new MasteryNode(CANNON_DEPTH_CHARGE));
            tree.AddRow(row);

                // Tier 3
            row = new MasteryRow();
            row.AddNode(new MasteryNode(CANNON_CAPTAINS_REPRIEVE));
            row.AddNode(new MasteryNode(CANNON_CANNONEER));
            tree.AddRow(row);

                // Tier 4
            row = new MasteryRow();
            row.AddNode(new MasteryNode(CANNON_WRECKING_BALL));
            tree.AddRow(row);
            model.AddTree(tree);


            // ------- Build Voodoo Mastery Tree -------
            tree = new MasteryTree(TREE_VOODOO_MASTERY);

                // Tier 1
            row = new MasteryRow();
            row.AddNode(new MasteryNode(VOODOO_DAVYS_FURY));
            row.AddNode(new MasteryNode(VOODOO_TWISTED_SISTERS));
            tree.AddRow(row);

                // Tier 2
            row = new MasteryRow();
            row.AddNode(new MasteryNode(VOODOO_GHOSTLY_AURA));
            row.AddNode(new MasteryNode(VOODOO_SPECTER_OF_SALVATION));
            tree.AddRow(row);

                // Tier 3
            row = new MasteryRow();
            row.AddNode(new MasteryNode(VOODOO_MOLTEN_ARMY));
            row.AddNode(new MasteryNode(VOODOO_LEAGUES_OF_ANGUISH));
            row.AddNode(new MasteryNode(VOODOO_WITCH_DOCTOR));
            tree.AddRow(row);

                // Tier 4
            row = new MasteryRow();
            row.AddNode(new MasteryNode(VOODOO_ENCHANTED_REJUVENATION));
            tree.AddRow(row);
            model.AddTree(tree);


            // ------- Build Rogue Mastery Tree -------
            tree = new MasteryTree(TREE_ROGUE_MASTERY);

                // Tier 1
            row = new MasteryRow();
            row.AddNode(new MasteryNode(ROGUE_ENTANGLEMENT));
            row.AddNode(new MasteryNode(ROGUE_BLAZE_OF_GLORY));
            tree.AddRow(row);

                // Tier 2
            row = new MasteryRow();
            row.AddNode(new MasteryNode(ROGUE_FRIEND_OR_FOE));
            row.AddNode(new MasteryNode(ROGUE_THUNDERMAKER));
            tree.AddRow(row);

                // Tier 3
            row = new MasteryRow();
            row.AddNode(new MasteryNode(ROGUE_SHARK_BAIT));
            row.AddNode(new MasteryNode(ROGUE_THICK_AS_THIEVES));
            row.AddNode(new MasteryNode(ROGUE_ROYAL_PARDON));
            tree.AddRow(row);

                // Tier 4
            row = new MasteryRow();
            row.AddNode(new MasteryNode(ROGUE_SCOUNDRELS_WAGER));
            tree.AddRow(row);
            model.AddTree(tree);
        }
        #endregion
    }
}
