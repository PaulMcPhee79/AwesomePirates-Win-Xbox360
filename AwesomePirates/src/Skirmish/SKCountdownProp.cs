using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class SKCountdownProp : Prop
    {
        public SKCountdownProp(int category, int max)
            : base(category)
        {
            mHasPlayedConclusionSequence = false;
            mValue = -1;
            mMax = Math.Min(99, Math.Max(0, max));
            SetupProp();
        }

        #region Fields
        private bool mHasPlayedConclusionSequence;
        private int mValue;
        private int mMax;
        private SPSprite[] mNumerals;
        private SPSprite mConclusion;
        private SPSprite mCanvas;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvas != null)
                return;

            mCanvas = new SPSprite();
            mCanvas.X = mScene.ViewWidth / 2;
            mCanvas.Y = 0.4f * mScene.ViewHeight;
            AddChild(mCanvas);

            mNumerals = new SPSprite[mMax];
            for (int i = 0; i < mMax; ++i)
            {
                SPSprite numeral = GuiHelper.CountdownSpriteForValue(i + 1, mScene);
                numeral.X = -numeral.Width / 2;
                numeral.Y = -numeral.Height / 2;
                
                SPSprite sprite = new SPSprite();
                sprite.ScaleX = sprite.ScaleY = 1.5f;
                sprite.Visible = false;
                sprite.AddChild(numeral);

                mNumerals[i] = sprite;
                mCanvas.AddChild(sprite);
            }

            SPImage conclusionImage = new SPImage(mScene.TextureByName("sk-text-fight"));
            conclusionImage.X = -conclusionImage.Width / 2;
            conclusionImage.Y = -conclusionImage.Height / 2;

            mConclusion = new SPSprite();
            mConclusion.Visible = false;
            mConclusion.AddChild(conclusionImage);
            mCanvas.AddChild(mConclusion);

            Flip(mScene.Flipped);
        }

        public override void Flip(bool enable)
        {
            if (mCanvas != null)
                mCanvas.ScaleX = (enable) ? -1 : 1;
        }

        public void SetCountdownValue(int value)
        {
            if (value <= 0 || value > mMax || value == mValue)
                return;

            if (mValue != -1)
                mNumerals[mValue - 1].Visible = false;

            mValue = value;
            mNumerals[mValue - 1].Visible = true;
            mScene.PlaySound("Heartbeat");
        }

        public void PlayConclusionSequence(float delay = 0f)
        {
            if (mHasPlayedConclusionSequence)
                return;
            mHasPlayedConclusionSequence = true;

            if (mValue != -1)
                mNumerals[mValue - 1].Visible = false;
            mConclusion.Visible = false;
            StampAnimationWithStamp(mConclusion, 0.1f, delay);

            SPTween tween = new SPTween(mConclusion, 1f);
            tween.AnimateProperty("Alpha", 0f);
            tween.Delay = delay + 2f;
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnConcluded);
            mScene.Juggler.AddObject(tween);
        }

        private void StampAnimationWithStamp(SPSprite stamp, float duration, float delay)
        {
            float oldScaleX = stamp.ScaleX, oldScaleY = stamp.ScaleY;

            stamp.ScaleX = 3.0f;
            stamp.ScaleY = 3.0f;

            mScene.Juggler.RemoveTweensWithTarget(stamp);

            SPTween tween = new SPTween(stamp, duration);
            tween.AnimateProperty("ScaleX", oldScaleX);
            tween.AnimateProperty("ScaleY", oldScaleY);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_STARTED, (SPEventHandler)OnStamping);
            tween.Delay = delay;
            mScene.Juggler.AddObject(tween);
        }

        private void OnStamping(SPEvent ev)
        {
            mConclusion.Visible = true;
            mScene.PlaySound("Stamp");
        }

        private void OnConcluded(SPEvent ev)
        {
            mScene.RemoveProp(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mConclusion != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mConclusion);
                            mConclusion = null;
                        }

                        mCanvas = null;
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
