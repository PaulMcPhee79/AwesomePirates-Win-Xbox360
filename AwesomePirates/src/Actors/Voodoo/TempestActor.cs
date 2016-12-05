using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class TempestActor : Actor, IPursuer, IReusable
    {
        private const uint kTempestReuseKey = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(1);

            int cacheSize = 6;
            double duration = Idol.DurationForIdol(new Idol(Idol.VOODOO_SPELL_TEMPEST));
            uint reuseKey = kTempestReuseKey;
            IReusable reusable = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                reusable = GetTempestActor(-200, -200, 0, duration, Color.White, false);
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

        public static TempestActor GetTempestActor(float x, float y, float rotation, double duration, Color cloudColor, bool audible = true)
        {
            TempestActor actor = CheckoutReusable(kTempestReuseKey) as TempestActor;

            if (actor != null)
            {
                actor.Duration = duration;
                actor.CloudColor = cloudColor;
                actor.Audible = audible;
                actor.Reuse();

                Body body = actor.B2Body;
                body.SetTransform(new Vector2(x, y), rotation);
                body.SetActive(true);

                actor.X = actor.PX;
                actor.Y = actor.PY;
                actor.Rotation = -actor.B2Rotation;
            }
            else
            {
                actor = new TempestActor(MiscFactory.Factory.CreateTempestDef(x, y, rotation), duration, cloudColor, audible);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed TempestActor ReusableCache.");
#endif
            }

            return actor;
        }

        private enum TempestState
        {
            Alive = 0,
            Dead = 1
        }

        private const int kTempestDebrisBufferSize = 3;

        public TempestActor(ActorDef def, double duration, Color cloudColor, bool audible = true)
            : base(def)
        {
            mCategory = (int)PFCat.CLOUD_SHADOWS;
            mAdvanceable = true;
            mInUse = true;
            mPoolIndex = -1;
            mAudible = audible;
		    mDuration = duration;
            mRequestTargetTimer = 0.0;
            mCloudColor = cloudColor;
            SetupActorCostume();
            SetState(TempestState.Alive);
        }

        #region Fields
        private bool mInUse;
        private int mPoolIndex;

        private TempestState mState;
        private bool mAudible;
        private double mDuration;
        private double mRequestTargetTimer;
        private Color mCloudColor;
        private ShipActor mTarget;
        private SPSprite mCostume;
        private SPImage mCloudsImage;
        private SPSprite mClouds;
        private SPSprite mSwirl;
        private SPSprite mDebris;
        private SPSprite mDebrisRotor;
        private SPImage mStem;
        private SPMovieClip mSplash;
        private List<SPSprite> mDebrisCache; // Convenience accessor
        private RingBuffer mDebrisBuffer;
        private List<SPTween[]> mDebrisTweens;
        private SPTween[] mDespawnTweens;
        #endregion

        #region Properties
        public uint ReuseKey { get { return kTempestReuseKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        protected double Duration { get { return mDuration; } set { mDuration = value; } }
        protected Color CloudColor { get { return mCloudColor; } set { mCloudColor = value; } }
        protected bool Audible { get { return mAudible; } set { mAudible = value; } }
        public ShipActor Target
        {
            get { return mTarget; }
            set
            {
                if (mTarget == value)
		            return;
	            if (mTarget != null)
                {
                    mTarget.RemovePursuer(this);
                    mTarget = null;
	            }
	
	            if (value != null)
                {
		            mTarget = value;
                    mTarget.AddPursuer(this);
	            }
            }
        }
        public SKTeamIndex OwnerID { get; set; }
        public static int DebrisBufferSize { get { return kTempestDebrisBufferSize; } }
        #endregion

        #region Methods
        private void SetupActorCostume()
        {
            if (mCostume == null)
            {
                mCostume = new SPSprite();
                mCostume.X = -92;
                mCostume.Y = -91;
                
                SPSprite costumeScaler = new SPSprite();
                costumeScaler.ScaleX = costumeScaler.ScaleY = 1.35f;
                costumeScaler.AddChild(mCostume);
                AddChild(costumeScaler);
            }

	        // Splash
            if (mSplash == null)
            {
                mSplash = new SPMovieClip(mScene.TexturesStartingWith("tempest-splash_"), 8);
                mSplash.X = 122;
                mSplash.Y = 130;
                mCostume.AddChild(mSplash);
            }

            mScene.Juggler.AddObject(mSplash);

	        // Stem
            if (mStem == null)
            {
                mStem = new SPImage(mScene.TextureByName("tempest-stem"));
                mStem.X = 52;
                mStem.Y = 84;
                mCostume.AddChild(mStem);
            }

            mStem.Alpha = 1f;
	
	        // Swirl
            if (mSwirl == null)
            {
                mSwirl = new SPSprite();
                SPImage swirlImage = new SPImage(mScene.TextureByName("tempest-swirl"));
                swirlImage.X = -swirlImage.Width / 2;
                swirlImage.Y = -swirlImage.Height / 2;
                mSwirl.X = swirlImage.Width / 2;
                mSwirl.Y = swirlImage.Height / 2;
                mSwirl.AddChild(swirlImage);
                mCostume.AddChild(mSwirl);
            }
	
	        // Debris
            if (mDebrisBuffer == null)
                mDebrisBuffer = new RingBuffer(kTempestDebrisBufferSize);
            if (mDebris == null)
            {
                mDebris = new SPSprite();
                mCostume.AddChild(mDebris);
            }

            if (mDebrisRotor == null)
            {
                mDebrisRotor = new SPSprite();
                mDebris.AddChild(mDebrisRotor);
            }
    
            if (mDebrisCache == null) 
            {
                SPTexture debrisTexture = mScene.TextureByName("tempest-debris");
                List<SPSprite> array = new List<SPSprite>(kTempestDebrisBufferSize);
        
                for (int i = 0; i < kTempestDebrisBufferSize; ++i)
                {
                    SPImage image = new SPImage(debrisTexture);
                    image.X = -image.Width / 2;
                    image.Y = -image.Height / 2;

                    mDebris.X = image.Width / 2;
                    mDebris.Y = image.Height / 2;

                    SPSprite sprite = new SPSprite();
                    sprite.Rotation = i * SPMacros.PI / kTempestDebrisBufferSize;
                    sprite.AddChild(image);

                    array.Add(sprite);
                }
        
                mDebrisCache = array;

                foreach (SPSprite sprite in mDebrisCache)
                    mDebrisBuffer.AddItem(sprite);
            }

            if (mDebrisTweens == null)
            {
                mDebrisTweens = new List<SPTween[]>(mDebrisCache.Count);

                foreach (SPSprite sprite in mDebrisCache)
                {
                    SPTween fadeIn = new SPTween(sprite, 1f);
                    fadeIn.AnimateProperty("Alpha", 1f);

                    SPTween fadeOut = new SPTween(sprite, 1f);
                    fadeOut.AnimateProperty("Alpha", 0f);
                    fadeOut.Delay = fadeIn.TotalTime;
                    fadeOut.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnDebrisComplete));

                    SPTween[] tweens = new SPTween[] { fadeIn, fadeOut };
                    mDebrisTweens.Add(tweens);
                }
            }

            if (mDespawnTweens == null)
            {
                SPTween stemTween = new SPTween(mStem, Globals.VOODOO_DESPAWN_DURATION);
                stemTween.AnimateProperty("Alpha", 0f);

                SPTween bodyTween = new SPTween(this, Globals.VOODOO_DESPAWN_DURATION);
                bodyTween.AnimateProperty("Alpha", 0f);
                bodyTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnDespawnCompleted));

                mDespawnTweens = new SPTween[] { stemTween, bodyTween };
            }
	
	        // Clouds
            if (mClouds == null)
            {
                mClouds = new SPSprite();
                mCloudsImage = new SPImage(mScene.TextureByName("tempest-clouds"));
                mCloudsImage.X = -mCloudsImage.Width / 2;
                mCloudsImage.Y = -mCloudsImage.Height / 2;
                mClouds.X = mCloudsImage.Width / 2;
                mClouds.Y = mCloudsImage.Height / 2;
                mClouds.Alpha = 0.6f;
                mClouds.AddChild(mCloudsImage);
                mCostume.AddChild(mClouds);
            }

            mCloudsImage.Color = mCloudColor;
	
	        X = PX;
	        Y = PY;
            Alpha = 1f;
	
	        if (mDuration <= Globals.VOODOO_DESPAWN_DURATION)
            {
		        // Start in despawn mode
		        Alpha = (float)mDuration / Globals.VOODOO_DESPAWN_DURATION;
                DespawnOverTime((float)mDuration);
	        }
    
            if (mAudible)
                mScene.PlaySound("GhostlyTempest");
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;
            mRequestTargetTimer = 0.0;
            SetupActorCostume();
            SetState(TempestState.Alive);

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mTarget != null)
            {
                mTarget.RemovePursuer(this);
                mTarget = null;
            }

            if (mState != TempestState.Dead)
                mScene.StopSound("GhostlyTempest");

            if (mDebrisCache != null)
            {
                foreach (SPSprite sprite in mDebrisCache)
                    mScene.Juggler.RemoveTweensWithTarget(sprite);
            }

            if (mDebrisTweens != null)
            {
                foreach (SPTween[] tweens in mDebrisTweens)
                {
                    mScene.Juggler.RemoveObject(tweens[0]);
                    mScene.Juggler.RemoveObject(tweens[1]);
                }
            }

            if (mDespawnTweens != null)
            {
                mScene.Juggler.RemoveObject(mDespawnTweens[0]);
                mScene.Juggler.RemoveObject(mDespawnTweens[1]);
            }

            if (mDebrisRotor != null)
                mDebrisRotor.RemoveAllChildren();

            if (mSplash != null)
                mScene.Juggler.RemoveObject(mSplash);

            if (mStem != null)
                mScene.Juggler.RemoveTweensWithTarget(mStem);

            mBody.SetActive(false);
            ClearContacts();

            mInUse = false;
            CheckinReusable(this);
        }

        private void SetState(TempestState state)
        {
            switch (state)
            {
                case TempestState.Alive:
                    break;
                case TempestState.Dead:
                    Target = null;
                    break;
            }
            mState = state;
        }

        public override void Flip(bool enable)
        {
            if (enable)
            {
                ScaleX = -1;
                mCostume.X = -100;
                mCostume.Y = -104;
            }
            else
            {
                ScaleX = 1;
                mCostume.X = -110;
                mCostume.Y = -110;
            }
        }

        public void PursueeDestroyed(ShipActor pursuee)
        {
            if (pursuee != Target)
                throw new ArgumentException("Tempest pursuee/target mismatch.");
            Target = null;
        }

        private void ShowShipDebris()
        {
            int index = mDebrisBuffer.IndexOfNextItem;
	        SPSprite sprite = mDebrisBuffer.NextItem as SPSprite;
	
	        if (sprite == null)
		        return;
            if (index >= mDebrisTweens.Count)
                return;

            mScene.Juggler.RemoveTweensWithTarget(sprite);
            sprite.Alpha = 0;
            mDebrisRotor.AddChild(sprite);

            SPTween[] tweens = mDebrisTweens[index];
            Debug.Assert(tweens[0].Target == sprite && tweens[1].Target == sprite, "TempestActor debris tweens referencing incorrect sprites.");

            tweens[0].Reset();
            mScene.Juggler.AddObject(tweens[0]);

            tweens[1].Reset();
            mScene.Juggler.AddObject(tweens[1]);
        }

        private void DebrisComplete(SPSprite debris)
        {
            if (debris != null)
                mDebrisRotor.RemoveChild(debris);
        }

        private void OnDebrisComplete(SPEvent ev)
        {
            SPTween tween = ev.CurrentTarget as SPTween;

            if (tween.Target != null && tween.Target is SPSprite)
                DebrisComplete(tween.Target as SPSprite);
        }

        public void DespawnOverTime(float duration)
        {
            if (mState != TempestState.Alive)
                return;

            if (mDespawnTweens != null && SPMacros.SP_IS_FLOAT_EQUAL(duration, Globals.VOODOO_DESPAWN_DURATION))
            {
                mDespawnTweens[0].Reset();
                mScene.Juggler.AddObject(mDespawnTweens[0]);

                mDespawnTweens[1].Reset();
                mScene.Juggler.AddObject(mDespawnTweens[1]);
            }
            else
            {
                SPTween tween = new SPTween(mStem, duration);
                tween.AnimateProperty("Alpha", 0f);
                mScene.Juggler.AddObject(tween);

                tween = new SPTween(this, duration);
                tween.AnimateProperty("Alpha", 0f);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnDespawnCompleted);
                mScene.Juggler.AddObject(tween);
            }

            mScene.StopSound("GhostlyTempest");
            SetState(TempestState.Dead);
        }

        private void OnDespawnCompleted(SPEvent ev)
        {
            Target = null;
    
            if (TurnID == GameController.GC.ThisTurn.TurnID && mScene.GameMode == GameMode.Career)
                mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_VOODOO_GADGET_EXPIRED, Idol.VOODOO_SPELL_TEMPEST);
            if (mSplash != null)
                mScene.Juggler.RemoveObject(mSplash);
            mScene.RemoveActor(this);
        }

        public override void AdvanceTime(double time)
        {
            if (mBody == null)
		        return;

            Vector2 velocity;
    
            if (mDuration > Globals.VOODOO_DESPAWN_DURATION)
            {
                mDuration -= time;
        
                if (mDuration <= Globals.VOODOO_DESPAWN_DURATION)
                    DespawnOverTime(Globals.VOODOO_DESPAWN_DURATION);
            }
    
	        X = PX;
	        Y = PY;
	        mSwirl.Rotation -= (float)time * 12.0f;
	        mClouds.Rotation -= (float)time * 5.0f;
            mDebrisRotor.Rotation -= (float)time * 12.0f;
	
	        if (mState == TempestState.Dead)
		        return;
	        if (mTarget == null)
            {
                mRequestTargetTimer -= time;
                if (mRequestTargetTimer <= 0)
                {
                    mScene.RequestTargetForPursuer(this);

                    // Requesting an enemy target is expensive, so don't do it at 60fps.
                    if (Target == null)
                        mRequestTargetTimer = 0.25;
                }
		
		        // Slow to a crawl while waiting for new target
		        velocity = new Vector2(-2.0f, -2.0f);
		        mBody.SetLinearVelocity(velocity);
		        return;
	        }
	
	        Vector2 dest = mTarget.B2Body.GetPosition() - mBody.GetPosition();
	        dest.Normalize();

            velocity = ResManager.RESM.GameFactorArea * 8.0f * dest;
	        mBody.SetLinearVelocity(velocity);
        }

        public override void RespondToPhysicalInputs()
        {
            if (mState == TempestState.Dead)
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
                        if (B2Body != null && ship.B2Body != null)
                        {
                            // Don't destroy ship unless we're right over the top of it, else it looks bad.
                            if ((B2Body.GetPosition() - ship.B2Body.GetPosition()).LengthSquared() < 2f)
                            {
                                ship.DeathBitmap = DeathBitmaps.GHOSTLY_TEMPEST;
                                ship.SinkerID = OwnerID;
                                ship.Sink();
                                ShowShipDebris();

                                if (ship == mTarget)
                                    Target = null;
                            }
                        }
			        }
                    else if (ship == mTarget)
                        Target = null;
		        }
                else if (actor is OverboardActor)
                {
			        OverboardActor person = actor as OverboardActor;
			
			        if (!person.Dying)
                    {
                        person.DeathBitmap = DeathBitmaps.GHOSTLY_TEMPEST;
                        person.EnvironmentalDeath();
                        mScene.AchievementManager.GrantNoPlaceLikeHomeAchievement();
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
                    if (ship.TeamIndex != OwnerID)
                        ship.ApplyEnvironmentalDamage(3);
                }
	        }
        }

        private bool IgnoresContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            return false;
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (IgnoresContact(other, fixtureSelf, fixtureOther, contact))
                return;
            if (other.IsPreparingForNewGame)
                return;
            base.BeginContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
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
                        if (mTarget != null)
                        {
                            mTarget.RemovePursuer(this);
                            mTarget = null;
                        }

                        if (mState != TempestState.Dead)
                            mScene.StopSound("GhostlyTempest");

                        if (mDebrisCache != null)
                        {
                            foreach (SPSprite sprite in mDebrisCache)
                            {
                                mScene.Juggler.RemoveTweensWithTarget(sprite);
                                sprite.Dispose();
                            }

                            mDebrisCache = null;
                        }

                        if (mDebrisTweens != null)
                        {
                            foreach (SPTween[] tweens in mDebrisTweens)
                            {
                                mScene.Juggler.RemoveObject(tweens[0]);
                                mScene.Juggler.RemoveObject(tweens[1]);
                                tweens[1].RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            }

                            mDebrisTweens = null;
                        }

                        if (mDespawnTweens != null)
                        {
                            mScene.Juggler.RemoveObject(mDespawnTweens[0]);
                            mScene.Juggler.RemoveObject(mDespawnTweens[1]);
                            mDespawnTweens[1].RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mDespawnTweens = null;
                        }

                        if (mSplash != null)
                        {
                            mScene.Juggler.RemoveObject(mSplash);
                            mSplash = null;
                        }

                        if (mStem != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mStem);
                            mStem = null;
                        }

                        if (mClouds != null)
                        {
                            mClouds.Dispose();
                            mClouds = null;
                        }

                        mCloudsImage = null;
                        mSwirl = null;
                        mCostume = null;
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
