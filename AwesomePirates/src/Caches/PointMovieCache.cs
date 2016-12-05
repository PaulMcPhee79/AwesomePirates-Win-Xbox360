using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class PointMovieCache : CacheManager
    {
        public const uint RESOURCE_KEY_PM_MOVIE = 1;

        public override void FillResourcePoolForScene(SceneController scene)
        {
            if (mDictPool != null)
		        return;
	        mDictPool = new Dictionary<string,List<ResourceServer>>(3);
            mDictIndexers = new Dictionary<string, PoolIndexer>(3);
	
	        List<SPTexture> splashFrames = scene.TexturesStartingWith("splash_");
	        List<SPTexture> explodeFrames = scene.TexturesStartingWith("explode_");
	        List<SPTexture> smokeFrames = scene.TexturesStartingWith("cannon-smoke-small_");
	
	        List<string> keys = new List<string>() { "Splash", "Explosion", "CannonFire" };
            List<List<SPTexture>> frames = new List<List<SPTexture>>() { splashFrames, explodeFrames, smokeFrames };
            List<float> framesPerSec = new List<float>() { Splash.Fps, Explosion.Fps, CannonFire.Fps };
            List<int> poolCounts = new List<int>() { 40, 30, 30 };

            if (!(keys.Count == frames.Count && keys.Count == framesPerSec.Count))
                throw new InvalidOperationException("Invalid PointMovieCache settings.");

	        for (int i = 0; i < keys.Count; ++i)
            {
                int poolCount = poolCounts[i];
                List<ResourceServer> poolArray = new List<ResourceServer>(poolCount);
                PoolIndexer poolIndexer = new PoolIndexer(poolCount, "PointMovieCache");
                poolIndexer.InitIndexes(0, 1);
                string key = keys[i];
                List<SPTexture> clipFrames = frames[i];
                float fps = framesPerSec[i];

                for (int j = 0; j < poolCount; ++j)
                {
                    ResourceServer resources = new ResourceServer(0, key);
            
                    SPMovieClip movie = new SPMovieClip(clipFrames, fps);
                    //movie.AddEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)resources.OnMovieCompleted);

                    resources.AddMovie(movie, RESOURCE_KEY_PM_MOVIE);
                    poolArray.Add(resources);
		        }

                mDictPool.Add(key, poolArray);
                mDictIndexers.Add(key, poolIndexer);
	        }
        }
    }
}
