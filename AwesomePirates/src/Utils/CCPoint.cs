using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AwesomePirates
{
    class CCPoint
    {
        public CCPoint(float x, float y)
        {
            pt.X = x;
            pt.Y = y;
        }

        private Vector2 pt;
        public float X { get { return pt.X; } set { pt.X = value; } }
        public float Y { get { return pt.Y; } set { pt.Y = value; } }
    }
}
