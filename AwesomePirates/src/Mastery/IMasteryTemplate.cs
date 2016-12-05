using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    interface IMasteryTemplate
    {
        uint MasteryBitmap { get; set; }
        float ApplyScoreBonus(float score, ShipActor ship);
        float ApplyScoreBonus(float score, OverboardActor prisoner);
    }
}
