
#if WINDOWS
    #define DETAILED_OCEAN
#endif

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
//using SPImage = SparrowXNA.SPQuad;

namespace AwesomePirates
{
    class Sea : Prop
    {
        private enum SeaState
        {
            Normal = 0,
            TransitionToWhirlpool,
            Whirlpool,
            TransitionFromWhirlpool
        }

        private enum LavaState
        {
            Inactive = 0,
            TransitionTo,
            Active,
            TransitionFrom
        }

        public const string CUST_EVENT_TYPE_SEA_OF_LAVA_PEAKED = "seaOfLavaPeakedEvent";

        private const float kWhirlpoolOceanAlpha = 0.4f;

        private const float kShoreBreakAlphaMin = 0.4f;
        private const float kShoreBreakAlphaMax = 0.65f;
        private const float kShoreBreakAlphaMid = 0.525f;
        private const float kShoreBreakAlphaRange = 0.25f;

        private const float kShoreBreakScaleMin = 0.2f;
        private const float kShoreBreakScaleMax = 1.0f;
        private const float kShoreBreakScaleMid = 0.6f;
        private const float kShoreBreakScaleRange = 0.8f;

        private const float kLavaTransitionDuration = 2.5f; // Must not be zero to prevent possible DBZ

        // Add a bloom filter to sunrise and sunset: http://create.msdn.com/en-US/education/catalog/sample/bloom
        public Sea()
            : base(PFCat.SEA)
        {
            mAdvanceable = true;

            GameController gc = GameController.GC;
            mLavaID = gc.ThisTurn.TurnID;
            mScreenshotModeEnabled = false;
            mWhirlpoolTimer = 0.0;

            mLavaTweener = new FloatTweener(0f, SPTransitions.SPLinear);
            mOnTransitionedToLava = new Action(OnTransitionedToLava);
            mOnTransitionedFromLava = new Action(OnTransitionedFromLava);

            mOceanTweener = new FloatTweener(1f, SPTransitions.SPLinear);
            mOnTransitionedToWhirlpool = new Action(OnTransitionedToWhirlpool);
            mOnTransitionedFromWhirlpool = new Action(OnTransitionedFromWhirlpool);

            //mTweener = new FloatTweener(0f, SPTransitions.SPLinear);

            // Load time gradient settings
            mTimeGradientJuggler = new SPJuggler();
            mTimeGradients = new List<Color[]>();

            List<object> settings = PlistParser.ArrayFromPlist("data/plists/TimeGradients.plist");

            foreach (object obj in settings)
            {
                List<object> objGradients = obj as List<object>;
                Color[] gradient = new Color[objGradients.Count];

                for (int i = 0; i < objGradients.Count; ++i)
                {
                    int color = (int)objGradients[i];
                    gradient[i] = new Color(SPMacros.SP_COLOR_PART_RED(color), SPMacros.SP_COLOR_PART_GREEN(color), SPMacros.SP_COLOR_PART_BLUE(color));
                }
                
                mTimeGradients.Add(gradient);
            }

            mRenderMarker = new SPImage(mScene.TextureByName("clear-texture"));

            // Water
            mWaterSprite = new SPSprite();
            mWater = new SPQuad(mScene.ViewWidth, mScene.ViewHeight);
            //mWater.Rotation = SPMacros.SP_D2R(45);
            //mWater.Alpha = 0.02f;
            mWaterSprite.AddChild(mWater);
            AddChild(mWaterSprite);

            // Voodoo Quads
            mLava = new SPQuad(mScene.ViewWidth, mScene.ViewHeight);
            mLava.SetColor(new Color(0xff, 0, 0), 0);
            mLava.SetColor(new Color(0xfd, 0x62, 0), 1);
            mLava.SetColor(new Color(0xfe, 0x2b, 0), 2);
            mLava.SetColor(new Color(0xfb, 0x8e, 0), 3);
            AddChild(mLava);

            // Ocean
#if IOS_SCREENS
            mOceanProp = new Prop(PFCat.WAVES);
            mScene.AddProp(mOceanProp);
            AddWaves();
#else
            mOcean = new Ocean(mScene.ViewWidth, mScene.ViewHeight);
            //mOcean = new Ocean(768, 768);
            mOceanProp = new Prop(mOcean.Category);
            mOceanProp.AddChild(mOcean);
    #if DETAILED_OCEAN
            mScene.AddProp(mOceanProp);
    #else

            Effect oceanEffect = mScene.EffectForKey("OceanShader");
            oceanEffect.Parameters["waveScale"].SetValue(6.25f);

            mRenderedOceanTexture = new SPRenderTexture(gc.GraphicsDevice, 0.75f * mScene.ViewWidth, 0.75f * mScene.ViewHeight); // mScene.ViewWidth, 256
            mRenderedOceanImage = new SPImage(mRenderedOceanTexture.VolatileTexture);
            mRenderedOceanImage.Texture.Repeat = true;
            mRenderedOceanProp = new Prop(mOcean.Category);
            mRenderedOceanProp.AddChild(mRenderedOceanImage);
            mScene.AddProp(mRenderedOceanProp);

            mRenderedOceanImage.ScaleX = mScene.ViewWidth / mRenderedOceanImage.Width;
            mRenderedOceanImage.ScaleY = mScene.ViewHeight / mRenderedOceanImage.Height;

            /*
            mRenderedOceanTexture = new SPRenderTexture(gc.GraphicsDevice, mScene.ViewWidth, 272); // mScene.ViewWidth, 256
            mRenderedOceanImage = new SPImage(mRenderedOceanTexture.VolatileTexture);
            mRenderedOceanImage.Texture.Repeat = true;
            mRenderedOceanProp = new Prop(mOcean.Category);
            mRenderedOceanProp.AddChild(mRenderedOceanImage);
            mScene.AddProp(mRenderedOceanProp);

            mRenderedOceanImage.Width = mScene.ViewWidth;
            mRenderedOceanImage.Height = mScene.ViewHeight;

            float xRepeat = mRenderedOceanImage.Width / mRenderedOceanImage.Texture.Width;
            float yRepeat = mRenderedOceanImage.Height / (mRenderedOceanImage.Texture.Height);
            mRenderedOceanImage.SetTexCoord(new Vector2(xRepeat, 0), 1);
            mRenderedOceanImage.SetTexCoord(new Vector2(0, yRepeat), 2);
            mRenderedOceanImage.SetTexCoord(new Vector2(xRepeat, yRepeat), 3);
            */
    #endif
#endif

            // Current time gradient
            mTimeOfDay = gc.TimeOfDay;

            // Initialize this for first call to transitionTimeGradients
            SetTimeGradient(mWater);

            mGradientTweens = new Dictionary<TimeOfDay, SPTween>(10, TimeOfDayComparer.Instance);

            for (TimeOfDay timeOfDay = TimeOfDay.NewGameTransition; timeOfDay <= TimeOfDay.Dawn; ++timeOfDay)
            {
                if (TimeKeeper.DoesTimePeriodTransition(timeOfDay))
                {
                    Color[] gradient = mTimeGradients[(int)timeOfDay];
                    QuadColorer qc = new QuadColorer(mWater);

                    SPTween tween = new SPTween(qc, TimeKeeper.DurationForPeriod(timeOfDay));

                    for (int i = 0; i < gradient.Length; ++i)
                        qc.AnimateVertexColor(gradient[i], i, tween);

                    mGradientTweens.Add(timeOfDay, tween);
                }
            }

            if (gc.TimeKeeper.Transitions)
                TransitionTimeGradients(gc.TimeKeeper.TimeRemaining, gc.TimeKeeper.ProportionRemaining);

            // Shore break
            mShorebreak = new Prop(PFCat.SHOREBREAK);
            mShorebreak.X = mScene.ViewWidth - 168f;  //2 * (397f + 32f);
            mShorebreak.Y = mScene.ViewHeight - 138f;  //2 * (252f + 64f);
            mShorebreak.Rotation = -SPMacros.PI / 4.6f;
            mScene.AddProp(mShorebreak);

            mShorebreakApproachTweens = null;
            mShorebreakRecedeTweens = null;

            SPImage whiteWater = null;
            List<SPTexture> whiteWaterTextures = mScene.TexturesStartingWith("shorebreak_");
            float scaleStart = 0.9f, scaleTarget = 0.0f, alphaTarget = 0.0f;

            for (int i = 0; i < whiteWaterTextures.Count; ++i, scaleStart -= 0.3333f)
            {
                whiteWater = new SPImage(whiteWaterTextures[i]);
                whiteWater.X = -whiteWater.Width / 2;
                whiteWater.Y = -whiteWater.Height / 2;
                whiteWater.ScaleY = scaleStart;
                mShorebreak.AddChild(whiteWater);

                bool receding = (whiteWater.ScaleY > kShoreBreakScaleMid);

                scaleTarget = (receding) ? kShoreBreakScaleMin : kShoreBreakScaleMax;
                alphaTarget = kShoreBreakAlphaMin;
                whiteWater.Alpha = (receding) ? kShoreBreakAlphaMin : kShoreBreakAlphaMax - kShoreBreakAlphaRange * (Math.Abs(scaleTarget - scaleStart) / kShoreBreakScaleRange);
                AnimateShoreBreak(whiteWater, scaleTarget, alphaTarget, receding);
            }

            SetState(SeaState.Normal);
            SetLavaState(LavaState.Inactive);

            // Whirlpool
            mWhirlpool = null;
        }

