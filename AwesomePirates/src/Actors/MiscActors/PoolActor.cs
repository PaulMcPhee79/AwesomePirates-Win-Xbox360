
//#define UNIQUE_POOL_REFRACTION // This is extremely expensive.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class PoolActor : Actor, IResourceClient, IReusable
    {
        public enum PoolActorType
        {
            AcidPool = 1,
            MagmaPool = 2
        }

        public const string kPoolVisualStyleAcid = "AcidPool";
        public const string kPoolVisualStyleMagma = "MagmaPool";

        public static readonly string[] kPoolVisualStylesSK = new string[]
        {
            "RedPool",
            "BluePool",
            "GreenPool",
            "YellowPool"
        };

        protected enum PoolState
        {
            Idle = 0,
            Spawning,
            Spawned,
            Despawning,
            Despawned
        }

        private const float kSpawnDuration = 2f;
        private const float kSpawnedAlpha = 0.7f;
        private const float kSpawnedScale = 1.5f;

        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(2);

            // AcidPools
            int cacheSize = 50;
            uint reuseKey = (uint)PoolActorType.AcidPool;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = PoolActorWithType(PoolActorType.AcidPool, 0, 0, Globals.ASH_DURATION_ACID_POOL, kPoolVisualStyleAcid);
                reusable.Hibernate();
                sCache.AddReusable(reusable);
            }

            // MagmaPools
            cacheSize = 30;
            reuseKey = (uint)PoolActorType.MagmaPool;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = PoolActorWithType(PoolActorType.MagmaPool, 0, 0, Globals.ASH_DURATION_MAGMA_POOL, kPoolVisualStyleMagma);
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

        public static PoolActor PoolActorWithType(PoolActorType type, float x, float y, float duration, string visualStyle)
        {
            uint reuseKey = (uint)type;
            PoolActor actor = CheckoutReusable(reuseKey) as PoolActor;

            if (actor != null)
            {
                actor.Duration = duration;
                actor.DurationRemaining = duration;
                actor.ResourceKey = visualStyle;
                actor.Reuse();

                Body body = actor.B2Body;
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);
                body.SetTransform(new Vector2(x, y), 0);
                body.SetActive(true);
                body.ApplyLinearImpulse(new Vector2(0.05f, 0.05f), body.GetPosition()); // Hack to ignite Box2D contacts on motionless bodies.

                actor.X = actor.PX;
                actor.Y = actor.PY;
                actor.Rotation = -actor.B2Rotation;
            }
            else
            {
                switch (type)
                {
                    case PoolActorType.AcidPool:
                        actor = new AcidPoolActor(MiscFactory.Factory.CreatePoolDefinition(x, y), duration, visualStyle);
                        break;
                    case PoolActorType.MagmaPool:
                        actor = new MagmaPoolActor(MiscFactory.Factory.CreatePoolDefinition(x, y), duration, visualStyle);
                        break;
                }
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed PoolActor ReusableCache.");
#endif
            }

            return actor;
        }

        public PoolActor(ActorDef def, float duration, string visualStyle)
            : base(def)
        {
            mCategory = (int)PFCat.POOLS;
		    mAdvanceable = true;
            mInUse = true;
            mPoolIndex = -1;
            mResourceKey = visualStyle;
		    mState = PoolState.Idle;
		    mDuration = duration;
		    mDurationRemaining = duration;
		    mRipples = null;
		    mResources = null;
            CheckoutPooledResources();
            SetupActorCostume();
        }
        
        #region Fields
        protected bool mInUse;
        protected int mPoolIndex;

        protected string mResourceKey;
        protected PoolState mState;
        protected double mDuration;
        protected double mDurationRemaining;
#if UNIQUE_POOL_REFRACTION
        private int mRefractionIndex;
        private float[] mRefractionFactors;
