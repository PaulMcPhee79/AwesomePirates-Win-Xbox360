using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class SafeAreaView : Prop, IInteractable
    {
        public const string CUST_EVENT_TYPE_DISPLAY_ADJUSTMENT_COMPLETED = "displayAdjustmentCompletedEvent";
        public const string CUST_EVENT_TYPE_DISPLAY_ADJUSTED_UP = "displayAdjustedUpEvent";
        public const string CUST_EVENT_TYPE_DISPLAY_ADJUSTED_DOWN = "displayAdjustedDownEvent";

        private const float kScreenWidth = 320f;
        private const float kScreenHeight = 240f;
        private const float kAspectRatio = kScreenWidth / kScreenHeight;

        public SafeAreaView(int category)
            : base(category)
        {
            mCostume = null;
            SetupProp();

            mScene.SubscribeToInputUpdates(this);
        }

        #region Fields
        private SPQuad mScreen;
        private SPImage mContentImage;
        private SPSprite mContent;
        private SPButton mDoneButton;
        private SPSprite mCostume;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_MENU_DISPLAY_ADJUST; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;

            // Costume
            mCostume = new SPSprite();
            AddChild(mCostume);

            // Black screen
            float aspectRatio = mScene.ViewAspectRatio;
            if (kScreenHeight * aspectRatio > kScreenWidth)
            {
                // Scene's height a smaller portion. Use our max width and scale down our height
                mScreen = new SPQuad(kScreenWidth, kScreenWidth / aspectRatio);
            }
            else
            {
                // Scene's height is a larger portion. Use our max height and scale down our width
                mScreen = new SPQuad(kScreenHeight * aspectRatio, kScreenHeight);
            }

            mScreen.X = 320 + (kScreenWidth - mScreen.Width) / 2;
            mScreen.Y = 136 + (kScreenHeight - mScreen.Height) / 2;
            mScreen.Color = Color.Black;
            mCostume.AddChild(mScreen);

            // Title
            SPTextField title = new SPTextField(350, 64, "Display Adjustment", mScene.FontKey, 38);
            title.X = mScreen.X + (mScreen.Width - title.Width) / 2;
            title.Y = mScreen.Y - title.Height;
            title.HAlign = SPTextField.SPHAlign.Center;
            title.VAlign = SPTextField.SPVAlign.Top;
            title.Color = Color.Black;
            mCostume.AddChild(title);

            // Dpads
                // Left (+)
            SPImage dPad = new SPImage(mScene.TextureByName("large_dpad_up"));
            dPad.X = 186;
            dPad.Y = 176;
            mCostume.AddChild(dPad);

            SPTextField dPadLabel = new SPTextField(64, 96, "+", mScene.FontKey, 84);
            dPadLabel.X = dPad.X + (dPad.Width - dPadLabel.Width) / 2;
            dPadLabel.Y = dPad.Y + dPad.Height;
            dPadLabel.HAlign = SPTextField.SPHAlign.Center;
            dPadLabel.VAlign = SPTextField.SPVAlign.Center;
            dPadLabel.Color = Color.Black;
            mCostume.AddChild(dPadLabel);

                // Right (-)
            dPad = new SPImage(mScene.TextureByName("large_dpad_down"));
            dPad.X = 680;
            dPad.Y = 176;
            mCostume.AddChild(dPad);

            dPadLabel = new SPTextField(64, 96, "-", mScene.FontKey, 84);
            dPadLabel.X = dPad.X + (dPad.Width - dPadLabel.Width) / 2;
            dPadLabel.Y = dPad.Y + dPad.Height - 10;
            dPadLabel.HAlign = SPTextField.SPHAlign.Center;
            dPadLabel.VAlign = SPTextField.SPVAlign.Center;
            dPadLabel.Color = Color.Black;
            mCostume.AddChild(dPadLabel);

            // Instructions
            SPTextField instructions = new SPTextField(570, 80, "Use the Dpad to maximize the game's viewable region for your display.", mScene.FontKey, 32);
            instructions.X = 200;
            instructions.Y = 374;
            instructions.HAlign = SPTextField.SPHAlign.Center;
            instructions.VAlign = SPTextField.SPVAlign.Top;
            instructions.Color = Color.Black;
            mCostume.AddChild(instructions);

            // Press A when done
            SPTextField done = new SPTextField(420, 56, "Press       when done", mScene.FontKey, 42);
            done.X = 270;
            done.Y = 496;
            done.HAlign = SPTextField.SPHAlign.Center;
            done.VAlign = SPTextField.SPVAlign.Top;
            done.Color = Color.Black;
            mCostume.AddChild(done);

            mDoneButton = new SPButton(mScene.TextureByName("large_face_a"));
            mDoneButton.X = done.X + 124;
            mDoneButton.Y = done.Y - 8;
            mDoneButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnDoneButtonPressed);
            mCostume.AddChild(mDoneButton);

            // Screen Content
            mContent = new SPSprite();
            mContent.X = mScreen.X + mScreen.Width / 2;
            mContent.Y = mScreen.Y + mScreen.Height / 2;
            mContent.ScaleX = mContent.ScaleY = mScene.SafeAreaFactor;
            mCostume.AddChild(mContent);

            SPStage stage = GameController.GC.Stage;
            stage.RenderSupport.SuspendRendering(true);

            mScene.EnableScreenshotMode(true);
            SPRenderTexture renderTexture = new SPRenderTexture(
                GameController.GC.GraphicsDevice,
                mScene.EffectForKey("RenderTexturedQuad"),
                mScene.EffectForKey("RenderColoredQuad"),
                stage.Width,
                stage.Height);
            renderTexture.BundleDrawCalls(delegate(SPRenderSupport support)
            {
                stage.Draw(null, support, Matrix.Identity);
            });
            mScene.EnableScreenshotMode(false);
            stage.RenderSupport.SuspendRendering(false);
            mScene.SafeAreaFactor = mScene.SafeAreaFactor;

            float safeAreaFactor = mScene.SafeAreaFactor;
            mContentImage = new SPImage(renderTexture.Texture);
            mContentImage.Scale = new Vector2((mScreen.Width + 1) / (mContentImage.Width * safeAreaFactor), (mScreen.Height + 1) / (mContentImage.Height * safeAreaFactor));
            mContentImage.X = -mContentImage.Width / 2;
            mContentImage.Y = -mContentImage.Height / 2;
            mContent.AddChild(mContentImage);
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            bool didAdjust = false;
            ControlsManager cm = ControlsManager.CM;

            if (cm.DidButtonDepress(Buttons.DPadUp))
            {
                didAdjust = true;
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_DISPLAY_ADJUSTED_UP));
            }
            else if (cm.DidButtonDepress(Buttons.DPadDown))
            {
                didAdjust = true;
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_DISPLAY_ADJUSTED_DOWN));
            }

            if (cm.DidButtonDepress(Buttons.A))
            {
                mDoneButton.AutomatedButtonDepress();
            }
            else if (cm.DidButtonRelease(Buttons.A))
            {
                mDoneButton.AutomatedButtonRelease();
            }

            if (didAdjust && mContent != null)
            {
                mContent.ScaleX = mContent.ScaleY = mScene.SafeAreaFactor;
            }
        }

        private void OnDoneButtonPressed(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_DISPLAY_ADJUSTMENT_COMPLETED));
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mScene.UnsubscribeToInputUpdates(this);

                        if (mDoneButton != null)
                        {
                            mDoneButton.RemoveEventListener(CUST_EVENT_TYPE_DISPLAY_ADJUSTMENT_COMPLETED, (SPEventHandler)OnDoneButtonPressed);
                            mDoneButton = null;
                        }

                        RemoveAllChildren();

                        if (mContentImage != null)
                        {
                            SPTexture texture = mContentImage.Texture;

                            if (texture != null)
                            {
                                mContentImage.Texture = null;

                                if (texture.Texture != null && !texture.Texture.IsDisposed)
                                    texture.Texture.Dispose();
                            }

                            mContentImage = null;
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