        #region Fields
        private SeaState mState;
        private LavaState mLavaState;
        private TimeOfDay mTimeOfDay;
        private uint mLavaID;
        private bool mScreenshotModeEnabled;
        private double mWhirlpoolTimer;

        private SPImage mRenderMarker; // Hack to ensure texture is set when taking screenshot (otherwise non-textured quads don't draw correctly)
        private SPQuad mWater;
        private SPQuad mLava;
        private SPSprite mWaterSprite;
        private Prop mShorebreak;
        private SPJuggler mTimeGradientJuggler;
        private List<Color[]> mTimeGradients;

        private Ocean mOcean;
        private Prop mOceanProp;

        private Dictionary<TimeOfDay, SPTween> mGradientTweens;
        private List<SPTween[]> mShorebreakApproachTweens;
        private List<SPTween> mShorebreakRecedeTweens;
        private WhirlpoolActor mWhirlpool;

        private FloatTweener mLavaTweener;
        private Action mOnTransitionedToLava = null;
        private Action mOnTransitionedFromLava = null;

        private FloatTweener mOceanTweener;
        private Action mOnTransitionedToWhirlpool = null;
        private Action mOnTransitionedFromWhirlpool = null;

#if !DETAILED_OCEAN
        private SPRenderTexture mRenderedOceanTexture;
        private SPImage mRenderedOceanImage;
        private Prop mRenderedOceanProp;
#endif

#if IOS_SCREENS
        private List<Wave> mWaves = new List<Wave>();
#endif
        #endregion

