using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class BookletSubview : TitleSubview, IInteractable
    {
        public const string CUST_EVENT_TYPE_BOOKLET_PAGE_TURNED = "bookletPageTurnedEvent";

        public BookletSubview(int category, string key)
            : base(category)
        {
            mAdvanceable = true;
            Touchable = true;
		    mLoop = true;
            mNavigationDisabled = false;
            mInputFocus = 0;
            mBookKey = key;
		    mPageTurns = 0;
		    mPageIndex = 0;
            mLeftInertia = new InputInertia();
            mRightInertia = new InputInertia();
		
		    mCover = null;
		    mCurrentPage = null;
        }
        
        #region Fields
        protected uint mInputFocus;
        protected bool mLoop;
        protected bool mNavigationDisabled;
        protected string mBookKey;
        protected int mPageTurns;
        protected int mNumPages;
        protected int mPageIndex;
        protected InputInertia mLeftInertia;
        protected InputInertia mRightInertia;

        protected TitleSubview mCover;
        protected MenuDetailView mCurrentPage;
        #endregion

        #region Properties
        public virtual uint InputFocus
        {
            get { return mInputFocus; }
            set
            {
                mScene.UnsubscribeToInputUpdates(this);
                mInputFocus = value;
                mScene.SubscribeToInputUpdates(this);
            }
        }
        public bool Loop { get { return mLoop; } set { mLoop = value; } }
        public bool NavigationDisabled { get { return mNavigationDisabled; } set { mNavigationDisabled = value; } }
        public string BookKey { get { return mBookKey; } }
        public int PageIndex { get { return mPageIndex; } set { mPageIndex = value; } }
        public int PageTurns { get { return mPageTurns; } set { mPageTurns = value; } }
        public int NumPages { get { return mNumPages; } set { mNumPages = value; ShowPageNo(mNumPages > 1); } }
        public TitleSubview Cover
        {
            get { return mCover; }
            set
            {
                if (mCover != null)
                    RemoveChild(mCover);

	            mCover = value;

                if (mCover != null)
                {
                    mCover.Touchable = true;
                    CloseSelector = mCover.CloseSelector;
                    AddChildAtIndex(mCover, 0);
                }
            }
        }
        public MenuDetailView CurrentPage
        {
            get { return mCurrentPage; }
            set
            {
                if (mCurrentPage != null)
                    RemoveChild(mCurrentPage);
	
	            mCurrentPage = value;

                if (mCurrentPage != null)
                {
                    mCurrentPage.Touchable = false;
                    AddChildAtIndex(mCurrentPage, Math.Min(NumChildren, mCover != null ? 1 : 0));
                }
            }
        }
        #endregion

        #region Methods
        public virtual void DidGainFocus() { }
        public virtual void WillLoseFocus() { }

        public override void Update(GamePadState gpState, KeyboardState kbState)
        {
            base.Update(gpState, kbState);

            if (mNavigationDisabled)
                return;

            GameController gc = GameController.GC;

            if (gpState.DPad.Left == ButtonState.Pressed || gpState.ThumbSticks.Left.X < -0.5f || kbState.IsKeyDown(Keys.Left | Keys.NumPad4))
            {
                if (mLeftInertia.CanMove)
                    PrevPage();
            }
            else
            {
                mLeftInertia.Reset();
            }

            if (gpState.DPad.Right == ButtonState.Pressed || gpState.ThumbSticks.Left.X > 0.5f || kbState.IsKeyDown(Keys.Right | Keys.NumPad6))
            {
                if (mRightInertia.CanMove)
                    NextPage();
            }
            else
            {
                mRightInertia.Reset();
            }
        }

        public override void AdvanceTime(double time)
        {
            mLeftInertia.AdvanceTime(time);
            mRightInertia.AdvanceTime(time);
        }

        protected void ShowPageNo(bool value)
        {
            SPTextField pageNo = null;
            SPButton prevButton = null, nextButton = null;

            try
            {
                if (mCover != null)
                {
                    if (mCover.MutableLabels.ContainsKey("pageNo"))
                    {
                        pageNo = mCover.MutableLabels["pageNo"];
                        pageNo.Visible = value;
                    }

                    if (mCover.Buttons.ContainsKey("prevPage"))
                    {
                        prevButton = mCover.Buttons["prevPage"];
                        prevButton.Visible = value;
                    }

                    if (mCover.Buttons.ContainsKey("nextPage"))
                    {
                        nextButton = mCover.Buttons["nextPage"];
                        nextButton.Visible = value;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        public override void Flip(bool enable)
        {
            if (enable)
            {
                ScaleX = -1;
                X = mScene.ViewWidth;
            }
            else
            {
                ScaleX = 1;
                X = 0;
            }
        }

        public void RefreshPageNo()
        {
            SPTextField pageNo = null;

            try
            {
                if (mCover != null && mCover.MutableLabels.ContainsKey("pageNo"))
                {
                    pageNo = mCover.MutableLabels["pageNo"];
                    pageNo.Text = (mPageIndex + 1).ToString() + "/" + mNumPages;
                }
            }
            catch (Exception) { }
        }

        public virtual void TurnToPage(int page)
        {
            if (page >= 0 && page < mNumPages)
            {
                mPageIndex = page;
                RefreshPageNo();
                PlayPageTurnSound();
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_BOOKLET_PAGE_TURNED));
            }
        }

        public virtual void NextPage()
        {
            int index = mPageIndex + 1;
	
	        if (mNumPages > 0 && ((index < mNumPages) || mLoop))
            {
		        ++mPageTurns;
                TurnToPage(index % mNumPages);
	        }
        }

        public virtual void PrevPage()
        {
            int index = mPageIndex - 1;
	
	        if (index < 0 && mLoop)
		        index += mNumPages;
	
	        if (mNumPages > 0 && index >= 0)
            {
		        --mPageTurns;
                TurnToPage(index);
	        }
        }

        private void PlayPageTurnSound()
        {
            mScene.PlaySound("PageTurn");
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
