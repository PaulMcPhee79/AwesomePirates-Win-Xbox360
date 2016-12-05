using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    interface IResourceClient
    {
        void ResourceEventFiredWithKey(uint key, string type, object target);
    }
}
