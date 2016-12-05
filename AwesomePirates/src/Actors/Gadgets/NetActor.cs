using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class NetActor : Actor, IIgnitable, IReusable
    {
        private const uint kNetReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 3;
            double duration = Idol.DurationForIdol(new Idol(Idol.GADGET_SPELL_NET));
            uint reuseKey = kNetReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetNetActor(-200, -200, 0, 1f, duration, Color.Gray);
                reusable.Hibernate();
                sCache.AddReusable(reusable);
            }

            sCache.VerifyCacheIntegrity();
            sCaching = false;
        }

        private static IReusable CheckoutReusable(uint reuseKey)
        {
            IReusable reusable = null;

            if (sCache != null && !sCaching)
                reusable = sCache.Checkout(reuseKey);

            return reusable;
        }

        private static void CheckinReusable(IReusable reusable)
        {
            if (sCache != null && !sCaching)
                sCache.Checkin(reusable);
        }

        public static NetActor GetNetActor(float x, float y, float rotation, float scale, double duration, Color color)
        {
            NetActor actor = CheckoutReusable(kNetReuseKey) as NetActor;

            if (actor != null)
            {
                actor.SpawnLoc = new Vector2(x, y);
                actor.SpawnRotation = rotation;
                actor.NetScale = scale;
                actor.Duration = duration;
#if IOS_SCREENS
                actor.RopeColor = Color.White;
#else
                actor.RopeColor = color;
#endif
                bool reuseBody = actor.NetScale == actor.PrevNetScale;
                actor.Reuse();

                if (reuseBody)
                {
                    Body body = actor.B2Body;
                    body.SetLinearVelocity(Vector2.Zero);
                    body.SetAngularVelocity(0);
                    body.SetTransform(new Vector2(x, y), rotation);
                    body.SetActive(true);
                    body.ApplyLinearImpulse(new Vector2(0.05f, 0.05f), body.GetPosition()); // Hack to ignite Box2D contacts on motionless bodies.

                    actor.SetupLocation();
                }
            }
            else
            {
                ActorDef actorDef = MiscFactory.Factory.CreateNetDef(x, y, rotation, scale);
                actor = new NetActor(actorDef, scale, duration, color);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed NetActor ReusableCache.");
#endif
            }

            return actor;
        }

        public const string CUST_EVENT_TYPE_NET_DESPAWNED = "netDespawnedEvent";

        private const float kSpawnDuration = 3.0f;
        private const int kNetFlameCount = 25;
        private static readonly Vector4 kDisplacementFactor = new Vector4(0.2f, 0.15f, 0.2f, 0.15f);

        public NetActor(ActorDef def, float scale, double duration, Color color)
            : base(def)
        {
            mCategory = (int)PFCat.SEA;
		    mAdvanceable = true;
		    mIgnited = false;
            mInUse = true;
            mPoolIndex = -1;
		    mNetScale = scale;
		    mDuration = duration;
            mRopeColor = color;
		    mHasShrunk = false;
		    mShrinking = false;
		    mDespawning = false;
		    mCostume = null;
        
            mZombieNet = false;
		    mZombieCounter = 0;
        
		    // Save fixtures
            Debug.Assert(def.fixtureDefCount == 2, "NetActor ActorDef.fixtureDefCount must be 2.");
            mCenterFixture = def.fixtures[0];
            mAreaFixture = def.fixtures[1];
            mCollidableRadius = (mAreaFixture.GetShape() as CircleShape)._radius;
            mCollidableRadiusFactor = 1;
            mSpawnScale = 0;
            SetupActorCostume();
        }
        
        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private bool mDespawning;
        private bool mIgnited;
        private bool mZombieNet;
        private double mZombieCounter;
        private Vector2 mSpawnLoc;
        private float mSpawnRotation;
        private float mPrevNetScale;
        private float mNetScale;
        private float mSpawnScale;
        private double mDuration;
        private bool mHasShrunk;
        private bool mShrinking;
        private float mCollidableRadiusFactor;
        private float mCollidableRadius;
        private Color mRopeColor;
        private Fixture mCenterFixture;
        private Fixture mAreaFixture;
        private SPImage mNet;
        private SPSprite mCostume;

        private SPTween mSpawnTween;
        private SPTween mDespawnTween;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kNetReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        protected float PrevNetScale { get { return mPrevNetScale; } }
        protected Vector2 SpawnLoc { get { return mSpawnLoc; } set { mSpawnLoc = value; } }
        protected float SpawnRotation { get { return mSpawnRotation; } set { mSpawnRotation = value; } }
        protected double Duration { get { return mDuration; } set { mDuration = value; } }
        protected Color RopeColor { get { return mRopeColor; } set { mRopeColor = value; } }

        public bool Ignited { get { return mIgnited; } }
        public bool Despawning { get { return mDespawning; } }
        public float NetScale { get { return mNetScale; } set { mNetScale = value; } }
        public float SpawnScale { get { return mSpawnScale; } set { mSpawnScale = value; } }
        public float CollidableRadiusFactor { get { return mCollidableRadiusFactor; } set { mCollidableRadiusFactor = value; mHasShrunk = true; } }
        public SKTeamIndex OwnerID { get; set; }
        public Fixture CenterFixture { get { return mCenterFixture; } }
        public Fixture AreaFixture { get { return mAreaFixture; } }
        #endregion

        #region Methods
        private void SetupActorCostume()
        {
            if (mCostume == null)
            {
                mCostume = new SPSprite();
                AddChild(mCostume);
            }

            mCostume.Alpha = 1f;

            if (mNet == null)
            {
                mNet = new SPImage(mScene.TextureByName("netR"));
                mNet.X = -mNet.Width / 2;
                mNet.Y = -mNet.Height / 2;
                mNet.Effecter = new SPEffecter(mScene.EffectForKey("Refraction"), NetDraw);
            }

            mNet.Color = mRopeColor;
            mCostume.AddChild(mNet);

            if (mSpawnTween == null || !SPMacros.SP_IS_FLOAT_EQUAL(mPrevNetScale, mNetScale))
            {
                mSpawnTween = new SPTween(this, kSpawnDuration, SPTransitions.SPEaseOut);
                mSpawnTween.AnimateProperty("SpawnScale", mNetScale);
                mPrevNetScale = mNetScale;
            }

            if (mDespawnTween == null)
            {
                mDespawnTween = new SPTween(mCostume, Globals.VOODOO_DESPAWN_DURATION);
                mDespawnTween.AnimateProperty("Alpha", 0);
                mDespawnTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnDespawnCompleted));
            }
            
	        X = PX;
	        Y = PY;
	        mCostume.Rotation = -B2Rotation;
	        mSpawnScale = mCostume.ScaleX = mCostume.ScaleY = 0.0f;
            Alpha = 1f; // mScene.GameMode == GameMode.Career ? 0.75f : 1f;
	
	        double idolDuration;
            if (mScene.GameMode == GameMode.Career)
                idolDuration = Idol.DurationForIdol(mScene.IdolForKey(Idol.GADGET_SPELL_NET));
            else
                idolDuration = SKPup.DurationForKey(SKPup.PUP_NET);
	
	        if (mDuration <= Globals.VOODOO_DESPAWN_DURATION)
            {
		        // Start in despawn mode
		        mSpawnScale = mCostume.ScaleX = mCostume.ScaleY = mNetScale;
		        Alpha = Alpha * ((float)mDuration / Globals.VOODOO_DESPAWN_DURATION);
                DespawnOverTime((float)mDuration);
	        }
            else if (SPMacros.SP_IS_DOUBLE_EQUAL(idolDuration, mDuration) || mDuration > idolDuration)
            {
		        // Start as new net
                SpawnOverTime(kSpawnDuration);
	        }
            else if (mDuration > (idolDuration - kSpawnDuration))
            {
		        // Start spawning
		        float spawnFraction = (float)(idolDuration - mDuration) / kSpawnDuration;
		        float spawnDuration = (1 - spawnFraction) * kSpawnDuration;
		
		        mSpawnScale = mCostume.ScaleX = mCostume.ScaleY = spawnFraction * mNetScale;
                SpawnOverTime(spawnDuration);
	        }
            else
            {
		        // Start already spawned
		        mSpawnScale = mCostume.ScaleX = mCostume.ScaleY = mNetScale;
	        }
        }

        protected void SetupLocation()
        {
            X = PX;
            Y = PY;

            if (mCostume != null)
                mCostume.Rotation = -B2Rotation;
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;

            if (!SPMacros.SP_IS_FLOAT_EQUAL(mPrevNetScale, mNetScale))
            {
                ActorDef def = MiscFactory.Factory.CreateNetDef(mSpawnLoc.X, mSpawnLoc.Y, mSpawnRotation, mNetScale);
                ConstructBody(def);

                // Save fixtures
                Debug.Assert(def.fixtureDefCount == 2, "NetActor ActorDef.fixtureDefCount must be 2.");
                mCenterFixture = def.fixtures[0];
                mAreaFixture = def.fixtures[1];
                mCollidableRadius = (mAreaFixture.GetShape() as CircleShape)._radius;
            }

            mCollidableRadiusFactor = 1;
            mIgnited = false;
            mHasShrunk = false;
            mShrinking = false;
            mDespawning = false;
            mZombieNet = false;
            mZombieCounter = 0;
            mSpawnScale = 0;
            SetupActorCostume();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            FreeTrappedShips();
            mScene.Juggler.RemoveTweensWithTarget(this);

            if (mCostume != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mCostume);
                mCostume.RemoveAllChildren();
            }

            mBody.SetActive(false);
            ClearContacts();

            mInUse = false;
            CheckinReusable(this);
        }

        public void NetDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            mScene.CustomDrawer.RefractionFactor = 0.05f;
            mScene.CustomDrawer.DisplacementFactor = kDisplacementFactor;
            mScene.CustomDrawer.RefractionDrawLge(displayObject, gameTime, support, parentTransform);
        }

        public void Ignite()
        {
            mIgnited = true;
        }

        public override void AdvanceTime(double time)
        {
            if (!mZombieNet)
            {
                mZombieCounter += time;
        
                if (mZombieCounter > 3.0)
                {
                    mZombieCounter = 0;
            
                    if (TurnID != GameController.GC.ThisTurn.TurnID)
                        mZombieNet = true;
                }
            }
    
	        if (MarkedForRemoval)
		        return;
    
            if (mDuration > Globals.VOODOO_DESPAWN_DURATION)
            {
                mDuration -= time;
        
                if (mDuration <= Globals.VOODOO_DESPAWN_DURATION)
                    DespawnOverTime(Globals.VOODOO_DESPAWN_DURATION);
            }
    
	        X = PX;
	        Y = PY;
	        mCostume.Rotation = -B2Rotation;
	
	        if (mShrinking)
		        CollidableRadiusFactor *= 0.99f;
	        mCostume.ScaleX = mCostume.ScaleY = mSpawnScale * mCollidableRadiusFactor;
        }

        private void SpawnOverTime(float duration)
        {
            if (mDespawning)
		        return;
            mScene.Juggler.RemoveTweensWithTarget(this);

            if (mSpawnTween != null && SPMacros.SP_IS_FLOAT_EQUAL(duration, kSpawnDuration))
            {
                mSpawnTween.Reset();
                mScene.Juggler.AddObject(mSpawnTween);
            }
            else
            {
                SPTween tween = new SPTween(this, duration, SPTransitions.SPEaseOut);
                tween.AnimateProperty("SpawnScale", mNetScale);
                mScene.Juggler.AddObject(tween);
            }
        }

        public void DespawnOverTime(float duration)
        {
            if (mDespawning)
		        return;
	        mDespawning = true;
            mScene.Juggler.RemoveTweensWithTarget(this);
            mScene.Juggler.RemoveTweensWithTarget(mCostume);
	
	        if (duration < 0)
		        duration = Globals.VOODOO_DESPAWN_DURATION;

            if (mDespawnTween != null && SPMacros.SP_IS_FLOAT_EQUAL(duration, Globals.VOODOO_DESPAWN_DURATION))
            {
                mDespawnTween.Reset();
                mScene.Juggler.AddObject(mDespawnTween);
            }
            else
            {
                SPTween tween = new SPTween(mCostume, duration);
                tween.AnimateProperty("Alpha", 0);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnDespawnCompleted);
                mScene.Juggler.AddObject(tween);
            }
        }

        private void OnDespawnCompleted(SPEvent ev)
        {
            mScene.Juggler.RemoveTweensWithTarget(this);
    
            if (TurnID == GameController.GC.ThisTurn.TurnID && mScene.GameMode == GameMode.Career)
                mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_VOODOO_GADGET_EXPIRED, Idol.GADGET_SPELL_NET);
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_NET_DESPAWNED));
            mScene.RemoveActor(this);
        }


        public void BeginShinking()
        {
            if (!mShrinking)
            {
                //[mScene.juggler removeTweensWithTarget:mCostume]; // This cancels despawn = bad!
                mShrinking = true;
                mHasShrunk = true;
            }
        }

        public void StopShrinking()
        {
            mShrinking = false;
        }

        public override void RespondToPhysicalInputs()
        {
            if (mBody == null || mZombieNet || MarkedForRemoval)
		        return;
            int shipCount = 0;
	        float radius = mCollidableRadiusFactor * (mCollidableRadius * mCollidableRadius);
	        Vector2 selfPos = mBody.GetPosition();
	
	        foreach (Actor actor in mContacts.EnumerableSet)
            {
		        if (actor.MarkedForRemoval)
			        continue;
		        if (actor is NpcShip || actor is SkirmishShip)
                {
                    if (mHasShrunk)
                    {
                        ShipActor ship = actor as ShipActor;
			
                        Vector2 otherPos = ship.B2Body.GetPosition();
                        Vector2 dist = otherPos - selfPos;
			
                        if (dist.LengthSquared() > radius)
                        {
                            ship.Drag = 1.0f;
                        }
                        else
                        {
                            ship.Drag = ship.NetDragFactor;
                            ++shipCount;
                        }
                    }
                    else
                    {
                        ++shipCount;
                    }
		        }
	        }
    
            if (!IsPreparingForNewGame && mScene.GameMode == GameMode.Career) // && mZombieNet == NO && self.markedForRemoval == NO // These ones are already checked for on function entry.
            {
                if (shipCount >= 8)
                    mScene.AchievementManager.GrantEntrapmentAchievement();
                mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_TRAWLING_NET, shipCount);
            }
        }

        public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            bool collidable = true;

            if (!mZombieNet && !MarkedForRemoval)
            {
                if (mHasShrunk && mBody != null)
                {
                    // Make sure we abide by our potentially smaller radius
                    Vector2 otherPos = other.B2Body.GetPosition();
                    Vector2 selfPos = mBody.GetPosition();
                    Vector2 dist = otherPos - selfPos;

                    if (dist.LengthSquared() > mCollidableRadiusFactor * (mCollidableRadius * mCollidableRadius))
                        collidable = false;
                }
            }
            else
            {
                collidable = false;
            }

            return collidable;
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
#if SK_BOTS
            if (other is SKPursuitShip)
                return;
