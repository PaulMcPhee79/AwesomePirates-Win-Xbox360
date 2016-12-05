using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class OutlineTextField : Prop
    {
        public enum OutlineDrawMode
        {
            Pass4 = 4,
            Pass8 = 8
        }

        public OutlineTextField(int category, float width, float height, int fontSize, int numOutlinePixels,
                string text = null, string fontName = null, OutlineDrawMode drawMode = OutlineDrawMode.Pass4)
            : base(category)
        {
            mTextField = SPTextField.CachedSPTextField(width, height, text, (fontName != null) ? fontName : mScene.FontKey, fontSize);
            mTextField.HAlign = SPTextField.SPHAlign.Center;
            mTextField.VAlign = SPTextField.SPVAlign.Center;

		    // Outline
            mOutline = new SPSprite();
            mOutlines = new SPTextField[(int)drawMode];

            for (int i = 0; i < mOutlines.Length; ++i)
            {
                SPTextField outline = SPTextField.CachedSPTextField(width, height, text, (fontName != null) ? fontName : mScene.FontKey, fontSize);
                outline.HAlign = SPTextField.SPHAlign.Center;
                outline.VAlign = SPTextField.SPVAlign.Center;
                outline.Color = Color.Black;
                mOutline.AddChild(outline);
                mOutlines[i] = outline;

                switch (i)
                {
                    case 0: outline.Y = -numOutlinePixels; break;
                    case 1: outline.X = numOutlinePixels; break;
                    case 2: outline.Y = numOutlinePixels; break;
                    case 3: outline.X = -numOutlinePixels; break;
                    case 4: outline.X = outline.Y = -numOutlinePixels; break;
                    case 5: outline.X = numOutlinePixels; outline.Y = -numOutlinePixels; break;
                    case 6: outline.X = outline.Y = numOutlinePixels; break;
                    case 7: outline.X = -numOutlinePixels; outline.Y = numOutlinePixels; break;
                }
            }

		    SetupProp();
        }

        private int mNumPasses;
        private SPTextField mTextField;
        private SPTextField[] mOutlines;
        private SPSprite mOutline;

        public Color FontColor { get { return mTextField.Color; } set { mTextField.Color = Color.White; } }
        public Color OutlineColor
        {
            get { return mOutlines[0].Color; }
            set
            {
                foreach (SPTextField outline in mOutlines)
                    outline.Color = value;
            }
        }
        public string Text
        {
            get { return mTextField.Text; }
            set
            {
                mTextField.Text = value;

                foreach (SPTextField outline in mOutlines)
                    outline.Text = value;
            }
        }

        protected override void SetupProp()
        {
            AddChild(mOutline);
            AddChild(mTextField);
        }

        public void HideOutline(bool hide)
        {
            mOutline.Visible = !hide;
        }

        public void SetTextAlignment(SPTextField.SPHAlign hAlign, SPTextField.SPVAlign vAlign)
        {
            mTextField.HAlign = hAlign;
            mTextField.VAlign = vAlign;

            foreach (SPTextField outline in mOutlines)
            {
                outline.HAlign = hAlign;
                outline.VAlign = vAlign;
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
                        if (mTextField != null)
                        {
                            mTextField.Dispose();
                            mTextField = null;
                        }

                        if (mOutlines != null)
                        {
                            foreach (SPTextField textField in mOutlines)
                                textField.Dispose();
                            mOutlines = null;
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
    }
}
