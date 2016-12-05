using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    public class PoolIndexer
    {
        public PoolIndexer(int capacity, string tag = null)
        {
#if DEBUG
            if (capacity <= 0)
                throw new ArgumentException("PoolIndexer must have capacity > 0.");
#endif
            mCapacity = capacity;
            mIndices = new int[capacity];
            mIndicesIndex = 0;
            mTag = tag;
        }

        private int mCapacity;
        private int mIndicesIndex;
        private int[] mIndices;
        private string mTag;

        public int Capacity { get { return mCapacity; } }
        public string Tag { get { return mTag; } }

        public int CheckoutNextIndex()
        {
            if (mIndicesIndex < mCapacity)
                return mIndices[mIndicesIndex++];
            else
                return -1;
        }

        public void CheckinIndex(int index)
        {
            if (index >= 0 && mIndicesIndex > 0 && mIndicesIndex <= mCapacity)
            {
                --mIndicesIndex;
                mIndices[mIndicesIndex] = index;
            }
#if DEBUG
            else
                throw new InvalidOperationException(string.Format("Bad PoolIndexer checkin. Tag: {0}", mTag));
#endif
        }

        public void InitIndexes(int startIndex, int increment)
        {
#if DEBUG
            if (startIndex < 0 || (startIndex + (increment * mCapacity) < 0))
                throw new InvalidOperationException(string.Format("Bad PoolIndexer initialization. Tag: {0}", mTag));
#endif
            for (int i = 0; i < mCapacity; ++i)
                mIndices[i] = startIndex + i * increment;
        }

        public void InsertPoolIndex(int index, int poolIndex)
        {
            if (index >= 0 && index < mCapacity)
                mIndices[index] = poolIndex;
        }
    }
}
