using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class ShadowTextField : Prop
    {
        public ShadowTextField(int category, float width, float height, int fontSize, string text = null, string fontName = null)
            : base(category)
        {
            mTextField = SPTextField.CachedSPTextField(width, height, text, (fontName != null) ? fontName : mScene.FontKey, fontSize);
            mTextField.HAlign = SPTextField.SPHAlign.Center;
            mTextField.VAlign = SPTextField.SPVAlign.Center;

		    // Drop shadow
            mDropShadow = SPTextField.CachedSPTextField(width, height, text, (fontName != null) ? fontName : mScene.FontKey, fontSize);
            mDropShadow.X = 2f;
		    mDropShadow.Y = 2f;
            mDropShadow.HAlign = SPTextField.SPHAlign.Center;
            mDropShadow.VAlign = SPTextField.SPVAlign.Center;
            mDropShadow.Color = Color.Black;

		    SetupProp();
        }

        private SPTextField mTextField;
        private SPTextField mDropShadow;

        public Color FontColor { get { return mTextField.Color; } set { mTextField.Color = value; } }
        public Color ShadowColor { get { return mDropShadow.Color; } set { mDropShadow.Color = value; } }
        public string Text { get { return mTextField.Text; } set { mTextField.Text = value; mDropShadow.Text = value; } }

        #region Methods
        protected override void SetupProp()
        {
            AddChild(mDropShadow);
            AddChild(mTextField);
        }

        public void SetDropShadowOffset(float x, float y)
        {
            mDropShadow.X = x;
            mDropShadow.Y = y;
        }

        public void SetTextAlignment(SPTextField.SPHAlign hAlign, SPTextField.SPVAlign vAlign)
        {
            mTextField.HAlign = mDropShadow.HAlign = hAlign;
            mTextField.VAlign = mDropShadow.VAlign = vAlign;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mTextField != null)
                        {
                            mTextField.Dispose();
                            mTextField = null;
                        }

                        if (mDropShadow != null)
                        {
                            mDropShadow.Dispose();
                            mDropShadow = null;
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
