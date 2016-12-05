using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class NavyShip : PursuitShip
    {
        public NavyShip(ActorDef def, string key)
            : base(def, key)
        {
            mBootyGoneWanting = false;
        }

        public override void Reuse()
        {
            if (InUse)
                return;
            base.Reuse();

            mBootyGoneWanting = false;
        }

        public override void CreditPlayerSinker()
        {
            if (mScene.GameMode == GameMode.Career)
                mScene.AchievementManager.NavyShipSunk(this);
            else
                base.CreditPlayerSinker();
        }

        public override void PlayerCamouflageActivated(bool activated)
        {
            if (mPursuitEnded)
                return;

            if (activated)
            {
                if (DuelState != PursuitState.Ferrying)
                    DuelState = PursuitState.SailingToDock;
            }
            else
            {
                if (mEnemy != null)
                    DuelState = PursuitState.Chasing;
                else if (DuelState != PursuitState.Ferrying)
                    DuelState = PursuitState.Searching;
            }
        }
    }
}
