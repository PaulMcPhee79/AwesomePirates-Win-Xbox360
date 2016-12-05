using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class WaterFire : Prop, IResourceClient
    {
        public WaterFire(int category, int[] flameCoords, string flameTexName)
            : base(category)
        {
            if (flameCoords == null)
                throw new ArgumentException("Null flameCoords in WaterFire.");

            mInUse = true;
            mIgnited = false;
            mExtinguishing = false;
            mFlameCoords = flameCoords;
            mResourceKey = flameTexName;
            CheckoutPooledResources();
            SetupProp();
        }
        
        #region Fields
        private bool mInUse;

        private bool mIgnited;
        private bool mExtinguishing;
        private int[] mFlameCoords;

        private SPImage mEdgePeg;
        private SPSprite mCanvas;
        private List<SPMovieClip> mFlames;
        private SPTween mExtinguishTween;

        private string mResourceKey;
        private ResourceServer mResources;
        #endregion

        #region Properties
        public bool Ignited { get { return mIgnited; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvas == null)
            {
                mCanvas = new SPSprite();
                AddChild(mCanvas);
            }

            mCanvas.Alpha = 1f;

            int numFlames = mFlameCoords.Length;
            if (mFlames == null)
            {
                mFlames = new List<SPMovieClip>(numFlames);
                List<SPTexture> frames = mScene.TexturesStartingWith(mResourceKey);
                for (int i = 0; i < numFlames / 2; ++i)
                {
                    SPMovieClip clip = new SPMovieClip(frames, 12);
                    clip.X = mFlameCoords[2 * i];
                    clip.Y = mFlameCoords[2 * i + 1];
                    clip.CurrentFrame = i % clip.NumFrames; // Add some variance
                    clip.Pause();
                    mCanvas.AddChild(clip);
                    mFlames.Add(clip);
                    mScene.Juggler.AddObject(clip);
                }
            }
            else
            {
                for (int i = 0; i < numFlames / 2; ++i)
                {
                    SPMovieClip clip = mFlames[i];
                    clip.X = mFlameCoords[2 * i];
                    clip.Y = mFlameCoords[2 * i + 1];
                    clip.CurrentFrame = i % clip.NumFrames; // Add some variance
                    clip.Pause();
                    mCanvas.AddChild(clip);
                    mScene.Juggler.AddObject(clip);
                }
            }

            if (mExtinguishTween == null)
            {
                mExtinguishTween = new SPTween(mCanvas, Globals.VOODOO_DESPAWN_DURATION);
                mExtinguishTween.AnimateProperty("Alpha", 0);
                mExtinguishTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnFlamesExtinguished));
            }
	
            if (mEdgePeg == null)
                mEdgePeg = new SPImage(mScene.TextureByName("clear-texture"));

            mCanvas.AddChild(mEdgePeg);
	        mCanvas.Visible = false;
	        mCanvas.X = -mCanvas.Width / 2;
	        mCanvas.Y = -mCanvas.Height / 2;
        }

        public void Reuse(int[] flameCoords, string flameTexName)
        {
            if (mInUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mIgnited = false;
            mExtinguishing = false;
            mFlameCoords = flameCoords;
            mResourceKey = flameTexName;
            CheckoutPooledResources();
            SetupProp();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!mInUse)
                return;

            if (mIgnited && !mExtinguishing)
                mScene.StopSound("Fire");
            if (mFlames != null)
            {
                foreach (SPMovieClip clip in mFlames)
                    mScene.Juggler.RemoveObject(clip);
                mFlames = null;
            }

            if (mCanvas != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mCanvas);
                mCanvas.RemoveAllChildren();
            }

            CheckinPooledResources();

            mInUse = false;
        }

        public void Ignite()
        {
            if (mIgnited || mExtinguishing)
		        return;
	        mIgnited = true;
	        mCanvas.Visible = true;
	
	        foreach (SPMovieClip clip in mFlames)
		        clip.Play();
            mScene.PlaySound("Fire");
        }

        public void ExtinguishOverTime(float duration)
        {
            mScene.Juggler.RemoveTweensWithTarget(mCanvas);

            if (mExtinguishTween != null && SPMacros.SP_IS_FLOAT_EQUAL(duration, Globals.VOODOO_DESPAWN_DURATION))
            {
                mExtinguishTween.Reset();
                mScene.Juggler.AddObject(mExtinguishTween);
            }
            else
            {
                SPTween tween = new SPTween(mCanvas, duration);
                tween.AnimateProperty("Alpha", 0);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnFlamesExtinguished);
                mScene.Juggler.AddObject(tween);
            }

            if (mIgnited && !mExtinguishing)
                mScene.StopSound("Fire");
            mExtinguishing = true;
        }

        private void OnFlamesExtinguished(SPEvent ev)
        {
            mCanvas.Visible = false;

            foreach (SPMovieClip clip in mFlames)
                clip.Pause();
        }

        public void ResourceEventFiredWithKey(uint key, string type, object target) { }

        public override void CheckoutPooledResources()
        {
            if (mResources == null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_MISC);

                if (cache != null)
                    mResources = cache.CheckoutPoolResourcesForKey(mResourceKey);
            }

            if (mResources == null)
                Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED WATERFIRE MISC CACHE _+_++_+_+_+_+_+_+");
            else
            {
                mResources.Client = this;

                if (mFlames == null)
                    mFlames = mResources.MiscResourceForKey(MiscCache.RESOURCE_KEY_WATERFIRE) as List<SPMovieClip>;
            }
        }

        public override void CheckinPooledResources()
        {
            if (mResources != null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_MISC);

                if (cache != null)
                    cache.CheckinPoolResources(mResources);
                mResources = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mIgnited && !mExtinguishing)
                            mScene.StopSound("Fire");

                        if (mFlames != null)
                        {
                            foreach (SPMovieClip clip in mFlames)
                                mScene.Juggler.RemoveObject(clip);
                            mFlames = null;
                        }

                        if (mCanvas != null)
                        {
                            if (mResources != null)
                                mCanvas.RemoveFromParent();
                            mScene.Juggler.RemoveTweensWithTarget(mCanvas);
                            mCanvas = null;
                        }

                        if (mEdgePeg != null)
                        {
                            mEdgePeg.RemoveFromParent();
                            mEdgePeg.Dispose();
                            mEdgePeg = null;
                        }

                        if (mExtinguishTween != null)
                        {
                            mExtinguishTween.RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mExtinguishTween = null;
                        }

                        CheckinPooledResources();
                        mResourceKey = null;
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
