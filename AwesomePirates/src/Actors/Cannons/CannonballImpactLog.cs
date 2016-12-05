using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class CannonballImpactLog : IReusable
    {
        public enum ImpactType
        {
            Water = 0,
            Land,
            NpcShip,
            PlayerShip,
            RemoveMe
        }

        private const uint kImpactLogReuseKey = 1;

        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 10;
            uint reuseKey = kImpactLogReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetImpactLog(null, ImpactType.Water, null);
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

        public static CannonballImpactLog GetImpactLog(Cannonball cannonball, ImpactType impactType, Actor ricochetTarget)
        {
            CannonballImpactLog log = CheckoutReusable(kImpactLogReuseKey) as CannonballImpactLog;

            if (log != null)
            {
                log.Reuse();
                log.TypeOfImpact = impactType;
                log.Cannonball = cannonball;
                log.RicochetTarget = ricochetTarget;
            }
            else
            {
                log = new CannonballImpactLog(cannonball, impactType, ricochetTarget);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed CannonballImpactLog ReusableCache.");
#endif
            }

            return log;
        }

        public CannonballImpactLog(Cannonball cannonball, ImpactType impactType, Actor ricochetTarget)
        {
            mImpactType = impactType;
            mCannonball = cannonball;
            mRicochetTarget = ricochetTarget;
            mInUse = true;
            mPoolIndex = -1;
            mGroupMissed = false;
            mMayRicochet = false;
		    mShouldPlaySounds = false;
        }

        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private bool mGroupMissed;
        private bool mMayRicochet;
        private bool mShouldPlaySounds;
        private ImpactType mImpactType;
        private Cannonball mCannonball;
        private Actor mRicochetTarget;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kImpactLogReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public ImpactType TypeOfImpact { get { return mImpactType; } protected set { mImpactType = value; } }
        public bool Missed { get { return (mImpactType == ImpactType.Water || mImpactType == ImpactType.Land); } }
        public Cannonball Cannonball { get { return mCannonball; } protected set { mCannonball = value; } }
        public Actor RicochetTarget { get { return mRicochetTarget; } protected set { mRicochetTarget = value; } }
        public bool IsCannonballMarkedForRemoval { get { return (mImpactType == ImpactType.RemoveMe); } }

        // Feedback properties (can be set by receivers)
        public bool GroupMissed { get { return mGroupMissed; } set { mGroupMissed = value; } }
        public bool MayRicochet { get { return mMayRicochet; } set { mMayRicochet = value; } }
        public bool ShouldPlaySounds { get { return mShouldPlaySounds; } set { mShouldPlaySounds = value; } }
        #endregion

        #region Methods
        public void Reuse()
        {
            if (InUse)
                return;
            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            mCannonball = null;
            mRicochetTarget = null;
            mGroupMissed = false;
            mMayRicochet = false;
            mShouldPlaySounds = false;

            mInUse = false;
            CheckinReusable(this);
        }
        #endregion
    }
}
