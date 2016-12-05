using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AwesomePirates
{
    class TimeOfDayComparer : IEqualityComparer<TimeOfDay>
    {
        public static readonly TimeOfDayComparer Instance = new TimeOfDayComparer();

        #region IEqualityComparer<TimeOfDayComparer> Members
        public bool Equals(TimeOfDay x, TimeOfDay y)
        {
            return (x == y);
        }

        public int GetHashCode(TimeOfDay obj)
        {
            return (int)obj;
        }
        #endregion
    }
}
