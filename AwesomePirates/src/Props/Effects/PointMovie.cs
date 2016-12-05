using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class PointMovie : Prop, IResourceClient, IReusable
    {
        public enum PointMovieType
        {
            Splash = 1,
            Explosion,
            CannonFire
        }

        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(3);

            // Splash
            int cacheSize = 20;
            uint reuseKey = (uint)PointMovieType.Splash;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = PointMovieWithType(PointMovieType.Splash, 0, 0);
                reusable.Hibernate();
                sCache.AddReusable(reusable);
            }

            // Explosion
            cacheSize = 20;
            reuseKey = (uint)PointMovieType.Explosion;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = PointMovieWithType(PointMovieType.Explosion, 0, 0);
                reusable.Hibernate();
                sCache.AddReusable(reusable);
            }

            // CannonFire
            cacheSize = 20;
            reuseKey = (uint)PointMovieType.CannonFire;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = PointMovieWithType(PointMovieType.CannonFire, 0, 0);
                reusable.Hibernate();
                sCache.AddReusable(reusable);
            }

            sCache.VerifyCacheIntegrity();
            sCaching = false;
        }

        private static IReusable CheckoutReusable(uint reuseKey)
        {
            IReusable reusable = null;

            if (sCache != null && !sCaching)
                reusable = sCache.Checkout(reuseKey);

            return reusable;
        }

        private static void CheckinReusable(IReusable reusable)
        {
            if (sCache != null && !sCaching)
                sCache.Checkin(reusable);
        }

        public static PointMovie PointMovieWithType(PointMovieType movieType, float x, float y)
        {
            PointMovie pointMovie = CheckoutReusable((uint)movieType) as PointMovie;

            if (pointMovie != null)
            {
                pointMovie.Reuse();
                pointMovie.X = x;
                pointMovie.Y = y;
            }
            else
            {
                switch (movieType)
                {
                    case PointMovieType.Splash: pointMovie = new Splash(x, y); break;
                    case PointMovieType.Explosion: pointMovie = new Explosion(x, y); break;
                    case PointMovieType.CannonFire: pointMovie = new CannonFire(x, y); break;
                }

#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed PointMovie ReusableCache for type: " + movieType.ToString());
#endif
            }

            return pointMovie;
        }

        public PointMovie(int category, PointMovieType movieType, float x, float y)
            : base(category)
        {
            mAdvanceable = true;
            mInUse = true;
            mPoolIndex = -1;
            mMovieCompleted = false;
            mMovieTimer = 0;
            mResourceKey = PointMovie.ResourceKeyForType(movieType);
		    mResources = null;
		    mMovie = null;
		    X = x;
		    Y = y;
            CheckoutPooledResources();
        }

        #region Fields
        protected bool mInUse;
        private int mPoolIndex;

        protected bool mMovieCompleted;
        protected double mMovieTimer;
        protected string mResourceKey;
	    protected SPMovieClip mMovie;
        protected ResourceServer mResources;
        #endregion

        #region Properties
        public virtual uint ReuseKey { get { return 0; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }
        public bool Loop { get { return mMovie.Loop; } set { mMovie.Loop = value; } }
        #endregion

        #region Methods
        private static string ResourceKeyForType(PointMovieType movieType)
        {
            string key = null;

            switch (movieType)
            {
                case PointMovieType.Splash: key = "Splash"; break;
                case PointMovieType.Explosion: key = "Explosion"; break;
                case PointMovieType.CannonFire: key = "CannonFire"; break;
            }
            return key;
        }

        protected virtual void SetupMovie()
        {
            if (mMovie == null)
                throw new InvalidOperationException("Cannot setup PointMovie with null movie.");
            mMovie.CurrentFrame = 0;
            mMovieTimer = mMovie.Duration;
            mMovie.Play();
            AddChild(mMovie);

            if (!sCaching)
            {
                mScene.AddProp(this);
                mScene.Juggler.AddObject(mMovie);
            }
        }

        public virtual void Reuse()
        {
            if (InUse)
                return;

            mMovieCompleted = false;
            mMovieTimer = 0;
            CheckoutPooledResources();
            SetupMovie();
            mInUse = true;
        }

        public virtual void Hibernate()
        {
            if (!InUse)
                return;

            if (mMovie != null)
            {
                mScene.Juggler.RemoveObject(mMovie);
                mMovie = null;
            }

            CheckinPooledResources();
            mInUse = false;

            CheckinReusable(this);
        }

        public override void AdvanceTime(double time)
        {
            if (mMovieCompleted)
                return;

            if (mMovieTimer > 0)
                mMovieTimer -= time;
            else if (mMovieTimer <= 0)
                MovieCompleted();
        }
        
        protected virtual void MovieCompleted()
        {
            if (!mMovieCompleted)
            {
                mMovieCompleted = true;
                mScene.Juggler.RemoveObject(mMovie);
                mScene.RemoveProp(this);
            }
        }

        public virtual void ResourceEventFiredWithKey(uint key, string type, object target) { }

        public override void CheckoutPooledResources()
        {
            if (mResources == null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_POINT_MOVIE);

                if (cache != null)
                    mResources = cache.CheckoutPoolResourcesForKey(mResourceKey);
            }

	        if (mResources == null)
		        Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED POINT MOVIE CACHE _+_++_+_+_+_+_+_+");
            else
            {
                mResources.Client = this;
        
                if (mMovie == null)
                    mMovie = mResources.DisplayObjectForKey(PointMovieCache.RESOURCE_KEY_PM_MOVIE) as SPMovieClip;
            }
        }

        public override void CheckinPooledResources()
        {
            if (mResources != null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_POINT_MOVIE);

                if (cache != null)
                    cache.CheckinPoolResources(mResources);
                mResources = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mMovie != null)
                        {
                            if (mResources != null)
                                mMovie.RemoveFromParent();
                            mScene.Juggler.RemoveObject(mMovie);
                            mMovie = null;
                        }

                        CheckinPooledResources();
                        mResourceKey = null;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion
    }
}
