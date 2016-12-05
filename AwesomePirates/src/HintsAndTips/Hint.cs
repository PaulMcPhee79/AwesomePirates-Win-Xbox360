using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class Hint : Prop
    {
        public Hint(int category, Vector2 location, SPDisplayObject target = null)
            : base(category)
        {
            mAdvanceable = true;
            mTarget = target;

            if (target != null)
                mPrevPos = target.Origin;
            else
                mPrevPos = Vector2.Zero;
        }



        private Vector2 mPrevPos;
        private SPDisplayObject mTarget;

        public SPDisplayObject Target
        {
            get { return mTarget; }
            set
            {
                if (mTarget != value)
                {
                    mTarget = value;

                    if (mTarget != null)
                        mPrevPos = mTarget.Origin;
                }
            }
        }

        public override void AdvanceTime(double time)
        {
            if (mTarget == null)
                return;

            // Maintain our initial distance from the target
            float deltaX = mTarget.X - mPrevPos.X, deltaY = mTarget.Y - mPrevPos.Y;

            X += deltaX;
            Y += deltaY;
            mPrevPos = mTarget.Origin;
        }
    }
}
