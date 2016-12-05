using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    interface IReusable
    {
        bool InUse { get; }
        uint ReuseKey { get; }
        int PoolIndex { get; set; }

        void Reuse();
        void Hibernate();
    }
}
