using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class ShipDetails : SPEventDispatcher, IReusable
    {
        public enum ShipSide
        {
            Port = 0,
            Starboard
        }

        public const int NUM_NPC_COSTUME_IMAGES = 7;
        public const string CUST_EVENT_TYPE_PRISONERS_VALUE_CHANGED = "prisonersValueChangedEvent";

        private static ReusableCache sCache = null;
        private static bool sCaching = false;
        private static Dictionary<string, uint> sNpcShipDetailsReuseKeys = null;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            List<string> npcShipTypes = ShipFactory.Factory.AllNpcShipTypes;
            sCache = new ReusableCache(npcShipTypes.Count);

            if (sNpcShipDetailsReuseKeys == null)
                sNpcShipDetailsReuseKeys = new Dictionary<string, uint>(npcShipTypes.Count);

            int cacheSize = 10;
            uint reuseKey = 1;
            ShipDetails shipDetails = null;

            foreach (string shipType in npcShipTypes)
            {
                switch (shipType)
                {
                    case "SilverTrain":
                    case "TreasureFleet":
                        cacheSize = 2;
                        break;
                    case "Pirate":
                    case "Navy":
                        cacheSize = 10;
                        break;
                    case "Escort":
                        cacheSize = 6;
                        break;
                    default:
                        cacheSize = 15; // Merchant ships x3
                        break;
                }

                sCache.AddKey(cacheSize, reuseKey);
                sNpcShipDetailsReuseKeys.Add(shipType, reuseKey);

                for (int i = 0; i < cacheSize; ++i)
                {
                    shipDetails = GetNpcShipDetails(shipType);
                    shipDetails.ReuseKey = reuseKey;
                    shipDetails.Hibernate();
                    sCache.AddReusable(shipDetails);
                }

                ++reuseKey;
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

        public static ShipDetails GetNpcShipDetails(string type)
        {
            ShipDetails shipDetails = CheckoutReusable(sNpcShipDetailsReuseKeys[type]) as ShipDetails;

            if (shipDetails != null)
            {
                shipDetails.Reuse();
            }
            else
            {
                shipDetails = ShipFactory.Factory.CreateNpcShipDetailsForType(type);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed ShipDetails ReusableCache.");
#endif
            }

            return shipDetails;
        }

        public ShipDetails(string type)
        {
            mType = type;
            mInUse = true;
            mPoolIndex = -1;
            mBitmap = 0;
            mPrisoners = new Dictionary<string, Prisoner>();
            mSpeedRating = 0;
            mControlRating = 0;
            mReloadInterval = 1f;
            mInfamyBonus = 0;
            mMutinyPenalty = 0;
            mTextureName = null;
            mTextureFutureName = null;
        }

        #region Fields
        private bool mInUse;
        private uint mReuseKey;
        private int mPoolIndex;

        private string mType;
        private uint mBitmap; // Ship Type ID
        private int mSpeedRating;
        private int mControlRating;
        private float mRudderOffset;
        private float mReloadInterval;
        private int mInfamyBonus;
        private int mMutinyPenalty;
        private string mTextureName;
        private string mTextureFutureName;
        private Dictionary<string,Prisoner> mPrisoners;
        #endregion

        #region Properties
        public uint ReuseKey { get { return mReuseKey; } protected set { mReuseKey = value; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public string Type { get { return mType; } }
        public uint Bitmap { get { return mBitmap; } set { mBitmap = value; } }
        public int SpeedRating { get { return mSpeedRating; } set { mSpeedRating = value; } }
        public int ControlRating { get { return mControlRating; } set { mControlRating = value; } }

        public float RudderOffset { get { return mRudderOffset; } set { mRudderOffset = value; } }
        public float ReloadInterval { get { return mReloadInterval; } set { mReloadInterval = value; } }
        public int InfamyBonus { get { return mInfamyBonus; } set { mInfamyBonus = value; } }
        public int MutinyPenalty { get { return mMutinyPenalty; } set { mMutinyPenalty = value; } }

        public string TextureName { get { return mTextureName; } set { mTextureName = value; } }
        public string TextureFutureName { get { return mTextureFutureName; } set { mTextureFutureName = value; } }

        public Dictionary<string,Prisoner> Prisoners { get { return mPrisoners; } }
        public Prisoner PlankVictim
        {
            get
            {
                Prisoner victim = null;

                foreach (string key in mPrisoners.Keys)
                {
                    victim = mPrisoners[key];
                    break;
                }

                return victim;
            }
        }
        public bool IsBrigFull { get { return (mPrisoners.Count >= ShipFactory.Factory.AllPrisonerNames.Count); } }
        #endregion

        #region Methods
        public void Reuse()
        {
            if (InUse)
                return;

            if (mPrisoners == null)
                mPrisoners = new Dictionary<string, Prisoner>();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mPrisoners != null)
                mPrisoners.Clear();

            mInUse = false;
            CheckinReusable(this);
        }

        public void AddPrisoner(string prisonerName)
        {
            if (prisonerName == null)
                return;

            Prisoner prisoner;
            mPrisoners.TryGetValue(prisonerName, out prisoner);

            if (prisoner == null)
            {
                prisoner = ShipFactory.Factory.CreatePrisonerForName(prisonerName);
                mPrisoners[prisonerName] = prisoner;
                DispatchEvent(new NumericValueChangedEvent(CUST_EVENT_TYPE_PRISONERS_VALUE_CHANGED, mPrisoners.Count, mPrisoners.Count - 1));
            }
        }

        public Prisoner AddRandomPrisoner()
        {
            List<string> prisonerNames = ShipFactory.Factory.AllPrisonerNames;

            if (mPrisoners.Count == prisonerNames.Count)
                return null;

            Prisoner p;

            for (int i = GameController.GC.NextRandom(prisonerNames.Count - 1), count = 0; count < prisonerNames.Count; ++i, ++count)
            {
                if (i >= prisonerNames.Count)
                    i = 0;

                string name = prisonerNames[i];
                mPrisoners.TryGetValue(name, out p);

                if (p == null)
                {
                    AddPrisoner(name);
                    return mPrisoners[name];
                }
            }

            return null;
        }

        public void AddPrisonersFromDictionary(Dictionary<string, Prisoner> dict)
        {
            foreach (string key in dict.Keys)
                AddPrisoner(key);
        }

        public void PrisonerPushedOverboard(Prisoner prisoner)
        {
            RemovePrisoner(prisoner.Name);
        }

        public void RemovePrisoner(string prisonerName)
        {
            if (prisonerName == null)
                return;

            Prisoner prisoner;
            mPrisoners.TryGetValue(prisonerName, out prisoner);

            if (prisoner != null)
            {
                mPrisoners.Remove(prisonerName);
                DispatchEvent(new NumericValueChangedEvent(CUST_EVENT_TYPE_PRISONERS_VALUE_CHANGED, mPrisoners.Count, mPrisoners.Count + 1));
            }
        }

        public void RemoveAllPrisoners()
        {
            if (mPrisoners != null)
            {
                int count = mPrisoners.Count;
                mPrisoners.Clear();
                DispatchEvent(new NumericValueChangedEvent(CUST_EVENT_TYPE_PRISONERS_VALUE_CHANGED, 0, count));
            }
        }

        public void Reset()
        {
            RemoveAllPrisoners();
        }
        #endregion
    }
}
