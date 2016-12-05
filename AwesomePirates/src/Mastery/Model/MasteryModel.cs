using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SparrowXNA;

namespace AwesomePirates
{
    sealed class MasteryModel : SPEventDispatcher, IDisposable
    {
        public const int kMaxMasteryLevel = 7;

        private const string kDataVersion = "Version_1.0";
        private const int kXPDivider = 10;

        public MasteryModel()
        {
            mLevelXP = 0;
            mMasteryLevel = 0;
            mMasteryBitmap = 0;
            mSelectedTree = null;
            mTrees = new List<MasteryTree>();
        }

        #region Fields
        private bool mIsDisposed = false;
        protected readonly object s_lock = new object();
        private int mLevelXP;
        private int mMasteryLevel;
        private uint mMasteryBitmap;
        private MasteryTree mSelectedTree;
        private List<MasteryTree> mTrees;
        #endregion

        #region Properties
        public bool ShouldLevelUp { get { return LevelXP >= LevelXPRequired && MasteryLevel < kMaxMasteryLevel; } }
        public bool SaveRequired
        {
            get
            {
                if (mTrees == null)
                    return false;

                bool didChange = false;
                foreach (MasteryTree tree in mTrees)
                {
                    if (tree.RowCount == 0)
                        continue;

                    foreach (MasteryRow row in tree.Rows)
                    {
                        if (row.NodeCount == 0)
                            continue;

                        foreach (MasteryNode node in row.Nodes)
                        {
                            if (node.DidChange)
                            {
                                didChange = true;
                                break;
                            }
                        }
                    }
                }

                return didChange;
            }

            private set
            {
                if (!value && mTrees != null)
                {
                    foreach (MasteryTree tree in mTrees)
                    {
                        if (tree.RowCount == 0)
                            continue;

                        foreach (MasteryRow row in tree.Rows)
                        {
                            if (row.NodeCount == 0)
                                continue;

                            foreach (MasteryNode node in row.Nodes)
                                node.DidChange = false;
                        }
                    }
                }
            }
        }
        public int PointsSpent
        {
            get
            {
                if (mTrees == null)
                    return 0;

                int points = 0;

                foreach (MasteryTree tree in mTrees)
                {
                    uint[] nodeKeys = tree.NodeKeys;

                    if (nodeKeys == null)
                        continue;

                    foreach (uint key in nodeKeys)
                        points += tree.PointsForKey(key);
                }

                return points;
            }
        }
        public int PointsTotal { get { return Math.Min(kMaxMasteryLevel, MasteryLevel); } }
        public int PointsRemaining { get { return PointsTotal - PointsSpent; } }
        public int LevelXP { get { return mLevelXP; } }
        public int LevelXPRequired { get { return XPRequiredForLevel(mMasteryLevel+1); } }
        public int MasteryLevel { get { return mMasteryLevel; } }
        public float LevelXPFraction
        {
            get
            {
                int xpRequired = LevelXPRequired;

                if (xpRequired == 0)
                    return 0f;
                else
                    return LevelXP / (float)xpRequired;
            }
        }
        public uint MasteryBitmap { get { return mMasteryBitmap; } }
        public int TreeCount { get { return (mTrees != null) ? mTrees.Count : 0; } }
        public uint[] TreeKeys
        {
            get
            {
                if (TreeCount == 0)
                    return null;

                uint[] keys = new uint[mTrees.Count];

                int i = 0;
                foreach (MasteryTree tree in mTrees)
                    keys[i++] = tree.Key;
                return keys;
            }
        }
        private MasteryTree SelectedTree { get { return mSelectedTree; } set { mSelectedTree = value; } }
        #endregion

        #region Methods
        public static int XPRequiredForLevel(int level)
        {
            int xpRequired = 0;

            switch (level)
            {
                case 1: xpRequired = 100000; break;
                case 2: xpRequired = 250000; break;
                case 3: xpRequired = 750000; break;
                case 4: xpRequired = 1500000; break;
                case 5: xpRequired = 2500000; break;
                case 6: xpRequired = 3500000; break;
                case 7: xpRequired = 5000000; break;
                default: break;
            }

            return xpRequired;
        }

        public void AddTree(MasteryTree tree)
        {
            if (tree != null && mTrees != null && !mTrees.Contains(tree))
                mTrees.Add(tree);
        }

