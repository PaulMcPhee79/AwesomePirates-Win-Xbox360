
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class HudCell : Prop
    {
#if IOS_SCREENS
        private const string kHudFont = "PlayfieldCCFont";
#else
        private const string kHudFont = "HUDFont";
#endif
        private const float kIconSize = 32.0f;
        private const int kFontHeightPadding = 6;

        public HudCell(int category, float x, float y, int fontSize, uint maxChars)
            : base(category)
        {
            X = x;
            Y = y;
            mValue = 0;
            mQueuedChange = 0;
            mFontSize = fontSize;
            mMaxChars = maxChars;
            mIcon = null;
            mText = null;
            mLabel = null;
            mCanvas = null;
            mFlipCanvas = null;
        }
        
        #region Fields
        private int mValue;
        private int mQueuedChange;
        private uint mMaxChars;
        private int mFontSize;
        private SPImage mIcon;
        private SPTextField mText;
        private SPTextField mLabel;
        private SPSprite mCanvas;
        private SPSprite mFlipCanvas;
        #endregion

        #region Properties
        public int Value { get { return mValue; } set { mValue = value; RefreshCellText(); } }
        public Color TextColor { get { return (mText != null) ? mText.Color : Color.White; } set { if (mText != null) mText.Color = value; } }
        public Color LabelColor { get { return (mLabel != null) ? mLabel.Color : Color.White; } set { if (mLabel != null) mLabel.Color = value; } }
        public StringBuilder CachedTextBuilder { get { return (mText != null) ? mText.CachedBuilder : null; } }
        public StringBuilder CachedLabelBuilder { get { return (mLabel != null) ? mLabel.CachedBuilder : null; } }
        #endregion

        #region Methods
        public void SetupWithLabel(string label, float labelWidth, Color textColor, Color? outlineColor = null)
        {
            bool canvasCreation = false;
    
            if (mCanvas == null)
            {
                mCanvas = new SPSprite();
                canvasCreation = true;
        
                if (mFlipCanvas == null)
                {
                    mFlipCanvas = new SPSprite();
                    AddChild(mFlipCanvas);
                }
        
                mCanvas.RemoveFromParent();
                mFlipCanvas.AddChild(mCanvas);
            }

            if (mLabel == null)
            {
                mLabel = SPTextField.CachedSPTextField(labelWidth, mFontSize + kFontHeightPadding, label, kHudFont, mFontSize);
                mLabel.Color = textColor;
                mLabel.HAlign = SPTextField.SPHAlign.Left;
                mLabel.VAlign = SPTextField.SPVAlign.Top;
                mCanvas.AddChild(mLabel);
            }
            else
            {
                mLabel.Text = label;
                mLabel.Color = textColor;
            }

            if (mText == null)
            {
                mText = SPTextField.CachedSPTextField((1.25f * mFontSize * mMaxChars) / 2, mFontSize + kFontHeightPadding, "", kHudFont, mFontSize);
                mText.Color = textColor;
                mText.HAlign = SPTextField.SPHAlign.Left;
                mText.VAlign = SPTextField.SPVAlign.Top;
                mCanvas.AddChild(mText);
            }
            else
            {
                mText.Color = textColor;
            }

            if (mLabel != null)
                mText.X = mLabel.Width;

            if (canvasCreation)
            {
                float halfWidth = (mText.X + mText.Width) / 2;
                mCanvas.X = -halfWidth;
                X += halfWidth;
            }
        }

        public void SetupWithiconTexture(SPTexture texture, Color textColor, Color? outlineColor = null)
        {
            bool canvasCreation = false;
            float iconSpacer = 0.0f;

            if (mCanvas == null)
            {
                mCanvas = new SPSprite();
                canvasCreation = true;

                if (mFlipCanvas == null)
                {
                    mFlipCanvas = new SPSprite();
                    AddChild(mFlipCanvas);
                }

                mCanvas.RemoveFromParent();
                mFlipCanvas.AddChild(mCanvas);
            }

            if (texture != null)
            {
		        if (mIcon == null)
                {
			        mIcon = new SPImage(texture);
                    mCanvas.AddChild(mIcon);
		        }
                else
                {
			        mIcon.ScaleX = 1.0f;
			        mIcon.ScaleY = 1.0f;
			        mIcon.Texture = texture;
		        }
		
		        mIcon.ScaleX = kIconSize / mIcon.Width;
		        mIcon.ScaleY = kIconSize / mIcon.Height;
		        iconSpacer = mIcon.Width + 4.0f;
	        }

            if (mText == null)
            {
                mText = SPTextField.CachedSPTextField((1.25f * mFontSize * mMaxChars) / 2, mFontSize + kFontHeightPadding, "", kHudFont, mFontSize);
                mText.Color = textColor;
                mText.HAlign = SPTextField.SPHAlign.Left;
                mText.VAlign = SPTextField.SPVAlign.Top;
                mCanvas.AddChild(mText);
            }
            else
            {
                mText.Color = textColor;
            }

            mText.X = iconSpacer;

            if (canvasCreation)
            {
                float halfWidth = (mText.X + mText.Width) / 2;
                mCanvas.X = -halfWidth;
                X += halfWidth;
            }
        }

        public void SetIconTexture(SPTexture texture)
        {
            if (texture != null && mIcon != null)
                mIcon.Texture = texture;
        }

        public void SetCellText(string text)
        {
            if (mText != null)
                mText.Text = text;
        }

        public override void Flip(bool enable)
        {
            mFlipCanvas.ScaleX = (enable) ? -1 : 1;
        }

        private void RefreshCellText()
        {
            Globals.CommaSeparatedValue(mValue, mText.CachedBuilder);
            mText.ForceCompilation();
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mText != null)
                        {
                            mText.Dispose();
                            mText = null;
                        }

                        if (mLabel != null)
                        {
                            mLabel.Dispose();
                            mLabel = null;
                        }
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
