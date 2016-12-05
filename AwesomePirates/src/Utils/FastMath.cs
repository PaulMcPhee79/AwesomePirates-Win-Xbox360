using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class FastMath
    {
        private const int ATAN2_BITS = 7;
		private const int ATAN2_BITS2 = ATAN2_BITS << 1;
		private const int ATAN2_MASK = ~( -1 << ATAN2_BITS2);
		private const int ATAN2_COUNT = ATAN2_MASK + 1;
		private static readonly int ATAN2_DIM = (int)Math.Sqrt(ATAN2_COUNT);
		private static readonly float ATAN2_DIM_MINUS_1 = (((int)Math.Sqrt(ATAN2_COUNT)) - 1);
		
		private static bool s_isLutPrimed = false;
		private static float[] s_atan2lut = new float[ATAN2_COUNT];

        public static float FastAtan2(float y, float x)
		{
            if (x == 0 && y == 0)
                return (float)Math.Atan2(y, x);

			float add, mul;
    
            if (x < 0.0f)
            {
                if (y < 0.0f)
                {
                    x = -x;
                    y = -y;
                    mul = 1.0f;
                }
                else
                {
                    x = -x;
                    mul = -1.0f;
                }
        
                add = -3.141592653f;
            }
            else
            {
                if (y < 0.0f)
                {
                    y = -y;
                    mul = -1.0f;
                }
                else
                {
                    mul = 1.0f;
                }
        
                add = 0.0f;
            }

            float invDiv = ATAN2_DIM_MINUS_1 / ((x < y) ? y : x);
    
            int xi = (int)(x * invDiv);
            int yi = (int)(y * invDiv);
            int index = yi * ATAN2_DIM + xi;
    
            if (index < 0 || index > (ATAN2_COUNT - 1))
                return (float)Math.Atan2(y, x);
            return (s_atan2lut[index] + add) * mul;
		}
		
		public static void PrimeAtan2Lut()
		{
			if (s_isLutPrimed)
                return;
            s_isLutPrimed = true;
    
            for (int i = 0; i < ATAN2_DIM; ++i)
            {
                for (int j = 0; j < ATAN2_DIM; ++j)
                {
                    float x0 = (float)i / ATAN2_DIM;
                    float y0 = (float)j / ATAN2_DIM;

                    s_atan2lut[j * ATAN2_DIM + i] = (float)Math.Atan2(y0, x0);
                }
            }
		}
    }
}
