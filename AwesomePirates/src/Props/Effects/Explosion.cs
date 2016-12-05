using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class Explosion : PointMovie
    {
        public Explosion(float x, float y)
            : base((int)PFCat.EXPLOSIONS, PointMovieType.Explosion, x, y)
        {
            SetupMovie();
        }

        public override uint ReuseKey { get { return (uint)PointMovieType.Explosion; } }
        public static float Fps { get { return 12f; } }

        protected override void SetupMovie()
        {
            if (mMovie == null)
                mMovie = new SPMovieClip(mScene.TexturesStartingWith("explode_"), Explosion.Fps);
            mMovie.X = -mMovie.Width / 2;
            mMovie.Y = -mMovie.Height / 2;
            mMovie.Loop = false;
            base.SetupMovie();
        }
    }
}
