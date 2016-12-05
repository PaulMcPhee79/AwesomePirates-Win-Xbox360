using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AwesomePirates
{
    class SKTeamIndexComparer : IEqualityComparer<SKTeamIndex>
    {
        public static readonly SKTeamIndexComparer Instance = new SKTeamIndexComparer();

        #region IEqualityComparer<SKTeamIndexComparer> Members
        public bool Equals(SKTeamIndex x, SKTeamIndex y)
        {
            return (x == y);
        }

        public int GetHashCode(SKTeamIndex obj)
        {
            return (int)obj;
        }
        #endregion
    }
}
