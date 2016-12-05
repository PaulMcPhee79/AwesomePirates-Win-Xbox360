using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class Locale
    {
        private static char s_NumberDecimalSeparator = '.';
        private const char kUnknownReplaceChar = '?';
        private const string kEllipses = "...";
        private static StringBuilder s_StrBuilder = new StringBuilder(128, 128);

        public static char NumberDecimalSeparator
        {
            get { return s_NumberDecimalSeparator; }
            set
            {
                if ((int)value == 0xa0) // Convert non-breaking space (0xa0) to space (0x20)
                    value = ' ';
                if (value == ' ' || value == '.' || value == ',')
                    s_NumberDecimalSeparator = value;
            }
        }

        // Returns text unchanged if its display length is <= maxLen. Else it shortens the text to
        // display within the display length and adds ellipses to indicate that it has been shortened.
        public static string SanitizeTextForDisplay(string text, SpriteFont font, float maxLen)
        {
            if (text == null)
                return null;

            s_StrBuilder.Length = 0;
            s_StrBuilder.Append(text);

            if (font.MeasureString(s_StrBuilder).X > maxLen)
            {
                float ellipsesLen = font.MeasureString(kEllipses).X;
                while ((font.MeasureString(s_StrBuilder).X + ellipsesLen) > maxLen && s_StrBuilder.Length > 0)
                    s_StrBuilder.Remove(s_StrBuilder.Length - 1, 1);
                s_StrBuilder.Append(kEllipses);
            }

            return s_StrBuilder.ToString();
        }

        public static string SanitizeText(string text, string fontName, int fontSize)
        {
            if (fontName == null || fontSize <= 0 || text == null || text.Length == 0)
                return "";

            s_StrBuilder.Length = 0;
            s_StrBuilder.Append(text, 0, Math.Min(text.Length, s_StrBuilder.Capacity));

            for (int i = 0; i < s_StrBuilder.Length; ++i)
            {
                if (!SPTextField.RegisteredFontContainsChar(s_StrBuilder[i], fontName, fontSize))
                    s_StrBuilder.Replace(s_StrBuilder[i], kUnknownReplaceChar);
            }

            return s_StrBuilder.ToString();
        }

        public static void SanitizeText(StringBuilder sb, string fontName, int fontSize)
        {
            if (sb == null || fontName == null || fontSize <= 0)
                return;

            for (int i = 0; i < sb.Length; ++i)
            {
                if (!SPTextField.RegisteredFontContainsChar(sb[i], fontName, fontSize))
                    sb.Replace(sb[i], kUnknownReplaceChar);
            }
        }

        public static string SanitizedFloat(float value, string format, string fontName, int fontSize)
        {
            s_StrBuilder.Length = 0;

            if (fontName != null)
            {
                s_StrBuilder.Append(value.ToString(format));
                for (int i = 0; i < s_StrBuilder.Length; ++i)
                {
                    if (!SPTextField.RegisteredFontContainsChar(s_StrBuilder[i], fontName, fontSize))
                        s_StrBuilder.Replace(s_StrBuilder[i], s_NumberDecimalSeparator);
                }
            }

            return s_StrBuilder.ToString();
        }
    }
}