#endif

        protected SPSprite mCostume;
        protected List<SPSprite> mRipples;
        protected ResourceServer mResources;
        #endregion

        #region Properties
        public virtual uint ReuseKey { get { return 0; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        protected double Duration { get { return mDuration; } set { mDuration = value; } }
        public double DurationRemaining { get { return mDurationRemaining; } protected set { mDurationRemaining = value; } }
        public bool Despawning { get { return (mState == PoolState.Despawning || mState == PoolState.Despawned); } }
        public SKTeamIndex SinkerID { get; set; }

        // Override in subclass
        public virtual double FullDuration { get { return 0; } }
        public virtual uint BitmapID { get { return 0; } }
        public virtual uint DeathBitmap { get { return 0; } }
        public virtual string PoolTextureName { get { return null; } }
        public virtual string ResourceKey { get { return mResourceKey; } protected set { mResourceKey = value; } }
        
        public static float SpawnDuration { get { return kSpawnDuration; } }
        public static float DespawnDuration { get { return Globals.VOODOO_DESPAWN_DURATION; } }
        public static float SpawnedAlpha { get { return kSpawnedAlpha; } }
        public static float SpawnedScale { get { return kSpawnedScale; } }
        public static int NumPoolRipples { get { return 3; } }
        #endregion

        #region Methods
        protected virtual void SetupActorCostume()
        {
            if (mCostume == null)
                mCostume = new SPSprite();
            mCostume.ScaleX = mCostume.ScaleY = 0.01f;
	        mCostume.Alpha = 0.01f;
            AddChild(mCostume);
    
	        if (mRipples == null)
            {
                int numRipples = PoolActor.NumPoolRipples;
		        mRipples = new List<SPSprite>(numRipples);
		
		        SPTexture poolTexture = mScene.TextureByName(PoolTextureName);
		
		        for (int i = 0; i < numRipples; ++i)
                {
			        SPImage image = new SPImage(poolTexture);
			        image.X = -image.Width / 2;
			        image.Y = -image.Height / 2;
			
			        SPSprite sprite = new SPSprite();
			        sprite.ScaleX = sprite.ScaleY = 0;
                    sprite.AddChild(image);
                    mRipples.Add(sprite);
                    mCostume.AddChild(sprite);
		        }
	        }
            else
            {
		        foreach (SPSprite sprite in mRipples)
			        mCostume.AddChild(sprite);
	        }

#if UNIQUE_POOL_REFRACTION
            // Setup refraction drawing
            mRefractionIndex = 0;

            if (mRefractionFactors == null)
            {
                mRefractionFactors = new float[mRipples.Count];

                for (int i = 0; i < mRefractionFactors.Length; ++i)
                    mRefractionFactors[i] = 0.075f + GameController.GC.NextRandom(1, 10) * 0.0075f; // 0.075 - 0.15
            }

            if (Effecter == null)
                Effecter = new SPEffecter(mScene.EffectForKey("Refraction"), PoolDraw);
#endif


            X = PX;
	        Y = PY;
	        Rotation = -B2Rotation;
	
	        if (mDuration <= Globals.VOODOO_DESPAWN_DURATION)
            {
		        // Start in despawn mode
		        ScaleX = ScaleY = kSpawnedScale;
		        Alpha = kSpawnedAlpha * (float)(mDuration / Globals.VOODOO_DESPAWN_DURATION);
                SetState(PoolState.Spawned);
                DespawnOverTime((float)mDuration);
	        }
            else if (SPMacros.SP_IS_DOUBLE_EQUAL(FullDuration, mDuration) || mDuration > FullDuration)
            {
		        // Start as new pool
                SetState(PoolState.Idle);
                SpawnOverTime(kSpawnDuration);
	        }
            else if (mDuration > (FullDuration - kSpawnDuration))
            {
		        // Start spawning
		        float spawnFraction = (float)(FullDuration - mDuration) / kSpawnDuration;
		        float spawnDuration = (1f - spawnFraction) * kSpawnDuration;
		
		        Alpha = kSpawnedAlpha * spawnFraction;
		        ScaleX = ScaleY = kSpawnedScale * spawnFraction;
                SetState(PoolState.Idle);
                SpawnOverTime(spawnDuration);
	        }
            else
            {
		        // Start already spawned
		        Alpha = kSpawnedAlpha;
		        ScaleX = ScaleY = kSpawnedScale;
                SetState(PoolState.Spawned);
	        }

            StartPoolAnimation();
        }

        public virtual void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;

            mState = PoolState.Idle;
            mRipples = null;
            mResources = null;
            CheckoutPooledResources();

            Alpha = 1f;
            Visible = true;
            SetupActorCostume();

            mInUse = true;
        }

        public virtual void Hibernate()
        {
            if (!InUse)
                return;

            StopPoolAnimation();

            if (mCostume != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mCostume);
                mCostume.RemoveAllChildren();
                mCostume = null;
            }

            RemoveAllChildren();

            CheckinPooledResources();
            mRipples = null;

            mBody.SetActive(false);
            ClearContacts();

            mInUse = false;
            CheckinReusable(this);
        }

