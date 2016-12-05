using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class TownActor : Actor
    {
        public TownActor(ActorDef def)
            : base(def)
        {
            mAdvanceable = true;
            mCategory = (int)PFCat.BUILDINGS;

            mTweened = null;
            mTweener = new FloatTweener(0f, SPTransitions.SPLinear);
        }

        #region Fields
        private SPSprite mTownSprite;
        private SPSprite mTownFutureSprite;
        private Prop mTownLights;
        private Prop mTownHouseAndCannon;
        private Prop mTownFutureLights;
        private TownCannon mLeftCannon;
        private TownCannon mRightCannon;
        private NightShade mNightShade;

        private SPDisplayObject mTweened;
        private FloatTweener mTweener;
        #endregion

        #region Properties
        public TownCannon LeftCannon { get { return mLeftCannon; } set { mLeftCannon = value; } }
        public TownCannon RightCannon { get { return mRightCannon; } set { mRightCannon = value; } }
        public bool Hidden
        {
            set
            {
                Visible = !value;
                mTownHouseAndCannon.Visible = !value;
                mTownLights.Visible = !value;
                mTownFutureLights.Visible = !value;
            }
        }
        #endregion

        #region Methods
        public void SetupTown()
        {
            mTownSprite = new SPSprite();
            AddChild(mTownSprite);

            // Town
            SPImage townImage = new SPImage(mScene.TextureByName("town"));
            mTownSprite.AddChild(townImage);

            // Town House and Cannon overlay
            mTownHouseAndCannon = new Prop(PFCat.EXPLOSIONS);

            SPImage townHouseImage = new SPImage(mScene.TextureByName("town-house"));
            townHouseImage.X = 0f;
            townHouseImage.Y = 2 * 17f;
            mTownHouseAndCannon.AddChild(townHouseImage);

            SPImage townCannonImage = new SPImage(mScene.TextureByName("town-cannon"));
            townCannonImage.X = 2 * 13f;
            townCannonImage.Y = 2 * 38f;
            mTownHouseAndCannon.AddChild(townCannonImage);
            mScene.AddProp(mTownHouseAndCannon);

            // Lights
            float[] townLightCoords = new [] { 12f, 144f, 22f, 134f, -2f, 70f, 8f, 60f, 124f, 20f };
            mTownLights = new Prop(PFCat.EXPLOSIONS);

            for (int i = 0; i < 5; ++i)
            {
                SPImage townLightImage = new SPImage(mScene.TextureByName(String.Format("town-light-{0}", i)));
                townLightImage.X = townLightCoords[2*i];
                townLightImage.Y = townLightCoords[2*i+1];
                mTownLights.AddChild(townLightImage);
            }

            mTownLights.Alpha = 0;
            mScene.AddProp(mTownLights);

            // Cannons
	        mLeftCannon = new TownCannon("single-shot_");
	        mLeftCannon.X = 56;
	        mLeftCannon.Y = 90;
	        mLeftCannon.Idle();

            mRightCannon = new TownCannon("single-shot_");
	        mRightCannon.X = 178;
	        mRightCannon.Y = 12;
	        mRightCannon.Idle();

            // Day/Night cycle
            List<SPImage> shaders = new List<SPImage>() { townImage, townHouseImage, townCannonImage };
            mNightShade = new NightShade(shaders);

            GameController gc = GameController.GC;
            mNightShade.TransitionTimeOfDay(gc.TimeOfDay, gc.TimeKeeper.TimeRemaining, gc.TimeKeeper.ProportionRemaining);
            TransitionLightsForTimeOfDay(gc.TimeOfDay, gc.TimeKeeper.TimeRemaining, gc.TimeKeeper.ProportionRemaining);
        }

        public override void AdvanceTime(double time)
        {
            if (mTweened != null)
            {
                mTweener.AdvanceTime(time);
                if (mTweened.Alpha != mTweener.TweenedValue)
                    mTweened.Alpha = mTweener.TweenedValue;
                else
                    mTweened = null;
            }

            mNightShade.AdvanceTime(time);
        }

        public void TravelBackInTime(float duration)
        {
            if (mTownFutureSprite == null || mTownFutureLights == null)
                return;

            if (mTownFutureLights != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mTownFutureLights);
                mScene.RemoveProp(mTownFutureLights);
                mTownFutureLights = null;
            }

            mScene.Juggler.RemoveTweensWithTarget(mTownSprite);
            mScene.Juggler.RemoveTweensWithTarget(mTownFutureSprite);
            mScene.Juggler.RemoveTweensWithTarget(mTownHouseAndCannon);

            SPTween tween = new SPTween(mTownFutureSprite, mTownFutureSprite.Alpha * duration);
            tween.AnimateProperty("Alpha", 0f);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)delegate(SPEvent ev)
            {
                if (mTownFutureSprite != null)
                {
                    if (mTownFutureSprite.NumChildren > 0)
                    {
                        SPImage townFutureImage = mTownFutureSprite.ChildAtIndex(0) as SPImage;
                        mNightShade.RemoveShader(townFutureImage);
                    }

                    mTownFutureSprite.RemoveFromParent();
                    mTownFutureSprite = null;
                }
            }, true);
            mScene.Juggler.AddObject(tween);

            tween = new SPTween(mTownSprite, (1f - mTownSprite.Alpha) * duration);
            tween.AnimateProperty("Alpha", 1f);
            mScene.Juggler.AddObject(tween);

            tween = new SPTween(mTownHouseAndCannon, (1f - mTownHouseAndCannon.Alpha) * duration);
            tween.AnimateProperty("Alpha", 1f);
            mScene.Juggler.AddObject(tween);
        }

        public void TravelForwardInTime(float duration)
        {
            if (mTownFutureSprite != null || mTownFutureLights != null)
                return;

            mTownFutureSprite = new SPSprite();
            mTownFutureSprite.Alpha = 0f;

            SPImage image = new SPImage(mScene.TextureByName("town-future"));
            image.X = 0f;
            image.Y = 0f;
            mTownFutureSprite.AddChild(image);
            mNightShade.AddShader(image);
            AddChild(mTownFutureSprite);

            // Lights
            SPImage townFutureLightsImage = new SPImage(mScene.TextureByName("town-future-lights"));
            mTownFutureLights = new Prop(PFCat.EXPLOSIONS);
            mTownFutureLights.AddChild(townFutureLightsImage);
            mTownFutureLights.Alpha = 0f;
            mScene.AddProp(mTownFutureLights);

            SPTween tween = new SPTween(mTownFutureSprite, duration);
            tween.AnimateProperty("Alpha", 1f);
            mScene.Juggler.AddObject(tween);

            tween = new SPTween(mTownSprite, duration);
            tween.AnimateProperty("Alpha", 0f);
            mScene.Juggler.AddObject(tween);

            tween = new SPTween(mTownHouseAndCannon, duration);
            tween.AnimateProperty("Alpha", 0f);
            mScene.Juggler.AddObject(tween);
        }

        private float mLightsAlphaFrom;
        private float mLightsAlphaTo;
        private double mShadeDuration;
        private double mShadeTotalDuration;

        private void TransitionLightsForTimeOfDay(TimeOfDay timeOfDay, float transitionDuration, float proportionRemaining)
        {
            SPDisplayObject lights = mTownLights;

            //mScene.Juggler.RemoveTweensWithTarget(mTownLights);

            if (mTownFutureLights != null)
            {
                //mScene.Juggler.RemoveTweensWithTarget(mTownFutureLights);
                lights = mTownFutureLights;
            }

            bool transition = false;
            float alphaFrom = 0f, alphaTo = 0f;

            switch (timeOfDay)
            {
                case TimeOfDay.DuskTransition:
                    transition = true;
                    alphaFrom = 0f;
                    alphaTo = 0.25f;
                    break;
                case TimeOfDay.Dusk:
                    alphaFrom = 0.25f;
                    alphaTo = 0.25f;
                    break;
                case TimeOfDay.EveningTransition:
                    transition = true;
                    alphaFrom = 0.25f;
                    alphaTo = 1f;
                    break;
                case TimeOfDay.Evening:
                case TimeOfDay.Midnight:
                    alphaFrom = 1f;
                    alphaTo = 1f;
                    break;
                case TimeOfDay.DawnTransition:
                    transition = true;
                    alphaFrom = 1f;
                    alphaTo = 0f;
                    break;
                case TimeOfDay.NewGameTransition:
                    transition = true;
                    alphaFrom = lights.Alpha;
                    alphaTo = 0f;
                    break;
                default:
                    break;
            }

            if (!transition)
            {
                mTweened = null;
                mTweener.Reset(alphaTo);
                lights.Alpha = mTweener.TweenedValue;
            }
            else
            {
                int alphaRange = (int)(alphaTo - alphaFrom);
                alphaFrom += (1f - proportionRemaining) * alphaRange;
                mTweened = lights;
                mTweener.Reset(alphaFrom, alphaTo, transitionDuration);
                mTweened.Alpha = mTweener.TweenedValue;

                //SPTween tween = new SPTween(lights, transitionDuration);
                //tween.AnimateProperty("Alpha", alphaTo);
                //mScene.Juggler.AddObject(tween);
            }
        }

        public void OnTimeOfDayChanged(TimeOfDayChangedEvent ev)
        {
            if (ev.Transitions)
            {
                mNightShade.TransitionTimeOfDay(ev.TimeOfDay, ev.TimeRemaining, ev.ProportionRemaining);
                TransitionLightsForTimeOfDay(ev.TimeOfDay, ev.TimeRemaining, ev.ProportionRemaining);
            }
        }

        public override void PrepareForNewGame()
        {
            // Do nothing
        }
        #endregion
    }
}
