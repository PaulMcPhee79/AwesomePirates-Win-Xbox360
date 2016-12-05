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
    class OverboardActor : Actor, IPathFollower, IResourceClient, IReusable
    {
        private enum OverboardActorState
        {
            Alive = 0,
            Eaten,
            Dead
        }

        private const uint kOverboardReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 35;
            uint reuseKey = kOverboardReuseKey;
            string key = "Prisoner";
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = OverboardActorAt(-100, -100, 0, key);
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

        public static OverboardActor OverboardActorAt(float x, float y, float angle, string key)
        {
            OverboardActor actor = CheckoutReusable(kOverboardReuseKey) as OverboardActor;

            if (actor != null)
            {
                actor.Reuse();

                Body body = actor.B2Body;
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);
                body.SetTransform(new Vector2(x, y), angle);
                body.SetActive(true);
                body.ApplyLinearImpulse(new Vector2(0.05f, 0.05f), body.GetPosition()); // Hack to ignite Box2D contacts on motionless bodies.

                actor.X = actor.PX;
                actor.Y = actor.PY;
                actor.Rotation = -actor.B2Rotation;
            }
            else
            {
                actor = new OverboardActor(MiscFactory.Factory.CreatePersonOverboardDef(x, y, angle), key);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed OverboardActor ReusableCache.");
#endif
            }

            return actor;
        }

        public OverboardActor(ActorDef def, string key)
            : base(def)
        {
            mCategory = (int)PFCat.POINT_MOVIES;
		    mAdvanceable = true;
		    mIsCollidable = true;
            mHasRepellent = false;
            mIsPlayer = false;
            mInUse = true;
            mPoolIndex = -1;
		    mKey = key;
		    mState = OverboardActorState.Alive;
            mDeathBitmap = 0;
		    mPrisoner = null;
		    mPersonClip = null;
		    mBlood = null;
		    mDestination = null;
		    mPredator = null;
		    mResources = null;
            CheckoutPooledResources();
		    SetupActorCostume();
        }
        
        #region Fields
        private bool mIsCollidable;
        private bool mHasRepellent;
        private bool mIsPlayer;
        private bool mInUse;
        private int mPoolIndex;
        private OverboardActorState mState;
        private uint mDeathBitmap;
        private Prisoner mPrisoner;
        private SPMovieClip mPersonClip;
        private SPSprite mBlood;
        private Destination mDestination;
        private Shark mPredator;
        private ResourceServer mResources;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kOverboardReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public bool IsCollidable { get { return mIsCollidable; } set { mIsCollidable = value; } }
        public bool Edible { get { return (mPredator == null && mState == OverboardActorState.Alive && !mHasRepellent); } }
        public bool HasRepellent { get { return mHasRepellent; } set { mHasRepellent = value; } }
        public bool IsPlayer { get { return mIsPlayer; } set { mIsPlayer = value; } }
        public bool Dying { get { return mState != OverboardActorState.Alive; } }
        public Prisoner Prisoner { get { return mPrisoner; } set { mPrisoner = value; } }
        public Gender Gender { get { return mPrisoner.Gender; } }
        public int InfamyBonus { get { return mPrisoner.InfamyBonus; } }
        public SKTeamIndex KillerID { get; set; }
        public uint DeathBitmap { get { return mDeathBitmap; } set { mDeathBitmap = value; } }
        public Destination Destination { get { return mDestination; } set { mDestination = value; } }
        public Shark Predator
        {
            get { return mPredator; }
            set
            {
                if (mPredator == value)
                    return;
    
                // Prevent stack overflow when Shark tries to unset us.
                Shark currentPredator = mPredator;
                mPredator = null;
    
                if (currentPredator != null)
                {
                    if (currentPredator.Prey == this)
                        currentPredator.Prey = null;
                    currentPredator = null;
                }

                mPredator = value;
            }
        }
        public static float Fps { get { return 6f; } }
        #endregion

        #region Methods
        private void SetupActorCostume()
        {
            if (mPersonClip == null)
            {
		        mPersonClip = new SPMovieClip(mScene.TexturesStartingWith("overboard_"), OverboardActor.Fps);
		        mPersonClip.X = -mPersonClip.Width / 2;
		        mPersonClip.Y = -mPersonClip.Height / 2;
		        mPersonClip.Loop = true;
	        }
    
            mPersonClip.ScaleX = mPersonClip.ScaleY = 1;
            mPersonClip.Alpha = 1;
            mPersonClip.CurrentFrame = 0;
            mPersonClip.Play();
	
	        if (mBlood == null)
            {
		        SPImage bloodImage = new SPImage(mScene.TextureByName("blood"));
		        bloodImage.X = -bloodImage.Width / 2;
		        bloodImage.Y = -bloodImage.Height / 2;
                //bloodImage.Effecter = new SPEffecter(mScene.EffectForKey("Refraction"), GameController.GC.BloodDraw);
		
		        mBlood = new SPSprite();
                mBlood.AddChild(bloodImage);
	        }
    
            mBlood.ScaleX = mBlood.ScaleY = 0.5f;
            mBlood.Alpha = 1;
	
	        X = PX;
	        Y = PY;
	        Rotation = -B2Rotation;
	
            AddChild(mPersonClip);
            mScene.Juggler.AddObject(mPersonClip);
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;

            mIsCollidable = true;
            mHasRepellent = false;
            mIsPlayer = false;
            Visible = true;
            Alpha = 1f;
            mState = OverboardActorState.Alive;
            mDeathBitmap = 0;
            mPrisoner = null;
            mPersonClip = null;
            mBlood = null;
            mDestination = null;
            mPredator = null;
            mResources = null;
            CheckoutPooledResources();
            SetupActorCostume();
            
            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mPredator != null)
                Predator = null;

            if (mState != OverboardActorState.Dead)
            {
                if (mPersonClip != null)
                {
                    mScene.Juggler.RemoveTweensWithTarget(mPersonClip);
                    mScene.Juggler.RemoveObject(mPersonClip);
                    mPersonClip = null;
                }

                if (mState == OverboardActorState.Eaten)
                {
                    if (mBlood != null)
                    {
                        mScene.Juggler.RemoveTweensWithTarget(mBlood);
                        mScene.SpriteLayerManager.RemoveChild(mBlood, (int)PFCat.BLOOD);
                        mState = OverboardActorState.Dead;
                        mBlood = null;
                    }
                }
            }

            if (mDestination != null)
            {
                if (mDestination.PoolIndex != -1)
                    mDestination.Hibernate();
                mDestination = null;
            }

            RemoveAllChildren();
            CheckinPooledResources();
            mPrisoner = null;

            mBody.SetActive(false);
            ClearContacts();

            mInUse = false;
            CheckinReusable(this);
        }

        private void SetState(OverboardActorState state)
        {
            mState = state;
        }

        public void GetEatenByShark()
        {
            if (mState != OverboardActorState.Alive)
		        return;
    
            SetState(OverboardActorState.Eaten);
            PlayEatenAliveSound();
    
            SPTween tween = null;
            if (mResources == null || !mResources.StartTweenForKey(SharkCache.RESOURCE_KEY_SHARK_PERSON_TWEEN))
            {
                tween = new SPTween(mPersonClip, 0.5f);
                tween.AnimateProperty("Alpha", 0f);
                tween.AnimateProperty("ScaleX", 0.7f);
                tween.AnimateProperty("ScaleY", 0.7f);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnBodyShrunk);
                mScene.Juggler.AddObject(tween);
            }
	
	        mBlood.X = X;
	        mBlood.Y = Y;
	        mBlood.ScaleX = 0.5f;
	        mBlood.ScaleY = 0.5f;
	        mBlood.Alpha = 1.0f;
            mScene.SpriteLayerManager.AddChild(mBlood, (int)PFCat.BLOOD);
	
            if (mResources == null || !mResources.StartTweenForKey(SharkCache.RESOURCE_KEY_SHARK_BLOOD_TWEEN))
            {
                tween = new SPTween(mBlood, 5f);
                tween.AnimateProperty("Alpha", 0f);
                tween.AnimateProperty("ScaleX", 2f);
                tween.AnimateProperty("ScaleY", 2f);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnBloodFaded);
                mScene.Juggler.AddObject(tween);
            }
        }

        public void EnvironmentalDeath()
        {
            if (Dying || IsPlayer)
		        return;
            GetEatenByShark();
	
	        if (mPredator != null)
            {
                if (mPredator.Prey == this)
                    mPredator.Prey = null;
                mPredator = null;
	        }
        }

        private void BodyShrunk()
        {
            if (mScene.GameMode == GameMode.Career)
                mScene.AchievementManager.PrisonerKilled(this);
            else
                mScene.SKManager.PrisonerKilled(this, KillerID);
        }

        private void BloodFaded()
        {
            mScene.Juggler.RemoveObject(mPersonClip);

            if (mResources != null)
                mPersonClip.RemoveFromParent();
            mPersonClip = null;
            mScene.SpriteLayerManager.RemoveChild(mBlood, (int)PFCat.BLOOD);
            mScene.RemoveActor(this);
            SetState(OverboardActorState.Dead);
        }

        private void OnBodyShrunk(SPEvent ev)
        {
            BodyShrunk();
        }

        private void OnBloodFaded(SPEvent ev)
        {
            BloodFaded();
        }

        public void PlayEatenAliveSound()
        {
            mScene.PlaySound(Gender == AwesomePirates.Gender.Male ? "ScreamMan" : "ScreamWoman");
        }

        public override void AdvanceTime(double time)
        {
            X = PX;
            Y = PY;
        }

        public void Dock()
        {
            // Just to satisfy IPathFollower interface.
        }

        public void ResourceEventFiredWithKey(uint key, string type, object target)
        {
            switch (key)
            {
                case SharkCache.RESOURCE_KEY_SHARK_PERSON_TWEEN:
                    BodyShrunk();
                    break;
                case SharkCache.RESOURCE_KEY_SHARK_BLOOD_TWEEN:
                    BloodFaded();
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
                    mResources = cache.CheckoutPoolResourcesForKey("Overboard");
            }

            if (mResources == null)
		        Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED OVERBOARD CACHE _+_++_+_+_+_+_+_+");
            else
            {
                mResources.Client = this;
        
                if (mPersonClip == null)
                    mPersonClip = mResources.DisplayObjectForKey(SharkCache.RESOURCE_KEY_SHARK_PERSON) as SPMovieClip;
                if (mBlood == null)
                    mBlood = mResources.DisplayObjectForKey(SharkCache.RESOURCE_KEY_SHARK_BLOOD) as SPSprite;
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
                        if (mPredator != null)
                            Predator = null;

                        if (mState != OverboardActorState.Dead)
                        {
                            if (mPersonClip != null)
                            {
                                mPersonClip.RemoveFromParent();
                                mScene.Juggler.RemoveTweensWithTarget(mPersonClip);
                                mScene.Juggler.RemoveObject(mPersonClip);

                                if (mResources == null)
                                    mPersonClip.Dispose();
                                mPersonClip = null;
                            }

                            if (mState == OverboardActorState.Eaten)
                            {
                                if (mBlood != null)
                                {
                                    mBlood.RemoveFromParent();
                                    mScene.Juggler.RemoveTweensWithTarget(mBlood);
                                    mScene.SpriteLayerManager.RemoveChild(mBlood, (int)PFCat.BLOOD);
                                    mState = OverboardActorState.Dead;

                                    if (mResources == null)
                                        mBlood.Dispose();
                                    mBlood = null;
                                }
                            }
                        }

                        if (mDestination != null)
                        {
                            if (mDestination.PoolIndex != -1)
                                mDestination.Hibernate();
                            mDestination = null;
                        }

                        CheckinPooledResources();
                        mPrisoner = null;
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
