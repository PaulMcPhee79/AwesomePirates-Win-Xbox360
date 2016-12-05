using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class BrandySlickActor : Actor, IIgnitable, IReusable
    {
        private const uint kBrandySlickReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 3;
            double duration = Idol.DurationForIdol(new Idol(Idol.GADGET_SPELL_BRANDY_SLICK));
            uint reuseKey = kBrandySlickReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetBrandySlickActor(-200, -200, 0, 1f, duration, "brandy-flame_");
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

        public static BrandySlickActor GetBrandySlickActor(float x, float y, float rotation, float scale, double duration, string flameTexName)
        {
            BrandySlickActor actor = CheckoutReusable(kBrandySlickReuseKey) as BrandySlickActor;

            if (actor != null)
            {
                actor.SpawnLoc = new Vector2(x, y);
                actor.SpawnRotation = rotation;
                actor.BrandyScale = scale;
                actor.Duration = duration;
                actor.FlameTextureName = flameTexName;

                bool reuseBody = actor.BrandyScale == actor.PrevBrandyScale;
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
                ActorDef actorDef = MiscFactory.Factory.CreateBrandySlickDef(x, y, rotation, scale);
                actor = new BrandySlickActor(actorDef, scale, duration, flameTexName);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed BrandySlickActor ReusableCache.");
#endif
            }

            return actor;
        }

        public const string CUST_EVENT_TYPE_BRANDY_SLICK_DESPAWNED = "brandySlickDespawnedEvent";

        private enum BrandySlickState
        {
            Spawning = 0,
            Spawned,
            Despawning,
            Despawned
        }

        private const float kSpawnDuration = 3.0f;
        private const int kBrandyFlameCount = 21;

        private static readonly int[] s_FlameCoords = new int[]
        {
            4, 10, 18, 0, 20, 16, 34, 6, 48, 18, 64,
            16, 76, 4, 90, 0, 108, 2, 120, 16, 126, 0,
            136, 20, 146, 8, 158, 20, 160, 0, 174, 8,
            188, 16, 190, 0, 204, 14, 212, 2, 220, 20
        };

        private static readonly Vector4 kDisplacementFactor = new Vector4(0, 0, 0.4f, 0.25f);
        private static readonly Vector4 kDisplacementFactorIgnited = new Vector4(0, 0, 0.15f, 0.1f);

        public BrandySlickActor(ActorDef def, float scale, double duration, string flameTexName)
            : base(def)
        {
            mCategory = (int)PFCat.SEA;
            mAdvanceable = true;
            mInUse = true;
            mPoolIndex = -1;
		    mState = BrandySlickState.Spawning;
            mBrandyScale = mPrevBrandyScale = scale;
		    mDuration = duration;
            mFlameTextureName = flameTexName;
		    mPrisonersFried = 0;
            mZombieSlick = false;
            mZombieCounter = 0;
            SetupActorCostume();
        }

        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private BrandySlickState mState;
        private Vector2 mSpawnLoc;
        private float mSpawnRotation;
        private float mBrandyScale;
        private float mPrevBrandyScale;
        private double mDuration;
        private uint mPrisonersFried;

        private bool mZombieSlick;
        private double mZombieCounter;

        private SPImage mSlickImage;
        private SPSprite mSlick;
        private string mFlameTextureName;
        private WaterFire mFire;

        private SPTween[] mSpawnTweens;
        private SPTween mDespawnTween;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kBrandySlickReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        protected float PrevBrandyScale { get { return mPrevBrandyScale; } }
        protected float BrandyScale { get { return mBrandyScale; } set { mBrandyScale = value; } }
        protected Vector2 SpawnLoc { get { return mSpawnLoc; } set { mSpawnLoc = value; } }
        protected float SpawnRotation { get { return mSpawnRotation; } set { mSpawnRotation = value; } }
        protected double Duration { get { return mDuration; } set { mDuration = value; } }
        protected string FlameTextureName { get { return mFlameTextureName; } set { mFlameTextureName = value; } }
        protected WaterFire Fire { get { return mFire; } }
        public bool Despawning { get { return (mState == BrandySlickState.Despawning || mState == BrandySlickState.Despawned); } }
        public bool Ignited { get { return mFire.Ignited; } }
        public SKTeamIndex OwnerID { get; set; }
        #endregion

        #region Methods
        private void SetupActorCostume()
        {
            if (mSlickImage == null)
            {
                mSlickImage = new SPImage(mScene.TextureByName("brandy-slickR"));
                mSlickImage.X = -mSlickImage.Width / 2;
                mSlickImage.Y = -mSlickImage.Height / 2;
                mSlickImage.Effecter = new SPEffecter(mScene.EffectForKey("Refraction"), SlickDraw);
            }

            if (mSlick == null)
            {
                mSlick = new SPSprite();
                mSlick.AddChild(mSlickImage);
                AddChild(mSlick);
            }

            mSlick.Alpha = 1f;

            if (mFire == null)
            {
                mFire = new WaterFire((int)PFCat.WAVES, s_FlameCoords, mFlameTextureName);
                mSpawnTweens = null; // Need current fire object as target.
            }
            else
                mFire.Reuse(s_FlameCoords, mFlameTextureName);
            mScene.AddProp(mFire);

            if (mSpawnTweens == null || !SPMacros.SP_IS_FLOAT_EQUAL(mPrevBrandyScale, mBrandyScale))
            {
                SPTween slickTween = new SPTween(mSlick, kSpawnDuration, SPTransitions.SPEaseOut);
                slickTween.AnimateProperty("ScaleX", mBrandyScale);
                slickTween.AnimateProperty("ScaleY", mBrandyScale);

                SPTween fireTween = new SPTween(mFire, kSpawnDuration, SPTransitions.SPEaseOut);
                fireTween.AnimateProperty("ScaleX", mBrandyScale);
                fireTween.AnimateProperty("ScaleY", mBrandyScale);

                mSpawnTweens = new SPTween[] { slickTween, fireTween };
                mPrevBrandyScale = mBrandyScale;
            }

            if (mDespawnTween == null)
            {
                mDespawnTween = new SPTween(mSlick, Globals.VOODOO_DESPAWN_DURATION);
                mDespawnTween.AnimateProperty("Alpha", 0f);
                mDespawnTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnDespawnCompleted));
            }

            SetupLocation();
	        mSlick.ScaleX = mSlick.ScaleY = mFire.ScaleX = mFire.ScaleY = 0.0f;
	
	        double idolDuration;
            if (mScene.GameMode == GameMode.Career)
                idolDuration = Idol.DurationForIdol(mScene.IdolForKey(Idol.GADGET_SPELL_BRANDY_SLICK));
            else
                idolDuration = SKPup.DurationForKey(SKPup.PUP_BRANDY_SLICK);
	
	        if (mDuration <= Globals.VOODOO_DESPAWN_DURATION)
            {
		        // Start in despawn mode
                SetState(BrandySlickState.Despawning);
		        mSlick.ScaleX = mSlick.ScaleY = mFire.ScaleX = mFire.ScaleY = mBrandyScale;
		        mSlick.Alpha = mFire.Alpha = (float)mDuration / Globals.VOODOO_DESPAWN_DURATION;
                DespawnOverTime((float)mDuration);
	        }
            else if (SPMacros.SP_IS_DOUBLE_EQUAL(idolDuration, mDuration) || mDuration > idolDuration)
            {
		        // Start as new brandy slick
                SpawnOverTime(kSpawnDuration);
	        }
            else if (mDuration > (idolDuration - kSpawnDuration))
            {
		        // Start spawning
                SetState(BrandySlickState.Spawning);
		
		        float spawnFraction = (float)(idolDuration - mDuration) / kSpawnDuration;
		        float spawnDuration = (1 - spawnFraction) * kSpawnDuration;
		        mSlick.ScaleX = mSlick.ScaleY = mFire.ScaleX = mFire.ScaleY = spawnFraction * mBrandyScale;
                SpawnOverTime(spawnDuration);
	        } 
            else
            {
		        // Start already spawned
                SetState(BrandySlickState.Spawned);
		        mSlick.ScaleX = mSlick.ScaleY = mFire.ScaleX = mFire.ScaleY = mBrandyScale;
	        }
        }

        protected void SetupLocation()
        {
            if (mFire == null || mSlick == null)
                return;

            X = mFire.X = PX;
            Y = mFire.Y = PY;
            mSlick.Rotation = -B2Rotation - SPMacros.PI_HALF;
            mFire.Rotation = -B2Rotation - SPMacros.PI_HALF;
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;

            if (!SPMacros.SP_IS_FLOAT_EQUAL(mPrevBrandyScale, mBrandyScale))
            {
                ActorDef actorDef = MiscFactory.Factory.CreateBrandySlickDef(mSpawnLoc.X, mSpawnLoc.Y, mSpawnRotation, mBrandyScale);
                ConstructBody(actorDef);
            }

            mState = BrandySlickState.Spawning;
            mPrisonersFried = 0;
            mZombieSlick = false;
            mZombieCounter = 0;
            SetupActorCostume();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mSlick != null)
                mScene.Juggler.RemoveTweensWithTarget(mSlick);

            if (mFire != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mFire);
                mScene.RemoveProp(mFire, false);
                mFire.Hibernate();
            }

            mBody.SetActive(false);
            ClearContacts();

            mInUse = false;
            CheckinReusable(this);
        }

        public void SlickDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            mScene.CustomDrawer.RefractionFactor = 0.15f;
            mScene.CustomDrawer.DisplacementFactor = (Ignited) ? kDisplacementFactorIgnited : kDisplacementFactor;
            mScene.CustomDrawer.RefractionDrawLge(displayObject, gameTime, support, parentTransform);
        }

        private void SetState(BrandySlickState state)
        {
	        switch (state)
            {
		        case BrandySlickState.Spawning:
			        break;
		        case BrandySlickState.Spawned:
			        break;
		        case BrandySlickState.Despawning:
			        break;
		        case BrandySlickState.Despawned:
                    mScene.HideHintByName(GameSettings.BRANDY_SLICK_TIPS);
			        break;
		        default:
			        break;
	        }

	        mState = state;
        }

        public override void AdvanceTime(double time)
        {
            if (!mZombieSlick)
            {
                mZombieCounter += time;
        
                if (mZombieCounter > 3.0) {
                    mZombieCounter = 0;
            
                    if (TurnID != GameController.GC.ThisTurn.TurnID)
                        mZombieSlick = true;
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
        }

        private void SpawnOverTime(float duration)
        {
            if (mState != BrandySlickState.Spawning)
		        return;

            if (mSpawnTweens != null && SPMacros.SP_IS_FLOAT_EQUAL(duration, kSpawnDuration))
            {
                mSpawnTweens[0].Reset();
                mScene.Juggler.AddObject(mSpawnTweens[0]);

                mSpawnTweens[1].Reset();
                mScene.Juggler.AddObject(mSpawnTweens[1]);
            }
            else
            {
                SPTween tween = new SPTween(mSlick, duration, SPTransitions.SPEaseOut);
                tween.AnimateProperty("ScaleX", mBrandyScale);
                tween.AnimateProperty("ScaleY", mBrandyScale);
                mScene.Juggler.AddObject(tween);

                tween = new SPTween(mFire, duration, SPTransitions.SPEaseOut);
                tween.AnimateProperty("ScaleX", mBrandyScale);
                tween.AnimateProperty("ScaleY", mBrandyScale);
                mScene.Juggler.AddObject(tween);
            }
        }

        public void Ignite()
        {
            mFire.Ignite();

            if (!GameSettings.GS.SettingForKey(GameSettings.BRANDY_SLICK_TIPS))
                GameSettings.GS.SetSettingForKey(GameSettings.BRANDY_SLICK_TIPS, true);
            mScene.HideHintByName(GameSettings.BRANDY_SLICK_TIPS);
        }

        public void DespawnOverTime(float duration)
        {
            if (Despawning)
		        return;
            SetState(BrandySlickState.Despawning);
            mScene.Juggler.RemoveTweensWithTarget(mFire);
            mScene.Juggler.RemoveTweensWithTarget(mSlick);
	
	        if (duration < 0)
		        duration = Globals.VOODOO_DESPAWN_DURATION;

            mFire.ExtinguishOverTime(duration);

            if (mDespawnTween != null && SPMacros.SP_IS_FLOAT_EQUAL(duration, Globals.VOODOO_DESPAWN_DURATION))
            {
                mDespawnTween.Reset();
                mScene.Juggler.AddObject(mDespawnTween);
            }
            else
            {
                SPTween tween = new SPTween(mSlick, duration);
                tween.AnimateProperty("Alpha", 0f);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnDespawnCompleted);
                mScene.Juggler.AddObject(tween);
            }
        }

        private void OnDespawnCompleted(SPEvent ev)
        {
            if (mState != BrandySlickState.Despawning)
                throw new InvalidOperationException("BrandySlickActor should be despawning in OnDespawnCompleted.");
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_BRANDY_SLICK_DESPAWNED));
            SafeRemove();

            mScene.RemoveProp(mFire, false);

            if (TurnID == GameController.GC.ThisTurn.TurnID && mScene.GameMode == GameMode.Career)
                mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_VOODOO_GADGET_EXPIRED, Idol.GADGET_SPELL_BRANDY_SLICK);
            SetState(BrandySlickState.Despawned);
        }

        public override void RespondToPhysicalInputs()
        {
            if (!Ignited || mZombieSlick)
		        return;
	        foreach (Actor actor in mContacts.EnumerableSet)
            {
		        if (actor.MarkedForRemoval)
			        continue;

		        if (actor is NpcShip)
                {
			        NpcShip ship = actor as NpcShip;
			
			        if (!ship.Docking)
                    {
				        ship.DeathBitmap = DeathBitmaps.BRANDY_SLICK;
                        ship.SinkerID = OwnerID;
                        ship.Sink();
			        }
		        }
                else if (actor is OverboardActor)
                {
			        OverboardActor person = actor as OverboardActor;
			
			        if (!person.Dying)
                    {
                        person.DeathBitmap = DeathBitmaps.BRANDY_SLICK;
                        person.EnvironmentalDeath();
                
                        if (person.Prisoner.Planked)
                            ++mPrisonersFried;
			
				        if (mPrisonersFried == 3)
                            mScene.AchievementManager.GrantDeepFriedAchievement();
			        }
		        }
                else if (actor is PowderKegActor)
                {
			        PowderKegActor keg = actor as PowderKegActor;
			        keg.Detonate();
		        }
                else if (actor is SkirmishShip)
                {
                    SkirmishShip ship = actor as SkirmishShip;
                    ship.ApplyEnvironmentalDamage(2);
                }
	        }
        }

        private bool IgnoresContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            bool ignores = false;

            if (other is SkirmishShip)
            {
                SkirmishShip ship = other as SkirmishShip;
                if (ship.TeamIndex == OwnerID)
                    ignores = true;
            }

            return ignores;
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
#if SK_BOTS
            if (other is SKPursuitShip)
                return;
#endif
            if (IgnoresContact(other, fixtureSelf, fixtureOther, contact))
                return;
            base.BeginContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
#if SK_BOTS
            if (other is SKPursuitShip)
                return;
#endif
            if (IgnoresContact(other, fixtureSelf, fixtureOther, contact))
                return;
            base.EndContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void PrepareForNewGame()
        {
            if (mPreparingForNewGame)
                return;
            mPreparingForNewGame = true;
            DespawnOverTime(mNewGamePreparationDuration);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mSlick != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mSlick);
                            mSlick = null;
                        }

                        if (mFire != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mFire);
                            mScene.RemoveProp(mFire, false);
                            mFire.Dispose();
                            mFire = null;
                        }

                        if (mDespawnTween != null)
                        {
                            mDespawnTween.RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mDespawnTween = null;
                        }

                        mSlickImage = null;
                        mSpawnTweens = null;
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
