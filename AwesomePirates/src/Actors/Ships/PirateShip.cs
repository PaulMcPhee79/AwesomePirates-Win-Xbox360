using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class PirateShip : PursuitShip
    {
        private const float kPirateLeashClearance = 2 * 110.0f;
        private const float kPirateDeckClearance = 2 * 25.0f;

        public PirateShip(ActorDef def, string key)
            : base(def, key)
        {
            mBootyGoneWanting = false;
            mLeash = kPirateLeashClearance;
        }

        #region Fields
        private float mLeash;
        #endregion

        #region Properties
        protected bool ShouldBeginPirating
        {
            get
            {
                bool unleashed = false, visibleToPlayer = false;

                visibleToPlayer = (X > mLeash && X < (mScene.ViewWidth - mLeash));
                visibleToPlayer = visibleToPlayer && (Y > mLeash && Y < (mScene.ViewHeight - (kPirateDeckClearance + mLeash)));

                if (visibleToPlayer && mBody != null)
                {
                    Vector2 bodyPos = mBody.GetPosition();
                    Vector2 spawnPoint = mDestination.Loc;
                    Vector2 dist = bodyPos - spawnPoint;

                    if (dist.LengthSquared() > (ResManager.P2M(150) * ResManager.P2M(150)))
                        unleashed = true;
                }

                return unleashed;
            }
        }
        protected bool IsOutOfCombatBounds
        {
            get
            {
                bool result = (mDuelState == PursuitState.Chasing || mDuelState == PursuitState.Aiming);
                return (result && (X < 0f || X > mScene.ViewWidth || Y < 0f || Y > mScene.ViewHeight));
            }
        }
        #endregion

        #region Methods
        public override void SetupShip()
        {
            base.SetupShip();

            // Shorten leash and reload interval as game gets harder
            mLeash = ResManager.RESM.GameFactorArea * Math.Max(2 * 50.0f, kPirateLeashClearance / Math.Max(1.0f, mAiModifier));
            mReloadInterval = Math.Max(5.0f, mReloadInterval / Math.Max(1.0f, mAiModifier));
        }

        public override void Reuse()
        {
            if (InUse)
                return;
            base.Reuse();

            mBootyGoneWanting = false;
            mLeash = kPirateLeashClearance;
        }

        protected override void DropLoot()
        {
            GameController gc = GameController.GC;
    
	        if (gc.ThisTurn.IsGameOver || gc.PlayerShip.IsBrigFull || TurnID != gc.ThisTurn.TurnID || mPreparingForNewGame)
		        return;

            LootProp prisonerProp = LootProp.LootPropWithType(LootProp.LootPropType.Prisoner, (int)PFCat.DECK);
            prisonerProp.Visible = false;
            prisonerProp.SetPosition(Origin, LootProp.CommonLootDestination);
            mScene.AddProp(prisonerProp);
        }

        public override void CreditPlayerSinker()
        {
            if (mScene.GameMode == GameMode.Career)
                mScene.AchievementManager.PirateShipSunk(this);
            else
                base.CreditPlayerSinker();
        }

        protected override float Navigate()
        {
            float sailForce = base.Navigate();
	
	        if (mDuelState == PursuitState.Ferrying || mDuelState == PursuitState.OutOfBounds) {
                if (ShouldBeginPirating)
                    DidReachDestination();
	        }
	
	        return sailForce;
        }
        #endregion
    }
}
