using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using SparrowXNA;

namespace AwesomePirates
{
    class ExitView : Prop
    {
        public enum ExitViewMode
        {
            Trial,
            Normal
        }

        public ExitView(int category, ExitViewMode mode)
            : base(category)
        {
            mAdvanceable = true;
            mMode = mode;
            mButtons = new MenuButton[3];
            mButtonsProxy = new ButtonsProxy(InputFocus, Globals.kNavHorizontal);
            SetupProp();
        }

        #region Fields
        private ExitViewMode mMode;
        private SPSprite mScrollSprite;
        private SPSprite mCanvasSprite;
        private SPSprite mCanvasScaler;

        private FloatTweener mFailedTweener;
        private SPTextField mUpgradeText;
        private SPTextField mFailedText;
        private SPSprite mTrialModeSprite;
        private SPSprite mNormalModeSprite;

        private MenuButton[] mButtons;
        private ButtonsProxy mButtonsProxy;
        #endregion

        #region Properties
        private uint InputFocus { get { return InputManager.HAS_FOCUS_EXIT_MENU; } }
        public ExitViewMode Mode
        {
            get { return mMode; }
            set
            {
                if (mMode == value || value == ExitViewMode.Trial)
                    return;
                mMode = value;
                ModeDidChange(value);
            }
        }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);

            mCanvasScaler = new SPSprite();
            mCanvasScaler.X = mScene.ViewWidth / 2 - X;
            mCanvasScaler.Y = mScene.ViewHeight / 2 - Y;
            AddChild(mCanvasScaler);

            mCanvasSprite = new SPSprite();
            mCanvasScaler.AddChild(mCanvasSprite);

            // Background scroll
            SPTexture scrollTexture = GuiHelper.CachedScrollTextureByName("scroll-quarter-large", mScene);
            SPImage scrollImage = new SPImage(scrollTexture);
            mScrollSprite = new SPSprite();
            mScrollSprite.AddChild(scrollImage);

            mScrollSprite.ScaleX = mScrollSprite.ScaleY = 720.0f / mScrollSprite.Width;
            mCanvasSprite.AddChild(mScrollSprite);

