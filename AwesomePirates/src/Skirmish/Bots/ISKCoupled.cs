using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    interface ISKCoupled
    {
        float CoupledTurnForce { get; }
        float CoupledSailForce { get; }
        void CopyPhysicsFrom(ISKCoupled couple);
        void UpdateAppearance(double time);
        void AdvanceTimeProxy(double time);
    }
}