        #region Properties
        public float LavaAlpha
        {
            get { return mLava.Alpha; }
            set
            {
                mLava.Alpha = value;

                if (mWhirlpool != null && mLavaState != LavaState.Inactive)
                    mWhirlpool.SetWaterColor(SPUtils.ColorFromColor(0xff0000 + (uint)(255 * (1f - value)) * 0x101));
            }
        }
        public float WaterAlpha { set { mWaterSprite.Alpha = value; } }
        public float OceanAlpha { set { mOceanProp.Alpha = value; } }
        public PlayerIndex OwnerID { get; set; }
        public static float LavaTransitionDuration { get { return kLavaTransitionDuration; } }
        public static float WhirlpoolOceanAlpha { get { return kWhirlpoolOceanAlpha; } }
        #endregion

        #region Methods
#if IOS_SCREENS
        private void AddWaves()
        {
            if (mWaves.Count != 0)
                return;

            SPTexture texture = mScene.TextureByName("waves0");
            texture.Repeat = true;

            Wave wave = new Wave(texture, 0.7f, 0.35f, 1.5f);
            wave.FlowXOverTime(120f);
            wave.FlowYOverTime(60f);
            mWaves.Add(wave);
            mOceanProp.AddChild(wave);
        }
#endif

        public void SetShorebreakHidden(bool hidden)
        {
            mShorebreak.Visible = !hidden;
        }

