using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class MasteryTreeView : Prop
    {
        public MasteryTreeView(int category, float width, float height, MasteryTree data)
            : base(category)
        {
            if (data == null || data.RowCount == 0)
                throw new ArgumentException("MasteryTreeView's data cannot be null and must have at least one row of data.");
            mTreeWidth = Math.Max(1, width);
            mTreeHeight = Math.Max(1, height);
            mData = data;
            mNodeViews = new Dictionary<uint, MasteryNodeView>();
            mCostume = null;
            SetupProp();
        }

        #region Fields
        private float mTreeWidth;
        private float mTreeHeight;
        private SPSprite mCostume;
        private MasteryTree mData;
        private Dictionary<uint, MasteryNodeView> mNodeViews;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;

            mCostume = new SPSprite();
            AddChild(mCostume);

            // Debug layout quad
            //SPQuad quad = new SPQuad(mTreeWidth, mTreeHeight);
            //mCostume.AddChild(quad);

            int rowCount = mData.RowCount;

            if (rowCount == 0)
                return;

            int rowIter = 0, nodeCount = 0, nodeIter = 0;
            float rowHeight = mTreeHeight / rowCount;
            float nodeHeight = 0.9f * rowHeight;

            int maxNodesPerRow = 0;
            foreach (MasteryRow row in mData.Rows)
            {
                if (row.NodeCount > maxNodesPerRow)
                    maxNodesPerRow = row.NodeCount;
            }

            foreach (MasteryRow row in mData.Rows)
            {
                nodeIter = 0;
                nodeCount = row.NodeCount;

                if (nodeCount == 0)
                    continue;

                foreach (MasteryNode node in row.Nodes)
                {
                    MasteryNodeView nodeView = new MasteryNodeView(Category, node.Key);
                    nodeView.ScaleX = nodeView.ScaleY = nodeHeight / nodeView.IconHeight;

                    if (nodeCount < 3 || (nodeIter != 0 && nodeIter != nodeCount - 1))
                        nodeView.X = (nodeIter + 1) * mTreeWidth / (nodeCount + 1);
                    else if (nodeIter == 0)
                        nodeView.X = (nodeIter + 1) * mTreeWidth / (nodeCount + 1) - nodeView.IconWidth / 4;
                    else if (nodeIter == nodeCount - 1)
                        nodeView.X = (nodeIter + 1) * mTreeWidth / (nodeCount + 1) + nodeView.IconWidth / 4;

                    nodeView.Y = rowIter * rowHeight + nodeView.IconHeight / 2;

                    mNodeViews.Add(node.Key, nodeView);
                    mCostume.AddChild(nodeView);
                    ++nodeIter;
                }

                ++rowIter;
            }
        }

        public void RefreshNodes(uint highlightKey)
        {
            if (mData == null || mNodeViews == null)
                return;

            foreach (KeyValuePair<uint, MasteryNodeView> kvp in mNodeViews)
            {
                MasteryNode node = mData.NodeForKey(kvp.Key);

                if (node != null)
                {
                    kvp.Value.State = mData.NodeStateForKey(kvp.Key);
                    kvp.Value.EnableHighlight(node == mData.SelectedNode);
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
                        if (mNodeViews != null)
                        {
                            foreach (KeyValuePair<uint, MasteryNodeView> kvp in mNodeViews)
                                kvp.Value.Dispose();

                            mNodeViews = null;
                        }

                        mData = null;
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
