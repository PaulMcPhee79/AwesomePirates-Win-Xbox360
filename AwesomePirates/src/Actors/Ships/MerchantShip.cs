using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class MerchantShip : NpcShip
    {
        public MerchantShip(ActorDef def, string key)
            : base(def, key)
        {
            mTargetAcquired = false;
            mIsDimming = true;
            mIsFlashing = false;
            mFlashColor = new Color(0xff, 0xff, 0xff);
        }

        #region Fields
        protected bool mIsDimming;
        protected bool mIsFlashing;
        protected Color mFlashColor;
        protected bool mTargetAcquired;
        protected ShipDetails.ShipSide mTargetSide;
        protected float mTargetX;
        protected float mTargetY;
        protected Fixture mDefender;
        #endregion

        #region Properties
        public Fixture Defender { get { return mDefender; } }
        protected virtual bool IsCloseToDocking
        {
            get
            {
                bool result = false;

                if (!GameController.GC.ThisTurn.IsGameOver && !mInWhirlpoolVortex)
                {
                    switch (mDestination.Finish)
                    {
                        case ActorAi.kPlaneIdNorth:
                            if (X > (mScene.ViewWidth - 2 * 60))
                                result = true;
                            break;
                        case ActorAi.kPlaneIdEast:
                            if (Y > (mScene.ViewHeight - 2 * 95))
                                result = true;
                            break;
                        case ActorAi.kPlaneIdSouth:
                            if (X < 2 * 60)
                                result = true;
                            break;
                        case ActorAi.kPlaneIdWest:
                            if (Y < 2 * 60)
                                result = true;
                            break;
                        case ActorAi.kPlaneIdTown:
                            if (X < 2 * 70 && Y < 2 * 70)
                                result = true;
                            break;
                        default:
                            result = false;
                            break;
                    }
                }

                return result;
            }
        }
        #endregion

        #region Methods
        public override void Reuse()
        {
            if (InUse)
                return;
            base.Reuse();

            mTargetAcquired = false;
            mIsDimming = true;
            mIsFlashing = false;
            mFlashColor = new Color(0xff, 0xff, 0xff);
        }

        public override void NegotiateTarget(ShipActor target)
        {
            //if (mTargetAcquired || mInWhirlpoolVortex || mDocking || mBody == null)
            //    return;
            //Vector2 bodyPos = mBody.GetPosition();
            //Vector2 enemyPos = target.B2Body.GetPosition();
            //Vector2 dest = bodyPos - enemyPos;
	
            //Vector2 linearVel = mBody.GetLinearVelocity();
            //float angleToTarget = Box2DUtils.SignedAngle(ref dest, ref linearVel);
            //int angleInDegrees = (int)SPMacros.SP_R2D(angleToTarget);
	
            //mTargetSide = (angleInDegrees > 0) ? AwesomePirates.ShipDetails.ShipSide.Port : AwesomePirates.ShipDetails.ShipSide.Starboard;
            //Vector2 closest = target.ClosestPositionTo(bodyPos);
            //mTargetX = ResManager.M2PX(closest.X);
            //mTargetY = ResManager.M2PY(closest.Y);
            //mTargetAcquired = true;
        }

        public override void AdvanceTime(double time)
        {
            base.AdvanceTime(time);

            if (mRemoveMe || mDocking)
		        return;
	        if (mTargetAcquired)
            {
		        Cannonball cannonball = FireCannon(mTargetSide, 1f);
                cannonball.CalculateTrajectoryFrom(mTargetX, mTargetY);
		        mTargetAcquired = false;
	        }

            if (IsCloseToDocking)
                Flash();
            else if (mIsFlashing)
            {
                mIsFlashing = false;
                mCurrentCostumeImages[mCostumeIndex].Color = new Color(0xff, 0xff, 0xff);
            }
        }

        protected override void SaveFixture(Fixture fixture, int index)
        {
            base.SaveFixture(fixture, index);

            switch (index)
            {
                case 7:
                    mDefender = fixture;
                    break;
                default: break;
            }
        }

        //public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        //{
        //    if (fixtureSelf == mDefender)
        //    {
        //        if (!mReloading && other is PlayableShip)
        //        {
        //            PlayableShip ship = other as PlayableShip;
			
        //            if (!ship.IsCamouflaged && !ship.MotorBoating)
        //                NegotiateTarget(ship);
        //        }
        //        return false;
        //    }
        //    else
        //        return base.PreSolve(other, fixtureSelf, fixtureOther, contact);
        //}

        protected void Flash()
        {
#if IOS_SCREENS
            return;
#else
            if (mScene.RaceEnabled || mScene.GameMode != GameMode.Career)
                return;
            mIsFlashing = true;
    
            if (mIsDimming) {
                mFlashColor.R -= 0x06;
                mFlashColor.G -= 0x06;
                mFlashColor.B -= 0x06;
        
                if (mFlashColor.R < 0x88)
                    mIsDimming = false;
            } else {
                mFlashColor.R += 0x06;
                mFlashColor.G += 0x06;
                mFlashColor.B += 0x06;
        
                if (mFlashColor.R > 0xf8)
                    mIsDimming = true;
            }
    
            mCurrentCostumeImages[mCostumeIndex].Color = mFlashColor;
#endif
        }

        public override bool HasBootyGoneWanting(SPSprite shooter)
        {
            return (shooter is PirateShip);
        }

        public override void CreditPlayerSinker()
        {
            if (mScene.GameMode == GameMode.Career)
                mScene.AchievementManager.MerchantShipSunk(this);
            else
                base.CreditPlayerSinker();
        }

        public override void CheckinPooledResources()
        {
            if (mCurrentCostumeImages != null)
            {
                Color c = new Color(0xff, 0xff, 0xff);
                foreach (SPImage image in mCurrentCostumeImages)
                    image.Color = c;
            }

            base.CheckinPooledResources();
        }

        protected override void ZeroOutFixtures()
        {
            base.ZeroOutFixtures();
            mDefender = null;
        }
        #endregion
    }
}
