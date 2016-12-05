using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class QuadColorer
    {
        private SPQuad mQuad;
        private List<string>[] mProperties;

        public QuadColorer(SPQuad quad)
        {
            mQuad = quad;
            mProperties = new List<string>[4];

            for (int i = 0; i < mProperties.Length; ++i)
            {
                mProperties[i] = new List<string>();
                mProperties[i].Add("VertexColorR" + i);
                mProperties[i].Add("VertexColorG" + i);
                mProperties[i].Add("VertexColorB" + i);
            }
        }

        public byte VertexColorR0
        {
            get { return mQuad.ColorAtVertex(0).R; }
            set { SetColorRAtVertex(value, 0); }
        }
        public byte VertexColorG0
        {
            get { return mQuad.ColorAtVertex(0).G; }
            set { SetColorGAtVertex(value, 0); }
        }
        public byte VertexColorB0
        {
            get { return mQuad.ColorAtVertex(0).B; }
            set { SetColorBAtVertex(value, 0); }
        }

        public byte VertexColorR1
        {
            get { return mQuad.ColorAtVertex(1).R; }
            set { SetColorRAtVertex(value, 1); }
        }
        public byte VertexColorG1
        {
            get { return mQuad.ColorAtVertex(1).G; }
            set { SetColorGAtVertex(value, 1); }
        }
        public byte VertexColorB1
        {
            get { return mQuad.ColorAtVertex(1).B; }
            set { SetColorBAtVertex(value, 1); }
        }

        public byte VertexColorR2
        {
            get { return mQuad.ColorAtVertex(2).R; }
            set { SetColorRAtVertex(value, 2); }
        }
        public byte VertexColorG2
        {
            get { return mQuad.ColorAtVertex(2).G; }
            set { SetColorGAtVertex(value, 2); }
        }
        public byte VertexColorB2
        {
            get { return mQuad.ColorAtVertex(2).B; }
            set { SetColorBAtVertex(value, 2); }
        }

        public byte VertexColorR3
        {
            get { return mQuad.ColorAtVertex(3).R; }
            set { SetColorRAtVertex(value, 3); }
        }
        public byte VertexColorG3
        {
            get { return mQuad.ColorAtVertex(3).G; }
            set { SetColorGAtVertex(value, 3); }
        }
        public byte VertexColorB3
        {
            get { return mQuad.ColorAtVertex(3).B; }
            set { SetColorBAtVertex(value, 3); }
        }

        private void SetColorRAtVertex(byte colorPart, int vertex)
        {
            Color color = mQuad.ColorAtVertex(vertex);
            color.R = colorPart;
            mQuad.SetColor(color, vertex);
        }

        private void SetColorGAtVertex(byte colorPart, int vertex)
        {
            Color color = mQuad.ColorAtVertex(vertex);
            color.G = colorPart;
            mQuad.SetColor(color, vertex);
        }

        private void SetColorBAtVertex(byte colorPart, int vertex)
        {
            Color color = mQuad.ColorAtVertex(vertex);
            color.B = colorPart;
            mQuad.SetColor(color, vertex);
        }

        public void AnimateVertexColor(Color color, int vertex, SPTween tween)
        {
            List<string> properties = mProperties[vertex];
            tween.AnimateProperty(properties[0], color.R);
            tween.AnimateProperty(properties[1], color.G);
            tween.AnimateProperty(properties[2], color.B);
        }
    }
}
