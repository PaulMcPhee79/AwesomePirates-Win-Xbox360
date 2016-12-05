using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class WhirlpoolActor : Actor
    {
        private enum WhirlpoolState
        {
            Idle = 0,
            Alive,
            Dying,
            Dead
        }

        public const string CUST_EVENT_TYPE_WHIRLPOOL_DESPAWNED = "whirlpoolDespawnedEvent";
        private const float kSpawnDuration = 3.0f; // Can't be zero or will enable possible DBZ

        public WhirlpoolActor(ActorDef def, float scale, float duration)
            : base(def)
        {
            mCategory = (int)PFCat.WAVES;
		    mAdvanceable = true;
            mSwirlFactor = 1;
            mSuckFactor = 2.25f;
            mCostumeScale = scale;
		    mDuration = duration;
		    mState = WhirlpoolState.Idle;
		    mRoyalFlushes = 0;
		    mWater = null;
		    mCostume = null;
		    mVictims = new SPHashSet<Actor>();
		
            // Save fixtures
            if (def.fixtureDefCount != 2)
                throw new ArgumentException("WhirlpoolState ActorDef.fixtureDefCount must be 2.");
            mPool = def.fixtures[0];
            mEye = def.fixtures[1];
		    mRadius = Math.Max(1.0f, (mPool.GetShape() as CircleShape)._radius);
		
            SetupActorCostume();
            SetState(WhirlpoolState.Alive);
        }

        public static WhirlpoolActor CreateWhirlpoolActor(float x, float y, float rotation, float scale, float duration)
        {
            ActorDef actorDef = MiscFactory.Factory.CreateWhirlpoolDef(x, y, rotation, scale);
            WhirlpoolActor whirlpool = new WhirlpoolActor(actorDef, scale, duration);
            return whirlpool;
        }
        
        #region Fields
        private WhirlpoolState mState;
        private int mRoyalFlushes;
        private float mSwirlFactor;
        private float mSuckFactor;
        private double mDuration;
        private float mRadius;
        private float mCostumeScale;
        private SPImage mWater;
        private SPSprite mCostume;
        private Fixture mPool;
        private Fixture mEye;
        private SPHashSet<Actor> mVictims;
        #endregion

        #region Properties
        public float SwirlFactor { get { return mSwirlFactor; } set { mSwirlFactor = value; } }
        public float SuckFactor { get { return mSuckFactor; } set { mSuckFactor = value; } }
        public static float SpawnDuration { get { return kSpawnDuration; } }
        #endregion

        #region Methods
        private void SetupActorCostume()
        {
            if (mCostume != null)
		        return;
	        mCostume = new SPSprite();
	        mWater = new SPImage(mScene.TextureByName("whirlpool"));
	        mWater.X = -mWater.Width / 2;
	        mWater.Y = -mWater.Height / 2;
            mCostume.Scale = new Vector2(mCostumeScale, mCostumeScale);
            mCostume.AddChild(mWater);
	        AddChild(mCostume);

	        X = PX;
	        Y = PY;
	        Alpha = 0.0f;
	
	        double idolDuration = Idol.DurationForIdol(mScene.IdolForKey(Idol.VOODOO_SPELL_WHIRLPOOL));
	
	        if (mDuration <= Globals.VOODOO_DESPAWN_DURATION)
            {
		        // Start in despawn mode
		        Alpha = (float)mDuration / Globals.VOODOO_DESPAWN_DURATION;
                DespawnOverTime((float)mDuration);
	        }
            else if (SPMacros.SP_IS_DOUBLE_EQUAL(idolDuration, mDuration) || mDuration > idolDuration)
            {
		        // Start as new whirlpool
                SpawnOverTime(kSpawnDuration);
	        }
            else if (mDuration > (idolDuration - kSpawnDuration))
            {
		        // Start spawning
		        float spawnFraction = (float)(idolDuration - mDuration) / kSpawnDuration;
		        float spawnDuration = (1 - spawnFraction) * kSpawnDuration;
		
		        Alpha = spawnFraction;
                SpawnOverTime(spawnDuration);
	        }
            else
            {
		        // Start already spawned
		        Alpha = 1.0f;
	        }

            mScene.PlaySound("Whirlpool");
        }

        public void SetWaterColor(Color color)
        {
            mWater.Color = color;
        }

        private void SetState(WhirlpoolState state)
        {
            switch (state)
            {
		        case WhirlpoolState.Idle:
			        break;
		        case WhirlpoolState.Alive:
			        break;
		        case WhirlpoolState.Dying:
			        break;
		        case WhirlpoolState.Dead:
			        foreach (Actor actor in mContacts.EnumerableSet)
                    {
				        if (actor is NpcShip)
                        {
					        NpcShip ship = actor as NpcShip;
					        ship.InWhirlpoolVortex = false;
				        }
			        }
                    mScene.RemoveActor(this);
                    mScene.Juggler.RemoveTweensWithTarget(this);
			        break;
	        }
	        mState = state;
        }

        private void SpawnOverTime(float duration)
        {
            if (mState != WhirlpoolState.Idle)
                throw new InvalidOperationException("Cannot spawn non-Idle Whirlpool");
            SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("Alpha", 1);
            mScene.Juggler.AddObject(tween);
        }

        public void DespawnOverTime(float duration)
        {
            if (mState != WhirlpoolState.Alive)
                return;
    
	        SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("Alpha", 0.01f);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnDespawnCompleted);
            mScene.Juggler.AddObject(tween);
            mScene.StopSound("Whirlpool");
            SetState(WhirlpoolState.Dying);
        }

        private void OnDespawnCompleted(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_WHIRLPOOL_DESPAWNED));

            if (TurnID == GameController.GC.ThisTurn.TurnID)
                mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_VOODOO_GADGET_EXPIRED, Idol.VOODOO_SPELL_WHIRLPOOL);
            SetState(WhirlpoolState.Dead);
        }

        public override void AdvanceTime(double time)
        {
            if (MarkedForRemoval)
		        return;
    
	        mCostume.Rotation += (float)time * 3.0f;
    
            if (mDuration > Globals.VOODOO_DESPAWN_DURATION)
            {
                mDuration -= time;
        
                if (mDuration <= Globals.VOODOO_DESPAWN_DURATION)
                    DespawnOverTime(Globals.VOODOO_DESPAWN_DURATION);
            }
        }

        private void ApplyVortexForceToBody(Body body, float suckFactor, float swirlFactor)
        {
            if (body == null || mBody == null)
		        return;
	        Vector2 vec = mBody.GetPosition() - body.GetPosition();
	        float len = vec.Length();
	
	        if (len < SPMacros.SP_FLOAT_EPSILON)
		        return;
	        vec.Normalize();
	
	        Vector2 angularVelocity = vec;
            Box2DUtils.RotateVector(ref angularVelocity, SPMacros.PI_HALF);
	        body.SetLinearVelocity(((5.0f + 20.0f * (len / mRadius)) * swirlFactor) * angularVelocity);
	        vec *= (body.GetMass() / 10.0f) * 1200.0f * suckFactor;
	        body.ApplyForce(vec, body.GetPosition());
        }

        public override void RespondToPhysicalInputs()
        {
            if (mState == WhirlpoolState.Dead)
		        return;
	
	        foreach (Actor actor in mVictims.EnumerableSet)
            {
		        if (actor.MarkedForRemoval)
			        continue;
		        if (actor is NpcShip)
                {
			        NpcShip ship = actor as NpcShip;
			
			        if (!ship.Docking)
                    {
				        ship.DeathBitmap = DeathBitmaps.WHIRLPOOL;
				        ship.Sink();
                        ship.ShrinkOverTime(1);
                
                        if (ship is NavyShip)
                        {
                            ++mRoyalFlushes;
                    
                            if (mRoyalFlushes == 3)
                                mScene.AchievementManager.GrantRoyalFlushAchievement();
                        }
			        }
		        }
                else if (actor is PowderKegActor)
                {
			        PowderKegActor keg = actor as PowderKegActor;
			        keg.Detonate();
		        }
                else if (actor is NetActor)
                {
			        NetActor net = actor as NetActor;
			
			        if (!net.Despawning)
                        net.DespawnOverTime(Globals.VOODOO_DESPAWN_DURATION);
		        }
                else if (actor is OverboardActor)
                {
			        OverboardActor person = actor as OverboardActor;
                    person.DeathBitmap = DeathBitmaps.WHIRLPOOL;
                    person.EnvironmentalDeath();
		        }
                else
                {
                    mScene.RemoveActor(actor);
		        }
	        }
    
	        mVictims.Clear();
		
	        foreach (Actor actor in mContacts.EnumerableSet)
            {
                float suckFactor = mSuckFactor * mCostumeScale, swirlFactor = mSwirlFactor * mCostumeScale;
		
		        if (actor.MarkedForRemoval)
			        continue;
		        if (actor is NpcShip)
                {
			        NpcShip ship = actor as NpcShip;
			
			        if (!ship.InWhirlpoolVortex)
				        ship.InWhirlpoolVortex = true;
		        }
                else if (actor is OverboardActor)
                {
                    OverboardActor person = actor as OverboardActor;
            
                    if (person.IsPlayer)
                        continue;
			        swirlFactor *= 1.25f;
		        }
                else if (actor is BrandySlickActor)
                {
			        BrandySlickActor brandySlick = actor as BrandySlickActor;
			
			        if (!brandySlick.Despawning)
                        brandySlick.DespawnOverTime(Globals.VOODOO_DESPAWN_DURATION / 2);
			        continue;
		        }
                else if (actor is NetActor)
                {
			        NetActor net = actor as NetActor;
                    net.BeginShinking();
			        net.B2Body.ApplyTorque(-2000 * net.NetScale);
		        }
                else if (actor is PoolActor)
                {
			        PoolActor poolActor = actor as PoolActor;
			
			        if (!poolActor.Despawning)
                        poolActor.DespawnOverTime(Globals.VOODOO_DESPAWN_DURATION / 2);
		        }
        
                ApplyVortexForceToBody(actor.B2Body, suckFactor, swirlFactor);
	        }
        }

        private bool IgnoresContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            return other is TempestActor;
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (IgnoresContact(other, fixtureSelf, fixtureOther, contact))
                return;
            if (other.IsPreparingForNewGame)
                return;
    
            if (!other.IsSensor || other is OverboardActor || other is PowderKegActor)
            {
		        if (fixtureSelf == mEye)
                    mVictims.Add(other);
	        }
            else if (other is NetActor)
            {
		        NetActor net = other as NetActor;
		
		        if (fixtureSelf == mEye && fixtureOther == net.CenterFixture)
                    mVictims.Add(other);
	        }

            base.BeginContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (IgnoresContact(other, fixtureSelf, fixtureOther, contact))
                return;

            base.EndContact(other, fixtureSelf, fixtureOther, contact);

            if (RemovedContact)
            {
                if (other is NpcShip)
                {
                    NpcShip ship = other as NpcShip;
                    ship.InWhirlpoolVortex = false;
                }
                else if (other is NetActor)
                {
                    NetActor net = other as NetActor;
                    net.StopShrinking();
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

        protected override void ZeroOutFixtures()
        {
            base.ZeroOutFixtures();

            mPool = null;
            mEye = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mState != WhirlpoolState.Dying && mState != WhirlpoolState.Dead)
                            mScene.StopSound("Whirlpool");
                        mWater = null;
                        mCostume = null;
                        mVictims = null;
                    }
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
