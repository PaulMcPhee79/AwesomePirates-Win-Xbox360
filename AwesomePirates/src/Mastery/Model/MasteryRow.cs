using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    sealed class MasteryRow : IDisposable
    {
        public MasteryRow()
        {
            mNodes = new List<MasteryNode>();
            mNodesDict = new Dictionary<uint, MasteryNode>();
        }

        #region Fields
        private bool mIsDisposed = false;
        private List<MasteryNode> mNodes;
        private Dictionary<uint, MasteryNode> mNodesDict;
        #endregion

        #region Properties
        public int NodeCount { get { return (mNodes != null) ? mNodes.Count : 0; } }
        public uint DefaultNodeKey { get { return (NodeCount != 0) ? mNodes[0].Key : 0; } }
        public List<MasteryNode> Nodes { get { return (mNodes != null) ? new List<MasteryNode>(mNodes) : null; } }
        public Dictionary<uint, MasteryNode> NodesDict { get { return (mNodesDict != null) ? new Dictionary<uint, MasteryNode>(mNodesDict) : null; } }
        public int Points
        {
            get
            {
                int points = 0;

                if (mNodes != null)
                {
                    foreach (MasteryNode node in mNodes)
                        points += node.Points;
                }

                return points;
            }
        }
        public int PointsCapacity
        {
            get
            {
                int capacity = 0;

                if (mNodes != null)
                {
                    foreach (MasteryNode node in mNodes)
                        capacity += node.PointsCapacity;
                }

                return capacity;
            }
        }
        #endregion

        #region Methods
        public MasteryRow Clone()
        {
            MasteryRow clone = MemberwiseClone() as MasteryRow;

            clone.mNodes = new List<MasteryNode>(mNodes.Count);
            clone.mNodesDict = new Dictionary<uint, MasteryNode>(mNodesDict.Count);
            foreach (MasteryNode node in mNodes)
            {
                MasteryNode cloneNode = node.Clone();
                cloneNode.Row = clone;
                clone.mNodes.Add(cloneNode);
                clone.mNodesDict[cloneNode.Key] = cloneNode;
            }

            return clone;
        }

        public void AddNode(MasteryNode node)
        {
            if (node != null && mNodes != null && mNodesDict != null && !mNodesDict.ContainsKey(node.Key))
            {
                mNodes.Add(node);
                mNodesDict[node.Key] = node;

                if (node.Row != null)
                    node.Row.RemoveNode(node.Key);
                node.Row = this;
            }
        }

        public void RemoveNode(uint nodeKey)
        {
            if (mNodes != null && mNodesDict != null && mNodesDict.ContainsKey(nodeKey))
            {
                MasteryNode node = NodeForKey(nodeKey);
                node.Row = null;
                mNodes.Remove(node);
                mNodesDict.Remove(node.Key);
            }
        }

        public void RemoveNodeAt(int index)
        {
            if (mNodes != null && index >= 0 && index < mNodes.Count)
                RemoveNode(mNodes[index].Key);
        }

        public MasteryNode NextNode(int dir, uint originNodeKey)
        {
            if (mNodes == null)
                return null;

            MasteryNode movedTo = null;
            int index = IndexForKey(originNodeKey);

            if (index != -1)
            {
                if (dir == -1 && index > 0)
                    movedTo = mNodes[index - 1];
                else if (dir == 1 && index < mNodes.Count - 1)
                    movedTo = mNodes[index + 1];
            }

            return movedTo;
        }

        public int IndexForKey(uint nodeKey)
        {
            int index = -1;
            if (mNodesDict != null && mNodesDict.ContainsKey(nodeKey))
            {
                MasteryNode node = mNodesDict[nodeKey];
                index = mNodes.IndexOf(node);
            }

            return index;
        }

        public MasteryNode NodeForKey(uint nodeKey)
        {
            if (mNodesDict != null && mNodesDict.ContainsKey(nodeKey))
                return mNodesDict[nodeKey];
            else
                return null;
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
                        if (mNodes != null)
                        {
                            foreach (MasteryNode node in mNodes)
                                node.Dispose();
                            mNodes = null;
                        }

                        mNodesDict = null;
                    }
                }
                finally
                {
                    mIsDisposed = true;
                }
            }
        }

        ~MasteryRow()
        {
            Dispose(false);
        }
        #endregion
    }
}
