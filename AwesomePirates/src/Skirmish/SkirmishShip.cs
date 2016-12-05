using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class SkirmishShip : PlayableShip
    {
        protected const double kEnviroDmgTimeout = 0.25;

        public SkirmishShip(ActorDef def, PlayerIndex playerIndex, SKTeamIndex teamIndex)
            : base(def, "Skirmish")
        {
            mSKPlayerIndex = playerIndex;
            mTeamIndex = teamIndex;
            mShipDeck = null;
            mBoost = 0f;
            mEnviroDmgTimer = 0.0;
            mSinkingTimer = 0.0;

            if (mCrewAiming != null)
                mCrewAiming.OwnerID = teamIndex;
        }

        #region Fields
        protected float mBoost;
        protected double mEnviroDmgTimer;
        protected double mSinkingTimer;
        protected PlayerIndex mSKPlayerIndex;
        protected SKTeamIndex mTeamIndex;
        protected Fixture mHitBox; // What other players' cannon raytraces are tested against
        protected SKShipDeck mShipDeck;
        protected VibrationDescriptor mVibrationEnviroDescriptor = new VibrationDescriptor(0f, 0, 0.75f, 1.2f * kEnviroDmgTimeout);
        #endregion

        #region Properties
        public PlayerIndex SKPlayerIndex { get { return mSKPlayerIndex; } }
        public SKTeamIndex TeamIndex { get { return mTeamIndex; } set { mTeamIndex = value; } }
        public Fixture HitBox { get { return mHitBox; } set { mHitBox = value; } }
        public SKShipDeck ShipDeck { get { return mShipDeck; } set { mShipDeck = value; } }
        public Vector2 DeckLoc { get { return (mShipDeck != null) ? mShipDeck.DeckLoc : Vector2.Zero; } }
        public override AshProc AshProc
        {
            get { return mAshProc; }
            set
            {
                if (value != mAshProc)
                {
                    DeactivateCannonProc();
                    mAshProc = value;

                    if (mAshProc != null && mAshProc.IsActive)
                        ActivateCannonProc();
                }
            }
        }
        protected override PlayerCannon RightCannon { get { return (mShipDeck != null) ? mShipDeck.RightCannon(SKPlayerIndex) : null; } }
        protected override PlayerCannon LeftCannon { get { return (mShipDeck != null) ? mShipDeck.LeftCannon(SKPlayerIndex) : null; } }
        protected override Helm Helm { get { return (mShipDeck != null) ? mShipDeck.Helm(SKPlayerIndex) : null; } }
        protected SKTeam Team { get { return mScene.SKManager.TeamForIndex(TeamIndex); } }
        protected override int WakeCategory { get { return (int)PFCat.SK_WAKES; } }
        protected override uint KegStyleKey { get { return (uint)TeamIndex; } }
        protected override Color NetColor { get { return SKHelper.ColorForTeamIndex(TeamIndex); } }
        protected override string BrandyFlameTexName { get { return "sk-brandy-flame-p" + (int)TeamIndex + "_"; } }
        public override float NetDragFactor { get { return 0.4f; } }
        public override double ReloadInterval { get { return 0.6; } }
        #endregion

        #region Methods
        protected override OffscreenArrow CreateOffscreenArrow()
        {
            SKTeam team = Team;
            if (team == null)
                return base.CreateOffscreenArrow();
            else
            {
                return new OffscreenArrow(new Vector4(-20.0f, -20.0f, 980.0f, 660.0f), 
                    SKHelper.OffscreenArrowTextureNameForTeamIndex(TeamIndex, team.IndexForTeamMember(SKPlayerIndex)));
            }
        }

        protected override void SetupCostumeImages()
        {
            mCostumeImages = SetupCostumeForTexturesStartingWith("sk-pf-ship-p" + (int)TeamIndex + "_", false);
            mDutchmanCostumeImages = SetupCostumeForTexturesStartingWith("ship-pf-dutchman_", false);
            mCamoCostumeImages = SetupCostumeForTexturesStartingWith("ship-pf-navy_", true);
        }

        protected override void SetupCustomizations()
        {
            SKTeam team = Team;
            if (team == null)
                return;

            Color customColor = SKHelper.ColorForTeamIndex(TeamIndex, team.IndexForTeamMember(SKPlayerIndex));

            if (mWake != null)
                mWake.CustomizeColor(customColor);
        }

        protected override void SaveFixture(Fixture fixture, int index)
        {
            switch (index)
            {
                case 5: mHitBox = fixture; break;
            }
            base.SaveFixture(fixture, index);
        }

        public override void Sink()
        {
            if (mSinking)
                return;

            PlayerCannon leftCannon = LeftCannon, rightCannon = RightCannon;

            if (leftCannon != null)
                leftCannon.Activated = false;
            if (rightCannon != null)
                rightCannon.Activated = false;

            if (ShipDeck != null)
                ShipDeck.SetPlayerDead(SKPlayerIndex);

            base.Sink();
            mSinking = true;
            mScene.RemoveProp(mOffscreenArrow, false);
            mWardrobe.RemoveChild(mCostume);
            mSinkingTimer = mSinkingClip.Duration;

            if (mBody != null)
                mBody.SetLinearVelocity(Vector2.Zero);
        }

        public override void AdvanceTime(double time)
        {
            base.AdvanceTime(time);

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

            float sailForce = mDrag * mSailForce * mBoost;
            SailWithForce(sailForce);

            // Based on helm rotation and ship specs. Also, the faster we travel, the more force on the rudder.
            float turnForce = mDrag * Helm.TurnAngle * mTurnForceMax * (sailForce / mSailForceMax);
            TurnWithForce(turnForce);

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
        }

        public void ApplyEnvironmentalDamage(int damage)
        {
            if (damage == 0 || Sinking || IsFlyingDutchman)
                return;

            if (mEnviroDmgTimer <= 0)
            {
                SKTeam team = Team;
                if (team != null)
                    team.AddHealth(-damage);
                mEnviroDmgTimer = kEnviroDmgTimeout;

                ControlsManager.CM.VibrateGamePad(SKPlayerIndex, ref mVibrationEnviroDescriptor);
            }
        }

        public override void DamageShip(int damage)
        {
            if (damage == 0 || Sinking || IsFlyingDutchman)
                return;

            SKTeam team = Team;
            if (team != null)
                team.AddHealth(-damage);

            ControlsManager.CM.VibrateGamePad(SKPlayerIndex, ref mVibrationDamageDescriptor);
        }

        protected void SinkingComplete()
        {
            mScene.Juggler.RemoveTweensWithTarget(this);
            mScene.RemoveActor(this); // Calls safe remove for us
        }

        protected override void CustomizePowderKeg(PowderKegActor keg)
        {
            base.CustomizePowderKeg(keg);

            if (keg != null)
                keg.OwnerID = TeamIndex;
        }

        protected override void CustomizeNet(NetActor net)
        {
            base.CustomizeNet(net);

            if (net != null)
                net.OwnerID = TeamIndex;
        }

        protected override void CustomizeBrandySlick(BrandySlickActor brandySlick)
        {
            base.CustomizeBrandySlick(brandySlick);

            if (brandySlick != null)
                brandySlick.OwnerID = TeamIndex;
        }

        protected override void ZeroOutFixtures()
        {
            base.ZeroOutFixtures();

            mHitBox = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (ShipDeck != null)
                        {
                            //if (RightCannon != null)
                            //    RightCannon.RemoveEventListener(PlayerCannonFiredEvent.CUST_EVENT_TYPE_PLAYER_CANNON_FIRED, (PlayerCannonFiredEventHandler)OnPlayerCannonFired);
                            if (LeftCannon != null)
                                LeftCannon.RemoveEventListener(PlayerCannonFiredEvent.CUST_EVENT_TYPE_PLAYER_CANNON_FIRED, (PlayerCannonFiredEventHandler)OnPlayerCannonFired);
                            mShipDeck = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion
    }
}
