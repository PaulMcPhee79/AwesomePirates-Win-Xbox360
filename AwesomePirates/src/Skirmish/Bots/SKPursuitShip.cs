using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Box2D.XNA;
using SparrowXNA;

namespace AwesomePirates
{
    class SKPursuitShip : PirateShip, ISKCoupled
    {
        public const string CUST_EVENT_TYPE_SK_BOT_CANNON_FIRED = "skBotCannonFiredEvent";

        public SKPursuitShip(ActorDef def, string key)
            : base(def, key)
        {
            mCoupledTurnForce = 0;
            mCoupledSailForce = 0;
        }

        #region Fields
        protected float mCoupledTurnForce;
        protected float mCoupledSailForce;
        #endregion

        #region Properties
        public float CoupledTurnForce { get { return mCoupledTurnForce; } }
        public float CoupledSailForce { get { return mCoupledSailForce; } }
        #endregion

        protected override void TurnWithForce(float force)
        {
            base.TurnWithForce(force);
            mCoupledTurnForce = force;
        }

        protected override void SailWithForce(float force)
        {
            base.SailWithForce(force);
            mCoupledSailForce = force;
        }

        public override void SetupShip()
        {
            base.SetupShip();

            if (mWake != null)
                mWake.Visible = false;
        }

        public override void Dock()
        {
            return;
        }

        public override void Sink()
        {
            return;
        }

        public override Cannonball FireCannon(ShipDetails.ShipSide side, float trajectory)
        {
            mReloading = true;
            mReloadTimer = mReloadInterval;
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_BOT_CANNON_FIRED));
            return null;
        }

        public override void DisplayExplosionGlow()
        {
            // Do nothing
        }

        public void CopyPhysicsFrom(ISKCoupled couple)
        {
#if false
            TurnWithForce(couple.CoupledTurnForce);
            SailWithForce(couple.CoupledSailForce);
#else
            ShipActor ship = couple as ShipActor;
            mBody.SetLinearVelocity(ship.B2Body.GetLinearVelocity());
            mBody.SetAngularVelocity(ship.B2Body.GetAngularVelocity());
            Transform xf;
            ship.B2Body.GetTransform(out xf);
            mBody.SetTransform(xf.Position, xf.GetAngle());
#endif
        }

        public void UpdateAppearance(double time)
        {

        }

        public override void AdvanceTime(double time)
        {
            // Do nothing
        }

        public void AdvanceTimeProxy(double time)
        {
            base.AdvanceTime(time);
        }

        public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (other is SKBotShip || !(other is NpcShip || other is Cannonball))
                return false;
            else
                return base.PreSolve(other, fixtureSelf, fixtureOther, contact);
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (!(other is SKBotShip) && fixtureSelf != mFeeler && (other is NpcShip || other is Cannonball))
                base.BeginContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (!(other is SKBotShip) && fixtureSelf != mFeeler && (other is NpcShip || other is Cannonball))
                base.EndContact(other, fixtureSelf, fixtureOther, contact);
        }
    }
}
