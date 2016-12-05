using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class SKTutorialView : Prop
    {
        public const string CUST_EVENT_TYPE_SK_TUTORIAL_VIEW_HIDDEN = "skTutorialViewHiddenEvent";
        public const string CUST_EVENT_TYPE_SK_TUTORIAL_VIEW_SHOWN = "skTutorialViewShownEvent";

        private readonly Color kSKTutorialTextColor = SPUtils.ColorFromColor(0xffd541);

        public SKTutorialView(int category)
            : base(category)
        {
            SetupProp();
        }

        #region Fields
        private SPSprite mCostume;
        private SPSprite mFlipSprite;
        private SPTween mShowTween;
        private SPTween mHideTween;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;

            mFlipSprite = new SPSprite();
            mFlipSprite.X = mScene.ViewWidth / 2;
            mFlipSprite.Y = mScene.ViewHeight / 2;
            mFlipSprite.ScaleX = mScene.Flipped ? -1 : 1;
            AddChild(mFlipSprite);

            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            mCostume = new SPSprite();
            mCostume.X = -mFlipSprite.X + ResManager.RESX(0);
            mCostume.Y = -mFlipSprite.Y + ResManager.RESY(0);
            mCostume.Alpha = 0f;
            mFlipSprite.AddChild(mCostume);
            ResManager.RESM.PopOffset();

            // Steering
            SPImage steerImage = new SPImage(mScene.TextureByName("large_thumbstick_left"));
            steerImage.X = 146;
            steerImage.Y = 52;
            mCostume.AddChild(steerImage);

            SPTextField steerText = new SPTextField(128, 64, "Steer", mScene.FontKey, 54);
            steerText.X = steerImage.X + steerImage.Width;
            steerText.Y = steerImage.Y + (steerImage.Height - steerText.Height) / 2;
            steerText.HAlign = SPTextField.SPHAlign.Left;
            steerText.VAlign = SPTextField.SPVAlign.Center;
            steerText.Color = kSKTutorialTextColor;
            mCostume.AddChild(steerText);

            // Shooting
            SPImage shootImage = new SPImage(mScene.TextureByName("large_face_a"));
            shootImage.X = 358;
            shootImage.Y = 202;
            mCostume.AddChild(shootImage);

            SPTextField shootText = new SPTextField(128, 64, "Shoot", mScene.FontKey, 54);
            shootText.X = shootImage.X + shootImage.Width;
            shootText.Y = shootImage.Y + (shootImage.Height - shootText.Height) / 2;
            shootText.HAlign = SPTextField.SPHAlign.Left;
            shootText.VAlign = SPTextField.SPVAlign.Center;
            shootText.Color = kSKTutorialTextColor;
            mCostume.AddChild(shootText);

            // Boosting
            SPImage bgTint = new SPImage(mScene.TextureByName("sk-boost-mid"));
            bgTint.X = 600;
            bgTint.Y = 308;
            bgTint.Color = Color.Red;
            bgTint.Alpha = 0.25f;
            mCostume.AddChild(bgTint);

            SXGauge boostGauge = new SXGauge(mScene.TextureByName("sk-boost-mid"), SXGauge.SXGaugeOrientation.Horizontal);
            boostGauge.X = bgTint.X;
            boostGauge.Y = bgTint.Y;
            boostGauge.Color = Color.Red;
            boostGauge.Alpha = 0.5f;
            boostGauge.Ratio = 0.65f;
            mCostume.AddChild(boostGauge);

            SPImage leftTrigger = new SPImage(mScene.TextureByName("large_trigger_left"));
            leftTrigger.X = 556;
            leftTrigger.Y = 360;
            mCostume.AddChild(leftTrigger);

            SPImage rightTrigger = new SPImage(mScene.TextureByName("large_trigger_right"));
            rightTrigger.X = 638;
            rightTrigger.Y = 360;
            mCostume.AddChild(rightTrigger);

            SPTextField boostText = new SPTextField(128, 64, "Boost", mScene.FontKey, 54);
            boostText.X = rightTrigger.X + rightTrigger.Width;
            boostText.Y = rightTrigger.Y + (rightTrigger.Height - boostText.Height) / 2;
            boostText.HAlign = SPTextField.SPHAlign.Left;
            boostText.VAlign = SPTextField.SPVAlign.Center;
            boostText.Color = kSKTutorialTextColor;
            mCostume.AddChild(boostText);
        }

        public override void Flip(bool enable)
        {
            mFlipSprite.ScaleX = enable ? -1 : 1;
        }

        public void Show()
        {
            if (mCostume == null)
                return;

            mScene.Juggler.RemoveTweensWithTarget(mCostume);

            if (mShowTween == null)
            {
                float duration = 1f;
                mShowTween = new SPTween(mCostume, (1f - mCostume.Alpha) * duration);
                mShowTween.AnimateProperty("Alpha", 1f);
                mShowTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnShown));
            }
            else
                mShowTween.Reset();

            mScene.Juggler.AddObject(mShowTween);
        }

        public void Hide()
        {
            if (mCostume == null)
                return;

            mScene.Juggler.RemoveTweensWithTarget(mCostume);

            if (mHideTween == null)
            {
                float duration = 1f;
                mHideTween = new SPTween(mCostume, mCostume.Alpha * duration);
                mHideTween.AnimateProperty("Alpha", 0f);
                mHideTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnHidden));
            }
            else
                mHideTween.Reset();

            mScene.Juggler.AddObject(mHideTween);
        }

        private void OnShown(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_TUTORIAL_VIEW_SHOWN));
        }

        private void OnHidden(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_TUTORIAL_VIEW_HIDDEN));
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mCostume != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mCostume);
                            mCostume = null;
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
