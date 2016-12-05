using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class Racer : SPEventDispatcher
    {
        private const int kUpdateThrottleLimit = 3; // Prevent excessive event generation

        public Racer(ShipActor owner, int laps, int checkpoints)
        {
            mRacing = false;
            mDidJustCrossFinishLine = false;
		    mUpdateThrottle = 0;
		    mLap = 0;
		    mTotalLaps = laps;
		    mNextCheckpoint = 0;
		    mTotalCheckpoints = checkpoints;
		    mPrevCheckpoint = mTotalCheckpoints - 1;
		    mLapTime = 0;
		    mRaceTime = 0;
		    mOwner = owner;
            AddEventListener(RaceEvent.CUST_EVENT_TYPE_RACE_UPDATE, (RaceEventHandler)mOwner.OnRaceUpdate);
        }
        
        #region Fields
        private bool mRacing;
        private bool mDidJustCrossFinishLine;
        private int mUpdateThrottle;
        private int mTotalCheckpoints;
        private int mNextCheckpoint;
        private int mPrevCheckpoint;
        private int mTotalLaps;
        private int mLap;
        private double mLapTime;
        private double mRaceTime;
        private RaceEvent mRaceEvent;
        private ShipActor mOwner;
        #endregion

        #region Properties
        public bool Racing { get { return mRacing; } }
        public bool FinishedRace { get { return (mLap == mTotalLaps && !mRacing); } }
        public bool DidJustCrossFinishLine { get { return mDidJustCrossFinishLine; } }
        public double LapTime { get { return mLapTime; } }
        public double RaceTime { get { return mRaceTime; } }
        public int Lap { get { return mLap; } }
        public int TotalLaps { get { return mTotalLaps; } }
        public int NextCheckpoint { get { return mNextCheckpoint; } }
        public int PrevCheckpoint { get { return mPrevCheckpoint; } }
        public int TotalCheckpoints { get { return mTotalCheckpoints; } }
        public ShipActor Owner { get { return mOwner; } }
        #endregion

        #region Methods
        private void NextCheckpointSetTo(int value)
        {
            if (value <= mTotalCheckpoints)
            {
                mPrevCheckpoint = mNextCheckpoint;
                mNextCheckpoint = value;
            }
        }

        public void PrepareForNewRace()
        {
            mLap = 0;
            mNextCheckpoint = 0;
            mPrevCheckpoint = mTotalCheckpoints - 1;
            mLapTime = 0;
            mRaceTime = 0;
        }

        public void StartRace()
        {
            mRacing = true;
            mLap = 1;
        }

        public int CheckpointReached(int index)
        {
            if (mRacing && index == mNextCheckpoint)
                NextCheckpointSetTo(mNextCheckpoint + 1);
	        return mNextCheckpoint;
        }

        public bool FinishLineCrossed()
        {
            if (mRacing && mNextCheckpoint == mTotalCheckpoints)
            {
                mDidJustCrossFinishLine = true;
        
		        if (mLap < mTotalLaps)
                {
                    BroadcastRaceUpdate();
			        ++mLap;
			        mLapTime = 0;
			        NextCheckpointSetTo(0);
		        }
                else
                {
			        mRacing = false;
                    BroadcastRaceUpdate();
		        }
        
                mDidJustCrossFinishLine = false;
	        }

	        return mRacing;
        }

        public void BroadcastRaceUpdate()
        {
            if (mRaceEvent == null)
                mRaceEvent = new RaceEvent(this);
            else
                mRaceEvent.ResetWithRacer(this);

            DispatchEvent(mRaceEvent);
        }

        public void AdvanceTime(double time)
        {
            if (mRacing)
            {
		        mLapTime += time;
		        mRaceTime += time;
	        }
	
	        if (++mUpdateThrottle == kUpdateThrottleLimit)
            {
		        mUpdateThrottle = 0;
                BroadcastRaceUpdate();
	        }
        }
        #endregion
    }
}
