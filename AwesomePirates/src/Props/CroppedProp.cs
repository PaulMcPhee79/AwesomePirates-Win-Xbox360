using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class CroppedProp : Prop
    {
        public CroppedProp(int category, Rectangle viewableRegion)
            : base(category)
        {
            mRasterState = SPRenderSupport.NewDefaultRasterizerState;
            mRasterState.ScissorTestEnable = true;
            ViewableRegion = viewableRegion;
            mSPViewableRegion = new SPRectangle(viewableRegion);
        }

        #region Fields
        private Rectangle mViewableRegion;
        private SPRectangle mSPViewableRegion;
        private RasterizerState mRasterState;
        #endregion

        #region Properties
        public Rectangle ViewableRegion { get { return mViewableRegion; } set { mViewableRegion = value; mSPViewableRegion = new SPRectangle(value); } }
        #endregion

        #region Methods
        protected void VerifyViewableRegion()
        {
            bool contains = false;
            Rectangle viewBounds = GameController.GC.GraphicsDevice.Viewport.Bounds;
            viewBounds.Contains(ref mViewableRegion, out contains);

            if (!contains)
                ViewableRegion = new SPRectangle(viewBounds).IntersectionWithRectangle(mSPViewableRegion).ToRectangle();
        }

        public void ClampToContent()
        {
            SPRectangle contentBounds = BoundsInSpace(Stage);
            contentBounds.X += X;
            contentBounds.Y += Y;
            ViewableRegion = contentBounds.ToRectangle();
        }

        public override void Draw(GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            support.PushRasterState(mRasterState);
            VerifyViewableRegion();

            // FIXME: Some items draw beyond scissor rectangle when we restore the previous setting. Is it being overridden somewhere?
            Rectangle rect = support.GraphicsDevice.ScissorRectangle;
            support.GraphicsDevice.ScissorRectangle = mViewableRegion;
            base.Draw(gameTime, support, parentTransform);
            support.PopRasterState();
            support.GraphicsDevice.ScissorRectangle = rect;
        }
        #endregion
    }
}
