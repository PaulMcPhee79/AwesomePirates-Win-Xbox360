using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Box2D.XNA;
using SparrowXNA;

namespace AwesomePirates
{
    class DockActor : Actor
    {
        public DockActor(ActorDef def)
            : base(def)
        {
            mCategory = (int)PFCat.PICKUPS;
            X = PX;
            Y = PY;
        }

        #region Methods
        private bool IgnoresContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            bool ignores = false;

            if (other is NpcShip)
            {
                NpcShip ship = other as NpcShip;

                if (ship.Feeler == fixtureOther)
                    ignores = true;
            }

            return ignores;
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (IgnoresContact(other, fixtureSelf, fixtureOther, contact))
                return;

            base.BeginContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (IgnoresContact(other, fixtureSelf, fixtureOther, contact))
                return;

            base.EndContact(other, fixtureSelf, fixtureOther, contact);
        }
        #endregion
    }
}
