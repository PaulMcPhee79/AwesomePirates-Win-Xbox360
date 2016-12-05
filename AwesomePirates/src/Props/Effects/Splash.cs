using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class Splash : PointMovie
    {
        public Splash(float x, float y)
            : base((int)PFCat.POINT_MOVIES, PointMovieType.Splash, x, y)
        {
            SetupMovie();
        }

        public override uint ReuseKey { get { return (uint)PointMovieType.Splash; } }
        public static float Fps { get { return 12f; } }

        protected override void SetupMovie()
        {
            if (mMovie == null)
                mMovie = new SPMovieClip(mScene.TexturesStartingWith("splash_"), Splash.Fps);
            mMovie.X = -mMovie.Width / 2;
            mMovie.Y = -mMovie.Height / 2;
            mMovie.Loop = false;
            Alpha = 0.65f;
            base.SetupMovie();
        }
    }
}
