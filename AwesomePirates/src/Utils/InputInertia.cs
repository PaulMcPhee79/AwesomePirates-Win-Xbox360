using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class InputInertia
    {
        public const double kDefaultInertia = 0.5;

        public InputInertia(double inertia = kDefaultInertia)
        {
            mMoveCounter = 0;
            mInertia = kDefaultInertia;
            mMoveMeter = 0;
        }

        private int mMoveCounter;
        private double mInertia;
        private double mMoveMeter;

        public double Inertia { get { return mInertia; } set { mInertia = value; } }
        public bool CanMove
        {
            get
            {
                if (mMoveMeter <= 0)
                {
                    ++mMoveCounter;
                    mMoveMeter += mInertia * Math.Max(0.25, 1.0 / mMoveCounter);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Reset()
        {
            mMoveCounter = 0;
            mMoveMeter = 0;
        }

        public void AdvanceTime(double time)
        {
            if (mMoveMeter > 0)
                mMoveMeter -= time;
        }
    }
}
