using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using System.Text;
using System.Globalization;
using SparrowXNA;

namespace AwesomePirates
{
    enum PFCat
    {

        SEA = 0,
        WAKES,
        SK_WAKES,
        BLOOD,
        WAVES,
        POOLS,
        SURFACE,
        LAND,
        SHOREBREAK,
        BUILDINGS,
        SK_HUD,
        PICKUPS,
        POINT_MOVIES,
        NPC_SHIPS,
        PLAYABLE_SHIPS,
        EXPLOSIONS,
        DIALOGS,
        CLOUD_SHADOWS,
        CLOUDS,
        COMBAT_TEXT,
        DECK,
        HUD,
        GUIDE
    }

    class Globals
    {
        public enum TextureFold
        {
            Horiz = 1,
            Vert = 2,
            Quad = 3
        }

        public const uint kNavNone = 0x0;
        public const uint kNavVertical = 0x1;
        public const uint kNavHorizontal = 0x2;
        public const uint kNavAll = 0x3;

        public const int OVERBOARD_SCORE_BONUS = 500;
        public const string CC_FONT_NAME = "CCFont";
        public const float VOODOO_DESPAWN_DURATION = 3f;
        public const float CRIT_FACTOR = 1.25f;

        // Crowd-control Ashes
        public const uint ASH_SPELL_ACID_POOL =  (1 << 0);
        public const uint ASH_SPELL_MAGMA_POOL = (1 << 1);

        // Crowd-control Ash Proc durations
        public const float ASH_DURATION_ACID_POOL = (20.0f + VOODOO_DESPAWN_DURATION);
        public const float ASH_DURATION_MAGMA_POOL = (20.0f + VOODOO_DESPAWN_DURATION);

        private static char s_NumberGroupSeparator = ',';
        private static StringBuilder s_StrBuilder = new StringBuilder(16, 16);

        public static char NumberGroupSeparator
        {
            get { return s_NumberGroupSeparator; }
            set
            {
                if ((int)value == 0xa0) // Convert non-breaking space (0xa0) to space (0x20)
                    value = ' ';
                if (value == ' ' || value == '.' || value == ',')
                    s_NumberGroupSeparator = value;
            }
        }

        private static StringBuilder CommaSeparatedValueU(uint value, StringBuilder builder)
        {
            if (value < 1000)
                return builder.Concat(value);
            else
                return Globals.CommaSeparatedValueU(value / 1000, builder).Append(s_NumberGroupSeparator).Concat(value % 1000, 3);
        }

        private static StringBuilder CommaSeparatedValueS(int value, StringBuilder builder)
        {
            if (value < 1000)
                return builder.Concat(value);
            else
                return Globals.CommaSeparatedValueS(value / 1000, builder).Append(s_NumberGroupSeparator).Concat(value % 1000, 3);
        }

        public static string CommaSeparatedValue(uint value)
        {
            s_StrBuilder.Length = 0;
            return CommaSeparatedValueU(value, s_StrBuilder).ToString();
        }

        public static string CommaSeparatedValue(int value)
        {
            s_StrBuilder.Length = 0;
            return CommaSeparatedValueS(value, s_StrBuilder).ToString();
        }

        public static StringBuilder CommaSeparatedValue(uint value, StringBuilder builder)
        {
            if (builder == null)
                return null;

            builder.Length = 0;
            return CommaSeparatedValueU(value, builder);
        }

        public static StringBuilder CommaSeparatedValue(int value, StringBuilder builder)
        {
            if (builder == null)
                return null;

            builder.Length = 0;
            return CommaSeparatedValueS(value, builder);
        }

        public static string FormatElapsedTime(double time)
        {
            int mins,secs,ms;
	
	        mins = (int)(time / 60);
	        time -= mins * 60;
	        secs = (int)time;
	        time -= secs;
	        ms = (int)(time * 1000);
            s_StrBuilder.Length = 0;
            return s_StrBuilder.Concat(mins, 1).Append(':').Concat(secs, 2).Append(':').Concat(ms, 3).ToString();
        }

