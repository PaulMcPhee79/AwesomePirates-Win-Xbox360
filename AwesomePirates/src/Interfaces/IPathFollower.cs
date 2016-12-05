using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    interface IPathFollower
    {
        bool IsCollidable { get; set; }
        Destination Destination { get; set; }

        void Dock();
    }
}
