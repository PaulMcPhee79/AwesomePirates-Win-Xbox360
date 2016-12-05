using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    public class ResManager
    {
        public enum ResAlignment
        {
            None = 0,
            LowerLeft,
            LowerRight,
            LowerCenter,
            Center,
            CenterLeft,
            CenterRight,
            UpperCenter,
            UpperLeft,
            UpperRight
        }

        // Must base it off 960x640. All resource files (physics etc) are written for this original resolution.
        public const float kResStandardWidth = 960f;
        public const float kResStandardHeight = 640f;

#if XBOX
        private const float kResMaxUnscaledWidth = 1024f; // 1152f;
        private const float kResMaxUnscaledHeight = 600f; //576f; // 648f;

        public static float OUTPUT_WIDTH = 1152f;
        public static float OUTPUT_HEIGHT = 648f;
#else
        private const float kResMaxUnscaledWidth = 1024f; // 1152f;
        private const float kResMaxUnscaledHeight = 600f; // 648f;
#endif

        public static int RES_BACKBUFFER_WIDTH = (int)kResMaxUnscaledWidth;
        public static int RES_BACKBUFFER_HEIGHT = (int)kResMaxUnscaledHeight;
        private static ResManager s_ResManager = null;

        public static ResManager RESM
        {
            get
            {
                if (s_ResManager == null)
                    s_ResManager = new ResManager(RES_BACKBUFFER_WIDTH, RES_BACKBUFFER_HEIGHT);
                return s_ResManager;
            }
        }
        public static float RESX(float x) { return RESM.ResX(x); }
        public static float RESY(float y) { return RESM.ResY(y); }
        public static float RESW { get { return RESM.Width; } }
        public static float RESH { get { return RESM.Height; } }
        public static float CUSTX { get { return RESM.Width - kResStandardWidth; } }
        public static float CUSTY { get { return RESM.Height - kResStandardHeight; } }
        public static float RITMFX(float x) { return RESM.ResItemFx(x); }
        public static float RITMFY(float y) { return RESM.ResItemFy(y); }
        public static float RITMFXY(float xy) { return (RITMFX(xy) + RITMFY(xy)) / 2; }
        public static ResOffset RES_OFFSET { get { return RESM.Offset; } }
        public static bool IS_CUST_RES { get { return RESM.IsCustRes; } }
        public static void RES_SETX(SPDisplayObject displayObject, float x) { if (displayObject != null) displayObject.X = x + RES_OFFSET.X; }
        public static void RES_SETY(SPDisplayObject displayObject, float y) { if (displayObject != null) displayObject.Y = y + RES_OFFSET.Y; }

        // Physics Res Helpers
        public const float PPM = 16f; // Pixels per meter

        public static float M2P(float a) { return a * PPM; } // Meters to pixels ratio
        public static float P2M(float a) { return a / PPM; } // Pixels to meters ratio

        public static float M2PX(float x) { return x * PPM; } // Meters to Pixels X-Axis
        public static float M2PY(float y) { return RESH - y * PPM; } // Meters to Pixels Y-Axis

        public static float P2MX(float x) { return x / PPM; } // Pixels to Meters X-Axis
        public static float P2MY(float y) { return (RESH - y) / PPM; } // Pixels to meters Y-Axis

        private ResManager(float width, float height)
        {
            mIsCustRes = false;
            ResOffset.IsCustRes = mIsCustRes;

#if XBOX
            mWidth = OUTPUT_WIDTH;
            mHeight = OUTPUT_HEIGHT;

            mResScaleX = width / OUTPUT_WIDTH;
            mResScaleY = height / OUTPUT_HEIGHT;
#else

            mWidth = width;
            mHeight = height;

            mResScaleX = 1;
            mResScaleY = 1;

            /*
            mWidth = width;
            mHeight = height;

            if (mWidth > kResMaxUnscaledWidth || mHeight > kResMaxUnscaledHeight)
            {
                float widthScale = mWidth / kResMaxUnscaledWidth, heightScale = mHeight / kResMaxUnscaledHeight;

                if (widthScale > 1f && heightScale > 1f)
                    mResScaleX = mResScaleY = Math.Min(widthScale, heightScale);
                else if (widthScale > 1f)
                    mResScaleX = mResScaleY = widthScale;
                else
                    mResScaleX = mResScaleY = heightScale;

                mWidth = (float)Math.Round(mWidth / mResScaleX); //, MidpointRounding.AwayFromZero);
                mHeight = (float)Math.Round(mHeight / mResScaleY); //, MidpointRounding.AwayFromZero);
            }
            else if (mWidth < kResStandardWidth || mHeight < kResStandardHeight)
            {
                float widthScale = mWidth / kResStandardWidth, heightScale = mHeight / kResStandardHeight;

                if (widthScale < 1f && heightScale < 1f)
                    mResScaleX = mResScaleY = Math.Min(widthScale, heightScale);
                else if (widthScale < 1f)
                    mResScaleX = mResScaleY = widthScale;
                else
                    mResScaleX = mResScaleY = heightScale;

                mWidth = (float)Math.Round(mWidth / mResScaleX); //, MidpointRounding.AwayFromZero);
                mHeight = (float)Math.Round(mHeight / mResScaleY); //, MidpointRounding.AwayFromZero);
            }
            else
            {
                mResScaleX = mResScaleY = 1;
            }
            */
#endif

            mResFactorBGX = 1;
            mResFactorBGY = 1;
            mResFactorItemX = 1;
            mResFactorItemY = 1;
            mOffsetStack = new ResOffsetStack();
            mDefaultOffset = new ResOffset(0, 0, 0, 0);
            PushOffset(mDefaultOffset);
        }

        #region Fields
        private bool mIsCustRes;
        private float mWidth;
        private float mHeight;
        private float mResScaleX;
        private float mResScaleY;
        private float mResFactorBGX;
        private float mResFactorBGY;
        private float mResFactorItemX;
        private float mResFactorItemY;
        private ResOffset mDefaultOffset;
        private ResOffsetStack mOffsetStack;

        private float mGameFactorArea = 1f;
        private float mGameFactorWidth = 1f;
        private float mGameFactorHeight = 1f;
        #endregion

        #region Properties
        public bool IsCustRes
        {
            get { return mIsCustRes; }
            set
            {
                if (value)
                {
                    mResFactorBGX = 1f;
                    mResFactorBGY = 1f;
                    mResFactorItemX = mWidth / kResStandardWidth;
                    mResFactorItemY = mHeight / kResStandardHeight;
                }
                else
                {
                    mResFactorBGX = 1f;
                    mResFactorBGY = 1f;
                    mResFactorItemX = 1f;
                    mResFactorItemY = 1f;
                }

                mIsCustRes = value;
                ResOffset.IsCustRes = value;
            }
        }
        public float Width { get { return mWidth; } set { mWidth = value; } }
        public float Height { get { return mHeight; } set { mHeight = value; } }
        public float ScaleX { get { return mResScaleX; } set { mResScaleX = value; } }
        public float ScaleY { get { return mResScaleY; } set { mResScaleY = value; } }
        public float HudScale { get { return Math.Min(1.15f, (mWidth * mResScaleX) / (kResStandardWidth + (kResMaxUnscaledWidth - kResStandardWidth) / 2)); } }
        public float HudScaleW { get { return HudScale; } }
        public float HudScaleH { get { return Math.Min(1.15f, (mHeight * mResScaleY) / (kResStandardHeight + (kResMaxUnscaledHeight - kResStandardHeight) / 2)); } }
        public ResOffset Offset { get { return mOffsetStack.Offset; } }
        public float GameFactorArea { get { return mGameFactorArea; } set { mGameFactorArea = value; } }
        public float GameFactorWidth { get { return mGameFactorWidth; } set { mGameFactorWidth = value; } }
        public float GameFactorHeight { get { return mGameFactorHeight; } set { mGameFactorHeight = value; } }
        //public float GameFactor { get { return Math.Max(1, Math.Min(1.2f, (mWidth * mResScaleX) / (kResStandardWidth + (kResMaxUnscaledWidth - kResStandardWidth) / 2))); } }
        //public float GameFactorLge { get { return Math.Max(1, Math.Min(1.3f, (mWidth * mResScaleX) / (kResStandardWidth + (kResMaxUnscaledWidth - kResStandardWidth) / 2))); } }
        #endregion

        #region Methods
        public float ResX(float x)
        {
            return x + Offset.X;
        }

        public float ResY(float y)
        {
            return y + Offset.Y;
        }

        public float ResBGFx(float x)
        {
            return x * mResFactorBGX;
        }

        public float ResBGFy(float y)
        {
            return y * mResFactorBGY;
        }

        public float ResItemFx(float x)
        {
            return x * mResFactorItemX;
        }

        public float ResItemFy(float y)
        {
            return y * mResFactorItemY;
        }

        public ResOffset PushOffset(ResOffset offset)
        {
            return mOffsetStack.Push(offset);
        }

        public ResOffset PushBGOffsetWithAlignment(ResAlignment alignment)
        {
            return PushOffset(BGOffsetWithAlignment(alignment));
        }

        public ResOffset PushItemOffsetWithAlignment(ResAlignment alignment)
        {
            return PushOffset(ItemOffsetWithAlignment(alignment));
        }

        public void PopOffset()
        {
            if (mOffsetStack.Count > 1)
                mOffsetStack.Pop();
        }

        public ResOffset BGOffsetWithAlignment(ResAlignment alignment)
        {
            float x = 0, y = 0, custX = 0, custY = 0;
            float xOffset = CUSTX, yOffset = CUSTY;

	        switch (alignment)
            {
		        case ResAlignment.None: break;
                case ResAlignment.LowerLeft: x = 0; y = -yOffset; break;
                case ResAlignment.LowerRight: x = -xOffset; y = -yOffset; break;
                case ResAlignment.LowerCenter: x = -xOffset / 2; y = -yOffset; break;
                case ResAlignment.Center: x = -xOffset / 2; y = -yOffset / 2; break;
                case ResAlignment.CenterLeft: x = 0; y = -yOffset / 2; break;
                case ResAlignment.CenterRight: x = -xOffset; y = -yOffset / 2; break;
                case ResAlignment.UpperCenter: x = -xOffset / 2; y = 0; break;
		        case ResAlignment.UpperLeft: x = 0; y = 0; break;
                case ResAlignment.UpperRight: x = -xOffset; y = 0; break;
		        default:
			        break;
	        }
	
            return new ResOffset(x, y, custX, custY);
        }

        public ResOffset ItemOffsetWithAlignment(ResAlignment alignment)
        {
            float x = 0, y = 0, custX = 0, custY = 0;
            float xOffset = CUSTX, yOffset = CUSTY;
	
	        switch (alignment)
            {
		        case ResAlignment.None: break;
                case ResAlignment.LowerLeft: custX = 0; custY = yOffset; break;
                case ResAlignment.LowerRight: custX = xOffset; custY = yOffset; break;
                case ResAlignment.LowerCenter: custX = xOffset / 2; custY = yOffset; break;
                case ResAlignment.Center: custX = xOffset / 2; custY = yOffset / 2; break;
                case ResAlignment.CenterLeft: custX = 0; custY = yOffset / 2; break;
                case ResAlignment.CenterRight: custX = xOffset; custY = yOffset / 2; break;
		        case ResAlignment.UpperCenter: custX = xOffset / 2; custY = 0; break;
		        case ResAlignment.UpperLeft: custX = 0; custY = 0; break;
                case ResAlignment.UpperRight: custX = xOffset; custY = 0; break;
		        default:
			        break;
	        }

            return new ResOffset(x, y, custX, custY);
        }
        #endregion
    }
}
