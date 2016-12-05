using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class CannonFire : PointMovie
    {
        public CannonFire(float x, float y)
            : base((int)PFCat.EXPLOSIONS, PointMovieType.CannonFire, x, y)
        {
            mAdvanceable = true;
            mVelX = mVelY = 0f;
            SetupMovie();
        }

        private float mVelX;
        private float mVelY;

        public override uint ReuseKey { get { return (uint)PointMovieType.CannonFire; } }
        public float CannonRotation { get { return Rotation; } set { Rotation = value; } }
        public static float Fps { get { return 10f; } }

        protected override void SetupMovie()
        {
            if (mMovie == null)
                mMovie = new SPMovieClip(mScene.TexturesStartingWith("cannon-smoke-small_"), CannonFire.Fps);
            mMovie.X = -mMovie.Width / 2;
            mMovie.Y = -mMovie.Height;
            mMovie.Loop = false;
            base.SetupMovie();
        }

        public override void Reuse()
        {
            if (InUse)
                return;
            base.Reuse();
            mVelX = mVelY = 0f;
        }

        public void SetLinearVelocity(float x, float y)
        {
            mVelX = x;
            mVelY = y;
        }

        public override void AdvanceTime(double time)
        {
            X += mVelX * (float)time;
            Y += mVelY * (float)time;

            base.AdvanceTime(time);
        }
    }
    
}
