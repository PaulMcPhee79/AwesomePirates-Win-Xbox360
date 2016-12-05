using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    delegate void NumericValueChangedEventHandler(NumericValueChangedEvent ev);

    class NumericValueChangedEvent : SPEvent
    {
        public const string CUST_EVENT_TYPE_NUMERIC_VALUE_CHANGED = "numericValueChangedEvent";
        public const string CUST_EVENT_TYPE_COMBO_MULTIPLIER_CHANGED = "comboMultiplierChangedEvent";
        public const string CUST_EVENT_TYPE_ASH_PICKUP_LOOTED = "ashPickupLootedEvent";
        public const string CUST_EVENT_TYPE_SK_PUP_LOOTED = "skPupLootedEvent";
        public const string CUST_EVENT_TYPE_AI_KNOB_VALUE_CHANGED = "aiKnobValueChangedEvent";
        public const string CUST_EVENT_TYPE_AI_STATE_VALUE_CHANGED = "aiStateValueChangedEvent";
        public const string CUST_EVENT_TYPE_SPRITE_CAROUSEL_INDEX_CHANGED = "spriteCarouselIndexChangedEvent";

        public NumericValueChangedEvent(string type, byte val, byte oldVal, bool bubbles = false) : base(type, bubbles) { mByteValue = new byte[] { val, oldVal }; mUsageValidator = (1 << 0); }
        public NumericValueChangedEvent(string type, int val, int oldVal, bool bubbles = false) : base(type, bubbles) { mIntValue = new int[] { val, oldVal }; mUsageValidator = (1 << 1); }
        public NumericValueChangedEvent(string type, uint val, uint oldVal, bool bubbles = false) : base(type, bubbles) { mUintValue = new uint[] { val, oldVal }; mUsageValidator = (1 << 2); }
        public NumericValueChangedEvent(string type, long val, long oldVal, bool bubbles = false) : base(type, bubbles) { mLongValue = new long[] { val, oldVal }; mUsageValidator = (1 << 3); }
        public NumericValueChangedEvent(string type, float val, float oldVal, bool bubbles = false) : base(type, bubbles) { mFloatValue = new float[] { val, oldVal }; mUsageValidator = (1 << 4); }
        public NumericValueChangedEvent(string type, double val, double oldVal, bool bubbles = false) : base(type, bubbles) { mDoubleValue = new double[] { val, oldVal }; mUsageValidator = (1 << 5); }
        
        private int mUsageValidator;

        private byte[] mByteValue;
        private int[] mIntValue;
        private uint[] mUintValue;
        private long[] mLongValue;
        private float[] mFloatValue;
        private double[] mDoubleValue;

        #region Properties
        public byte ByteValue { get { return (((mUsageValidator & (1 << 0)) != 0) ? mByteValue[0] : NotifyBadUsage()); } }
        public byte OldByteValue { get { return (((mUsageValidator & (1 << 0)) != 0) ? mByteValue[1] : NotifyBadUsage()); } }

        public int IntValue { get { return (((mUsageValidator & (1 << 1)) != 0) ? mIntValue[0] : NotifyBadUsage()); } }
        public int OldIntValue { get { return (((mUsageValidator & (1 << 1)) != 0) ? mIntValue[1] : NotifyBadUsage()); } }

        public uint UintValue { get { return (((mUsageValidator & (1 << 2)) != 0) ? mUintValue[0] : NotifyBadUsage()); } }
        public uint OldUintValue { get { return (((mUsageValidator & (1 << 2)) != 0) ? mUintValue[1] : NotifyBadUsage()); } }

        public long LongValue { get { return (((mUsageValidator & (1 << 3)) != 0) ? mLongValue[0] : NotifyBadUsage()); } }
        public long OldLongValue { get { return (((mUsageValidator & (1 << 3)) != 0) ? mLongValue[1] : NotifyBadUsage()); } }

        public float FloatValue { get { return (((mUsageValidator & (1 << 4)) != 0) ? mFloatValue[0] : NotifyBadUsage()); } }
        public float OldFloatValue { get { return (((mUsageValidator & (1 << 4)) != 0) ? mFloatValue[1] : NotifyBadUsage()); } }
        
        public double DoubleValue { get { return (((mUsageValidator & (1 << 5)) != 0) ? mDoubleValue[0] : NotifyBadUsage()); } }
        public double OldDoubleValue { get { return (((mUsageValidator & (1 << 5)) != 0) ? mDoubleValue[1] : NotifyBadUsage()); } }

        private byte NotifyBadUsage()
        {
            throw new InvalidOperationException("NumericValueChangedEvent: attempt to access invalid property.");
        }
        #endregion

        #region Methods
        public void UpdateValues(byte val, byte oldVal)
        {
            if (((mUsageValidator & (1 << 0)) != 0))
            {
                mByteValue[0] = val;
                mByteValue[1] = oldVal;
            }
            else
                NotifyBadUsage();
        }

        public void UpdateValues(int val, int oldVal)
        {
            if (((mUsageValidator & (1 << 1)) != 0))
            {
                mIntValue[0] = val;
                mIntValue[1] = oldVal;
            }
            else
                NotifyBadUsage();
        }

        public void UpdateValues(uint val, uint oldVal)
        {
            if (((mUsageValidator & (1 << 2)) != 0))
            {
                mUintValue[0] = val;
                mUintValue[1] = oldVal;
            }
            else
                NotifyBadUsage();
        }

        public void UpdateValues(long val, long oldVal)
        {
            if (((mUsageValidator & (1 << 3)) != 0))
            {
                mLongValue[0] = val;
                mLongValue[1] = oldVal;
            }
            else
                NotifyBadUsage();
        }

        public void UpdateValues(float val, float oldVal)
        {
            if (((mUsageValidator & (1 << 4)) != 0))
            {
                mFloatValue[0] = val;
                mFloatValue[1] = oldVal;
            }
            else
                NotifyBadUsage();
        }

        public void UpdateValues(double val, double oldVal)
        {
            if (((mUsageValidator & (1 << 5)) != 0))
            {
                mDoubleValue[0] = val;
                mDoubleValue[1] = oldVal;
            }
            else
                NotifyBadUsage();
        }
        #endregion
    }
}
