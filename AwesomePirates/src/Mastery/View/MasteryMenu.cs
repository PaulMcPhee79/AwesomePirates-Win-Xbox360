using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class MasteryMenu : Prop
    {
        public const string CUST_EVENT_TYPE_MASTERY_PAGE_REQUESTED = "masteryPageRequestedEvent";

        public MasteryMenu(int category, uint[] treeKeys)
            : base(category)
        {
            if (treeKeys == null || treeKeys.Length == 0 )
                throw new ArgumentException("MasteryMenu initialized with invalid arguments.");

            mCostume = null;
            mPageRequestKey = 0;
            mTreeKeys = new uint[treeKeys.Length];

            for (int i = 0; i < treeKeys.Length; ++i)
                mTreeKeys[i] = treeKeys[i];

            mPageTitles = new List<SPTextField>();
            mPageStats = new List<SPTextField>();
            mPageButtons = new List<MenuButton>();
            mPageButtonProxy = new ButtonsProxy(InputManager.HAS_FOCUS_MENU_MASTERY, Globals.kNavVertical, true);
            mPageButtonProxy.Repeats = true;
            mPageButtonProxy.RepeatDelay = 0.3;
            mPageButtonProxy.AddEventListener(ButtonsProxy.CUST_EVENT_TYPE_DID_NAVIGATE_BUTTONS, (SPEventHandler)ButtonsDidNavigate);

            SetupProp();
        }

        #region Fields
        private ShadowTextField mMenuTitle;
        private SPTextField mMasteryLevel;
        private SPTextField mMasteryBuild;
        private SPTextField mMasteryPoints;
        private SPTextField mMasterySpecialty;
        private MasteryXPPanel mXPPanel;
        private SPSprite mCostume;

        private ButtonsProxy mPageButtonProxy;
        private List<MenuButton> mPageButtons;
        private List<SPTextField> mPageTitles;
        private List<SPTextField> mPageStats;
        private uint[] mTreeKeys;
        private uint mPageRequestKey;
        #endregion

        #region Properties
        public uint PageRequestKey { get { return mPageRequestKey; } private set { mPageRequestKey = value; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;

            mCostume = new SPSprite();
            AddChild(mCostume);

            Color dynamicTextColor = MasteryLog.HighlightedTextColor;

            // Left side of menu
            mMenuTitle = new ShadowTextField(Category, 220, 72, 48, "Mastery", mScene.FontKey);
            mMenuTitle.X = 112;
            mMenuTitle.Y = 16;
            mMenuTitle.FontColor = SPUtils.ColorFromColor(0x797ca9);
            mCostume.AddChild(mMenuTitle);

            // Mastery Level
            int staticTextFontSize = 24, dynamicTextFontSize = 26;
            float staticTextX = 72, staticTextY = 104, staticTextTrail = 8;
            SPTextField textField = new SPTextField(176, 40, "Mastery Level:", mScene.FontKey, staticTextFontSize);
            textField.X = staticTextX;
            textField.Y = staticTextY;
            textField.HAlign = SPTextField.SPHAlign.Right;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = Color.Black;
            mCostume.AddChild(textField);

            mMasteryLevel = new SPTextField(134, 48, "", mScene.FontKey, dynamicTextFontSize);
            mMasteryLevel.X = textField.X + textField.Width + staticTextTrail;
            mMasteryLevel.Y = textField.Y;
            mMasteryLevel.HAlign = SPTextField.SPHAlign.Left;
            mMasteryLevel.VAlign = SPTextField.SPVAlign.Top;
            mMasteryLevel.Color = Color.Black;
            mCostume.AddChild(mMasteryLevel);

            // Mastery Build
            staticTextY += 48;
            textField = new SPTextField(176, 40, "Mastery Build:", mScene.FontKey, staticTextFontSize);
            textField.X = staticTextX;
            textField.Y = staticTextY;
            textField.HAlign = SPTextField.SPHAlign.Right;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = Color.Black;
            mCostume.AddChild(textField);

            mMasteryBuild = new SPTextField(134, 48, "", mScene.FontKey, dynamicTextFontSize);
            mMasteryBuild.X = textField.X + textField.Width + staticTextTrail;
            mMasteryBuild.Y = textField.Y;
            mMasteryBuild.HAlign = SPTextField.SPHAlign.Left;
            mMasteryBuild.VAlign = SPTextField.SPVAlign.Top;
            mMasteryBuild.Color = Color.Black;
            mCostume.AddChild(mMasteryBuild);

            // Mastery Points
            staticTextY += 48;
            textField = new SPTextField(176, 40, "Points Used:", mScene.FontKey, staticTextFontSize);
            textField.X = staticTextX;
            textField.Y = staticTextY;
            textField.HAlign = SPTextField.SPHAlign.Right;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = Color.Black;
            mCostume.AddChild(textField);

            mMasteryPoints = new SPTextField(134, 48, "", mScene.FontKey, dynamicTextFontSize);
            mMasteryPoints.X = textField.X + textField.Width + staticTextTrail;
            mMasteryPoints.Y = textField.Y;
            mMasteryPoints.HAlign = SPTextField.SPHAlign.Left;
            mMasteryPoints.VAlign = SPTextField.SPVAlign.Top;
            mMasteryPoints.Color = Color.Black;
            mCostume.AddChild(mMasteryPoints);

            // Specialty
            staticTextY += 48;
            textField = new SPTextField(176, 40, "Specialty:", mScene.FontKey, staticTextFontSize);
            textField.X = staticTextX;
            textField.Y = staticTextY;
            textField.HAlign = SPTextField.SPHAlign.Right;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = Color.Black;
            mCostume.AddChild(textField);

            mMasterySpecialty = new SPTextField(134, 48, "", mScene.FontKey, dynamicTextFontSize);
            mMasterySpecialty.X = textField.X + textField.Width + staticTextTrail;
            mMasterySpecialty.Y = textField.Y;
            mMasterySpecialty.HAlign = SPTextField.SPHAlign.Left;
            mMasterySpecialty.VAlign = SPTextField.SPVAlign.Top;
            mMasterySpecialty.Color = Color.Black;
            mCostume.AddChild(mMasterySpecialty);

            // XP Panel
            mXPPanel = new MasteryXPPanel(Category);
            mXPPanel.X = 68;
            mXPPanel.Y = 316;
            mCostume.AddChild(mXPPanel);

            // Right side of menu
            float treeIntervalY = 138;
            staticTextX = 450; staticTextY = 22;

            
            for (int i = 0; i < mTreeKeys.Length; ++i)
            {
                uint treeKey = mTreeKeys[i];
                SPTextField treeTitle = new SPTextField(240, 36, MasteryGuiHelper.TitleForTree(treeKey), mScene.FontKey, 28);
                treeTitle.X = staticTextX;
                treeTitle.Y = staticTextY + i * treeIntervalY;
                treeTitle.HAlign = SPTextField.SPHAlign.Center;
                treeTitle.VAlign = SPTextField.SPVAlign.Top;
                treeTitle.Color = (i == 0) ? dynamicTextColor : Color.Black;
                mCostume.AddChild(treeTitle);
                mPageTitles.Add(treeTitle);

                SPTextField treeStats = new SPTextField(96, 32, MasteryGuiHelper.TitleForTree(treeKey), mScene.FontKey, 24);
                treeStats.X = 642;
                treeStats.Y = 116 + i * treeIntervalY;
                treeStats.HAlign = SPTextField.SPHAlign.Right;
                treeStats.VAlign = SPTextField.SPVAlign.Top;
                treeStats.Color = (i == 0) ? dynamicTextColor : Color.Black;
                mCostume.AddChild(treeStats);
                mPageStats.Add(treeStats);

                MenuButton pageButton = new MenuButton(null, mScene.TextureByName(MasteryGuiHelper.TextureNameForTree(treeKey)));
                pageButton.Tag = treeKey;
                pageButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnPageButtonPressed);
                mPageButtons.Add(pageButton);
                mPageButtonProxy.AddButton(pageButton, Buttons.A);

                if (pageButton.SelectedEffecter != null)
                    pageButton.SelectedEffecter.Factor = 1.5f;

                SPSprite pageButtonSprite = new SPSprite();
                pageButtonSprite.AddChild(pageButton);
                pageButtonSprite.ScaleX = pageButtonSprite.ScaleY = 0.8f;
                pageButtonSprite.X = treeTitle.X + (treeTitle.Width - pageButtonSprite.Width) / 2;
                pageButtonSprite.Y = treeTitle.Y + treeTitle.Height + 6;
                mCostume.AddChild(pageButtonSprite);
            }
        }

        public void ButtonsDidNavigate(SPEvent ev)
        {
            if (mPageButtons == null || mPageButtonProxy == null || mPageTitles == null || mPageStats == null || !(mPageTitles.Count == mPageStats.Count))
                return;

            MenuButton selectedButton = mPageButtonProxy.SelectedButton;
            if (selectedButton != null)
            {
                int selectedIndex = mPageButtons.IndexOf(selectedButton);
                Color selectedTextColor = MasteryLog.HighlightedTextColor;
                for (int i = 0; i < mPageTitles.Count; ++i)
                {
                    if (i == selectedIndex)
                    {
                        mPageTitles[i].Color = selectedTextColor;
                        mPageStats[i].Color = selectedTextColor;
                    }
                    else
                    {
                        mPageTitles[i].Color = Color.Black;
                        mPageStats[i].Color = Color.Black;
                    }
                }
            }
        }

        private void OnPageButtonPressed(SPEvent ev)
        {
            if (ev.CurrentTarget != null && ev.CurrentTarget is MenuButton)
            {
                MenuButton button = ev.CurrentTarget as MenuButton;
                PageRequestKey = button.Tag;
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MASTERY_PAGE_REQUESTED));
            }
        }

        public void RefreshWithModel(MasteryModel model)
        {
            if (model == null || mTreeKeys == null)
                return;

            // Left side of menu
            int i;
            uint treeKey;
            Color selectedTextColor = SPUtils.ColorFromColor(0x147fe3);
            MasteryTree tree = null;
            mMasteryLevel.Text = model.MasteryLevel.ToString() + "/" + MasteryModel.kMaxMasteryLevel;

            int maxPoints = 0;
            uint specialtyTree = 0;
            string buildText = "(";
            for (i = 0; i < mTreeKeys.Length; ++i)
            {
                treeKey = mTreeKeys[i];
                int treePoints = model.PointsForTree(treeKey);

                if (treePoints > maxPoints)
                {
                    maxPoints = treePoints;
                    specialtyTree = treeKey;
                }

                if (i < mTreeKeys.Length - 1)
                    buildText = buildText + treePoints + "/";
                else
                    buildText = buildText + treePoints;
            }
            buildText += ")";
            mMasteryBuild.Text = buildText;

            mMasteryPoints.Text = model.PointsSpent.ToString() + "/" + model.PointsTotal;
            mMasteryPoints.Color = (model.PointsSpent < model.PointsTotal) ? Color.Green : Color.Black;
            mMasterySpecialty.Text = (specialtyTree == 0) ? "-" : MasteryGuiHelper.TitleForSpecialty(specialtyTree);

            // XP Panel
            mXPPanel.PercentComplete = model.LevelXPFraction;

            // Right side of menu
            for (i = 0; i < mTreeKeys.Length; ++i)
            {
                if (mPageStats == null || i >= mPageStats.Count)
                    break;

                treeKey = mTreeKeys[i];
                tree = model.TreeForKey(treeKey);

                SPTextField pageStat = mPageStats[i];
                pageStat.Text = "(" + model.PointsForTree(treeKey) + "/" + ((tree != null) ? tree.RowCount : 12) + ")"; //model.PointsCapacityForTree(treeKey)
            }
        }

        public void DidGainFocus()
        {
            if (mPageButtonProxy != null)
                mPageButtonProxy.DidGainFocus();
        }

        public void WillLoseFocus()
        {
            if (mPageButtonProxy != null)
                mPageButtonProxy.WillLoseFocus();
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mPageButtonProxy != null)
                mPageButtonProxy.Update(gpState, kbState);
        }

        public override void AdvanceTime(double time)
        {
            if (mPageButtonProxy != null)
                mPageButtonProxy.AdvanceTime(time);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mPageButtonProxy != null)
                        {
                            mPageButtonProxy.RemoveEventListener(ButtonsProxy.CUST_EVENT_TYPE_DID_NAVIGATE_BUTTONS, (SPEventHandler)ButtonsDidNavigate);
                            mPageButtonProxy = null;
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
