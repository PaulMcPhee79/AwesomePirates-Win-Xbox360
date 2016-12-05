using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class SXGauge : SPSprite
    {
        public enum SXGaugeOrientation
        {
            Horizontal = 0,
            Vertical
        }

        public SXGauge(SPTexture texture, SXGaugeOrientation orientation)
        {
            mRatio = 1.0f;
            mOrientation = orientation;
            mQuad = new SPQuad(texture);
            AddChild(mQuad);
        }

        private SXGaugeOrientation mOrientation;
        private SPQuad mQuad;
        private float mRatio;

        public float Ratio
        {
            get { return mRatio; }
            set
            {
                if (mRatio != value)
                {
                    mRatio = Math.Max(0f, Math.Min(1f, value));
                    Update();
                }
            }
        }
        public Color Color { get { return mQuad.Color; } set { mQuad.Color = value; } }

        public void SetTexture(SPTexture texture)
        {
            if (texture != null)
                mQuad.Texture = texture;
        }

        private void Update()
        {
            if (mOrientation == SXGaugeOrientation.Horizontal)
            {
                mQuad.ScaleX = mRatio;
                mQuad.SetTexCoord(new Vector2(mRatio, 0), 1);
                mQuad.SetTexCoord(new Vector2(mRatio, 1), 3);
            }
            else
            {
                mQuad.ScaleY = 1;
        
                float maxHeight = mQuad.Height;
                mQuad.ScaleY = mRatio;
                mQuad.Y = (maxHeight - mQuad.Height);

                mQuad.SetTexCoord(new Vector2(0, 1.0f-mRatio), 0);
                mQuad.SetTexCoord(new Vector2(1, 1.0f-mRatio), 1);
            }
        }
    }
}
