using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class AchievementPanel : Prop
    {
        public const string CUST_EVENT_TYPE_ACHIEVEMENT_HIDDEN = "achievementHiddenEvent";

        public const uint ACHIEVEMENT_TIER_SWABBY = 0;
        public const uint ACHIEVEMENT_TIER_PIRATE = 1;
        public const uint ACHIEVEMENT_TIER_CAPTAIN = 2;

        public AchievementPanel(int category)
            : base(category)
        {
            Touchable = true;
            mAdvanceable = true;
            mSlowable = false;
            mHiding = false;
            mHideTimer = 0;
		    Tier = ACHIEVEMENT_TIER_SWABBY;
		    mDuration = 15.0;
		    mOriginY = 0f;
		    mIcon = null;
            mContainer = null;
            SetupProp();
        }
        
        #region Fields
        private bool mHiding;
        private uint mTier;
        private double mDuration;
        private double mHideTimer;
        private float mOriginY;
        private SPSprite mIcon;
        private SPSprite mContainer;
        private SPTextField mTitle;
        private SPTextField mText;
        #endregion

        #region Properties
        public bool Busy { get { return Visible; } }
        public uint Tier
        {
            get { return mTier; }
            set
            {
                SPImage image = null;

	            switch (value)
                {
		            case ACHIEVEMENT_TIER_SWABBY:
			            image = new SPImage(mScene.TextureByName("swabby-tier-complete"));
			            break;
		            case ACHIEVEMENT_TIER_PIRATE:
			            image = new SPImage(mScene.TextureByName("pirate-tier-complete"));
			            break;
		            case ACHIEVEMENT_TIER_CAPTAIN:
			            image = new SPImage(mScene.TextureByName("captain-tier-complete"));
			            break;
		            default:
			            break;
	            }
	            mTier = 0;
                IconImage = image;
            }
        }
        public double Duration { get { return mDuration; } set { mDuration = value; } }
        public string Title { get { return mTitle.Text; } set { mTitle.Text = value; } }
        public string Text { get { return mText.Text; } set { mText.Text = value; } }
        private SPImage IconImage
        {
            set
            {
                if (mIcon != null)
                {
                    mIcon.RemoveAllChildren();

                    if (value != null)
                    {
                        mIcon.AddChild(value);
                        mIcon.ScaleX = 64.0f / mIcon.Width;
                        mIcon.ScaleY = 64.0f / mIcon.Height;
                    }
                }
            }
        }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mContainer != null)
                return;
    
            mContainer = new SPSprite();
            AddChild(mContainer);
    
	        SPTexture bgTexture = mScene.TextureByName("achievement-banner");
	        SPImage bgImage = new SPImage(bgTexture);
            mContainer.AddChild(bgImage);

	        bgImage = new SPImage(bgTexture);
	        bgImage.ScaleX = -1f;
	        bgImage.X = 2 * bgImage.Width - 1f;
	        mContainer.AddChild(bgImage);
	
	        mTitle = new SPTextField(360f, 40f, "", mScene.FontKey, 26);
	        mTitle.X = (Width - mTitle.Width) / 2;
	        mTitle.Y = 16f;
	        mTitle.HAlign = SPTextField.SPHAlign.Center;
	        mTitle.VAlign = SPTextField.SPVAlign.Top;
            mTitle.Color = SPUtils.ColorFromColor(0x0072ff);
	        mContainer.AddChild(mTitle);

            mText = new SPTextField(390f, 84f, "", mScene.FontKey, 22);
	        mText.X = 144f;
	        mText.Y = 58f;
	        mText.HAlign = SPTextField.SPHAlign.Left;
	        mText.VAlign = SPTextField.SPVAlign.Top;
            mText.Color = new Color(0, 0, 0);
	        mContainer.AddChild(mText);
	
	        mIcon = new SPSprite();
	        mIcon.X = 68f;
	        mIcon.Y = 56f;
	        mContainer.AddChild(mIcon);
            mContainer.X = -mContainer.Width / 2;
            
	        ResOffset offset = ResManager.RESM.ItemOffsetWithAlignment(ResManager.ResAlignment.LowerCenter);
	        X = 160f + offset.X + mContainer.Width / 2;
	        Y = 660f + offset.Y;
	        mOriginY = Y;
	        Visible = false;
            AddEventListener(SPTouchEvent.SP_EVENT_TYPE_TOUCH, (SPTouchEventHandler)OnTouch);
        }

        public override void MoveToCategory(int category)
        {
            if (Visible)
                base.MoveToCategory(category);
            else
                Category = category;
        }

        public override void Flip(bool enable)
        {
            ScaleX = (enable) ? -1f : 1f;
        }

        public void Display()
        {
            if (Busy)
		        return;
            mScene.AddProp(this);
	        Visible = true;
	
	        SPTween tweenIn = new SPTween(this, 0.05f * mDuration, SPTransitions.SPEaseOutBack);
            tweenIn.AnimateProperty("Y", mOriginY - 280f);
            mScene.HudJuggler.AddObject(tweenIn);
            mHideTimer = 0.6 * mDuration;
        }

        private void Hide()
        {
            if (mHiding)
		        return;
	        mHiding = true;
            mScene.HudJuggler.RemoveTweensWithTarget(this);
	
	        SPTween tweenOut = new SPTween(this, 0.05f * mDuration, SPTransitions.SPEaseInBack);
            tweenOut.AnimateProperty("Y", mOriginY);
            tweenOut.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnAchievementHidden);
            mScene.HudJuggler.AddObject(tweenOut);
        }

        private void OnAchievementHidden(SPEvent ev)
        {
            mHiding = false;
	        Visible = false;
            mScene.RemoveProp(this, false);
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_ACHIEVEMENT_HIDDEN));
        }

        public override void AdvanceTime(double time)
        {
            if (mHideTimer > 0)
            {
                mHideTimer -= time;

                if (mHideTimer <= 0)
                    Hide();
            }
        }

        private void OnTouch(SPTouchEvent ev)
        {
            if (!Busy)
                return;

            SPTouch touch = ev.AnyTouch();

            if (touch != null)
                Hide();
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mIcon = null;
                        mContainer = null;
                        mTitle = null;
                        mText = null;
                        RemoveEventListener(SPTouchEvent.SP_EVENT_TYPE_TOUCH, (SPTouchEventHandler)OnTouch);
                    }
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
