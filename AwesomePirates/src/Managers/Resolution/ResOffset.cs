using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    public class ResOffset
    {
        private static bool s_isCustRes = false;

        public ResOffset(float x, float y, float custX, float custY)
        {
            mIsCustomRes = s_isCustRes;
            mX = x;
            mY = y;
            mCustX = custX;
            mCustY = custY;
        }

        public ResOffset(float x, float y) : this(x, y, x, y) { }
        
        #region Fields
        private bool mIsCustomRes;
        private float mX;
        private float mY;
        private float mCustX;
        private float mCustY;
        #endregion

        #region Properties
        public float X { get { return ((mIsCustomRes) ? mCustX : mX); } }
        public float Y { get { return ((mIsCustomRes) ? mCustY : mY); } }
        public static bool IsCustRes { set { s_isCustRes = value; } }
        #endregion
    }
}
