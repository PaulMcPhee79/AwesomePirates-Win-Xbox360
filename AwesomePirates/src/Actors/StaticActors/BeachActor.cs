using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class BeachActor : Actor
    {
        private enum BeachState
        {
            Idle = 0,
            Departing
        }

        public BeachActor(ActorDef def)
            : base(def)
        {
            if (def == null || def.fixtureDefCount != 6)
                throw new ArgumentException("Invalid ActorDef passed to BeachActor.");
            mAdvanceable = true;
            mCoveGate = def.fixtures[2];
            mDepartSensor = def.fixtures[4];
            mCoveSensor = def.fixtures[5];
            mDepartures = new Queue<PlayableShip>(4);
            mCoveFutureImage = null;
            mNightShade = null;
            mState = BeachState.Departing;
        }

        #region Fields
        private BeachState mState;

        private Fixture mCoveGate;
        private Fixture mDepartSensor;
        private Fixture mCoveSensor;

        private SPImage mCoveImage;
        private SPImage mCoveFutureImage;
        private Prop mCoveProp;
        private PlayableShip mDepartingShip;
        private Queue<PlayableShip> mDepartures;
        private NightShade mNightShade;
        #endregion

        #region Properties
        private BeachState State
        {
            get { return mState; }
            set
            {
                if (mState == value)
                    return;

                switch(value)
                {
                    case BeachState.Idle:
                        mDepartingShip = null;
                        break;
                    case BeachState.Departing:
                        LaunchNextShip();
                        break;
                }

                mState = value;
            }
        }
        public bool Hidden { set { Visible = !value; mCoveProp.Visible = !value; } }
        #endregion

        #region Methods
        public void SetupBeach()
        {
            SPImage sandImage = new SPImage(mScene.TextureByName("beach"));
            sandImage.X = mScene.ViewWidth - sandImage.Width;
            sandImage.Y = mScene.ViewHeight - sandImage.Height;
            AddChild(sandImage);

            // Cove
            mCoveProp = new Prop(PFCat.EXPLOSIONS);
            mCoveImage = new SPImage(mScene.TextureByName("cove"));
            mCoveImage.X = mScene.ViewWidth - mCoveImage.Width;
            mCoveImage.Y = mScene.ViewHeight - 2 * 161f;
            mCoveProp.AddChild(mCoveImage);
            mScene.AddProp(mCoveProp);

            // Day/Night cycle
            List<SPImage> shaders = new List<SPImage>() { sandImage, mCoveImage };
            mNightShade = new NightShade(shaders);

            GameController gc = GameController.GC;
            mNightShade.TransitionTimeOfDay(gc.TimeOfDay, gc.TimeKeeper.TimeRemaining, gc.TimeKeeper.ProportionRemaining);
        }

        public void EnqueueDepartingShip(PlayableShip ship)
        {
            if (ship != null && mDepartures != null && !mDepartures.Contains(ship))
            {
                ship.B2Body.SetActive(false);
                mDepartures.Enqueue(ship);
            }
        }

        public void ClearDepartures()
        {
            if (mDepartures != null)
                mDepartures.Clear();
            State = BeachState.Idle;
        }

        private void LaunchNextShip()
        {
            if (mDepartures == null || mDepartures.Count == 0 || State != BeachState.Idle)
#if DEBUG
                throw new InvalidOperationException("Attempt to launch ship from busy/empty BeachActor");
#else
                return;
#endif

            PlayableShip ship = mDepartures.Dequeue();

            if (!ship.MarkedForRemoval)
            {
                mDepartingShip = ship;
                ship.B2Body.SetActive(true);
                mScene.AddActor(ship);

                if (ship is SkirmishShip)
                {
                    SkirmishShip skShip = (ship as SkirmishShip);
                    skShip.ShipDeck.EnableCombatControls(true, skShip.SKPlayerIndex);
                }
            }
        }

        public override void AdvanceTime(double time)
        {
            if (mState == BeachState.Idle && mDepartures.Count > 0)
                State = BeachState.Departing;
            else if (mState != BeachState.Idle && mDepartingShip == null && mDepartures.Count == 0)
                State = BeachState.Idle;

            mNightShade.AdvanceTime(time);
        }

        public void OnTimeOfDayChanged(TimeOfDayChangedEvent ev)
        {
            if (ev.Transitions)
                mNightShade.TransitionTimeOfDay(ev.TimeOfDay, ev.TimeRemaining, ev.ProportionRemaining);
        }

        public void TravelBackInTime(float duration)
        {
            if (mCoveFutureImage == null)
                return;

            mScene.Juggler.RemoveTweensWithTarget(mCoveFutureImage);
            mScene.Juggler.RemoveTweensWithTarget(mCoveImage);

            SPTween tween = new SPTween(mCoveFutureImage, mCoveFutureImage.Alpha * duration);
            tween.AnimateProperty("Alpha", 0f);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)delegate(SPEvent ev)
            {
                if (mCoveFutureImage != null)
                {
                    mNightShade.RemoveShader(mCoveFutureImage);
                    mCoveFutureImage.RemoveFromParent();
                    mCoveFutureImage = null;
                }
            }, true);
            mScene.Juggler.AddObject(tween);

            tween = new SPTween(mCoveImage, (1f - mCoveImage.Alpha) * duration);
            tween.AnimateProperty("Alpha", 1f);
            mScene.Juggler.AddObject(tween);
        }

        public void TravelForwardInTime(float duration)
        {
            if (mCoveFutureImage != null)
                return;
            mCoveFutureImage = new SPImage(mScene.TextureByName("cove-future"));
            mCoveFutureImage.X = mScene.ViewWidth - 2 * 66;
            mCoveFutureImage.Y = mScene.ViewHeight - 2 * 153;
            mCoveFutureImage.Alpha = 0f;
            mCoveProp.AddChild(mCoveFutureImage);
            mNightShade.AddShader(mCoveFutureImage);

            SPTween tween = new SPTween(mCoveFutureImage, duration);
            tween.AnimateProperty("Alpha", 1f);
            mScene.Juggler.AddObject(tween);

            tween = new SPTween(mCoveImage, duration);
            tween.AnimateProperty("Alpha", 0f);
            mScene.Juggler.AddObject(tween);
        }

        public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (mState == BeachState.Departing && other == mDepartingShip && fixtureSelf == mCoveGate)
                return false;
            return base.PreSolve(other, fixtureSelf, fixtureOther, contact);
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (other is PlayableShip)
                base.BeginContact(other, fixtureSelf, fixtureOther, contact);
            // Don't care about other ships
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            base.EndContact(other, fixtureSelf, fixtureOther, contact);

            if (State == BeachState.Departing && other == mDepartingShip && (fixtureSelf == mCoveGate || other.MarkedForRemoval))
            {
                if (RemovedContact || other.MarkedForRemoval)
                {
                    mDepartingShip.Launching = false;
                    State = BeachState.Idle;
                }
            }
        }

        public override void PrepareForNewGame()
        {
            ClearDepartures();
        }

        public override void DestroyActorBody()
        {
            base.DestroyActorBody();

            mNightShade.DestroyNightShade();
        }

        protected override void  ZeroOutFixtures()
        {
            base.ZeroOutFixtures();

            mCoveGate = null;
            mCoveSensor = null;
            mDepartSensor = null;
        }
        #endregion
    }
}
