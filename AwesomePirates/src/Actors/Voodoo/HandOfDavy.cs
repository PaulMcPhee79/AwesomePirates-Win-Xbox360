using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class HandOfDavy : Prop, IPursuer, IReusable
    {
        private const uint kHodReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 6;
            double duration = Idol.DurationForIdol(new Idol(Idol.VOODOO_SPELL_HAND_OF_DAVY));
            uint reuseKey = kHodReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetHandOfDavy(duration);
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

        public static HandOfDavy GetHandOfDavy(double duration)
        {
            HandOfDavy hod = CheckoutReusable(kHodReuseKey) as HandOfDavy;

            if (hod != null)
            {
                hod.Duration = duration;
                hod.Reuse();
            }
            else
            {
                hod = new HandOfDavy(duration);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed HandOfDavy ReusableCache.");
#endif
            }

            return hod;
        }

        public const string CUST_EVENT_TYPE_HAND_OF_DAVY_DISMISSED = "handOfDavyDismissedEvent";

        private enum DavyState
        {
            Idle = 0,
            Emerging,
            Submerging,
            Dying,
            Dead
        }

        public HandOfDavy(double duration)
            : base(PFCat.POINT_MOVIES)
        {
            mAdvanceable = true;
            mInUse = true;
            mPoolIndex = -1;
            mDuration = duration;
            mRequestTargetTimer = 0.0;
            mSubmergedDelay = -1;
            SetupProp();
            SetState(DavyState.Idle);
        }
        
        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private DavyState mState;
        private double mSubmergedDelay;
        private double mDuration;
        private double mRequestTargetTimer;
        private NpcShip mTarget;
        private SPMovieClip mEmergeClip;
        private SPMovieClip mSubmergeClip;
        private SPSprite mCostume;
        private SPTween mSubmergeTween;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kHodReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        protected double Duration { get { return mDuration; } set { mDuration = value; } }
        public NpcShip Target
        {
            get { return mTarget; }
            set
            {
                if (mTarget == value)
		            return;
	            if (mTarget != null)
                {
                    mTarget.RemovePursuer(this);
                    mTarget = null;
	            }
	
	            if (value != null)
                {
		            mTarget = value;
                    mTarget.AddPursuer(this);
	            }
            }
        }
        public SKTeamIndex OwnerID { get; set; }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume == null)
            {
                mCostume = new SPSprite();
                mCostume.ScaleX = mCostume.ScaleY = 1.35f;
                AddChild(mCostume);
            }

            if (mEmergeClip == null)
            {
                mEmergeClip = new SPMovieClip(mScene.TexturesStartingWith("death-grip-emerge_"), 8);
                mEmergeClip.SetDurationAtIndex(0.25f, mEmergeClip.NumFrames - 1);
                mEmergeClip.X = -mEmergeClip.Width / 2;
                mEmergeClip.Y = -mEmergeClip.Height / 2;
                mEmergeClip.Loop = false;
                mEmergeClip.AddActionEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, new Action<SPEvent>(OnTargetGrabbed));
                mCostume.AddChild(mEmergeClip);
            }

            mEmergeClip.Pause();
            mScene.Juggler.AddObject(mEmergeClip);
            
            if (mSubmergeClip == null)
            {
                mSubmergeClip = new SPMovieClip(mScene.TexturesStartingWith("death-grip-submerge_"), 8);
                mSubmergeClip.SetDurationAtIndex(0.25f, 0);
                mSubmergeClip.SetDurationAtIndex(0.5f, mSubmergeClip.NumFrames - 1);
                mSubmergeClip.X = -mSubmergeClip.Width / 2;
                mSubmergeClip.Y = -mSubmergeClip.Height / 2;
                mSubmergeClip.Loop = false;
                mSubmergeClip.AddActionEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, new Action<SPEvent>(OnTargetSubmerged));
                mCostume.AddChild(mSubmergeClip);
            }

            mSubmergeClip.Pause();
            mScene.Juggler.AddObject(mSubmergeClip);

            if (mSubmergeTween == null)
            {
                float tweenDuration = (float)mSubmergeClip.DurationAtIndex(mSubmergeClip.NumFrames - 1);
                mSubmergeTween = new SPTween(mSubmergeClip, tweenDuration);
                mSubmergeTween.AnimateProperty("Alpha", 0.01f);
                mSubmergeTween.Delay = mSubmergeClip.Duration - tweenDuration;
            }
    
            if (mScene.Flipped)
                ScaleX = -1;
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRequestTargetTimer = 0.0;
            mSubmergedDelay = -1;
            SetupProp();
            SetState(DavyState.Idle);

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mEmergeClip != null)
                mScene.Juggler.RemoveObject(mEmergeClip);

            if (mSubmergeClip != null)
                mScene.Juggler.RemoveObject(mSubmergeClip);

            if (mSubmergeTween != null)
                mScene.Juggler.RemoveObject(mSubmergeTween);

            mInUse = false;
            CheckinReusable(this);
        }

        private void SetState(DavyState state)
        {
            switch (state)
            {
		        case DavyState.Idle:
			        Visible = false;
			        Target = null;
			        break;
		        case DavyState.Emerging:
			        Visible = true;
                    mSubmergeClip.Pause();
			        mSubmergeClip.Visible = false;
			
			        mEmergeClip.CurrentFrame = 0;
                    mEmergeClip.Play();
                    mEmergeClip.Visible = true;
			        break;
		        case DavyState.Submerging:
			        Visible = true;
                    mEmergeClip.Pause();
                    mEmergeClip.Visible = false;

                    mSubmergeClip.Alpha = 1;
                    mSubmergeClip.CurrentFrame = 0;
                    mSubmergeClip.Play();
                    mSubmergeClip.Visible = true;
			        break;
		        case DavyState.Dying:
			        break;
		        case DavyState.Dead:
			        Visible = false;
			        Target = null;
            
                    if (TurnID == GameController.GC.ThisTurn.TurnID && mScene.GameMode == GameMode.Career)
                        mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_VOODOO_GADGET_EXPIRED, Idol.VOODOO_SPELL_HAND_OF_DAVY);
                    DispatchEvent(SPEvent.SPEventWithType(HandOfDavy.CUST_EVENT_TYPE_HAND_OF_DAVY_DISMISSED));
			        break;
	        }
	        mState = state;
        }

        public override void Flip(bool enable)
        {
            ScaleX = (enable) ? -1 : 1;
        }

        public void PursueeDestroyed(ShipActor pursuee)
        {
            if (pursuee != Target)
                throw new ArgumentException("HandOfDavy pursuee/target mismatch.");
            Target = null;
        }

        public override void AdvanceTime(double time)
        {
            if (mDuration > 0.0)
            {
                mDuration -= time;
        
                if (mDuration <= 0.0)
                    Despawn();
            }
    
	        if (mState != DavyState.Idle)
		        return;
	        if (mSubmergedDelay >= 0)
		        mSubmergedDelay -= time;
	        if (mSubmergedDelay < 0)
            {
                if (mTarget == null)
                {
                    mRequestTargetTimer -= time;
                    if (mRequestTargetTimer <= 0)
                    {
                        mScene.RequestTargetForPursuer(this);

                        // Requesting an enemy target is expensive, so don't do it at 60fps.
                        if (Target == null)
                            mRequestTargetTimer = 0.25;
                    }
                }

		        if (mTarget != null)
                    GrabTarget();
	        }
        }

        public void Despawn()
        {
            if (mState == DavyState.Dead)
		        return;
	        else if (mState == DavyState.Idle)
		        SetState(DavyState.Dead);
	        else
                SetState(DavyState.Dying);
        }

        private void GrabTarget()
        {
            if (mState != DavyState.Idle || mTarget == null)
		        return;
	        mTarget.InDeathsHands = true;
	        X = mTarget.X;
	        Y = mTarget.Y;
            mScene.PlaySound("HandOfDavy");
            SetState(DavyState.Emerging);
        }

        private void SubmergeTarget()
        {
            DavyState oldState = mState;
            SetState(DavyState.Submerging);
	
            mSubmergeTween.Reset();
            mScene.Juggler.AddObject(mSubmergeTween);

	        if (oldState != DavyState.Emerging)
                SetState(oldState);
        }

        private void OnTargetGrabbed(SPEvent ev)
        {
            if (mTarget != null)
            {
                mTarget.DeathBitmap = DeathBitmaps.HAND_OF_DAVY;
                mTarget.SinkerID = OwnerID;
                mTarget.Visible = false;
                mTarget.Sink(); // Will destroy our pursuee, making mTarget nil
            }

            SubmergeTarget();
        }

        private void OnTargetSubmerged(SPEvent ev)
        {
            if (mState == DavyState.Submerging)
                SetState(DavyState.Idle);
	        else if (mState == DavyState.Dying)
                SetState(DavyState.Dead);
            if (mEmergeClip != null && mSubmergeClip != null)
	            mSubmergedDelay = 2.7f - (mEmergeClip.Duration + mSubmergeClip.Duration); // 2.7f is the duration of the sound effect.
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mEmergeClip != null)
                        {
                            mEmergeClip.RemoveEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED);
                            mScene.Juggler.RemoveObject(mEmergeClip);
                            mEmergeClip = null;
                        }

                        if (mSubmergeClip != null)
                        {
                            mSubmergeClip.RemoveEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED);
                            mScene.Juggler.RemoveObject(mSubmergeClip);
                            mSubmergeClip = null;
                        }

                        if (mSubmergeTween != null)
                        {
                            mScene.Juggler.RemoveObject(mSubmergeTween);
                            mSubmergeTween = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore
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
