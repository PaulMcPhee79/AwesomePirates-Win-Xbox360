using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    struct Score
    {
        public const string kDefaultScoreName = "Cheeky Mammoth";

        public Score(int value = 0, string name = kDefaultScoreName)
        {
            this.rank = -1;
            this.value = value;
            this.name = name;
        }

        public int rank;
        public int value;
        public string name;
    }
}
