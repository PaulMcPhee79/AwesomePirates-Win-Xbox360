using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class MasteryPage : Prop
    {
        public MasteryPage(int category, MasteryTree treeData, MasteryTreeView treeView)
            : base(category)
        {
            if (treeData == null || treeData.RowCount == 0 || treeView == null)
                throw new ArgumentException("MasteryPage initialized with invalid arguments.");
            mTreeData = treeData;
            mTreeView = treeView;
            mFeaturedNodes = new Dictionary<uint, MasteryNodeView>();
            mCostume = null;
            SetupProp();
        }

        #region Fields
        private MasteryTree mTreeData;
        private MasteryTreeView mTreeView;

        private SPButton mSpendButton;
        private SPButton mRefundButton;
        private SPButton mResetButton;

        private ShadowTextField mPageTitle;
        private SPImage mEmptyFeature;
        private Dictionary<uint, MasteryNodeView> mFeaturedNodes;
        private SPTextField mFeaturedPoints;
        private SPTextField mFeaturedTitle;
        private SPTextField mFeaturedDesc;
        private SPTextField mPointsRemaining;
        private SPSprite mCostume;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;

            mCostume = new SPSprite();
            AddChild(mCostume);

            // Tree View
            mTreeView.X = 420;
            mTreeView.Y = 46;
            mCostume.AddChild(mTreeView);

            // Page Title
            mPageTitle = new ShadowTextField(Category, 300, 48, 40, MasteryGuiHelper.TitleForTree(mTreeData.Key), mScene.FontKey);
            mPageTitle.X = 80;
            mPageTitle.Y = 28;
            mPageTitle.FontColor = SPUtils.ColorFromColor(0x797ca9);
            mCostume.AddChild(mPageTitle);

            // Empty Feature (for when there is no featured node)
            mEmptyFeature = new SPImage(mScene.TextureByName(MasteryGuiHelper.TextureNameForNodeActiveBg(0)));
            mEmptyFeature.X = mPageTitle.X + (mPageTitle.Width - mEmptyFeature.Width) / 2;
            mEmptyFeature.Y = mPageTitle.Y + mPageTitle.Height + 20;
            mCostume.AddChild(mEmptyFeature);

            // Featured Nodes
            foreach (MasteryRow row in mTreeData.Rows)
            {
                if (row.NodeCount == 0)
                    continue;

                foreach (MasteryNode node in row.Nodes)
                {
                    MasteryNodeView nodeView = new MasteryNodeView(Category, node.Key);
                    nodeView.X = mEmptyFeature.X + mEmptyFeature.Width / 2;
                    nodeView.Y = mEmptyFeature.Y + mEmptyFeature.Height / 2;
                    nodeView.ScaleX = nodeView.ScaleY = 88f / nodeView.IconWidth;
                    nodeView.Visible = false;
                    mFeaturedNodes.Add(node.Key, nodeView);
                    mCostume.AddChild(nodeView);
                }
            }

            // Featured Points
            mFeaturedPoints = new SPTextField(64, 40, "", mScene.FontKey, 28);
            mFeaturedPoints.X = mEmptyFeature.X + (mEmptyFeature.Width - mFeaturedPoints.Width) / 2;
            mFeaturedPoints.Y = mEmptyFeature.Y + mEmptyFeature.Height + 4;
            mFeaturedPoints.HAlign = SPTextField.SPHAlign.Center;
            mFeaturedPoints.VAlign = SPTextField.SPVAlign.Top;
            mFeaturedPoints.Color = Color.Black;
            mCostume.AddChild(mFeaturedPoints);

            // +/- TextFields
            SPTextField plusTextField = new SPTextField(48, 40, "+", mScene.FontKey, 40);
            plusTextField.X = mFeaturedPoints.X - plusTextField.Width;
            plusTextField.Y = mFeaturedPoints.Y;
            plusTextField.HAlign = SPTextField.SPHAlign.Left;
            plusTextField.VAlign = SPTextField.SPVAlign.Top;
            plusTextField.Color = SPUtils.ColorFromColor(0x0c6e0a);
            mCostume.AddChild(plusTextField);

            SPTextField minusTextField = new SPTextField(48, 40, "-", mScene.FontKey, 40);
            minusTextField.X = mFeaturedPoints.X + mFeaturedPoints.Width;
            minusTextField.Y = mFeaturedPoints.Y - 4;
            minusTextField.HAlign = SPTextField.SPHAlign.Right;
            minusTextField.VAlign = SPTextField.SPVAlign.Top;
            minusTextField.Color = SPUtils.ColorFromColor(0xbd0404);
            mCostume.AddChild(minusTextField);

            // Featured Title
            mFeaturedTitle = new SPTextField(300, 34, "", mScene.FontKey, 26);
            mFeaturedTitle.X = mPageTitle.X;
            mFeaturedTitle.Y = mFeaturedPoints.Y + mFeaturedPoints.Height + 12;
            mFeaturedTitle.HAlign = SPTextField.SPHAlign.Center;
            mFeaturedTitle.VAlign = SPTextField.SPVAlign.Top;
            mFeaturedTitle.Color = MasteryLog.HighlightedTextColor;
            mCostume.AddChild(mFeaturedTitle);

            // Featured Description
            mFeaturedDesc = new SPTextField(320, 108, "", mScene.FontKey, 24);
            mFeaturedDesc.X = -10 + mPageTitle.X + (mPageTitle.Width - mFeaturedTitle.Width) / 2;
            mFeaturedDesc.Y = mFeaturedTitle.Y + mFeaturedTitle.Height;
            mFeaturedDesc.HAlign = SPTextField.SPHAlign.Left;
            mFeaturedDesc.VAlign = SPTextField.SPVAlign.Top;
            mFeaturedDesc.Color = Color.Black;
            mCostume.AddChild(mFeaturedDesc);

            // Points Remaining
            SPTextField pointsRemaining = new SPTextField(240, 36, "Points Remaining: ", mScene.FontKey, 28);
            pointsRemaining.X = mFeaturedDesc.X;
            pointsRemaining.Y = mFeaturedDesc.Y + mFeaturedDesc.Height;
            pointsRemaining.HAlign = SPTextField.SPHAlign.Left;
            pointsRemaining.VAlign = SPTextField.SPVAlign.Top;
            pointsRemaining.Color = SPUtils.ColorFromColor(0x0c6e0a);
            mCostume.AddChild(pointsRemaining);

            mPointsRemaining = new SPTextField(280, 80, "", mScene.FontKey, 28);
            mPointsRemaining.X = pointsRemaining.X + pointsRemaining.TextBounds.Width;
            mPointsRemaining.Y = pointsRemaining.Y;
            mPointsRemaining.HAlign = SPTextField.SPHAlign.Left;
            mPointsRemaining.VAlign = SPTextField.SPVAlign.Top;
            mPointsRemaining.Color = SPUtils.ColorFromColor(0x0c6e0a);
            mCostume.AddChild(mPointsRemaining);

            // Buttons
            mSpendButton = new SPButton(mScene.TextureByName("large_face_a"));
            mSpendButton.X = plusTextField.X - (0.8f * mSpendButton.Width + 4);
            mSpendButton.Y = plusTextField.Y - 4;
            mSpendButton.ScaleX = mSpendButton.ScaleY = 0.8f;
            mCostume.AddChild(mSpendButton);

            mRefundButton = new SPButton(mScene.TextureByName("large_face_x"));
            mRefundButton.X = minusTextField.X + minusTextField.Width + 4;
            mRefundButton.Y = minusTextField.Y;
            mRefundButton.ScaleX = mRefundButton.ScaleY = 0.8f;
            mCostume.AddChild(mRefundButton);

            mResetButton = new SPButton(mScene.TextureByName("large_face_y"));
            mResetButton.ScaleX = mResetButton.ScaleY = 0.8f;
            SPTextField resetAllTextField = new SPTextField(180, 40, "Reset All", mScene.FontKey, 40);
            resetAllTextField.X = mResetButton.Width + 10;
            resetAllTextField.Y = (mResetButton.Height - resetAllTextField.Height) / 2;
            resetAllTextField.HAlign = SPTextField.SPHAlign.Left;
            resetAllTextField.VAlign = SPTextField.SPVAlign.Center;
            resetAllTextField.Color = Color.Black;
            mResetButton.AddContent(resetAllTextField);
            mResetButton.X = 490;
            mResetButton.Y = 368;
            mCostume.AddChild(mResetButton);
        }

        public void RefreshNodes()
        {
            if (mTreeData == null)
                return;

            MasteryNode selectedNode = mTreeData.SelectedNode;
            uint featureKey = (selectedNode != null) ? selectedNode.Key : 0;
            MasteryNode.NodeState nodeState = mTreeData.NodeStateForKey(featureKey);

            mEmptyFeature.Visible = true;

            foreach (KeyValuePair<uint, MasteryNodeView> kvp in mFeaturedNodes)
            {
                if (kvp.Key == featureKey)
                {
                    mEmptyFeature.Visible = false;
                    kvp.Value.Visible = true;
                    kvp.Value.State = mTreeData.NodeStateForKey(kvp.Key);
                }
                else
                {
                    kvp.Value.Visible = false;
                }
            }

            if (selectedNode != null)
            {
                mFeaturedPoints.Text = selectedNode.Points.ToString() + "/" + selectedNode.PointsCapacity;
                mFeaturedTitle.Text = MasteryGuiHelper.TitleForNode(featureKey);
                mFeaturedDesc.Text = MasteryGuiHelper.DescForNode(featureKey);

                if (nodeState == MasteryNode.NodeState.Inactive)
                {
                    mFeaturedPoints.Color = Color.Black;
                    mSpendButton.Alpha = mRefundButton.Alpha = 0.5f;
                }
                else
                {
                    mFeaturedPoints.Color = SPUtils.ColorFromColor(0x0c6e0a);
                    mSpendButton.Alpha = (mScene.MasteryManager.CurrentModel.PointsRemaining > 0 && mTreeData.MaySpendForKey(featureKey)) ? 1f : 0.5f;
                    mRefundButton.Alpha = (mTreeData.MayRefundForKey(featureKey)) ? 1f : 0.5f;
                }
            }
            else
            {
                mFeaturedPoints.Text = "";
                mFeaturedTitle.Text = "";
                mFeaturedDesc.Text = "";
            }

            int pointsRemaining = mScene.MasteryManager.CurrentModel.PointsRemaining;
            mPointsRemaining.Text = pointsRemaining.ToString();
            mPointsRemaining.Color = (pointsRemaining > 0) ? SPUtils.ColorFromColor(0x0c6e0a) : SPUtils.ColorFromColor(0xbd0404);

            if (mTreeView != null)
                mTreeView.RefreshNodes(featureKey);
        }

        public void SpendPoint(uint nodeKey)
        {
            if (mTreeData == null || mTreeData.SelectedNode == null)
                return;

            if (mScene.MasteryManager.CurrentModel.PointsRemaining > 0 && mTreeData.MaySpendForKey(nodeKey))
            {
                mTreeData.AddPoints(nodeKey, 1);
                mScene.PlaySound("Button");
            }
            else
            {
                mScene.PlaySound("Locked");
            }
        }

        public void RefundPoint(uint nodeKey)
        {
            if (mTreeData == null || mTreeData.SelectedNode == null)
                return;

            if (mTreeData.MayRefundForKey(nodeKey))
            {
                if (mTreeData.PointsForKey(nodeKey) > 0)
                {
                    mTreeData.AddPoints(nodeKey, -1);
                    mScene.PlaySound("Button");
                }
            }
            else
            {
                mScene.PlaySound("Locked");
            }
        }

        public void RefundAllPoints()
        {
            if (mTreeData == null)
                return;

            if (mTreeData.Points > 0)
            {
                mTreeData.ResetPoints();
                mScene.PlaySound("Button");
            }
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mTreeData == null)
                return;

            ControlsManager cm = ControlsManager.CM;
            bool didActivate = false, didNavigate = false;

            if (mTreeData.SelectedNode != null)
            {
                if (cm.DidButtonDepress(Buttons.A))
                {
                    mSpendButton.AutomatedButtonDepress();
                    SpendPoint(mTreeData.SelectedNode.Key);
                    didActivate = true;
                }
                else if (cm.DidButtonDepress(Buttons.X))
                {
                    mRefundButton.AutomatedButtonDepress();
                    RefundPoint(mTreeData.SelectedNode.Key);
                    didActivate = true;
                }
                else if (cm.DidButtonDepress(Buttons.Y))
                {
                    mResetButton.AutomatedButtonDepress();
                    RefundAllPoints();
                    didActivate = true;
                }
            }

            if (cm.DidButtonRelease(Buttons.A))
                mSpendButton.AutomatedButtonRelease(false);
            if (cm.DidButtonRelease(Buttons.X))
                mRefundButton.AutomatedButtonRelease(false);
            if (cm.DidButtonRelease(Buttons.Y))
                mResetButton.AutomatedButtonRelease(false);

            if (!didActivate)
            {
                if (cm.DidButtonDepress(Buttons.DPadUp) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLUp))
                {
                    mTreeData.NextRow(-1);
                    didNavigate = true;
                }
                else if (cm.DidButtonDepress(Buttons.DPadDown) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLDown))
                {
                    mTreeData.NextRow(1);
                    didNavigate = true;
                }
            }

            if (!didActivate && !didNavigate)
            {
                if (cm.DidButtonDepress(Buttons.DPadLeft) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLLeft))
                {
                    mTreeData.NextNode(-1);
                    didNavigate = true;
                }
                else if (cm.DidButtonDepress(Buttons.DPadRight) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLRight))
                {
                    mTreeData.NextNode(1);
                    didNavigate = true;
                }
            }

            if (didNavigate || didActivate)
                RefreshNodes();
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mFeaturedNodes != null)
                        {
                            foreach (KeyValuePair<uint, MasteryNodeView> kvp in mFeaturedNodes)
                                kvp.Value.Dispose();
                            mFeaturedNodes = null;
                        }

                        if (mTreeView != null)
                        {
                            mTreeView.Dispose();
                            mTreeView = null;
                        }

                        mTreeData = null;
                    }
                }
                catch (Exception)
                {
                    // Do nothing
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
