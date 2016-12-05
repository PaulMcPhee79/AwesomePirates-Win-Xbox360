using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class BlastProp : Prop, IResourceClient, IReusable
    {
        public enum BlastPropType
        {
            Abyssal = 1
        }

        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 15;
            uint reuseKey = (uint)BlastPropType.Abyssal;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = BlastPropWithType(BlastPropType.Abyssal, (int)PFCat.SEA);
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

        public static BlastProp BlastPropWithType(BlastPropType type, int category)
        {
            uint reuseKey = (uint)type;
            BlastProp prop = CheckoutReusable(reuseKey) as BlastProp;

            if (prop != null)
            {
                prop.Reuse();
            }
            else
            {
                //switch (type)
                prop = new AbyssalBlastProp(category);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed BlastProp ReusableCache.");
#endif
            }

            return prop;
        }

        private static BlastQueryCallback s_BlastQueryCB = null;
        protected static BlastQueryCallback BlastQueryCB
        {
            get
            {
                if (s_BlastQueryCB == null)
                    s_BlastQueryCB = new BlastQueryCallback();
                return s_BlastQueryCB;
            }
        }

        public BlastProp(int category, string resourceKey)
            : base(category)
        {
            mInUse = true;
            mPoolIndex = -1;
            mHasBlasted = false;
            mBlastScale = 1.5f;
            mBlastDuration = BlastProp.BlastAnimationDuration;
            mAftermathDuration = BlastProp.AftermathAnimationDuration;
            mBlastSound = null;
            mBlastTexture = null;
            mCostume = null;
        
            mResourceKey = resourceKey;
		    mResources = null;
            CheckoutPooledResources();
        }
        
        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        protected bool mHasBlasted;
        protected float mBlastScale;
        protected double mBlastDuration;
        protected double mAftermathDuration;
        protected string mBlastSound;
        protected SPTexture mBlastTexture;
        protected SPSprite mCostume;
        protected string mResourceKey;
        protected ResourceServer mResources;
        #endregion

        #region Properties
        public virtual uint ReuseKey { get { return 0; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public string BlastSound { get { return mBlastSound; } set { mBlastSound = value; } }
        public static float BlastAnimationDuration { get { return 0.15f; } }
        public static float AftermathAnimationDuration { get { return 0.9f; } }
        public SKTeamIndex SinkerID { get; set; }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume == null)
            {
                mCostume = new SPSprite();
        
                SPImage blastImage = new SPImage(mBlastTexture);
                blastImage.X = -blastImage.Width / 2;
                blastImage.Y = -blastImage.Height / 2;
                mCostume.AddChild(blastImage);
            }
    
            mCostume.Alpha = 0;
            mCostume.ScaleX = mCostume.ScaleY = mBlastScale;
            AddChild(mCostume);
        }

        public virtual void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mHasBlasted = false;
            mBlastDuration = BlastProp.BlastAnimationDuration;
            mAftermathDuration = BlastProp.AftermathAnimationDuration;
            CheckoutPooledResources();
            SetupProp();

            mInUse = true;
        }

        public virtual void Hibernate()
        {
            if (!InUse)
                return;

            if (mCostume != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mCostume);
                mCostume.RemoveFromParent();
                mCostume = null;
            }

            CheckinPooledResources();

            mInUse = false;
            CheckinReusable(this);
        }

        public void Blast()
        {
            if (mHasBlasted)
                return;
            mHasBlasted = true;
    
            if (mResources == null || !(mResources.StartTweenForKey(BlastCache.RESOURCE_KEY_BP_BLAST_TWEEN) && mResources.StartTweenForKey(BlastCache.RESOURCE_KEY_BP_AFTERMATH_TWEEN)))
            {
                mScene.Juggler.RemoveTweensWithTarget(mCostume);
        
                SPTween blastTween = new SPTween(mCostume, mBlastDuration);
                blastTween.AnimateProperty("Alpha", 1);
                mScene.Juggler.AddObject(blastTween);
        
                SPTween aftermathTween = new SPTween(mCostume, mAftermathDuration);
                aftermathTween.AnimateProperty("Alpha", 0);
                aftermathTween.Delay = blastTween.Delay + blastTween.TotalTime;
                mScene.Juggler.AddObject(aftermathTween);
        
                blastTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnBlasted);
                aftermathTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnAftermathCompleted);
            }

            PlayBlastSound();
        }

        protected void PlayBlastSound()
        {
            if (BlastSound != null)
                mScene.PlaySound(mBlastSound);
        }

        public virtual void BlastDamage() { }

        public void AfterMath()
        {
            mScene.RemoveProp(this);
        }

        protected virtual void OnBlasted(SPEvent ev)
        {
            BlastDamage();
        }

        protected virtual void OnAftermathCompleted(SPEvent ev)
        {
            AfterMath();
        }

        public virtual void ResourceEventFiredWithKey(uint key, string type, object target)
        {
            switch (key)
            {
                case BlastCache.RESOURCE_KEY_BP_BLAST_TWEEN:
                    BlastDamage();
                    break;
                case BlastCache.RESOURCE_KEY_BP_AFTERMATH_TWEEN:
                    AfterMath();
                    break;
                default:
                    break;
            }
        }

        public override void CheckoutPooledResources()
        {
            if (mResources == null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_BLAST_PROP);

                if (cache != null)
                    mResources = cache.CheckoutPoolResourcesForKey(mResourceKey);
            }

	        if (mResources == null)
		        Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED BLAST CACHE _+_++_+_+_+_+_+_+");
            else
            {
                mResources.Client = this;
        
                if (mCostume == null)
                    mCostume = mResources.DisplayObjectForKey(BlastCache.RESOURCE_KEY_BP_COSTUME) as SPSprite;
            }
        }

        public override void CheckinPooledResources()
        {
            if (mResources != null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_BLAST_PROP);

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
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mCostume);

                            if (mResources != null)
                                mCostume.RemoveFromParent();
                            mCostume = null;
                        }

                        CheckinPooledResources();
                        mBlastSound = null;
                        mBlastTexture = null;
                        mResourceKey = null;
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
