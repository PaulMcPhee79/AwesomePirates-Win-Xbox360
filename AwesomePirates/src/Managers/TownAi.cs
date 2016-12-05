using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class TownAi : IDisposable
    {
        private const double kTownAiThinkInterval = 0.5;
        private const double kTownShotInterval = 20.0;

        public TownAi(PlayfieldController scene)
        {
            mScene = scene;
            mSuspendedMode = false;
		    mAiModifier = 1.0f;
		    mTimeSinceLastShot = kTownShotInterval;
		    mShotQueue = 0;
		    mCannons = new List<TownCannon>();
            mSortedCannons = new List<TownCannon>();
		    mTargets = new List<ShipActor>();
		    mTracers = new List<TargetTracer>();
        
            mThinkTimer = kTownAiThinkInterval;
            mThinking = false;
        }
        
        #region Fields
        protected bool mIsDisposed = false;
        private bool mSuspendedMode;
        private float mAiModifier;
        private int mShotQueue;

        private bool mThinking;
        private double mThinkTimer;
        private double mTimeSinceLastShot;

        private List<TownCannon> mCannons;
        private List<TownCannon> mSortedCannons;
        private List<ShipActor> mTargets;
        private List<TargetTracer> mTracers;
        private PlayfieldController mScene;
        #endregion

        #region Properties
        public float AiModifier
        {
            get { return mAiModifier; }
            set
            {
                mAiModifier = value;

                if (mCannons != null)
                {
                    foreach (TownCannon cannon in mCannons)
                        cannon.AiModifier = value;
                }
            }
        }
        public double TimeSinceLastShot { get { return mTimeSinceLastShot; } set { mTimeSinceLastShot = value; } }
        #endregion

        #region Methods
        public void OnAiModifierChanged(NumericValueChangedEvent ev)
        {
            AiModifier = ev.FloatValue;
        }

        public void AddCannon(TownCannon cannon)
        {
            if (cannon != null && mCannons != null && !mCannons.Contains(cannon))
            {
                mCannons.Add(cannon);
                cannon.AiModifier = AiModifier;
            }
        }

        public void AddTarget(ShipActor target)
        {
            if (target != null && mTargets != null && !mTargets.Contains(target))
            {
                mTargets.Add(target);

                TargetTracer tracer = new TargetTracer();
                tracer.Target = target;
                mTracers.Add(tracer);
            }
        }

        public void RemoveTarget(ShipActor target)
        {
            if (target == null)
                return;

            int index = mTargets.IndexOf(target);

            if (index != -1 && index < mTracers.Count)
            {
                mTargets.Remove(target);
                mTracers.RemoveAt(index);
            }
        }

        public void EnableSuspendedMode(bool enable)
        {
            StopThinking();

            if (!enable)
                Think();
            mSuspendedMode = enable;
        }

        public void Think()
        {
            mThinking = true;
        }

        private void Think(double time)
        {
            if (!mThinking)
                return;
    
            mThinkTimer -= time;
    
            if (mThinkTimer <= 0)
            {
                mThinkTimer = kTownAiThinkInterval;
                ThinkCannons();
    
                if (mTimeSinceLastShot >= kTownAiThinkInterval)
                    mTimeSinceLastShot -= kTownAiThinkInterval;
            }
        }

        public void StopThinking()
        {
            mThinking = false;
        }

        public void PrepareForNewGame()
        {
            mTimeSinceLastShot = kTownShotInterval;
            Think();
        }

        public void PrepareForGameOver()
        {
            if (mTargets != null)
                mTargets.Clear();
            if (mTracers != null)
                mTracers.Clear();
            StopThinking();
        }

        private void ThinkCannons()
        {
            if (mCannons == null || mTargets == null || mTracers == null)
                return;

            bool done = (mTimeSinceLastShot > kTownAiThinkInterval);
	        float x,y,dist;

            foreach (TownCannon cannon in mCannons)
                mSortedCannons.Add(cannon);

            mSortedCannons.Sort(TownCannon.ShotQueueCompare);
	
	        int shipIndex = 0;
	
	        for (int i = mTargets.Count - 1; i >= 0; --i)
            {
		        ShipActor ship = mTargets[i];
		        if (ship is PlayerShip)
                {
			        PlayerShip playerShip = ship as PlayerShip;
			
			        if (playerShip.IsCamouflaged)
				        continue;
		        }

                if (done || mSortedCannons.Count == 0)
			        break;

                for (int j = mSortedCannons.Count - 1; j >= 0; --j)
                {
                    TownCannon cannon = mSortedCannons[j];
	
			        x = ship.PX - cannon.X;
			        y = ship.PY - cannon.Y;
			        dist = new Vector2(x, y).LengthSquared();
			
			        if (dist < cannon.RangeSquared)
                    {
				        if (cannon.AimAt(ship.PX, ship.PY))
                        {
					        if (shipIndex < mTracers.Count) // Could be false if removeTarget occurred between loop start and here
                            {
						        TargetTracer tracer = mTracers[shipIndex];

						        if (cannon.Fire(tracer.TargetVel))
                                {
							        cannon.ShotQueue = ++mShotQueue;
							        mTimeSinceLastShot = kTownShotInterval;
						        }
					        }
                            mSortedCannons.RemoveAt(j);
                            done = true;
					        break;
				        }
			        }
		        }
		        ++shipIndex;
	        }

            foreach (TownCannon cannon in mSortedCannons)
                cannon.Idle();
            mSortedCannons.Clear();
        }

        public void AdvanceTime(double time)
        {
            foreach (TargetTracer tracer in mTracers)
                tracer.AdvanceTime(time);
            Think(time);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                if (disposing)
                {
                    StopThinking();
                    mCannons = null;
                    mSortedCannons = null;
                    mTargets = null;
                    mTracers = null;
                    mScene = null;
                }

                mIsDisposed = true;
            }
        }

        ~TownAi()
        {
            Dispose(false);
        }
        #endregion
    }
}
