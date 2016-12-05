using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class Helm : Prop
    {
        public Helm(float rotationIncrement, bool visual = true)
            : base(PFCat.DECK)
        {
            mVisual = visual;
            mActivated = true;
            mFlyingDutchman = false;
            mSpeedboat = false;
            mFlyingDutchmanTexture = mScene.TextureByName("ghost-helm");
            mSpeedboatTexture = mScene.TextureByName("8-Speedboat-helm");

            mHelmRotation = 0f;
            mRotationIncrement = rotationIncrement;
            mRotationCeiling = SPMacros.TWO_PI;
            mPrevThumbstickVector = Vector2.Zero;

            mLeftKey = Keys.Left;
            mRightKey = Keys.Right;
            SetupProp();
        }

        #region Fields
        private bool mVisual;
        private bool mFlyingDutchman;
        private bool mSpeedboat;
        private bool mActivated;
        private SPTexture mFlyingDutchmanTexture;
        private SPTexture mSpeedboatTexture;
        private SPSprite mWheel;
        private SPImage mWheelImage;
        private float mRotationIncrement;
        private float mPreviousRotation;
        private float mHelmRotation;
        private float mRotationCeiling;
        private Vector2 mPrevThumbstickVector;
        private Keys mLeftKey;
        private Keys mRightKey;
        #endregion

        #region Properties
        public bool Activated { get { return mActivated; } set { mActivated = value; } }
        public Keys LeftKey { get { return mLeftKey; } set { mLeftKey = value; } }
        public Keys RightKey { get { return mRightKey; } set { mRightKey = value; } }
        public float TurnAngle
        {
            get
            {
                float angle = Math.Min(2f, Math.Abs(mHelmRotation / SPMacros.PI_HALF));
                return (mHelmRotation < 0f) ? -angle : angle;
            }
        }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (!mVisual)
                return;

            mWheel = new SPSprite();

            mWheelImage = new SPImage(mScene.TextureByName("7-Man-o'-War-helm"));
            mWheelImage.X = -mWheelImage.Width / 2;
            mWheelImage.Y = -mWheelImage.Height / 2;
            mWheel.AddChild(mWheelImage);
            AddChild(mWheel);
        }

        private void SwapSpeedboatTextures()
        {
            if (!mVisual)
                return;

            SPTexture swap = mWheelImage.Texture;
	        mWheelImage.Texture = mSpeedboatTexture;
	        mSpeedboatTexture = swap;
        }

        public void ActivateSpeedboat()
        {
            if (!mSpeedboat)
            {
		        mSpeedboat = true;
                SwapSpeedboatTextures();
	        }
        }

        public void DeactivateSpeedboat()
        {
            if (mSpeedboat)
            {
		        mSpeedboat = false;
                SwapSpeedboatTextures();
	        }
        }

        private void SwapFlyingDutchmanTextures()
        {
            if (!mVisual)
                return;

            SPTexture swap = mWheelImage.Texture;
	        mWheelImage.Texture = mFlyingDutchmanTexture;
	        mFlyingDutchmanTexture = swap;
        }

        public void ActivateFlyingDutchman()
        {
            if (!mFlyingDutchman)
            {
		        mFlyingDutchman = true;
                SwapFlyingDutchmanTextures();
	        }
        }

        public void DeactivateFlyingDutchman()
        {
            if (mFlyingDutchman)
            {
		        mFlyingDutchman = false;
                SwapFlyingDutchmanTextures();
	        }
        }

        public float AddRotation(float angle)
        {
            float newAngle = mHelmRotation + angle;

            if (Math.Abs(newAngle) > mRotationCeiling)
                newAngle *= mRotationCeiling / Math.Abs(newAngle);

            if (newAngle >= -mRotationCeiling && newAngle <= mRotationCeiling)
            {
                mHelmRotation = newAngle;
            }

            // Match display object to underlying helm rotation
            float halfHelmRotation = mHelmRotation / 2;

            if (mVisual && mWheel.Rotation != halfHelmRotation)
            {
                float diff = 0.1f * (halfHelmRotation - mWheel.Rotation);
                mWheel.Rotation += diff;

                //if (Math.Abs(halfHelmRotation - mWheel.Rotation) < Math.Abs(diff))
                //    mWheel.Rotation = halfHelmRotation;
            }

            return mHelmRotation; // mWheel.Rotation;
        }

        public void ResetRotation()
        {
            mHelmRotation = 0f;

            if (mVisual)
                mWheel.Rotation = mHelmRotation;
        }

        public void Update(GamePadState state)
        {
            if (!mActivated)
            {
                FractionalTurn(mPrevThumbstickVector);
                return;
            }

            if (state.DPad.Right == ButtonState.Pressed)
            {
                BinaryTurn(1);
                return;
            }
            else if (state.DPad.Left == ButtonState.Pressed)
            {
                BinaryTurn(-1);
                return;
            }

            float leftStickX = Math.Abs(state.ThumbSticks.Left.X), rightStickX = Math.Abs(state.ThumbSticks.Right.X);
            if (leftStickX >= rightStickX)
            {
                FractionalTurn(state.ThumbSticks.Left);
                mPrevThumbstickVector = state.ThumbSticks.Left;
            }
            else
            {
                FractionalTurn(state.ThumbSticks.Right);
                mPrevThumbstickVector = state.ThumbSticks.Right;
            }
        }

        public void Update(KeyboardState state)
        {
            if (state.IsKeyDown(mRightKey)) BinaryTurn(1);
            else if (state.IsKeyDown(mLeftKey)) BinaryTurn(-1);
            else BinaryTurn(0);
        }

        private void BinaryTurn(int dir)
        {
            mRotationCeiling = SPMacros.TWO_PI;

            if (dir == 0)
            {
                mPreviousRotation = 0f;
                mHelmRotation = 0f;
                return;
            }

            if (mScene.Flipped)
                dir *= -1;

            if ((dir == 1 && mPreviousRotation < 0) || (dir == -1 && mPreviousRotation > 0))
                mHelmRotation = 0f;

            mPreviousRotation = dir * mRotationIncrement;
        }

        private void FractionalTurn(Vector2 axes)
        {
            if (SPMacros.SP_IS_FLOAT_EQUAL(0f, axes.X))
            {
                mPreviousRotation = 0f;
                mHelmRotation = 0f;
                return;
            }

            if (mScene.Flipped)
                axes *= -1;

            // Reset rotation on direction change
            if ((axes.X > 0 && mPreviousRotation < 0) || (axes.X < 0 && mPreviousRotation > 0))
                mHelmRotation = 0f;

            // Turn left or right
            mPreviousRotation = (axes.X > 0) ? mRotationIncrement : -mRotationIncrement;
            mRotationCeiling = Math.Abs(axes.X * SPMacros.TWO_PI);
        }

        public override void AdvanceTime(double time)
        {
            //if (mPreviousRotation != 0f)
            AddRotation(mPreviousRotation);
        }
        #endregion
    }
}
