using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class Ocean : Prop
    {
        private enum OceanSurfaceState
        {
            Normal = 0x1,
            TransitionToNight = 0x2,
            TransitionToDay = 0x4
        }

        private const float kNormalAlpha = 0.125f;

        public Ocean(float width, float height)
            : base(PFCat.WAVES)
        {
            //mAdvanceable = true; // Advanced by sea (because we're contained within a parent Prop, which hides our advance call from the scene)
            mSlowable = true;
            mSurface = new SPQuad(width, height);
            mSurface.Effecter = new SPEffecter(mScene.EffectForKey("OceanShader"), CustomOceanDraw);
            AddChild(mSurface);

            mSurfaceNormalMaps = new SPTexture[4];
            for (int i = 0; i < 4; i++)
                mSurfaceNormalMaps[i] = mScene.TextureByName("ocean" + i + "_N");

            //mShorelineMask = mScene.TextureByName("ocean_M");

            mSky = new SkyBox();
            mSkyEffect = mScene.EffectForKey("SkyShader");
            mSkyTex = GameController.GC.Content.Load<TextureCube>("cubemaps/Sky");
            mEyeRot = Matrix.Identity;
            mEyePos = new Vector3(0, 0, 1835);
            mElapsedTime = 0;
            Alpha = kNormalAlpha;

            Alpha = SetState(OceanSurfaceState.Normal);
            mTweener = new FloatTweener(Alpha, SPTransitions.SPLinear);
        }

        #region Fields
        private OceanSurfaceState mState;
        private FloatTweener mTweener;
        private SPQuad mSurface;
        private SPTexture[] mSurfaceNormalMaps;
        //private SPTexture mShorelineMask;

        private double mElapsedTime;
        private Vector3 mEyePos;
        private Matrix mEyeRot;
        private Effect mSkyEffect;
        private TextureCube mSkyTex;
        private SkyBox mSky;
        #endregion

        #region Methods
        private float SetState(OceanSurfaceState state)
        {
            float targetAlpha = Alpha;

            switch (state)
            {
                case OceanSurfaceState.Normal:
                    targetAlpha = kNormalAlpha;
                    break;
                case OceanSurfaceState.TransitionToDay:
                    targetAlpha = kNormalAlpha;
                    break;
                case OceanSurfaceState.TransitionToNight:
                    targetAlpha = 0.6f * kNormalAlpha;
                    break;
            }

            mState = state;
            return targetAlpha; 
        }

        public void OnTimeOfDayChanged(TimeOfDayChangedEvent ev)
        {
            float targetAlpha = Alpha;

            switch (ev.TimeOfDay)
            {
                case TimeOfDay.NewGameTransition:
                    targetAlpha = SetState(OceanSurfaceState.TransitionToDay);
                    break;
                case TimeOfDay.EveningTransition:
                    targetAlpha = SetState(OceanSurfaceState.TransitionToNight);
                    break;
                case TimeOfDay.DawnTransition:
                    targetAlpha = SetState(OceanSurfaceState.TransitionToDay);
                    break;
                default:
                    targetAlpha = SetState(OceanSurfaceState.Normal);
                    break;
            }

            if (ev.Transitions && Alpha != targetAlpha)
                mTweener.Reset(Alpha, targetAlpha, ev.TimeRemaining);
        }

        public override void Draw(GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            // No need to draw the skybox - we just use its texture to sample from.
#if false
            // Draw Sky
            support.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            support.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            mSkyEffect.Parameters["View"].SetValue(support.ViewMatrix);
            mSkyEffect.Parameters["Projection"].SetValue(support.ProjectionMatrix);
            mSkyEffect.Parameters["cubeTex"].SetValue(mSkyTex);

            foreach (EffectPass pass in mSkyEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                mSky.Draw(gameTime, support, Matrix.Identity);
            }

            support.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            support.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
#endif
            // Draw Water
            base.Draw(gameTime, support, parentTransform);
        }

        public void CustomOceanDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            SPEffecter effecter = support.CurrentEffecter;

            effecter.EffectParameterForKey("Local").SetValue(mSurface.TransformationMatrix);
            effecter.EffectParameterForKey("World").SetValue(parentTransform);
            effecter.EffectParameterForKey("View").SetValue(support.ViewMatrix);
            effecter.EffectParameterForKey("Projection").SetValue(support.ProjectionMatrix);
            effecter.EffectParameterForKey("EyePos").SetValue(mEyePos);

            // choose and set the ocean textures
            int oceanTexIndex = ((int)(mElapsedTime) % 4);

            effecter.EffectParameterForKey("normalTex").SetValue(mSurfaceNormalMaps[(oceanTexIndex + 1) % 4].Texture);
            effecter.EffectParameterForKey("normalTex2").SetValue(mSurfaceNormalMaps[(oceanTexIndex) % 4].Texture);
            //effect.Parameters["maskTex"].SetValue(mShorelineMask.Texture);
            effecter.EffectParameterForKey("textureLerp").SetValue((((((float)mElapsedTime) - (int)(mElapsedTime)) * 2 - 1) * 0.5f) + 0.5f);

            // set the alpha
            effecter.EffectParameterForKey("oceanAlpha").SetValue(Alpha);

            // set the time used for moving waves
            effecter.EffectParameterForKey("time").SetValue((float)mElapsedTime * 0.025f);

            // set the sky texture
            effecter.EffectParameterForKey("cubeTex").SetValue(mSkyTex);

            support.AddPrimitive(mSurface, Matrix.Identity);
        }

        public override void AdvanceTime(double time)
        {
            mTweener.AdvanceTime(time);
            if (Alpha != mTweener.TweenedValue)
                Alpha = mTweener.TweenedValue;
#if true
            mElapsedTime += time;
#else
            // Development purposes.
            GamePadState state = GameController.GC.GamePadState;
            float vel = 5f;

            // X-Axis Rotation
            if (state.DPad.Up == ButtonState.Pressed)
            {
                mEyePos += vel * mEyeRot.Forward;
                Debug.WriteLine(mEyePos.ToString());
            }
            else if (state.DPad.Down == ButtonState.Pressed)
            {
                mEyePos -= vel * mEyeRot.Forward;
                Debug.WriteLine(mEyePos.ToString());
            }

            Vector2 vec = state.ThumbSticks.Right;

            if (vec.X != 0 || vec.Y != 0)
                mEyeRot = Matrix.CreateRotationY(vec.X * (SPMacros.PI / 180)) * Matrix.CreateRotationX(vec.Y * (SPMacros.PI / 180)) * mEyeRot;
#endif
        }
        #endregion
    }

    /*
     * -- Original Skymap --
     * Subtle: X: -1365.068, Y: 118.7962, Z: -669.4024
     * Subtle2: X: 443.2458, Y: 444.1096, Z: -419.9092
     * Reflective: X: 0, Y: 0, Z: -1185
     * Reflective2: X: 0, Y: 0, Z: 1835
     * Reflective3: X: -2081.776, Y: 0, Z: -1927.677
     * Reflective4: X: -1932.294, Y: -302.7159, Z: -1788.957
     * 
     * 
     * 
     */
}
