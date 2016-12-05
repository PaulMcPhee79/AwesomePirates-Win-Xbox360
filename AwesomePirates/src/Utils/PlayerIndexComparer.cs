using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AwesomePirates
{
    class PlayerIndexComparer : IEqualityComparer<PlayerIndex>
    {
        public static readonly PlayerIndexComparer Instance = new PlayerIndexComparer();

        #region IEqualityComparer<PlayerIndexComparer> Members
        public bool Equals(PlayerIndex x, PlayerIndex y)
        {
            return (x == y);
        }

        public int GetHashCode(PlayerIndex obj)
        {
            return (int)obj;
        }

        public static bool Contains(List<PlayerIndex> playerIndexes, PlayerIndex obj)
        {
            if (playerIndexes == null)
                return false;

            foreach (PlayerIndex playerIndex in playerIndexes)
            {
                if (playerIndex == obj)
                    return true;
            }

            return false;
        }
        #endregion
    }
}
