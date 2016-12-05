using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    delegate void RaceEventHandler(RaceEvent ev);

    class RaceEvent : SPEvent
    {
        public const string CUST_EVENT_TYPE_RACE_UPDATE = "raceUpdateEvent";

        private const double kRequiredLapTime = 12.0;
        private const double kRequiredMph = 88.0;

        public RaceEvent(Racer racer)
            : base(CUST_EVENT_TYPE_RACE_UPDATE, false)
        {
            mRaceFinished = racer.FinishedRace;
            mCrossedFinishLine = racer.DidJustCrossFinishLine;
		    mLapTime = racer.LapTime;
		    mRaceTime = racer.RaceTime;
		    mLap = racer.Lap;
		    mTotalLaps = racer.TotalLaps;
            mMph = CalculateMph(racer);
        }
        
        #region Fields
        private bool mRaceFinished;
        private bool mCrossedFinishLine;
        private double mLapTime;
        private double mRaceTime;
        private double mMph;
        private int mLap;
        private int mTotalLaps;
        #endregion

        #region Properties
        public bool RaceFinished { get { return mRaceFinished; } }
        public bool CrossedFinishLine { get { return mCrossedFinishLine; } }
        public double LapTime { get { return mLapTime; } }
        public double RaceTime { get { return mRaceTime; } }
        public double Mph { get { return mMph; } }
        public int Lap { get { return mLap; } }
        public int TotalLaps { get { return mTotalLaps; } }
        #endregion

        #region Methods
        public void ResetWithRacer(Racer racer)
        {
            mRaceFinished = racer.FinishedRace;
            mCrossedFinishLine = racer.DidJustCrossFinishLine;
            mLapTime = racer.LapTime;
            mRaceTime = racer.RaceTime;
            mLap = racer.Lap;
            mTotalLaps = racer.TotalLaps;
            mMph = CalculateMph(racer);
        }

        private double CalculateMph(Racer racer)
        {
            double requiredRaceTime = RaceEvent.RequiredRaceTimeForLapCount(mTotalLaps) * ((racer.Lap - 1) / Math.Max(1.0, racer.TotalLaps));
	        double requiredLapTime = kRequiredLapTime * (racer.NextCheckpoint / Math.Max(1.0, racer.TotalCheckpoints));
            double mph = (SPMacros.SP_IS_DOUBLE_EQUAL(racer.RaceTime, 0)) ? 0 : kRequiredMph / Math.Max(0.5f, (racer.RaceTime / Math.Max(1.0, requiredRaceTime + requiredLapTime)));
	
	        // Smooth it out for lap 1 until the data pool averages out
	        if (racer.Lap == 1)
            {
                float fractionComplete = (float)Math.Min(1.0f, racer.RaceTime / kRequiredLapTime);
                mph = 92.0 * (1.0f - fractionComplete) + mph * fractionComplete;
            }

	        return mph;
        }

        public static double RequiredRaceTimeForLapCount(int lapCount)
        {
            return lapCount * kRequiredLapTime;
        }
        #endregion
    }
}
