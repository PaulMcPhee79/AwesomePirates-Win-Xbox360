using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class CannonballInfamyBonus : IReusable
    {
        private const uint kInfamyBonusReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 100;
            uint reuseKey = kInfamyBonusReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetCannonballInfamyBonus();
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

        public static CannonballInfamyBonus GetCannonballInfamyBonus()
        {
            CannonballInfamyBonus infamyBonus = CheckoutReusable(kInfamyBonusReuseKey) as CannonballInfamyBonus;

            if (infamyBonus != null)
            {
                infamyBonus.Reuse();
            }
            else
            {
                infamyBonus = new CannonballInfamyBonus();
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed CannonballInfamyBonus ReusableCache.");
#endif
            }

            return infamyBonus;
        }

        public CannonballInfamyBonus()
        {
            mProcType = 0;
            mProcMultiplier = 1;
            mProcAddition = 0;

            mRicochetBonus = 0;
            mRicochetAddition = 0;
            mRicochetMultiplier = 1;

            mMiscBitmap = 0;
        }

        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private uint mProcType;
        private int mProcMultiplier;
        private int mProcAddition;

        private int mRicochetBonus;
        private int mRicochetAddition;
        private float mRicochetMultiplier;

        private uint mMiscBitmap;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kInfamyBonusReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public uint ProcType { get { return mProcType; } set { mProcType = value; } }
        public int ProcMultiplier { get { return mProcMultiplier; } set { mProcMultiplier = value; } }
        public int ProcAddition { get { return mProcAddition; } set { mProcAddition = value; } }

        public int RicochetBonus { get { return mRicochetBonus; } set { mRicochetBonus = value; } }
        public int RicochetAddition { get { return mRicochetAddition; } set { mRicochetAddition = value; } }
        public float RicochetMultiplier { get { return mRicochetMultiplier; } set { mRicochetMultiplier = value; } }

        public uint MiscBitmap { get { return mMiscBitmap; } set { mMiscBitmap = value; } }
        #endregion

        #region Methods
        public void Reuse()
        {
            if (InUse)
                return;

            mProcType = 0;
            mProcMultiplier = 1;
            mProcAddition = 0;

            mRicochetBonus = 0;
            mRicochetAddition = 0;
            mRicochetMultiplier = 1;

            mMiscBitmap = 0;

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            mInUse = false;
            CheckinReusable(this);
        }

        public CannonballInfamyBonus Copy()
        {
            CannonballInfamyBonus copy = GetCannonballInfamyBonus();
            copy.ProcType = ProcType;
            copy.ProcMultiplier = ProcMultiplier;
            copy.ProcAddition = ProcAddition;

            copy.RicochetBonus = RicochetBonus;
            copy.RicochetAddition = RicochetAddition;
            copy.RicochetMultiplier = RicochetMultiplier;

            copy.MiscBitmap = MiscBitmap;

            return copy;
        }
        #endregion
    }
}
