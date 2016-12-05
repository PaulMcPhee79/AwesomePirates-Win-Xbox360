using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class LootProp : Prop, IReusable
    {
        public enum LootPropType
        {
            Prisoner = 1
        }

        public static Vector2 CommonLootDestination = Vector2.Zero;

        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 10;
            uint reuseKey = (uint)LootPropType.Prisoner;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = LootPropWithType(LootPropType.Prisoner, (int)PFCat.DECK);
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

        public static LootProp LootPropWithType(LootPropType type, int category)
        {
            uint reuseKey = (uint)type;
            LootProp prop = CheckoutReusable(reuseKey) as LootProp;

            if (prop != null)
            {
                prop.Reuse();
            }
            else
            {
                //switch (type)
                prop = new PrisonerProp(category);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed LootProp ReusableCache.");
#endif
            }

            return prop;
        }

        protected const int kLootAlphaTweenerIndex = 0;
        protected const int kLootScaleTweenerIndex = 1;
        protected const int kLootXTweenerIndex = 2;
        protected const int kLootYTweenerIndex = 3;

        public LootProp(int category)
            : base(category)
        {
            mAdvanceable = true;
		    mLooted = false;
            mInUse = true;
            mPoolIndex = -1;
		    mAlphaFrom = 1f;
		    mAlphaTo = 0;
		    mScaleFrom = 0.01f;
		    mScaleTo = 1.25f;
            mTranslateFrom = Vector2.Zero;
            mTranslateTo = Vector2.Zero;
            mDuration = 1.25;
            mLootSfxKey = null;
            mWardrobe = null;
        }
        
        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        protected bool mLooted;
        protected float mAlphaFrom;
        protected float mAlphaTo;
        protected float mScaleFrom;
        protected float mScaleTo;
        protected Vector2 mTranslateFrom;
        protected Vector2 mTranslateTo;
        protected double mDuration;
        protected SPImage mCostume;
        protected SPSprite mWardrobe;
        protected string mLootSfxKey;

        protected FloatTweener[] mLootTweeners;
        #endregion

        #region Properties
        public virtual uint ReuseKey { get { return 0; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public static float LootAnimationDuration { get { return 1.25f; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            Debug.Assert(mCostume != null, "LootProp derived classes must create a costume.");

            if (mWardrobe == null)
            {
                mWardrobe = new SPSprite();
                mWardrobe.ScaleX = mWardrobe.ScaleY = mScaleFrom;
                mWardrobe.Alpha = mAlphaFrom;

                if (mCostume != null)
                    mWardrobe.AddChild(mCostume);
                AddChild(mWardrobe);
            }

            if (mLootTweeners == null)
            {
                mLootTweeners = new FloatTweener[]
                {
                    new FloatTweener(mAlphaFrom, SPTransitions.SPEaseIn, new Action(OnLooted)),
                    new FloatTweener(mScaleFrom, SPTransitions.SPEaseOut),
                    new FloatTweener(mTranslateFrom.X, SPTransitions.SPEaseOut),
                    new FloatTweener(mTranslateFrom.Y, SPTransitions.SPEaseOut)
                };
            }
        }

        public virtual void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mLooted = false;
            mDuration = 1.25;
            
            SetupProp();

            mInUse = true;
        }

        public virtual void Hibernate()
        {
            if (!InUse)
                return;

            mInUse = false;
            CheckinReusable(this);
        }

        public void SetPosition(Vector2 origin, Vector2 dest)
        {
            mTranslateFrom = origin;
            mTranslateTo = dest;
        }

        public void PlayLootSound()
        {
            if (mLootSfxKey != null)
		        mScene.PlaySound(mLootSfxKey);
        }

        public override void AdvanceTime(double time)
        {
            if (mDuration > 0.0)
            {
                mDuration -= time;

                if (mDuration <= 0.0)
                    Loot();
            }

            if (mLooted && mLootTweeners != null)
            {
                FloatTweener tweener = null;
                for (int i = kLootAlphaTweenerIndex; i <= kLootYTweenerIndex; ++i)
                {
                    tweener = mLootTweeners[i];

                    switch (i)
                    {
                        case kLootAlphaTweenerIndex:
                            {
                                tweener.AdvanceTime(time);
                                if (!tweener.Delaying && mWardrobe.Alpha != tweener.TweenedValue)
                                    mWardrobe.Alpha = tweener.TweenedValue;
                            }
                            break;
                        case kLootScaleTweenerIndex:
                            {
                                tweener.AdvanceTime(time);
                                if (!tweener.Delaying && mWardrobe.ScaleX != tweener.TweenedValue)
                                {
                                    mWardrobe.ScaleX = tweener.TweenedValue;
                                    mWardrobe.ScaleY = tweener.TweenedValue;
                                }
                            }
                            break;
                        case kLootXTweenerIndex:
                            {
                                tweener.AdvanceTime(time);
                                if (!tweener.Delaying && X != tweener.TweenedValue)
                                    X = tweener.TweenedValue;
                            }
                            break;
                        case kLootYTweenerIndex:
                            {
                                tweener.AdvanceTime(time);
                                if (!tweener.Delaying && Y != tweener.TweenedValue)
                                    Y = tweener.TweenedValue;
                            }
                            break;
                    }
                }
            }
        }

        public virtual void Loot()
        {
            if (mLooted)
		        return;
	        mLooted = true;
	        mWardrobe.Alpha = mAlphaFrom;
            mWardrobe.ScaleX = mWardrobe.ScaleY = mScaleFrom;
            X = mTranslateFrom.X;
            Y = mTranslateFrom.Y;

	        Visible = true;
            PlayLootSound();

            float lootAnimationDuration = LootProp.LootAnimationDuration;
            mLootTweeners[kLootAlphaTweenerIndex].Reset(mWardrobe.Alpha, mAlphaTo, lootAnimationDuration, 0.5);
            mLootTweeners[kLootScaleTweenerIndex].Reset(mWardrobe.ScaleX, mScaleTo, lootAnimationDuration);
            mLootTweeners[kLootXTweenerIndex].Reset(X, mTranslateTo.X, lootAnimationDuration);
            mLootTweeners[kLootYTweenerIndex].Reset(Y, mTranslateTo.Y, lootAnimationDuration);
        }

        public void DestroyLoot()
        {
            mScene.RemoveProp(this);
        }

        private void OnLooted()
        {
            DestroyLoot();
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mLootTweeners != null)
                        {
                            foreach (FloatTweener tweener in mLootTweeners)
                            {
                                if (tweener != null)
                                    tweener.TweenComplete = null;
                            }

                            mLootTweeners = null;
                        }
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
