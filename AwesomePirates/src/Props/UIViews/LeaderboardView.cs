using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class LeaderboardView : Prop, IInteractable
    {
        public LeaderboardView(int category, PlayerIndex? playerIndex, float width, float height, int scoresPerPage)
            : base(category)
        {
            mPlayerIndex = playerIndex;
            mScopeWidth = width;
            mScopeHeight = height;
            mScoresPerPage = scoresPerPage;
            mScopeIndex = -1;
            mScopes = new List<LeaderboardScope>(3);
            SetupProp();

            mScene.SubscribeToInputUpdates(this);
        }

        #region Fields
        private bool mIsTrialMode = false;
        private int mScoresPerPage;
        private float mScopeWidth;
        private float mScopeHeight;

        private SPTextField mScopeButtonTextField;
        private SPTextField mPrevButtonTextField;
        private SPTextField mNextButtonTextField;
        private SPTextField mEmptyTextField;
        private SPTextField mTrialTextField;

        private SPButton mScopeButton;
        private SPButton mPrevButton;
        private SPButton mNextButton;

        private PlayerIndex? mPlayerIndex;
        private SPImage mGamerPic;

        private SPSprite mCanvas;

        private int mScopeIndex;
        private List<LeaderboardScope> mScopes;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_MENU_LEADERBOARD; } }
        public bool IsTrialMode
        {
            get { return mIsTrialMode; }
            set
            {
                mIsTrialMode = value;
                mScopeButton.Visible = mScopeButton.Enabled = !value;
                mTrialTextField.Visible = value;

                if (value && CurrentScope != null)
                {
                    while (mScopeIndex != 0)
                        MoveNextScope();
                }
            }
        }
        private LeaderboardScope CurrentScope { get { return mScopes != null && mScopeIndex >= 0 && mScopeIndex < mScopes.Count ? mScopes[mScopeIndex] : null; } }
        private LeaderboardScope NextScope
        {
            get
            {
                if (mScopes == null || mScopes.Count == 0 || mScopeIndex < 0)
                    return null;
                else
                    return mScopes[(mScopeIndex + 1) % mScopes.Count];
            }
        }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvas != null)
                return;

            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            mCanvas = new SPSprite();
            mCanvas.X = ResManager.RESX(0);
            mCanvas.Y = ResManager.RESY(0);
            AddChild(mCanvas);
            ResManager.RESM.PopOffset();

            // Scope
            mScopeButton = new SPButton(mScene.TextureByName("large_face_a"));
            mScopeButton.X = 360;
            mScopeButton.Y = 496;
            mScopeButton.Visible = false;
            mScopeButton.AddActionEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, new Action<SPEvent>(OnScopeButtonPressed));
            mCanvas.AddChild(mScopeButton);

            mScopeButtonTextField = new SPTextField(256, 48, "", mScene.FontKey, 40);
            mScopeButtonTextField.X = 6 + mScopeButton.Width;
            mScopeButtonTextField.Y = (mScopeButton.Height - mScopeButtonTextField.Height) / 2;
            mScopeButtonTextField.HAlign = SPTextField.SPHAlign.Left;
            mScopeButtonTextField.VAlign = SPTextField.SPVAlign.Center;
            mScopeButtonTextField.Color = Color.Black;
            mScopeButton.AddContent(mScopeButtonTextField);
            mScopeButton.ScaleX = mScopeButton.ScaleY = 0.75f;

            // Prev
            mPrevButton = new SPButton(mScene.TextureByName("large_bumper_left"));
            mPrevButton.X = 200;
            mPrevButton.Y = 476;
            mPrevButton.ScaleX = mPrevButton.ScaleY = 0.85f;
            mPrevButton.AddActionEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, new Action<SPEvent>(OnPrevButtonPressed));
            mCanvas.AddChild(mPrevButton);

            mPrevButtonTextField = new SPTextField(128, 32, "", mScene.FontKey, 24);
            mPrevButtonTextField.X = (mPrevButton.Width - mPrevButtonTextField.Width) / 2;
            mPrevButtonTextField.Y = mPrevButton.Height;
            mPrevButtonTextField.HAlign = SPTextField.SPHAlign.Center;
            mPrevButtonTextField.VAlign = SPTextField.SPVAlign.Top;
            mPrevButtonTextField.Color = Color.Black;
            mPrevButton.AddContent(mPrevButtonTextField);

            // Next
            mNextButton = new SPButton(mScene.TextureByName("large_bumper_right"));
            mNextButton.X = 652;
            mNextButton.Y = 476;
            mNextButton.ScaleX = mNextButton.ScaleY = 0.85f;
            mNextButton.AddActionEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, new Action<SPEvent>(OnNextButtonPressed));
            mCanvas.AddChild(mNextButton);

            mNextButtonTextField = new SPTextField(128, 32, "", mScene.FontKey, 24);
            mNextButtonTextField.X = (mNextButton.Width - mNextButtonTextField.Width) / 2;
            mNextButtonTextField.Y = mNextButton.Height;
            mNextButtonTextField.HAlign = SPTextField.SPHAlign.Center;
            mNextButtonTextField.VAlign = SPTextField.SPVAlign.Top;
            mNextButtonTextField.Color = Color.Black;
            mNextButton.AddContent(mNextButtonTextField);

            // Empty
            mEmptyTextField = new SPTextField(420, 40, "No scores to show...", mScene.FontKey, 32);
            mEmptyTextField.X = 288;
            mEmptyTextField.Y = 280;
            mEmptyTextField.HAlign = SPTextField.SPHAlign.Center;
            mEmptyTextField.VAlign = SPTextField.SPVAlign.Top;
            mEmptyTextField.Color = Color.Black;
            mEmptyTextField.Visible = false;
            mCanvas.AddChild(mEmptyTextField);

            // Trial
            mTrialTextField = new SPTextField(630, 40, "Get the full version for global scores.", mScene.FontKey, 32);
            mTrialTextField.X = 212;
            mTrialTextField.Y = 496;
            mTrialTextField.HAlign = SPTextField.SPHAlign.Left;
            mTrialTextField.VAlign = SPTextField.SPVAlign.Top;
            mTrialTextField.Color = Color.Black;
            mTrialTextField.Visible = false;
            mCanvas.AddChild(mTrialTextField);

            // Gamer Picture
            if (mPlayerIndex.HasValue)
            {
                SPTexture gamerPicTexture = GameController.GC.ProfileManager.GamerPictureForPlayer(mPlayerIndex.Value);
                if (gamerPicTexture != null)
                {
                    mGamerPic = new SPImage(gamerPicTexture);
                    mGamerPic.X = 136;
                    mGamerPic.Y = 176;
                    mGamerPic.ScaleX = mGamerPic.ScaleY = 0.9f;
                    mGamerPic.Visible = false;
                    mCanvas.AddChild(mGamerPic);
                }
            }

            UpdatePageNav(null);
        }

        public void OnGamerPicsRefreshed(SPEvent ev)
        {
            if (mGamerPic == null || !mPlayerIndex.HasValue)
                return;

            SPTexture texture = GameController.GC.ProfileManager.GamerPictureForPlayer(mPlayerIndex.Value);
            if (texture != null)
                mGamerPic.Texture = texture;
            else
            {
                mGamerPic.RemoveFromParent();
                mGamerPic.Dispose();
                mGamerPic = null;
            }
        }

        private void UpdatePageNav(LeaderboardScope scope)
        {
            if (scope == null)
            {
                mPrevButton.Visible = mPrevButton.Enabled = false;
                mNextButton.Visible = mNextButton.Enabled = false;
            }
            else
            {
                int pageIndex = scope.PageIndex, maxPageIndex = scope.MaxPageIndex, scoresPerPage = scope.NumScoresPerPage;
                mPrevButton.Visible = mPrevButton.Enabled = maxPageIndex > 0 && pageIndex > 0;
                mNextButton.Visible = mNextButton.Enabled = maxPageIndex > 0 && pageIndex < maxPageIndex;

                if (mPrevButton.Visible)
                    mPrevButtonTextField.Text = string.Format("{0}-{1}", 1 + (pageIndex - 1) * scoresPerPage, pageIndex * scoresPerPage);
                if (mNextButton.Visible)
                    mNextButtonTextField.Text = string.Format("{0}-{1}", 1 + (pageIndex + 1) * scoresPerPage, (pageIndex + 2) * scoresPerPage);
                mEmptyTextField.Visible = scope.NumScoresCurrentPage == 0;
            }
        }

        public void AddScope(string name, string iconName, bool dynamicContent, bool showGamerPic, Func<int> maxIndexFunc, Func<int, int, HiScoreTable> contentFunc, Dictionary<string, string> localScorers = null)
        {
            if (mCanvas == null)
                return;
            if (mScopes == null)
                mScopes = new List<LeaderboardScope>(3);

            SPImage icon = null;
            if (iconName != null)
            {
                SPTexture iconTexture = mScene.TextureByName(iconName);
                if (iconTexture != null)
                {
                    icon = new SPImage(iconTexture);
                    icon.X = 212;
                    icon.Y = 106;
                }
            }

            LeaderboardScope scope = new LeaderboardScope(name, mScene.FontKey, mScopeWidth, mScopeHeight, mScoresPerPage,
                    icon, maxIndexFunc, contentFunc, localScorers);
            scope.ScoreView.X = 200;
            scope.ScoreView.Y = 158;
            scope.ShouldRefresh = dynamicContent;
            scope.ShowGamerPic = showGamerPic;
            scope.Visible = false;
            mScopes.Add(scope);
            mCanvas.AddChild(scope.ScoreView);
            if (icon != null)
                mCanvas.AddChild(icon);
            // Don't waste time refreshing scopes that will get refreshed again before they're viewed.
            if (mScopeIndex == -1 || !scope.ShouldRefresh)
                scope.RefreshContent(mScene); // Must place on stage before refreshing content.

            if (mScopeIndex == -1)
            {
                mScopeIndex = 0;
                scope.Visible = true;
                UpdatePageNav(scope);

                if (mGamerPic != null)
                    mGamerPic.Visible = scope.ShowGamerPic;
            }

            if (mScopes.Count > 1)
            {
                mScopeButton.Visible = true;
                mScopeButtonTextField.Text = NextScope == null ? "" : "Show " + NextScope.Name;
            }
        }

        public void MoveNextScope()
        {
            if (CurrentScope == null)
                return;

            CurrentScope.Visible = false;
            ++mScopeIndex;
            if (mScopeIndex >= mScopes.Count)
                mScopeIndex = 0;

            if (CurrentScope.ShouldRefresh)
                CurrentScope.RefreshContent(mScene);
            if (mGamerPic != null)
                mGamerPic.Visible = CurrentScope.ShowGamerPic;
            CurrentScope.Visible = true;
            UpdatePageNav(CurrentScope);
            mScopeButtonTextField.Text = NextScope == null ? "" : "Show " + NextScope.Name;
        }

        public bool MoveNextPage()
        {
            LeaderboardScope scope = CurrentScope;
            if (scope != null)
            {
                if (scope.PageIndex < scope.MaxPageIndex)
                {
                    ++scope.PageIndex;
                    scope.RefreshContent(mScene);
                    UpdatePageNav(scope);
                    return true;
                }
            }

            return false;
        }

        public bool MovePrevPage()
        {
            LeaderboardScope scope = CurrentScope;
            if (scope != null)
            {
                if (scope.PageIndex > 0)
                {
                    --scope.PageIndex;
                    scope.RefreshContent(mScene);
                    UpdatePageNav(scope);
                    return true;
                }
            }

            return false;
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            ControlsManager cm = ControlsManager.CM;

            if (cm.DidButtonDepress(Buttons.A))
                mScopeButton.AutomatedButtonDepress();
            else if (cm.DidButtonDepress(Buttons.LeftShoulder))
                mPrevButton.AutomatedButtonDepress();
            else if (cm.DidButtonDepress(Buttons.RightShoulder))
                mNextButton.AutomatedButtonDepress();

            if (cm.DidButtonRelease(Buttons.A))
                mScopeButton.AutomatedButtonRelease(true);
            if (cm.DidButtonRelease(Buttons.LeftShoulder))
                mPrevButton.AutomatedButtonRelease(true);
            if (cm.DidButtonRelease(Buttons.RightShoulder))
                mNextButton.AutomatedButtonRelease(true);

            if (CurrentScope != null)
                CurrentScope.ScoreView.Update(gpState, kbState);
        }

        private void PlayButtonSound()
        {
            mScene.PlaySound("Button");
        }

        private void OnScopeButtonPressed(SPEvent ev)
        {
            PlayButtonSound();
            MoveNextScope();
        }

        private void OnPrevButtonPressed(SPEvent ev)
        {
            if (MovePrevPage())
                PlayButtonSound();
        }

        private void OnNextButtonPressed(SPEvent ev)
        {
            if (MoveNextPage())
                PlayButtonSound();
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

                        if (mScopes != null)
                        {
                            foreach (LeaderboardScope scope in mScopes)
                                scope.DisposeContent();
                            mScopes = null;
                        }

                        if (mScopeButton != null)
                        {
                            mScopeButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED);
                            mScopeButton = null;
                        }

                        if (mPrevButton != null)
                        {
                            mPrevButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED);
                            mPrevButton = null;
                        }

                        if (mNextButton != null)
                        {
                            mNextButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED);
                            mNextButton = null;
                        }
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion

        private class LeaderboardScope
        {
            public LeaderboardScope(string name, string fontName, float width, float height, int scoresPerPage, SPImage icon,
                    Func<int> maxIndexFunc, Func<int, int, HiScoreTable> contentFunc, Dictionary<string, string> localScorers = null)
            {
                mName = name;
                mScoresPerPage = Math.Max(1, scoresPerPage);
                mMaxIndexFunc = maxIndexFunc;
                mContentFunc = contentFunc;
                mLocalScorers = localScorers;
                mIcon = icon;
                mPageIndex = 0;
                mScoreView = new TableView(0, width, height);
            }

            #region Fields2
            private bool mIsDisposed = false;
            private bool mShouldRefresh = true;
            private bool mShowGamerPic = false;
            private string mName;
            private int mPageIndex;
            private int mScoresPerPage;
            private SPImage mIcon;
            private Func<int> mMaxIndexFunc;
            private Func<int, int, HiScoreTable> mContentFunc;
            private TableView mScoreView;
            Dictionary<string, string> mLocalScorers;
            #endregion

            #region Properties2
            public bool ShouldRefresh { get { return mShouldRefresh; } set { mShouldRefresh = value; } }
            public bool ShowGamerPic { get { return mShowGamerPic; } set { mShowGamerPic = value; } }
            public bool Visible
            {
                get { return mScoreView != null ? mScoreView.Visible : false; }
                set
                {
                    if (mScoreView != null)
                        mScoreView.Visible = value;
                    if (mIcon != null)
                        mIcon.Visible = value;
                }
            }
            public string Name { get { return mName; } set { mName = value; } }
            public int PageIndex { get { return mPageIndex; } set { mPageIndex = value; } }
            public int MaxPageIndex { get { return Math.Max(0, (MaxIndex-1) / mScoresPerPage); } }
            public int MaxIndex { get { return mMaxIndexFunc != null ? mMaxIndexFunc() : mPageIndex; } }
            public int NumScoresPerPage { get { return mScoresPerPage; } }
            public int NumScoresCurrentPage { get { return mScoreView != null ? mScoreView.NumCells : 0; } }
            public SPImage Icon { get { return mIcon; } }
            public TableView ScoreView { get { return mScoreView; } }
            #endregion

            #region Methods2
            public void RefreshContent(SceneController scene)
            {
                if (mScoreView == null)
                    return;

                mScoreView.DisposeContent();
                
                HiScoreTable scoreTable = mContentFunc(mPageIndex, mScoresPerPage);
                if (scoreTable != null)
                {
                    scoreTable.SetLocalScorers(mLocalScorers);
                    mScoreView.BeginBatchAdd();
                    for (int i = 0; i < scoreTable.NumScores; ++i)
                        mScoreView.AddCell(scoreTable.HiScoreCellForIndex(i, scene));
                    mScoreView.EndBatchAdd();
                }

                mScoreView.UpdateViewport();
            }

            public void DisposeContent()
            {
                if (mIsDisposed)
                    return;

                mIsDisposed = true;
                if (mScoreView != null)
                {
                    mScoreView.Dispose();
                    mScoreView = null;
                }
            }
            #endregion
        }
    }
}