            // Resume/Quit buttons
            MenuButton resumeButton = new MenuButton(null, mScene.TextureByName("pause-resume-button"));
            resumeButton.X = ResManager.RESX(238);
            resumeButton.Y = ResManager.RESY(492);
            resumeButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnResumePressed);
            mCanvasSprite.AddChild(resumeButton);

            MenuButton quitButton = new MenuButton(null, mScene.TextureByName("pause-quit-button"));
            quitButton.X = ResManager.RESX(578);
            quitButton.Y = ResManager.RESY(492);
            quitButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnQuitPressed);
            mCanvasSprite.AddChild(quitButton);

            // Trial Mode
            mTrialModeSprite = new SPSprite();
            mTrialModeSprite.X = ResManager.RESX(0);
            mTrialModeSprite.Y = ResManager.RESY(0);
            mCanvasSprite.AddChild(mTrialModeSprite);

            mUpgradeText = new SPTextField(560, 112, "Upgrade to the full version to continue the adventure!", mScene.FontKey, 40);
            mUpgradeText.X = 206;
            mUpgradeText.Y = 104;
            mUpgradeText.HAlign = SPTextField.SPHAlign.Center;
            mUpgradeText.VAlign = SPTextField.SPVAlign.Top;
            mUpgradeText.Color = Color.Black;
            mTrialModeSprite.AddChild(mUpgradeText);

            mFailedText = new SPTextField(560, 112, "You must be logged into Xbox Live with payment privileges.", mScene.FontKey, 40);
            mFailedText.X = 206;
            mFailedText.Y = 104;
            mFailedText.HAlign = SPTextField.SPHAlign.Center;
            mFailedText.VAlign = SPTextField.SPVAlign.Top;
            mFailedText.Color = SPUtils.ColorFromColor(0xba0000);
            mFailedText.Alpha = 0f;
            mTrialModeSprite.AddChild(mFailedText);

            mFailedTweener = new FloatTweener(mFailedText.Alpha, SPTransitions.SPLinear);

            MenuButton buyButton = new MenuButton(null, mScene.TextureByName("buy-now"));
            buyButton.X = 396;
            buyButton.Y = 354;
            buyButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnBuyPressed);
            mTrialModeSprite.AddChild(buyButton);

            mButtons[0] = resumeButton;
            mButtons[1] = buyButton;
            mButtons[2] = quitButton;
            mButtonsProxy.AddButton(buyButton);
            mButtonsProxy.AddButton(resumeButton);
            mButtonsProxy.AddButton(quitButton);
            mScene.SubscribeToInputUpdates(mButtonsProxy, true);

            SPImage shadyImage = new SPImage(mScene.TextureByName("shady"));
            shadyImage.X = 206;
            shadyImage.Y = 224;
            shadyImage.ScaleX = shadyImage.ScaleY = 180f / shadyImage.Width;
            mTrialModeSprite.AddChild(shadyImage);

            SPImage etherealImage = new SPImage(mScene.TextureByName("ethereal"));
            etherealImage.X = 562;
            etherealImage.Y = 256;
            etherealImage.ScaleX = etherealImage.ScaleY = 150f / etherealImage.Width;
            mTrialModeSprite.AddChild(etherealImage);

            // Normal Mode
            mNormalModeSprite = new SPSprite();
            mNormalModeSprite.X = ResManager.RESX(0);
            mNormalModeSprite.Y = ResManager.RESY(0);
            mCanvasSprite.AddChild(mNormalModeSprite);

            SPImage logoImage = new SPImage(mScene.TextureByName("logo"));
            logoImage.X = 330;
            logoImage.Y = 96;
            logoImage.ScaleX = logoImage.ScaleY = 300f / logoImage.Width;
            mNormalModeSprite.AddChild(logoImage);

            SPImage leftMascot = new SPImage(mScene.TextureByName("cm-mascot"));
            leftMascot.X = 244;
            leftMascot.Y = 412;
            mNormalModeSprite.AddChild(leftMascot);

            SPImage rightMascot = new SPImage(mScene.TextureByName("cm-mascot1"));
            rightMascot.X = 616;
            rightMascot.Y = 416;
            mNormalModeSprite.AddChild(rightMascot);

            SPTextField normalText = new SPTextField(290, 56, "Exit the game?", mScene.FontKey, 32);
            normalText.X = 344;
            normalText.Y = 422;
            normalText.HAlign = SPTextField.SPHAlign.Center;
            normalText.VAlign = SPTextField.SPVAlign.Top;
            normalText.Color = Color.Black;
            mNormalModeSprite.AddChild(normalText);

            mScrollSprite.X = (mScene.ViewWidth - mScrollSprite.Width) / 2;
            mScrollSprite.Y = (mScene.ViewHeight - mScrollSprite.Height) / 2;
            mCanvasSprite.X = -mCanvasScaler.X;
            mCanvasSprite.Y = -mCanvasScaler.Y;

            mCanvasScaler.ScaleX = mCanvasScaler.ScaleY = mScene.ScaleForUIView(mScrollSprite, 1f, 0.9f);
            ModeDidChange(mMode);

            ResManager.RESM.PopOffset();
        }

        private void ModeDidChange(ExitViewMode mode)
        {
            if (mode == ExitViewMode.Trial)
            {
                if (mNormalModeSprite != null)
                    mNormalModeSprite.Visible = false;
                if (mTrialModeSprite != null)
                    mTrialModeSprite.Visible = true;
                if (mButtons != null && mButtonsProxy != null)
                {
                    mButtonsProxy.Clear();
                    foreach (MenuButton button in mButtons)
                        mButtonsProxy.AddButton(button);
                }
                mButtonsProxy.ResetNav();
                mButtonsProxy.MoveNextNav();
            }
            else
            {
                if (mTrialModeSprite != null)
                    mTrialModeSprite.Visible = false;
                if (mNormalModeSprite != null)
                    mNormalModeSprite.Visible = true;
                if (mButtons != null && mButtonsProxy != null && mButtons.Length > 0)
                    mButtonsProxy.RemoveButton(mButtons[1]);
                mButtonsProxy.ResetNav();
            }
        }

        private void PlayButtonSound()
        {
            mScene.PlaySound("Button");
        }

        private void OnBuyPressed(SPEvent ev)
        {
            bool purchaseFailed = true;
            PlayButtonSound();

            if (ControlsManager.CM.PrevQueryPlayerIndex.HasValue && mScene.CanPurchase(ControlsManager.CM.PrevQueryPlayerIndex.Value))
            {
                if (GameController.GC.IsTrialMode && !Guide.IsVisible)
                {
                    mUpgradeText.Alpha = 1f;
                    mFailedText.Alpha = 0f;
                    mFailedTweener.Reset(mFailedText.Alpha);
                    purchaseFailed = false;

                    try
                    {
                        Guide.ShowMarketplace(ControlsManager.CM.PrevQueryPlayerIndex.Value);
                    }
                    catch (Exception)
                    {
                        purchaseFailed = true;
                    }
                }
            }

            if (purchaseFailed)
            {
                mScene.PlaySound("Locked");
                mUpgradeText.Alpha = 0f;
                mFailedText.Alpha = 1f;
                mFailedTweener.Reset(mFailedText.Alpha, 0f, 1f, 6f);
            }
        }

        private void OnResumePressed(SPEvent ev)
        {
            PlayButtonSound();
            mScene.HideExitView();
        }

        private void OnQuitPressed(SPEvent ev)
        {
            PlayButtonSound();
            GameController.GC.BeginExit();
        }

        public override void AdvanceTime(double time)
        {
            bool isTrialMode = GameController.GC.IsTrialMode;

            if (mMode == ExitViewMode.Trial && !isTrialMode)
                Mode = ExitViewMode.Normal;
            else if (mMode == ExitViewMode.Normal && isTrialMode)
                Mode = ExitViewMode.Trial;

            if (mMode == ExitViewMode.Trial)
            {
                mFailedTweener.AdvanceTime(time);
                if (!mFailedTweener.Delaying && mFailedText.Alpha != mFailedTweener.TweenedValue)
                {
                    mFailedText.Alpha = mFailedTweener.TweenedValue;
                    mUpgradeText.Alpha = 1f - mFailedText.Alpha;
                }
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
                        if (mButtonsProxy != null)
                        {
                            mScene.UnsubscribeToInputUpdates(mButtonsProxy, true);
                            mButtonsProxy = null;
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
