using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class ObjectivesRankupPanel : Prop
    {
        public const string CUST_EVENT_TYPE_OBJECTIVES_RANKUP_PANEL_CONTINUED = "objectivesRankupPanelContinuedEvent";

        public ObjectivesRankupPanel(int category, uint rank)
            : base(category)
        {
            Touchable = true;
            mRank = rank;
            mButtonsProxy = null;
            SetupProp();
        }

        #region Fields
        private uint mRank;
        private SPSprite mCanvas;
        private SPSprite mCanvasScaler;
        private SPSprite mScrollSprite;
        private SPSprite mMainSprite;
        private SPSprite mMultiplierSprite;
        private ObjectivesHat mHat;
        private MenuButton mContinueButton;
        private ButtonsProxy mButtonsProxy;
        private ShadowTextField mRankText;
        private SPQuad mTouchBarrier;
        private List<SPSprite> mTicks;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_OBJECTIVES_RANKUP; } }
        public float ScrollHeight { get { return (mScrollSprite != null) ? mScrollSprite.Height : Height; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvas != null)
                return;

            mCanvasScaler = new SPSprite();
            mCanvasScaler.X = mScene.ViewWidth / 2 - X;
            mCanvasScaler.Y = mScene.ViewHeight / 2 - Y;
            AddChild(mCanvasScaler);

            mCanvas = new SPSprite();
            mCanvasScaler.AddChild(mCanvas);
    
            // Scroll
            SPTexture scrollTexture = GuiHelper.CachedScrollTextureByName("scroll-quarter-large", mScene);
            SPImage scrollImage = new SPImage(scrollTexture);
            mScrollSprite = new SPSprite();
            mScrollSprite.AddChild(scrollImage);
            mScrollSprite.ScaleX = mScrollSprite.ScaleY = 600.0f / mScrollSprite.Width;
            mScrollSprite.X = 180;
            mScrollSprite.Y = 64;
            mCanvas.AddChild(mScrollSprite);
    
            // Button
            mContinueButton = new MenuButton(null, mScene.TextureByName("continue-button"));
            mContinueButton.X = 396;
            mContinueButton.Y = 426;
            mContinueButton.Selected = true;
            mContinueButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnContinuePressed);
            mCanvas.AddChild(mContinueButton);

            mButtonsProxy = new ButtonsProxy(InputFocus);
            mButtonsProxy.AddButton(mContinueButton, Buttons.A);
            mScene.SubscribeToInputUpdates(mButtonsProxy);
    
            // Decorations
            SPTexture skullTexture = mScene.TextureByName("objectives-skull");
            SPImage skullImage = new SPImage(skullTexture);
            skullImage.X = 214;
            skullImage.Y = 390;
            mCanvas.AddChild(skullImage);
    
            skullImage = new SPImage(skullTexture);
            skullImage.X = 2 * 373;
            skullImage.Y = 2 * 195;
            skullImage.ScaleX = -1;
            mCanvas.AddChild(skullImage);
    
            // Ticks
            mTicks = new List<SPSprite>(ObjectivesRank.kNumObjectivesPerRank);
            SPTexture tickTexture = mScene.TextureByName("good-point");
    
            for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
            {
                SPImage tickImage = new SPImage(tickTexture);
                tickImage.X = -tickImage.Width / 2;
                tickImage.Y = -tickImage.Height / 2;
        
                SPSprite tickSprite = new SPSprite();
                tickSprite.X = 2 * (183 + i * 43) + tickImage.Width / 2;
                tickSprite.Y = 2 * 181 + tickImage.Height / 2;
                tickSprite.Visible = false;
                tickSprite.AddChild(tickImage);

                mCanvas.AddChild(tickSprite);
                mTicks.Add(tickSprite);
            }
    
            // Main Section
            mMainSprite = new SPSprite();
            mMainSprite.X = mScrollSprite.X + mScrollSprite.Width / 2;
            mMainSprite.Y = mScrollSprite.Y + mScrollSprite.Height / 2;
            mMainSprite.Visible = false;
            mCanvas.AddChild(mMainSprite);
    
            // Title text
            SPSprite mainSprite = new SPSprite();
            mainSprite.X = -mMainSprite.X;
            mainSprite.Y = -mMainSprite.Y;
            mMainSprite.AddChild(mainSprite);

            mRankText = new ShadowTextField(Category, 450, 64, 48);
            mRankText.X = 256;
            mRankText.Y = 100;
            mRankText.FontColor = SPUtils.ColorFromColor(0x797ca9);
            mRankText.Text = ObjectivesRank.TitleForRank(mRank) + "!";
            mainSprite.AddChild(mRankText);
    
            // Body text
            SPTextField textField = new SPTextField(360, 88, "Your score multiplier has increased to...", mScene.FontKey, 32);
            textField.X = mRankText.X + (mRankText.Width - textField.Width) / 2;
            textField.Y = 176;
            textField.HAlign = SPTextField.SPHAlign.Center;
            textField.VAlign = SPTextField.SPVAlign.Center;
            textField.Color = Color.Black;
            mainSprite.AddChild(textField);
    
            // Left cutlass
            SPTexture cutlassTexture = mScene.TextureByName("pointer");
            SPImage leftCutlassImage = new SPImage(cutlassTexture);
            leftCutlassImage.X = -leftCutlassImage.Width / 2;
            leftCutlassImage.Y = -leftCutlassImage.Height / 2;
    
            SPSprite leftCutlassSprite = new SPSprite();
            leftCutlassSprite.X = 344;
            leftCutlassSprite.Y = 280;
            leftCutlassSprite.Rotation = SPMacros.SP_D2R(-45);
            leftCutlassSprite.AddChild(leftCutlassImage);
            mainSprite.AddChild(leftCutlassSprite);
    
            // Right cutlass
            SPImage rightCutlassImage = new SPImage(cutlassTexture);
            rightCutlassImage.X = -rightCutlassImage.Width / 2;
            rightCutlassImage.Y = -rightCutlassImage.Height / 2;
    
            SPSprite rightCutlassSprite = new SPSprite();
            rightCutlassSprite.X = 614;
            rightCutlassSprite.Y = 280;
            rightCutlassSprite.ScaleX = -1;
            rightCutlassSprite.Rotation = SPMacros.SP_D2R(45);
            rightCutlassSprite.AddChild(rightCutlassImage);
            mainSprite.AddChild(rightCutlassSprite);
    
            // Multiplier sprite
            mMultiplierSprite = GuiHelper.ScoreMultiplierSpriteForValue(ObjectivesRank.MultiplierForRank(mRank), mScene);
            mMultiplierSprite.X = -mMultiplierSprite.Width / 2;
            mMultiplierSprite.Y = 0;
    
            SPSprite multiplierContainer = new SPSprite();
            multiplierContainer.X = 480;
            multiplierContainer.Y = 278;
            multiplierContainer.AddChild(mMultiplierSprite);
            mainSprite.AddChild(multiplierContainer);
    
            // Hat
            mHat = new ObjectivesHat(-1, ObjectivesHat.HatType.Angled, mScene.ObjectivesManager.RankLabel);
            mHat.X = 238;
            mHat.Y = -(mCanvas.Y + mHat.Height + 10);
            mHat.Visible = false;
            mCanvas.AddChild(mHat);
    
            float delay = DisplayStampsAfterDelay(0.5f);
            DropHatAfterDelay(delay);
    
            // Touch Barrier
            mTouchBarrier = new SPQuad(mScene.ViewWidth, mScene.ViewHeight);
            mTouchBarrier.Alpha = 0;
            mTouchBarrier.Visible = false;
            mCanvas.AddChildAtIndex(mTouchBarrier, 0);

            mCanvas.X = -(mScrollSprite.X + mScrollSprite.Width / 2);
            mCanvas.Y = -(mScrollSprite.Y + mScrollSprite.Height / 2);
            mCanvasScaler.ScaleX = mCanvasScaler.ScaleY = mScene.ScaleForUIView(mScrollSprite, 1f, 0.65f);
        }

        public void AttachGamerPic(SPDisplayObject gamerPic)
        {
            if (gamerPic != null && mScrollSprite != null)
            {
                gamerPic.X = 40;
                gamerPic.Y = 22;
                mScrollSprite.AddChild(gamerPic);
            }
        }

        public void DetachGamerPic(SPDisplayObject gamerPic)
        {
            if (gamerPic != null && mScrollSprite != null)
                mScrollSprite.RemoveChild(gamerPic);
        }

        public void EnableTouchBarrier(bool enable)
        {
            mTouchBarrier.Visible = enable;
        }

        private void DropHatAfterDelay(float delay)
        {
            mScene.Juggler.RemoveTweensWithTarget(mHat);

            mHat.Y = -(mCanvas.Y + mHat.Height + 10);
    
            SPTween tween = new SPTween(mHat, 0.5f, SPTransitions.SPEaseIn);
            tween.AnimateProperty("Y", 10 + mHat.Height / 2);
            tween.Delay = delay;
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnHatDropped);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_STARTED, (SPEventHandler)OnHatDropping);
            mScene.Juggler.AddObject(tween);
        }

        private float DisplayStampsAfterDelay(float delay)
        {
            foreach (SPSprite tickSprite in mTicks)
                mScene.Juggler.RemoveTweensWithTarget(tickSprite);
            mScene.Juggler.RemoveTweensWithTarget(mMainSprite);
    
            foreach (SPSprite tickSprite in mTicks)
            {
                StampAnimationWithStamp(tickSprite, 0.1f, delay, false);
                delay += 0.75f;
            }
    
            StampAnimationWithStamp(mMainSprite, 0.1f, delay, true);
            delay += 1f;
    
            return delay;
        }

        private void StampAnimationWithStamp(SPDisplayObject stamp, float duration, float delay, bool shakes)
        {
            float oldScaleX = stamp.ScaleX, oldScaleY = stamp.ScaleY;
	
	        stamp.ScaleX = 3f;
	        stamp.ScaleY = 3f;
    
            mScene.Juggler.RemoveTweensWithTarget(stamp);
	
	        SPTween tween = new SPTween(stamp, duration);
            tween.AnimateProperty("ScaleX", oldScaleX);
            tween.AnimateProperty("ScaleY", oldScaleY);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_STARTED, (SPEventHandler)OnStamping);
            
            if (shakes)
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnStamped);
            tween.Delay = delay;
            mScene.Juggler.AddObject(tween);
        }

        private void ShakeCanvas()
        {
            mScene.Juggler.RemoveTweensWithTarget(mCanvas);
    
	        float delay = 0.0f;
	        float xTarget, yTarget;
	        float xAccum = 0, yAccum = 0;
            GameController gc = GameController.GC;
	
	        for (int i = 0; i < 6; ++i)
            {
		        if (i < 5)
                {
                    xTarget = gc.NextRandom(-40, 40);
                    yTarget = gc.NextRandom(-40, 40);
			
			        xAccum += xTarget;
			        yAccum += yTarget;
			
			        // Don't let it shake too far from center
			        if (Math.Abs(xAccum) > 60)
                    {
				        xTarget = -xTarget;
				        xAccum += xTarget;
			        }
			
			        if (Math.Abs(yAccum) > 60)
                    {
				        yTarget = -yTarget;
				        yAccum += yTarget;
			        }
		        }
                else
                {
			        // Move it back to original position
			        xTarget = 0;
			        yTarget = 0;
		        }
		
		        SPTween tween = new SPTween(mCanvas, 0.05f);
                tween.AnimateProperty("X", mCanvas.X + xTarget);
                tween.AnimateProperty("Y", mCanvas.Y + yTarget);
		        tween.Delay = delay;
		        delay += (float)tween.TotalTime;
                mScene.Juggler.AddObject(tween);
	        }
        }

        private void OnHatDropping(SPEvent ev)
        {
            mHat.Visible = true;
        }

        private void OnHatDropped(SPEvent ev)
        {
            mScene.PlaySound("CrowdCheer");
        }

        private void OnStamping(SPEvent ev)
        {
            SPTween tween = ev.CurrentTarget as SPTween;
            SPDisplayObject target = tween.Target as SPDisplayObject;
            target.Visible = true;

            if (target == mMainSprite)
                mScene.PlaySound("StampLoud");
            else
                mScene.PlaySound("Stamp");
        }

        private void OnStamped(SPEvent ev)
        {
            ShakeCanvas();
        }

        private void OnContinuePressed(SPEvent ev)
        {
            mScene.PlaySound("Button");
            mScene.UnsubscribeToInputUpdates(mButtonsProxy);
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_OBJECTIVES_RANKUP_PANEL_CONTINUED));
        }
        #endregion

        #region Dispose
        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mTicks != null)
                        {
                            foreach (SPSprite tickSprite in mTicks)
                                mScene.Juggler.RemoveTweensWithTarget(tickSprite);
                            mTicks = null;
                        }

                        if (mHat != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mHat);
                            mHat = null;
                        }

                        if (mMainSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mMainSprite);
                            mMainSprite = null;
                        }

                        if (mCanvas != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mCanvas);
                            mCanvas = null;
                        }

                        if (mButtonsProxy != null)
                        {
                            mScene.UnsubscribeToInputUpdates(mButtonsProxy);
                            mButtonsProxy = null;
                        }

                        if (mContinueButton != null)
                        {
                            mContinueButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnContinuePressed);
                            mContinueButton = null;
                        }

                        mCanvasScaler = null;
                        mScrollSprite = null;
                        mRankText = null;
                        mMultiplierSprite = null;
                        mTouchBarrier = null;
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