        private void SetState(SeaState state)
        {
            switch (state)
            {
                case SeaState.Normal:
                    mOceanTweener.Reset(1f);
                    break;
                case SeaState.TransitionToWhirlpool:
                    break;
                case SeaState.Whirlpool:
                    mOceanTweener.Reset(kWhirlpoolOceanAlpha);
                    break;
                case SeaState.TransitionFromWhirlpool:
                    break;
            }

            mState = state;
        }

        private void SetLavaState(LavaState state)
        {
            switch (state)
            {
                case LavaState.Inactive:
                    mLavaTweener.Reset(0f);
                    mLava.Visible = false;
                    if (mWhirlpool != null) mWhirlpool.SetWaterColor(new Color(0xff, 0xff, 0xff));
                    mScene.StopSound("SeaOfLava");
                    break;
                case LavaState.TransitionTo:
                    mLavaID = TurnID;
                    mLava.Visible = true;

                    if (mLavaState == LavaState.Inactive)
                        mScene.PlaySound("SeaOfLava");
                    break;
                case LavaState.Active:
                    mLavaTweener.Reset(1f);
                    mLava.Visible = true;
                    if (mWhirlpool != null) mWhirlpool.SetWaterColor(new Color(0xff, 0x0, 0x0));
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SEA_OF_LAVA_PEAKED));
                    break;
                case LavaState.TransitionFrom:
                    mLava.Visible = true;
                    break;
            }

            mLavaState = state;
        }

        public void EnableScreenshotMode(bool enable)
        {
            if (mRenderMarker != null)
            {
                if (enable && !mScreenshotModeEnabled)
                    AddChildAtIndex(mRenderMarker, 0);
                else if (!enable && mScreenshotModeEnabled)
                    mRenderMarker.RemoveFromParent();
            }

#if !DETAILED_OCEAN
            if (mRenderedOceanProp != null && mOceanProp != null && mLava != null)
            {
                if (enable && !mScreenshotModeEnabled)
                {
                    mRenderedOceanProp.Visible = false;
                    AddChildAtIndex(mOceanProp, ChildIndex(mLava) + 1);
                }
                else if (!enable && mScreenshotModeEnabled)
                {
                    mOceanProp.RemoveFromParent();
                    mRenderedOceanProp.Visible = true;
                }
            }

#endif
            mScreenshotModeEnabled = enable;
        }

#if !DETAILED_OCEAN
        public override void Draw(GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (!mScreenshotModeEnabled)
            {
                support.SuspendRendering(true);
                Rectangle rect = support.GraphicsDevice.ScissorRectangle;
                RenderOcean(gameTime, Matrix.Identity);
                support.GraphicsDevice.ScissorRectangle = rect;
                support.SuspendRendering(false);
            }

            base.Draw(gameTime, support, parentTransform);
        }

        private Action<SPRenderSupport> mOceanDelegate = null;

        private void RenderOcean(GameTime gameTime, Matrix parentTransform)
        {
            if (mOceanDelegate == null)
                mOceanDelegate = RenderOceanCallback;

            mRenderedOceanTexture.ClearWithColor(Color.Transparent);
            mRenderedOceanTexture.BundleDrawCalls(mOceanDelegate);
        }

        void RenderOceanCallback(SPRenderSupport support)
        {
            mOceanProp.Draw(null, support, Matrix.Identity);
        }
