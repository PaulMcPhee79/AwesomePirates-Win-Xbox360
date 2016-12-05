using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class EscortShip : PursuitShip
    {
        public EscortShip(ActorDef def, string key)
            : base(def, key)
        {
            mWillEnterTown = false;
            mFleetID = 0;
        }

        #region Fields
        private bool mWillEnterTown;
        private uint mFleetID;
        private PrimeShip mEscortee;
        #endregion

        #region Properties
        public bool WillEnterTown { get { return mWillEnterTown; } set { mWillEnterTown = value; } }
        public uint FleetID { get { return mFleetID; } set { mFleetID = value; } }
        public PrimeShip Escortee
        {
            get { return mEscortee; }
            set
            {
                if (mEscortee == value)
		            return;
	
	            if (mEscortee != null)
                    mEscortee.RemoveEventListener(CUST_EVENT_TYPE_ESCORTEE_DESTROYED, (SPEventHandler)OnEscorteeDestroyed);
	            mEscortee = value;

	            if (value != null)
                {
                    mEscortee.AddEventListener(CUST_EVENT_TYPE_ESCORTEE_DESTROYED, (SPEventHandler)OnEscorteeDestroyed);
                    DuelState = PursuitState.Escorting;
                }
            }
        }
        public override PursuitShip.PursuitState DuelState
        {
            get
            {
                return base.DuelState;
            }
            set
            {
                if (mDestination == null)
                    return;

                PursuitState state = value;
                IsCollidable = true;
	
	            switch (state) {
		            case PursuitState.Searching:
			            state = PursuitState.Escorting;
                        goto case PursuitState.Escorting;
		            case PursuitState.Escorting:
			            if (mEscortee != null)
				            break;
			            state = PursuitState.SailingToDock;
                        goto case PursuitState.SailingToDock;
		            case PursuitState.SailingToDock:
			            if (mEscortee != null && !mEscortee.Docking)
                        {
				            state = PursuitState.Escorting;
			            }
                        else
                        {
				            if (mDestination.SeaLaneC == null)
                            {
					            IsCollidable = !mWillEnterTown;
                                Destination.SetFinishAsDest();
				            }
                            else
                            {
					            mDestination.SeaLaneC = mDestination.SeaLaneC; // Make way to town entrance
				            }
				            mDuelState = state;
				            return; // Exit early
			            }
			            break;
		            default:
			            break; 
	            }
	            base.DuelState = value;
            }
        }
        #endregion

        #region Methods
        public override void SetupShip()
        {
            base.SetupShip();
            mSlowedFraction = 0.5f;
            mDuelState = PursuitState.Escorting; // Don't use property in case mEscortee is not yet set
        }

        public override void Reuse()
        {
            if (InUse)
                return;
            base.Reuse();

            mWillEnterTown = false;
            mFleetID = 0;
        }

        public override void Hibernate()
        {
            if (!InUse)
                return;

            if (mEscortee != null)
            {
                mEscortee.RemoveEventListener(CUST_EVENT_TYPE_ESCORTEE_DESTROYED, (SPEventHandler)OnEscorteeDestroyed);
                mEscortee = null;
            }

            base.Hibernate();
        }

        protected void OnEscorteeDestroyed(SPEvent ev)
        {
	        mEscortee = null;

            if (mEnemy == null && InUse)
                DuelState = PursuitState.SailingToDock;
        }

        public override void PlayerCamouflageActivated(bool activated)
        {
            if (mPursuitEnded)
                return;

            if (activated)
            {
                if (DuelState != PursuitState.Ferrying)
                    DuelState = PursuitState.SailingToDock;
            }
            else
            {
                if (mEnemy != null)
                    DuelState = PursuitState.Chasing;
                else if (DuelState != PursuitState.Ferrying)
                    DuelState = PursuitState.Escorting;
            }
        }

        public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (mDocking || mRemoveMe || mPreparingForNewGame)
                return false;

            return IsCollidable || other == Escortee;
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (other is EscortShip)
            {
                EscortShip ship = other as EscortShip;

                // Prevent circular referencing
                if (IsCollidable && ship.IsCollidable && Avoiding != ship && ship.Avoiding != this && !mInWhirlpoolVortex && !mInDeathsHands)
                {
                    if (!ship.Docking && !ship.MarkedForRemoval && !ship.IsPreparingForNewGame)
                    {
                        if (fixtureOther == ship.Feeler && fixtureSelf != mFeeler)
                        {
                            ship.Avoiding = this;
                            ship.AvoidingState = AvoidState.Decelerating;
                        }
                        else
                        {
                            Avoiding = ship;
                            AvoidingState = AvoidState.Decelerating;
                        }
                    }
                }

                Debug.Assert(!(Avoiding == ship && ship.Avoiding == this), "Avoiding cycle detected in EscortShip");
            }

            base.BeginContact(other, fixtureSelf, fixtureOther, contact);
        }

        protected override void SailWithForce(float force)
        {
            // Slow down when entering the town so that we can enter more orderly
            if (Destination.FinishIsDest && mDestination.Finish == ActorAi.kPlaneIdTown)
                base.SailWithForce(0.75f * force);
            else
                base.SailWithForce(force);
        }

        protected override float Navigate()
        {
            if (mInWhirlpoolVortex || mInDeathsHands)
		        return 0f;
	        float sailForce = 0.0f;
	
	        if (DuelState == PursuitState.Escorting)
            {
		        if (mEscortee == null)
                {
			        DuelState = PursuitState.SailingToDock;
		        }
                else if (mDestination != null && mBody != null)
                {
			        sailForce = mEscortee.SailForce;
			        Vector2 bodyPos = mBody.GetPosition();
			        Vector2 dest = mEscortee.FlankPosition(this);
			        mDestination.Dest = dest;
			        dest = bodyPos - dest;
			
                    float destLenSquared = dest.LengthSquared();
            
                    // 12.0f is optimal distance
                    if (destLenSquared > 24.0f) sailForce *= 1.35f;
			        else if (destLenSquared > 12.05f) sailForce *= 1.15f;
                    else if (destLenSquared < 11.95f) sailForce = Math.Min(0.85f * sailForce, mEscortee.CurrentSailForce);
                    else sailForce = Math.Min(sailForce, mEscortee.CurrentSailForce);
            
			        sailForce *= mDrag;
			
			        SailWithForce(sailForce);

			        // Turn towards destination
			        Vector2 linearVel = mBody.GetLinearVelocity();
			        float angleToTarget = Box2DUtils.SignedAngle(ref dest, ref linearVel);
			
			        if (angleToTarget != 0.0f)
                    {
                        //float escorteeAngleFactor = MIN(1.0f, 0.5f + 0.5f * (fabsf(self.escortee.b2rotation - self.b2rotation) / (PI_HALF / 2)));
				        float turnForce = ((angleToTarget > 0.0f) ? -1.0f : 1.0f) * (mTurnForceMax * (Math.Min(sailForce,mSailForce) / mSailForceMax));
				        TurnWithForce(turnForce);
			        }
		        }
	        }
            else
            {
                sailForce = base.Navigate();
	        }
	        return sailForce;
        }

        public override bool HasBootyGoneWanting(SPSprite shooter)
        {
            return (shooter is PirateShip);
        }

        public override void CreditPlayerSinker()
        {
            if (mScene.GameMode == GameMode.Career)
                mScene.AchievementManager.EscortShipSunk(this);
            else
                base.CreditPlayerSinker();
        }

        public override void SafeRemove()
        {
            if (mRemoveMe)
		        return;
	        base.SafeRemove();
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_ESCORT_DESTROYED));
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mEscortee != null)
                        {
                            mEscortee.RemoveEventListener(CUST_EVENT_TYPE_ESCORTEE_DESTROYED, (SPEventHandler)OnEscorteeDestroyed);
                            mEscortee = null;
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
