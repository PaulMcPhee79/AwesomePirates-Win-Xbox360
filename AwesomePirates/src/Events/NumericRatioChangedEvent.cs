using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    delegate void NumericRatioChangedEventHandler(NumericRatioChangedEvent ev);

    class NumericRatioChangedEvent : SPEvent
    {
        public NumericRatioChangedEvent(string type, float val, float min, float max, float delta, bool bubbles = false)
            : base(type, bubbles)
        {
            mValue = val;
            mMinValue = min;
            mMaxValue = max;
            mDelta = delta;
        }

        #region Fields
        private float mDelta;
        private float mValue;
        private float mMinValue;
        private float mMaxValue;
        #endregion

        #region Properties
        public float Value { get { return mValue; } }
        public float MinVal { get { return mMinValue; } }
        public float MaxVal { get { return mMaxValue; } }
        public float Delta { get { return mDelta; } }
        public float Ratio
        {
            get
            {
                float result = 1, range = mMaxValue - mMinValue;

                if (!SPMacros.SP_IS_FLOAT_EQUAL(range, 0))
                    result = (mValue - mMinValue) / range;
                return result;
            }
        }
        public float AbsRatio
        {
            get
            {
                float result = 1;

                if (!SPMacros.SP_IS_FLOAT_EQUAL(mMaxValue, 0))
                    result = mValue / mMaxValue;
                return result;
            }
        }
        #endregion

        #region Methods
        public void UpdateValues(float val, float min, float max, float delta)
        {
            mValue = val;
            mMinValue = min;
            mMaxValue = max;
            mDelta = delta;
        }
        #endregion
    }
}
