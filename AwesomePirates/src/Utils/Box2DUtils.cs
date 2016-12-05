using System;
using Microsoft.Xna.Framework;
using Box2D.XNA;

namespace AwesomePirates
{
    class Box2DUtils
    {
        public static void RotateVector(ref Vector2 v, float angle)
        {
            float cosAngle = (float)Math.Cos(angle), sinAngle = (float)Math.Sin(angle);
			
			float x = cosAngle * v.X - sinAngle * v.Y;
			v.Y = sinAngle * v.X + cosAngle * v.Y;
			v.X = x;
        }

        public static float SignedAngle(ref Vector2 v1, ref Vector2 v2)
        {
            return FastMath.FastAtan2(Box2D.XNA.MathUtils.Cross(v1, v2), Vector2.Dot(v1, v2));
        }
    }
}
