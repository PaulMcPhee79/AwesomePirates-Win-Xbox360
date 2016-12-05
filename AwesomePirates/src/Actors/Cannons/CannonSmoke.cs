using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class CannonSmoke : Prop
    {
        public CannonSmoke(float x, float y)
            : base(-1)
        {
            X = x; Y = y;
            mTweening = false;
            mSmokeTween = null;
            SetupProp();
        }

        #region Fields
        private bool mTweening;
        private SPSprite mBurstFrame;
        private SPMovieClip mBurst;
        private SPSprite mSmokeFrame;
        private SPMovieClip mSmoke;
        private SPTween mSmokeTween;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            mBurst = new SPMovieClip(mScene.TexturesStartingWith("cannon-burst-smoke_"), 12);
	        mBurst.X = -mBurst.Width / 2;
	        mBurst.Y = -mBurst.Height / 2;
	        mBurst.Loop = false;
	
	        mBurstFrame = new SPSprite();
	        mBurstFrame.X = mBurst.Width / 2;
            mBurstFrame.AddChild(mBurst);
            AddChild(mBurstFrame);
            mBurst.AddEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)OnBurstClipCompleted);
	
	        mSmoke = new SPMovieClip(mScene.TexturesStartingWith("cannon-smoke_"), 12);
	        mSmoke.X = -mSmoke.Width / 2;
	        mSmoke.Y = -mSmoke.Height / 2;
	        mSmoke.Loop = false;
	
	        mSmokeFrame = new SPSprite();
	        mSmokeFrame.X = mSmoke.Width / 2;
            mSmokeFrame.AddChild(mSmoke);
            AddChild(mSmokeFrame);
            mSmoke.AddEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)OnSmokeClipCompleted);
	
	        mBurstFrame.Visible = false;
            mSmokeFrame.Visible = false;
        }

        public void StartWithAngle(float angle)
        {
            mSmokeFrame.Rotation = -angle; // Keep smoke floating skyward
	        mSmokeFrame.Visible = false;
	        mBurstFrame.Visible = true;
	        mBurst.CurrentFrame = 0;
	        mBurst.Play();
            mScene.Juggler.AddObject(mBurst);
        }

        private void OnBurstClipCompleted(SPEvent ev)
        {
            mScene.Juggler.RemoveObject(mBurst);
	
	        mBurstFrame.Visible = false;
	        mSmokeFrame.Visible = true;
	        mSmoke.Y = -mSmoke.Height / 2;
	        mSmoke.Alpha = 1;
	        mSmoke.CurrentFrame = 0;
            mSmoke.Play();
            mScene.Juggler.AddObject(mSmoke);

            if (mSmokeTween == null)
            {
                mSmokeTween = new SPTween(mSmoke, mSmoke.Duration);
                mSmokeTween.AnimateProperty("Alpha", 0f);
                mSmokeTween.AnimateProperty("Y", mSmoke.Y - mSmoke.Height / 2);
            }
            else
                mSmokeTween.Reset();

            if (!mTweening)
            {
                mTweening = true;
                mScene.Juggler.AddObject(mSmokeTween);
            }
        }

        private void OnSmokeClipCompleted(SPEvent ev)
        {
            mScene.Juggler.RemoveObject(mSmoke);
	        mBurstFrame.Visible = false;
	        mSmokeFrame.Visible = false;
            mTweening = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mBurst != null)
                        {
                            mBurst.RemoveEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)OnBurstClipCompleted);
                            mScene.Juggler.RemoveObject(mBurst);
                            mBurst = null;
                        }

                        if (mSmoke != null)
                        {
                            mSmoke.RemoveEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)OnSmokeClipCompleted);
                            mScene.Juggler.RemoveTweensWithTarget(mSmoke);
                            mScene.Juggler.RemoveObject(mSmoke);
                            mSmoke = null;
                        }

                        mBurstFrame = null;
                        mSmokeFrame = null;
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
