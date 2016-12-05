using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class PrisonerProp : LootProp
    {
        public PrisonerProp(int category)
            : base(category)
        {
            mLootSfxKey = null;
            SetupProp();
        }

        public override uint ReuseKey { get { return (uint)LootPropType.Prisoner; } }

        protected override void SetupProp()
        {
            if (mCostume == null)
            {
		        mCostume = new SPImage(mScene.TextureByName("pirate-hat"));
		        mCostume.X = -mCostume.Width / 2;
		        mCostume.Y = -mCostume.Height / 2;
	        }
	
	        base.SetupProp();
        }

        public override void Loot()
        {
            if (mLooted)
                return;

            GameController gc = GameController.GC;
            if (mLooted || gc.ThisTurn.IsGameOver || TurnID != gc.ThisTurn.TurnID || gc.PlayerShip.IsBrigFull)
            {
                DestroyLoot();
                return;
            }

	        Prisoner prisoner = gc.PlayerShip.AddRandomPrisoner();
	        if (prisoner != null)
		        base.Loot();
	        else
                DestroyLoot();
        }
    }
}
