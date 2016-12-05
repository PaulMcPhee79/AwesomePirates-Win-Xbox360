using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class HintTicker : Prop
    {
        public HintTicker(int category, string text, int fontSize = 48, float slideRate = 4.5f)
            : base(category)
        {
            mAdvanceable = true;
            mFontSize = fontSize;
            mSlideRate = slideRate;
            SetupPropWithText(text != null ? text : "");
        }

        #region Fields
        private int mFontSize;
        private float mSlideRate;
        private float mTextWidth;
        private SPTextField mTextField;
        #endregion

        #region Properties
        public Color TextColor { get { return mTextField.Color; } set { mTextField.Color = value; } }
        public string Text
        {
            get { return mTextField.Text; }
            set
            {
                mTextField.Text = value;
                mTextWidth = mTextField.TextBounds.Width;
            }
        }
        #endregion

        protected void SetupPropWithText(string text)
        {
            if (mTextField != null)
                return;

            mTextField = new SPTextField("", mScene.FontKey, mFontSize);
            mTextField = new SPTextField(1.1f * mTextField.Font.MeasureString(text).X, 1.2f * mFontSize, text, mScene.FontKey, mFontSize);
            mTextField.HAlign = SPTextField.SPHAlign.Left;
            mTextField.VAlign = SPTextField.SPVAlign.Top;
            mTextField.Color = Color.Honeydew;
            AddChild(mTextField);

            mTextWidth = mTextField.TextBounds.Width;
            X = mScene.ViewWidth;
            Y = mScene.ViewHeight - (mTextField.Height + 136f);
        }

        public override void AdvanceTime(double time)
        {
            X -= mSlideRate;

            if (X < -mTextWidth)
            {
                Visible = false;
                mScene.RemoveProp(this);
            }
        }
    }
}
