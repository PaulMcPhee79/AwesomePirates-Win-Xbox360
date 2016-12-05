using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class ObjectivesLog : Prop, IInteractable
    {
        private const string CUST_EVENT_TYPE_LOG_TAB_TOUCHED = "logTabTouchedEvent";

        public ObjectivesLog(int category, uint rank)
            : base(category)
        {
            mAdvanceable = true;
            mDirtyFlag = true;
            mRank = rank;
            mLogBook = null;
            SetupProp();

            mScene.SubscribeToInputUpdates(this);
        }
        
        #region Fields
        private bool mDirtyFlag;
        private uint mRank;

        private ObjectivesHat mHat;
        private ShadowTextField mRankTextField;
        private SPTextField mMultiplierTextField;
        private SPSprite mMultiplierSprite;
        private SPSprite mMaxRankSprite;
        private SPSprite mObjDescSprite;
        private List<SPImage> mIconImages;
        private List<SPTextField> mRankDescTextFields;

        private SPSprite mLogPage;
        private SPSprite mLogBook;
        private LogTab mPrevTab;
        private LogTab mNextTab;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_MENU_OBJECTIVES_LOG; } }
        public uint Rank { get { return mRank; } set { mRank = value; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mLogBook != null)
                return;
    
            // Log Book
            SPTexture logbookTexture = mScene.TextureByName("logbook");
            SPImage leftPage = new SPImage(logbookTexture);
            SPImage rightPage = new SPImage(logbookTexture);
            rightPage.ScaleX = -1;
            rightPage.X = 2 * rightPage.Width;
    
	        mLogPage = new SPSprite();
            mLogPage.Touchable = false;
            mLogPage.AddChild(leftPage);
            mLogPage.AddChild(rightPage);
	        mLogPage.X = -mLogPage.Width / 2;
	        mLogPage.Y = -mLogPage.Height / 2;

            mObjDescSprite = new SPSprite();
            mLogPage.AddChild(mObjDescSprite);
    
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
	        mLogBook = new SPSprite();
	        mLogBook.X = ResManager.RESX(480);
	        mLogBook.Y = ResManager.RESY(312);
            mLogBook.AddChild(mLogPage);
            AddChild(mLogBook);
    
            // Tabs
            mPrevTab = new LogTab(0, -1);
            mPrevTab.X = ResManager.RESX(62) - mLogBook.X;
            mPrevTab.Y = ResManager.RESY(180) - mLogBook.Y;
            mPrevTab.AddEventListener(CUST_EVENT_TYPE_LOG_TAB_TOUCHED, (SPEventHandler)OnPrevLogTab);
    
            mNextTab = new LogTab(0, 1);
            mNextTab.X = ResManager.RESX(834) - mLogBook.X;
            mNextTab.Y = ResManager.RESY(180) - mLogBook.Y;
            mNextTab.AddEventListener(CUST_EVENT_TYPE_LOG_TAB_TOUCHED, (SPEventHandler)OnNextLogTag);
            ResManager.RESM.PopOffset();
            mLogBook.AddChild(mPrevTab);
            mLogBook.AddChild(mNextTab);

            SyncWithObjectives();
        }

        public void SyncWithObjectives()
        {
            if (!mDirtyFlag)
                return;
    
            mPrevTab.PageNo = (int)mRank-1;
            mPrevTab.Visible = (mRank > 0);
    
            mNextTab.Visible = (mRank < mScene.ObjectivesManager.Rank);
            mNextTab.PageNo = (int)mRank+1;
    
            // Left Page
            if (mHat == null)
            {
                mHat = new ObjectivesHat(-1, ObjectivesHat.HatType.Straight, mScene.ObjectivesManager.RankLabel);
                mHat.X = 222;
                mHat.Y = 96;
                mHat.ScaleX = mHat.ScaleY = 96.0f / mHat.Height;
                mLogPage.AddChild(mHat);
            }
            else
            {
                mHat.SetText(mScene.ObjectivesManager.RankLabelForRank(mRank));
            }
    
            if (mRankTextField == null)
            {
                mRankTextField = new ShadowTextField(Category, 320, 48, 40);
                mRankTextField.X = 64;
                mRankTextField.Y = 152;
                mRankTextField.FontColor = SPUtils.ColorFromColor(0x797ca9);
                mRankTextField.Text = ObjectivesRank.TitleForRank(mRank);
                mLogPage.AddChild(mRankTextField);
            }
            else
            {
                string rankText = ObjectivesRank.TitleForRank(mRank);
        
                if (!mRankTextField.Text.Equals(rankText))
                    mRankTextField.Text = rankText;
            }
    
            if (mMultiplierTextField == null)
            {
                mMultiplierTextField = new SPTextField(280, 48, "Score Multiplier", mScene.FontKey, 32);
                mMultiplierTextField.X = 90;
                mMultiplierTextField.Y = 246;
                mMultiplierTextField.Color = Color.Black;
                mMultiplierTextField.HAlign = SPTextField.SPHAlign.Center;
                mMultiplierTextField.VAlign = SPTextField.SPVAlign.Top;
                mLogPage.AddChild(mMultiplierTextField);
            }
    
            if (mMultiplierSprite != null)
            {
                mLogPage.RemoveChild(mMultiplierSprite);
                mMultiplierSprite.Dispose();
                mMultiplierSprite = null;
            }
    
            mMultiplierSprite = GuiHelper.ScoreMultiplierSpriteForValue(ObjectivesRank.MultiplierForRank(mRank), mScene);
            mMultiplierSprite.X = mMultiplierTextField.X + (mMultiplierTextField.Width - mMultiplierSprite.Width) / 2;
            mMultiplierSprite.Y = mMultiplierTextField.Y + mMultiplierTextField.Height + 12;
            mLogPage.AddChild(mMultiplierSprite);
    
            // Right Page
            ObjectivesRank syncedRank = mScene.ObjectivesManager.SyncedObjectivesForRank(mRank);
    
            if (syncedRank.IsMaxRank)
            {
                if (mMaxRankSprite == null)
                {
                    mMaxRankSprite = mScene.ObjectivesManager.MaxRankSprite();
                    mMaxRankSprite.X = 410;
                    mMaxRankSprite.Y = 72;
                    mLogPage.AddChild(mMaxRankSprite);
                }

                mObjDescSprite.Visible = false;
                mMaxRankSprite.Visible = true;
            }
            else if (mMaxRankSprite != null)
            {
                mObjDescSprite.Visible = true;
                mMaxRankSprite.Visible = false;
            }

            if (mIconImages == null)
                mIconImages = new List<SPImage>(ObjectivesRank.kNumObjectivesPerRank);
            if (mRankDescTextFields == null)
                mRankDescTextFields = new List<SPTextField>(ObjectivesRank.kNumObjectivesPerRank);
    
            for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
            {
                bool completed = syncedRank.IsObjectiveCompletedAtIndex(i);
                SPImage iconImage = null;
                SPTexture iconTexture = mScene.TextureByName((completed) ? "objectives-tick" : "objectives-cross");
        
                if (mIconImages.Count > i)
                {
                    iconImage = mIconImages[i];
                    iconImage.Texture = iconTexture;
                }
                else
                {
                    iconImage = new SPImage(iconTexture);
                    iconImage.X = 410;
                    iconImage.Y = 98 + i * 124;
                    mIconImages.Add(iconImage);
                    mObjDescSprite.AddChild(iconImage);
                }
        
                SPTextField rankDescTextField = null;
                string rankDescText = syncedRank.ObjectiveLogbookTextAtIndex(i);

                if (rankDescText == null)
                    rankDescText = "";

                if (mRankDescTextFields.Count > i)
                {
                    rankDescTextField = mRankDescTextFields[i];
            
                    if (!rankDescTextField.Text.Equals(rankDescText))
                        rankDescTextField.Text = rankDescText;
                }
                else
                {
                    rankDescTextField = new SPTextField(284, 120, rankDescText, mScene.FontKey, 26);
                    rankDescTextField.X = 456;
                    rankDescTextField.Y = 52 + i * 124;
                    rankDescTextField.Color = Color.Black;
                    rankDescTextField.HAlign = SPTextField.SPHAlign.Left;
                    rankDescTextField.VAlign = SPTextField.SPVAlign.Center;
                    mRankDescTextFields.Add(rankDescTextField);
                    mObjDescSprite.AddChild(rankDescTextField);
                }
        
                iconImage.Visible = (rankDescTextField.Text != null);
            }
        }

        private void OnPrevLogTab(SPEvent ev)
        {
            if (Rank > 0)
            {
                --Rank;
                SyncWithObjectives();
                mScene.PlaySound("PageTurn");
            }
        }

        private void OnNextLogTag(SPEvent ev)
        {
            if (Rank < mScene.ObjectivesManager.Rank)
            {
                ++Rank;
                SyncWithObjectives();
                mScene.PlaySound("PageTurn");
            }
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mPrevTab != null)
                mPrevTab.Update(gpState, kbState);
            if (mNextTab != null)
                mNextTab.Update(gpState, kbState);
        }

        public override void AdvanceTime(double time)
        {
            if (mPrevTab != null)
                mPrevTab.AdvanceTime(time);
            if (mNextTab != null)
                mNextTab.AdvanceTime(time);
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

                        if (mPrevTab != null)
                        {
                            mPrevTab.RemoveEventListener(CUST_EVENT_TYPE_LOG_TAB_TOUCHED, (SPEventHandler)OnPrevLogTab);
                            mPrevTab = null;
                        }

                        if (mNextTab != null)
                        {
                            mNextTab.RemoveEventListener(CUST_EVENT_TYPE_LOG_TAB_TOUCHED, (SPEventHandler)OnNextLogTag);
                            mNextTab = null;
                        }

                        mHat = null;
                        mRankTextField = null;
                        mMultiplierTextField = null;
                        mMultiplierSprite = null;
                        mMaxRankSprite = null;
                        mIconImages = null;
                        mRankDescTextFields = null;
                        mLogPage = null;
                        mLogBook = null;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion


        private class LogTab : Prop
        {
            public LogTab(int category, int side)
                : base(category)
            {
                Touchable = true;
                mPageNo = 0;
                mSide = side;
                mText = null;
                mTab = null;
                mInputInertia = new InputInertia();
                SetupProp();
            }

            private int mPageNo;
            private int mSide; // 1: Right, -1: Left
            private SPTextField mText;
            private SPImage mTab;
            private InputInertia mInputInertia;

            public int PageNo
            {
                get { return mPageNo; }
                set
                {
                    mText.Text = value.ToString();
                    mPageNo = value;
                }
            }

            protected override void SetupProp()
            {
                if (mTab != null)
                    return;
    
                mTab = new SPImage(mScene.TextureByName("bookmark"));
                mTab.Color = SPUtils.ColorFromColor(0xdddddd);
                AddChild(mTab);
    
                mText = new SPTextField(48, 48, "0", mScene.FontKey, 34);
                mText.HAlign = SPTextField.SPHAlign.Center;
                mText.VAlign = SPTextField.SPVAlign.Top;
                mText.Color = Color.Black;
                mText.X = mTab.X + (mTab.Width - mText.Width) / 2;
                mText.Y = mTab.Y + (mTab.Height - mText.Height) / 2;
                AddChild(mText);
            }

            public void Update(GamePadState gpState, KeyboardState kbState)
            {
                GameController gc = GameController.GC;

                if (mSide == -1)
                {
                    if (gpState.DPad.Left == ButtonState.Pressed || gpState.ThumbSticks.Left.X < -0.5f || kbState.IsKeyDown(Keys.Left | Keys.NumPad4))
                    {
                        if (mInputInertia.CanMove)
                            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_LOG_TAB_TOUCHED));
                    }
                    else
                    {
                        mInputInertia.Reset();
                    }
                }
                else if (mSide == 1)
                {
                    if (gpState.DPad.Right == ButtonState.Pressed || gpState.ThumbSticks.Left.X > 0.5f || kbState.IsKeyDown(Keys.Right | Keys.NumPad6))
                    {
                        if (mInputInertia.CanMove)
                            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_LOG_TAB_TOUCHED));
                    }
                    else
                    {
                        mInputInertia.Reset();
                    }
                }
            }

            public override void AdvanceTime(double time)
            {
                mInputInertia.AdvanceTime(time);
            }
        }
    }
}
