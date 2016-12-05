using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class PupProp : Prop
    {
        public enum PupPropAnimStyle
        {
            Rotate = 0,
            RotateAndScale
        }

        public PupProp(int category, SPDisplayObject pupIcon, string pupWheelTexName = "pickup-wheel")
            : base(category)
        {
            mPupIcon = pupIcon;
            mPupWheelTextureName = pupWheelTexName;
            mAnimating = false;
            mPup = null;
            SetupProp();
        }

        #region Fields
        private string mPupWheelTextureName;
        private bool mAnimating;
        private SPDisplayObject mPupIcon;
        private SPSprite mPupBase;
        private SPSprite mPupHighlight;
        private SPSprite mPup;

        private SPTween mCWRotateTween;
        private SPTween mCCWRotateTween;
        private SPTween mScaleTween;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mPup != null)
                return;

            mPup = new SPSprite();
            AddChild(mPup);

            mPupBase = new SPSprite();
            mPup.AddChild(mPupBase);

            SPTexture pupWheelTexture = mScene.TextureByName(mPupWheelTextureName);

            SPImage baseImage = new SPImage(pupWheelTexture);
            baseImage.X = -baseImage.Width / 2;
            baseImage.Y = -baseImage.Height / 2;
            baseImage.Color = new Color(0xaa, 0xaa, 0xaa);
            mPupBase.AddChild(baseImage);

            baseImage = new SPImage(pupWheelTexture);
            baseImage.X = -baseImage.Width / 2;
            baseImage.Y = -baseImage.Height / 2;
            baseImage.Color = new Color(0xaa, 0xaa, 0xaa);

            SPSprite sprite = new SPSprite();
            sprite.Rotation = SPMacros.PI / 4;
            sprite.AddChild(baseImage);
            mPupBase.AddChild(sprite);

            // Highlight
            mPupHighlight = new SPSprite();
            mPup.AddChild(mPupHighlight);

            SPImage highlightImage = new SPImage(pupWheelTexture);
            highlightImage.X = -highlightImage.Width / 2;
            highlightImage.Y = -highlightImage.Height / 2;
            mPupHighlight.AddChild(highlightImage);

            if (mPupIcon != null)
                AddChild(mPupIcon);
        }

        public void StartAnimation(float cycleDuration, PupPropAnimStyle style)
        {
            if (mAnimating)
                return;
            mAnimating = true;

            if (mCWRotateTween == null)
            {
                mCWRotateTween = new SPTween(mPup, cycleDuration);
                mCWRotateTween.AnimateProperty("Rotation", 2 * SPMacros.PI);
                mCWRotateTween.Loop = SPLoopType.Repeat;
            }

            mScene.Juggler.AddObject(mCWRotateTween);

            if (style == PupPropAnimStyle.RotateAndScale)
            {
                if (mScaleTween == null)
                {
                    mScaleTween = new SPTween(mPup, cycleDuration);
                    mScaleTween.AnimateProperty("ScaleX", 0.5f);
                    mScaleTween.AnimateProperty("ScaleY", 0.5f);
                    mScaleTween.Loop = SPLoopType.Reverse;
                }

                mScene.Juggler.AddObject(mScaleTween);
            }

            if (mCCWRotateTween == null)
            {
                mCCWRotateTween = new SPTween(mPupHighlight, cycleDuration / 2);
                mCCWRotateTween.AnimateProperty("Rotation", -2 * SPMacros.PI);
                mCCWRotateTween.Loop = SPLoopType.Repeat;
            }

            mScene.Juggler.AddObject(mCCWRotateTween);
        }

        public void StopAnimation()
        {
            if (!mAnimating)
                return;

            mAnimating = false;
            if (mCWRotateTween != null)
                mScene.Juggler.RemoveObject(mCWRotateTween);

            if (mCCWRotateTween != null)
                mScene.Juggler.RemoveObject(mCCWRotateTween);

            if (mScaleTween != null)
                mScene.Juggler.RemoveObject(mScaleTween);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        StopAnimation();
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
