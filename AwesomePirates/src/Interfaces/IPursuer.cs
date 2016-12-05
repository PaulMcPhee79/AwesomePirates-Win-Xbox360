using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    interface IPursuer
    {
        void PursueeDestroyed(ShipActor pursuee);
    }
}
