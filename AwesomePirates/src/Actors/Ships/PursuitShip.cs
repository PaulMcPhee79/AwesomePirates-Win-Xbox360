using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class PursuitShip : NpcShip, IPursuer
    {
        public enum PursuitState
        {
            Idle = 0,
            Departing,
            Ferrying,
            OutOfBounds,
            Chasing,
            Aiming,
            Strafing,
            Searching,
            Escorting,
            SailingToDock,
            Sinking
        }

        public PursuitShip(ActorDef def, string key)
            : base(def, key)
        {
            mPursuitEnded = false;
            mTracer = new TargetTracer();
        }

        public override void SetupShip()
        {
            base.SetupShip();
            DuelState = PursuitState.Ferrying;
        }

        #region Fields
        protected bool mPursuitEnded;
        protected PursuitState mDuelState;
        protected ShipActor mEnemy;
        protected TargetTracer mTracer;
        #endregion

        #region Properties
        public virtual PursuitState DuelState
        {
            get { return mDuelState; }
            set
            {
                PursuitState oldState = mDuelState;
	            mDuelState = value;
	
	            switch (value)
                {
		            case PursuitState.Idle:
			            break;
		            case PursuitState.Ferrying:
			            break;
		            case PursuitState.OutOfBounds:
			            RequestNewDestination();
			            break;
		            case PursuitState.Chasing:
			            break;
		            case PursuitState.Aiming:
			            break;
		            case PursuitState.Strafing:
			            RequestNewDestination();
			            break;
		            case PursuitState.Searching:
			            if (oldState == PursuitState.SailingToDock)
				            RequestNewDestination();
			            break;
		            case PursuitState.Escorting:
			            break;
		            case PursuitState.SailingToDock:
                        Destination.SetFinishAsDest();
			            break;
		            case PursuitState.Sinking:
			            break;
		            default:
                        throw new ArgumentException("Invalid PursuitState.");
	            }
            }
        }
        public ShipActor Enemy
        { 
            get { return mEnemy; }
            set
            {
                if (mEnemy == value)
		            return;
	
	            if (mEnemy != null)
                {
		            mTracer.Target = null;
                    mEnemy.RemovePursuer(this);
                    mEnemy = null;
	            }
	
	            if (value != null)
                {
		            mTracer.Target = value;
		            mEnemy = value;
                    mEnemy.AddPursuer(this);
	            }
            }
        }
        protected bool IsNavigationDisabled { get { return (mInWhirlpoolVortex || mInDeathsHands || mBody == null); } }
        #endregion

        #region Methods
        public override void Reuse()
        {
            if (InUse)
                return;
            base.Reuse();

            mPursuitEnded = false;

            if (mTracer == null)
                mTracer = new TargetTracer();
        }

        public override void Hibernate()
        {
            if (!InUse)
                return;

            if (mEnemy != null)
            {
                mTracer.Target = null;
                mEnemy.RemovePursuer(this);
                mEnemy = null;
            }

            base.Hibernate();
        }

        public void PursueeDestroyed(ShipActor pursuee)
        {
            if (pursuee != Enemy)
                throw new InvalidOperationException("PursuitShip: Pursuee must also be enemy.");
            Enemy = null;

            if (DuelState != PursuitState.SailingToDock)
                DuelState = PursuitState.Searching;
        }

        public override void Dock()
        {
            Enemy = null;
            base.Dock();
        }

        public override void Sink()
        {
            Enemy = null;
            base.Sink();
        }

        public override void DidReachDestination()
        {
            if (mDuelState == PursuitState.SailingToDock || mDuelState == PursuitState.Searching || mDuelState == PursuitState.Strafing)
                base.DidReachDestination();
	        else if (mDuelState != PursuitState.Chasing && mDuelState != PursuitState.Aiming && mDuelState != PursuitState.Sinking)
		        DuelState = PursuitState.Searching;
        }

        public virtual void RequestNewEnemy()
        {
            mScene.RequestTargetForPursuer(this);
        }

        public virtual void PlayerCamouflageActivated(bool activated) { }

        public virtual void EndPursuit()
        {
            if (mPursuitEnded)
                return;
            mPursuitEnded = true;
            DuelState = PursuitState.SailingToDock;
        }

        public override void NegotiateTarget(ShipActor target)
        {
            if (mInWhirlpoolVortex || target == null || mBody == null)
		        return;
	        Vector2 bodyPos = mBody.GetPosition();
	        Vector2 enemyPos = target.B2Body.GetPosition();
	        Vector2 dest = bodyPos - enemyPos;
	
	        Vector2 linearVel = mBody.GetLinearVelocity();
	        float angleToTarget = Box2DUtils.SignedAngle(ref dest, ref linearVel);
	
	        int angleInDegrees = (int)SPMacros.SP_R2D(angleToTarget);
	
	        if (Math.Abs(angleInDegrees) > 87 && Math.Abs(angleInDegrees) < 93)
            {
		        Cannonball cannonball = FireCannon(((angleInDegrees > 0) ? AwesomePirates.ShipDetails.ShipSide.Port : AwesomePirates.ShipDetails.ShipSide.Starboard), 1f);
                if (cannonball != null)
                {
                    cannonball.CalculateTrajectoryFrom(target.X, target.Y);
                    cannonball.B2Body.SetLinearVelocity(cannonball.B2Body.GetLinearVelocity() + mTracer.TargetVel);
                }
		        DuelState = PursuitState.Strafing;
	        }
        }

        protected override float Navigate()
        {
            if (IsNavigationDisabled)
		        return 0;

	        float sailForce = mDrag * mSailForce;
	
	        // TODO: when an escort ship is attacking the player and another Silver Train is hit by this escort's cannonball, body can be zero...
	        if (mEnemy != null && mEnemy.B2Body == null)
            {
		        Enemy = null;
		        DuelState = PursuitState.Searching;
		        return base.Navigate();
	        }
	
	        if (!mReloading && mDuelState != PursuitState.Ferrying && mDuelState != PursuitState.Departing && mDuelState != PursuitState.SailingToDock)
		        NegotiateTarget(mEnemy); // We want to shoot if we're strafing and our target passes into our shooting window
	
	        switch (mDuelState)
            {
		        case PursuitState.Idle:
			        sailForce /= 3.0f;
			        SailWithForce(sailForce);
			        break;
		        case PursuitState.Ferrying:
			        sailForce = base.Navigate();
			        break;
		        case PursuitState.OutOfBounds:
			        sailForce = base.Navigate();
			        break;
		        case PursuitState.Chasing:
		        {
			        if (mEnemy == null)
                    {
				        DuelState = PursuitState.Searching;
			        }
                    else
                    {
				        Vector2 enemyPos = mEnemy.B2Body.GetPosition();
				        mDestination.Dest = enemyPos;
				        Vector2 dist = enemyPos - mBody.GetPosition();
			
				        if (Math.Abs(dist.X) + Math.Abs(dist.Y) < 25.0f && !mReloading) // In meters
					        DuelState = PursuitState.Aiming;
			        }
			        sailForce = base.Navigate();
			        break;
		        }
		        case PursuitState.Aiming:
		        {
			        if (mEnemy == null)
                    {
				        DuelState = PursuitState.Searching;
			        }
                    else
                    {
				        SailWithForce(sailForce);
				
				        Vector2 bodyPos = mBody.GetPosition();
				        Vector2 enemyPos = mEnemy.B2Body.GetPosition();
				        Vector2 dest = enemyPos - bodyPos;
				
				        Vector2 linearVel = mBody.GetLinearVelocity();
				        float angleToTarget = Box2DUtils.SignedAngle(ref dest, ref linearVel);
				
				        int angleInDegrees = (int)SPMacros.SP_R2D(angleToTarget);
				
				        if ((angleInDegrees > -89 && angleInDegrees < 89) || angleInDegrees > 91 || angleInDegrees < -91)
                        {
					        float turnForce = 0f;
					
					        if (angleInDegrees >= 0)
						        turnForce = ((angleInDegrees < 90) ? -1.0f : 1.0f) * (mTurnForceMax * (sailForce / mSailForceMax));
					        else
						        turnForce = ((angleInDegrees < -90) ? -1.0f : 1.0f) * (mTurnForceMax * (sailForce / mSailForceMax));
					        TurnWithForce(turnForce);
				        }
                        else if (Math.Abs(dest.X) + Math.Abs(dest.Y) > 35.0f) // In meters
                        {
					        DuelState = PursuitState.Chasing;
				        }
			        }
			        break;
		        }
		        case PursuitState.Strafing:
		        {
			        if (mEnemy == null)
                    {
				        DuelState = PursuitState.Searching;
				        SailWithForce(sailForce);
			        }
                    else if (!mReloading)
                    {
				        Vector2 bodyPos = mBody.GetPosition();
				        Vector2 dest = mEnemy.B2Body.GetPosition() - bodyPos;
					
				        if ((Math.Abs(dest.X) + Math.Abs(dest.Y)) > 30.0f) // In meters
					        DuelState = PursuitState.Chasing;
				        else
					        DuelState = PursuitState.Aiming;
				        SailWithForce(sailForce);
			        }
                    else
                    {
				        sailForce = base.Navigate();
			        }
			        break;
		        }
		        case PursuitState.Searching:
			        sailForce = base.Navigate();
			        break;
		        case PursuitState.Escorting:
			        sailForce = base.Navigate();
			        break;
		        case PursuitState.SailingToDock:
                    sailForce = base.Navigate();
			        break;
		        case PursuitState.Sinking:
		        default:
			        sailForce = 0f;
			        break;
	        }
	        return sailForce;
        }

        public override void AdvanceTime(double time)
        {
            base.AdvanceTime(time);

            switch (mDuelState) {
		        case PursuitState.Searching:
                    if (!mReloading)
                    {
                        if (mEnemy == null)
                            RequestNewEnemy();
                        else
                            DuelState = PursuitState.Chasing;
                    }
			        break;
		        default:
			        break;
	        }
	
	        if (mRemoveMe || mDocking)
		        return;
            mTracer.AdvanceTime(time);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mEnemy != null)
                        {
                            mTracer.Target = null;
                            mEnemy.RemovePursuer(this);
                            mEnemy = null;
                            mTracer = null;
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
