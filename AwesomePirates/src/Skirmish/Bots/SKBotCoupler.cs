using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class SKBotCoupler : Prop, IInteractable
    {
        public SKBotCoupler(int category, PlayerIndex playerIndex, SKBotShip botShip)
            : base(category)
        {
            mAdvanceable = true;
            mPlayerIndex = playerIndex;
            mSKBotShip = botShip;

            string shipKey = "Pirate";
            ShipDetails shipDetails = ShipDetails.GetNpcShipDetails(shipKey);
            ResOffset offset = ResManager.RESM.ItemOffsetWithAlignment(ResManager.ResAlignment.LowerRight);
            //ActorDef actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, ResManager.P2MX(938 + offset.X), ResManager.P2MY(410 + offset.Y), SPMacros.SP_D2R(45));
            ActorDef actorDef = ShipFactory.Factory.CreateSkirmishShipDefForShipType("SkirmishShip", ResManager.P2MX(938 + offset.X), ResManager.P2MY(410 + offset.Y), SPMacros.SP_D2R(45));

            //PirateShip ship = new PirateShip(actorDef, shipKey);
            mSKPursuitShip = new SKPursuitShip(actorDef, "Pirate");
            mSKPursuitShip.ShipDetails = shipDetails;
            mSKPursuitShip.CannonDetails = CannonDetails.GetCannonDetails("Perisher");
            mSKPursuitShip.AiModifier = 1.5f;
            mSKPursuitShip.SetupShip();
            mSKPursuitShip.Destination = new Destination();
            mSKPursuitShip.Visible = false;
            mSKPursuitShip.B2Body.SetActive(false);
            mSKPursuitShip.AddEventListener(SKPursuitShip.CUST_EVENT_TYPE_SK_BOT_CANNON_FIRED, (SPEventHandler)OnBotCannonFired);
        }

        #region Fields
        protected bool mIsPassive = false;
        protected double mLaunchTimer = 5.0;
        protected PlayerIndex mPlayerIndex;
        protected SKBotShip mSKBotShip;
        protected SKPursuitShip mSKPursuitShip;
        #endregion

        #region Properties
        protected bool IsPassive
        {
            get { return mIsPassive; }
            set
            {
                mIsPassive = value;

                if (value && mLaunchTimer < 2.0)
                    mLaunchTimer = 2.0;
            }
        }
        public uint InputFocus { get { return InputManager.HAS_FOCUS_DECK; } }
        public SKBotShip BotShip { get { return mSKBotShip; } }
        public SKPursuitShip PursuitShip { get { return mSKPursuitShip; } }
        #endregion

        #region Methods
        public void DidGainFocus() { }
        public void WillLoseFocus() { }

        public override void AdvanceTime(double time)
        {
            if (mSKBotShip == null || mSKBotShip.B2Body == null || mSKPursuitShip == null || mSKPursuitShip.B2Body == null)
                return;

            // Return early if we're still in the Cove launch queue.
            if (!mSKBotShip.B2Body.IsActive())
                return;
            else if (!mSKPursuitShip.B2Body.IsActive())
            {
                if (mLaunchTimer <= 0)
                {
                    mSKPursuitShip.B2Body.SetActive(true);
                    mScene.AddActor(mSKPursuitShip);
                    mSKPursuitShip.RequestNewDestination();
                }
            }

            if (IsPassive || mLaunchTimer > 0 || (mSKBotShip.X < 0f || mSKBotShip.X > mScene.ViewWidth) || (mSKBotShip.Y < 0f || mSKBotShip.Y > mScene.ViewHeight))
            {
                mLaunchTimer -= time;
                mSKBotShip.AdvanceTimeProxy(time);
                mSKPursuitShip.CopyPhysicsFrom(mSKBotShip);
            }
            else
            {
                mSKPursuitShip.AdvanceTimeProxy(time);
                mSKBotShip.CopyPhysicsFrom(mSKPursuitShip);
                mSKBotShip.UpdateAppearance(time);
            }
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            ControlsManager cm = ControlsManager.CM;
            GamePadState gps = cm.GamePadStateForPlayer(mPlayerIndex);
            IsPassive = gps.IsButtonDown(Buttons.LeftTrigger) || gps.IsButtonDown(Buttons.RightTrigger) || gps.IsButtonDown(Buttons.A);
            IsPassive = IsPassive || (Math.Abs(gps.ThumbSticks.Left.X + gps.ThumbSticks.Left.Y) > 0.1f) || (Math.Abs(gps.ThumbSticks.Right.X + gps.ThumbSticks.Right.Y) > 0.1f);
        }

        protected virtual void OnBotCannonFired(SPEvent ev)
        {
            mSKBotShip.FireCannons();
        }
        #endregion
    }
}
