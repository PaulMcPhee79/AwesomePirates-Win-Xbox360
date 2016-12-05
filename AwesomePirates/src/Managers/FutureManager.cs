using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class FutureManager : Prop
    {
        private const float kElectricityDuration = 1.0f;
        private const float kFlamePathDuration = 5.0f;
        private const float kExtinguishDuration = 2.0f;
        private const int kFlamePathLength = 10;

        public FutureManager()
            : base(-1)
        {
            mSparkTarget = null;
            mFlamePathsClips = new List<SPMovieClip>(kFlamePathLength * 2);
            SetupProp();
        }

        #region Fields
        private SPSprite mSparkTarget;
        private Prop mElectricityProp;
        private Prop mFlamePathsProp;
        private List<SPMovieClip> mFlamePathsClips;
        #endregion

        #region Properties
        public static float ElectricityDuration { get { return kElectricityDuration; } }
        public static float FlamePathDuration { get { return kFlamePathDuration; } }
        public static float FlamePathExtinguishDuration { get { return kExtinguishDuration; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            // Electricity
	        mElectricityProp = new Prop(PFCat.EXPLOSIONS);
	        SPImage image = new SPImage(mScene.TextureByName("energy-ball"));
	        image.X = -image.Width / 2;
	        image.Y = -image.Height / 2;
            mElectricityProp.AddChild(image);
	
	        // Flame Paths
	        mFlamePathsProp = new Prop(PFCat.WAVES);

	        SPMovieClip baseClip = new SPMovieClip(mScene.TexturesStartingWith("brandy-flame_"), 8);
	        float yOffset = -baseClip.Height;
	        baseClip.X = -baseClip.Width - 10.0f;
	        baseClip.Y = yOffset;
            mScene.Juggler.AddObject(baseClip);
            mFlamePathsClips.Add(baseClip);
            mFlamePathsProp.AddChild(baseClip);
	
	        for (int i = 1; i < kFlamePathLength * 2; ++i)
            {
		        SPMovieClip clip = new SPMovieClip(baseClip.FrameAtIndex(0), 8);
		        clip.X = ((i & 1) != 0) ? 10.0f : -clip.Width - 10.0f;
		        clip.Y = yOffset;
		
		        for (int j = 1; j < baseClip.NumFrames; ++j)
                    clip.AddFrame(baseClip.FrameAtIndex(j));

                mScene.Juggler.AddObject(clip);
                mFlamePathsClips.Add(clip);
                mFlamePathsProp.AddChildAtIndex(clip, 0);
		
		        if ((i & 1) != 0)
			        yOffset -= 16.0f;
	        }
        }

        private void SparkElectricity()
        {
            mElectricityProp.ScaleX = 1.0f;
            mElectricityProp.ScaleY = 1.0f;
            mElectricityProp.ScaleX = 1.0f;
            mElectricityProp.ScaleY = 1.0f;

            mScene.Juggler.DelayInvocation(this, 0.25, delegate { OrientElectricCharge(-1, 1); });
            mScene.Juggler.DelayInvocation(this, 0.5, delegate { OrientElectricCharge(-1, -1); });
            mScene.Juggler.DelayInvocation(this, 0.75, delegate { OrientElectricCharge(1, -1); });
            PlayElectricitySound();
        }

        public void SparkElectricityAt(float x, float y)
        {
            mElectricityProp.X = x;
	        mElectricityProp.Y = y;
            SparkElectricity();
            mScene.AddProp(mElectricityProp);
            mScene.Juggler.DelayInvocation(this, kElectricityDuration, delegate { HideElectricity(); });
        }

        public void SparkElectricityOnSprite(SPSprite sprite)
        {
            if (mSparkTarget != null || sprite == null)
                return;
            mSparkTarget = sprite;
	        mElectricityProp.X = 0;
	        mElectricityProp.Y = 0;
            SparkElectricity();
            sprite.AddChild(mElectricityProp);
            mScene.Juggler.DelayInvocation(this, kElectricityDuration, delegate { HideElectricity(); });
        }

        private void HideElectricity()
        {
            if (mElectricityProp != null)
            {
                if (mSparkTarget != null)
                {
                    mSparkTarget.RemoveChild(mElectricityProp);
                    mSparkTarget = null;
                }

                mScene.RemoveProp(mElectricityProp, false);
            }
        }

        public void IgniteFlamePathsAtSprite(SPSprite sprite)
        {
            int i = 0;
	        float delay = 0.0f;
	
	        foreach (SPMovieClip clip in mFlamePathsClips)
            {
		        clip.Alpha = 0.0f;
		
		        SPTween tween = new SPTween(clip, 0.05f);
                tween.AnimateProperty("Alpha", 1);
                tween.Delay = delay;
                mScene.Juggler.AddObject(tween);
		
		        if ((i & 1) != 0)
			        delay += (float)tween.TotalTime;
		        ++i;
	        }
	
	        mFlamePathsProp.X = sprite.X;
	        mFlamePathsProp.Y = sprite.Y;
	        mFlamePathsProp.Rotation = sprite.Rotation;
            mScene.AddProp(mFlamePathsProp);
            PlayFlamePathSound();
	
	        SPTween containerTween = new SPTween(mFlamePathsProp, kExtinguishDuration);
            containerTween.AnimateProperty("Alpha", 0);
            containerTween.Delay = kFlamePathDuration;
            containerTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_STARTED, (SPEventHandler)OnFlamePathExtinguishing);
            containerTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnFlamePathExtinguished);
            mScene.Juggler.AddObject(containerTween);
        }

        private void OnFlamePathExtinguishing(SPEvent ev)
        {
            StopFlamePathSound();
        }

        private void OnFlamePathExtinguished(SPEvent ev)
        {
            if (mFlamePathsClips != null)
            {
                foreach (SPMovieClip clip in mFlamePathsClips)
                {
                    mScene.Juggler.RemoveTweensWithTarget(clip);
                    mScene.Juggler.RemoveObject(clip);
                }

                mFlamePathsClips = null;
            }

            mScene.RemoveProp(mFlamePathsProp);
            mFlamePathsProp = null;
        }

        public void OrientElectricCharge(float scaleX, float scaleY)
        {
            mElectricityProp.ScaleX = scaleX;
            mElectricityProp.ScaleY = scaleY;
        }

        private void PlayElectricitySound()
        {
            mScene.PlaySound("Electricity");
        }

        private void PlayFlamePathSound()
        {
            mScene.PlaySound("Fire");
        }

        private void StopFlamePathSound()
        {
            mScene.StopSound("Fire");
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        StopFlamePathSound();
                        mScene.Juggler.RemoveTweensWithTarget(this);

                        if (mFlamePathsClips != null)
                        {
                            foreach (SPMovieClip clip in mFlamePathsClips)
                            {
                                mScene.Juggler.RemoveTweensWithTarget(clip);
                                mScene.Juggler.RemoveObject(clip);
                            }

                            mFlamePathsClips = null;
                        }

                        if (mFlamePathsProp != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mFlamePathsProp);
                            mScene.RemoveProp(mFlamePathsProp);
                            mFlamePathsProp = null;
                        }

                        HideElectricity();
                        mSparkTarget = null;
                        mElectricityProp = null;
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
