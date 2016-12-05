using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Box2D.XNA;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class SKBotShip : SkirmishShip, ISKCoupled
    {
        public SKBotShip(ActorDef def, PlayerIndex playerIndex, SKTeamIndex teamIndex)
            : base(def, playerIndex, teamIndex)
        {
            mCoupledTurnForce = 0;
            mCoupledSailForce = 0;
        }

        #region Fields
        protected float mCoupledTurnForce;
        protected float mCoupledSailForce;
        #endregion

        #region Properties
        public float CoupledTurnForce { get { return mCoupledTurnForce; } }
        public float CoupledSailForce { get { return mCoupledSailForce; } }
        #endregion

        protected override void TurnWithForce(float force)
        {
            base.TurnWithForce(force);
            mCoupledTurnForce = force;
        }

        protected override void SailWithForce(float force)
        {
            base.SailWithForce(force);
            mCoupledSailForce = force;
        }

        public void CopyPhysicsFrom(ISKCoupled couple)
        {
#if false
            TurnWithForce(couple.CoupledTurnForce);
            SailWithForce(couple.CoupledSailForce);
#else
            ShipActor ship = couple as ShipActor;
            mBody.SetLinearVelocity(ship.B2Body.GetLinearVelocity());
            mBody.SetAngularVelocity(ship.B2Body.GetAngularVelocity());
            Transform xf;
            ship.B2Body.GetTransform(out xf);
            mBody.SetTransform(xf.Position, xf.GetAngle());

            mCoupledTurnForce = couple.CoupledTurnForce;
            mCoupledSailForce = couple.CoupledSailForce;
#endif
        }

        public void UpdateAppearance(double time)
        {
            SKTeam team = Team;
            if (team != null && team.Health == 0 && !Sinking)
            {
                Sink();
                return;
            }

            if (mRemoveMe || Sinking)
            {
                if (mSinkingTimer > 0.0)
                {
                    mSinkingTimer -= time;

                    if (mSinkingTimer <= 0.0)
                        SinkingComplete();
                }
                return;
            }

            if (mPowderKegTimer > 0.0)
            {
                mPowderKegTimer -= time;

                if (mPowderKegTimer <= 0.0)
                    DropNextPowderKeg();
            }

            if (mEnviroDmgTimer > 0.0)
                mEnviroDmgTimer -= time;

            if (mStern == null)
                return;

            // Ship position/orientation
            AABB aabb;
            mStern.GetAABB(out aabb, 0);
            Vector2 rudder = aabb.GetCenter();

            X = ResManager.M2PX(rudder.X);
            Y = ResManager.M2PY(rudder.Y);
            Rotation = -B2Rotation;

            if (mShipDeck == null || mSinking || mTimeTravelling)
                return;
            else if (mSuspendedMode)
            {
                UpdateCostumeWithAngularVelocity(((mBody != null) ? mBody.GetAngularVelocity() : 0f));
                return;
            }

            if (mDragDuration > 0)
            {
                mDragDuration -= (float)time;

                if (mDragDuration <= 0)
                    Drag = 1f;
            }

            if (mShipDeck.IsBoosting(SKPlayerIndex))
                mBoost = Math.Min(2f, mBoost + 0.1f);
            else
                mBoost = Math.Max(1f, mBoost - 0.1f);

            // We should't get slowed in nets when in Ghost Ship.
            if (mFlyingDutchman)
                mDrag = 1f;

            float sailForce = mCoupledSailForce;

            // Based on helm rotation and ship specs. Also, the faster we travel, the more force on the rudder.
            float turnForce = mCoupledTurnForce;

            if (mWake != null)
            {
                mWake.SpeedFactor = Math.Min(1f, 0.83f * sailForce / ((mMotorBoatingSob) ? 153f : 183f));
                TickWakeOdometer(sailForce * (float)time * GameController.GC.Fps);
            }

            UpdateCostumeWithAngularVelocity(((mBody != null) ? mBody.GetAngularVelocity() : 0));

            // Offscreen arrow
            mOffscreenArrow.UpdateArrowLocation(PX, PY);
            mOffscreenArrow.UpdateArrowRotation(Rotation);

            // Out of bounds mutiny penalty
            if ((X < -20f || X > mScene.ViewWidth + 20f) || (Y < -20f || Y > mScene.ViewHeight + 20f))
            {
                // Orientate towards center of playfield
                if (mBody != null)
                {
                    Vector2 pfCenter = new Vector2(ResManager.P2M(mScene.ViewWidth / 2), ResManager.P2M(mScene.ViewHeight / 2));
                    Vector2 dir = mBody.GetPosition();
                    pfCenter -= dir;

                    // We subtract 90 degrees because Box2D's axes have their angular origin on the positive vertical axis, whereas atan2f uses the positive horizontal axis.
                    if (dir.X != 0 || dir.Y != 0)
                        mBody.SetTransform(mBody.GetPosition(), (float)Math.Atan2(pfCenter.Y, pfCenter.X) - SPMacros.PI_HALF);
                }
            }


            // Cannon overheat maintenance
            if (mCannonSpamCapacitor > 0)
            {
                mCannonSpamCapacitor -= time;

                if (mCannonSpamCapacitor <= 0)
                {
                    if (mCannonsOverheated)
                        DisableOverheatedCannons(false);
                    mCannonSpamCapacitor = 0;
                }
            }

            if (mShipHitGlows != null)
            {
                foreach (ShipHitGlow glow in mShipHitGlows)
                {
                    glow.X = PX;
                    glow.Y = PY;
                }
            }
        }

        public override void AdvanceTime(double time)
        {
            // Do nothing
        }

        public void AdvanceTimeProxy(double time)
        {
            base.AdvanceTime(time);
        }

        public void FireCannons()
        {
            PlayerCannonWasRequestedToFire(mShipDeck.LeftCannon(mSKPlayerIndex));
        }

        public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (other is SKPursuitShip)
                return false;
            else
                return base.PreSolve(other, fixtureSelf, fixtureOther, contact);
        }
    }
}
