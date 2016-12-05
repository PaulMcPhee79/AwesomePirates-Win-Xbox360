using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class TableView : Prop, IInteractable
    {
        protected const float kDefaultScrollSpeed = 8.0f;
        protected const float kDefaultScrollAccel = 0.25f;
        protected const float kDefaultAccelLag = 512f;

        public TableView(int category, float width, float height)
            : base(category)
        {
            mViewWidth = width;
            mViewHeight = height;
            mBatching = false;
            mTallestCell = 0;
            mContentHeight = 0;
            mScrollSpeed = kDefaultScrollSpeed;
            mScrollAccel = 1;
            mAccelLag = kDefaultAccelLag;
            mPrevThumbY = 0;
            mPrevScrollDir = 0;
            mInputFocus = 0;
            mButtonsProxy = null;
            mCells = new List<SPDisplayObject>();
            SetupProp();
        }

        #region Fields
        private bool mBatching;
        private float mTallestCell;
        private float mViewWidth;
        private float mViewHeight;
        private float mContentHeight;
        private float mScrollSpeed;
        private float mScrollAccel;
        private float mAccelLag;
        private float mPrevThumbY;
        private int mPrevScrollDir;
        private uint mInputFocus;
        private SPQuad mBoundsQuad;
        private SPQuad mScrollBar;
        protected Prop mContent;
        protected CroppedProp mContainer;
        protected ButtonsProxy mButtonsProxy;
        protected List<SPDisplayObject> mCells;
        #endregion

        #region Properties
        public virtual uint InputFocus { get { return mInputFocus; } set { mInputFocus = value; } }
        public float ViewWidth
        {
            get { return mViewWidth; }
            set
            {
                mViewWidth = Math.Max(0, value);
                ViewDimensionsDidChange();
            }
        }
        public float ViewHeight
        {
            get { return mViewHeight; }
            set
            {
                mViewHeight = Math.Max(0, value);
                ViewDimensionsDidChange();
            }
        }
        protected float FlooredContentHeight { get { return Math.Max(mViewHeight, mContentHeight); } }
        public float ScrollPercent { get { return Math.Abs(mContent.Y / FlooredContentHeight); } }
        public float ScrollSpeed { get { return mScrollSpeed; } set { mScrollSpeed = value; } }
        public int NumCells { get { return mCells.Count; } }
        public bool AtStart { get { return SPMacros.SP_IS_FLOAT_EQUAL(mContent.Y, 0); } }
        public bool AtEnd { get { return mViewHeight > mContentHeight || SPMacros.SP_IS_FLOAT_EQUAL(mContent.Y, mViewHeight - mContentHeight); } }
        #endregion
        
        #region Methods
        protected override void SetupProp()
        {
            if (mContent != null)
                return;

            mBoundsQuad = new SPQuad(mViewWidth, mViewHeight);
            mBoundsQuad.Visible = false;
            AddChild(mBoundsQuad);

            mContent = new Prop(Category);
            mContainer = new CroppedProp(Category, Rectangle.Empty);
            mContainer.AddChild(mContent);
            AddChild(mContainer);

            mScrollBar = new SPQuad(10, mViewHeight);
            mScrollBar.X = mViewWidth - mScrollBar.Width;
            mScrollBar.Color = Color.LawnGreen;
            mScrollBar.Alpha = 0;
            AddChild(mScrollBar);
        }

        public void UpdateViewport()
        {
            ViewDimensionsDidChange();
        }

        protected virtual void ContentDimensionsDidChange()
        {
            UpdateCellVisibility();
            UpdateScrollBarSize();
        }

        protected virtual void ViewDimensionsDidChange()
        {
            if (Stage == null)
                return;

            mContainer.ViewableRegion = mBoundsQuad.BoundsInSpace(Stage).ToRectangle();
            UpdateCellVisibility();
        }

        protected void UpdateCellVisibility()
        {
            if (mCells == null || mContainer == null || mContent == null)
                return;

            Rectangle viewableRegion = mContainer.ViewableRegion;
            foreach (SPDisplayObject cell in mCells)
                cell.Visible = ((mContent.Y + cell.Y - mScene.BaseSprite.Y / 2) * mScene.ViewScale < viewableRegion.Height && (mContent.Y + cell.Y) > -mTallestCell);
                //cell.Visible = ((mContent.Y + cell.Y) * mScene.SafeAreaFactor < viewableRegion.Height && mContent.Y + cell.Y > -mTallestCell);
        }

        public void SetScrollBarColor(Color color)
        {
            if (mScrollBar != null)
                mScrollBar.Color = color;
        }

        private void UpdateScrollBarPosition()
        {
            mScrollBar.Y = mViewHeight * ScrollPercent;
        }

        private void UpdateScrollBarSize()
        {
            mScrollBar.ScaleY = mViewHeight / FlooredContentHeight;
        }

        public void BeginBatchAdd()
        {
            mBatching = true;
        }

        public void EndBatchAdd()
        {
            if (mBatching)
            {
                mBatching = false;
                ContentDimensionsDidChange();
            }
        }

        public void AddCell(SPDisplayObject cell)
        {
            if (cell == null || mCells == null || mContent == null)
                return;

            cell.Y = mContentHeight;
            mContent.AddChild(cell);
            mCells.Add(cell);

            float cellHeight = cell.Height;
            mContentHeight += cell.Height;

            if (cellHeight > mTallestCell)
                mTallestCell = cellHeight;

            if (!mBatching)
                ContentDimensionsDidChange();
        }

        public void RemoveCell(SPDisplayObject cell)
        {
            if (cell == null || mCells == null || mContent == null)
                return;

            int index = mCells.IndexOf(cell);

            if (index != -1)
            {
                mContent.RemoveChild(cell);
                mCells.Remove(cell);

                int numCells = NumCells;
                float yAdjust = cell.Height;

                for (int i = index; i < numCells; ++i)
                {
                    SPDisplayObject childCell = mContent.ChildAtIndex(i);

                    if (childCell != null)
                        childCell.Y -= yAdjust;
                }

                mContentHeight -= cell.Height;
                ContentDimensionsDidChange();
            }
        }

        public void RemoveCellAtIndex(int index)
        {
            if (mCells != null && index < mCells.Count)
                RemoveCell(mCells[index]);
        }

        public void AddButton(MenuButton menuButton, Buttons xnaButton)
        {
            if (mButtonsProxy == null)
                mButtonsProxy = new ButtonsProxy(InputFocus);
            mButtonsProxy.AddButton(menuButton, xnaButton);
        }

        public void RemoveButton(MenuButton menuButton)
        {
            if (mButtonsProxy != null)
                mButtonsProxy.RemoveButton(menuButton);
        }

        public void ScrollToStart()
        {
            if (mContent != null)
            {
                mContent.Y = 0;
                UpdateCellVisibility();
                UpdateScrollBarPosition();
                mScrollBar.Alpha = 1f;
            }
        }

        public void ScrollToEnd()
        {
            if (mContent != null)
            {
                mContent.Y = Math.Min(0, mViewHeight - mContentHeight);
                UpdateCellVisibility();
                UpdateScrollBarPosition();
                mScrollBar.Alpha = 1f;
            }
        }

        public void Scroll(float distance)
        {
            if (mContent != null)
            {
                mContent.Y = Math.Min(0, Math.Max(mViewHeight - mContentHeight, mContent.Y + distance));
                UpdateCellVisibility();
                UpdateScrollBarPosition();
                mScrollBar.Alpha = Math.Min(mScrollBar.Alpha + 0.04f, 1f);
            }
        }

        public void DidGainFocus() { }

        public virtual void WillLoseFocus() { }

        public virtual void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (!Visible)
                return;

            int scrollDir = 0;
            float speed = mScrollSpeed + mScrollAccel;

            if (gpState.DPad.Up == ButtonState.Pressed)
            {
                Scroll(speed);
                scrollDir = -1;
            }
            else if (gpState.DPad.Down == ButtonState.Pressed)
            {
                Scroll(-speed);
                scrollDir = 1;
            }
            else if (gpState.ThumbSticks.Left.Y != 0)
            {
                float thumbY = gpState.ThumbSticks.Left.Y;
                Scroll(thumbY * speed);

                // On Threshold = 0.8, Off Threshold = 0.6, On Deadzone = 0.2 (to prevent stutter from thumbstick bounce)
                if (thumbY > 0.8)
                    scrollDir = -1;
                else if (thumbY < -0.8)
                    scrollDir = 1;
                else if (mPrevThumbY > 0.6)
                    scrollDir = -1;
                else if (mPrevThumbY < -0.6)
                    scrollDir = 1;

                if (scrollDir != 0)
                    mPrevThumbY = thumbY;
            }
            else
                mScrollBar.Alpha = Math.Max(mScrollBar.Alpha - 0.04f, 0f);

            if (scrollDir == 0 || scrollDir != mPrevScrollDir)
            {
                mScrollAccel = kDefaultScrollAccel;
                mAccelLag = kDefaultAccelLag;
            }
            else
            {
                if (mAccelLag > 0)
                    mAccelLag -= speed;
                else
                    mScrollAccel += kDefaultScrollAccel;
            }
            mPrevScrollDir = scrollDir;

            if (mButtonsProxy != null)
                mButtonsProxy.Update(gpState, kbState);
        }

        public void DisposeContent()
        {
            if (mCells != null)
            {
                if (mContent != null)
                    mContent.RemoveAllChildren();
                if (mButtonsProxy != null)
                    mButtonsProxy.Clear();

                foreach (SPDisplayObject cell in mCells)
                    cell.Dispose();
                mCells.Clear();
            }

            mTallestCell = 0;
            mContentHeight = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        DisposeContent();
                        mContent = null;
                        mContainer = null;
                        mButtonsProxy = null;
                        mCells = null;
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