#endif
        public override void AdvanceTime(double time)
        {
            mLavaTweener.AdvanceTime(time);
            if (!mLavaTweener.Delaying && LavaAlpha != mLavaTweener.TweenedValue)
                LavaAlpha = mLavaTweener.TweenedValue;

            mOceanTweener.AdvanceTime(time);
            if (!mOceanTweener.Delaying && mOceanProp.Alpha != mOceanTweener.TweenedValue)
                mOceanProp.Alpha = mOceanTweener.TweenedValue;

            if (mWhirlpool != null && mWhirlpoolTimer > 0.0)
            {
                mWhirlpoolTimer -= time;
        
                if (mWhirlpoolTimer <= 0.0)
                    TransitionFromWhirlpoolOverTime(Globals.VOODOO_DESPAWN_DURATION);
            }

            if (mTimeGradientJuggler != null)
                mTimeGradientJuggler.AdvanceTime(time);
            if (mOcean != null)
                mOcean.AdvanceTime(time);
        }

        public void AdoptWaterColor(SPQuad quad)
        {
            if (quad != null && mWater != null)
            {
                for (int i = 0; i < 4; ++i)
                    quad.SetColor(mWater.ColorAtVertex(i), i);
            }
        }

        private void SetTimeGradient(SPQuad quad)
        {
            mTimeGradientJuggler.RemoveAllObjects();

            Color[] gradient = mTimeGradients[(int)mTimeOfDay];

            for (int i = 0; i < gradient.Length; ++i)
                quad.SetColor(gradient[i], i);
        }

        private void TransitionTimeGradients(float transitionDuration, float proportionRemaining)
        {
            mTimeGradientJuggler.RemoveAllObjects();

            if (!mGradientTweens.ContainsKey(mTimeOfDay))
                return;

            SPTween tween = mGradientTweens[mTimeOfDay];

            // Re-use cached tweens in case of full duration
            if (tween != null && SPMacros.SP_IS_FLOAT_EQUAL(proportionRemaining, 1f))
            {
                tween.Reset();
                mTimeGradientJuggler.AddObject(tween);
            }
            else
            {
                Color[] gradient = mTimeGradients[(int)mTimeOfDay];
                QuadColorer qc = new QuadColorer(mWater);

                tween = new SPTween(qc, transitionDuration);

                for (int i = 0; i < gradient.Length; ++i)
                    qc.AnimateVertexColor(gradient[i], i, tween);

                mTimeGradientJuggler.AddObject(tween);
            }
        }

        public void OnTimeOfDayChanged(TimeOfDayChangedEvent ev)
        {
            mTimeOfDay = ev.TimeOfDay;

            if (ev.Transitions)
                TransitionTimeGradients(ev.TimeRemaining, ev.ProportionRemaining);
            else
                SetTimeGradient(mWater);

            if (mOcean != null)
                mOcean.OnTimeOfDayChanged(ev);

#if IOS_SCREENS
            if (mWaves != null)
            {
                foreach (Wave wave in mWaves)
                    wave.OnTimeOfDayChanged(ev);
            }
#endif
        }

        private void OnShoreBreakApproached(SPEvent ev)
        {
            SPTween recedeTween = null;
            SPTween approachTween = ev.CurrentTarget as SPTween;
            SPImage whiteWater = approachTween.Target as SPImage;

            whiteWater.Alpha = kShoreBreakAlphaMin;
            mScene.Juggler.RemoveTweensWithTarget(whiteWater); // Remove looping alpha tween

            if (mShorebreakRecedeTweens == null)
                mShorebreakRecedeTweens = new List<SPTween>(3);
            else if (mShorebreakRecedeTweens.Count >= 3)
            {
                int index = mShorebreakRecedeTweens.Count - 1;
                recedeTween = mShorebreakRecedeTweens[index];
                mShorebreakRecedeTweens.RemoveAt(index);
                mShorebreakRecedeTweens.Insert(0, recedeTween);
            }

            if (recedeTween == null)
            {
                // Receding from shore
                float scaleTarget = kShoreBreakScaleMin;
                float duration = 4 * Math.Abs(whiteWater.ScaleY - scaleTarget);

                recedeTween = new SPTween(whiteWater, duration, SPTransitions.SPEaseInOut);
                recedeTween.AnimateProperty("ScaleY", scaleTarget);
                recedeTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnShoreBreakReceded));

                mShorebreakRecedeTweens.Insert(0, recedeTween);
            }

            recedeTween.Reset();
            mScene.Juggler.AddObject(recedeTween);
        }

        private void OnShoreBreakReceded(SPEvent ev)
        {
            SPTween[] approachTweens = null;
            SPTween recedeTween = ev.CurrentTarget as SPTween;
            SPImage whiteWater = recedeTween.Target as SPImage;

            whiteWater.Alpha = kShoreBreakAlphaMin;

            if (mShorebreakApproachTweens == null)
                mShorebreakApproachTweens = new List<SPTween[]>(3);
            else if (mShorebreakApproachTweens.Count >= 3)
            {
                int index = mShorebreakApproachTweens.Count - 1;
                approachTweens = mShorebreakApproachTweens[index];
                mShorebreakApproachTweens.RemoveAt(index);
                mShorebreakApproachTweens.Insert(0, approachTweens);
            }

            if (approachTweens == null)
            {
                // Approach shore
                float scaleTarget = kShoreBreakScaleMax, alphaTarget = kShoreBreakAlphaMax;
                float duration = 4 * Math.Abs(whiteWater.ScaleY - scaleTarget);

                SPTween alphaTween = new SPTween(whiteWater, duration / 2, SPTransitions.SPLinear);
                alphaTween.AnimateProperty("Alpha", alphaTarget);
                alphaTween.Loop = SPLoopType.Reverse;

                SPTween scaleTween = new SPTween(whiteWater, duration, SPTransitions.SPEaseInOut);
                scaleTween.AnimateProperty("ScaleY", scaleTarget);
                scaleTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnShoreBreakApproached));

                approachTweens = new SPTween[] { alphaTween, scaleTween };
                mShorebreakApproachTweens.Insert(0, approachTweens);
            }

            foreach (SPTween tween in approachTweens)
            {
                tween.Reset();
                mScene.Juggler.AddObject(tween);
            }
        }

        private void AnimateShoreBreak(SPImage whiteWater, float scaleTarget, float alphaTarget, bool receding)
        {
            float duration = 4 * Math.Abs(whiteWater.ScaleY - scaleTarget);

            SPTween tween = new SPTween(whiteWater, duration, SPTransitions.SPLinear);
            tween.AnimateProperty("Alpha", alphaTarget);
            mScene.Juggler.AddObject(tween);

            tween = new SPTween(whiteWater, duration, SPTransitions.SPEaseInOut);
            tween.AnimateProperty("ScaleY", scaleTarget);

            if (receding)
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnShoreBreakReceded);
            else
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnShoreBreakApproached);
            mScene.Juggler.AddObject(tween);
        }

        private void OnShoreBreakCompleted(SPEvent ev)
        {
            // Do nothing for now
        }

        public void TransitionToLavaOverTime(float duration)
        {
            if (mLavaState == LavaState.Active || mLavaState == LavaState.TransitionTo)
                return;

            mScene.Juggler.RemoveTweensWithTarget(this);

            if (SPMacros.SP_IS_FLOAT_EQUAL(duration, 0f))
                SetLavaState(LavaState.Active);
            else
            {
                SetLavaState(LavaState.TransitionTo);
                mLavaTweener.Reset(LavaAlpha, 1f, duration);
                mLavaTweener.TweenComplete = mOnTransitionedToLava;
            }
        }

        public void TransitionFromLavaOverTime(float duration, float delay = 0f)
        {
            if (mLavaState == LavaState.Inactive || mLavaState == LavaState.TransitionFrom)
                return;

            mScene.Juggler.RemoveTweensWithTarget(this);

            if (SPMacros.SP_IS_FLOAT_EQUAL(duration, 0f))
                SetLavaState(LavaState.Inactive);
            else
            {
                SetLavaState(LavaState.TransitionFrom);
                mLavaTweener.Reset(LavaAlpha, 0f, duration, delay);
                mLavaTweener.TweenComplete = mOnTransitionedFromLava;
            }
        }

        private void OnTransitionedToLava()
        {
            SetLavaState(LavaState.Active);
        }

        private void OnTransitionedFromLava()
        {
            SetLavaState(LavaState.Inactive);

            if (mLavaID == TurnID && mScene.GameMode == GameMode.Career)
                mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_VOODOO_GADGET_EXPIRED, Idol.VOODOO_SPELL_SEA_OF_LAVA);
        }

        public void SummonWhirlpoolWithDuration(float duration)
        {
            if (mWhirlpool != null)
            {
                mWhirlpool.RemoveEventListener(WhirlpoolActor.CUST_EVENT_TYPE_WHIRLPOOL_DESPAWNED, (SPEventHandler)OnWhirlpoolDespawned);
                mWhirlpool = null;
            }
    
	        float spawnDuration = WhirlpoolActor.SpawnDuration;
	        double idolDuration = Idol.DurationForIdol(mScene.IdolForKey(Idol.VOODOO_SPELL_WHIRLPOOL));
	
	        if (SPMacros.SP_IS_DOUBLE_EQUAL(duration,idolDuration))
                TransitionToWhirlpoolOverTime(spawnDuration);
	        else if (duration > (idolDuration - spawnDuration))
            {
                OceanAlpha = Sea.WhirlpoolOceanAlpha * (float)((idolDuration-duration) / spawnDuration); // Won't DBZ
                TransitionToWhirlpoolOverTime((float)(spawnDuration - (idolDuration-duration)));
	        }
            else
                TransitionToWhirlpoolOverTime(0);

            mWhirlpool = WhirlpoolActor.CreateWhirlpoolActor(ResManager.P2MX(ResManager.RESW / 2), ResManager.P2MY(ResManager.RESH / 2), 0f, ResManager.RESM.GameFactorHeight, duration);
            mWhirlpool.AddEventListener(WhirlpoolActor.CUST_EVENT_TYPE_WHIRLPOOL_DESPAWNED, (SPEventHandler)OnWhirlpoolDespawned);
            mScene.AddActor(mWhirlpool);
	
	        if (duration <= Globals.VOODOO_DESPAWN_DURATION)
            {
                OceanAlpha = 1 - Sea.WhirlpoolOceanAlpha * (duration / Globals.VOODOO_DESPAWN_DURATION);
                TransitionFromWhirlpoolOverTime(duration);
	        }
            else
                mWhirlpoolTimer = duration - Globals.VOODOO_DESPAWN_DURATION;
        }

        private void OnWhirlpoolDespawned(SPEvent ev)
        {
            mWhirlpool.RemoveEventListener(WhirlpoolActor.CUST_EVENT_TYPE_WHIRLPOOL_DESPAWNED, (SPEventHandler)OnWhirlpoolDespawned);
            mWhirlpool = null;
        }

        public void TransitionToWhirlpoolOverTime(float duration)
        {
            if (mState != SeaState.Normal)
		        return;
	
	        if (SPMacros.SP_IS_FLOAT_EQUAL(duration,0))
                SetState(SeaState.Whirlpool);
	        else
            {
                SetState(SeaState.TransitionToWhirlpool);
                mOceanTweener.Reset(mOceanProp.Alpha, kWhirlpoolOceanAlpha, duration);
                mOceanTweener.TweenComplete = mOnTransitionedToWhirlpool;
	        }
        }

        public void TransitionFromWhirlpoolOverTime(float duration)
        {
            if (mState != SeaState.Whirlpool)
		        return;
	
	        if (SPMacros.SP_IS_FLOAT_EQUAL(duration,0))
		        SetState(SeaState.Normal);
	        else
            {
                SetState(SeaState.TransitionFromWhirlpool);
                mOceanTweener.Reset(mOceanProp.Alpha, 1f, duration);
                mOceanTweener.TweenComplete = mOnTransitionedFromWhirlpool;
	        }
        }

        private void OnTransitionedToWhirlpool()
        {
            SetState(SeaState.Whirlpool);
        }

        private void OnTransitionedFromWhirlpool()
        {
            SetState(SeaState.Normal);
        }

        public void EnableSlowedTime(bool enable)
        {
            if (mWhirlpool != null)
                mWhirlpool.SuckFactor = (enable) ? 4.0f : 1.0f;
        }

        public void PrepareForNewGame()
        {
            mTurnID = GameController.GC.ThisTurn.TurnID;
            TransitionFromLavaOverTime(2.0f);
            TransitionFromWhirlpoolOverTime(2.0f);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
#if IOS_SCREENS
                        if (mWaves != null)
                        {
                            foreach (Wave wave in mWaves)
                                mScene.Juggler.RemoveTweensWithTarget(wave);
                            mWaves = null;
                        }
#endif

                        if (mTimeGradientJuggler != null)
                        {
                            mTimeGradientJuggler.RemoveAllObjects();
                            mTimeGradientJuggler = null;
                        }

                        if (mWhirlpool != null)
                        {
                            mWhirlpool.RemoveEventListener(WhirlpoolActor.CUST_EVENT_TYPE_WHIRLPOOL_DESPAWNED, (SPEventHandler)OnWhirlpoolDespawned);
                            mWhirlpool = null;
                        }

                        if (mOcean != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mOcean);
                            mOcean = null;
                        }

                        if (mOceanProp != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mOceanProp);
                            mScene.RemoveProp(mOceanProp);
                            mOceanProp = null;
                        }

                        if (mLava != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mLava);
                            mLava = null;
                        }

                        if (mWater != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mWater);
                            mWater = null;
                        }

                        if (mShorebreak != null)
                        {
                            mScene.SpriteLayerManager.RemoveChild(mShorebreak, (int)PFCat.SHOREBREAK);

                            for (int i = 0; i < mShorebreak.NumChildren; ++i)
                            {
                                SPDisplayObject whiteWater = mShorebreak.ChildAtIndex(i);
                                mScene.Juggler.RemoveTweensWithTarget(whiteWater);
                            }

                            mScene.Juggler.RemoveTweensWithTarget(mShorebreak);
                            mShorebreak = null;
                        }

                        if (mShorebreakApproachTweens != null)
                        {
                            foreach (SPTween[] tweens in mShorebreakApproachTweens)
                                tweens[1].RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mShorebreakApproachTweens = null;
                        }

                        if (mShorebreakRecedeTweens != null)
                        {
                            foreach (SPTween tween in mShorebreakRecedeTweens)
                                tween.RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mShorebreakRecedeTweens = null;
                        }

                        if (mLavaTweener != null)
                            mLavaTweener.TweenComplete = null;
                        if (mOceanTweener != null)
                            mOceanTweener.TweenComplete = null;

                        mRenderMarker = null;
                        mShorebreakApproachTweens = null;
                        mShorebreakRecedeTweens = null;
                        mWaterSprite = null;
                        mTimeGradients = null;
                        mGradientTweens = null;
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
