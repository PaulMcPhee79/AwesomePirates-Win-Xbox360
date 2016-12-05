using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class SKHelper
    {
        public static Color ColorForTeamIndex(SKTeamIndex teamIndex)
        {
            Color color = Color.White;

            switch (teamIndex)
            {
                case SKTeamIndex.Red: color = Color.Red; break;
                case SKTeamIndex.Blue: color = Color.Blue; break;
                case SKTeamIndex.Green: color = Color.Green; break;
                case SKTeamIndex.Yellow: color = Color.Yellow; break;
            }

            return color;
        }

        public static Color ColorForTeamIndex(SKTeamIndex teamIndex, int positionInTeam)
        {
            Color color = Color.White;
            
            switch (teamIndex)
            {
                case SKTeamIndex.Red:
                    if (positionInTeam == 0)
                        color = Color.Red;
                    else
                        color = Color.Yellow;
                    break;
                case SKTeamIndex.Blue:
                    if (positionInTeam == 0)
                        color = Color.Blue;
                    else
                        color = Color.Green;
                    break;
                case SKTeamIndex.Green:
                    color = Color.Green;
                    break;
                case SKTeamIndex.Yellow:
                    color = Color.Yellow;
                    break;
            }

            return color;
        }

        public static string OffscreenArrowTextureNameForTeamIndex(SKTeamIndex teamIndex, int positionInTeam)
        {
            string texName = null;

            switch (teamIndex)
            {
                case SKTeamIndex.Red:
                    if (positionInTeam == 0)
                        texName = "sk-offscreen-arrow-p0";
                    else
                        texName = "sk-offscreen-arrow-p3";
                    break;
                case SKTeamIndex.Blue:
                    if (positionInTeam == 0)
                        texName = "sk-offscreen-arrow-p1";
                    else
                        texName = "sk-offscreen-arrow-p2";
                    break;
                case SKTeamIndex.Green:
                    texName = "sk-offscreen-arrow-p2";
                    break;
                case SKTeamIndex.Yellow:
                    texName = "sk-offscreen-arrow-p3";
                    break;
            }

            return texName;
        }

        public static string AvatarTextureNameForPlayerIndex(PlayerIndex playerIndex)
        {
            string texName = null;

            switch (playerIndex)
            {
                case PlayerIndex.One: texName = "sk-crew-0"; break;
                case PlayerIndex.Two: texName = "sk-crew-1"; break;
                case PlayerIndex.Three: texName = "sk-crew-2"; break;
                case PlayerIndex.Four: texName = "sk-crew-3"; break;
            }
            return texName;
        }

        public static string TrophyTextureNameForPosition(int position)
        {
            string texName = null;

            switch (position)
            {
                case 0: texName = "sk-trophy-0"; break;
                case 1: texName = "sk-trophy-1"; break;
                case 2: texName = "sk-trophy-2"; break;
                case 3: texName = "sk-trophy-3"; break;
            }

            return texName;
        }
    }
}