#if UNIQUE_POOL_REFRACTION
        public void PoolDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (mRefractionIndex >= mRefractionFactors.Length)
                mRefractionIndex = 0;
            mScene.CustomDrawer.RefractionFactor = mRefractionFactors[mRefractionIndex];
            mScene.CustomDrawer.RefractionDrawSml(displayObject, gameTime, support, parentTransform);
            ++mRefractionIndex;
        }
#endif
        private void SetState(PoolState state)
        {
            if (state < mState)
                return;
    
	        switch (state)
            {
		        case PoolState.Idle:
			        break;
		        case PoolState.Spawning:
			        break;
		        case PoolState.Spawned:
			        break;
		        case PoolState.Despawning:
			        break;
		        case PoolState.Despawned:
                    mScene.RemoveActor(this);
			        break;
		        default:
			        break;
	        }
	        mState = state;
        }

        public void StartPoolAnimation()
        {
            StopPoolAnimation();
	
	        float delay = 0;
            uint index = 0;
        
            foreach (SPSprite sprite in mRipples)
            {
                sprite.ScaleX = sprite.ScaleY = 0;
                sprite.Alpha = 1;
            
                if (mResources == null || !mResources.StartTweenForKey(PoolActorCache.RESOURCE_KEY_POOL_RIPPLE_TWEEN_SCALE + index))
                {
                    SPTween tween = new SPTween(sprite, 0.8f * mRipples.Count);
                    tween.AnimateProperty("ScaleX", 1.2f);
                    tween.AnimateProperty("ScaleY", 1.2f);
                    tween.Delay = delay;
                    tween.Loop = SPLoopType.Repeat;
                    mScene.Juggler.AddObject(tween);
                }
            
                if (mResources == null || !mResources.StartTweenForKey(PoolActorCache.RESOURCE_KEY_POOL_RIPPLE_TWEEN_ALPHA + index))
                {
                    SPTween tween = new SPTween(sprite, 0.8f * mRipples.Count, SPTransitions.SPEaseInLinear);
                    tween.AnimateProperty("Alpha", 0);
                    tween.Delay = delay;
                    tween.Loop = SPLoopType.Repeat;
                    mScene.Juggler.AddObject(tween);
                    delay += (float)tween.TotalTime / mRipples.Count;
                }
            
                ++index;
            }
        }

        public void StopPoolAnimation()
        {
            if (mRipples != null)
            {
                foreach (SPSprite sprite in mRipples)
                    mScene.Juggler.RemoveTweensWithTarget(sprite);
            }
        }

        public override void AdvanceTime(double time)
        {
            if (MarkedForRemoval)
		        return;
    
            if (mDurationRemaining > Globals.VOODOO_DESPAWN_DURATION)
            {
                mDurationRemaining -= time;
        
                if (mDurationRemaining <= Globals.VOODOO_DESPAWN_DURATION)
                    DespawnOverTime(Globals.VOODOO_DESPAWN_DURATION);
            }
            else
            {
                mDurationRemaining -= time;
        
                if (mDurationRemaining < 0)
                    mDurationRemaining = 0;
            }

            X = PX;
            Y = PY;
            Rotation = -B2Rotation;
        }

        private void SpawnOverTime(float duration)
        {
            if (mState != PoolState.Idle)
                throw new InvalidOperationException("PoolActor already spawned.");
	
            if (!SPMacros.SP_IS_DOUBLE_EQUAL(duration, kSpawnDuration) || mResources == null || !mResources.StartTweenForKey(PoolActorCache.RESOURCE_KEY_POOL_SPAWN_TWEEN))
            {
                SPTween tween = new SPTween(mCostume, duration);
                tween.AnimateProperty("Alpha", kSpawnedAlpha);
                tween.AnimateProperty("ScaleX", kSpawnedScale);
                tween.AnimateProperty("ScaleY", kSpawnedScale);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnSpawnCompleted);
                mScene.Juggler.AddObject(tween);
            }

            SetState(PoolState.Spawning);
        }

        public void DespawnOverTime(float duration)
        {
            if (mState != PoolState.Spawning && mState != PoolState.Spawned)
                return;
	
            if (!SPMacros.SP_IS_DOUBLE_EQUAL(duration, Globals.VOODOO_DESPAWN_DURATION) || mResources == null || !mResources.StartTweenForKey(PoolActorCache.RESOURCE_KEY_POOL_DESPAWN_TWEEN))
            {
                // Irregular despawn time indicates we may still be spawning when asked to despawn. Remove possible concurrent tweens.
                mScene.Juggler.RemoveTweensWithTarget(mCostume);
        
                SPTween tween = new SPTween(mCostume, duration);
                tween.AnimateProperty("Alpha", 0.01f);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnDespawnCompleted);
                mScene.Juggler.AddObject(tween);
            }

            SetState(PoolState.Despawning);
        }

        private void SpawnCompleted()
        {
            SetState(PoolState.Spawned);
        }

        private void DespawnCompleted()
        {
            SetState(PoolState.Despawned);
        }

        private void OnSpawnCompleted(SPEvent ev)
        {
            SpawnCompleted();
        }

        private void OnDespawnCompleted(SPEvent ev)
        {
            DespawnCompleted();
        }

        public virtual void SinkNpcShip(NpcShip ship)
        {
            if (ship == null)
                return;

            ship.DeathBitmap = DeathBitmap;
            ship.SinkerID = SinkerID;
            ship.Sink();
        }

        public override void RespondToPhysicalInputs()
        {
            if (mState == PoolState.Despawned || IsPreparingForNewGame)
		        return;
	        foreach (Actor actor in mContacts.EnumerableSet)
            {
		        if (actor.MarkedForRemoval)
			        continue;
		
		        if (actor is NpcShip)
                {
			        NpcShip ship = actor as NpcShip;
			
			        if (!ship.Docking)
                        SinkNpcShip(ship);
		        }
                else if (actor is OverboardActor)
                {
			        OverboardActor person = actor as OverboardActor;

                    if (!person.Dying)
                    {
                        person.DeathBitmap = DeathBitmap;
                        person.EnvironmentalDeath();
                    }
		        }
                else if (actor is PowderKegActor)
                {
                    PowderKegActor keg = actor as PowderKegActor;
                    keg.Detonate();
                }
                else if (actor is BrandySlickActor)
                {
                    BrandySlickActor slick = actor as BrandySlickActor;
                    slick.Ignite();
                }
                else if (actor is SkirmishShip)
                {
                    SkirmishShip ship = actor as SkirmishShip;
                    ship.ApplyEnvironmentalDamage(1);
                }
	        }
        }

        private bool IgnoresContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            bool ignores = false;
    
            if (other is NpcShip)
            {
		        NpcShip ship = other as NpcShip;
		        if (fixtureOther != ship.Stern)
			        ignores = true;
	        }
            else if (other is SkirmishShip)
            {
                SkirmishShip ship = other as SkirmishShip;
                if (ship.TeamIndex == SinkerID)
                    ignores = true;
            }
            else if (other is OverboardActor == false && other is PowderKegActor == false && other is BrandySlickActor == false)
            {
                ignores = true;
            }
    
            return ignores;
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
            if (mPreparingForNewGame)
                return;
            mPreparingForNewGame = true;
            DespawnOverTime(mNewGamePreparationDuration);
        }

        public void ResourceEventFiredWithKey(uint key, string type, object target)
        {
            switch (key)
            {
                case PoolActorCache.RESOURCE_KEY_POOL_SPAWN_TWEEN:
                    SpawnCompleted();
                    break;
                case PoolActorCache.RESOURCE_KEY_POOL_DESPAWN_TWEEN:
                    DespawnCompleted();
                    break;
                default:
                    break;
            }
        }

        public override void CheckoutPooledResources()
        {
            if (mResources == null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_POOL_ACTOR);

                if (cache != null)
                    mResources = cache.CheckoutPoolResourcesForKey(ResourceKey);
            }

	        if (mResources == null)
		        Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED POOL ACTOR CACHE _+_++_+_+_+_+_+_+");
            else
            {
                mResources.Client = this;
        
                if (mRipples == null)
                    mRipples = mResources.MiscResourceForKey(PoolActorCache.RESOURCE_KEY_POOL_RIPPLES) as List<SPSprite>;
                if (mCostume == null)
                    mCostume = mResources.DisplayObjectForKey(PoolActorCache.RESOURCE_KEY_POOL_COSTUME) as SPSprite;
            }
        }

        public override void CheckinPooledResources()
        {
            if (mResources != null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_POOL_ACTOR);

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
                        if (mCostume != null)
                            mScene.Juggler.RemoveTweensWithTarget(mCostume);

                        StopPoolAnimation();

                        if (mResources != null)
                        {
                            CheckinPooledResources();

                            if (mCostume != null)
                                mCostume.RemoveFromParent();
                        }

                        mRipples = null;
                        mCostume = null;
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
