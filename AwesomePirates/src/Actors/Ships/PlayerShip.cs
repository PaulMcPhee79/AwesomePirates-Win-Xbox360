using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class PlayerShip : PlayableShip
    {
        public enum MontyState
        {
            FirstMate = 0,
            Skipper,
            Tripper,
            Conspirator,
            Mutineer
        }

        public PlayerShip(ActorDef def)
            : base(def, "Player")
        {
            mShipDeck = null;
            mMontyDest = null;
            mPreparedForGameOver = false;
            mMonty = MontyState.FirstMate;
        }

        #region Fields
        protected ShipDeck mShipDeck;

        // Monty
        protected bool mPreparedForGameOver;
        protected MontyState mMonty;
        protected Destination mMontyDest;
        #endregion

        #region Properties
        public ShipDeck ShipDeck { get { return mShipDeck; } set { mShipDeck = value; } }
        public override AshProc AshProc
        {
            get { return mAshProc; }
            set
            {
                if (value != mAshProc)
                {
                    DeactivateCannonProc();
                    mAshProc = value;

                    if (mAshProc != null)
                    {
                        if (mAshProc.ChargesRemaining == mAshProc.TotalCharges)
                            mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_ASH_PICKED_UP, mAshProc.Proc);
                        mShipDeck.ComboDisplay.SetupProcWithTexturePrefix(mAshProc.TexturePrefix);

                        if (mAshProc.IsActive)
                            ActivateCannonProc();
                    }
                }
            }
        }
        public bool IsPlankingEnqueued { get { return (mShipDeck != null && mShipDeck.Plank != null) ? mShipDeck.Plank.State == Plank.PlankState.DeadManWalking : false; } }
        public MontyState Monty
        {
            get { return mMonty; }
            set
            {
                if (mMonty == value)
                    return;

                switch (value)
                {
                    case MontyState.FirstMate:
                        if (mOffscreenArrow != null)
                            mOffscreenArrow.Enabled = true;
                        if (mShipDeck != null)
                            mShipDeck.CombatControlsEnabled = true;
                        break;
                    case MontyState.Skipper:
                        if (mMontyDest == null)
                            mMontyDest = new Destination();
                        mMontyDest.Dest = new Vector2(ResManager.P2MX(mScene.ViewWidth / 2), ResManager.P2MY(mScene.ViewHeight / 2));
                        mOffscreenArrow.Enabled = false;
                        mShipDeck.CombatControlsEnabled = false;
                        break;
                    case MontyState.Tripper:
                        mTripCounter = 2;
                        mOffscreenArrow.Enabled = false;
                        mShipDeck.CombatControlsEnabled = false;
                        DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MONTY_SKIPPERED));
                        break;
                    case MontyState.Conspirator:
                        mOffscreenArrow.Enabled = false;
                        mShipDeck.CombatControlsEnabled = false;
                        break;
                    case MontyState.Mutineer:
                        mOffscreenArrow.Enabled = false;
                        mShipDeck.CombatControlsEnabled = false;

                        if (mMontyDest == null)
                            mMontyDest = new Destination();
                        mMontyDest.Dest = new Vector2(ResManager.P2MX(mScene.ViewWidth / 2), ResManager.P2MY(-5 * mScene.ViewHeight));
                        break;
                }

                mMonty = value;
            }
        }
        protected override PlayerCannon RightCannon { get { return (mShipDeck != null) ? mShipDeck.RightCannon : null; } }
        protected override PlayerCannon LeftCannon { get { return (mShipDeck != null) ? mShipDeck.LeftCannon : null; } }
        protected override Helm Helm { get { return (mShipDeck != null) ? mShipDeck.Helm : null; } }
        #endregion

        #region Methods
        protected override void ActivateCannonProc()
        {
            mShipDeck.ComboDisplay.ActivateProc();
            base.ActivateCannonProc();
        }

        protected override void DeactivateCannonProc()
        {
            mShipDeck.ComboDisplay.DeactivateProc();
            base.DeactivateCannonProc();
        }

        public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (Monty != MontyState.FirstMate)
                return false;

            return base.PreSolve(other, fixtureSelf, fixtureOther, contact);
        }

        protected float MontyNavigate()
        {
            float sailForce = mDrag * mSailForce;
            SailWithForce(sailForce);

            if (mBody == null)
                return sailForce;

            if (mMontyDest != null)
            {
                Vector2 bodyPos = mBody.GetPosition();
                Vector2 dest = mMontyDest.Dest;
                dest -= bodyPos;

                if (Math.Abs(dest.X) < 2f && Math.Abs(dest.Y) < 2f)
                {
                    if (Monty == MontyState.Skipper)
                        Monty = MontyState.Tripper;
                }
                else
                {
                    // Turn towards destination
                    Vector2 linearVel = mBody.GetLinearVelocity();
                    float angleToTarget = Box2DUtils.SignedAngle(ref dest, ref linearVel);

                    if (angleToTarget != 0.0f)
                    {
                        float turnForce = ((angleToTarget > 0.0f) ? 2.0f : -2.0f) * (mTurnForceMax * (sailForce / mSailForceMax));
                        TurnWithForce(turnForce);

                        if (mCostumeIndex != mCostumeUprightIndex && mShipDeck != null && Helm != null)
                            Helm.AddRotation(-1 * ((mCostumeIndex - mCostumeUprightIndex) / 10f));
                    }
                }
            }

            return sailForce;
        }

