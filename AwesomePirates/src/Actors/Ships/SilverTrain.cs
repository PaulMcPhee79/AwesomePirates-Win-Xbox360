using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class SilverTrain : PrimeShip
    {
        public SilverTrain(ActorDef def, string key)
            : base(def, key)
        {

        }

        public override void CreditPlayerSinker()
        {
            if (mScene.GameMode == GameMode.Career)
                mScene.AchievementManager.SilverTrainSunk(this);
            else
                base.CreditPlayerSinker();
        }
    }
}
