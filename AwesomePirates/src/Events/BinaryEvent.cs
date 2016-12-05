using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    delegate void BinaryEventHandler(BinaryEvent ev);

    class BinaryEvent : SPEvent
    {
        public const string CUST_EVENT_TYPE_BINARY = "binaryEvent";

        public BinaryEvent(string type, bool value, bool bubbles = false)
            : base(type, bubbles)
        {
            mValue = value;
        }

        private bool mValue;
        public bool BinaryValue { get { return mValue; } set { mValue = value; } }
    }
}
