using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class MiscCache : CacheManager
    {
        public const uint RESOURCE_KEY_WATERFIRE = 1;

        public override void FillResourcePoolForScene(SceneController scene)
        {
            if (mDictPool != null)
                return;
            mDictPool = new Dictionary<string, List<ResourceServer>>(5);
            mDictIndexers = new Dictionary<string, PoolIndexer>(5);

            List<string> keys = new List<string>()
            {
                "brandy-flame_",
                "sk-brandy-flame-p0_",
                "sk-brandy-flame-p1_",
                "sk-brandy-flame-p2_",
                "sk-brandy-flame-p3_"
            };

            List<List<SPTexture>> frames = new List<List<SPTexture>>()
            {
                scene.TexturesStartingWith("brandy-flame_"),
                scene.TexturesStartingWith("sk-brandy-flame-p0_"),
                scene.TexturesStartingWith("sk-brandy-flame-p1_"),
                scene.TexturesStartingWith("sk-brandy-flame-p2_"),
                scene.TexturesStartingWith("sk-brandy-flame-p3_")
            };

            List<int> poolCounts = new List<int>() { 21, 21, 21, 21, 21 };

            if (!(keys.Count == frames.Count && keys.Count == poolCounts.Count))
                throw new InvalidOperationException("Invalid MiscCache settings.");

            for (int i = 0; i < keys.Count; ++i)
            {
                int poolCount = poolCounts[i];
                List<ResourceServer> poolArray = new List<ResourceServer>(poolCount);
                PoolIndexer poolIndexer = new PoolIndexer(poolCount, "MiscCache");
                poolIndexer.InitIndexes(0, 1);
                string key = keys[i];
                List<SPTexture> clipFrames = frames[i];
                float fps = 12;

                ResourceServer resources = new ResourceServer(0, key);
                List<SPMovieClip> clips = new List<SPMovieClip>(poolCount);
                for (int j = 0; j < poolCount; ++j)
                {
                    SPMovieClip movie = new SPMovieClip(clipFrames, fps);
                    clips.Add(movie);
                }

                resources.AddMiscResource(clips, RESOURCE_KEY_WATERFIRE);
                poolArray.Add(resources);
                mDictPool.Add(key, poolArray);
                mDictIndexers.Add(key, poolIndexer);
            }
        }
    }
}