        public void RemoveTree(uint treeKey)
        {
            MasteryTree tree = TreeForKey(treeKey);

            if (tree != null)
            {
                if (tree == mSelectedTree)
                    SelectedTree = null;
                mTrees.Remove(tree);
            }
        }

        public void Clear()
        {
            if (mTrees == null)
                return;

            foreach (MasteryTree tree in mTrees)
                tree.Dispose();
            mTrees.Clear();
        }

        public void SelectTreeForKey(uint treeKey)
        {
            MasteryTree tree = TreeForKey(treeKey);

            if (tree != null)
                SelectedTree = tree;
        }

        public MasteryTree TreeForKey(uint treeKey)
        {
            MasteryTree tree = null;

            if (mTrees != null)
            {
                foreach (MasteryTree masteryTree in mTrees)
                {
                    if (masteryTree.Key == treeKey)
                    {
                        tree = masteryTree;
                        break;
                    }
                }
            }

            return tree;
        }

        public void AddXP(int score)
        {
            if (MasteryLevel < kMaxMasteryLevel)
            {
                if (!GameController.GC.IsTrialMode || mMasteryLevel < 2)
                    mLevelXP += score / kXPDivider;
            }
        }

        public void AttemptLevelUp()
        {
            while (ShouldLevelUp)
            {
                mLevelXP -= LevelXPRequired;
                ++mMasteryLevel;

                if (mMasteryLevel == kMaxMasteryLevel || (GameController.GC.IsTrialMode && mMasteryLevel >= 2))
                    mLevelXP = 0;
            }
        }

        public void RefreshMasteryBitmap()
        {
            mMasteryBitmap = 0;

            if (mTrees == null)
                return;

            List<MasteryNode> nodes = new List<MasteryNode>();

            foreach (MasteryTree tree in mTrees)
            {
                if (tree.Rows == null)
                    continue;

                foreach (MasteryRow row in tree.Rows)
                    nodes.AddRange(row.Nodes);
            }

            foreach (MasteryNode node in nodes)
            {
                if (node.PointsMaxed)
                    mMasteryBitmap |= node.Key;
            }
        }

        public int PointsForTree(uint treeKey)
        {
            int points = 0;
            MasteryTree tree = TreeForKey(treeKey);

            if (tree != null)
                points = tree.Points;

            return points;
        }

        public int PointsCapacityForTree(uint treeKey)
        {
            int capacity = 0;
            MasteryTree tree = TreeForKey(treeKey);

            if (tree != null)
                capacity = tree.PointsCapacity;

            return capacity;
        }

        // Pass throughs
        public MasteryNode.NodeState NodeStateForKey(uint nodeKey)
        {
            MasteryNode.NodeState state = MasteryNode.NodeState.Inactive;
            MasteryNode.NodeState treeState = (mSelectedTree != null) ? mSelectedTree.NodeStateForKey(nodeKey) : MasteryNode.NodeState.Inactive;

            // Show Active nodes as Inactive if player is out of points to spend.
            if (PointsRemaining > 0 || treeState == MasteryNode.NodeState.Invested || treeState == MasteryNode.NodeState.Maxed)
                state = treeState;

            return state;
        }

        public void BeginNavigation(uint nodeKey = 0)
        {
            if (mSelectedTree != null)
                mSelectedTree.BeginNavigation((nodeKey == 0) ? mSelectedTree.DefaultNodeKey : nodeKey);
        }

        public void NextNode(int dir)
        {
            if (mSelectedTree != null)
                mSelectedTree.NextNode(dir);
        }

        public void NextRow(int dir)
        {
            if (mSelectedTree != null)
                mSelectedTree.NextRow(dir);
        }

        public int PointsForKey(uint nodeKey)
        {
            int points = -1;

            if (mSelectedTree != null)
                points = mSelectedTree.PointsForKey(nodeKey);
            return points;
        }

        public int PointsCapacityForKey(uint nodeKey)
        {
            int pointsCapacity = -1;

            if (mSelectedTree != null)
                pointsCapacity = mSelectedTree.PointsCapacityForKey(nodeKey);
            return pointsCapacity;
        }

        public void AddPoints(uint nodeKey, int points)
        {
            if (mSelectedTree != null)
                mSelectedTree.AddPoints(nodeKey, points);
        }

        public void SetPoints(uint nodeKey, int points)
        {
            if (mSelectedTree != null)
                mSelectedTree.SetPoints(nodeKey, points);
        }

