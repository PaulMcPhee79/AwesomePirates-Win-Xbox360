using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class Cloud : Prop
    {
        public enum CloudType
        {
            TypeOne = 0,
            TypeTwo,
            TypeThree
        }

        private const float kCloudShadowOffsetMax = 160.0f;

        public Cloud(CloudType cloudType, float velX, float velY, float vapourAlpha)
            : base(-1)
        {
            mCloudType = cloudType;
		    mVelX = velX;
		    mVelY = velY;
            mVapourAlpha = vapourAlpha;
        }
        
        #region Fields
        private CloudType mCloudType;
        private float mVelX;
        private float mVelY;
        private float mVapourAlpha;

        private float mVapourHalfWidth;
        private float mVapourHalfHeight;
        private float mShadowHalfWidth;
        private float mShadowHalfHeight;

        private float mShadowOffsetX;
        private float mShadowOffsetY;
        private Prop mVapour;
        private Prop mShadow;
        #endregion

        #region Properties
        public float ShadowOffsetX { get { return mShadowOffsetX; } set { mShadowOffsetX = value; } }
        public float ShadowOffsetY { get { return mShadowOffsetY; } set { mShadowOffsetY = value; } }
        public override SPRectangle Bounds { get { return mVapour.Bounds.UnionWithRectangle(mShadow.Bounds); } }
        public bool IsBlownOffscreen
        {
            get
            {
                float leftMost = Math.Min(mVapour.X - mVapourHalfWidth, mShadow.X - mShadowHalfWidth);
                float rightMost = Math.Max(mVapour.X + mVapourHalfWidth, mShadow.X + mShadowHalfWidth);
                float topMost = Math.Min(mVapour.Y - mVapourHalfHeight, mShadow.Y - mShadowHalfHeight);
                float bottomMost = Math.Max(mVapour.Y + mVapourHalfHeight, mShadow.Y + mShadowHalfHeight);

                return ((mVelX > 0 && leftMost > mScene.ViewWidth) ||
                        (mVelX < 0 && rightMost < 0) ||
                        (mVelY > 0 && topMost > mScene.ViewHeight) ||
                        (mVelY < 0 && bottomMost < 0));
            }
        }
        public static CloudType RandomCloudType { get { return (CloudType)GameController.GC.NextRandom((int)CloudType.TypeOne, (int)CloudType.TypeThree); } }
        #endregion

        #region Methods
        public void SetupCloud()
        {
            // Water Vapour Mist
            mVapour = new Prop(PFCat.CLOUDS);
            mVapour.Alpha = mVapourAlpha;

            SPImage image = new SPImage(mScene.TextureByName("cloud" + (int)mCloudType));
            image.X = -image.Width / 2;
            image.Y = -image.Height / 2;
            mVapour.AddChild(image);
            mVapour.ScaleX = 2.5f;
            mVapour.ScaleY = 2.5f;

            mVapourHalfWidth = mVapour.Width / 2;
            mVapourHalfHeight = mVapour.Height / 2;

            // Shadow
            mShadow = new Prop(PFCat.CLOUD_SHADOWS);
            mShadow.Alpha = 0.3f;

            image = new SPImage(mScene.TextureByName("cloud" + (int)mCloudType));
            image.X = -image.Width / 2;
            image.Y = -image.Height / 2;
            image.Color = Color.Black;
            image.Alpha = 0.375f;
            mShadow.AddChild(image);
            mShadow.ScaleX = 4f;
            mShadow.ScaleY = 4f;

            mShadowHalfWidth = mShadow.Width / 2;
            mShadowHalfHeight = mShadow.Height / 2;

            mScene.AddProp(mVapour);
            mScene.AddProp(mShadow);

            // First, position vapour and shadow relatively so we get an accurate bounding box.
            mShadow.X = mShadowOffsetX * kCloudShadowOffsetMax;
            mShadow.Y = mShadowOffsetY * kCloudShadowOffsetMax;

            float x, y;
            SPRectangle bbox = Bounds;

            if (mVelX > 0)
                x = GameController.GC.NextRandom((int)-mScene.ViewWidth / 2, (int)mScene.ViewWidth / 2);
            else
                x = GameController.GC.NextRandom((int)mScene.ViewWidth / 2, (int)(1.5f * mScene.ViewWidth));

            if (mVelY > 0)
                y = -bbox.Height / 2;
            else
                y = mScene.ViewHeight + bbox.Height / 2;
            mVapour.X = x;
            mVapour.Y = y;
            mShadow.X = x + mShadowOffsetX * kCloudShadowOffsetMax;
            mShadow.Y = y + mShadowOffsetY * kCloudShadowOffsetMax;
        }

        public override void AdvanceTime(double time)
        {
            mVapour.X += mVelX * (float)time;
            mVapour.Y += mVelY * (float)time;
            mShadow.X = mVapour.X + mShadowOffsetX * kCloudShadowOffsetMax;
            mShadow.Y = mVapour.Y + mShadowOffsetY * kCloudShadowOffsetMax;

            mShadow.ScaleX = mShadow.ScaleY = 1.1f * mVapour.ScaleX + 1.25f * mShadowOffsetY;
            //mShadow.ScaleY = mVapour.ScaleY + 1.5f * mShadowOffsetY;
            mShadowHalfWidth = mShadow.Width / 2;
            mShadowHalfHeight = mShadow.Height / 2;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mVapour != null)
                        {
                            mScene.RemoveProp(mVapour);
                            mVapour = null;
                        }

                        if (mShadow != null)
                        {
                            mScene.RemoveProp(mShadow);
                            mShadow = null;
                        }
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