        public static string SuffixForRank(int rank)
        {
            int rankMod = rank % 10;
            string suffix = "th";

            if (((rank % 100) / 10) != 1)
            {
                if (rankMod == 1)
                    suffix = "st";
                else if (rankMod == 2)
                    suffix = "nd";
                else if (rankMod == 3)
                    suffix = "rd";
            }

            return suffix;
        }

        public static float ConvertToSingle(object obj)
        {
            if (obj == null)
                return 0f;
            else
                return Convert.ToSingle(obj, CultureInfo.InvariantCulture);
        }

        public static float ConvertToSingle(string str)
        {
            if (str == null)
                return 0f;
            else
                return Convert.ToSingle(str, CultureInfo.InvariantCulture);
        }

        public static void RotatePointThroughAngle(ref Vector2 point, float angle)
        {
	        float cosAngle = (float)Math.Cos(angle), sinAngle = (float)Math.Sin(angle);
	
	        float x = cosAngle * point.X - sinAngle * point.Y;
	        point.Y = sinAngle * point.X + cosAngle * point.Y;
	        point.X = x;
        }

        public static SPTexture FoldoutTexture(SPTexture texture, TextureFold settings)
        {
            if (texture == null)
                return null;

            float width = MathHelper.Max(texture.Width, texture.Frame.Width), height = MathHelper.Max(texture.Height, texture.Frame.Height);
	
	        if ((settings & TextureFold.Horiz) == TextureFold.Horiz)
		        width *= 2f;
	        if ((settings & TextureFold.Vert) == TextureFold.Vert)
		        height *= 2f;
	
	        SPRenderTexture renderTexture = new SPRenderTexture(
                GameController.GC.GraphicsDevice,
                GameController.GC.TextureManager.EffectForKey("RenderTexturedQuad"),
                GameController.GC.TextureManager.EffectForKey("RenderColoredQuad"),
                width,
                height);
	        renderTexture.BundleDrawCalls(delegate(SPRenderSupport support)
            {
		        SPImage image = new SPImage(texture);

		        for (int i = 0; i < 4; ++i)
                {	
			        switch (i)
                    {
				        case 0:
					        // Always do first quadrant
					        image.X = 0;
					        image.Y = 0;
					        image.ScaleX = 1f;
					        image.ScaleY = 1f;
					        break;
				        case 1:
					        if ((settings & TextureFold.Horiz) == 0)
						        continue;
					        image.X = 2 * image.Width;
					        image.Y = 0;
					        image.ScaleX = -1f;
					        image.ScaleY = 1f;
					        break;
				        case 2:
					        if ((settings & TextureFold.Vert) == 0)
						        continue;
					        image.X = 0;
					        image.Y = 2 * image.Height;
					        image.ScaleX = 1f;
					        image.ScaleY = -1f;
					        break;
				        case 3:
					        if ((settings & TextureFold.Quad) != TextureFold.Quad)
						        continue;
					        image.X = 2 * image.Width;
					        image.Y = 2 * image.Height;
					        image.ScaleX = -1f;
					        image.ScaleY = -1f;
					        break;
			        }
                    image.Draw(null, support, Matrix.Identity);
		        }
	        });
	
	        return renderTexture.Texture;
        }

        public static SPTexture WholeTextureFromQuarter(SPTexture texture)
        {
            return Globals.FoldoutTexture(texture, TextureFold.Quad);
        }

        public static SPTexture WholeTextureFromHalfHoriz(SPTexture texture)
        {
            return Globals.FoldoutTexture(texture, TextureFold.Horiz);
        }

        public static SPTexture WholeTextureFromHalfVert(SPTexture texture)
        {
            return Globals.FoldoutTexture(texture, TextureFold.Vert);
        }

        public static Vector2 AngleToVector(float angle)
        {
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        public static float VectorToAngle(Vector2 vector)
        {
            float angle = (float)Math.Atan2(vector.X, -vector.Y);

            if (angle < 0)
                angle += SPMacros.TWO_PI;
            return angle;
        }

        public static float VectorToAngle2(Vector2 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }
    }
}
