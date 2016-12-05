
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class Wake : Prop, IReusable
    {
        public enum WakeState
        {
            Idle = 0,
            Active,
            Dying,
            Dead
        }

        public const int kRippleCount = 20;
        private const uint kWakeReuseKey = 1;

        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            // Splash
            int cacheSize = 40;
            uint reuseKey = kWakeReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = WakeWithCategory((int)PFCat.SEA, kRippleCount);
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

        public static Wake WakeWithCategory(int category, int numRipples)
        {
            Wake wake = CheckoutReusable(kWakeReuseKey) as Wake;

            if (wake != null)
            {
                wake.Category = category;
                wake.Reuse();
            }
            else
            {
                wake = new Wake(category, numRipples);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed Wake ReusableCache.");
#endif
            }

            return wake;
        }

        public Wake(int category, int numRipples)
            : base(category)
        {
            mAdvanceable = true;
		    mState = WakeState.Idle;
            mInUse = true;
            mPoolIndex = -1;
            mResourcePoolIndex = -1;
		    mNumRipples = numRipples;
		    mRipplePeriod = Wake.DefaultRipplePeriod;
            mSpeedFactor = 1f;
            mCustomColored = false;
		    mRipples = new RingBuffer(numRipples);
		    mVisibleRipples = new List<SPSprite>(numRipples);
            mCachedRipples = null;
            SetupProp();
        }
        
        #region Fields
        private WakeState mState;
        private bool mInUse;
        private int mPoolIndex;
        private int mResourcePoolIndex;
        private int mNumRipples;
        private double mRipplePeriod;
        private float mSpeedFactor;
        private bool mCustomColored;
        private RingBuffer mRipples;
        private List<SPSprite> mCachedRipples;
        private List<SPSprite> mVisibleRipples;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kWakeReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public double RipplePeriod { get { return mRipplePeriod; } set { mRipplePeriod = Math.Max(Wake.MinRipplePeriod, value); } }
        public float SpeedFactor { get { return mSpeedFactor; } set { mSpeedFactor = value; } }
        public static int DefaultWakeBufferSize { get { return kRippleCount; } }
        public static int MaxWakeBufferSize { get { return kRippleCount; } }
        public static float DefaultWakePeriod { get { return 12f; } }
        public static double DefaultRipplePeriod { get { return 1.5; } } // 1.75
        public static double MaxRipplePeriod { get { return 2.0; } }
        public static double MinRipplePeriod { get { return 0.75; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mState != WakeState.Idle)
		        return;
	
	        SPSprite rippleSprite = null;
	        SPImage rippleImage = null;
            SPTexture wakeTexture = mScene.TextureByName("wake");
	        float widthCache = wakeTexture.Width, heightCache = wakeTexture.Height;

            WakeCache cache = (mScene.CacheManagerForKey(CacheManager.CACHE_WAKE) as WakeCache);

            if (cache != null)
                mCachedRipples = cache.CheckoutRipples(mNumRipples, out mResourcePoolIndex);

            if (mCachedRipples != null)
            {
                foreach (SPSprite sprite in mCachedRipples)
                {
                    sprite.Visible = false;
                    AddChild(sprite);
                }

                mRipples.AddItems(mCachedRipples);
            }
            else
            {
                for (int i = mRipples.Count; i < mNumRipples; ++i)
                {
                    if (i == mNumRipples - 1)
                        Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED WAKE CACHE _+_++_+_+_+_+_+_+");
                    rippleSprite = new SPSprite();
                    rippleSprite.Visible = false;
                    rippleImage = new SPImage(wakeTexture);
                    rippleImage.X = -widthCache / 2;
                    rippleImage.Y = -heightCache / 2;
                    rippleSprite.AddChild(rippleImage);
                    mRipples.AddItem(rippleSprite);
                    AddChild(rippleSprite);
                }
            }

            SetState(WakeState.Active);
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mState = WakeState.Idle;
            mResourcePoolIndex = -1;
            mRipplePeriod = Wake.DefaultRipplePeriod;
            mSpeedFactor = 1f;

            if (mRipples == null)
                mRipples = new RingBuffer(mNumRipples);
            if (mVisibleRipples == null)
                mVisibleRipples = new List<SPSprite>(mNumRipples);
            mCachedRipples = null;

            Alpha = 1f;
            SetupProp();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mScene != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(this);

                if (mCachedRipples != null)
                {
                    WakeCache cache = mScene.CacheManagerForKey(CacheManager.CACHE_WAKE) as WakeCache;

                    if (cache != null && mResourcePoolIndex != -1)
                    {
                        if (mCustomColored)
                        {
                            CustomizeColor(Color.White);
                            mCustomColored = false;
                        }

                        cache.CheckinRipples(mCachedRipples, mResourcePoolIndex);
                        mResourcePoolIndex = -1;
                    }
                    mCachedRipples = null;
                }
            }

            if (mRipples != null)
                mRipples.Clear();
            if (mVisibleRipples != null)
                mVisibleRipples.Clear();

            mInUse = false;

            CheckinReusable(this);
        }

        private void SetState(WakeState state)
        {
            if (state == mState)
		        return;
	
	        switch (state)
            {
		        case WakeState.Idle:
			        break;
		        case WakeState.Active:
			        break;
		        case WakeState.Dying:
			        break;
		        case WakeState.Dead:
                    mScene.RemoveProp(this);
			        break;
		        default:
			        break;
	        }
	
	        mState = state;
        }

        public void CustomizeColor(Color color)
        {
            if (mRipples == null)
                return;

            List<SPDisplayObject> ripples = mRipples.AllItems;

            if (ripples != null)
            {
                foreach (SPDisplayObject ripple in ripples)
                {
                    if (ripple is SPSprite)
                    {
                        SPSprite sprite = ripple as SPSprite;

                        if (sprite.NumChildren > 0 && sprite.ChildAtIndex(0) is SPImage)
                        {
                            SPImage image = sprite.ChildAtIndex(0) as SPImage;
                            image.Color = color;
                        }
                    }
                }

                mCustomColored = true;
            }
        }

        public void NextRippleAt(float x, float y, float rotation)
        {
            if (mState != WakeState.Active)
		        return;
	
	        SPSprite ripple = mRipples.NextItem as SPSprite;
	        ripple.Visible = true;
	        ripple.Rotation = rotation;
	        ripple.X = x;
	        ripple.Y = y;
	        ripple.Alpha = 1.0f;
            ripple.ScaleX = 0.25f;
	
	        if (!mVisibleRipples.Contains(ripple))
                mVisibleRipples.Add(ripple);
        }

        private void FadeRipplesAfterTime(double time)
        {
            bool performExpensiveTest = true;
	        float alphaAdjust = (float)(time / (2 * mRipplePeriod));
	        float scaleAdjust = mSpeedFactor * (float)(1.6 * (time / mRipplePeriod));
	
	        for (int i = mVisibleRipples.Count-1; i >= 0; --i)
            {
		        SPSprite ripple = mVisibleRipples[i];
                ripple.Alpha -= (1.75f - ripple.Alpha) * alphaAdjust;
                ripple.ScaleX = Math.Min(3.5f, ripple.ScaleX + (0.6f / ripple.ScaleX * ripple.ScaleX * ripple.ScaleX) * scaleAdjust);

                //if (ripple.Alpha > 0.925f)
                    //ripple.ScaleX += 2.5f * scaleAdjust;
                if (ripple.Alpha > 0.9f)
                    ripple.ScaleX += 2.5f * scaleAdjust;
		
		        if (performExpensiveTest)
                {
			        if (SPMacros.SP_IS_FLOAT_EQUAL(0, ripple.Alpha))
                    {
				        ripple.Visible = false;
                        mVisibleRipples.RemoveAt(i);
			        }
                    else
                    {
                        // If this ripple is still visible, so will be all younger ripples.
				        performExpensiveTest = false;
			        }
		        }
	        }
        }

