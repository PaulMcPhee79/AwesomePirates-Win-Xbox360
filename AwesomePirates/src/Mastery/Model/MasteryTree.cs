using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    sealed class MasteryTree : IDisposable
    {
        public MasteryTree(uint key)
        {
            mKey = key;
            mSelectedNode = null;
            mRows = new List<MasteryRow>();
        }

        #region Fields
        private bool mIsDisposed = false;
        private uint mKey;
        private MasteryNode mSelectedNode;
        private List<MasteryRow> mRows;
        #endregion

        #region Properties
        public uint Key { get { return mKey; } set { mKey = value; } }
        public uint[] NodeKeys
        {
            get
            {
                int keyCount = 0;

                if (RowCount != 0)
                {
                    foreach (MasteryRow row in mRows)
                        keyCount += row.NodeCount;
                }

                if (keyCount == 0)
                    return null;

                uint[] keys = new uint[keyCount];

                int i = 0;
                foreach (MasteryRow row in mRows)
                {
                    if (row.NodeCount == 0)
                        continue;

                    foreach (MasteryNode node in row.Nodes)
                        keys[i++] = node.Key;
                }

                return keys;
            }
        }
        public uint DefaultNodeKey { get { return (RowCount != 0) ? mRows[0].DefaultNodeKey : 0; } }
        public int RowCount { get { return (mRows != null) ? mRows.Count : 0; } }
        public MasteryNode SelectedNode { get { return mSelectedNode; } private set { mSelectedNode = value; } }
        public List<MasteryRow> Rows { get { return (mRows != null) ? new List<MasteryRow>(mRows) : null; } }
        public int Points
        {
            get
            {
                int points = 0;

                if (mRows != null)
                {
                    foreach (MasteryRow row in mRows)
                        points += row.Points;
                }

                return points;
            }
        }
        public int PointsCapacity
        {
            get
            {
                int capacity = 0;

                if (mRows != null)
                {
                    foreach (MasteryRow row in mRows)
                        capacity += row.PointsCapacity;
                }

                return capacity;
            }
        }
        #endregion

        #region Methods
        public MasteryTree Clone()
        {
            MasteryTree clone = MemberwiseClone() as MasteryTree;

            clone.mRows = new List<MasteryRow>(mRows.Count);
            foreach (MasteryRow row in mRows)
                clone.mRows.Add(row.Clone());

            int selectedRowIndex = -1, selectedNodeIndex = -1;

            if (mSelectedNode != null)
            {
                foreach (MasteryRow row in mRows)
                {
                    ++selectedRowIndex;
                    selectedNodeIndex = row.IndexForKey(mSelectedNode.Key);

                    if (selectedNodeIndex != -1)
                        break;
                }

                if (selectedRowIndex != -1 && selectedNodeIndex != -1)
                    clone.mSelectedNode = clone.mRows[selectedRowIndex].NodeForKey(mSelectedNode.Key);
            }

            return clone;
        }

        public void AddRow(MasteryRow row)
        {
            if (row != null && mRows != null && !mRows.Contains(row))
                mRows.Add(row);
        }

        public void RemoveRow(MasteryRow row)
        {
            if (row != null && mRows != null)
            {
                if (mSelectedNode != null && row == mSelectedNode.Row)
                    SelectedNode = null;
                mRows.Remove(row);
            }
        }

        public void RemoveRowAt(int index)
        {
            if (mRows != null && index >= 0 && index < mRows.Count)
                RemoveRow(mRows[index]);
        }

        public MasteryNode.NodeState NodeStateForKey(uint nodeKey)
        {
            MasteryNode.NodeState state = MasteryNode.NodeState.Inactive;
            MasteryNode node = NodeForKey(nodeKey);

            if (node != null && node.Row != null && node.Row.Nodes != null)
            {
                // Check if Active or Inactive
                int rowIndex = RowIndexForKey(nodeKey);

                if (rowIndex != -1)
                {
                    if (node.Row.Points > 0)
                        state = (node.Points == 0) ? MasteryNode.NodeState.Inactive : MasteryNode.NodeState.Active;
                    else
                    {
                        if (rowIndex == 0)
                            state = MasteryNode.NodeState.Active;
                        else
                        {
                            MasteryRow upperRow = mRows[rowIndex - 1];
                            state = MasteryNode.NodeState.Inactive;

                            if (upperRow.NodeCount > 0)
                            {
                                foreach (MasteryNode upperNode in upperRow.Nodes)
                                {
                                    if (upperNode.PointsMaxed)
                                    {
                                        state = MasteryNode.NodeState.Active;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Check if Active state is actually more advanced: Invested or Maxed
                if (state != MasteryNode.NodeState.Inactive)
                {
                    foreach (MasteryNode rowNode in node.Row.Nodes)
                    {
                        int points = rowNode.Points;
                        if (rowNode.PointsMaxed)
                            state = MasteryNode.NodeState.Maxed;
                        else if (rowNode.Points > 0)
                            state = MasteryNode.NodeState.Invested;
                        else 
                            continue;

                        break;
                    }
                }
            }

            return state;
        }

        public void BeginNavigation(uint nodeKey)
        {
            SelectedNode = NodeForKey(nodeKey);
        }

        public void NextNode(int dir)
        {
            if (mSelectedNode == null || mSelectedNode.Row == null)
                return;
            MasteryNode node = mSelectedNode.Row.NextNode(dir, mSelectedNode.Key);

            if (node != null)
                SelectedNode = node;
        }

        public void NextRow(int dir)
        {
            if (mRows == null || mSelectedNode == null || mSelectedNode.Row == null)
                return;

            MasteryRow row = null;
            int index = RowIndexForKey(mSelectedNode.Key);

            if (dir == -1 && index > 0)
                row = mRows[index - 1];
            else if (dir == 1 && index < mRows.Count - 1)
                row = mRows[index + 1];

            if (row != null && row.NodeCount > 0)
            {
                int currNodeCount = mSelectedNode.Row.NodeCount, nextNodeCount = row.NodeCount;

                if (nextNodeCount == 1)
                    SelectedNode = row.Nodes[0];
                else if (nextNodeCount == currNodeCount)
                    SelectedNode = row.Nodes[SelectedNode.Row.IndexForKey(SelectedNode.Key)];
                else if (nextNodeCount == 2)
                    SelectedNode = row.Nodes[Math.Max(0, Math.Min(SelectedNode.Row.IndexForKey(SelectedNode.Key)-1, row.NodeCount - 1))];
                else if (nextNodeCount == 3 && currNodeCount < 3)
                {
                    if (currNodeCount == 1)
                        SelectedNode = row.Nodes[1];
                    else if (currNodeCount == 2)
                        SelectedNode = row.Nodes[SelectedNode.Row.IndexForKey(SelectedNode.Key)];
                }
                else
                    SelectedNode = row.Nodes[Math.Min(SelectedNode.Row.IndexForKey(SelectedNode.Key), row.NodeCount - 1)];
            }
                
        }

        public int RowIndexForKey(uint nodeKey)
        {
            int index = -1;
            MasteryNode node = NodeForKey(nodeKey);

            if (node != null && node.Row != null)
                index = mRows.IndexOf(node.Row);

            return index;
        }

        public MasteryNode NodeForKey(uint nodeKey)
        {
            MasteryNode node = null;

            if (mRows != null)
            {
                foreach (MasteryRow row in mRows)
                {
                    node = row.NodeForKey(nodeKey);

                    if (node != null)
                        break;
                }
            }

            return node;
        }

        public int PointsForKey(uint nodeKey)
        {
            int points = 0;
            MasteryNode node = NodeForKey(nodeKey);

            if (node != null)
                points = node.Points;
            return points;
        }

        public int PointsCapacityForKey(uint nodeKey)
        {
            int pointsCapacity = 0;
            MasteryNode node = NodeForKey(nodeKey);

            if (node != null)
                pointsCapacity = node.PointsCapacity;
            return pointsCapacity;
        }

        public void AddPoints(uint nodeKey, int points)
        {
            MasteryNode node = NodeForKey(nodeKey);

            if (node != null)
                node.Points += points;
        }

        public void SetPoints(uint nodeKey, int points)
        {
            MasteryNode node = NodeForKey(nodeKey);

            if (node != null)
                node.Points = points;
        }

        public bool MaySpendForKey(uint nodeKey)
        {
            MasteryNode.NodeState nodeState = NodeStateForKey(nodeKey);
            MasteryNode node = NodeForKey(nodeKey);
            return (nodeState != MasteryNode.NodeState.Inactive && node != null && node.Points < node.PointsCapacity);
        }

        public bool MayRefundForKey(uint nodeKey)
        {
            MasteryNode.NodeState nodeState = NodeStateForKey(nodeKey);
            MasteryNode node = NodeForKey(nodeKey);
            int rowIndex = RowIndexForKey(nodeKey);
            return (nodeState != MasteryNode.NodeState.Inactive && node != null && node.Points > 0 && ((rowIndex == RowCount - 1) || Rows[rowIndex + 1].Points == 0));
        }

        public void ResetPoints()
        {
            if (mRows == null)
                return;

            foreach (MasteryRow row in mRows)
            {
                if (row.Nodes == null)
                    continue;

                foreach (MasteryNode node in row.Nodes)
                    node.Points = 0;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mRows != null)
                        {
                            foreach (MasteryRow row in mRows)
                                row.Dispose();
                            mRows = null;
                        }

                        mSelectedNode = null;
                    }
                }
                finally
                {
                    mIsDisposed = true;
                }
            }
        }

        ~MasteryTree()
        {
            Dispose(false);
        }
        #endregion
    }
}
