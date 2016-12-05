using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;
using ButtonsXNA = Microsoft.Xna.Framework.Input.Buttons;

namespace AwesomePirates
{
    class TutorialBooklet : BookletSubview
    {
        public const string CUST_EVENT_TYPE_TUTORIAL_DONE_PRESSED = "tutorialDonePressedEvent";

        public TutorialBooklet(int category, string key, int minIndex, int maxIndex)
            : base(category, key)
        {
            Touchable = true;
            mLoop = false;
            mMinIndex = minIndex;
            mMaxIndex = maxIndex;
            mContinueButtons = new ButtonsXNA[] { ButtonsXNA.A };
            SetupProp();
        }
        
        #region Fields
        private int mMinIndex;
        private int mMaxIndex;
        private SPButton mPrevButton;
        private SPButton mNextButton;
        private SPButton mDoneButton;
        private MenuButton mContinueButton;
        private ButtonsXNA[] mContinueButtons;
        #endregion

        #region Properties
        public int MinIndex { get { return mMinIndex; } set { mMinIndex = value; } }
        public int MaxIndex { get { return mMaxIndex; } set { mMaxIndex = value; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            Cover = CreateCover();
        }

        private TitleSubview CreateCover()
        {
            if (Cover != null)
		        return Cover;
            // Cover
            TitleSubview cover = new TitleSubview(-1);
    
            // Continue
            mContinueButton = new MenuButton(null, mScene.TextureByName("continue-button"));
            mContinueButton.X = (mScene.ViewWidth - mContinueButton.Width) / 2;
            mContinueButton.Y = mScene.ViewHeight - (220 - mContinueButton.Height / 2);
            mContinueButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnContinueButtonPressed);
            mContinueButton.Selected = true;
            AddChild(mContinueButton);
    
            mPrevButton = null;
            mNextButton = null;
            mDoneButton = null;

            return cover;
        }

        private SPButton CreateArrowButtonWithLabel(string label, int dir)
        {
            if (dir != 1 && dir != -1)
                dir = 1;
            SPButton button = new SPButton(mScene.TextureByName("arrow"));
            button.X = -dir * button.Width / 2;
            button.Y = -button.Height / 2;
            button.ScaleWhenDown = 0.9f;
            button.ScaleX = dir;
    
            SPTextField textField = new SPTextField(64, 32, label, mScene.FontKey, 28);
            textField.X = (dir == -1) ? textField.Width : 10;
            textField.Y = (button.Height - textField.Height) / 2;
            textField.Color = Color.White;
            textField.ScaleX = dir;
            textField.HAlign = SPTextField.SPHAlign.Left;
            textField.VAlign = SPTextField.SPVAlign.Center;
            button.AddContent(textField);

            return button;
        }

        private void PlayButtonSound()
        {
            mScene.PlaySound("Button");
        }

        public override void TurnToPage(int page)
        {
            if (page < mMinIndex || page > mMaxIndex)
                return;

            base.TurnToPage(page);
            UpdateNavigationButtons();
        }

        public override void NextPage()
        {
            if (mPageIndex == mMaxIndex)
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_TUTORIAL_DONE_PRESSED));

            base.NextPage();
        }

        private void UpdateNavigationButtons()
        {
            if (mPrevButton != null)
                mPrevButton.Visible = (mPageIndex != mMinIndex);
            if (mNextButton != null)
                mNextButton.Visible = (mPageIndex != mMaxIndex);
            if (mDoneButton != null)
                mDoneButton.Visible = (mPageIndex == mMaxIndex);
        }

        public override void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mIsDisposed)
                return;

            base.Update(gpState, kbState);

            ControlsManager cm = ControlsManager.CM;

            if (mContinueButton != null)
            {
                if (cm.DidButtonsDepress(mContinueButtons))
                    mContinueButton.AutomatedButtonDepress();
                else if (cm.DidButtonsRelease(mContinueButtons))
                    mContinueButton.AutomatedButtonRelease();
            }
        }

        private void OnPrevButtonPressed(SPEvent ev)
        {
            PlayButtonSound();
            PrevPage();
            UpdateNavigationButtons();
        }

        private void OnNextButtonPressed(SPEvent ev)
        {
            PlayButtonSound();
            NextPage();
            UpdateNavigationButtons();
        }

        private void OnDoneButtonPressed(SPEvent ev)
        {
            PlayButtonSound();
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_TUTORIAL_DONE_PRESSED));
        }

        private void OnContinueButtonPressed(SPEvent ev)
        {
            PlayButtonSound();
            NextPage();
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mContinueButton != null)
                        {
                            mContinueButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnContinueButtonPressed);
                            mContinueButton = null;
                        }

                        mPrevButton = null;
                        mNextButton = null;
                        mDoneButton = null;
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
