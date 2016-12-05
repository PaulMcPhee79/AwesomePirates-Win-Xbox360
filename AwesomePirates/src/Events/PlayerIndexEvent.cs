using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using SparrowXNA;

namespace AwesomePirates
{
    delegate void PlayerIndexEventHandler(PlayerIndexEvent ev);

    class PlayerIndexEvent : SPEvent
    {
        public PlayerIndexEvent(string type, PlayerIndex playerIndex, bool bubbles = false)
            : base(type, bubbles)
        {
            mPlayerIndex = playerIndex;
        }

        private PlayerIndex mPlayerIndex;
        public PlayerIndex PlayerIndex { get { return mPlayerIndex; } }

        public void ReuseWithIndex(PlayerIndex playerIndex)
        {
            mPlayerIndex = playerIndex;
        }
    }
}
