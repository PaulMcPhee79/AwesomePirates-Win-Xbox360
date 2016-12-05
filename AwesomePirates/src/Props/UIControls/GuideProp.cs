using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class GuideProp : Prop
    {
        public const int kPlayerOneIndex = (1 << 0);
        public const int kPlayerTwoIndex = (1 << 1);
        public const int kPlayerThreeIndex = (1 << 2);
        public const int kPlayerFourIndex = (1 << 3);
        public const int kAllPlayerIndexes = 0xf;

        private const float kShowTweenDuration = 0.1f;
        private const float kHideTweenDuration = 0.5f;

        public static int NumPlayersSetInMap(int map)
        {
            int numPlayersSet = 0;

            for (int i = 0; i < 4; ++i)
            {
                if ((map & (1 << i)) == (1 << i))
                    ++numPlayersSet;
            }

            return numPlayersSet;
        }

        public GuideProp(int category)
            : base(category)
        {
            mPlayerIndexMap = 0;
            mShowDuration = 0f;
            mShowTween = mHideTween = null;
            mCostume = null;
            mIndicators = new SPImage[4];
            SetupProp();
        }

        #region Fields
        private int mPlayerIndexMap;
        private float mShowDuration;
        private SPImage[] mIndicators;
        private SPSprite mCostume;
        private SPTween mShowTween;
        private SPTween mHideTween;
        #endregion

        #region Properties
        public int PlayerIndexMap { get { return mPlayerIndexMap; } set { mPlayerIndexMap = value; UpdateDisplay(); } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;

            mCostume = new SPSprite();
            AddChild(mCostume);

            SPImage baseImage = new SPImage(mScene.TextureByName("pf-base"));
            baseImage.X = -baseImage.Width / 2;
            baseImage.Y = -baseImage.Height / 2;
            mCostume.AddChild(baseImage);

            for (int i = 0; i < 4; ++i)
            {
                SPImage image = new SPImage(mScene.TextureByName("pf-" + (i + 1)));
                image.X = -image.Width / 2;
                image.Y = -image.Height / 2;
                image.Visible = false;
                mCostume.AddChild(image);
                mIndicators[i] = image;
            }
        }

        private void UpdateDisplay()
        {
            for (int i = 0; i < mIndicators.Length; ++i)
                mIndicators[i].Visible = (mPlayerIndexMap & (1 << i)) == (1 << i);
        }

        public void ShowForDuration(float duration)
        {
            mScene.SpecialJuggler.RemoveTweensWithTarget(this);

            if (mShowTween == null)
            {
                mShowTween = new SPTween(this, kShowTweenDuration);
                mShowTween.AnimateProperty("Alpha", 1f);
                mShowTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnShown);
            }
            else
                mShowTween.Reset(Alpha * kShowTweenDuration, kShowTweenDuration);

            mShowDuration = duration;
            Visible = true;
            mScene.SpecialJuggler.AddObject(mShowTween);
        }

        private void HideAfterDelay(float delay)
        {
            mScene.SpecialJuggler.RemoveTweensWithTarget(this);

            if (mHideTween == null)
            {
                mHideTween = new SPTween(this, Alpha * kHideTweenDuration);
                mHideTween.AnimateProperty("Alpha", 0f);
                mHideTween.Delay = delay;
                mHideTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnHidden);
            }
            else
            {
                mHideTween.Reset(-delay, Alpha * kHideTweenDuration);
            }

            Visible = true;
            mScene.SpecialJuggler.AddObject(mHideTween);
        }

        public void Hide()
        {
            mScene.SpecialJuggler.RemoveTweensWithTarget(this);
            Visible = false;
            Alpha = 0f;
        }

        private void OnShown(SPEvent ev)
        {
            HideAfterDelay(mShowDuration);
        }

        private void OnHidden(SPEvent ev)
        {
            Visible = false;
            Alpha = 0f;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mShowTween != null || mHideTween != null)
                        {
                            mScene.SpecialJuggler.RemoveTweensWithTarget(this);
                            mShowTween = null;
                            mHideTween = null;
                        }

                        mIndicators = null;
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
