using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class TreasureFleet : PrimeShip
    {
        public TreasureFleet(ActorDef def, string key)
            : base(def, key)
        {

        }

        public override void CreditPlayerSinker()
        {
            if (mScene.GameMode == GameMode.Career)
                mScene.AchievementManager.TreasureFleetSunk(this);
            else
                base.CreditPlayerSinker();
        }

        protected override void SailWithForce(float force)
        {
            // Slow down when entering the town so that we can enter more orderly
            if (Destination.FinishIsDest && mDestination.Finish == ActorAi.kPlaneIdTown)
                base.SailWithForce(0.75f * force);
            else
                base.SailWithForce(force);
        }

    }
}
