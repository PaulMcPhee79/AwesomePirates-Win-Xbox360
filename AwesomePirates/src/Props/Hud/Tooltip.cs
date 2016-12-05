using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class Tooltip : Prop
    {
        public Tooltip(int category, float scrollAlpha = 1f, float tipAlpha = 1f, double displayTime = 0.25, double hideTime = 0.25)
            : base(category)
        {
            mAdvanceable = true;
            mSlowable = false;
            mPrevKey = 0;
            mScrollAlpha = scrollAlpha;
            mTipAlpha = tipAlpha;
            mDisplayTime = displayTime;
            mHideTime = hideTime;
            mTitleColor = SPUtils.ColorFromColor(0x652ba7);
            mTextColor = Color.Black;
            mScrollScales = new Dictionary<uint, Vector2>(10);
            mTips = new Dictionary<uint, SPSprite>(10);
            mTweener = new FloatTweener(0f, SPTransitions.SPLinear);
            mTweener.TweenComplete = new Action(OnHidden);
            SetupProp();
            Visible = false;
        }

        #region Fields
        protected uint mPrevKey;
        protected float mScrollAlpha;
        protected float mTipAlpha;
        protected float mScrollImageHeight;
        protected double mDisplayTime;
        protected double mHideTime;

        protected Color mTitleColor;
        protected Color mTextColor;

        protected SPImage mScrollImage;
        protected SPSprite mScroll;
        protected SPSprite mCanvas;
        protected Dictionary<uint, Vector2> mScrollScales;
        protected Dictionary<uint, SPSprite> mTips;
        protected FloatTweener mTweener;
        #endregion

        #region Properties
        public Color TitleColor
        {
            get { return mTitleColor; }
            set
            {
                mTitleColor = value;

                if (mTips != null)
                {
                    foreach (KeyValuePair<uint, SPSprite> kvp in mTips)
                    {
                        if (kvp.Value.NumChildren > 0 && kvp.Value.ChildAtIndex(0) is SPTextField)
                        {
                            SPTextField title = kvp.Value.ChildAtIndex(0) as SPTextField;
                            title.Color = value;
                        }
                    }
                }
            }
        }
        public Color TextColor
        {
            get { return mTextColor; }
            set
            {
                mTextColor = value;

                if (mTips != null)
                {
                    foreach (KeyValuePair<uint, SPSprite> kvp in mTips)
                    {
                        if (kvp.Value.NumChildren > 1 && kvp.Value.ChildAtIndex(1) is SPTextField)
                        {
                            SPTextField text = kvp.Value.ChildAtIndex(1) as SPTextField;
                            text.Color = value;
                        }
                    }
                }
            }
        }
        private float DefaultScrollScaleY { get { return 340.0f / mScrollImageHeight; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvas != null)
                return;

            mCanvas = new SPSprite();
            AddChild(mCanvas);

            // Background scroll
            SPTexture scrollTexture = GuiHelper.CachedScrollTextureByName("scroll-quarter-small", mScene);
            mScrollImage = new SPImage(scrollTexture);
            mScrollImage.X = -mScrollImage.Width / 2;
            mScrollImage.Y = -mScrollImage.Height / 2;
            mScrollImageHeight = mScrollImage.Height;

            mScroll = new SPSprite();
            mScroll.ScaleX = 360.0f / mScrollImage.Width;
            mScroll.ScaleY = 340.0f / mScrollImageHeight;
            mScroll.Alpha = mScrollAlpha;
            mScroll.AddChild(mScrollImage);
            mCanvas.AddChild(mScroll);
        }

        protected virtual SPSprite CreateTip(string title, string text)
        {
            SPTextField titleField = new SPTextField(320, 56, title, mScene.FontKey, 42);
            titleField.HAlign = SPTextField.SPHAlign.Center;
            titleField.VAlign = SPTextField.SPVAlign.Top;
            titleField.Color = mTitleColor;

            SPTextField textField = new SPTextField(310, 200, text, mScene.FontKey, 32);
            textField.HAlign = SPTextField.SPHAlign.Center;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = mTextColor;
            textField.X = (titleField.Width - textField.Width) / 2;
            textField.Y = titleField.Y + titleField.Height + 8;

            SPSprite tip = new SPSprite();
            tip.AddChild(titleField);
            tip.AddChild(textField);
            tip.X = -mScroll.Width / 2 + (mScroll.Width - tip.Width) / 2;
            tip.Y = -mScroll.Height / 2 + 12;
            tip.Alpha = mTipAlpha;
            return tip;
        }

        protected virtual float ScrollScaleForTip(SPSprite tip)
        {
            if (mScrollImage == null || tip == null || tip.NumChildren < 2)
                return DefaultScrollScaleY;

            SPDisplayObject tipDesc = tip.ChildAtIndex(1);
            if (tipDesc is SPTextField == false)
                return DefaultScrollScaleY;
            else
                return (32 + tipDesc.Y + (tipDesc as SPTextField).TextBounds.Height) / mScrollImageHeight;
        }

        public void AddTip(uint key, string title, string text)
        {
            if (mTips == null || mCanvas == null)
                return;

            SPSprite tip = CreateTip(title, text);
            if (tip != null)
            {
                tip.Visible = false;
                mCanvas.AddChild(tip);
                mScrollScales[key] = new Vector2(1f, ScrollScaleForTip(tip));
                mTips[key] = tip;
            }
        }

        public void RemoveTip(uint key)
        {
            if (mTips != null && mTips.ContainsKey(key))
                mTips.Remove(key);
            if (mScrollScales != null && mScrollScales.ContainsKey(key))
                mScrollScales.Remove(key);
        }

        public void DisplayTip(uint key)
        {
            if (mTips == null || !mTips.ContainsKey(key))
                return;

            if (mTips.ContainsKey(mPrevKey))
                mTips[mPrevKey].Visible = false;

            mScrollImage.ScaleY = mScrollScales.ContainsKey(key) ? mScrollScales[key].Y : DefaultScrollScaleY;
            mTips[key].Visible = true;
            mPrevKey = key;

            if (Alpha == 1f)
                mTweener.Reset(1f);
            else
                mTweener.Reset(Alpha, 1f, (1f - Alpha) * mDisplayTime);

            Visible = true;
        }

        public void HideTip()
        {
            if (mTweener == null || !Visible)
            {
                Visible = false;
                return;
            }

            mTweener.Reset(Alpha, 0f, Alpha * mHideTime);
        }

        public override void AdvanceTime(double time)
        {
            if (mTweener == null)
                return;

            mTweener.AdvanceTime(time);
            if (!mTweener.Delaying && Alpha != mTweener.TweenedValue)
                Alpha = mTweener.TweenedValue;
        }

        private void OnHidden()
        {
            if (SPMacros.SP_IS_FLOAT_EQUAL(Alpha, 0f))
                Visible = false;
        }
        #endregion
    }
}
