using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class RingBuffer
    {
        public RingBuffer(int capacity)
        {
            mNext = 0;
		    mCapacity = capacity;
            mBuffer = new List<SPDisplayObject>(capacity);
        }

        private int mCapacity;
        private int mNext;
        private List<SPDisplayObject> mBuffer;

        public int Capacity { get { return mCapacity; } }
        public bool AtEnd { get { return mNext == mCapacity; } }
        public SPDisplayObject NextItem
        {
            get
            {
                if (mBuffer.Count > 0)
                {
		            if (mNext == mCapacity)
			            mNext = 0;
		            return mBuffer[mNext++];
	            }
                else
                {
		            return null;
	            }
            }
        }
        public int IndexOfNextItem { get { return ((mNext == mCapacity) ? 0 : mNext); } }
        public List<SPDisplayObject> AllItems { get { return mBuffer; } }
        public int Count { get { return mBuffer.Count; } }

        public object AddItem(SPDisplayObject item)
        {
            mBuffer.Add(item);
            return item;
        }

        public void AddItems<T>(List<T> items) where T : SPDisplayObject
        {
            if (items != null && items.Count > 0)
                mBuffer.AddRange(items.Cast<SPDisplayObject>());
        }

        public void ResetIterator()
        {
            mNext = 0;
        }

        public void Clear()
        {
            mBuffer.Clear();
            ResetIterator();
        }
    }
}
