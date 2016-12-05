using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    sealed class MasteryNode : IDisposable
    {
        public enum NodeState
        {
            Inactive,
            Active,
            Invested,
            Maxed
        }

        public MasteryNode(uint key, int pointsCapacity = 1, int points = 0)
        {
            mKey = key;
            mPointsCapacity = Math.Max(0, pointsCapacity);
            mPoints = Math.Max(0, Math.Min(points, pointsCapacity));
            mRow = null;
        }

        #region Fields
        private bool mIsDisposed = false;
        private bool mDidChange = false;
        private uint mKey;
        private int mPoints;
        private int mPointsCapacity;
        private MasteryRow mRow;
        #endregion

        #region Properties
        public uint Key { get { return mKey; } set { mKey = value; } }
        public bool DidChange { get { return mDidChange; } set { mDidChange = value; } }
        public MasteryRow Row { get { return mRow; } set { mRow = value; } }
        public int Points { get { return mPoints; } set { mPoints = Math.Max(0, Math.Min(PointsCapacity, value)); DidChange = true; } }
        public int PointsCapacity { get { return mPointsCapacity; } }
        public bool PointsMaxed { get { return mPoints == mPointsCapacity; } }
        #endregion

        #region Methods
        public MasteryNode Clone()
        {
            MasteryNode clone = MemberwiseClone() as MasteryNode;
            clone.mRow = null;
            return clone;
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
                        mRow = null;
                    }
                }
                finally
                {
                    mIsDisposed = true;
                }
            }
        }

        ~MasteryNode()
        {
            Dispose(false);
        }
        #endregion
    }
}
