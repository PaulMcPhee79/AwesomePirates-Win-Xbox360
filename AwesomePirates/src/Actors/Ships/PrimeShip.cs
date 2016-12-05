using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class PrimeShip : NpcShip
    {
        public PrimeShip(ActorDef def, string key)
            : base(def, key)
        {
            mLaunching = true;
            mFleetID = 0;
            mCurrentSailForce = 0;
            mTrailIndex = -1;
            mTrailInit = 0;
            mTrailIndexCount = (int)(GameController.GC.Fps / 2);
            mTrailLeft = new Vector2[mTrailIndexCount];
            mTrailRight = new Vector2[mTrailIndexCount];
        }

        #region Fields
        private bool mLaunching;
        private uint mFleetID;
        private float mCurrentSailForce;
	    private EscortShip mLeftEscort;
	    private EscortShip mRightEscort;

        private int mTrailInit;
        private int mTrailIndex;
        private int mTrailIndexCount;
        private Vector2[] mTrailLeft;
        private Vector2[] mTrailRight;
        #endregion

        #region Properties
        private int FlankIndex { get { return (mTrailIndex + 1) % mTrailIndexCount; } }
        public uint FleetID
        {
            get { return mFleetID; }
            set
            {
                mFleetID = value;
                mLeftEscort.FleetID = value;
                mRightEscort.FleetID = value;
            }
        }
        public EscortShip LeftEscort
        {
            get { return mLeftEscort; }
            set { SetupEscortAsFlank(value, ref mLeftEscort); }
        }
        public EscortShip RightEscort
        {
            get { return mRightEscort; }
            set { SetupEscortAsFlank(value, ref mRightEscort); }
        }
        public float CurrentSailForce { get { return mCurrentSailForce; } } // TODO
        #endregion

        #region Methods
        public override void Reuse()
        {
            if (InUse)
                return;
            base.Reuse();

            mLaunching = true;
            mFleetID = 0;
            mCurrentSailForce = 0;
            mTrailIndex = -1;
            mTrailInit = 0;

            if (mTrailLeft == null || mTrailRight == null)
            {
                mTrailIndexCount = (int)(GameController.GC.Fps / 2);
                mTrailLeft = new Vector2[mTrailIndexCount];
                mTrailRight = new Vector2[mTrailIndexCount];
            }
        }

        public override void Hibernate()
        {
            if (!InUse)
                return;

            if (mLeftEscort != null)
            {
                mLeftEscort.RemoveEventListener(CUST_EVENT_TYPE_ESCORT_DESTROYED, (SPEventHandler)OnEscortDestroyed);
                mLeftEscort = null;
            }
            if (mRightEscort != null)
            {
                mRightEscort.RemoveEventListener(CUST_EVENT_TYPE_ESCORT_DESTROYED, (SPEventHandler)OnEscortDestroyed);
                mRightEscort = null;
            }

            base.Hibernate();
        }

        protected void SetupTrail()
        {
            mTrailIndex = mTrailIndexCount - 1;
        }

        private void SetupEscortAsFlank(EscortShip ship, ref EscortShip flank)
        {
            if (flank == ship)
		        return;
	
	        if (flank != null)
                flank.RemoveEventListener(CUST_EVENT_TYPE_ESCORT_DESTROYED, (SPEventHandler)OnEscortDestroyed);
	        
            flank = ship;

            if (flank != null)
                flank.AddEventListener(CUST_EVENT_TYPE_ESCORT_DESTROYED, (SPEventHandler)OnEscortDestroyed);
        }

        public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            return fixtureSelf != mFeeler;
        }

        protected void OnEscortDestroyed(SPEvent ev)
        {
            EscortShip ship = ev.CurrentTarget as EscortShip;
	
	        if (ship == mLeftEscort)
		        mLeftEscort = null;
	        else if (ship == mRightEscort)
		        mRightEscort = null;
        }

        public Vector2 FlankPosition(EscortShip ship)
        {
            Vector2 flankPos;
    
            if (mTrailInit < mTrailIndexCount)
                flankPos = CalcFlankPosForAngle(((ship == mLeftEscort) ? -SPMacros.PI_HALF : SPMacros.PI_HALF));
            else
                flankPos = (ship == mLeftEscort) ? mTrailLeft[FlankIndex] : mTrailRight[FlankIndex];
    
            return flankPos;
        }

        protected Vector2 CalcFlankPosForAngle(float angle)
        {
            if (mBody == null)
                return Vector2.Zero;
    
            Vector2 primePos = mBody.GetPosition();
            AABB aabb;
            mStern.GetAABB(out aabb, 0);
            Vector2 sternCenter = aabb.GetCenter();
            mBow.GetAABB(out aabb, 0);
            Vector2 bowCenter = aabb.GetCenter();
    
            Vector2 flankPos = sternCenter - bowCenter;
            flankPos *= 0.75f;
            Box2DUtils.RotateVector(ref flankPos, angle);
            flankPos = primePos + flankPos;
            return flankPos;
        }

        protected override float Navigate()
        {
            if (mTrailIndex == -1)
                SetupTrail();
    
            // Drag when first launched so that escort ships can more easily match our speed
            if (mLaunching && mBody != null)
            {
		        Vector2 bodyPos = mBody.GetPosition();
		        Vector2 spawnPoint = mDestination.Loc;
		        Vector2 dist = bodyPos - spawnPoint;
        
		        if (dist.LengthSquared() < (ResManager.P2M(16) * ResManager.P2M(16)))
                {
			        mSailForce = Math.Min(mSailForce, 0.5f * mSailForceMax);
                }
                else
                {
                    if (AvoidingState == AvoidState.None)
                        mSailForce = mSailForceMax;
                    mLaunching = false;
                }
	        }
    
            mCurrentSailForce = base.Navigate();
    
            if (mBody != null)
            {
                if (LeftEscort != null)
                    mTrailLeft[mTrailIndex] = CalcFlankPosForAngle(-SPMacros.PI_HALF);
        
                if (RightEscort != null)
                    mTrailRight[mTrailIndex] = CalcFlankPosForAngle(SPMacros.PI_HALF);

                if (mTrailInit < mTrailIndexCount)
                    ++mTrailInit;
        
                ++mTrailIndex;
        
                if (mTrailIndex >= mTrailIndexCount)
                    mTrailIndex = 0;
            }
    
            return mCurrentSailForce;
        }

        public override void DamageShipWithCannonball(Cannonball cannonball)
        {
            if (IsCollidable && cannonball.Shooter is ShipActor)
            {
		        if (cannonball.Shooter is PlayerShip)
                {
			        PlayerShip playerShip = cannonball.Shooter as PlayerShip;
			
			        if (playerShip.IsCamouflaged)
                    {
                        base.DamageShipWithCannonball(cannonball);
				        return;
			        }
		        }
		
		        if (mLeftEscort != null)
                {
			        mLeftEscort.Enemy = cannonball.Shooter as ShipActor;
			        mLeftEscort.DuelState = PursuitShip.PursuitState.Chasing;
		        }
		        if (mRightEscort != null)
                {
			        mRightEscort.Enemy = cannonball.Shooter as ShipActor;
                    mRightEscort.DuelState = PursuitShip.PursuitState.Chasing;
		        }
	        }

            base.DamageShipWithCannonball(cannonball);
        }

        public override bool HasBootyGoneWanting(SPSprite shooter)
        {
            return (shooter is PirateShip);
        }

        public override void SafeRemove()
        {
            if (mRemoveMe)
                return;
            base.SafeRemove();
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_ESCORTEE_DESTROYED));
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mLeftEscort != null)
                        {
                            mLeftEscort.RemoveEventListener(CUST_EVENT_TYPE_ESCORT_DESTROYED, (SPEventHandler)OnEscortDestroyed);
                            mLeftEscort = null;
                        }

                        if (mRightEscort != null)
                        {
                            mRightEscort.RemoveEventListener(CUST_EVENT_TYPE_ESCORT_DESTROYED, (SPEventHandler)OnEscortDestroyed);
                            mRightEscort = null;
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
