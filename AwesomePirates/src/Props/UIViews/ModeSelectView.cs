using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class ModeSelectView : Prop, IInteractable
    {
        public enum ModeSelectState
        {
            None = 0,
            Single_Multi,
            FFA_2v2
        }

        public const string CUST_EVENT_TYPE_CAREER_MODE_SELECTED = "careerModeSelectedEvent";
        public const string CUST_EVENT_TYPE_FFA_MODE_SELECTED = "ffaModeSelectedEvent";
        public const string CUST_EVENT_TYPE_2V2_MODE_SELECTED = "2v2ModeSelectedEvent";

        private const float kButtonEffectFactor = 2f;

        public ModeSelectView(int category)
            : base(category)
        {
            mCanvas = null;
            mState = ModeSelectState.None;
            SetupProp();

            mScene.SubscribeToInputUpdates(this);
        }

        #region Fields
        private ModeSelectState mState;

        private SPSprite mSingleMulti;
        private MenuButton mSinglePlayerButton;
        private MenuButton mMultiPlayerButton;
        private ButtonsProxy mSingleMultiProxy;

        private SPImage mSelectedTickSingleMulti;
        private SPImage mSelectedTickFFA2v2;

        private SPSprite mFFA2v2;
        private MenuButton mFFAButton;
        private MenuButton m2v2Button;
        private ButtonsProxy mFFA2v2Proxy;

        private SPSprite mScroll;
        private SPSprite mCanvas;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_MENU_MODE_SELECT; } }
        public ModeSelectState State
        {
            get { return mState; }
            private set
            {
                if (mState == value)
                    return;

                switch (value)
                {
                    case ModeSelectState.None:
                        mSingleMulti.Visible = false;
                        mFFA2v2.Visible = false;
                        break;
                    case ModeSelectState.Single_Multi:
                        mSingleMulti.Visible = true;
                        mFFA2v2.Visible = false;
                        break;
                    case ModeSelectState.FFA_2v2:
                        mSingleMulti.Visible = false;
                        mFFA2v2.Visible = true;
                        break;
                }

                mState = value;
            }
        }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvas != null)
                return;

            // Background scroll
            SPTexture scrollTexture = GuiHelper.CachedScrollTextureByName("scroll-quarter-small", mScene);
            SPImage scrollImage = new SPImage(scrollTexture);
            scrollImage.X = -scrollImage.Width / 2;
            scrollImage.Y = -scrollImage.Height / 2;

            mScroll = new SPSprite();
            mScroll.ScaleX = mScroll.ScaleY = 540.0f / scrollImage.Width;
            mScroll.AddChild(scrollImage);
            AddChild(mScroll);

            mCanvas = new SPSprite();
            AddChild(mCanvas);

            // Create Single_Multi panel
            mSingleMulti = new SPSprite();
            mSingleMulti.X = 48;
            mCanvas.AddChild(mSingleMulti);

                // Single-Player button
            mSinglePlayerButton = new MenuButton(null, mScene.TextureByName("sk-text-single-player"));
            mSinglePlayerButton.X = 132;
            mSinglePlayerButton.Y = 68;
            mSinglePlayerButton.SelectedEffecter.Factor = kButtonEffectFactor;
            mSinglePlayerButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnSinglePlayerPressed);
            mSingleMulti.AddChild(mSinglePlayerButton);

            SPImage careerImage = new SPImage(mScene.TextureByName("sk-text-career"));
            careerImage.X = (mSinglePlayerButton.Width - careerImage.Width) / 2;
            careerImage.Y = 0.85f * mSinglePlayerButton.Height;
            mSinglePlayerButton.AddContent(careerImage);

            SPImage shadyImage = new SPImage(mScene.TextureByName("sk-shady"));
            shadyImage.X = -(shadyImage.Width + 10);
            shadyImage.Y = -12;
            mSinglePlayerButton.AddContent(shadyImage);
            
                // Multiplayer button
            mMultiPlayerButton = new MenuButton(null, mScene.TextureByName("sk-text-multiplayer"));
            mMultiPlayerButton.X = 150;
            mMultiPlayerButton.Y = 256;
            mMultiPlayerButton.SelectedEffecter.Factor = kButtonEffectFactor;
            mMultiPlayerButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnMultiplayerPressed);
            mSingleMulti.AddChild(mMultiPlayerButton);

            SPImage crewImage0 = new SPImage(mScene.TextureByName("sk-crew-0"));
            crewImage0.X = -(80 + 0.65f * crewImage0.Width);
            crewImage0.Y = -0.5f * crewImage0.Height;
            mMultiPlayerButton.AddContent(crewImage0);

            SPImage crewImage1 = new SPImage(mScene.TextureByName("sk-crew-1"));
            crewImage1.X = crewImage0.X;
            crewImage1.Y = 0.65f * crewImage1.Height;
            mMultiPlayerButton.AddContent(crewImage1);

            SPImage crewImage2 = new SPImage(mScene.TextureByName("sk-crew-2"));
            crewImage2.X = crewImage1.X + 0.65f * crewImage2.Width;
            mMultiPlayerButton.AddContent(crewImage2);

            SPImage crewImage3 = new SPImage(mScene.TextureByName("sk-crew-3"));
            crewImage3.X = crewImage2.X - 1.3f * crewImage3.Width;
            mMultiPlayerButton.AddContent(crewImage3);

                // Selected tick
            mSelectedTickSingleMulti = new SPImage(mScene.TextureByName("good-point"));
            mSelectedTickSingleMulti.X = 410;
            mSelectedTickSingleMulti.Y = 106;
            GameController.GC.SetupHighlightEffecter(mSelectedTickSingleMulti);
            mSelectedTickSingleMulti.Effecter.Factor = kButtonEffectFactor;
            mSingleMulti.AddChild(mSelectedTickSingleMulti);

                // Button proxy
            mSingleMultiProxy = new ButtonsProxy(InputFocus, Globals.kNavVertical);
            mSingleMultiProxy.AddButton(mSinglePlayerButton, Buttons.A);
            mSingleMultiProxy.AddButton(mMultiPlayerButton, Buttons.A);

            // Create FFA_2v2 panel
            mFFA2v2 = new SPSprite();
            mFFA2v2.X = 30;
            mFFA2v2.Y = 26;
            mCanvas.AddChild(mFFA2v2);

                // FFA Button
            mFFAButton = new MenuButton(null, mScene.TextureByName("sk-text-free-for-all"));
            mFFAButton.X = 120;
            mFFAButton.Y = 20;
            mFFAButton.SelectedEffecter.Factor = kButtonEffectFactor;
            mFFAButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnFFAPressed);
            mFFA2v2.AddChild(mFFAButton);

            crewImage0 = new SPImage(mScene.TextureByName("sk-crew-0"));
            crewImage0.X = -0.75f * crewImage0.Width;
            crewImage0.Y = mFFAButton.Height;
            mFFAButton.AddContent(crewImage0);
            mFFAButton.AddContent(CreateVersusImage(crewImage0));

            crewImage1 = new SPImage(mScene.TextureByName("sk-crew-1"));
            crewImage1.X = crewImage0.X + crewImage0.Width + 22;
            crewImage1.Y = crewImage0.Y;
            mFFAButton.AddContent(crewImage1);
            mFFAButton.AddContent(CreateVersusImage(crewImage1));

            crewImage2 = new SPImage(mScene.TextureByName("sk-crew-2"));
            crewImage2.X = crewImage1.X + crewImage1.Width + 22;
            crewImage2.Y = crewImage1.Y;
            mFFAButton.AddContent(crewImage2);
            mFFAButton.AddContent(CreateVersusImage(crewImage2));

            crewImage3 = new SPImage(mScene.TextureByName("sk-crew-3"));
            crewImage3.X = crewImage2.X + crewImage2.Width + 22;
            crewImage3.Y = crewImage2.Y;
            mFFAButton.AddContent(crewImage3);

                // Rope
            SPImage ropeImage = new SPImage(mScene.TextureByName("sk-rope"));
            ropeImage.X = (scrollImage.Width - ropeImage.Width) / 2;
            ropeImage.Y = (scrollImage.Height - ropeImage.Height) / 2;
            mFFA2v2.AddChild(ropeImage);

                // 2v2 Button
            m2v2Button = new MenuButton(null, mScene.TextureByName("sk-text-2v2"));
            m2v2Button.X = 192;
            m2v2Button.Y = 196;
            m2v2Button.SelectedEffecter.Factor = kButtonEffectFactor;
            m2v2Button.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)On2v2Pressed);
            mFFA2v2.AddChild(m2v2Button);

            crewImage0 = new SPImage(mScene.TextureByName("sk-crew-0"));
            crewImage0.X = -1.55f * crewImage0.Width;
            crewImage0.Y = m2v2Button.Height;
            m2v2Button.AddContent(crewImage0);

            crewImage1 = new SPImage(mScene.TextureByName("sk-crew-1"));
            crewImage1.X = crewImage0.X + crewImage0.Width;
            crewImage1.Y = crewImage0.Y;
            m2v2Button.AddContent(crewImage1);
            m2v2Button.AddContent(CreateVersusImage(crewImage1));

            crewImage2 = new SPImage(mScene.TextureByName("sk-crew-2"));
            crewImage2.X = crewImage1.X + crewImage1.Width + 22;
            crewImage2.Y = crewImage1.Y;
            m2v2Button.AddContent(crewImage2);

            crewImage3 = new SPImage(mScene.TextureByName("sk-crew-3"));
            crewImage3.X = crewImage2.X + crewImage2.Width;
            crewImage3.Y = crewImage2.Y;
            m2v2Button.AddContent(crewImage3);

            // Selected tick
            mSelectedTickFFA2v2 = new SPImage(mScene.TextureByName("good-point"));
            mSelectedTickFFA2v2.X = 428;
            mSelectedTickFFA2v2.Y = 96;
            GameController.GC.SetupHighlightEffecter(mSelectedTickFFA2v2);
            mSelectedTickFFA2v2.Effecter.Factor = kButtonEffectFactor;
            mFFA2v2.AddChild(mSelectedTickFFA2v2);

                // Button proxy
            mFFA2v2Proxy = new ButtonsProxy(InputFocus, Globals.kNavVertical);
            mFFA2v2Proxy.AddButton(mFFAButton, Buttons.A);
            mFFA2v2Proxy.AddButton(m2v2Button, Buttons.A);

            mCanvas.X = -mScroll.Width / 2;
            mCanvas.Y = -mScroll.Height / 2;

            mSingleMulti.Visible = mState == ModeSelectState.Single_Multi;
            mFFA2v2.Visible = mState == ModeSelectState.FFA_2v2;
        }

        private SPImage CreateVersusImage(SPDisplayObject anchor)
        {
            SPImage image = new SPImage(mScene.TextureByName("sk-text-versus"));
            image.X = anchor.X + anchor.Width;
            image.Y = anchor.Y + (anchor.Height - image.Height);
            return image;
        }

        private void PlayButtonSound()
        {
            mScene.PlaySound("Button");
        }

        private void OnSinglePlayerPressed(SPEvent ev)
        {
            PlayButtonSound();
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_CAREER_MODE_SELECTED));
        }

        private void OnMultiplayerPressed(SPEvent ev)
        {
            PlayButtonSound();
            State = ModeSelectState.FFA_2v2;
        }

        private void OnFFAPressed(SPEvent ev)
        {
            PlayButtonSound();
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_FFA_MODE_SELECTED));
        }

        private void On2v2Pressed(SPEvent ev)
        {
            PlayButtonSound();
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_2V2_MODE_SELECTED));
        }

        private void ResetButtons()
        {
            if (mSinglePlayerButton != null)
                mSinglePlayerButton.AutomatedButtonRelease(false);
            if (mMultiPlayerButton != null)
                mMultiPlayerButton.AutomatedButtonRelease(false);
            if (mFFAButton != null)
                mFFAButton.AutomatedButtonRelease(false);
            if (m2v2Button != null)
                m2v2Button.AutomatedButtonRelease(false);
        }

        private void UpdateSelectedTick(int index, SPImage tick)
        {
            if (tick == null)
                return;

            if (tick == mSelectedTickSingleMulti)
            {
                tick.Y = index == 0 ? 106 : 256;
            }
            else if (tick == mSelectedTickFFA2v2)
            {
                tick.Y = index == 0 ? 96 : 272;
            }
        }

        public void ShowMenu()
        {
            if (State == ModeSelectState.None)
                State = ModeSelectState.Single_Multi;
        }

        public bool CloseMenu()
        {
            bool shouldClose = true;

            if (State == ModeSelectState.Single_Multi)
                State = ModeSelectState.None;
            else if (State == ModeSelectState.FFA_2v2)
            {
                State = ModeSelectState.Single_Multi;
                shouldClose = false;
            }

            ResetButtons();

            return shouldClose;
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            switch (mState)
            {
                case ModeSelectState.Single_Multi:
                    {
                        if (mSingleMultiProxy != null)
                        {
                            mSingleMultiProxy.Update(gpState, kbState);
                            UpdateSelectedTick(mSingleMultiProxy.NavIndex, mSelectedTickSingleMulti);
                        }
                        break;
                    }
                case ModeSelectState.FFA_2v2:
                    {
                        if (mFFA2v2Proxy != null)
                        {
                            mFFA2v2Proxy.Update(gpState, kbState);
                            UpdateSelectedTick(mFFA2v2Proxy.NavIndex, mSelectedTickFFA2v2);
                        }
                        break;
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
                        mScene.UnsubscribeToInputUpdates(this);
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
