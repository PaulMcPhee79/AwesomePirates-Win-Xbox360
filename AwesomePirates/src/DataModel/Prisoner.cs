using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class Prisoner : Hostage
    {
        public Prisoner(string name)
            : base(name)
        {
            mPlanked = false;
            mInfamyBonus = Globals.OVERBOARD_SCORE_BONUS;
        }

        private bool mPlanked;
        private int mInfamyBonus;

        public bool Planked { get { return mPlanked; } set { mPlanked = value; } }
        public override int InfamyBonus { get { return mInfamyBonus; } set { mInfamyBonus = value; } }
    }
}
