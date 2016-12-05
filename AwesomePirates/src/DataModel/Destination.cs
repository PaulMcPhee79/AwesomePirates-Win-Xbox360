using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AwesomePirates
{
    class Destination : IReusable
    {
        private const uint kDestinationReuseKey = 1;

        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 100;
            uint reuseKey = kDestinationReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetDestination();
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

        public static Destination GetDestination()
        {
            Destination dest = CheckoutReusable(kDestinationReuseKey) as Destination;

            if (dest != null)
            {
                dest.Reuse();
            }
            else
            {
                dest = new Destination();
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed Destination ReusableCache.");
#endif
            }

            return dest;
        }

        public Destination()
        {
            mInUse = true;
            mPoolIndex = -1;
            mIsExclusive = true;
            mFinishIsDest = false;
            mSeaLaneA = null;
            mSeaLaneB = null;
            mSeaLaneC = null;
            mAdjustedSeaLaneC = null;
        }

        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private bool mIsExclusive;		// true: Prevents checking in of sea lanes that we don't own.
        private bool mFinishIsDest;
        private int mSpawnPlaneIndex;	// Superfluous - same as mStart. TODO: remove and let accessor point to mStart?
        private Vector2 mLoc;
        private Vector2 mDest;

        private CCPoint mSeaLaneA;
        private CCPoint mSeaLaneB;
        private CCPoint mSeaLaneC;
        private CCPoint mAdjustedSeaLaneC;
        private int mStart;			// Index into mVacantSpawnPlanes/mOccupiedSpawnPlanes in ActorAi
        private int mFinish;		// Index into mVacantSpawnPlanes/mOccupiedSpawnPlanes in ActorAi
        #endregion

        #region Properties
        public uint ReuseKey { get { return kDestinationReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public bool IsExclusive { get { return mIsExclusive; } set { mIsExclusive = value; } }
        public bool FinishIsDest { get { return mFinishIsDest; } set { mFinishIsDest = value; } }
        public int SpawnPlaneIndex { get { return mSpawnPlaneIndex; } set { mSpawnPlaneIndex = value; } }
        public Vector2 Loc { get { return mLoc; } set { mLoc = value; } }
        public Vector2 Dest { get { return mDest; } set { mDest = value; } }
        public CCPoint SeaLaneA
        {
            get { return mSeaLaneA; }
            set
            {
                mSeaLaneA = value;

                if (value != null)
                {
                    mLoc.X = value.X;
                    mLoc.Y = value.Y;
                }
            }
        }
        public CCPoint SeaLaneB
        {
            get { return mSeaLaneB; }
            set
            {
                mSeaLaneB = value;

                if (value != null)
                {
                    mDest.X = value.X;
                    mDest.Y = value.Y;
                }
            }
        }
        public CCPoint SeaLaneC
        {
            get { return mSeaLaneC; }
            set
            {
                mSeaLaneC = value;

                if (value != null)
                {
                    if (mAdjustedSeaLaneC != null)
                    {
                        mDest.X = mAdjustedSeaLaneC.X;
                        mDest.Y = mAdjustedSeaLaneC.Y;
                    }
                    else
                    {
                        mDest.X = value.X;
                        mDest.Y = value.Y;
                    }
                }
            }
        }
        public CCPoint AdjustedSeaLaneC { get { return mAdjustedSeaLaneC; } set { mAdjustedSeaLaneC = value; } }
        public int Start { get { return mStart; } set { mStart = value; } }
        public int Finish { get { return mFinish; } set { mFinish = value; } }
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

            mIsExclusive = true;
            mFinishIsDest = false;
            mSeaLaneA = null;
            mSeaLaneB = null;
            mSeaLaneC = null;
            mAdjustedSeaLaneC = null;

            mInUse = false;
            CheckinReusable(this);
        }

        public void SetFinishAsDest()
        {
            mDest.X = mSeaLaneB.X;
            mDest.Y = mSeaLaneB.Y;
            SeaLaneC = null; // Mark edge case as handled
            mFinishIsDest = true;
        }

        public void SetDestX(float x) { mDest.X = x; }
        public void SetDestY(float y) { mDest.Y = y; }
        public void SetLocX(float x) { mLoc.X = x; }
        public void SetLocY(float y) { mLoc.Y = y; }

        public static Destination DestinationWithDestination(Destination destination)
        {
            Destination dest = GetDestination();
            dest.IsExclusive = destination.IsExclusive;
            dest.SpawnPlaneIndex = destination.SpawnPlaneIndex;

            dest.AdjustedSeaLaneC = destination.AdjustedSeaLaneC;
            dest.SeaLaneA = destination.SeaLaneA;
            dest.SeaLaneB = destination.SeaLaneB;
            dest.SeaLaneC = destination.SeaLaneC;

            dest.Start = destination.Start;
            dest.Finish = destination.Finish;
            return dest;
        }
        #endregion
    }
}
