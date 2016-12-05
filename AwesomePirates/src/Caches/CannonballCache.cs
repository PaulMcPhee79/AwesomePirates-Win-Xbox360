using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class CannonballCache : CacheManager
    {
        public const uint RESOURCE_KEY_CANNONBALL_CLIP = 1;
        public const uint RESOURCE_KEY_SHADOW_CLIP = 2;

        public void FillResourcePoolForSceneWithShotTypes(SceneController scene, List<string> shotTypes)
        {
            if (mDictPool != null)
                return;

            mDictPool = new Dictionary<string, List<ResourceServer>>(shotTypes.Count);
            mDictIndexers = new Dictionary<string, PoolIndexer>(shotTypes.Count);

            foreach (string shotType in shotTypes)
            {
                int qty = (shotType.Equals(Ash.TexturePrefixForKey(Ash.ASH_MOLTEN))) ? 80 : 50;
                List<ResourceServer> poolArray = new List<ResourceServer>(qty);
                PoolIndexer poolIndexer = new PoolIndexer(qty, "CannonballCache");
                poolIndexer.InitIndexes(0, 1);

                for (int i = 0; i < qty; ++i)
                {
                    ResourceServer resources = new ResourceServer(0, shotType);
                    List<SPTexture> frames = scene.TexturesStartingWith(shotType);

                    SPMovieClip ballClip = new SPMovieClip(frames, Cannonball.Fps);
			        ballClip.X = -ballClip.Width/2;
			        ballClip.Y = -ballClip.Height/2;
			        ballClip.Loop = true;
                    resources.AddMovie(ballClip, RESOURCE_KEY_CANNONBALL_CLIP);
			
			        SPMovieClip shadowClip = new SPMovieClip(frames, Cannonball.Fps);
			        shadowClip.X = -shadowClip.Width/2;
			        shadowClip.Loop = true;
                    resources.AddMovie(shadowClip, RESOURCE_KEY_SHADOW_CLIP);
            
                    poolArray.Add(resources);
                }

                mDictPool.Add(shotType, poolArray);
                mDictIndexers.Add(shotType, poolIndexer);
            }
        }
    }
}
