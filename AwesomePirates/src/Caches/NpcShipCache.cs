using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class NpcShipCache : CacheManager
    {
        public const uint RESOURCE_KEY_NPC_GENERICS = 1;
        public const uint RESOURCE_KEY_NPC_SINKING = 2;
        public const uint RESOURCE_KEY_NPC_BURNING = 3;
        public const uint RESOURCE_KEY_NPC_COSTUME = 4;
        public const uint RESOURCE_KEY_NPC_WARDROBE = 5;
        public const uint RESOURCE_KEY_NPC_DOCK_TWEEN = 6;
        public const uint RESOURCE_KEY_NPC_BURN_IN_TWEEN = 7;
        public const uint RESOURCE_KEY_NPC_BURN_OUT_TWEEN = 8;
        public const uint RESOURCE_KEY_NPC_SHRINK_TWEEN = 9;

        private const float kDefaultNpcShipClipFps = 8f;

        public NpcShipCache()
        {
            mNpcShipArrayPool = null;
        }

        private List<object> mNpcShipArrayPool;
        private PoolIndexer mGenericIndexer;
        private Dictionary<string, PoolIndexer> mCustomIndexers;

        public override void FillResourcePoolForScene(SceneController scene)
        {
            if (mNpcShipArrayPool != null)
		        return;
	
	        mNpcShipArrayPool = new List<object>(2); // generic + custom
    
            // Generic (these are common to all ships, so we need to store a lot of them)
            int poolCount = 40;
	        List<Dictionary<string, SPMovieClip>> genericArray = new List<Dictionary<string,SPMovieClip>>(poolCount);
            mNpcShipArrayPool.Add(genericArray);

            mGenericIndexer = new PoolIndexer(poolCount, "NpcShipGenericCache");
            mGenericIndexer.InitIndexes(0, 1);
	
	        List<SPTexture> sinkingTextures = scene.TexturesStartingWith("ship-sinking_");
            List<SPTexture> burningTextures = scene.TexturesStartingWith("ship-burn_");

            SPMovieClip sinkingClip = null, burningClip = null;
    
	        // MovieClip Pools
	        for (int i = 0; i < poolCount; ++i)
            {
		        sinkingClip = new SPMovieClip(sinkingTextures, kDefaultNpcShipClipFps);
                burningClip = new SPMovieClip(burningTextures, kDefaultNpcShipClipFps);
                genericArray.Add(new Dictionary<string,SPMovieClip>() { { "Sinking", sinkingClip } , { "Burning", burningClip } });
	        }
    
            // Custom
	        Dictionary<string, object> npcShipDetailDict = ShipFactory.Factory.AllNpcShipDetails;
            poolCount = npcShipDetailDict.Count;
            Dictionary<string, List<ResourceServer>> customDictionary = new Dictionary<string, List<ResourceServer>>(poolCount);
            mNpcShipArrayPool.Add(customDictionary);

            mCustomIndexers = new Dictionary<string, PoolIndexer>(poolCount);
	
            foreach (KeyValuePair<string, object> kvp in npcShipDetailDict)
            {
		        Dictionary<string, object> details = npcShipDetailDict[kvp.Key] as Dictionary<string, object>;
                ShipDetails npcShipDetails = ShipFactory.Factory.CreateNpcShipDetailsForType(kvp.Key);
                string textureName = details["textureName"] as string;
                List<SPTexture> costumeTextures = scene.TexturesStartingWith(textureName);
        
                int cacheSize = Convert.ToInt32(details["cacheSize"]);
        
                if (cacheSize == 0)
                    continue;
        
		        List<ResourceServer> customArray = new List<ResourceServer>(cacheSize);
                PoolIndexer poolIndexer = new PoolIndexer(cacheSize, kvp.Key + "Cache");
                poolIndexer.InitIndexes(0, 1);
		
		        // Custom caches
		        for (int i = 0; i < cacheSize; ++i)
                {
                    ResourceServer resources = new ResourceServer(0, kvp.Key);
                    resources.SetPoolIndexCapacity(2);
			        int costumeIndex = ShipDetails.NUM_NPC_COSTUME_IMAGES / 2;
			        List<SPImage> images = new List<SPImage>(ShipDetails.NUM_NPC_COSTUME_IMAGES);
			
                    // Costume
			        for (int j = 0, frameIndex = costumeIndex, frameIncrement = -1; j < ShipDetails.NUM_NPC_COSTUME_IMAGES; ++j)
                    {
				        SPImage image = new SPImage(costumeTextures[frameIndex]);
				        image.ScaleX = (j < costumeIndex) ? -1 : 1;
                        image.X = -24 * image.ScaleX; // -18 * image.ScaleX;
				        image.Y = -2 * npcShipDetails.RudderOffset;
				        image.Visible = (j == costumeIndex);
                        images.Add(image);
				
				        if (frameIndex == 0)
					        frameIncrement = 1;
				        frameIndex += frameIncrement;
			        }

                    resources.AddMiscResource(images, RESOURCE_KEY_NPC_COSTUME);
            
                    // Wardrobe
                    SPSprite wardrobe = new SPSprite();
                    resources.AddDisplayObject(wardrobe, RESOURCE_KEY_NPC_WARDROBE);
            
                    // Tweens
                    SPTween tween = new SPTween(wardrobe, 0.5f);
                    tween.AnimateProperty("Alpha", 0f);
                    tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                    //tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                    resources.AddTween(tween, RESOURCE_KEY_NPC_DOCK_TWEEN);
            
                    tween = new SPTween(wardrobe, burningClip.Duration / 2, SPTransitions.SPEaseOut);
                    tween.AnimateProperty("Alpha", 1f);
                    tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                    //tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                    resources.AddTween(tween, RESOURCE_KEY_NPC_BURN_IN_TWEEN);
            
                    tween = new SPTween(wardrobe, burningClip.Duration / 2);
                    tween.AnimateProperty("Alpha", 0f);
                    resources.AddTween(tween, RESOURCE_KEY_NPC_BURN_OUT_TWEEN);
            
                    tween = new SPTween(wardrobe, 1f);
                    tween.AnimateProperty("ScaleX", 0.01f);
                    tween.AnimateProperty("ScaleY", 0.01f);
                    resources.AddTween(tween, RESOURCE_KEY_NPC_SHRINK_TWEEN);
            
                    customArray.Add(resources);
		        }
		
                customDictionary.Add(kvp.Key, customArray);
                mCustomIndexers.Add(kvp.Key, poolIndexer);
		
		        // For 88MPH achievement mode - just cache the texture with TextureManager
                if (details.ContainsKey("textureFutureName"))
                {
                    textureName = details["textureFutureName"] as string;

                    if (textureName != null)
                        scene.TexturesStartingWith(textureName);
                }
	        }
        }

        public override void DrainResourcePool()
        {
            base.DrainResourcePool();

            if (mNpcShipArrayPool != null)
            {
                mNpcShipArrayPool.Clear();
                mNpcShipArrayPool = null;
                mGenericIndexer = null;
                mCustomIndexers = null;
            }
        }

        public override ResourceServer CheckoutPoolResourcesForKey(string key)
        {
            if (mNpcShipArrayPool == null || key == null)
		        return null;

	        ResourceServer resources = null;
            List<Dictionary<string, SPMovieClip>> genericArray = mNpcShipArrayPool[0] as List<Dictionary<string, SPMovieClip>>;
            Dictionary<string, List<ResourceServer>> customDict = mNpcShipArrayPool[1] as Dictionary<string, List<ResourceServer>>;
            List<ResourceServer> customArray = customDict[key];
	
	        if (customArray != null && genericArray != null)
            {
                // Custom
                PoolIndexer customIndexer = mCustomIndexers[key];
                int customIndex = customIndexer.CheckoutNextIndex();
                if (customIndex != -1)
                {
                    resources = customArray[customIndex];
                    resources.PoolIndex = customIndex;

                    List<SPImage> costumeImages = resources.MiscResourceForKey(RESOURCE_KEY_NPC_COSTUME) as List<SPImage>;
                    foreach (SPImage image in costumeImages)
                        image.Visible = false;

                    // Generic
                    PoolIndexer genericIndexer = mGenericIndexer;
                    int genericIndex = genericIndexer.CheckoutNextIndex();
                    if (genericIndex != -1)
                    {
                        resources.SetPoolIndex(1, genericIndex);

                        Dictionary<string, SPMovieClip> genericMovies = genericArray[genericIndex];
                        resources.AddMiscResource(genericMovies, RESOURCE_KEY_NPC_GENERICS);

                        SPMovieClip sinkingClip = genericMovies["Sinking"];
                        resources.AddMovie(sinkingClip, RESOURCE_KEY_NPC_SINKING);

                        SPMovieClip burningClip = genericMovies["Burning"];
                        resources.AddMovie(burningClip, RESOURCE_KEY_NPC_BURNING);
                    }
                }
	        }
	
	        return resources;
        }

        public override void CheckinPoolResources(ResourceServer resources)
        {
            if (resources == null || mNpcShipArrayPool == null || resources.PoolIndex == -1)
		        return;
            resources.Reset();
    
            List<Dictionary<string, SPMovieClip>> genericArray = mNpcShipArrayPool[0] as List<Dictionary<string, SPMovieClip>>;
            Dictionary<string, List<ResourceServer>> customDict = mNpcShipArrayPool[1] as Dictionary<string, List<ResourceServer>>;
            List<ResourceServer> customArray = customDict[resources.Key];

            // Custom
            PoolIndexer indexer = mCustomIndexers[resources.Key];
            indexer.CheckinIndex(resources.PoolIndex);
            resources.PoolIndex = -1;

            // Generic
            int genericPoolIndex = resources.GetPoolIndex(1);
            if (genericPoolIndex != -1)
            {
                resources.RemoveMiscResourceForKey(RESOURCE_KEY_NPC_GENERICS);
                resources.RemoveDisplayObjectForKey(RESOURCE_KEY_NPC_SINKING);
                resources.RemoveDisplayObjectForKey(RESOURCE_KEY_NPC_BURNING);
                mGenericIndexer.CheckinIndex(genericPoolIndex);
                resources.SetPoolIndex(1, -1);
            }
        }

        public override void ReassignResourceServersToScene(SceneController scene)
        {
            if (mNpcShipArrayPool == null)
                return;

            Dictionary<string, List<ResourceServer>> customDict = mNpcShipArrayPool[1] as Dictionary<string, List<ResourceServer>>;
    
            foreach (KeyValuePair<string, List<ResourceServer>> kvp in customDict)
            {
                foreach (ResourceServer rs in kvp.Value)
                    rs.ReassignScene(scene);
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
                        if (mNpcShipArrayPool != null)
                        {
                            Dictionary<string, List<ResourceServer>> customDict = mNpcShipArrayPool[1] as Dictionary<string, List<ResourceServer>>;

                            foreach (KeyValuePair<string, List<ResourceServer>> kvp in customDict)
                            {
                                foreach (ResourceServer rs in kvp.Value)
                                    rs.Dispose();
                            }

                            mNpcShipArrayPool = null;
                        }

                        mGenericIndexer = null;
                        mCustomIndexers = null;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
    }
}
