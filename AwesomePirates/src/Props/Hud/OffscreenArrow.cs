using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class OffscreenArrow : Prop
    {
        public OffscreenArrow(Vector4 outOfRangeEdges, string textureName = "offscreen-arrow")
            : base((int)PFCat.HUD)
        {
            mOutOfRangeEdges = outOfRangeEdges;
            mEnabled = true;
		    mRealX = 0;
		    mRealY = 0;
		    mCachedArrowWidth = 0;
		    mCachedArrowHeight = 0;
            mArrowTextureName = textureName;
            SetupProp();
        }
        
        #region Fields
        private bool mEnabled;
        private float mRealX;
        private float mRealY;
        private float mCachedArrowWidth;
        private float mCachedArrowHeight;
        private string mArrowTextureName;
        private SPImage mArrowImage;
        private SPSprite mArrow;
        private SPSprite mCanvas;
        private Vector4 mOutOfRangeEdges;
        #endregion

        #region Properties
        public bool Enabled { get { return mEnabled; } set { mEnabled = value; } }
        public Color ArrowColor { get { return mArrowImage.Color; } set { mArrowImage.Color = value; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            X = mScene.ViewWidth / 2;
            Y = mScene.ViewHeight / 2;
    
            mCanvas = new SPSprite();
            mCanvas.X = -X;
            mCanvas.Y = -Y;
            AddChild(mCanvas);
    
	        mArrow = new SPSprite();
            mArrowImage = new SPImage(mScene.TextureByName(mArrowTextureName));
            mArrowImage.X = -mArrowImage.Width / 2;
            mArrowImage.Y = -mArrowImage.Height / 2;
            mArrow.AddChild(mArrowImage);
            mCanvas.AddChild(mArrow);
            Visible = false;
	
	        mCachedArrowWidth = mArrow.Width;
	        mCachedArrowHeight = mArrow.Height;
        }

        public void UpdateArrowLocation(float x, float y)
        {
            mRealX = x;
            mArrow.X = Math.Max(mCachedArrowWidth / 2, Math.Min(mScene.ViewWidth - mCachedArrowHeight / 2, x));
    
            mRealY = y;
            mArrow.Y = Math.Max(mCachedArrowHeight / 2, Math.Min(ResManager.RITMFY(mOutOfRangeEdges.W - 20f) - mCachedArrowHeight / 2, y));
    
            Visible = (Enabled && IsOutOfRange());
        }

        public void UpdateArrowRotation(float angle)
        {
            mArrow.Rotation = angle;
        }

        private bool IsOutOfRange()
        {
            return (mRealX < -mOutOfRangeEdges.X || mRealX > ResManager.RITMFX(mOutOfRangeEdges.Z) || mRealY < mOutOfRangeEdges.Y || mRealY > ResManager.RITMFY(mOutOfRangeEdges.W));
        }

        public override void Flip(bool enable)
        {
            ScaleX = (enable) ? -1 : 1;
        }
        #endregion
    }
}
