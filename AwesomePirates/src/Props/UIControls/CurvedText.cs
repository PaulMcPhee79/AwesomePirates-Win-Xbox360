using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class CurvedText : Prop
    {
        public enum CurvedTextOrientation
        {
            CW = 0,
            CCW
        }

        public CurvedText(int category, int maxLength, int fontSize)
            : base(category)
        {
            if (fontSize < 0 || SPMacros.SP_IS_FLOAT_EQUAL(fontSize, 0))
                throw new ArgumentException("CurvedText fontSize must be more than zero.");
            if (maxLength < 1)
                throw new ArgumentException("CurvedText length must be more than zero.");

            mNumChars = maxLength;
            mFontSize = fontSize;
            mColor = new Color(0, 0, 0);
            mOrientation = CurvedTextOrientation.CW;
            mOriginX = 0;
            mRadius = 0;
            mMaxTextSeparation = 25f;
            mText = null;
            mChars = null;
            SetupProp();
        }

        #region Fields
        private int mNumChars;
        private Color mColor;
        private CurvedTextOrientation mOrientation;
        private float mOriginX;
        private int mFontSize;
        private float mRadius;
        private float mMaxTextSeparation;
        private string mText;
        private List<SPTextField> mChars;
        #endregion

        #region Properties
        public string Text
        {
            get { return mText; }
            set
            {
                if (value.Length > mNumChars)
                    throw new ArgumentException("CurvedText text length too long.");

                if ((mText == null && value == null) || (mText != null && mText.Equals(value)))
		            return;
	            foreach (SPTextField textField in mChars)
		            textField.Text = "";

	            for (int i = 0; i < value.Length; ++i)
                {
		            SPTextField textField = mChars[i];
                    textField.Text = value.Substring(i, 1);
	            }
	
	            mText = value;
                LayoutText();
            } 
        }
        public Color TextColor
        {
            get { return mColor; }
            set
            {
                foreach (SPTextField textField in mChars)
                    textField.Color = value;
                mColor = value;
            }
        }
        public float OriginX { get { return mOriginX; } set { mOriginX = value; LayoutText(); } }
        public float Radius { get { return mRadius; } set { mRadius = value; LayoutText(); } }
        public float MaxTextSeparation { get { return mMaxTextSeparation; } set { mMaxTextSeparation = Math.Max(1, value); } }
        public CurvedTextOrientation Orientation { get { return mOrientation; } set { mOrientation = value; LayoutText(); } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mChars != null)
                return;
            mChars = new List<SPTextField>(mNumChars);

            SPTextField dummy = new SPTextField("", mScene.FontKey, mFontSize);
            Vector2 charSize = dummy.Font.MeasureString("O"); // O's are typically large, but not too large.

            for (int i = 0; i < mNumChars; ++i)
            {
                SPTextField textField = new SPTextField(charSize.X, charSize.Y, " ", mScene.FontKey, mFontSize);
                textField.X = i * textField.Width;
		        textField.FontName = mScene.FontKey;
		        textField.FontSize = mFontSize;
		        textField.Color = mColor;
                textField.HAlign = SPTextField.SPHAlign.Left;
                textField.VAlign = SPTextField.SPVAlign.Center;
                mChars.Add(textField);
                AddChild(textField);
            }
        }

        private void LayoutText()
        {
            if (mText == null)
                return;

            int textLength = mText.Length, previousChar = 0;
            float xAdvance = 0, scale = 1, angleIncrement = mRadius / (mRadius / mMaxTextSeparation), angle = 0, angleAccum = 0;
            SpriteFont font = mChars[0].Font;
            List<float> angles = new List<float>((int)textLength);
	
	        for (int i = 0; i < textLength; ++i)
            {
                string currentChar = mText.Substring(i, 1);
		        SPTextField textField = mChars[i];
		        previousChar = 0;
		
		        if (font != null)
                {
			        scale = textField.FontPtScale.X;
                    xAdvance = font.MeasureString(currentChar).X * scale;
			        previousChar = currentChar[0];
		        }
                else
                {
			        xAdvance = 0.6f * mFontSize;
		        }
		
		        angle = Math.Min(angleIncrement, 0.5f + angleIncrement * (xAdvance / (scale * 10.0f)));
		
		        // To account for our whacky font
		        if (previousChar == 'M' || previousChar == 'W')
			        angle += 1.0f;
		        angleAccum += angle;
		        angles.Add(angle);
	        }
	
	        int dir = (mOrientation == CurvedTextOrientation.CW) ? 1 : -1;
	        angle = 180.0f - 0.5f * angleAccum;
            Vector2 origin = new Vector2(0, mRadius * dir);
	
	        for (int i = 0; i < textLength; ++i)
            {
		        SPTextField textField = mChars[i];
                Vector2 point = origin;
                Globals.RotatePointThroughAngle(ref point, SPMacros.SP_D2R(angle));
		        textField.X = point.X;
		        textField.Y = point.Y + dir * mRadius;
		        textField.Rotation = SPMacros.SP_D2R(angle-180);
		        angle += angles[i];
	        }
        }
        #endregion

    }
}
