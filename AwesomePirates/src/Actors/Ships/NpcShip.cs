using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class NpcShip : ShipActor, IInfamous, IPathFollower, IResourceClient
    {
        public enum AvoidState
        {
            None = 0,
            Decelerating,
            Slowed,
            Accelerating
        }

        public const string CUST_EVENT_TYPE_ESCORTEE_DESTROYED = "escorteeDestroyedEvent";
        public const string CUST_EVENT_TYPE_ESCORT_DESTROYED = "escortDestroyedEvent";

        public const float kSpeedRatingMax = 10f;
        public const float kControlRatingMax = 10f;
        public const double kDefaultReloadDelay = 4f;

        public NpcShip(ActorDef def, string key)
            : base(def, key)
        {
            mCategory = (int)PFCat.NPC_SHIPS;
            mIsCollidable = true;
		    mHasLeftPort = false;
		    mDocking = false;
		    mReloading = false;
		    mInWhirlpoolVortex = false;
		    mInDeathsHands = false;
		    mBootyGoneWanting = true;
		    mInFuture = false;
		    mAiModifier = 1;
		    mWhirlpoolOverboardDelay = 2.0;
		    mReloadInterval = kDefaultReloadDelay;
            mReloadTimer = 0.0;
            mSinkingTimer = 0.0;
		    mDestination = null;
		    mAvoidState = AvoidState.None;
		    mAvoidAccel = 0.0f;
		    mSlowedFraction = 0.25f;
		    mAngVelUpright = 0.4f;
		    mAvoiding = null;
            mResources = null;
            CheckoutPooledResources();
        }

        #region Fields
        protected bool mIsCollidable;
        protected bool mHasLeftPort;
        protected bool mDocking;
        protected bool mReloading;
        protected bool mInWhirlpoolVortex;
        protected bool mInDeathsHands;
        protected bool mBootyGoneWanting;
        protected bool mInFuture;
        protected float mAiModifier;

        protected double mReloadInterval;
        protected double mReloadTimer;
        protected double mWhirlpoolOverboardDelay;
        protected double mSinkingTimer;
        protected Destination mDestination;

        protected Fixture mFeeler; // Collision Avoidance Detector
        protected Fixture mHitBox; // What the player's cannon raytraces are tested against
        protected AvoidState mAvoidState;
        protected float mAvoidAccel;
        protected float mSlowedFraction;
        protected NpcShip mAvoiding;

        private ResourceServer mResources;
        #endregion

        #region Properties
        public bool IsCollidable { get { return mIsCollidable; } set { mIsCollidable = value; } }
        public bool InWhirlpoolVortex
        {
            get { return mInWhirlpoolVortex; }
            set
            {
                if (value)
                    AvoidingState = AvoidState.None;
                mInWhirlpoolVortex = value;
            }
        }
        public bool InDeathsHands
        {
            get { return mInDeathsHands; }
            set
            {
                if (value && !mInWhirlpoolVortex && mBody != null)
                {
                    mBody.SetLinearVelocity(Vector2.Zero);
                    mBody.SetAngularVelocity(0);
                }

                AvoidingState = AvoidState.None;
                mInDeathsHands = value;
            }
        }
        public bool InFuture { get { return mInFuture; } set { mInFuture = value; } }
        public float AiModifier { get { return mAiModifier; } set { mAiModifier = value; RecalculateForces(); } }
        public Destination Destination { get { return mDestination; } set { mDestination = value; } }
        public Fixture Feeler { get { return mFeeler; } set { mFeeler = value; } }
        public Fixture HitBox { get { return mHitBox; } set { mHitBox = value; } }
        public AvoidState AvoidingState
        {
            get { return mAvoidState; }
            set
            {
                switch (value)
                {
                    case AvoidState.None:
                        Avoiding = null;
                        mSailForce = mSailForceMax;
                        mAvoidAccel = 0.0f;
                        break;
                    case AvoidState.Decelerating:
                        mAvoidAccel = -mSailForceMax / 100.0f;
                        break;
                    case AvoidState.Slowed:
                        mSailForce = 0.25f * mSailForceMax;
                        mAvoidAccel = 0.0f;
                        break;
                    case AvoidState.Accelerating:
                        mSailForce = 0.25f * mSailForceMax;
                        mAvoidAccel = mSailForceMax / 200.0f;
                        break;
                }

                mAvoidState = value;
            }
        }
        public NpcShip Avoiding { get { return mAvoiding; } set { mAvoiding = value; } }
        public bool Docking { get { return mDocking; } }
        public SKTeamIndex SinkerID { get; set; }
        public override int InfamyBonus { get { return mShipDetails.InfamyBonus; } }
        protected bool IsOutOfBounds { get { return (X < -300.0f || X > mScene.ViewWidth + 300.0f || Y < -300.0f || Y > mScene.ViewHeight + 300.0f); } }
        #endregion

        #region Methods
        public override void SetupShip()
        {
            //if (mWakeCount == -1)
		    //    mWakeCount = (int)Math.Min(Wake.MaxWakeBufferSize, 3f * mShipDetails.SpeedRating * mAiModifier);
            base.SetupShip();
            RecalculateForces();

            if (mWake != null)
                mWake.RipplePeriod = Math.Min(Wake.MaxRipplePeriod, Wake.DefaultRipplePeriod * Math.Max(Wake.MinRipplePeriod, ShipActor.DefaultSailForceMax / Math.Max(1, mSailForceMax)));
            mReloadInterval = (double)mShipDetails.ReloadInterval;

            string textureName = null;

            if (mInFuture && mShipDetails.TextureFutureName != null)
            {
		        textureName = mShipDetails.TextureFutureName;
                mCostumeImages = null;
	        }
            else
            {
		        textureName = mShipDetails.TextureName;
            }

            if (mCostume == null)
                mCostume = new SPSprite();

            if (mWardrobe == null)
                mWardrobe = new SPSprite();
            mWardrobe.Alpha = 1f;
            mWardrobe.ScaleX = mWardrobe.ScaleY = 1;
            mWardrobe.AddChild(mCostume);
            AddChild(mWardrobe);

            if (mCostumeImages == null)
                mCostumeImages = SetupCostumeForTexturesStartingWith(textureName, true);
            EnqueueCostumeImages(mCostumeImages);
            UpdatePositionOrientation();
        }

        public override void Reuse()
        {
            if (InUse)
                return;
            base.Reuse();

            mIsCollidable = true;
            mHasLeftPort = false;
            mDocking = false;
            mReloading = false;
            mInWhirlpoolVortex = false;
            mInDeathsHands = false;
            mBootyGoneWanting = true;
            mInFuture = false;
            mAiModifier = 1;
            mWhirlpoolOverboardDelay = 2.0;
            mReloadInterval = kDefaultReloadDelay;
            mReloadTimer = 0.0;
            mSinkingTimer = 0.0;
            mDestination = null;
            mAvoidState = AvoidState.None;
            mAvoidAccel = 0.0f;
            mSlowedFraction = 0.25f;
            mAngVelUpright = 0.4f;
            mAvoiding = null;
            mResources = null;
            CheckoutPooledResources();
        }

        public override void Hibernate()
        {
            if (!InUse)
                return;

            if (mDestination != null)
            {
                if (mDestination.PoolIndex != -1)
                    mDestination.Hibernate();
                mDestination = null;
            }

            mAvoiding = null;
            CheckinPooledResources();

            base.Hibernate();
        }

        public virtual void NegotiateTarget(ShipActor target) { }

        protected override void SaveFixture(Fixture fixture, int index)
        {
            switch (index)
            {
                case 5: mHitBox = fixture; break;
                case 6: mFeeler = fixture; break;
            }
            base.SaveFixture(fixture, index);
        }

        public virtual void RecalculateForces()
        {
            if (mBody == null)
		        return;
            float sailForceTweak = 1.35f, turnForceTweak = 1.1f;
            float speedRatingMax = kSpeedRatingMax;
            float controlRatingMax = kControlRatingMax;
	        mSailForceMax = mSpeedModifier * sailForceTweak * 2.0f * mBody.GetMass() * Math.Min(speedRatingMax, mShipDetails.SpeedRating * mAiModifier);
	        mSailForce = mSailForceMax;
            mTurnForceMax = mControlModifier * turnForceTweak * mBody.GetMass() * Math.Min(controlRatingMax, mShipDetails.ControlRating * mAiModifier) * SPMacros.PI_HALF / 2.0f / 3.0f;
            mTurnForceMax *= 1.4f;  // 1.4f because ships are 50% larger since v2.0
        }

        protected virtual void AvoidCollisions()
        {
            // PreSolve can return false even though an Avoiding has been set. Subsequently, BeginContact and
            // EndContact never gets called and the Avoidance is not removed, as long as the only contact ever
            // made is between both Feeler fixtures (rare). We make up for this situation here, although it requires
            // our Avoiding ship to sink in some manner.
            if (Avoiding != null && (Avoiding.Docking || Avoiding.MarkedForRemoval || Avoiding.IsPreparingForNewGame))
                Avoiding = null;

            mSailForce += mAvoidAccel;

            if (mAvoidState == AvoidState.None)
                return;
            else if (Avoiding == null && mAvoidState != AvoidState.Accelerating)
                AvoidingState = AvoidState.Accelerating;
            else if (mAvoidState == AvoidState.Decelerating && (mSailForce <= mSlowedFraction * mSailForceMax))
                AvoidingState = AvoidState.Slowed;
            else if (mAvoidState == AvoidState.Accelerating && mSailForce >= mSailForceMax)
                AvoidingState = AvoidState.None;
        }

        public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (mDocking || mRemoveMe || mPreparingForNewGame)
                return false;

            return mIsCollidable;

            // Commented out: Feeler fixture is now a sensor, so it won't fire a PreSolve.
            /*
            bool collidable = true;

            if (other is NpcShip)
            {
                NpcShip ship = other as NpcShip;
                collidable = mIsCollidable;

                if (Avoiding == null && ship.Avoiding == this && fixtureSelf == mFeeler && fixtureOther != ship.Feeler)
                {
                    if (!ship.Docking && !ship.MarkedForRemoval && !ship.IsPreparingForNewGame)
                    {
                        // Switch avoidance roles
                        Avoiding = ship;
                        AvoidingState = AvoidState.Decelerating;
                        ship.Avoiding = null;
                    }
                }
            }

            return (collidable && fixtureSelf != mFeeler);
            */
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (other is NpcShip)
            {
                NpcShip ship = other as NpcShip;
        
                do
                {
                    if ((Avoiding == null || ship.Avoiding == null) && !mInWhirlpoolVortex && !mInDeathsHands && mIsCollidable && ship.IsCollidable)
                    {
                        if (Avoiding != ship && ship.Avoiding != this)
                        {
                            if (this is PursuitShip)
                            {
                                if (this is EscortShip)
                                {
                                    EscortShip escortShip = this as EscortShip;
                            
                                    if (escortShip.Escortee != null)
                                        break;
                                }
                        
                                if (ship is PursuitShip)
                                {
                                    if (ship is EscortShip)
                                    {
                                        EscortShip escortShip = ship as EscortShip;
                                
                                        if (escortShip.Escortee != null)
                                        {
                                            if (fixtureSelf == mFeeler && escortShip.Escortee.Avoiding != this)
                                            {
                                                Avoiding = ship;
                                                AvoidingState = AvoidState.Decelerating;
                                            }
                                            break;
                                        }
                                    }
                            
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
                            else
                            {
                                if (this is PrimeShip)
                                {
                                    PrimeShip primeShip = this as PrimeShip;
                            
                                    if (ship == primeShip.LeftEscort || ship == primeShip.RightEscort || ship.Avoiding == primeShip.LeftEscort || ship.Avoiding == primeShip.RightEscort)
                                        break;
                                }
                        
                                if (ship is PursuitShip)
                                {
                                    if (fixtureSelf == mFeeler)
                                    {
                                        if (ship is EscortShip)
                                        {
                                            EscortShip escortShip = ship as EscortShip;
                                    
                                            if (escortShip.Escortee == null || escortShip.Escortee.Avoiding != this) {
                                                Avoiding = ship;
                                                AvoidingState = AvoidState.Decelerating;
                                            }
                                        }
                                        else
                                        {
                                            Avoiding = ship;
                                            AvoidingState = AvoidState.Decelerating;
                                        }
                                    }
                                }
                                else if (fixtureOther != ship.Feeler && fixtureSelf == mFeeler)
                                {
                                    Avoiding = ship;
                                    AvoidingState = AvoidState.Decelerating;
                                }
                                else if (fixtureOther == ship.Feeler && fixtureSelf != mFeeler)
                                {
                                    ship.Avoiding = this;
                                    ship.AvoidingState = AvoidState.Decelerating;
                                }
                                else if (ship.AvoidingState != AvoidState.None)
                                {
                                    ship.Avoiding = this;
                                    ship.AvoidingState = AvoidState.Decelerating;
                                }
                                else if (AvoidingState != AvoidState.None)
                                {
                                    Avoiding = ship;
                                    AvoidingState = AvoidState.Decelerating;
                                }
                                else
                                {
                                    Avoiding = ship;
                                    AvoidingState = AvoidState.Decelerating;
                                }
                            }
                        }
                        else if (fixtureSelf == mFeeler && fixtureOther != ship.Feeler && Avoiding == null && ship.Avoiding == this
                                && ((this is PursuitShip == false && this is PrimeShip == false)
                                || (this is PursuitShip && ship is PursuitShip)))
                        {
                            if (!ship.Docking && !ship.MarkedForRemoval && !ship.IsPreparingForNewGame)
                            {
                                // Switch avoidance roles
                                Avoiding = ship;
                                AvoidingState = AvoidState.Decelerating;
                                ship.Avoiding = null;
                            }
                        }
                    }
                } while (false);
            }

            base.BeginContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            base.EndContact(other, fixtureSelf, fixtureOther, contact);

            if (RemovedContact)
            {
                if (other is NpcShip)
                {
                    NpcShip ship = other as NpcShip;
                    if (ship == Avoiding)
                        Avoiding = null;
                    if (ship.Avoiding != null && ship.Avoiding == this)
                        ship.Avoiding = null;
                }
            }
        }

        public virtual void Dock()
        {
            DockOverTime(0.5f);
        }

        protected virtual void DockOverTime(float duration)
        {
            if (mDocking)
		        return;
            RemoveAllPursuers();
	        mDocking = true;
	        mIsCollidable = false;
	        //mLantern.visible = false;
	
	        if (mBootyGoneWanting && !mScene.RaceEnabled)
                GameController.GC.ThisTurn.AddMutiny(1);

            if (mResources == null || !mResources.StartTweenForKey(NpcShipCache.RESOURCE_KEY_NPC_DOCK_TWEEN))
            { 
                SPTween tween = new SPTween(mWardrobe, duration);
                tween.AnimateProperty("Alpha", 0f);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)delegate(SPEvent ev) { DockingComplete(); }, true);
                mScene.Juggler.AddObject(tween);
            }
        }

        public override void Sink()
        {
            if (mDocking)
		        return;
            RemoveAllPursuers();
    
	        base.Sink();
	        mDocking = true;
	
	        if ((mDeathBitmap & (DeathBitmaps.BRANDY_SLICK | DeathBitmaps.ACID_POOL | DeathBitmaps.MAGMA_POOL | DeathBitmaps.SEA_OF_LAVA)) != 0)
            {
                mSinkingClip.Pause();
                Burn();
        
                if ((mDeathBitmap & DeathBitmaps.SEA_OF_LAVA) != 0)
                    SpawnMagmaPool();
	        }
            else
            {
                if ((mDeathBitmap & DeathBitmaps.GHOSTLY_TEMPEST) != 0)
                    mDeathCostume.Visible = false;
                ProceedToSink();
	        }
	
	        if (mBootyGoneWanting && !mScene.RaceEnabled && ((mDeathBitmap & DeathBitmaps.NPC_CANNON) != 0) && mScene.GameMode == GameMode.Career)
                GameController.GC.ThisTurn.AddMutiny(1);
	
            mWardrobe.RemoveChild(mCostume);
            mSinkingTimer = mSinkingClip.Duration;
        }

        public override void Burn()
        {
            base.Burn();
            mWardrobe.Alpha = 0f;
	
            if (mResources == null || !mResources.StartTweenForKey(NpcShipCache.RESOURCE_KEY_NPC_BURN_IN_TWEEN))
            {
                SPTween tween = new SPTween(mWardrobe, mBurningClip.Duration / 2, SPTransitions.SPEaseOut);
                tween.AnimateProperty("Alpha", 1f);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)delegate(SPEvent ev) { BurnOut(); }, true);
                mScene.Juggler.AddObject(tween);
            }
        }

        protected virtual void ProceedToSink()
        {
            if (mBody != null)
		        mBody.SetLinearVelocity(Vector2.Zero);
	        mIsCollidable = false;
            mSinkingClip.Play();
            PlaySunkSound();

	        if (mDeathBitmap != 0 && (mDeathBitmap & (DeathBitmaps.NPC_CANNON | DeathBitmaps.TOWN_CANNON)) == 0 && !mScene.RaceEnabled)
		        CreditPlayerSinker();
            if (mDeathBitmap != 0 && (mDeathBitmap & DeathBitmaps.NPC_MASK) == 0)
                DropLoot();
        }

        public virtual void ShrinkOverTime(float duration)
        {
            //if (duration < 0)
	        //	duration = mSinkingClip.duration;
    
            // Ignore duration so that caching tweens works
	
            if (mResources == null || !mResources.StartTweenForKey(NpcShipCache.RESOURCE_KEY_NPC_SHRINK_TWEEN))
            {
                SPTween tween = new SPTween(mWardrobe, 1);
                tween.AnimateProperty("ScaleX", 0.001f);
                tween.AnimateProperty("ScaleY", 0.001f);
                mScene.Juggler.AddObject(tween);
            }
        }

        public override Cannonball FireCannon(ShipDetails.ShipSide side, float trajectory)
        {
            mReloading = true;
            mReloadTimer = mReloadInterval;
            return base.FireCannon(side, trajectory);
        }

        public override void PlayFireCannonSound()
        {
            mScene.PlaySound("NpcCannon");
        }

        protected void Reload()
        {
            mReloading = false;
        }

        public override void DamageShipWithCannonball(Cannonball cannonball)
        {
            base.DamageShipWithCannonball(cannonball);

            if (!mRemoveMe && !mDocking && cannonball != null && cannonball.Shooter != null)
			{
				if (cannonball.Shooter is PlayableShip)
                {
					mDeathBitmap = DeathBitmaps.PLAYER_CANNON;
					mBootyGoneWanting = false;
					Sink();
				}
                else
                {
					if (cannonball.Shooter is NpcShip)
						mDeathBitmap = DeathBitmaps.NPC_CANNON;
					else if (cannonball.Shooter is TownCannon)
						mDeathBitmap = DeathBitmaps.TOWN_CANNON;
					mBootyGoneWanting = HasBootyGoneWanting(cannonball.Shooter);
                    Sink();
				}
			}
        }

        public virtual bool HasBootyGoneWanting(SPSprite shooter)
        {
            return false;
        }

        public void ThrowCrewOverboard(int count)
        {
            if (mBody == null)
                return;

            for (int i = 0; i < count; ++i)
            {
                mScene.PrisonerOverboard(null, this);
                mOverboard = (mOverboard == mStern) ? mBow : mStern; // Spread multiple prisoners out in the water
            }
            mOverboard = mStern;
        }

        public void SpawnAcidPool()
        {
            if (mBody != null)
            {
		        Vector2 bodyPos = mBody.GetPosition();
                string visualStyle = (mScene.GameMode == GameMode.Career) ? PoolActor.kPoolVisualStyleAcid : PoolActor.kPoolVisualStylesSK[(int)SinkerID];
                AcidPoolActor acidPool = AcidPoolActor.CreateAcidPoolActor(bodyPos.X, bodyPos.Y, Globals.ASH_DURATION_ACID_POOL, visualStyle);
                acidPool.SinkerID = SinkerID;
                mScene.AddActor(acidPool);
            }
        }

        public void SpawnMagmaPool()
        {
            if (mBody != null)
            {
		        Vector2 bodyPos = mBody.GetPosition();
                string visualStyle = (mScene.GameMode == GameMode.Career) ? PoolActor.kPoolVisualStyleMagma : PoolActor.kPoolVisualStylesSK[(int)SinkerID];
                MagmaPoolActor magmaPool = MagmaPoolActor.CreateMagmaPoolActor(bodyPos.X, bodyPos.Y,
                    (mScene.GameMode == GameMode.Career)
                    ? Globals.ASH_DURATION_MAGMA_POOL * Potion.PotencyDurationFactorForPotion(mScene.PotionForKey(Potion.POTION_POTENCY))
                    : Globals.ASH_DURATION_MAGMA_POOL,
                    visualStyle);
                magmaPool.SinkerID = SinkerID;
                mScene.AddActor(magmaPool);
	        }
        }

        public virtual void CreditPlayerSinker()
        {
            mScene.SKManager.EnemyShipSunk(this, SinkerID);
        }

        public virtual void DidLeavePort()
        {
            if (mHasLeftPort)
		        return;
            mScene.ActorDepartedPort(this);
	        mHasLeftPort = true;
        }

        public virtual void DidReachDestination()
        {
            RequestNewDestination();
        }

        public virtual void RequestNewDestination()
        {
            mScene.ActorArrivedAtDestination(this);
        }

        protected virtual float Navigate()
        {
            if (mInWhirlpoolVortex || mInDeathsHands || mBody == null)
		        return 0f;
	        float sailForce = mDrag * mSailForce;
            SailWithForce(sailForce);
	
	        if (mDestination != null)
            {
		        Vector2 bodyPos = mBody.GetPosition();
		        Vector2 dest = mDestination.Dest;
		        dest -= bodyPos;
		
		        if (!mHasLeftPort)
                {
			        Vector2 distTravelled = bodyPos - mDestination.Loc;
			
			        if ((Math.Abs(distTravelled.X) + Math.Abs(distTravelled.Y)) > 20.0f) // In meters
				        DidLeavePort();
		        }
		
		        if (Math.Abs(dest.X) < 2.0f && Math.Abs(dest.Y) < 2.0f)
                {			
			        // Signal ship's arrival at destination
			        DidReachDestination();
		        }
                else
                {
			        // Turn towards destination
			        Vector2 linearVel = mBody.GetLinearVelocity();
			        float angleToTarget = Box2DUtils.SignedAngle(ref dest, ref linearVel);
			
			        if (angleToTarget != 0.0f)
                    {
				        float turnForce = mDrag * ((angleToTarget > 0.0f) ? 2.0f : -2.0f) * (mTurnForceMax * (sailForce / mSailForceMax));
                        TurnWithForce(turnForce);
			        }
		        }
	        }
	        return sailForce;
        }

        protected virtual void UpdatePositionOrientation()
        {
            if (mBody == null)
                return;
            // Ship position/orientation
            AABB aabb;
            mStern.GetAABB(out aabb, 0);
            Vector2 rudder = aabb.GetCenter();
            X = ResManager.M2PX(rudder.X);
            Y = ResManager.M2PY(rudder.Y);
            Rotation = -B2Rotation;
        }

        public override void AdvanceTime(double time)
        {
        	if (mRemoveMe || mDocking || mBody == null)
            {
		        if (!mRemoveMe && mInWhirlpoolVortex)
			        UpdatePositionOrientation();
                if (mSinkingTimer > 0.0)
                {
                    mSinkingTimer -= time;
            
                    if (mSinkingTimer <= 0.0)
                        SinkingComplete();
                }
		        return;
	        }
    
            if (mReloadTimer > 0.0)
            {
                mReloadTimer -= time;
        
                if (mReloadTimer <= 0.0)
                    Reload();
            }
	
	        base.AdvanceTime(time);
	        AvoidCollisions();
	        UpdatePositionOrientation();
	
	        float sailForce = Navigate();
            if (mWake != null)
            {
                mWake.SpeedFactor = Math.Min(1f, 0.83f * sailForce / 120f);
                TickWakeOdometer(sailForce * (float)time * GameController.GC.Fps);
            }
            
            UpdateCostumeWithAngularVelocity(mBody.GetAngularVelocity());

	        if (IsOutOfBounds)
            {
                // Ship is lost/out of control, so dispose of it.
		        mBootyGoneWanting = false;
                Dock();
	        }
        }

        protected virtual void BurnOut()
        {
            if (mResources == null || !mResources.StartTweenForKey(NpcShipCache.RESOURCE_KEY_NPC_BURN_OUT_TWEEN))
            {
                SPTween tween = new SPTween(mWardrobe, mBurningClip.Duration / 2);
                tween.AnimateProperty("Alpha", 0f);
                mScene.Juggler.AddObject(tween);
            }

            ProceedToSink();
        }

        protected void SinkingComplete()
        {
            if (AshBitmap == Ash.ASH_ABYSSAL && !mPreparingForNewGame)
            {
                AbyssalBlastProp blastProp = AbyssalBlastProp.GetAbyssalBlastProp((int)PFCat.SEA);
                blastProp.SinkerID = SinkerID;
                blastProp.X = CenterX;
                blastProp.Y = CenterY;
                mScene.AddProp(blastProp);
                blastProp.Blast();
                blastProp = null;
            }

            mScene.Juggler.RemoveTweensWithTarget(this);
            mScene.RemoveActor(this); // Calls safe remove for us
        }

        protected virtual void DockingComplete()
        {
            mScene.Juggler.RemoveTweensWithTarget(this);
            mScene.RemoveActor(this);
        }

        public override void PrepareForNewGame()
        {
            if (mPreparingForNewGame)
                return;
            mPreparingForNewGame = true;
            DockOverTime(mNewGamePreparationDuration);
        }

        public void ResourceEventFiredWithKey(uint key, string type, object target)
        {
            switch (key)
            {
                case NpcShipCache.RESOURCE_KEY_NPC_DOCK_TWEEN:
                    DockingComplete();
                    break;
                case NpcShipCache.RESOURCE_KEY_NPC_BURN_IN_TWEEN:
                    BurnOut();
                    break;
                case NpcShipCache.RESOURCE_KEY_NPC_BURN_OUT_TWEEN:
                    break;
                case NpcShipCache.RESOURCE_KEY_NPC_SHRINK_TWEEN:
                    break;
                default:
                    break;
            }
        }

        public override void CheckoutPooledResources()
        {
            if (mResources == null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_NPC_SHIP);

                if (cache != null)
                    mResources = cache.CheckoutPoolResourcesForKey(mKey);
            }

	        if (mResources == null)
		        Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED NPC_SHIP CACHE _+_++_+_+_+_+_+_+");
            else {
                mResources.Client = this;
        
                if (mWardrobe == null)
                    mWardrobe = mResources.DisplayObjectForKey(NpcShipCache.RESOURCE_KEY_NPC_WARDROBE) as SPSprite;
                if (mCostumeImages == null)
                    mCostumeImages = mResources.MiscResourceForKey(NpcShipCache.RESOURCE_KEY_NPC_COSTUME) as List<SPImage>;
                if (mSinkingClip == null)
                    mSinkingClip = mResources.DisplayObjectForKey(NpcShipCache.RESOURCE_KEY_NPC_SINKING) as SPMovieClip;
                if (mBurningClip == null)
                    mBurningClip = mResources.DisplayObjectForKey(NpcShipCache.RESOURCE_KEY_NPC_BURNING) as SPMovieClip;
            }
        }

        public override void CheckinPooledResources()
        {
            if (mResources != null)
            {
                mWardrobe.RemoveAllChildren();
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_NPC_SHIP);

                if (cache != null)
                    cache.CheckinPoolResources(mResources);
                mResources = null;
            }
        }

        protected override void ZeroOutFixtures()
        {
            base.ZeroOutFixtures();

            mFeeler = null;
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
                        if (mDestination != null)
                        {
                            if (mDestination.PoolIndex != -1)
                                mDestination.Hibernate();
                            mDestination = null;
                        }

                        mAvoiding = null;

                        if (mResources != null)
                        {
                            CheckinPooledResources();

                            if (mCostume != null)
                                mCostume.RemoveFromParent();
                            // CheckinPooledResources' mWardrobe.RemoveAllChildren() takes care of mSinkingClip and mBurningClip.
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
