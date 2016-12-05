using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    interface IIgnitable
    {
        bool Ignited { get; }
        void Ignite();
    }
}
