using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class Weather : Prop
    {
        private enum WeatherState
        {
            None = 0,
            Clear,
            Clearing,
            Cloudy,
            Overcast
        }

        private const int kDefaultCloudCount = 1;

        public Weather(int category, int cloudCount)
            : base(category)
        {
            mWeatherEnabled = true;
		    mCloudSpawnTimer = 0;
		    mMaxClouds = cloudCount;
		    mCloudAlpha = 1.0f;
		    mClouds = new List<Cloud>(mMaxClouds);
            SetState(WeatherState.Clear);
            SetState((WeatherState)GameController.GC.NextRandom((int)WeatherState.Cloudy, (int)WeatherState.Overcast));
        }
        
        #region Fields
        private bool mWeatherEnabled;
        private bool mCycleComplete;
        private WeatherState mState;
        private double mCycleTimer;
        private double mCloudSpawnTimer;
        private double mWeatherStateTimer;
        private int mMaxClouds;
        private int mCloudDensity;
        private float mWindVelX;
        private float mWindVelY;
        private float mCloudAlpha;
        private List<Cloud> mClouds;
        #endregion

        #region Properties
        public bool Enabled { get { return mWeatherEnabled; } set { mWeatherEnabled = value; } }
        public float CloudAlpha { get { return mCloudAlpha; } set { mCloudAlpha = value; } }
        #endregion

        #region Methods
        private void SetState(WeatherState state)
        {
            double duration = 0.0;
            GameController gc = GameController.GC;
	
	        switch (state)
            {
		        case WeatherState.None:
			        break;
		        case WeatherState.Clear:
		        {
			        // The only state in which we can safely change the wind's direction without it looking unnatural
			        mClouds.Clear();

                    float randVel = gc.NextRandom(3, 5) / 17.5f;
                    mWindVelX = ((gc.NextRandom(0, 1) != 0) ? randVel : -randVel) * gc.Fps;
                    randVel = gc.NextRandom(3, 5) / 17.5f;
                    mWindVelY = ((gc.NextRandom(0, 1) != 0) ? randVel : -randVel) * gc.Fps;
			        duration = 40.0;
			        break;
		        }
		        case WeatherState.Clearing:
			        break;
		        case WeatherState.Cloudy:
			        if (mClouds == null)
				        mClouds = new List<Cloud>(mMaxClouds);
			        mCloudDensity = Math.Max(1, mMaxClouds / 2);
			        duration = 180.0;
			        break;
		        case WeatherState.Overcast:
			        if (mClouds == null)
				        mClouds = new List<Cloud>(mMaxClouds);
			        mCloudDensity = mMaxClouds;
			        duration = 180.0;
			        break;
	        }
	        mState = state;
	
	        if (state != WeatherState.Clearing)
                BeginCycle(duration + gc.NextRandom(0, 60)); // Add some randomness to cycle durations
	
	        //NSLog(@"Weather Changed to State: %d", state);
        }

        public void BeginCycle(double duration)
        {
            if (!mCycleComplete)
                mScene.Juggler.RemoveTweensWithTarget(this);
	        mCycleComplete = false;
            mCycleTimer = duration;
        }

        private void SetCycleComplete()
        {
            mCycleComplete = true;
        }

        private void ThinkWeather()
        {
            if (mClouds.Count == 0 && mState == WeatherState.Clearing)
            {
                SetState(WeatherState.Clear);
	        }
            else if (mState >= WeatherState.Cloudy && mClouds.Count < mCloudDensity)
            {
		        SpawnCloud();
	        }
	
	        if (mCycleComplete && mState != WeatherState.Clearing)
            {
                WeatherState rndState = (WeatherState)GameController.GC.NextRandom((int)WeatherState.Clearing, (int)WeatherState.Overcast);
		
		        if (rndState == mState)
                {
			        if (rndState < WeatherState.Overcast)
				        ++rndState;
			        else
				        --rndState;
		        }
                SetState(rndState);
	        }
        }

        private void SpawnCloud()
        {
            if (mClouds.Count >= mMaxClouds || mCloudSpawnTimer > 0 || !mWeatherEnabled)
		        return;
	        TimeKeeper timeKeeper = GameController.GC.TimeKeeper;
	        Cloud cloud = new Cloud(Cloud.RandomCloudType, mWindVelX, mWindVelY, mCloudAlpha);
	        cloud.ShadowOffsetX = timeKeeper.ShadowOffsetX;
	        cloud.ShadowOffsetY = timeKeeper.ShadowOffsetY;
            cloud.SetupCloud();
            mClouds.Add(cloud);
            mCloudSpawnTimer = GameController.GC.NextRandom(2, 10); // Distribute clouds between 2 and 8 seconds apart.
        }

        public void ClearUpSky()
        {
            if (mState != WeatherState.Clearing)
                SetState(WeatherState.Clearing);
        }

        public override void AdvanceTime(double time)
        {
            TimeKeeper timeKeeper = GameController.GC.TimeKeeper;
    
            if (mCycleTimer > 0.0)
            {
                mCycleTimer -= time;
        
                if (mCycleTimer <= 0.0)
                    SetCycleComplete();
            }
	
	        for (int i = mClouds.Count - 1; i >= 0; --i)
            {
		        Cloud cloud = mClouds[i];
		        cloud.ShadowOffsetX = timeKeeper.ShadowOffsetX;
		        cloud.ShadowOffsetY = timeKeeper.ShadowOffsetY;
                cloud.AdvanceTime(time);

                if (cloud.IsBlownOffscreen)
                {
                    cloud.Dispose();
                    mClouds.RemoveAt(i);
                }
	        }
	
            ThinkWeather();
	
	        if (mCloudSpawnTimer > 0)
		        mCloudSpawnTimer -= time;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mScene.Juggler.RemoveTweensWithTarget(this);

                        if (mClouds != null)
                        {
                            foreach (Cloud cloud in mClouds)
                                cloud.Dispose();
                            mClouds = null;
                        }
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion
    }
}
