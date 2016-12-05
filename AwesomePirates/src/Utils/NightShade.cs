using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class NightShade
    {
        private const int kShadeInterval = 0x10101;

        public NightShade(List<SPImage> shaders)
        {
            mShade = 0xff;
            mTweener = new IntTweener(mShade, SPTransitions.SPLinear);
            mColor = new Color(mShade, mShade, mShade);
            mShaders = (shaders != null) ? new List<SPImage>(shaders) : new List<SPImage>();
            //mJuggler = new SPJuggler();
        }

        private int mShade;
        private IntTweener mTweener;

        private Color mColor;
        private List<SPImage> mShaders;
        //private SPJuggler mJuggler;

        public int Shade
        {
            get { return mShade; }
            set
            {
                mShade = value;

                int shadeColor = mShade * kShadeInterval;
                mColor.R = (byte)SPMacros.SP_COLOR_PART_RED(shadeColor);
                mColor.G = (byte)SPMacros.SP_COLOR_PART_GREEN(shadeColor);
                mColor.B = (byte)SPMacros.SP_COLOR_PART_BLUE(shadeColor);

                foreach (SPImage image in mShaders)
                    image.Color = mColor;
            }
        }

        public void AddShader(SPImage shader)
        {
            mShaders.Add(shader);
        }

        public void RemoveShader(SPImage shader)
        {
            mShaders.Remove(shader);
        }

        public void AdvanceTime(double time)
        {
            mTweener.AdvanceTime(time);
            if (Shade != mTweener.TweenedValue)
                Shade = mTweener.TweenedValue;

            //mJuggler.AdvanceTime(time);
        }

        public void TransitionTimeOfDay(TimeOfDay timeOfDay, float transitionDuration, float proportionRemaining)
        {
            //mJuggler.RemoveTweensWithTarget(this);

            bool transition = false;
            int colorFrom = 0xffffff, colorTo = 0xffffff;

            switch (timeOfDay)
            {
                case TimeOfDay.DuskTransition:
                    transition = true;
                    colorFrom = 0xffffff;
                    colorTo = 0xe0e0e0;
                    break;
                case TimeOfDay.Dusk:
                    colorFrom = 0xe0e0e0;
                    colorTo = 0xe0e0e0;
                    break;
                case TimeOfDay.EveningTransition:
                    transition = true;
                    colorFrom = 0xe0e0e0;
                    colorTo = 0x808080;
                    break;
                case TimeOfDay.Evening:
                case TimeOfDay.Midnight:
                    colorFrom = 0x808080;
                    colorTo = 0x808080;
                    break;
                case TimeOfDay.DawnTransition:
                    transition = true;
                    colorFrom = 0x808080;
                    colorTo = 0xe0e0e0;
                    break;
                case TimeOfDay.Dawn:
                    colorFrom = 0xe0e0e0;
                    colorTo = 0xe0e0e0;
                    break;
                case TimeOfDay.SunriseTransition:
                    transition = true;
                    colorFrom = 0xe0e0e0;
                    colorTo = 0xffffff;
                    break;
                case TimeOfDay.NewGameTransition:
                    transition = true;
                    colorFrom = Shade * kShadeInterval;
                    colorTo = 0xe0e0e0;
                    break;
                default:
                    break;
            }

            if (!transition)
            {
                mTweener.Reset(colorTo / kShadeInterval);
                Shade = mTweener.TweenedValue;
            }
            else
            {
                int colorRange = colorTo - colorFrom;
                colorFrom += (int)((1f - proportionRemaining) * colorRange);
                mTweener.Reset(colorFrom / kShadeInterval, colorTo / kShadeInterval, transitionDuration);
                Shade = mTweener.TweenedValue;
                //SPTween tween = new SPTween(this, transitionDuration);
                //tween.AnimateProperty("Shade", colorTo / kShadeInternal);
                //mJuggler.AddObject(tween);
            }
        }

        public void DestroyNightShade()
        {
            //mJuggler.RemoveAllObjects();
            mTweener.Reset(Shade);
        }
    }
}
