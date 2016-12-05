using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class TownDock : DockActor
    {
        public TownDock(ActorDef def)
            : base(def)
        {

        }

        public override void RespondToPhysicalInputs()
        {
            foreach (Actor actor in mContacts.EnumerableSet)
            {
                if (actor is NpcShip)
                    DockShip(actor as NpcShip);
            }
        }

        private void DockShip(NpcShip ship)
        {
            if (ship != null)
                ship.Dock();
        }

        public override void PrepareForNewGame()
        {
            // Do nothing
        }
    }
}