#if false
        public override void Draw(GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            PreDraw(support);

            float alpha = Alpha;
            CustomDrawer customDrawer = mScene.CustomDrawer;
            customDrawer.RefractionFactor = 0.2f;

            Matrix globalTransform = TransformationMatrix * parentTransform;

            foreach (SPSprite ripple in mVisibleRipples)
            {
                float childAlpha = ripple.Alpha;
                ripple.Alpha *= alpha;
                
                SPImage rippleImage = ripple.ChildAtIndex(0) as SPImage;
                float grandChildAlpha = rippleImage.Alpha;
                rippleImage.Alpha *= ripple.Alpha;
                customDrawer.RefractionDraw(rippleImage, gameTime, support, ripple.TransformationMatrix * globalTransform);

                rippleImage.Alpha = grandChildAlpha;
                ripple.Alpha = childAlpha;
            }

            PostDraw(support);
        }
#endif

        public override void AdvanceTime(double time)
        {
            if (mState == WakeState.Dead)
		        return;
	
            FadeRipplesAfterTime(time);
	
	        if (mState == WakeState.Dying && mVisibleRipples.Count == 0)
                SetState(WakeState.Dead);
        }

        public void SafeDestroy()
        {
            if (mState != WakeState.Dead)
                SetState(WakeState.Dying);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mScene != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(this);

                            if (mCachedRipples != null)
                            {
                                WakeCache cache = mScene.CacheManagerForKey(CacheManager.CACHE_WAKE) as WakeCache;

                                if (cache != null && mResourcePoolIndex != -1)
                                {
                                    if (mCustomColored)
                                    {
                                        CustomizeColor(Color.White);
                                        mCustomColored = false;
                                    }

                                    cache.CheckinRipples(mCachedRipples, mResourcePoolIndex);
                                    mResourcePoolIndex = -1;
                                }
                                mCachedRipples = null;
                            }
                        }

                        mRipples = null;
                        mVisibleRipples = null;
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
