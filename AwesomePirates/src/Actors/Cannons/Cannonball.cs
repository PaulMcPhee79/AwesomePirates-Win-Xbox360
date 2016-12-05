
//#define CANNONBALL_DEBUG
#define REUSE_INACTIVE_B2BODIES

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class Cannonball : Actor, IResourceClient, IReusable
    {
        public const float kGravity = 0.065f;

        protected const float kMaxAltitude = ((SPMacros.PI / 8.0f) / kGravity) / 4.0f;
        protected const float kShadowAlpha = 0.5f;
        protected const float kBaseShadowFactor = 2f * 128.0f; // 2f * 192.0f;
        // Inverse factor
        protected const float kScaleFactor = 0.025f * SPMacros.PI; // 0.04f * SPMacros.PI; // 0.0825f * SPMacros.PI;

        protected const int kCannonballCoreTag = 0x1;
        protected const int kCannonballCoreMask = 0xff;
        protected const int kCannonballConeTag = 0x100;
        protected const int kCannonballConeMask = 0xff00;

        private const uint kPlayerCannonballReuseKey = 1;
        private const uint kNpcCannonballReuseKey = 2;
        private static int sCacheGeneration = 1;
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        private static readonly string[] kExplosionSounds = { "Explosion1", "Explosion2", "Explosion3" };

        public static void PurgeReusables()
        {
            if (sCaching)
                return;

            if (sCache != null)
                sCache.Purge();
            sCache = null;
            ++sCacheGeneration;
        }

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(2);

            // Player cannonballs
            int cacheSize = 80;
            uint reuseKey = kPlayerCannonballReuseKey;
            string shotType = Ash.TexturePrefixForKey(Ash.ASH_DEFAULT);
            Cannonball cannonball = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                cannonball = CannonFactory.Factory.CreateCannonballForCache(shotType, true);
                cannonball.SetupCannonball();
                cannonball.ReuseKey = reuseKey;
                cannonball.CacheGen = sCacheGeneration;
                cannonball.Hibernate();
                sCache.AddReusable(cannonball);
            }

            // Npc Cannonballs
            cacheSize = 15;
            reuseKey = kNpcCannonballReuseKey;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                cannonball = CannonFactory.Factory.CreateCannonballForCache(shotType, false);
                cannonball.SetupCannonball();
                cannonball.ReuseKey = reuseKey;
                cannonball.CacheGen = sCacheGeneration;
                cannonball.Hibernate();
                sCache.AddReusable(cannonball);
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

        public static Cannonball CannonballForNpcShooter(SPSprite shooter, string shotType, Vector2 origin, Vector2 impulse, float bore, float trajectory)
        {
            uint reuseKey = kNpcCannonballReuseKey;
            Cannonball cannonball = CheckoutReusable(reuseKey) as Cannonball;

            if (cannonball != null)
            {
                cannonball.ShotType = shotType;
                cannonball.Shooter = shooter;
                cannonball.Trajectory = trajectory;

#if REUSE_INACTIVE_B2BODIES
                cannonball.Reuse();

                Body body = cannonball.B2Body;
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);
                body.SetTransform(origin, body.GetAngle());
                body.SetActive(true);
#else
                // Re-initialize body def
                cannonball.Def.bd.position.X = origin.X;
                cannonball.Def.bd.position.Y = origin.Y;
       
                cannonball.Reuse();
#endif
                // Apply forces
                cannonball.B2Body.ApplyLinearImpulse(impulse, cannonball.B2Body.GetPosition());
            }
            else
            {
                cannonball = CannonFactory.Factory.CreateCannonballForNpcShooter(shooter, shotType, origin, impulse, bore, trajectory);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed Cannonball ReusableCache.");
#endif
            }

            return cannonball;
        }

        public static Cannonball CannonballForShip(ShipActor ship, Vector2? shipVector, ShipDetails.ShipSide side, float trajectory, Vector2? target)
        {
            uint reuseKey = (ship != null && ship is PlayableShip) ? kPlayerCannonballReuseKey : kNpcCannonballReuseKey;
            Cannonball cannonball = CheckoutReusable(reuseKey) as Cannonball;

            if (cannonball != null)
            {
                cannonball.ShotType = ship.CannonDetails.ShotType;
                cannonball.Shooter = ship;
                cannonball.Trajectory = trajectory;

#if REUSE_INACTIVE_B2BODIES
                // Re-initialize body
                AABB aabb;
                ship.PortOrStarboard(side).GetAABB(out aabb, 0);
                Vector2 pos = aabb.GetCenter();
                cannonball.Reuse();

                Body body = cannonball.B2Body;
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);
                body.SetTransform(pos, body.GetAngle());
                body.SetActive(true);

                cannonball.mOrigin = body.GetPosition();
#else
                // Re-initialize body def
                AABB aabb;
                ship.PortOrStarboard(side).GetAABB(out aabb, 0);
                Vector2 pos = aabb.GetCenter();
                cannonball.Def.bd.position.X = pos.X;
                cannonball.Def.bd.position.Y = pos.Y;

                // Reshape ricochet cone
                if (reuseKey == kPlayerCannonballReuseKey)
                {
                    float coneSizeFactor = ResManager.RESM.GameFactor;
                    if ((GameController.GC.MasteryManager.MasteryBitmap & CCMastery.CANNON_CANNONEER) != 0)
                        coneSizeFactor *= 1.2f;
                    CannonFactory.Factory.ReshapeRicochetCone(cannonball.ShotType, coneSizeFactor, cannonball.Def.fds[1].shape as PolygonShape);
                }

                cannonball.Reuse();
#endif
                // Apply forces
                if (target == null)
                    CannonFactory.Factory.ApplyForcesToCannonball(cannonball, ship, side, pos);
                else
                    CannonFactory.Factory.ApplyForcesToTargetedCannonball(cannonball, ship, shipVector.Value, pos, target.Value);

                //Debug.WriteLine("Cannonball Lin Vel: " + cannonball.B2Body.GetLinearVelocity().Length());
            }
            else
            {
                if (target == null)
                    cannonball = CannonFactory.Factory.CreateCannonballForShip(ship, side, trajectory);
                else
                    cannonball = CannonFactory.Factory.CreateCannonballForShip(ship, shipVector.Value, side, trajectory, target.Value);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed Cannonball ReusableCache.");
#endif
            }

            return cannonball;
        }

        public Cannonball(ActorDef def, string shotType, SPSprite shooter, float bore, float trajectory)
            : base(def)
        {
            mCategory = (int)PFCat.EXPLOSIONS;
		    mAdvanceable = true;
            mInUse = true;
            mPoolIndex = -1;
            mCacheGen = 0;
		    mGroupId = 0;
            mGroup = null;
		    mShotType = shotType;
		    mShooter = shooter;
		    mOrigin = mBody.GetPosition();
		    mHasProcced = false;
		
            mCore = def.fixtures[0];
            mCone = (def.fixtures.Length == 2) ? def.fixtures[1] : null;

            mGravity = kGravity * 0.5f;
		    mBore = bore;
		    Trajectory = trajectory;
		    mShadowFactor = 0.0f;
		
		    mRicocheted = false;
		    mRicochetCount = 0;
            mInfamyBonus = CannonballInfamyBonus.GetCannonballInfamyBonus();
		
		    if (mKey == null)
			    mKey = "Cannonball";
            mSensors = new List<Actor>();
            mDestroyedShips = new SPHashSet<ShipActor>();
		    mResources = null;
            CheckoutPooledResources();
        }

        #region Fields
        private bool mInUse;
        private uint mReuseKey;
        private int mPoolIndex;
        private int mCacheGen;

        protected int mGroupId;
        protected WeakReference mGroup; // Weak reference to a CannonballGroup
        protected string mShotType;
        protected SPSprite mShooter;
        protected Fixture mCore;
        protected Fixture mCone;
        protected Vector2 mOrigin;

        protected bool mHasProcced;
        protected float mBore;
        protected float mTrajectory;
        protected float mShadowFactor;

        protected float mGravity;
        protected float mScaleFactor;
        protected float mMidDistance;
        protected float mDistanceRemaining;

        protected bool mRicocheted;
        protected uint mRicochetCount;
        protected CannonballInfamyBonus mInfamyBonus;

        protected SPMovieClip mBallClip;
        protected SPMovieClip mShadowClip;

        protected SPSprite mBallCostume;
        protected SPSprite mShadowCostume;

        protected List<Actor> mSensors;
        protected SPHashSet<ShipActor> mDestroyedShips;
        protected ResourceServer mResources;
        #endregion

        #region Properties
        public uint ReuseKey { get { return mReuseKey; } protected set { mReuseKey = value; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }
        protected int CacheGen { get { return mCacheGen; } set { mCacheGen = value; } }

        public int CannonballGroupId { get { return mGroupId; } set { mGroupId = value; } }
        public CannonballGroup CannonballGroup
        {
            get { return (mGroup != null && mGroup.Target != null) ? mGroup.Target as CannonballGroup : null; }
            set
            {
                if (mGroup != null && value != null)
                    throw new ArgumentException("Cannonball cannot be a member of more than one CannonballGroup");

                if (value == null)
                    mGroup = null;
                else if (mGroup != null)
                    mGroup.Target = value;
                else
                    mGroup = new WeakReference(value);
                mGroupId = (value != null) ? value.GroupId : 0;
            }
        }
        public string ShotType { get { return mShotType; } protected set { mShotType = value; } }
        public string ShooterName { get { return Cannonball.CannonShooterName(mShooter); } }
        public SPSprite Shooter { get { return mShooter; } protected set { mShooter = value; } }
        public bool HasProcced { get { return mHasProcced; } set { mHasProcced = value; } }
        public CannonballInfamyBonus InfamyBonus
        {
            get { return mInfamyBonus; }
            set
            {
                if (mInfamyBonus != null && mInfamyBonus.PoolIndex != -1)
                    mInfamyBonus.Hibernate();
                mInfamyBonus = value;
            }
        }
        public uint RicochetCount { get { return mRicochetCount; } set { mRicochetCount = value; } }
        public Fixture Core { get { return mCore; } set { mCore = value; } }
        public Fixture Cone { get { return mCone; } set { mCone = value; } }
        public float Bore { get { return mBore; } }
        public float Trajectory
        {
            get { return mTrajectory; }
            set
            {
                mTrajectory = value;
                mDistanceRemaining = Math.Abs(mTrajectory / mGravity);
                mMidDistance = mDistanceRemaining / 2;
                mScaleFactor = Math.Abs(mTrajectory / kScaleFactor);
            }
        }
        public float DistanceRemaining { get { return mDistanceRemaining; } set { mDistanceRemaining = value; } }
        public float Gravity { get { return mGravity; } set { mGravity = value; } }
        public float DistSq
        {
            get
            {
                Vector2 distVec = (mBody != null) ? mBody.GetPosition() - mOrigin : Vector2.Zero;
                float x = ResManager.M2P(distVec.X);
                float y = ResManager.M2P(distVec.Y);
                return x * x + y * y;
            }
        }
        public int DamageFromImpact { get { return 3; } }
        public static float Fps { get { return 12.0f; } }
        #endregion

        #region Methods
        public static string CannonShooterName(SPSprite shooter)
        {
            string name = null;

            if (shooter == null)
                name = "NULL_CANNON_SHOOTER";
            else if (shooter is PlayerShip)
                name = "PlayerShip";
            else if (shooter is SkirmishShip)
                name = "SkirmishShip";
            else if (shooter is PirateShip)
                name = "PirateShip";
            else if (shooter is NavyShip)
                name = "NavyShip";
            else if (shooter is MerchantShip)
                name = "MerchantShip";
            else if (shooter is TownCannon)
                name = "TownCannon";
            else
                name = "INVALID_CANNON_SHOOTER";

	        return name;
        }
        #endregion

        #region Methods
        public void SetupCannonball()
        {
            GameController gc = GameController.GC;
	
	        // Cannonball clips
	        List<SPTexture> textures = null;

	        if (mBallClip == null)
            {
		        textures = mScene.TexturesStartingWith(mShotType);
		        mBallClip = new SPMovieClip(textures, Cannonball.Fps);
		        mBallClip.X = -mBallClip.Width/2;
		        mBallClip.Loop = true;
	        }
	
            mBallClip.Y = 0;
            mBallClip.CurrentFrame = 0;
            mBallClip.Play();
            mScene.Juggler.AddObject(mBallClip);

	        if (mShadowClip == null)
            {
		        if (textures == null)
			        textures = mScene.TexturesStartingWith(mShotType);
		        mShadowClip = new SPMovieClip(textures, Cannonball.Fps);
		        mShadowClip.X = -mShadowClip.Width/2;
		        mShadowClip.Loop = true;
	        }
    
            mShadowClip.Y = 0;
            mShadowClip.CurrentFrame = 0;
            mShadowClip.Play();
            mScene.Juggler.AddObject(mShadowClip);

            if (mShadowCostume == null)
            {
                mShadowCostume = new SPSprite();
                mShadowCostume.Y = -mShadowClip.Height / 8;
            }
            mShadowCostume.AddChild(mShadowClip);
            AddChild(mShadowCostume);

            if (mBallCostume == null)
            {
                mBallCostume = new SPSprite();
                mBallCostume.Y = -mBallClip.Height / 8;
            }
            mBallCostume.AddChild(mBallClip);
            AddChild(mBallCostume);
    
#if CANNONBALL_DEBUG
            SPQuad testQuad = new SPQuad(48, 96);
            testQuad.X = -testQuad.Width / 2;
            testQuad.Y = mBallCostume.Y - testQuad.Height;
            testQuad.Alpha = 0.5f;
            mBallCostume.AddChild(testQuad);
#endif
	
	        // Calculate shadow length based on time of day
	        mShadowFactor = kBaseShadowFactor * gc.TimeKeeper.ShadowOffsetY;
	
            X = PX;
	        Y = PY;
            mBallCostume.Rotation = mShadowCostume.Rotation = -B2Rotation;
            DecorateCannonball();
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;
#if !REUSE_INACTIVE_B2BODIES
            ConstructBody(mActorDef);
#endif
            mGroupId = 0;
            mGroup = null;
            mHasProcced = false;

#if !REUSE_INACTIVE_B2BODIES
            mCore = mActorDef.fixtures[0];
            mCone = (mActorDef.fixtures.Length == 2) ? mActorDef.fixtures[1] : null;
#endif

            mGravity = kGravity * 0.5f;
            mShadowFactor = 0.0f;

            mRicocheted = false;
            mRicochetCount = 0;
            mInfamyBonus = CannonballInfamyBonus.GetCannonballInfamyBonus();

            if (mKey == null)
                mKey = "Cannonball";
            if (mSensors == null)
                mSensors = new List<Actor>();
            if (mDestroyedShips == null)
                mDestroyedShips = new SPHashSet<ShipActor>();
            mResources = null;
            CheckoutPooledResources();

            Visible = true;
            Alpha = 1f;

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            CannonballGroup = null;

            if (mBallClip != null)
            {
                mScene.Juggler.RemoveObject(mBallClip);
                mBallClip = null;
            }

            if (mShadowClip != null)
            {
                mScene.Juggler.RemoveObject(mShadowClip);
                mShadowClip = null;
            }

            if (mBallCostume != null)
                mBallCostume.RemoveAllChildren();
            if (mShadowCostume != null)
                mShadowCostume.RemoveAllChildren();
            RemoveAllChildren();

            if (mSensors != null)
                mSensors.Clear();
            if (mDestroyedShips != null)
                mDestroyedShips.Clear();

            CheckinPooledResources();

            InfamyBonus = null;
            mShotType = null;
            mShooter = null;
            mGravity = kGravity * 0.5f;

#if REUSE_INACTIVE_B2BODIES
            mBody.SetActive(false);
#else
            DestroyActorBody();
#endif
            ClearContacts();

#if !REUSE_INACTIVE_B2BODIES
            if (mActorDef != null)
                mActorDef.ResetFixtures();
#endif
            mInUse = false;
            CheckinReusable(this);
        }

        public void CalculateTrajectory(Body target)
        {
            if (target == null || mBody == null)
                return;

            Vector2 targetPos = target.GetPosition();
            Vector2 cannonballVelocity = B2Body.GetLinearVelocity();
            float cannonballMagnitude = cannonballVelocity.Length();

            // Calc initial trajectory
            Vector2 distVec = new Vector2(PX - ResManager.M2PX(targetPos.X), PY - ResManager.M2PY(targetPos.Y));
            float distance = distVec.Length();
            float distVel = cannonballMagnitude * (ResManager.PPM / mScene.Fps);

            if (SPMacros.SP_IS_FLOAT_EQUAL(distVel, 0))
                distVel = 1;

            mDistanceRemaining = mGravity * (distance / distVel);
            mTrajectory = -mDistanceRemaining * mGravity;
            mMidDistance = mDistanceRemaining / 2;
            mScaleFactor = Math.Abs((mDistanceRemaining * mGravity) / kScaleFactor);

            // Recalibrate trajectory based on the position our target will be at in future.
            Vector2 combinedVelocity = cannonballVelocity + target.GetLinearVelocity();
            float combinedMagnitude = combinedVelocity.Length();

            if (cannonballMagnitude != 0 && combinedMagnitude != 0)
            {
                AlterTrajectory(combinedMagnitude / cannonballMagnitude);
                B2Body.SetLinearVelocity(Vector2.Multiply(combinedVelocity, cannonballMagnitude / combinedMagnitude));
            }
        }

        public void CalculateTrajectoryFrom(float targetX, float targetY)
        {
            Vector2 v = new Vector2(PX - targetX, PY - targetY);
            Vector2 velVec = (mBody != null) ? mBody.GetLinearVelocity() : Vector2.Zero;
            velVec.X *= ResManager.PPM / mScene.Fps;
            velVec.Y *= ResManager.PPM / mScene.Fps;

            float dist = v.Length();
            float vel = velVec.Length();

            if (SPMacros.SP_IS_FLOAT_EQUAL(vel, 0))
                vel = 1;

            mDistanceRemaining = mGravity * (dist / vel);
            mTrajectory = -mDistanceRemaining * mGravity;
            mMidDistance = mDistanceRemaining / 2;
            mScaleFactor = Math.Abs((mDistanceRemaining * mGravity) / kScaleFactor);
        }

        public void CopyTrajectoryFrom(Cannonball other)
        {
            mDistanceRemaining = other.mDistanceRemaining;
            mTrajectory = -mDistanceRemaining * mGravity;
            mMidDistance = mDistanceRemaining / 2;
            mScaleFactor = Math.Abs((mDistanceRemaining * mGravity) / kScaleFactor);
        }

        public void AlterTrajectory(float factor, float padding = 0.1f)
        {
            mDistanceRemaining = (mDistanceRemaining * factor) + padding;
            mTrajectory = -mDistanceRemaining * mGravity;
            mMidDistance = mDistanceRemaining / 2;
            mScaleFactor = Math.Abs((mDistanceRemaining * mGravity) / kScaleFactor);
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            base.BeginContact(other, fixtureSelf, fixtureOther, contact);

            int tag = TagForContactWithActor(other);
	
	        if (fixtureSelf == mCore)
                SetTagForContactWithActor(tag + kCannonballCoreTag, other);
	        else if (fixtureSelf == mCone)
                SetTagForContactWithActor(tag + kCannonballConeTag, other);
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            int tag = TagForContactWithActor(other);

            if (tag != 0)
            {
                if (fixtureSelf == mCore)
                    SetTagForContactWithActor(tag - kCannonballCoreTag, other);
                else if (fixtureSelf == mCone)
                    SetTagForContactWithActor(tag - kCannonballConeTag, other);
            }

            base.EndContact(other, fixtureSelf, fixtureOther, contact);
        }

        public override void RespondToPhysicalInputs()
        {
            if (MarkedForRemoval || IsPreparingForNewGame || mDistanceRemaining > (mGravity + 0.005))
		        return;

	        bool hitSolidObject = false, playerHitRicochetableShip = false;
	        Actor hitShip = null;
            PlayableShip playableShip = null;
            BrandySlickActor brandySlick = null;
            CannonballImpactLog impactLog = null;
    
	        foreach (Actor actor in mContacts.EnumerableSet)
            {
		        if (actor.MarkedForRemoval)
			        continue;
		        int tag = TagForContactWithActor(actor);
		
		        // We're only interested in processing contacts with the core cannonball fixture; not the ricochet cone.
		        if ((tag & kCannonballCoreMask) == 0)
			        continue;
		
		        if (actor != mShooter && !actor.IsSensor)
                {
			        if (actor is ShipActor)
                    {
				        if (mDestroyedShips.Contains(actor as ShipActor))
					        continue;
				
				        if (actor is PlayableShip)
                        {
                            playableShip = actor as PlayableShip;

                            if (playableShip.IsFlyingDutchman)
						        continue;
                            if (playableShip is SkirmishShip)
                                playerHitRicochetableShip = true;
				        }
                        else if (actor is NpcShip)
                        {
					        if (mShooter is PlayableShip)
                            {
                                HibernateImpactLog(impactLog);
						        impactLog = NotifyOfImpact(CannonballImpactLog.ImpactType.NpcShip, actor);
						
						        // If not in a group OR if the first of a group to ricochet off this ship
						        if (impactLog == null || impactLog.MayRicochet)
                                {
							        NpcShip npcShip = actor as NpcShip;
							
							        if (!npcShip.Docking)
                                    {
                                        playableShip = mShooter as PlayableShip;
                                        playableShip.CannonballHitTarget(true, mRicocheted, mHasProcced);

                                        npcShip.RicochetCount = mRicochetCount;

                                        if (playableShip is PlayerShip)
                                        {
                                            mScene.AchievementManager.PlayerHitShip(npcShip, DistSq, mRicocheted);
                                            npcShip.RicochetBonus = (int)mRicochetCount * (int)Potion.RicochetBonusForPotion(mScene.PotionForKey(Potion.POTION_RICOCHET));
                                        }
                                        else if (playableShip is SkirmishShip)
                                            npcShip.SinkerID = (playableShip as SkirmishShip).TeamIndex;
                                
                                        // Apply procMultiplier to both because procs are supposed to multiply your entire score
                                        npcShip.SunkByPlayerCannonInfamyBonus = npcShip.InfamyBonus;
							
								        if ((mInfamyBonus.ProcType & Ash.ASH_SAVAGE) != 0)
                                            npcShip.ThrowCrewOverboard(1);
								        else if ((mInfamyBonus.ProcType & Ash.ASH_NOXIOUS) != 0)
                                            npcShip.SpawnAcidPool();
                                
                                        if (mInfamyBonus.ProcType != 0)
                                            npcShip.AshBitmap = mInfamyBonus.ProcType;
                                        npcShip.MiscBitmap |= mInfamyBonus.MiscBitmap;
                                        playerHitRicochetableShip = true;
							        }
						        }
					        }
				        }

                        (actor as ShipActor).DamageShipWithCannonball(this);
				        hitShip = actor;
                        mDestroyedShips.Add(actor as ShipActor);
                        hitSolidObject = true;
                        break;
			        }
                    else
                    {
                        hitSolidObject = true;
                    }
		        }
                else if (actor is BrandySlickActor || actor is PowderKegActor || actor is OverboardActor)
                {
                    mSensors.Add(actor);
		        }
	        }

            // Splash at 0.0f; hit below mGravity + 0.005.
	        if (mDistanceRemaining <= 0.0 || hitSolidObject)
            {
		        if (hitShip == null && !mRicocheted && mShooter is PlayableShip)
                {
                    bool ignoreMiss = false;
                    CannonballImpactLog.ImpactType impactType = (hitSolidObject) ? CannonballImpactLog.ImpactType.Land : CannonballImpactLog.ImpactType.Water;
                    HibernateImpactLog(impactLog);
			        impactLog = NotifyOfImpact(impactType, null);
            
                    if (impactType == CannonballImpactLog.ImpactType.Water) {
                        // Don't penalize player for igniting a Brandy Slick
                        foreach (Actor actor in mSensors)
                        {
                            if (actor is BrandySlickActor)
                            {
                                brandySlick = actor as BrandySlickActor;
                        
                                if (!brandySlick.Ignited)
                                {
                                    ignoreMiss = true;
                            
                                    if (CannonballGroup != null)
                                        CannonballGroup.IgnoreGroupMiss();
                                    break;
                                }
                            }
                        }
                    }
            
                    if ((impactLog == null || impactLog.GroupMissed) && !ignoreMiss)
                    {
                        playableShip = mShooter as PlayableShip;
                        playableShip.CannonballHitTarget(false, mRicocheted, mHasProcced);

                        if (playableShip is PlayerShip)
                            mScene.AchievementManager.PlayerMissed(mInfamyBonus.ProcType);
                    }
		        }
		
		        if (hitSolidObject)
                {
                    DisplayHitEffect(PointMovie.PointMovieType.Explosion);
			
			        if (impactLog == null || impactLog.ShouldPlaySounds)
				        PlayExplosionSound();
		        }
                else
                {
                    foreach (Actor actor in mSensors)
                    {
                        if (actor is BrandySlickActor)
                        {
                            brandySlick = actor as BrandySlickActor;
                            brandySlick.Ignite();
                        }
                        else if (actor is PowderKegActor)
                        {
                            PowderKegActor keg = actor as PowderKegActor;
                            keg.Detonate();
                        }
                        else if (actor is OverboardActor)
                        {
                            OverboardActor person = actor as OverboardActor;
                            person.EnvironmentalDeath();

                            if (mShooter is PlayableShip)
                                person.DeathBitmap = DeathBitmaps.PLAYER_CANNON;
                        }
                    }
            
                    DisplayHitEffect(PointMovie.PointMovieType.Splash);
			
			        if (impactLog == null || impactLog.ShouldPlaySounds)
				        PlaySplashSound();
		        }
		
		        mRicocheted = false;
		
		        if (hitShip != null && mShooter is PlayableShip)
                {
                    if (playerHitRicochetableShip && (impactLog == null || impactLog.MayRicochet) && mRicochetCount < 5)
                    {
				        // Test for ricochet
				        ShipActor ricochetTarget = NearestRicochetTarget(mContacts, hitShip);
				
				        if (ricochetTarget != null)
					        Ricochet(ricochetTarget);
			        }
		        }
		
		        if (!mRicocheted)
                {
                    HibernateImpactLog(impactLog);
                    impactLog = NotifyOfImpact(CannonballImpactLog.ImpactType.RemoveMe, null);
                    SafeRemove();
		        }
	        }

            HibernateImpactLog(impactLog);
            mSensors.Clear();
        }

        protected void HibernateImpactLog(CannonballImpactLog impactLog)
        {
            if (impactLog != null && impactLog.PoolIndex != -1)
                impactLog.Hibernate();
        }

        protected virtual CannonballImpactLog NotifyOfImpact(CannonballImpactLog.ImpactType impactType, Actor ricochetTarget)
        {
            CannonballImpactLog impactLog = null;
	
	        if (CannonballGroup != null)
            {
                impactLog = CannonballImpactLog.GetImpactLog(this, impactType, ricochetTarget);
                CannonballGroup.CannonballImpacted(impactLog);
            }

	        return impactLog;
        }

        protected virtual ShipActor NearestRicochetTarget(SPHashSet<Actor> targets, Actor ignoreActor)
        {
            float closest = 99999999.9f, distSq;
            Vector2 dist = new Vector2();
	        ShipActor target = null;
	
	        foreach (Actor actor in targets.EnumerableSet)
            {
		        if (actor == ignoreActor || actor.MarkedForRemoval)
			        continue;
		        int tag = TagForContactWithActor(actor);
		
		        if ((tag & kCannonballConeMask) != 0)
                {
                    if (actor is ShipActor == false)
                        continue;
                    else if (actor is NpcShip && (actor as NpcShip).Docking)
                        continue;
                    else if (actor is SkirmishShip && (actor as SkirmishShip).Sinking)
                        continue;
                    else
                    {
                        ShipActor ship = actor as ShipActor;
					    dist.X = X - ship.X;
					    dist.Y = Y - ship.Y;
					    distSq = dist.LengthSquared();
				
					    if (closest > distSq)
                        {
						    closest = distSq;
						    target = actor as ShipActor;
					    }
			        }
		        }
	        }
	
	        return target;
        }

        protected virtual void Ricochet(ShipActor ship)
        {
            if (ship == null || mBody == null)
		        return;
	        mRicocheted = true;
	        ++mRicochetCount;
	
	        // Calculate and apply linear velocity
	        Vector2 selfPos = mBody.GetPosition();
	        Vector2 target = ship.B2Body.GetPosition();
	        float x = target.X - selfPos.X;
	        float y = target.Y - selfPos.Y;
	
	        Vector2 impulse = new Vector2(x, y);
	        impulse.Normalize();
            impulse *= CannonFactory.CannonImpulse;
	        mBody.SetLinearVelocity(Vector2.Zero);
	        mBody.ApplyLinearImpulse(impulse, selfPos);
	
	        // Calculate trajectory
            CalculateTrajectoryFrom(ResManager.M2PX(target.X), ResManager.M2PY(target.Y));
	
	        // Add target's linear velocity to ensure we hit it
	        Vector2 selfVel = mBody.GetLinearVelocity();
	        Vector2 targetVelocity = ship.B2Body.GetLinearVelocity();
	        mBody.SetLinearVelocity(selfVel + targetVelocity);
	
	        // Set transform
	        Vector2 vertical = new Vector2(0, 1);
	        selfVel = mBody.GetLinearVelocity();
	        mBody.SetTransform(selfPos, -Box2DUtils.SignedAngle(ref selfVel, ref vertical));
	
	        // Adjust appearance
            mBallCostume.Rotation = mShadowCostume.Rotation = -B2Rotation;
	        mShadowFactor = kBaseShadowFactor * GameController.GC.TimeKeeper.ShadowOffsetY;
            DecorateCannonball();
        }

        protected virtual void DisplayHitEffect(PointMovie.PointMovieType effectType)
        {
            PointMovie.PointMovieWithType(effectType, X, Y);
        }

        protected virtual void PlayExplosionSound()
        {
            //mScene.PlaySound(kExplosionSounds[GameController.GC.NextRandom(0, 2)], 0.5f);
            //mScene.AudioPlayer.PlayRandomSoundWithKeyPrefix("Explosion", 1, 3, 0.5f);
            mScene.PlaySound("Explosion");
        }

        protected virtual void PlaySplashSound()
        {
            //mScene.PlaySound("Splash", 0.33f);
            mScene.PlaySound("Splash");
        }

        public override void AdvanceTime(double time)
        {
            X = PX;
            Y = PY;
#if CANNONBALL_DEBUG
            mBallCostume.Rotation = mShadowCostume.Rotation = - B2Rotation;
#endif
            mDistanceRemaining -= (float)((double)mGravity * (time * (double)GameController.GC.Fps));
            DecorateCannonball();
        }

        protected virtual void DecorateCannonball()
        {
            float scaleFunction = Math.Abs(mDistanceRemaining - mMidDistance) / mMidDistance;
            scaleFunction *= scaleFunction;
            float scale = Math.Min(1.75f, 0.3f + (mScaleFactor * 0.5f * (1f - scaleFunction)));
            float halfScale = scale * 0.35f; // 0.5f;

            mBallCostume.ScaleX = mBallCostume.ScaleY = scale;
            mShadowCostume.ScaleX = mShadowCostume.ScaleY = scale;

            mShadowClip.Alpha = Math.Max(0.1f, kShadowAlpha - halfScale);
            mShadowCostume.Y = -4 + halfScale * (-16 + mShadowFactor * halfScale); // -4 to retain original -mShadowClip.Height / 8; 
        }

        public override void SafeRemove()
        {
            if (mRemoveMe)
                return;
            base.SafeRemove();

            if (CannonballGroup != null)
                CannonballGroup.RemoveCannonball(this);

            if (!IsPreparingForNewGame && mShooter != null && mShooter is PlayerShip)
            {
                if (mRicochetCount >= 3)
                {
                    if (!mScene.AchievementManager.HasCopsAndRobbersAchievement())
                    {
                        int navyCount = 0, pirateCount = 0;

                        foreach (Actor actor in mDestroyedShips.EnumerableSet)
                        {
                            if (actor is NavyShip)
                                ++navyCount;
                            else if (actor is PirateShip)
                                ++pirateCount;
                        }

                        if (navyCount >= 2 && pirateCount >= 2)
                            mScene.AchievementManager.GrantCopsAndRobbersAchievement();
                    }
                }

                mScene.AchievementManager.GrantRicochetAchievement(mRicochetCount);
                mScene.ObjectivesManager.ProgressObjectiveWithRicochetVictims(mDestroyedShips);
            }

            // Cancel Hibernation if we're from a previous generation
            if (mCacheGen != sCacheGeneration)
                PoolIndex = -1;
        }

        public void ResourceEventFiredWithKey(uint key, string type, object target) { /* Do nothing */ }

        public override void CheckoutPooledResources()
        {
            if (mResources == null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_CANNONBALL);

                if (cache != null)
                    mResources = cache.CheckoutPoolResourcesForKey(mShotType);
            }

	        if (mResources == null)
		        Debug.WriteLine("_+_+_+_+_+_+_+_+_ MISSED CANNONBALL CACHE _+_++_+_+_+_+_+_+");
            else
            {
                mResources.Client = this;
        
                if (mBallClip == null)
                    mBallClip = mResources.DisplayObjectForKey(CannonballCache.RESOURCE_KEY_CANNONBALL_CLIP) as SPMovieClip;
                if (mShadowClip == null)
                    mShadowClip = mResources.DisplayObjectForKey(CannonballCache.RESOURCE_KEY_SHADOW_CLIP) as SPMovieClip;
            }
        }

        public override void CheckinPooledResources()
        {
            if (mResources != null)
            {
                CacheManager cache = mScene.CacheManagerForKey(CacheManager.CACHE_CANNONBALL);

                if (cache != null)
                    cache.CheckinPoolResources(mResources);
                mResources = null;
            }
        }

        protected override void ZeroOutFixtures()
        {
            base.ZeroOutFixtures();

            mCore = null;
            mCone = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        CannonballGroup = null;

                        if (mBallClip != null)
                        {
                            mBallClip.RemoveFromParent();
                            mScene.Juggler.RemoveObject(mBallClip);

                            if (mResources == null)
                                mBallClip.Dispose();
                            mBallClip = null;
                        }

                        if (mShadowClip != null)
                        {
                            mShadowClip.RemoveFromParent();
                            mScene.Juggler.RemoveObject(mShadowClip);

                            if (mResources == null)
                                mShadowClip.Dispose();
                            mShadowClip = null;
                        }

                        CheckinPooledResources();
                        InfamyBonus = null;
                        mBallCostume = null;
                        mShadowCostume = null;
                        mShotType = null;
                        mShooter = null;
                        mSensors = null;
                        mDestroyedShips = null;
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