        public MasteryModel Clone(bool willSave = true)
        {
            MasteryModel clone = MemberwiseClone() as MasteryModel;

            clone.mTrees = new List<MasteryTree>(mTrees.Count);
            foreach (MasteryTree tree in mTrees)
                clone.mTrees.Add(tree.Clone());

            if (mSelectedTree != null && mTrees.Contains(mSelectedTree))
            {
                int selectedIndex = mTrees.IndexOf(mSelectedTree);

                if (selectedIndex < clone.mTrees.Count)
                    clone.mSelectedTree = clone.mTrees[selectedIndex];
            }

            if (willSave && SaveRequired)
                SaveRequired = false;

            return clone;
        }

        public void DecodeWithReader(BinaryReader reader)
        {
            Clear();

            // Decrypt buffer
            int count = reader.ReadInt32();

            if (count > 50000)
                throw new Exception("Masteries data length is invalid. Loading aborted.");

            byte[] buffer = new byte[count];
            int bufferLen = reader.Read(buffer, 0, count);

            if (bufferLen != count)
                throw new Exception("Masteries could not be loaded due to file length inaccuracies.");
            FileManager.MaskUnmaskBuffer(0x12D, buffer, bufferLen);

            BinaryReader br = new BinaryReader(new MemoryStream(buffer));

            // Read Saved Data
            string dataVersion = br.ReadString();
            mLevelXP = br.ReadInt32();
            mMasteryLevel = br.ReadInt32();

            int treeCount = br.ReadInt32();

            if (mTrees == null)
                mTrees = new List<MasteryTree>();

            // Read in model data
            for (int treeIndex = 0; treeIndex < treeCount; ++treeIndex)
            {
                MasteryTree tree = new MasteryTree(br.ReadUInt32());
                int rowCount = br.ReadInt32();

                for (int rowIndex = 0; rowIndex < rowCount; ++rowIndex)
                {
                    MasteryRow row = new MasteryRow();
                    int nodeCount = br.ReadInt32();
                    for (int nodeIndex = 0; nodeIndex < nodeCount; ++nodeIndex)
                    {
                        MasteryNode node = new MasteryNode(br.ReadUInt32(), br.ReadInt32(), br.ReadInt32());
                        row.AddNode(node);
                    }

                    tree.AddRow(row);
                }

                AddTree(tree);
            }

            // Very basic and incomplete data integrity enforcement
            if (mTrees != null && PointsSpent > PointsTotal)
            {
                foreach (MasteryTree tree in mTrees)
                    tree.ResetPoints();
            }

            buffer = null;
            br = null;
            SaveRequired = false;
        }

        public void EncodeWithWriter(BinaryWriter writer)
        {
            if (mTrees == null || TreeCount == 0)
                return;

            BinaryWriter bw = new BinaryWriter(new MemoryStream(2500));
            bw.Write(kDataVersion);
            bw.Write(LevelXP);
            bw.Write(MasteryLevel);
            bw.Write(TreeCount);

            foreach (MasteryTree tree in mTrees)
            {
                if (tree.RowCount == 0)
                    continue;

                bw.Write(tree.Key);
                bw.Write(tree.RowCount);

                foreach (MasteryRow row in tree.Rows)
                {
                    if (row.NodeCount == 0)
                        continue;

                    bw.Write(row.NodeCount);

                    foreach (MasteryNode node in row.Nodes)
                    {
                        bw.Write(node.Key);
                        bw.Write(node.PointsCapacity);
                        bw.Write(node.Points);
                    }
                }
            }

            // Perform basic encryption on buffer
            Stream stream = bw.BaseStream;
            stream.Position = 0;

            byte[] buffer = new byte[(int)stream.Length];
            int bufferLen = stream.Read(buffer, 0, (int)stream.Length);
            FileManager.MaskUnmaskBuffer(0x12D, buffer, bufferLen);

            // Write encrypted buffer back to stream
            writer.Write(bufferLen);
            writer.Write(buffer, 0, bufferLen);

            buffer = null;
            bw = null;
            //SaveRequired = false; // Don't call from potentially asynchronous method (also, it is usually operating on a clone).
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
                        if (mTrees != null)
                        {
                            foreach (MasteryTree tree in mTrees)
                                tree.Dispose();
                            mTrees = null;
                        }

                        mSelectedTree = null;
                    }
                }
                finally
                {
                    mIsDisposed = true;
                }
            }
        }

        ~MasteryModel()
        {
            Dispose(false);
        }
        #endregion
    }
}
