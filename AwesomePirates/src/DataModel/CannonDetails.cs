using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class CannonDetails : IReusable
    {
        private const float kMaxCannonDamageRating = 7f;

        private const uint kCannonDetailsReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 40;
            uint reuseKey = kCannonDetailsReuseKey;
            string cannonType = "Perisher";
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetCannonDetails(cannonType);
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

        public static CannonDetails GetCannonDetails(string type)
        {
            CannonDetails cannonDetails = CheckoutReusable(kCannonDetailsReuseKey) as CannonDetails;

            if (cannonDetails != null)
            {
                cannonDetails.Reuse();
            }
            else
            {
                cannonDetails = CannonFactory.Factory.CreateCannonDetailsForType(type);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed CannonDetails ReusableCache.");
#endif
            }

            return cannonDetails;
        }

        public CannonDetails(string type)
        {
            mType = type;
            mInUse = true;
            mPoolIndex = -1;
		    mRangeRating = 0;
		    mDamageRating = 0;
		    mBitmap = 0;
		    mComboMax = 0;
		    mRicochetBonus = 0;
		    mImbues = 0;
		    mReloadInterval = 1.25f;
		    mShotType = null;
            mTextureNameBase = null;
            mTextureNameBarrel = null;
            mTextureNameWheel = null;
            mTextureNameMenu = null;
            mTextureNameFlash = null;
            mDeckSettings = null;
        }

        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private string mType;
        private int mRangeRating;
        private int mDamageRating;
        private uint mBitmap;
        private int mComboMax;
        private int mRicochetBonus;
        private uint mImbues;
        private float mReloadInterval;
        private string mShotType;

        private string mTextureNameBase;
        private string mTextureNameBarrel;
        private string mTextureNameWheel;
        private string mTextureNameMenu;
        private string mTextureNameFlash;
        private Dictionary<string, object> mDeckSettings;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kCannonDetailsReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public string Type { get { return mType; } }
        public int RangeRating { get { return mRangeRating; } set { mRangeRating = value; } }
        public int DamageRating { get { return mDamageRating; } set { mDamageRating = value; } }
        public float Bore { get { return (2 * mDamageRating + 18) / (2 * kMaxCannonDamageRating + 18); } }
        public static float DefaultBore { get { return 1f; } }
        public uint Bitmap { get { return mBitmap; } set { mBitmap = value; } }
        public int ComboMax { get { return mComboMax; } set { mComboMax = value; } }
        public int RicochetBonus { get { return mRicochetBonus; } set { mRicochetBonus = value; } }
        public uint Imbues { get { return mImbues; } set { mImbues = value; } }
        public float ReloadInterval { get { return mReloadInterval; } set { mReloadInterval = value; } }
        public string ShotType { get { return mShotType; } set { mShotType = value; } }

        public string TextureNameBase { get { return mTextureNameBase; } set { mTextureNameBase = value; } }
        public string TextureNameBarrel { get { return mTextureNameBarrel; } set { mTextureNameBarrel = value; } }
        public string TextureNameWheel { get { return mTextureNameWheel; } set { mTextureNameWheel = value; } }
        public string TextureNameMenu { get { return mTextureNameMenu; } set { mTextureNameMenu = value; } }
        public string TextureNameFlash { get { return mTextureNameFlash; } set { mTextureNameFlash = value; } }
        public Dictionary<string, object> DeckSettings { get { return mDeckSettings; } set { mDeckSettings = value; } }
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

            mInUse = false;
            CheckinReusable(this);
        }
        #endregion
    }
}
