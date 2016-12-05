using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class MasteryLog : Prop, IInteractable
    {
        public MasteryLog(int category, MasteryModel model)
            : base(category)
        {
            if (model == null)
                throw new ArgumentException("MasteryLog initialized with invalid arguments.");

            mAdvanceable = true;
            mModel = model;
            mPages = new Dictionary<uint, MasteryPage>();
            mCurrentPage = null;
            mMenu = null;
            mCostume = null;
            SetupProp();
        }

        #region Fields
        private Dictionary<uint, MasteryPage> mPages;
        private MasteryPage mCurrentPage;
        private MasteryMenu mMenu;
        private SPSprite mLogPage;
        private SPSprite mLogBook;
        private SPSprite mCostume;
        private MasteryModel mModel;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_MENU_MASTERY; } }
        private MasteryPage CurrentPage { get { return mCurrentPage; } set { mCurrentPage = value; } }
        public bool HasCurrentPage { get { return CurrentPage != null; } }
        public static Color HighlightedTextColor { get { return SPUtils.ColorFromColor(0x0074e1); } } // Light Blue: 0x0074e1
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;

            mCostume = new SPSprite();
            AddChild(mCostume);

            // Book
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

            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            mLogBook = new SPSprite();
            mLogBook.X = ResManager.RESX(480);
            mLogBook.Y = ResManager.RESY(312);
            mLogBook.AddChild(mLogPage);
            mCostume.AddChild(mLogBook);
            ResManager.RESM.PopOffset();

            // Menu
            uint[] treeKeys = mModel.TreeKeys;
            mMenu = new MasteryMenu(Category, treeKeys);
            mMenu.X = mLogBook.X - mLogBook.Width / 2;
            mMenu.Y = mLogBook.Y - mLogBook.Height / 2;
            mMenu.AddEventListener(MasteryMenu.CUST_EVENT_TYPE_MASTERY_PAGE_REQUESTED, (SPEventHandler)OnPageRequested);
            mMenu.RefreshWithModel(mModel);
            mCostume.AddChild(mMenu);

            // Pages
            foreach (uint treeKey in treeKeys)
            {
                MasteryTree tree = mModel.TreeForKey(treeKey);
                MasteryPage page = new MasteryPage(Category, tree, new MasteryTreeView(Category, 320, 320, tree));
                page.X = mLogBook.X - mLogBook.Width / 2;
                page.Y = mLogBook.Y - mLogBook.Height / 2;
                page.Visible = false;
                mCostume.AddChild(page);
                mPages.Add(treeKey, page);
            }
        }

        public void DidGainFocus()
        {
            if (CurrentPage != null)
                CurrentPage.DidGainFocus();
            else if (mMenu != null)
                mMenu.DidGainFocus();
        }

        public void WillLoseFocus()
        {
            if (CurrentPage != null)
                CurrentPage.WillLoseFocus();
            else if (mMenu != null)
                mMenu.WillLoseFocus();
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (CurrentPage != null)
                CurrentPage.Update(gpState, kbState);
            else if (mMenu != null)
                mMenu.Update(gpState, kbState);
        }

        public void TurnToPage(uint key)
        {
            if (mPages == null || mMenu == null)
                return;

            if (mPages.ContainsKey(key))
            {
                if (CurrentPage != null)
                    CurrentPage.Visible = false;
                mModel.SelectTreeForKey(key);
                mModel.BeginNavigation();
                CurrentPage = mPages[key];
                CurrentPage.RefreshNodes();
                CurrentPage.Visible = true;
                mMenu.Visible = false;
                mScene.PlaySound("PageTurn");
            }
        }

        public void TurnToMenu()
        {
            if (CurrentPage != null)
                CurrentPage.Visible = false;
            CurrentPage = null;

            if (mMenu != null)
            {
                mMenu.RefreshWithModel(mModel);
                mMenu.Visible = true;
                mScene.PlaySound("PageTurn");
            }
        }

        public void OnPageRequested(SPEvent ev)
        {
            if (mCurrentPage != null || mMenu == null)
                return;

            mScene.PlaySound("Button");
            TurnToPage(mMenu.PageRequestKey);
        }

        public override void AdvanceTime(double time)
        {
            if (mMenu != null)
                mMenu.AdvanceTime(time);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mPages != null)
                        {
                            foreach (KeyValuePair<uint, MasteryPage> kvp in mPages)
                                kvp.Value.Dispose();
                            mPages = null;
                        }

                        if (mMenu != null)
                        {
                            mMenu.RemoveEventListener(MasteryMenu.CUST_EVENT_TYPE_MASTERY_PAGE_REQUESTED, (SPEventHandler)OnPageRequested);
                            mMenu.Dispose();
                            mMenu = null;
                        }

                        mCurrentPage = null;
                        mCostume = null;
                        mModel = null;
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
