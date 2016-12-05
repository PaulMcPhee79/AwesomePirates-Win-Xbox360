using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    internal class ResOffsetStack
    {
        public ResOffsetStack()
        {
            mOffsetStack = new List<ResOffset>();
        }

        private List<ResOffset> mOffsetStack;

        public ResOffset Offset
        {
            get
            {
                ResOffset offset = null;
                int count = Count;
	
	            if (count > 0)
		            offset = mOffsetStack[count-1];
	
	            return offset;
            }
        }
        public int Count { get { return mOffsetStack.Count; } }

        public ResOffset Push(ResOffset offset)
        {
            if (offset != null)
                mOffsetStack.Add(offset);
            return offset;
        }

        public void Pop()
        {
            int count = Count;

            if (count > 0)
                mOffsetStack.RemoveAt(count - 1);
        }
    }
}
