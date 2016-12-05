using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class FloatTweener
    {
        public FloatTweener(float startValue, string transition, Action tweenComplete = null)
        {
            mFrom = mTo = mValue = startValue;
            mTotalDuration = 0.01;
            mTransitionFunc = (Func<float, float>)Delegate.CreateDelegate(typeof(Func<float, float>), typeof(SPTransitions), transition);
            TweenComplete = tweenComplete;
        }

        private float mFrom;
        private float mTo;
        private float mValue;
        private double mDuration;
        private double mTotalDuration;
        private Func<float, float> mTransitionFunc;

        public bool Delaying { get { return mDuration < 0; } }
        public float TweenedValue { get { return mValue; } }
        public Action TweenComplete { get; set; }

        public void Reset(float value)
        {
            mValue = mFrom = mTo = value;
            mDuration = 0;
        }

        public void Reset(float from, float to, double duration, double delay = 0)
        {
            mFrom = from;
            mTo = to;
            mDuration = -delay;
            mTotalDuration = Math.Max(0.01, duration);

            if (mDuration >= 0)
                mValue = mFrom;
        }

        public void AdvanceTime(double time)
        {
            if (mValue != mTo || mDuration < 0)
            {
                mDuration = Math.Min(mTotalDuration, mDuration + time);

                if (mDuration == mTotalDuration)
                {
                    mValue = mTo;

                    if (TweenComplete != null)
                        TweenComplete();
                }
                else if (mDuration >= 0)
                {
                    float ratio = (float)(mDuration / mTotalDuration);
                    mValue = mFrom + (mTo - mFrom) * ratio;
                }
            }
        }
    }
}
