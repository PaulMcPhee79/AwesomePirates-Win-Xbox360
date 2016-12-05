using System;
using System.Collections.Generic;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class Actor : SPSprite, IDisposable
    {
        private static PlayfieldController s_Scene = null;

        public static void PrimeContactCacheWithCapacity(int capacity)
        {
            ActorContact.PrimeActorContactCacheWithCapacity(capacity);
        }

        public Actor(ActorDef def, int actorId)
        {
            mScene = s_Scene;
            mActorId = actorId;
            mKey = null;
            mTurnID = GameController.GC.ThisTurn.TurnID;
            mCategory = 0;
            mAdvanceable = false;
            mRemoveMe = false;
            mRemovedContact = false;
            mPreparingForNewGame = false;
            mNewGamePreparationDuration = 1f;
            mActorDef = def;
            mBody = null;
            mContacts = new SPHashSet<Actor>();
            mContactCounts = new SPHashSet<ActorContact>();

            ConstructBody(def);
            Touchable = mScene.TouchableDefault;
        }

        public Actor(ActorDef def) : this(def, 0) { }

        #region Fields
        private bool mShouldDispose = true;
        protected int mCategory;
        protected string mKey;
        protected uint mTurnID;
        protected bool mAdvanceable;
        protected bool mRemoveMe;
        protected bool mPreparingForNewGame;
        protected float mNewGamePreparationDuration;
        protected ActorDef mActorDef;
        protected Body mBody;
        protected SPHashSet<Actor> mContacts;
        protected PlayfieldController mScene;

        private int mActorId;
        private bool mRemovedContact;
        private SPHashSet<ActorContact> mContactCounts;
        #endregion

        #region Properties
        public bool ShouldDispose { get { return mShouldDispose; } set { mShouldDispose = value; } }
        public string Key { get { return mKey; } }
        protected ActorDef Def { get { return mActorDef; } }
        public int ActorId { get { return mActorId; } set { mActorId = value; } }
        public bool MarkedForRemoval { get { return mRemoveMe; } }
        public bool IsPreparingForNewGame { get { return mPreparingForNewGame; } }
        public bool Advanceable { get { return mAdvanceable; } }
        public bool RemovedContact { get { return mRemovedContact; } }

        public int Category { get { return mCategory; } set { mCategory = value; } }
        public uint TurnID { get { return mTurnID; } }
        public float PX { get { return ResManager.M2PX(B2X); } }
        public float PY { get { return ResManager.M2PY(B2Y); } }
        public float B2X { get { return (mBody != null) ? mBody.GetPosition().X : 0; } }
        public float B2Y { get { return (mBody != null) ? mBody.GetPosition().Y : 0; } }
        public float B2Rotation { get { return (mBody != null) ? mBody.GetAngle() : 0; } }
        public Body B2Body { get { return mBody; } }
        public virtual bool IsSensor
        {
            get
            {
                // Only true of all fixtures are sensors
                if (mBody == null)
                    return false;
                
                Fixture fixtures = mBody.GetFixtureList();
                bool result = fixtures != null;

                while (fixtures != null)
                {
                    result = result && fixtures.IsSensor();
                    fixtures = fixtures.GetNext();
                }

                return result;
            }
        }
        #endregion

        #region Methods
        protected void ConstructBody(ActorDef def)
        {
            mBody = mScene.World.CreateBody(def.bd);
            def.fixtures = new Fixture[def.fixtureDefCount];

            for (int i = 0; i < def.fixtureDefCount; ++i)
                def.fixtures[i] = mBody.CreateFixture(def.fds[i]);

            mBody.SetUserData(this);
        }

        public int TagForContactWithActor(Actor actor)
        {
            int tag = 0;
            ActorContact actorContact = ActorContactForActor(actor);

            if (actorContact != null)
                tag = actorContact.Tag;
            return tag;
        }

        public void SetTagForContactWithActor(int tag, Actor actor)
        {
            ActorContact actorContact = ActorContactForActor(actor);

            if (actorContact != null)
                actorContact.Tag = tag;
        }

        private ActorContact ActorContactForActor(Actor actor)
        {
            ActorContact foundIt = null;

            foreach (ActorContact actorContact in mContactCounts.EnumerableSet)
            {
                if (actorContact.Actor == actor)
                {
                    foundIt = actorContact;
                    break;
                }
            }

            return foundIt;
        }

        public virtual void Flip(bool enable) { }
        public virtual void AdvanceTime(double time) { }
        public virtual void RespondToPhysicalInputs() { }

        public virtual void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (!mContacts.Contains(other))
            {
                mContacts.Add(other);

                ActorContact actorContact = ActorContact.GetActorContact(other);
                ++actorContact.Count;
                mContactCounts.Add(actorContact);
            }
            else
            {
                ActorContact actorContact = ActorContactForActor(other);
                ++actorContact.Count;
            }
        }

        public virtual bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            return true;
        }

        public virtual void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            mRemovedContact = false;

            if (mContacts.Contains(other))
            {
                ActorContact actorContact = ActorContactForActor(other);

                if (actorContact == null || actorContact.Count <= 0)
                    throw new InvalidOperationException("Actor Contact count should be > 0 in Actor.EndContact");

                --actorContact.Count;

                if (actorContact.Count == 0)
                {
                    if (actorContact.PoolIndex != -1)
                        ActorContact.CheckinBufferIndex(actorContact.PoolIndex);
                    mContactCounts.Remove(actorContact);
                    mRemovedContact = true;
                    mContacts.Remove(other);
                }
            }
        }

        protected virtual void ClearContacts()
        {
            if (mContacts != null)
                mContacts.Clear();
            if (mContactCounts != null)
            {
                foreach (ActorContact actorContact in mContactCounts.EnumerableSet)
                {
                    if (actorContact.PoolIndex != -1)
                        ActorContact.CheckinBufferIndex(actorContact.PoolIndex);
                }
                mContactCounts.Clear();
            }
            mRemovedContact = false;
        }

        public virtual void PrepareForNewGame()
        {
            if (MarkedForRemoval || mPreparingForNewGame)
                return;
            mPreparingForNewGame = true;

            SPTween tween = new SPTween(this, mNewGamePreparationDuration);
            tween.AnimateProperty("Alpha", 0);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)delegate(SPEvent ev)
            {
                if (!MarkedForRemoval)
                    mScene.RemoveActor(this);
            }, true);

            mScene.Juggler.AddObject(tween);
        }

        public virtual void CheckoutPooledResources() { }

        public virtual void CheckinPooledResources() { }

        public virtual void SafeRemove()
        {
            mRemoveMe = true;
        }

        public virtual void DestroyActorBody()
        {
            if (mBody != null)
            {
                Body b = mBody;
                mBody = null; // Don't try to destroy body twice
                mScene.World.DestroyBody(b);
                ZeroOutFixtures();
            }
        }

        protected virtual void ZeroOutFixtures() { }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        DestroyActorBody();
                        ClearContacts();
                        mActorDef = null;
                        mContacts = null;
                        mContactCounts = null;
                        mScene = null;
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        public static PlayfieldController ActorsScene
        {
            get
            {
                return s_Scene;
            }
            
            set
            {
                if (value == null)
                    throw new InvalidOperationException("Use RelinquishActorsScene instead of setting it to null directly.");
                s_Scene = value;
            }
        }

        public static void RelinquishActorsScene(PlayfieldController scene)
        {
            if (scene == s_Scene)
                s_Scene = null;
        }
        #endregion


        private class ActorContact
        {
            private static PoolIndexer s_ActorContactIndexer = null;
            private static ActorContact[] s_ActorContactCache = null;

            public static void PrimeActorContactCacheWithCapacity(int capacity)
            {
                if (capacity <= 0 || s_ActorContactCache != null)
                    return;

                s_ActorContactIndexer = new PoolIndexer(capacity, "ActorContact");
                s_ActorContactIndexer.InitIndexes(0, 1);
                s_ActorContactCache = new ActorContact[capacity];

                for (int i = 0; i < capacity; ++i)
                    s_ActorContactCache[i] = new ActorContact(null);
            }

            public static int CheckoutNextBufferIndex()
            {
                if (s_ActorContactIndexer != null)
                    return s_ActorContactIndexer.CheckoutNextIndex();
                else
                    return -1;
            }

            public static void CheckinBufferIndex(int index)
            {
                if (s_ActorContactIndexer != null)
                    s_ActorContactIndexer.CheckinIndex(index);
            }

            public static ActorContact GetActorContact(Actor actor)
            {
                ActorContact actorContact = null;
                int index = CheckoutNextBufferIndex();

                if (index != -1)
                {
                    actorContact = s_ActorContactCache[index];
                    actorContact.Actor = actor;
                    actorContact.Tag = 0;
                    actorContact.Count = 0;
                }
                else
                    actorContact = new ActorContact(actor);

                actorContact.PoolIndex = index;
                return actorContact;
            }

            public ActorContact(Actor actor)
            {
                mActor = actor;
                PoolIndex = -1;
                Tag = 0;
                Count = 0;
            }

            private Actor mActor;
            public int PoolIndex { get; set; }
            public int Tag { get; set; }
            public int Count { get; set; }
            public Actor Actor { get { return mActor; } private set { mActor = value; } }
        }
    }
}
