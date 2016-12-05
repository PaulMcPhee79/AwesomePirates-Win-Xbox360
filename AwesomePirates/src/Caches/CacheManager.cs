using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class CacheManager : IDisposable
    {
        //public const uint RESOURCE_KEY_CHAIN = UInt32.MaxValue;

        public const uint CACHE_CANNONBALL = 1;
        public const uint CACHE_LOOT_PROP = 2;
        public const uint CACHE_NPC_SHIP = 3;
        public const uint CACHE_POINT_MOVIE = 4;
        public const uint CACHE_WAKE = 5;
        public const uint CACHE_SHARK = 6;
        public const uint CACHE_POOL_ACTOR = 7;
        public const uint CACHE_TEMPEST = 8;
        public const uint CACHE_BLAST_PROP = 9;
        public const uint CACHE_MISC = 10;
        
        public CacheManager()
        {
            mArrayPool = null;
            mArrayIndexer = null;

            mDictPool = null;
            mDictIndexers = null;
        }

        #region Fields
        protected bool mIsDisposed = false;
        protected List<ResourceServer> mArrayPool;
        protected PoolIndexer mArrayIndexer;
        
        protected Dictionary<string, List<ResourceServer>> mDictPool;
        protected Dictionary<string, PoolIndexer> mDictIndexers;
        #endregion

        #region Methods
        public virtual void FillResourcePoolForScene(SceneController scene) { }

        public virtual void DrainResourcePool()
        {
            if (mArrayPool != null)
            {
                mArrayPool.Clear();
                mArrayPool = null;
                mArrayIndexer = null;
            }

            if (mDictPool != null)
            {
                mDictPool.Clear();
                mDictPool = null;
                mDictIndexers = null;
            }
        }

        public virtual ResourceServer CheckoutPoolResources()
        {
            ResourceServer resources = null;
            
            if (mArrayIndexer != null)
            {
                int poolIndex = mArrayIndexer.CheckoutNextIndex();
                if (poolIndex != -1)
                {
                    resources = mArrayPool[poolIndex];
                    resources.PoolIndex = poolIndex;
                }
	        }
	
	        return resources;
        }

        public virtual ResourceServer CheckoutPoolResourcesForKey(string key)
        {
            if (mDictPool == null)
                return null;

            ResourceServer resources = null;

            if (mDictPool.ContainsKey(key))
            {
                List<ResourceServer> array = mDictPool[key];
                PoolIndexer indexer = mDictIndexers[key];
                int poolIndex = indexer.CheckoutNextIndex();

                if (poolIndex != -1)
                {
                    resources = array[poolIndex];
                    resources.PoolIndex = poolIndex;
                }
            }

            return resources;
        }

        public virtual void CheckinPoolResources(ResourceServer resources)
        {
            if (resources == null)
		        return;
            resources.Reset();

	        if (resources.Key != null)
            {
                if (mDictPool != null && resources.PoolIndex != -1)
                {
                    if (mDictIndexers.ContainsKey(resources.Key))
                    {
                        PoolIndexer indexer = mDictIndexers[resources.Key];
                        indexer.CheckinIndex(resources.PoolIndex);
                        resources.PoolIndex = -1;
                    }
                }
	        }
            else if (mArrayPool != null && resources.PoolIndex != -1)
            {
                mArrayIndexer.CheckinIndex(resources.PoolIndex);
                resources.PoolIndex = -1;
	        }
        }

        public virtual void ReassignResourceServersToScene(SceneController scene)
        {
            if (mDictPool != null)
            {
                foreach (KeyValuePair<string, List<ResourceServer>> kvp in mDictPool)
                {
                    foreach (ResourceServer rs in kvp.Value)
                        rs.ReassignScene(scene);
                }
            }
    
            if (mArrayPool != null)
            {
                foreach (ResourceServer rs in mArrayPool)
                    rs.ReassignScene(scene);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                if (disposing)
                {
                    if (mArrayPool != null)
                    {
                        foreach (ResourceServer rs in mArrayPool)
                            rs.Dispose();
                    }

                    if (mDictPool != null)
                    {
                        foreach (KeyValuePair<string, List<ResourceServer>> kvp in mDictPool)
                        {
                            foreach (ResourceServer rs in kvp.Value)
                                rs.Dispose();
                        }
                    }

                    DrainResourcePool();
                }

                mIsDisposed = true;
            }
        }

        ~CacheManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
