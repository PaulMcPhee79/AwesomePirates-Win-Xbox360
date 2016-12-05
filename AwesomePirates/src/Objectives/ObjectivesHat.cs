using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class ObjectivesHat : Prop
    {
        public enum HatType
        {
            Straight = 0,
            Angled
        }

        public ObjectivesHat(int category, HatType hatType, string text)
            : base(category)
        {
            mHatType = hatType;
            mHatImage = null;
            mHatSprite = null;
            SetText(text);
        }

        #region Fields
        private HatType mHatType;
        private SPImage mHatImage;
        private CurvedText mHatText;
        private SPSprite mHatTextSprite;
        private SPSprite mHatSprite;
        #endregion

        #region Methods
        private void SetupHat()
        {
            if (mHatType == HatType.Angled)
                SetupAngledHat();
            else
                SetupStraightHat();
        }

        private void SetupStraightHat()
        {
            if (mHatSprite != null)
                return;
            mHatSprite = new SPSprite();
            AddChild(mHatSprite);

            mHatImage = new SPImage(mScene.TextureByName("objectives-hat-logbook"));
            mHatImage.X = -mHatImage.Width / 2;
            mHatImage.Y = -mHatImage.Height / 2;
            mHatSprite.AddChild(mHatImage);
    
            mHatTextSprite = new SPSprite();
    
            int maxLength = 8;
            mHatText = new CurvedText(-1, maxLength, 24);
            mHatText.X = -0.7f * mHatImage.Width;
            mHatText.Y = -0.45f * mHatText.Height;
	        mHatText.Radius = 140;
            mHatText.MaxTextSeparation = 9;
	        mHatText.OriginX = mHatText.X;
            mHatText.TextColor = Color.White;
            mHatTextSprite.AddChild(mHatText);
            mHatSprite.AddChild(mHatTextSprite);
    
            mHatTextSprite.X = 2 * ((maxLength == 7) ? 58 : 66);
            mHatTextSprite.Y = 2 * 6;
        }

        private void SetupAngledHat()
        {
            if (mHatSprite != null)
                return;
            mHatSprite = new SPSprite();
            AddChild(mHatSprite);
    
            mHatImage = new SPImage(mScene.TextureByName("objectives-hat"));
            mHatImage.X = -mHatImage.Width / 2;
            mHatImage.Y = -mHatImage.Height / 2;
            mHatSprite.AddChild(mHatImage);
    
            mHatTextSprite = new SPSprite();
            mHatTextSprite.Rotation = SPMacros.SP_D2R(-22);
    
            int maxLength = 8;
            mHatText = new CurvedText(-1, maxLength, 24);
            mHatText.X = -0.705f * mHatImage.Width;
            mHatText.Y = -0.7f * mHatText.Height;
            mHatText.Radius = 140;
            mHatText.MaxTextSeparation = 9;
            mHatText.OriginX = mHatText.X;
            mHatText.TextColor = Color.White;
            mHatTextSprite.AddChild(mHatText);
            mHatSprite.AddChild(mHatTextSprite);
    
            mHatTextSprite.X = 2 * ((maxLength == 7) ? 50 : 58);
            mHatTextSprite.Y = 2 * ((maxLength == 7) ? -23 : -25);
        }

        private void DismantleHat()
        {
            if (mHatSprite != null)
            {
                mHatSprite.RemoveFromParent();
                mHatSprite.Dispose();
                mHatSprite = null;
            }

            mHatImage = null;
            mHatText = null;
            mHatTextSprite = null;
        }

        public void SetText(string text)
        {
            if (mHatText != null && mHatText.Text.Equals(text))
                return;
            DismantleHat();
            SetupHat();
            mHatText.Text = text;
        }

        public void SetTextColor(Color color)
        {
            mHatText.TextColor = color;
        }
        #endregion
    }
}
