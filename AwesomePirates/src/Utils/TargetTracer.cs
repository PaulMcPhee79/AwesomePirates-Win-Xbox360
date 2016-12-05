using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Box2D;

namespace AwesomePirates
{
    class TargetTracer
    {
        private const int kTargetVelBufferSize = 30;

        public TargetTracer()
        {
            mTargetVelocityIter = 0;
            mTarget = null;
            mTargetVelocity = new Vector2[kTargetVelBufferSize];
        }

        private int mTargetVelocityIter;
	    private Vector2[] mTargetVelocity;
	    private ShipActor mTarget;

        public ShipActor Target
        {
            get { return mTarget; }
            set
            {
                if (value != mTarget)
                {
                    mTargetVelocityIter = 0;
                    mTarget = value;
                }
            }
        }
        public Vector2 TargetVel
        {
            get
            {
                int index = mTargetVelocityIter + 1;

                if (index >= kTargetVelBufferSize)
                    index -= kTargetVelBufferSize;
                return mTargetVelocity[index];
            }
        }

        public void AdvanceTime(double time)
        {
            if (mTarget != null && mTarget.B2Body != null)
            {
                mTargetVelocity[mTargetVelocityIter] = mTarget.B2Body.GetLinearVelocity();

                if (++mTargetVelocityIter == kTargetVelBufferSize)
                    mTargetVelocityIter = 0;
            }
        }
    }
}
