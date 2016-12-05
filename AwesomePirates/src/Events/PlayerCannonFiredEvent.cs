using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    delegate void PlayerCannonFiredEventHandler(PlayerCannonFiredEvent ev);

    class PlayerCannonFiredEvent : SPEvent
    {
        public const string CUST_EVENT_TYPE_PLAYER_CANNON_FIRED = "cannonFired";

        public PlayerCannonFiredEvent(PlayerCannon cannon, bool bubbles = false)
            : base(CUST_EVENT_TYPE_PLAYER_CANNON_FIRED, bubbles)
        {
            mCannon = cannon;
        }

        private PlayerCannon mCannon;
        public PlayerCannon Cannon { get { return mCannon; } }
    }
}
