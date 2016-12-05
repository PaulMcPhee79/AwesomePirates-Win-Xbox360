using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class CannonballGroup : Prop, IReusable
    {
        private static int s_nextGroupId = 1;
        private const double kSoundInterval = 0.1;

        private const uint kCannonballGroupReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 40;
            uint reuseKey = kCannonballGroupReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetCannonballGroup(1);
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

        public static CannonballGroup GetCannonballGroup(int hitQuota)
        {
            CannonballGroup grp = CheckoutReusable(kCannonballGroupReuseKey) as CannonballGroup;

            if (grp != null)
            {
                grp.Reuse();
                grp.HitQuota = hitQuota;
            }
            else
            {
                grp = new CannonballGroup(hitQuota);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed CannonballGroup ReusableCache.");
#endif
            }

            return grp;
        }

        public CannonballGroup(int hitQuota)
            : base(0)
        {
            mInUse = true;
            mPoolIndex = -1;
            mAdvanceable = true;
            mHitQuota = hitQuota;
		    mGroupId = ++s_nextGroupId;
		    mHitCounter = 0;
            mSplashSoundTimer = 0;
            mExplosionSoundTimer = 0;
            mIgnoreGroupMiss = false;
		    mCannonballs = new List<Cannonball>();
		    mRicochetTargets = new List<Actor>();
        }

        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private int mGroupId;
        private int mHitQuota;
        private int mHitCounter;
        private bool mIgnoreGroupMiss;
        private double mSplashSoundTimer;
        private double mExplosionSoundTimer;
        private List<Cannonball> mCannonballs;
        private List<Actor> mRicochetTargets;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kCannonballGroupReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        protected int HitQuota { get { return mHitQuota; } set { mHitQuota = value; } }
        public int GroupId { get { return mGroupId; } }
        private bool HasGroupMissed { get { return (!mIgnoreGroupMiss && mHitCounter < mHitQuota); } }
        #endregion

        #region Methods
        public void Reuse()
        {
            if (InUse)
                return;

            mGroupId = ++s_nextGroupId;
            mHitCounter = 0;
            mSplashSoundTimer = 0;
            mExplosionSoundTimer = 0;
            mIgnoreGroupMiss = false;

            if (mCannonballs == null)
                mCannonballs = new List<Cannonball>();
            if (mRicochetTargets == null)
                mRicochetTargets = new List<Actor>();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mCannonballs != null)
                mCannonballs.Clear();
            if (mRicochetTargets != null)
                mRicochetTargets.Clear();

            mInUse = false;
            CheckinReusable(this);
        }

        public void AddCannonball(Cannonball cannonball)
        {
            // Don't want to add it to more than one group
            if (cannonball.CannonballGroupId == 0 && !mCannonballs.Contains(cannonball))
            {
		        cannonball.CannonballGroup = this;
                mCannonballs.Add(cannonball);
		        ++mHitCounter;
	        }
        }

        public void RemoveCannonball(Cannonball cannonball)
        {
            if (cannonball != null) {
		        cannonball.CannonballGroup = null;
                mCannonballs.Remove(cannonball);
	        }

            if (mCannonballs.Count == 0)
                ExpireGroup();
        }

        public void CannonballImpacted(CannonballImpactLog log)
        {
            Cannonball cannonball = log.Cannonball;

	        // Destruction case
	        if (log.IsCannonballMarkedForRemoval)
            {
                RemoveCannonball(cannonball);
		        return;
	        }
	
	        if (log.RicochetTarget != null)
            {
		        log.MayRicochet = !mRicochetTargets.Contains(log.RicochetTarget);
	
		        if (log.MayRicochet)
                    mRicochetTargets.Add(log.RicochetTarget);
		
		        if (cannonball.RicochetCount == 0)
                {
			        // This is the initial hit and we should only play the sound once else it sounds bad in unison.
			        log.ShouldPlaySounds = (mExplosionSoundTimer <= 0);
                    mExplosionSoundTimer = kSoundInterval;
		        }
                else
                {
			        log.ShouldPlaySounds = true;
		        }
	        }
            else
            {
		        if (cannonball.RicochetCount == 0)
                {
                    log.ShouldPlaySounds = (mSplashSoundTimer <= 0);
                    mSplashSoundTimer = kSoundInterval;
			        --mHitCounter;
		        }
                else
                {
			        log.ShouldPlaySounds = true;
		        }
	        }
            log.GroupMissed = HasGroupMissed;
        }

        public void IgnoreGroupMiss()
        {
            mIgnoreGroupMiss = true; 
        }

        private void ExpireGroup()
        {
            mScene.RemoveProp(this);
        }

        public override void AdvanceTime(double time)
        {
            mSplashSoundTimer -= time;
            mExplosionSoundTimer -= time;
        }
        #endregion
    }
}
