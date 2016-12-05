using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class Wave : Prop
    {
        private enum WaveSurfaceState
        {
            Normal = 0,
            TransitionToNight,
            TransitionToDay
        }

        public Wave(SPTexture texture, float initAlpha, float target, float rate)
            : base(PFCat.WAVES)
        {
            mSurface = new SPQuad(texture);
            mSurfaceContainer = new SPSprite();
            mSurfaceContainer.AddChild(mSurface);
            AddChild(mSurfaceContainer);

            mSurface.Width = mScene.ViewWidth;
            mSurface.Height = mScene.ViewHeight;

            mXRepeat = mSurface.Width / texture.Width;
            mYRepeat = mSurface.Height / texture.Height;
            mSurface.SetTexCoord(new Vector2(mXRepeat, 0), 1);
            mSurface.SetTexCoord(new Vector2(0, mYRepeat), 2);
            mSurface.SetTexCoord(new Vector2(mXRepeat, mYRepeat), 3);
            mSurface.Alpha = initAlpha;

            mAlphaMin = Math.Min(initAlpha, target);
            mAlphaMax = Math.Max(initAlpha, target);
            mAlphaMid = mAlphaMin + (mAlphaMax - mAlphaMin) / 2;
            mAlphaRate = rate;

            mFlowX = 0f;
            mFlowY = 0f;

            SPTween tween = AnimateProperty("Alpha", mSurface, target, mAlphaRate, SPLoopType.Reverse, true);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new SPEventHandler(OnSurfaceTweenCompleted));

            SetState(WaveSurfaceState.Normal);
        }

        #region Fields

        private WaveSurfaceState mState;
        private float mAlphaMin;
        private float mAlphaMid;
        private float mAlphaMax;
        private float mAlphaRate;

        private float mFlowX;
        private float mFlowY;

        private float mXRepeat;
        private float mYRepeat;

        private SPQuad mSurface;
        private SPSprite mSurfaceContainer;

        #endregion

        #region Properties

        public float AlphaMin { get { return mAlphaMin; } }
        public float AlphaMax { get { return mAlphaMax; } }
        public float AlphaRate { get { return mAlphaRate; } }
        public float FlowX { get { return mFlowX; } set { mFlowX = value; AdjustFlow(); } }
        public float FlowY { get { return mFlowY; } set { mFlowY = value; AdjustFlow(); } }
        public SPQuad Surface { get { return mSurface; } }
        public float Orientation { set { Rotation = value; } }

        #endregion

        #region Methods

        private void SetState(WaveSurfaceState state)
        {
            mState = state;
        }

        private void AdjustFlow()
        {
            float nearX = mXRepeat - mFlowX * mXRepeat;
            float farX = nearX + mXRepeat;
            float nearY = mYRepeat - mFlowY * mYRepeat;
            float farY = nearY + mYRepeat;

            mSurface.SetTexCoord(new Vector2(nearX, nearY), 0);
            mSurface.SetTexCoord(new Vector2(farX, nearY), 1);
            mSurface.SetTexCoord(new Vector2(nearX, farY), 2);
            mSurface.SetTexCoord(new Vector2(farX, farY), 3);
        }

        public void FlowXOverTime(float duration)
        {
            AnimateProperty("FlowX", this, mSurface.Texture.Width / mSurface.Width, duration, SPLoopType.Repeat, false);
        }

        public void FlowYOverTime(float duration)
        {
            AnimateProperty("FlowY", this, mSurface.Texture.Height / mSurface.Height, duration, SPLoopType.Repeat, false);
        }

        private SPTween AnimateProperty(string property, SPDisplayObject displayObject, float targetValue, float duration, SPLoopType loop, bool exclusive)
        {
            if (exclusive)
                mScene.Juggler.RemoveTweensWithTarget(displayObject);

            SPTween tween = new SPTween(displayObject, duration);
            tween.AnimateProperty(property, targetValue);
            tween.Loop = loop;
            mScene.Juggler.AddObject(tween);
            return tween;
        }

        public void OnTimeOfDayChanged(TimeOfDayChangedEvent ev)
        {
            if (ev.TimeOfDay == TimeOfDay.NewGameTransition)
                SetState(WaveSurfaceState.TransitionToDay);
            else if (ev.TimeOfDay == TimeOfDay.DawnTransition)
            {
                AnimateProperty("Alpha", mSurfaceContainer, 1f, ev.PeriodDuration, SPLoopType.None, true);
                SetState(WaveSurfaceState.TransitionToDay);
            }
            else if (ev.TimeOfDay == TimeOfDay.SunriseTransition)
                AnimateProperty("Alpha", mSurfaceContainer, 0.6f, ev.PeriodDuration, SPLoopType.None, true);
            else if (ev.TimeOfDay == TimeOfDay.NoonTransition)
                AnimateProperty("Alpha", mSurfaceContainer, 1f, ev.PeriodDuration, SPLoopType.None, true);
            else if (ev.TimeOfDay == TimeOfDay.SunsetTransition)
                AnimateProperty("Alpha", mSurfaceContainer, 0.6f, ev.PeriodDuration, SPLoopType.None, true);
            else if (ev.TimeOfDay == TimeOfDay.DuskTransition)
                AnimateProperty("Alpha", mSurfaceContainer, 0.8f, ev.PeriodDuration, SPLoopType.None, true);
            else if (ev.TimeOfDay == TimeOfDay.EveningTransition)
            {
                SetState(WaveSurfaceState.TransitionToNight);
            }
        }

        private void OnSurfaceTweenCompleted(SPEvent ev)
        {
            switch (mState)
            {
                case WaveSurfaceState.TransitionToNight:
                {
                    if (mSurface.Alpha < mAlphaMid)
                    {
                        // Reduce range between min & max alpha and slow tween rate at night to minimize "flashing"
                        float target = 1.1f * mAlphaMid;
                        SPTween tween = AnimateProperty("Alpha", mSurface, target, 2 * mAlphaRate, SPLoopType.Reverse, true);
                        tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new SPEventHandler(OnSurfaceTweenCompleted));
                        SetState(WaveSurfaceState.Normal);
                    }
                }
                    break;
                case WaveSurfaceState.TransitionToDay:
                {
                    // WaveSurfaceState.TransitionToNight targets 1.1f * mAlphaMid so that this doesn't pass erroneously
                    if (mSurface.Alpha < mAlphaMid)
                    {
                        float target = mAlphaMax;
                        SPTween tween = AnimateProperty("Alpha", mSurface, target, mAlphaRate, SPLoopType.Reverse, true);
                        tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new SPEventHandler(OnSurfaceTweenCompleted));
                        SetState(WaveSurfaceState.Normal);
                    }
                }
                    break;
                case WaveSurfaceState.Normal:
                    break;
            }
        }

        #endregion
    }
}
