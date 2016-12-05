using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class Shark : Actor, IPathFollower, IResourceClient, IReusable
    {
        private enum SharkState
        {
            Null = 0,
            Swimming = 1,
            Pursuing = 2,
            Attacking = 3
        }

        private const float kSharkSwimSpeed = 20f;

        private const uint kSharkReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 15;
            uint reuseKey = kSharkReuseKey;
            string key = "Shark";
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = SharkAt(-200, -200, 0, key);
                reusable.Hibernate();
                sCache.AddReusable(reusable);
            }

            sCache.VerifyCacheIntegrity();
            sCaching = false;
        }

        private static IReusable CheckoutReusable(uint reuseKey)
        {
            IReusable reusable = null;

            if (sCache != null && !sCaching)
                reusable = sCache.Checkout(reuseKey);

            return reusable;
        }

        private static void CheckinReusable(IReusable reusable)
        {
            if (sCache != null && !sCaching)
                sCache.Checkin(reusable);
        }

        public static Shark SharkAt(float x, float y, float angle, string key)
        {
            Shark actor = CheckoutReusable(kSharkReuseKey) as Shark;

            if (actor != null)
            {
                actor.Reuse();

                Body body = actor.B2Body;
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);
                body.SetTransform(new Vector2(x, y), angle);
                body.SetActive(true);

                actor.X = actor.PX;
                actor.Y = actor.PY;
                actor.Rotation = -actor.B2Rotation;
            }
            else
            {
                actor = new Shark(MiscFactory.Factory.CreateSharkDef(x, y, angle), key);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed Shark ReusableCache.");
#endif
            }

            return actor;
        }

        public Shark(ActorDef def, string key)
            : base(def)
        {
            mKey = key;
		    mIsCollidable = true;
            mInUse = true;
            mPoolIndex = -1;
            mTimeToKill = 0;
		    mCategory = (int)PFCat.SEA;
		    mAdvanceable = true;
		    mDestination = null;
		    mPrey = null;
		    mResources = null;
		    mState = SharkState.Null;
		
		    // Save fixtures
            if (def.fixtureDefCount != 2)
                throw new ArgumentException("Shark ActorDef.fixtureDefCount must be 2.");
            mHead = def.fixtures[0];
            mNose = def.fixtures[1];

            mSwimClip = null;
            mAttackClip = null;
            CheckoutPooledResources();
            SetupActorCostume();
        }
        
        #region Fields
        private bool mIsCollidable;
        private bool mInUse;
        private int mPoolIndex;
        private double mTimeToKill;
        private Fixture mNose;
        private Fixture mHead;
        private SPMovieClip mSwimClip;
        private SPMovieClip mAttackClip;
        private SharkState mState;
        private float mSpeed;
        private Destination mDestination;
        private OverboardActor mPrey;
        private ResourceServer mResources;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kSharkReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public bool IsCollidable { get { return mIsCollidable; } set { mIsCollidable = value; } }
        public Destination Destination { get { return mDestination; } set { mDestination = value; } }
        public OverboardActor Prey
        {
            get { return mPrey; }
            set
            {
                if (mPrey == value)
                    return;
    
                // Prevent stack overflow when OverboardActor tries to unset us.
                OverboardActor currentPrey = mPrey;
                mPrey = null;
    
                if (currentPrey != null)
                {
                    if (currentPrey.Predator == this)
                        currentPrey.Predator = null;
                    currentPrey = null;
                }
    
	            mPrey = value;
    
                if (!MarkedForRemoval)
                {
                    if (mPrey != null)
                        SetSharkState(SharkState.Pursuing);
                    else
                        SetSharkState(SharkState.Swimming);
                }
            }
        }
        public static float SwimFps { get { return 10f; } }
        public static float AttackFps { get { return 12f; } }
        #endregion

        #region Methods
        private void SetupActorCostume()
        {
            if (mSwimClip == null)
            {
		        mSwimClip = new SPMovieClip(mScene.TexturesStartingWith("shark_"), Shark.SwimFps);
		        mSwimClip.Loop = true;
		        mSwimClip.X = -mSwimClip.Width / 2;
		        mSwimClip.Y = -mSwimClip.Height / 2;
	        }
    
            mSwimClip.Visible = true;
	
	        if (mAttackClip == null)
            {
		        mAttackClip = new SPMovieClip(mScene.TexturesStartingWith("shark-attack_"), Shark.AttackFps);
		        mAttackClip.Loop = false;
		        mAttackClip.X = -mAttackClip.Width / 2;
		        mAttackClip.Y = -mAttackClip.Height / 2;
                mAttackClip.AddEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)OnAttackMovieComplete);
	        }
    
            mAttackClip.CurrentFrame = 0;
            mAttackClip.Visible = false;
	
            AddChild(mSwimClip);
            AddChild(mAttackClip);
	
	        Alpha = 0.5f;
	        X = PX;
	        Y = PY;
	        Rotation = -B2Rotation;
	
            mScene.Juggler.AddObject(mSwimClip);
            mScene.Juggler.AddObject(mAttackClip);
            SetSharkState(SharkState.Swimming);
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;

            Visible = true;
            mIsCollidable = true;
            mTimeToKill = 0;
            mDestination = null;
            mPrey = null;
            mResources = null;
            mState = SharkState.Null;

            mSwimClip = null;
            mAttackClip = null;
            CheckoutPooledResources();
            SetupActorCostume();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mPrey != null)
                Prey = null;

            if (mAttackClip != null)
            {
                mAttackClip.RemoveEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)OnAttackMovieComplete);
                mScene.Juggler.RemoveTweensWithTarget(mAttackClip);
                mAttackClip = null;
            }

            if (mSwimClip != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mSwimClip);
                mSwimClip = null;
            }

            if (mDestination != null)
            {
                if (mDestination.PoolIndex != -1)
                    mDestination.Hibernate();
                mDestination = null;
            }

            RemoveAllChildren();
            CheckinPooledResources();

            mBody.SetActive(false);
            ClearContacts();

            mInUse = false;
            CheckinReusable(this);
        }

        private void SetSharkState(SharkState state)
        {
            if (state == mState)
		        return;
	
	        switch (state)
            {
                case SharkState.Null:
                    throw new ArgumentException("Bad Shark State");
		        case SharkState.Swimming:
                    {
			            mSpeed = kSharkSwimSpeed;
			            mSwimClip.CurrentFrame = 0;
			            mSwimClip.Visible = true;
                        mSwimClip.Play();
                        mAttackClip.Visible = false;
                        mAttackClip.Pause();
                        break;
		            }
		        case SharkState.Pursuing:
                    {
                        mTimeToKill = 0;
			            break;
                    }
		        case SharkState.Attacking:
		            {
                        PlayEatVictimSound();
			            mSpeed = kSharkSwimSpeed / 2;
			            mSwimClip.Visible = false;
                        mSwimClip.Pause();
			            mAttackClip.CurrentFrame = 0;
			            mAttackClip.Visible = true;
			            mAttackClip.Play();
			
                        AABB aabb;
                        mNose.GetAABB(out aabb, 0);
                        Vector2 waterPos = aabb.GetCenter();
                        SharkWater water = SharkWater.SharkWaterAt(ResManager.M2PX(waterPos.X), ResManager.M2PY(waterPos.Y));
                        mScene.AddProp(water);
                        water.PlayEffect();
                        break;
		            }
	        }
	
	        mState = state;
        }

        public void PlayEatVictimSound()
        {
            mScene.PlaySound("SharkAttack");
        }

        public float Navigate()
        {
            if (mRemoveMe || mBody == null)
            {
                if (mBody == null)
                    Dock();
		        return 0;
            }
    
	        float swimForce = mSpeed * mBody.GetMass();
            SwimWithForce(swimForce);
	
	        if (mState != SharkState.Attacking)
            {
		        Vector2 bodyPos = mBody.GetPosition(), destPos;
		
		        if (mPrey != null)
                {
			        Vector2 preyPos = mPrey.B2Body.GetPosition();
			        destPos = bodyPos - preyPos;
		        } else {
			        destPos = bodyPos - mDestination.Dest;
		        }
		
		        if (Math.Abs(destPos.X) < 2.5f && Math.Abs(destPos.Y) < 2.5f)
                {
			        if (mState == SharkState.Swimming)
				        Dock();
		        }
                else
                {
			        // Turn towards destination
			        Vector2 linearVel = mBody.GetLinearVelocity();
			        float angleToTarget = Box2DUtils.SignedAngle(ref destPos, ref linearVel);
			
			        if (angleToTarget != 0.0f)
                    {
				        float turnForce = ((angleToTarget > 0.0f) ? -1.0f : 1.0f) * mBody.GetMass() * 5f;
                        TurnWithForce(turnForce);
			        }
		        }
	        }

	        return swimForce;
        }

        public override void AdvanceTime(double time)
        {
            // Ship position/orientation
	        X = PX;
	        Y = PY;
	        Rotation = -B2Rotation;
	        Navigate();
    
            if (mState == SharkState.Pursuing)
            {
                mTimeToKill += time;
        
                if (mTimeToKill > 30.0)
                {
                    mTimeToKill = 0;
                    Dock();
                }
            }
        }

        private void AttackCompleted()
        {
            Prey = null;
        }

        private void OnAttackMovieComplete(SPEvent ev)
        {
            AttackCompleted();
        }

        private void SwimWithForce(float force)
        {
            if (mBody == null)
                return;
            AABB aabb;
            mNose.GetAABB(out aabb, 0);
            Vector2 noseCenter = aabb.GetCenter();
            Vector2 bodyCenter = mBody.GetWorldCenter();
            Vector2 delta = noseCenter - bodyCenter;
            delta.Normalize();
            mBody.ApplyForce(force * delta, bodyCenter);
        }

        private void TurnWithForce(float force)
        {
            if (mBody == null)
		        return;
	        float dir = (force < 0.0f) ? 1 : -1;
	        Vector2 turnVec = new Vector2(0.0f, Math.Abs(force));
            Box2DUtils.RotateVector(ref turnVec, mBody.GetAngle() + dir * SPMacros.PI_HALF);

            AABB aabb;
            mNose.GetAABB(out aabb, 0);
            Vector2 noseCenter = aabb.GetCenter();
            mBody.ApplyForce(turnVec, noseCenter);
        }

        public void Dock()
        {
            mScene.RemoveActor(this);
        }

        public override void RespondToPhysicalInputs()
        {
            if (mState == SharkState.Attacking)
		        return;
	
	        foreach (Actor actor in mContacts.EnumerableSet)
            {
		        if (actor == mPrey)
                {
                    SetSharkState(SharkState.Attacking);
                    mPrey.DeathBitmap = DeathBitmaps.SHARK;
                    mPrey.GetEatenByShark();
			        break;
		        }
	        }
        }

        private bool IgnoresContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            return (fixtureSelf != mNose || other is OverboardActor == false);
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (IgnoresContact(other, fixtureSelf, fixtureOther, contact))
                return;
            base.BeginContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (IgnoresContact(other, fixtureSelf, fixtureOther, contact))
                return;
            base.EndContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void PrepareForNewGame()
        {
            // Do nothing
        }

        public void ResourceEventFiredWithKey(uint key, string type, object target)
        {
            switch (key)
            {
                case SharkCache.RESOURCE_KEY_SHARK_SWIM:
                    break;
                case SharkCache.RESOURCE_KEY_SHARK_ATTACK:
                    AttackCompleted();
                    break;
                default:
                    break;
            }
        }

        public override void CheckoutPooledResources()
        {
            if (mResources == null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_SHARK);

                if (cache != null)
                    mResources = cache.CheckoutPoolResourcesForKey("Shark");
            }

	        if (mResources == null)
		        Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED SHARK CACHE _+_++_+_+_+_+_+_+");
            else
            {
                mResources.Client = this;
        
                if (mSwimClip == null)
                    mSwimClip = mResources.DisplayObjectForKey(SharkCache.RESOURCE_KEY_SHARK_SWIM) as SPMovieClip;
                if (mAttackClip == null)
                    mAttackClip = mResources.DisplayObjectForKey(SharkCache.RESOURCE_KEY_SHARK_ATTACK) as SPMovieClip;
            }
        }

        public override void CheckinPooledResources()
        {
            if (mResources != null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_SHARK);

                if (cache != null)
                    cache.CheckinPoolResources(mResources);
                mResources = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mPrey != null)
                            Prey = null;

                        if (mAttackClip != null)
                        {
                            mAttackClip.RemoveFromParent();
                            mAttackClip.RemoveEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)OnAttackMovieComplete);
                            mScene.Juggler.RemoveTweensWithTarget(mAttackClip);

                            if (mResources == null)
                                mAttackClip.Dispose();
                            mAttackClip = null;
                        }

                        if (mSwimClip != null)
                        {
                            mSwimClip.RemoveFromParent();
                            mScene.Juggler.RemoveTweensWithTarget(mSwimClip);

                            if (mResources == null)
                                mSwimClip.Dispose();
                            mSwimClip = null;
                        }

                        if (mDestination != null)
                        {
                            if (mDestination.PoolIndex != -1)
                                mDestination.Hibernate();
                            mDestination = null;
                        }

                        CheckinPooledResources();
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
