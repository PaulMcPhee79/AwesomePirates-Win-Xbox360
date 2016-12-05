using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class ReusableCache
    {
        public ReusableCache(int capacity = 10)
        {
            int adjustedCapacity = Math.Max(1, capacity);
            mCache = new Dictionary<uint,List<IReusable>>(adjustedCapacity);
            mIndexers = new Dictionary<uint,PoolIndexer>(adjustedCapacity);
        }

        #region Fields
        private Dictionary<uint, List<IReusable>> mCache;
        private Dictionary<uint, PoolIndexer> mIndexers;
        #endregion

        #region Methods
        public void AddKey(int qty, uint key)
        {
            if (qty <= 0 || mCache == null || mCache.ContainsKey(key))
                return;

            mCache[key] = new List<IReusable>(qty);

            PoolIndexer poolIndexer = new PoolIndexer(qty, "ReusableCache: " + key.ToString());
            poolIndexer.InitIndexes(0, 1);
            mIndexers[key] = poolIndexer;
        }

        public void AddReusable(IReusable reusable)
        {
            if (reusable == null || mCache == null || !mCache.ContainsKey(reusable.ReuseKey))
                return;

            mCache[reusable.ReuseKey].Add(reusable);
        }

        public void VerifyCacheIntegrity()
        {
            if (mCache == null)
                return;

            foreach (KeyValuePair<uint, List<IReusable>> kvp in mCache)
            {
                if (kvp.Value.Count != mIndexers[kvp.Key].Capacity)
                    throw new InvalidOperationException("ReusableCache failed data integrity test.");
            }
        }

        public IReusable Checkout(uint key)
        {
            IReusable checkout = null;

            if (mCache.ContainsKey(key))
            {
                int index = mIndexers[key].CheckoutNextIndex();

                if (index != -1)
                {
                    checkout = mCache[key][index];
                    checkout.PoolIndex = index;
                }
            }
            return checkout;
        }

        public void Checkin(IReusable reusable)
        {
            if (reusable == null || reusable.InUse || reusable.PoolIndex == -1)
            {
#if DEBUG
                throw new ArgumentException("Attempt to Checkin IReusable with invalid state.");
#else
                return;
#endif
            }

            mIndexers[reusable.ReuseKey].CheckinIndex(reusable.PoolIndex);
            reusable.PoolIndex = -1;
        }

        public void Purge(bool disposeContents = true)
        {
            if (mCache != null)
            {
                foreach (KeyValuePair<uint, List<IReusable>> kvp in mCache)
                {
                    foreach (IReusable reusable in kvp.Value)
                    {
                        if (reusable is IDisposable)
                            (reusable as IDisposable).Dispose();
                    }
                }

                mCache.Clear();
                mIndexers.Clear();
            }
        }
        #endregion
    }
}
