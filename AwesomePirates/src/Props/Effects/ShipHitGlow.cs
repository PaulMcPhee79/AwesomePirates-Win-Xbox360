using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class ShipHitGlow : Prop, IReusable
    {
        private const float kGlowAlphaMin = 0f;
        private const float kGlowAlphaMax = 0.25f;
        private const float kGlowScale = 1.5f;

        private const float kGlowLongDuration = 0.175f;
        private const float kGlowShortDuration = 0.075f;
        private static readonly float[] kDurations = new float[] { kGlowLongDuration, kGlowShortDuration, kGlowShortDuration, kGlowLongDuration };

        private const uint kShipHitGlowReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 25;
            uint reuseKey = kShipHitGlowReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = ShipHitGlowAt(0, 0);
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

        public static ShipHitGlow ShipHitGlowAt(float x, float y)
        {
            ShipHitGlow glow = CheckoutReusable(kShipHitGlowReuseKey) as ShipHitGlow;

            if (glow != null)
            {
                glow.Reuse();
                glow.X = x;
                glow.Y = y;
            }
            else
            {
                glow = new ShipHitGlow(x, y);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed ShipHitGlow ReusableCache.");
#endif
            }

            return glow;
        }

        public ShipHitGlow(float x, float y)
            : base(PFCat.SHOREBREAK)
        {
            X = x; Y = y;
            mInUse = true;
            mPoolIndex = -1;
            mGlowImage = null;
            mTweens = null;
            SetupProp();
        }

        private bool mInUse;
        private int mPoolIndex;
        private int mState;
        private float[] mTargets;
        private SPImage mGlowImage;
        private SPTween[] mTweens;

        public uint ReuseKey { get { return kShipHitGlowReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }
        public bool IsCompleted { get { return (mState >= 4); } }

        protected override void SetupProp()
        {
            GameController gc = GameController.GC;

            float glowAlphaMax = 0f;
            float glowAlphaPulse = 0f;
            float proportionRemaining = gc.TimeKeeper.ProportionRemaining;

            switch (gc.TimeOfDay)
            {
                case TimeOfDay.DawnTransition:
                    Alpha = Math.Max(0.6f, proportionRemaining);
                    break;
                case TimeOfDay.Dawn:
                    Alpha = 0.5f;
                    break;
                case TimeOfDay.Dusk:
                    Alpha = 0.5f;
                    break;
                case TimeOfDay.EveningTransition:
                    Alpha = Math.Max(0.6f, 0.9f - proportionRemaining);
                    break;
                case TimeOfDay.Evening:
                case TimeOfDay.Midnight:
                    Alpha = 1f;
                    break;
                default:
                    Alpha = 0f;
                    break;
            }

            glowAlphaMax = 0.4f;
            glowAlphaPulse = 0.25f;

            if (mTargets == null)
                mTargets = new float[] { kGlowAlphaMax + glowAlphaMax, kGlowAlphaMax + glowAlphaMax - glowAlphaPulse, kGlowAlphaMax + glowAlphaMax, kGlowAlphaMin };

            if (mGlowImage == null)
            {
                mGlowImage = new SPImage(mScene.TextureByName("explosion-glow"));
                mGlowImage.X = -mGlowImage.Width / 2;
                mGlowImage.Y = -mGlowImage.Height / 2;
                AddChild(mGlowImage);
            }

            mGlowImage.Alpha = kGlowAlphaMin;

            if (mTweens == null)
            {
                mTweens = new SPTween[kDurations.Length];

                for (int i = 0; i < mTweens.Length; ++i)
                {
                    SPTween tween = new SPTween(mGlowImage, kDurations[i], SPTransitions.SPLinear);
                    tween.AnimateProperty("Alpha", mTargets[i]);
                    tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnTweenCompleted));
                    mTweens[i] = tween;
                }
            }

            Visible = true;
            ScaleX = ScaleY = kGlowScale;
            mState = 0;

            if (!sCaching)
            {
                mScene.AddProp(this);
                NextGlowCycle();
            }
        }

        public void Reuse()
        {
            if (InUse)
                return;

            SetupProp();
            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mScene != null)
                mScene.Juggler.RemoveTweensWithTarget(this);
            mInUse = false;

            CheckinReusable(this);
        }

        private void NextGlowCycle()
        {
            mTweens[mState].Reset();
            mScene.Juggler.AddObject(mTweens[mState]);
        }

        private void OnTweenCompleted(SPEvent ev)
        {
            if (++mState < 4)
                NextGlowCycle();
            else
                Visible = false;
        }

        public void ReRun()
        {
            if (!IsCompleted)
                return;
            mState = 0;
            Visible = true;
            NextGlowCycle();
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mGlowImage != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mGlowImage);
                            mGlowImage = null;
                        }

                        if (mTweens != null)
                        {
                            foreach (SPTween tween in mTweens)
                                tween.RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mTweens = null;
                        }
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
    }
}