#if false
        private static double debugTimer = 0;
        public override void AdvanceTime(double time)
        {
            debugTimer += time;

            if (debugTimer >= 5.0)
            {
                debugTimer -= 5.0;
                Debug.WriteLine("LinearVel: {0} AngularVel: {1}", mBody.GetLinearVelocity().Length(), mBody.GetAngularVelocity());
            }
#else
        public override void AdvanceTime(double time)
        {
#endif
            base.AdvanceTime(time);

            if (mPowderKegTimer > 0.0)
            {
                mPowderKegTimer -= time;

                if (mPowderKegTimer <= 0.0)
                    DropNextPowderKeg();
            }

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

            if (Monty != MontyState.Conspirator)
            {
                if (Monty == MontyState.Tripper)
                {
                    mTripCounter -= time;

                    if (mTripCounter < 0)
                        Monty = MontyState.Conspirator;
                }

                if (Monty == MontyState.Mutineer && mPreparedForGameOver)
                {
                    if ((X < -70f || X > mScene.ViewWidth + 70f) || (Y < -70f || Y > mScene.ViewHeight + 70f))
                    {
                        mScene.RemoveActor(this);
                        return;
                    }
                }

                float sailForce = 0f;

                if (Monty != MontyState.FirstMate)
                {
                    sailForce = MontyNavigate();
                }
                else
                {
                    sailForce = mDrag * mSailForce;
                    SailWithForce(sailForce);

                    // Based on helm rotation and ship specs. Also, the faster we travel, the more force on the rudder.
                    float turnForce = mDrag * Helm.TurnAngle * mTurnForceMax * (sailForce / mSailForceMax);
                    TurnWithForce(turnForce);
                }

                if (mWake != null)
                {
                    mWake.SpeedFactor = Math.Min(1f, 0.83f * sailForce / ((mMotorBoatingSob) ? 153f : 183f));
                    TickWakeOdometer(sailForce * (float)time * GameController.GC.Fps);
                }
            }

            UpdateCostumeWithAngularVelocity(((mBody != null) ? mBody.GetAngularVelocity() : 0));

            // Offscreen arrow
            mOffscreenArrow.UpdateArrowLocation(PX, PY);
            mOffscreenArrow.UpdateArrowRotation(Rotation);

            // Plank
            if (mOffscreenArrow.Visible || !mPlankEnabled || Monty != MontyState.FirstMate)
            {
                // Disable walk-the-plank feature
                mShipDeck.Plank.State = Plank.PlankState.Inactive;
            }
            else if (mShipDeck.Plank.State == Plank.PlankState.Inactive && mShipDetails.Prisoners.Count > 0)
            {
                // Enable walk-the-plank feature
                mShipDeck.Plank.State = Plank.PlankState.Active;
            }

            // Out of bounds mutiny penalty
            if (Monty == MontyState.FirstMate && ((X < -20f || X > mScene.ViewWidth + 20f) || (Y < -20f || Y > mScene.ViewHeight + 20f)))
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

            // Failed speedboat achievement
            if (MotorBoating && mFailedMotorboating)
            {
                mDashDialFlashTimer += time;

                if (mDashDialFlashTimer > 0.5)
                {
                    mDashDialFlashTimer = 0.0;
                    mShipDeck.FlashFailedMphDial();
                }
            }
        }

        public override void Sink()
        {
            if (mSinking)
                return;

            base.Sink();
            mSinking = true;
            mScene.RemoveProp(mOffscreenArrow, false);
            RemoveAllChildren();

            if (mBody != null)
                mBody.SetLinearVelocity(Vector2.Zero);

            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_PLAYER_SHIP_SINKING));
        }

        public override void DamageShipWithCannonball(Cannonball cannonball)
        {
            base.DamageShipWithCannonball(cannonball);
            mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_PLAYER_HIT);
        }

        public override void OnRaceUpdate(RaceEvent ev)
        {
            if (mShipDeck == null)
                return;

            if (ev.RaceFinished || mRaceUpdateIndex == -1)
            {
                mShipDeck.SetRaceTime(Globals.FormatElapsedTime(ev.RaceTime));
                mShipDeck.SetLapTime(Globals.FormatElapsedTime(ev.LapTime));
                mShipDeck.SetMph((float)ev.Mph);
                mShipDeck.SetLap("" + ev.Lap + "/" + ev.TotalLaps);

                if (ev.RaceFinished)
                    mFailedMotorboating = ev.RaceTime > RaceEvent.RequiredRaceTimeForLapCount(ev.TotalLaps);
            }
            else
            {
                // Spread the updates out across sequential frames for performance reasons
                switch (mRaceUpdateIndex)
                {
                    case 0:
                        mShipDeck.SetRaceTime(Globals.FormatElapsedTime(ev.RaceTime));
                        mShipDeck.SetLapTime(Globals.FormatElapsedTime(ev.LapTime));
                        break;
                    case 1:
                        mShipDeck.SetMph((float)ev.Mph);
                        break;
                    case 2:
                        mShipDeck.SetLap("" + ev.Lap + "/" + ev.TotalLaps);
                        break;
                    default:
                        break;
                }

                if (ev.CrossedFinishLine)
                {
                    if (mRaceUpdateIndex != 1)
                        mShipDeck.SetLapTime(Globals.FormatElapsedTime(ev.LapTime));
                }
            }

            if (++mRaceUpdateIndex > 2)
                mRaceUpdateIndex = 0;
        }

        public override void DamageShip(int damage)
        {
            if (mMotorBoatingSob || Monty != MontyState.FirstMate)
                return;

            Drag = Math.Min(Drag, 0.7f);
            mDragDuration = 3.0f - Potion.MobilityReductionDurationForPotion(GameController.GC.GameStats.PotionForKey(Potion.POTION_MOBILITY));
            ControlsManager.CM.VibrateGamePad(GameController.GC.ProfileManager.MainPlayerIndex, ref mVibrationDamageDescriptor);
        }

        public void EnablePlank(bool enable)
        {
            mPlankEnabled = enable;
            ShipDeck.Plank.State = Plank.PlankState.Inactive;
        }

        public override void ActivateFlyingDutchman(float duration)
        {
            if (!mFlyingDutchman && !mSinking)
                mShipDeck.ActivateFlyingDutchman();
            base.ActivateFlyingDutchman(duration);
        }

        public override void DeactivateFlyingDutchman()
        {
            if (mFlyingDutchman)
                mShipDeck.DeactivateFlyingDutchman();
            base.DeactivateFlyingDutchman();
        }

        public void TravelThroughTime(float duration)
        {
            if (mTimeTravelling)
                return;
            mTimeTravelling = true;

            SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("Alpha", 0);
            mScene.Juggler.AddObject(tween);
        }

        public void EmergeInPresentAt(float x, float y, float duration)
        {
            if (!mTimeTravelling || mBody == null)
                return;
            Vector2 loc = new Vector2(ResManager.P2MX(x), ResManager.P2MY(y));
            mBody.SetTransform(loc, mBody.GetAngle());

            AABB aabb;
            mStern.GetAABB(out aabb, 0);
            Vector2 rudder = aabb.GetCenter();
            X = ResManager.M2PX(rudder.X);
            Y = ResManager.M2PY(rudder.Y);
            Rotation = -B2Rotation;

            SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("Alpha", 1);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnEmergedInPresent);
            mScene.Juggler.AddObject(tween);
        }

        private void OnEmergedInPresent(SPEvent ev)
        {
            mTimeTravelling = false;
        }

        public override void PrepareForGameOver()
        {
            if (mMonty != MontyState.Mutineer)
            {
                base.PrepareForGameOver();
                return;
            }

            if (MarkedForRemoval || mPreparedForGameOver)
                return;

            mScene.RemoveProp(mOffscreenArrow, false);
            mPreparedForGameOver = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mShipDeck != null)
                        {
                            //if (RightCannon != null)
                            //    RightCannon.RemoveEventListener(PlayerCannonFiredEvent.CUST_EVENT_TYPE_PLAYER_CANNON_FIRED, (PlayerCannonFiredEventHandler)OnPlayerCannonFired);
                            if (LeftCannon != null)
                                LeftCannon.RemoveEventListener(PlayerCannonFiredEvent.CUST_EVENT_TYPE_PLAYER_CANNON_FIRED, (PlayerCannonFiredEventHandler)OnPlayerCannonFired);
                            mShipDeck = null;
                        }

                        mMontyDest = null;
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
