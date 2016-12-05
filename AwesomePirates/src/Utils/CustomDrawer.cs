using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class CustomDrawer : Prop, ISPAnimatable
    {
        public CustomDrawer()
            : base(-1)
        {
            mAnimKey = SPJuggler.NextAnimKey();
            mElapsedTime = 0;
            mMetronome = 0.4;
            mMetronomeDir = 0.525;
            mRefractionFactor = 0.05f;
            mDisplacementTextures = new SPTexture[] { mScene.TextureByName("refraction-sml"), mScene.TextureByName("refraction") };
        }

        #region Fields
        private uint mAnimKey;
        private double mElapsedTime;
        private double mMetronome;
        private double mMetronomeDir;
        private float mRefractionFactor;
        private Vector4 mDisplacementFactor;
        private SPTexture[] mDisplacementTextures;
        #endregion

        #region Properties
        public object Target { get { return null; } }
        public bool IsComplete { get { return false; } }
        public uint AnimKey { get { return mAnimKey; } }
        public float RefractionFactor { set { mRefractionFactor = value; } }
        public Vector4 DisplacementFactor { set { mDisplacementFactor = value; } }
        #endregion

        public void RefractionDrawSml(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            RefractionDraw(mDisplacementTextures[0], displayObject, gameTime, support, parentTransform);
        }

        public void RefractionDrawLge(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            RefractionDraw(mDisplacementTextures[1], displayObject, gameTime, support, parentTransform);
        }

        private void RefractionDraw(SPTexture displacementTex, SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (displayObject is SPQuad == false)
#if DEBUG
                throw new ArgumentException("CustomDrawer.RefractionDraw can only operate on an SPQuads.");
#else
                return;
#endif

            SPEffecter effecter = support.CurrentEffecter;
            SPQuad quad = displayObject as SPQuad;

            //effect.Parameters["Local"].SetValue(displayObject.TransformationMatrix);
            //effect.Parameters["World"].SetValue(parentTransform);
            //effect.Parameters["View"].SetValue(support.ViewMatrix);
            //effect.Parameters["Projection"].SetValue(support.ProjectionMatrix);

            Vector2 displacementScroll = MoveInCircle(mElapsedTime, mRefractionFactor);
            SPRectangle texCoords = quad.Texture.TexCoords;

            effecter.EffectParameterForKey("Dimensions").SetValue(new Vector2(texCoords.Width, texCoords.Height));
            effecter.EffectParameterForKey("DisplacementScroll").SetValue(displacementScroll);
            effecter.EffectParameterForKey("DisplacementFactor").SetValue(mDisplacementFactor);
            effecter.EffectParameterForKey("tex").SetValue(quad.Texture.Texture);
            effecter.EffectParameterForKey("displacementTex").SetValue(displacementTex.Texture);

            Matrix globalTransform = quad.TransformationMatrix * parentTransform;
            globalTransform = globalTransform * support.ViewMatrix;
            globalTransform = globalTransform * support.ProjectionMatrix;
            support.AddPrimitive(quad, globalTransform);
        }

        public void PotionDrawSml(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            PotionDraw(mDisplacementTextures[0], displayObject, gameTime, support, parentTransform);
        }

        public void PotionDrawLge(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            PotionDraw(mDisplacementTextures[1], displayObject, gameTime, support, parentTransform);
        }

        private void PotionDraw(SPTexture displacementTex, SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (displayObject is SPQuad == false)
#if DEBUG
                throw new ArgumentException("CustomDrawer.PotionDraw can only operate on an SPQuads.");
#else
                return;
#endif

            SPEffecter effecter = support.CurrentEffecter;
            SPQuad quad = displayObject as SPQuad;
            double totalSeconds = gameTime.TotalGameTime.TotalSeconds;

            effecter.EffectParameterForKey("Local").SetValue(displayObject.TransformationMatrix);
            effecter.EffectParameterForKey("World").SetValue(parentTransform);
            effecter.EffectParameterForKey("View").SetValue(support.ViewMatrix);
            effecter.EffectParameterForKey("Projection").SetValue(support.ProjectionMatrix);

            float factor = support.CurrentEffecter.Factor;
            Vector2 displacementScroll = new Vector2((float)Math.Sin(mElapsedTime * factor / 10), (float)(mElapsedTime + mElapsedTime * (factor / 3f)) * mRefractionFactor);
            SPRectangle texCoords = quad.Texture.TexCoords;

            effecter.EffectParameterForKey("Dimensions").SetValue(new Vector2(texCoords.Width, texCoords.Height));
            effecter.EffectParameterForKey("DisplacementScroll").SetValue(displacementScroll);
            effecter.EffectParameterForKey("tex").SetValue(quad.Texture.Texture);
            effecter.EffectParameterForKey("displacementTex").SetValue(displacementTex.Texture);

            support.AddPrimitive(quad, Matrix.Identity);
        }

        public void AggregatePotionDrawSml(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            AggregatePotionDraw(mDisplacementTextures[0], displayObject, gameTime, support, parentTransform);
        }

        public void AggregatePotionDrawLge(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            AggregatePotionDraw(mDisplacementTextures[1], displayObject, gameTime, support, parentTransform);
        }

        private void AggregatePotionDraw(SPTexture displacementTex, SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (displayObject is SPQuad == false)
#if DEBUG
                throw new ArgumentException("CustomDrawer.AggregatePotionDraw can only operate on an SPQuads.");
#else
                return;
#endif

            SPEffecter effecter = support.CurrentEffecter;
            SPQuad quad = displayObject as SPQuad;
            double totalSeconds = gameTime.TotalGameTime.TotalSeconds;

            float factor = support.CurrentEffecter.Factor;
            Vector2 displacementScroll = new Vector2((float)Math.Sin(mElapsedTime * factor / 10), (float)(mElapsedTime + mElapsedTime * (factor / 3f)) * mRefractionFactor);
            SPRectangle texCoords = quad.Texture.TexCoords;

            effecter.EffectParameterForKey("Dimensions").SetValue(new Vector2(texCoords.Width, texCoords.Height));
            effecter.EffectParameterForKey("DisplacementScroll").SetValue(displacementScroll);
            effecter.EffectParameterForKey("tex").SetValue(quad.Texture.Texture);
            effecter.EffectParameterForKey("displacementTex").SetValue(displacementTex.Texture);

            Matrix globalTransform = quad.TransformationMatrix * parentTransform;
            globalTransform = globalTransform * support.ViewMatrix;
            globalTransform = globalTransform * support.ProjectionMatrix;
            support.AddPrimitive(quad, globalTransform);
        }

        public void HighlightDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            SPEffecter effecter = support.CurrentEffecter;
            effecter.EffectParameterForKey("Local").SetValue(displayObject.TransformationMatrix);
            effecter.EffectParameterForKey("World").SetValue(parentTransform);
            effecter.EffectParameterForKey("View").SetValue(support.ViewMatrix);
            effecter.EffectParameterForKey("Projection").SetValue(support.ProjectionMatrix);
            effecter.EffectParameterForKey("metronome").SetValue((float)mMetronome * support.CurrentEffecter.Factor);

            if (displayObject is SPQuad)
            {
                SPQuad quad = displayObject as SPQuad;
                effecter.EffectParameterForKey("tex").SetValue(quad.Texture.Texture);
                support.AddPrimitive(quad, Matrix.Identity);
            }
            else if (displayObject is SPTextField)
            {
                SPTextField textField = displayObject as SPTextField;
                effecter.EffectParameterForKey("tex").SetValue(mScene.TextureByName("clear-texture").Texture);
                support.AddText(textField, Matrix.Identity);
            }
        }

        public override void AdvanceTime(double time)
        {
            mElapsedTime += time;
            mMetronome += mMetronomeDir * time;

            if (mMetronome > 0.75)
            {
                mMetronomeDir = -0.525; // 0.375;
                //mMetronome = (int)(mMetronome + 1) - mMetronome;
            }
            else if (mMetronome < 0.4)
            {
                mMetronomeDir = 0.525;
                //mMetronome = (int)mMetronome - mMetronome;
            }
        }

        /// <summary>
        /// Helper calculates the destination rectangle needed
        /// to draw a sprite to one quarter of the screen.
        /// </summary>
        private Rectangle QuarterOfScreen(int x, int y)
        {
            int w = (int)(mScene.ViewWidth / 2);
            int h = (int)(mScene.ViewHeight / 2);

            return new Rectangle(w * x, h * y, w, h);
        }


        /// <summary>
        /// Helper calculates the destination position needed
        /// to center a sprite in the middle of the screen.
        /// </summary>
        private Vector2 CenterOnScreen(Texture2D texture)
        {
            int x = (int)((mScene.ViewWidth - texture.Width) / 2);
            int y = (int)((mScene.ViewHeight - texture.Height) / 2);

            return new Vector2(x, y);
        }


        /// <summary>
        /// Helper computes a value that oscillates over time.
        /// </summary>
        private static float Pulsate(double gameTime, float speed, float min, float max)
        {
            double time = gameTime * speed;

            return min + ((float)Math.Sin(time) + 1) / 2 * (max - min);
        }


        /// <summary>
        /// Helper for moving a value around in a circle.
        /// </summary>
        private static Vector2 MoveInCircle(double gameTime, float speed)
        {
            double time = gameTime * speed;

            float x = (float)Math.Cos(time);
            float y = (float)Math.Sin(time);

            return new Vector2(x, y);
        }


        /// <summary>
        /// Helper for moving a sprite around in a circle.
        /// </summary>
        private Vector2 MoveInCircle(double gameTime, Texture2D texture, float speed)
        {

            float x = (mScene.ViewWidth - texture.Width) / 2;
            float y = (mScene.ViewHeight - texture.Height) / 2;

            return MoveInCircle(gameTime, speed) * 128 + new Vector2(x, y);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mDisplacementTextures = null;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
    }
}
