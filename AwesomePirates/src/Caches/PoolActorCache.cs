using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class PoolActorCache : CacheManager
    {
        public const uint RESOURCE_KEY_POOL_COSTUME = 1;
        public const uint RESOURCE_KEY_POOL_RIPPLES = 2;
        public const uint RESOURCE_KEY_POOL_SPAWN_TWEEN = 3;
        public const uint RESOURCE_KEY_POOL_DESPAWN_TWEEN = 4;
        public const uint RESOURCE_KEY_POOL_RIPPLE_TWEEN_SCALE = 5;
        public const uint RESOURCE_KEY_POOL_RIPPLE_TWEEN_ALPHA = 105; // Can't overlap with scale tween indexes (ripple count must be < 100)

        public override void FillResourcePoolForScene(SceneController scene)
        {
            if (mDictPool != null)
		        return;
	        mDictPool = new Dictionary<string,List<ResourceServer>>(6);
            mDictIndexers = new Dictionary<string, PoolIndexer>(6);
	
            float spawnDuration = PoolActor.SpawnDuration;
            float despawnDuration = PoolActor.DespawnDuration;
            float spawnedAlpha = PoolActor.SpawnedAlpha;
            float spawnedScale = PoolActor.SpawnedScale;
            int numRipples = PoolActor.NumPoolRipples;
    
	        // Acid
            int acidPoolSize = 50, magmaPoolSize = 30, skPoolSize = 40;
            mDictPool.Add(PoolActor.kPoolVisualStyleAcid, new List<ResourceServer>(acidPoolSize));
            mDictPool.Add(PoolActor.kPoolVisualStyleMagma, new List<ResourceServer>(magmaPoolSize));
            mDictPool.Add(PoolActor.kPoolVisualStylesSK[0], new List<ResourceServer>(skPoolSize));
            mDictPool.Add(PoolActor.kPoolVisualStylesSK[1], new List<ResourceServer>(skPoolSize));
            mDictPool.Add(PoolActor.kPoolVisualStylesSK[2], new List<ResourceServer>(skPoolSize));
            mDictPool.Add(PoolActor.kPoolVisualStylesSK[3], new List<ResourceServer>(skPoolSize));

            PoolIndexer poolIndexer = new PoolIndexer(acidPoolSize, "AcidPoolCache");
            poolIndexer.InitIndexes(0, 1);
            mDictIndexers.Add(PoolActor.kPoolVisualStyleAcid, poolIndexer);

            poolIndexer = new PoolIndexer(magmaPoolSize, "MagmaPoolCache");
            poolIndexer.InitIndexes(0, 1);
            mDictIndexers.Add(PoolActor.kPoolVisualStyleMagma, poolIndexer);

            for (int i = 0; i < PoolActor.kPoolVisualStylesSK.Length; ++i)
            {
                poolIndexer = new PoolIndexer(skPoolSize, "SKPoolCache" + i);
                poolIndexer.InitIndexes(0, 1);
                mDictIndexers.Add(PoolActor.kPoolVisualStylesSK[i], poolIndexer);
            }
    
            List<SPTexture> poolTextures = new List<SPTexture>()
            {
                scene.TextureByName("pool-of-acidR"),
                scene.TextureByName("pool-of-magmaR"),
                scene.TextureByName("sk-poolR-p0"),
                scene.TextureByName("sk-poolR-p1"),
                scene.TextureByName("sk-poolR-p2"),
                scene.TextureByName("sk-poolR-p3")
            };

            List<string> keys = new List<string>()
            {
                PoolActor.kPoolVisualStyleAcid,
                PoolActor.kPoolVisualStyleMagma,
                PoolActor.kPoolVisualStylesSK[0],
                PoolActor.kPoolVisualStylesSK[1],
                PoolActor.kPoolVisualStylesSK[2],
                PoolActor.kPoolVisualStylesSK[3]
            };

            Dictionary<string, int> counts = new Dictionary<string,int>()
            {
                { PoolActor.kPoolVisualStyleAcid, acidPoolSize },
                { PoolActor.kPoolVisualStyleMagma, magmaPoolSize },
                { PoolActor.kPoolVisualStylesSK[0], skPoolSize },
                { PoolActor.kPoolVisualStylesSK[1], skPoolSize },
                { PoolActor.kPoolVisualStylesSK[2], skPoolSize },
                { PoolActor.kPoolVisualStylesSK[3], skPoolSize }
            };
                
            int keyIndex = 0;
            SPTween tween = null;
    
            foreach (string key in keys)
            {
                SPTexture poolTexture = poolTextures[keyIndex];
                List<ResourceServer> poolArray = mDictPool[key] as List<ResourceServer>;
        
                int count = counts[key];
        
                for (int i = 0; i < count; ++i)
                {
                    ResourceServer resources = new ResourceServer(0, key);
                    List<SPSprite> ripples = new List<SPSprite>(numRipples);
                    float delay = 0;
            
                    // Ripples
                    for (int j = 0; j < numRipples; ++j)
                    {
                        SPImage image = new SPImage(poolTexture);
                        image.X = -image.Width / 2;
                        image.Y = -image.Height / 2;
                
                        SPSprite sprite = new SPSprite();
                        sprite.ScaleX = sprite.ScaleY = 0;
                        sprite.Alpha = 1;
                        sprite.AddChild(image);
                        ripples.Add(sprite);
                
                        tween = new SPTween(sprite, 0.8f * numRipples);
                        tween.AnimateProperty("ScaleX", 1.2f);
                        tween.AnimateProperty("ScaleY", 1.2f);
                        tween.Delay = delay;
                        tween.Loop = SPLoopType.Repeat;
                        resources.AddTween(tween, RESOURCE_KEY_POOL_RIPPLE_TWEEN_SCALE + (uint)j);
                    
                        tween = new SPTween(sprite, 0.8f * numRipples, SPTransitions.SPEaseInLinear);
                        tween.AnimateProperty("Alpha", 0f);
                        tween.Delay = delay;
                        tween.Loop = SPLoopType.Repeat;
                        resources.AddTween(tween, RESOURCE_KEY_POOL_RIPPLE_TWEEN_ALPHA + (uint)j);
                    
                        delay += (float)tween.TotalTime / numRipples;
                    }
            
                    resources.AddMiscResource(ripples, RESOURCE_KEY_POOL_RIPPLES);
            
                    // Costume
                    SPSprite costume = new SPSprite();
                    resources.AddDisplayObject(costume, RESOURCE_KEY_POOL_COSTUME);
            
                    tween = new SPTween(costume, spawnDuration);
                    tween.AnimateProperty("Alpha", spawnedAlpha);
                    tween.AnimateProperty("ScaleX", spawnedScale);
                    tween.AnimateProperty("ScaleY", spawnedScale);
                    tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                    //tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                    resources.AddTween(tween, RESOURCE_KEY_POOL_SPAWN_TWEEN);
            
                    tween = new SPTween(costume, despawnDuration);
                    tween.AnimateProperty("Alpha", 0.01f);
                    tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                    //tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                    resources.AddTween(tween, RESOURCE_KEY_POOL_DESPAWN_TWEEN);

                    poolArray.Add(resources);
                }
        
                ++keyIndex;
            }
        }
    }
}
