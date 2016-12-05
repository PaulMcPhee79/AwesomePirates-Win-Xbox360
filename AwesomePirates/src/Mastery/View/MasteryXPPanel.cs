using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class MasteryXPPanel : Prop
    {
        public MasteryXPPanel(int category, float percentComplete = 0f)
            : base(category)
        {
            mPercentComplete = percentComplete;
            mCostume = null;
            SetupProp();
        }

        #region Fields
        private float mPercentComplete;
        private SXGauge mXPGauge;
        private SPImage mXPCompleteImage;
        private SPSprite mXPLevelUpStamp;
        private SPSprite mCostume;
        #endregion

        #region Properties
        public float PercentComplete
        {
            get { return mPercentComplete; }
            set
            {
                float adjustedValue = Math.Max(0f, Math.Min(1f, value));
                mPercentComplete = adjustedValue;

                if (mXPGauge != null)
                    mXPGauge.Ratio = adjustedValue;

                if (mXPCompleteImage != null)
                    mXPCompleteImage.Visible = adjustedValue == 1f;
            }
        }
        public SPSprite Stamp { get { return mXPLevelUpStamp; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;

            mCostume = new SPSprite();
            AddChild(mCostume);

            SPImage xpGreyCross = new SPImage(mScene.TextureByName("x-marks-the-spot-grey"));
            SPImage xpGreyTrail = new SPImage(mScene.TextureByName("treasure-trail-grey"));
            xpGreyTrail.X = 0;
            xpGreyTrail.Y = 54;
            mCostume.AddChild(xpGreyTrail);

            float trailScale = (300f - xpGreyCross.Width) / xpGreyTrail.Width;
            xpGreyTrail.ScaleX = xpGreyTrail.ScaleY = trailScale;

            xpGreyCross.X = xpGreyTrail.X + xpGreyTrail.Width + 2;
            xpGreyCross.Y = xpGreyTrail.Y - 7;
            mCostume.AddChild(xpGreyCross);

            mXPCompleteImage = new SPImage(mScene.TextureByName("x-marks-the-spot"));
            mXPCompleteImage.X = xpGreyCross.X;
            mXPCompleteImage.Y = xpGreyCross.Y;
            mXPCompleteImage.Visible = false;
            mCostume.AddChild(mXPCompleteImage);

            mXPGauge = new SXGauge(mScene.TextureByName("treasure-trail"), SXGauge.SXGaugeOrientation.Horizontal);
            mXPGauge.X = xpGreyTrail.X;
            mXPGauge.Y = xpGreyTrail.Y;
            mXPGauge.ScaleX = mXPGauge.ScaleY = trailScale;
            PercentComplete = mPercentComplete;
            mCostume.AddChild(mXPGauge);

            SPTextField textField = new SPTextField(240, 48, "Mastery XP", mScene.FontKey, 32);
            textField.X = xpGreyTrail.X + (Width - textField.Width) / 2;
            textField.Y = 0;
            textField.HAlign = SPTextField.SPHAlign.Center;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = Color.Black;
            mCostume.AddChild(textField);

            SPImage levelUpImage = new SPImage(mScene.TextureByName("mastery-stamp"));
            levelUpImage.X = -levelUpImage.Width / 2;
            levelUpImage.Y = -levelUpImage.Height / 2;

            mXPLevelUpStamp = new SPSprite();
            mXPLevelUpStamp.X = mXPCompleteImage.X + mXPCompleteImage.Width + levelUpImage.Width / 2 - 6;
            mXPLevelUpStamp.Y = mXPCompleteImage.Y;
            mXPLevelUpStamp.AddChild(levelUpImage);
            mXPLevelUpStamp.Visible = false;
            mCostume.AddChild(mXPLevelUpStamp);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mXPLevelUpStamp != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mXPLevelUpStamp);
                            mXPLevelUpStamp = null;
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
