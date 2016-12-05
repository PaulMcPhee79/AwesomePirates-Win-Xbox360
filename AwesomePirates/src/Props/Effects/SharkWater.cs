using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class SharkWater : Prop, IResourceClient, IReusable
    {
        private const float kWaterRingDuration = 2f;
        private const uint kSharkWaterReuseKey = 1;

        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            // Splash
            int cacheSize = 15;
            uint reuseKey = kSharkWaterReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = SharkWaterAt(0, 0);
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

        public static SharkWater SharkWaterAt(float x, float y)
        {
            SharkWater sharkWater = CheckoutReusable(kSharkWaterReuseKey) as SharkWater;

            if (sharkWater != null)
            {
                sharkWater.Reuse();
                sharkWater.X = x;
                sharkWater.Y = y;
            }
            else
            {
                sharkWater = new SharkWater(x, y);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed SharkWater ReusableCache.");
#endif
            }

            return sharkWater;
        }

        public SharkWater(float x, float y)
            : base(PFCat.WAVES)
        {
            X = x;
		    Y = y;
            mInUse = true;
            mPoolIndex = -1;
            mHasPlayedEffect = false;
		    mWaterRing = null;
            mRipples = null;
		    mResources = null;
            CheckoutPooledResources();
            SetupProp();
        }
        
        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private bool mHasPlayedEffect;
        private SPSprite mWaterRing;
        private List<SPSprite> mRipples;
        private ResourceServer mResources;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kSharkWaterReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public static int NumRipples { get { return 3; } }
        public static float WaterRingDuration { get { return kWaterRingDuration; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mWaterRing == null)
                mWaterRing = new SPSprite();
	
	        if (mRipples == null)
            {
		        SPTexture texture = mScene.TextureByName("shark-white-water");
                int rippleCount = SharkWater.NumRipples;
                List<SPSprite> ripples = new List<SPSprite>(rippleCount);
	
		        for (int i = 0; i < rippleCount; ++i)
                {
			        SPSprite sprite = new SPSprite();
			        SPImage image = new SPImage(texture);
			        image.X = -image.Width / 2;
			        image.Y = -image.Height / 2;
			        sprite.ScaleX = 0.01f;
			        sprite.ScaleY = 0.01f;
                    sprite.AddChild(image);
                    ripples.Add(sprite);
                    mWaterRing.AddChild(sprite);
		        }
        
                mRipples = ripples;
	        }
            else
            {
		        foreach (SPSprite ripple in mRipples)
                {
                    ripple.ScaleX = 0.01f;
                    ripple.ScaleY = 0.01f;
                    ripple.Alpha = 1;
			        mWaterRing.AddChild(ripple);
                }
	        }
	
	        mWaterRing.Visible = false;
            AddChild(mWaterRing);
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mHasPlayedEffect = false;
            mRipples = null;
            mResources = null;
            CheckoutPooledResources();
            SetupProp();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mScene != null && mRipples != null && mRipples.Count > 0)
            {
                // Kill the tween that has us as an event listener.
                SPSprite sprite = mRipples[mRipples.Count - 1];
                mScene.Juggler.RemoveTweensWithTarget(sprite);
            }

            if (mWaterRing != null)
            {
                mWaterRing.RemoveAllChildren();
                mWaterRing.Visible = false;
            }

            CheckinPooledResources();
            mRipples = null;

            mInUse = false;

            CheckinReusable(this);
        }

        public void PlayEffect()
        {
            if (mHasPlayedEffect)
                return;
    
	        SPTween tween = null;
	        float delay = 0.0f;
	
            uint index = 0;
    
	        foreach (SPSprite sprite in mRipples)
            {
                if (mResources == null || !mResources.StartTweenForKey(SharkCache.RESOURCE_KEY_SHARK_RIPPLES_TWEEN + index))
                {
                    tween = new SPTween(sprite, kWaterRingDuration);
                    tween.AnimateProperty("Alpha", 0f);
                    tween.AnimateProperty("ScaleX", 1f);
                    tween.AnimateProperty("ScaleY", 1f);
                    tween.Delay = delay;
                    mScene.Juggler.AddObject(tween);
                    delay += 0.5f;
            
                    if (index == mRipples.Count-1)
                        tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnWaterRingFaded);
                }
                ++index;
	        }
		
	        mWaterRing.Visible = true;
            mHasPlayedEffect = true;
        }

        private void WaterRingFaded()
        {
            mScene.RemoveProp(this);
        }

        private void OnWaterRingFaded(SPEvent ev)
        {
            WaterRingFaded();
        }

        public void ResourceEventFiredWithKey(uint key, string type, object target)
        {
            switch (key)
            {
                case SharkCache.RESOURCE_KEY_SHARK_RIPPLES:
                    break;
                case SharkCache.RESOURCE_KEY_SHARK_RIPPLES_TWEEN:
                default:
                    if (key >= SharkCache.RESOURCE_KEY_SHARK_RIPPLES_TWEEN)
                        WaterRingFaded();
                    break;
            }
        }

        public override void CheckoutPooledResources()
        {
            if (mResources == null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_SHARK);

                if (cache != null)
                    mResources = cache.CheckoutPoolResourcesForKey("SharkWater");
            }

	        if (mResources == null)
		        Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED SHARK WATER CACHE _+_++_+_+_+_+_+_+");
            else
            {
                mResources.Client = this;
        
                if (mRipples == null)
                    mRipples = mResources.MiscResourceForKey(SharkCache.RESOURCE_KEY_SHARK_RIPPLES) as List<SPSprite>;
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
                        if (mRipples != null && mRipples.Count > 0)
                        {
                            // Kill the tween that has us as an event listener.
                            SPSprite sprite = mRipples[mRipples.Count - 1];
                            mScene.Juggler.RemoveTweensWithTarget(sprite);
                        }

                        if (mWaterRing != null)
                        {
                            if (mResources != null)
                                mWaterRing.RemoveFromParent();
                            mWaterRing = null;
                        }

                        CheckinPooledResources();
                        mRipples = null;
                    }
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