#endif
            if (other is NpcShip)
            {
		        NpcShip ship = other as NpcShip;

                if (!mZombieNet && !MarkedForRemoval)
                    ship.Drag = ship.NetDragFactor;
                base.BeginContact(other, fixtureSelf, fixtureOther, contact);
	        }
            else if (other is SkirmishShip)
            {
                SkirmishShip ship = other as SkirmishShip;

                if (!mZombieNet && !MarkedForRemoval && ship.TeamIndex != OwnerID)
                    ship.Drag = ship.NetDragFactor;
                base.BeginContact(other, fixtureSelf, fixtureOther, contact);
            }
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
#if SK_BOTS
            if (other is SKPursuitShip)
                return;
#endif

            base.EndContact(other, fixtureSelf, fixtureOther, contact);

            if (RemovedContact && !mZombieNet && !MarkedForRemoval)
            {
                if (other is NpcShip)
                {
                    NpcShip ship = other as NpcShip;
			        ship.Drag = 1.0f;
                }
                else if (other is SkirmishShip)
                {
                    SkirmishShip ship = other as SkirmishShip;
                    if (ship.TeamIndex != OwnerID)
                        ship.Drag = 1.0f;
                }
            }
        }

        public override void PrepareForNewGame()
        {
            if (mPreparingForNewGame)
                return;
            mPreparingForNewGame = true;
            DespawnOverTime(mNewGamePreparationDuration);
        }

        public override void SafeRemove()
        {
            FreeTrappedShips();
            base.SafeRemove();
        }

        protected void FreeTrappedShips()
        {
            foreach (Actor actor in mContacts.EnumerableSet)
            {
                if (actor.MarkedForRemoval)
                    continue;
                if (actor is NpcShip)
                {
                    NpcShip ship = actor as NpcShip;
                    ship.Drag = 1.0f;
                }
                else if (actor is SkirmishShip)
                {
                    SkirmishShip ship = actor as SkirmishShip;
                    if (ship.TeamIndex != OwnerID)
                        ship.Drag = 1.0f;
                }
            }
        }

        protected override void ZeroOutFixtures()
        {
            base.ZeroOutFixtures();

            mCenterFixture = null;
            mAreaFixture = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mScene.Juggler.RemoveTweensWithTarget(this);

                        if (mCostume != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mCostume);
                            mCostume = null;
                        }

                        if (mDespawnTween != null)
                        {
                            mDespawnTween.RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mDespawnTween = null;
                        }

                        mNet = null;
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
        #endregion
    }
}
