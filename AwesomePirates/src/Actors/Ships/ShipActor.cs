using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class ShipActor : Actor, IReusable
    {
        protected const float kShipActorWakeFactor = 275f; // 235f;

        private static ReusableCache sCache = null;
        private static bool sCaching = false;
        private static Dictionary<string, uint> sNpcShipReuseKeys = null;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            Dictionary<string, object> npcShipDetailDict = ShipFactory.Factory.AllNpcShipDetails;
            List<string> npcShipTypes = ShipFactory.Factory.AllNpcShipTypes;
            sCache = new ReusableCache(npcShipTypes.Count);

            if (sNpcShipReuseKeys == null)
                sNpcShipReuseKeys = new Dictionary<string, uint>(npcShipTypes.Count);

            int cacheSize = 10;
            uint reuseKey = 1;
            ShipActor ship = null;

            foreach (string shipType in npcShipTypes)
            {
                Dictionary<string, object> details = npcShipDetailDict[shipType] as Dictionary<string, object>;
                cacheSize = Convert.ToInt32(details["cacheSize"]);
                sCache.AddKey(cacheSize, reuseKey);
                sNpcShipReuseKeys.Add(shipType, reuseKey);

                if (cacheSize == 0)
                    cacheSize = 5;

                for (int i = 0; i < cacheSize; ++i)
                {
                    ship = GetNpcShip(shipType, 0, 0, 0);
                    ship.ReuseKey = reuseKey;
                    ship.Hibernate();
                    sCache.AddReusable(ship);
                }

                ++reuseKey;
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

        public static NpcShip GetNpcShip(string shipKey, float x, float y, float angle)
        {
            NpcShip ship = CheckoutReusable(sNpcShipReuseKeys[shipKey]) as NpcShip;

            if (ship != null)
            {
                ship.Reuse();

                Body body = ship.B2Body;
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);
                body.SetTransform(new Vector2(x, y), angle);
                body.SetActive(true);
            }
            else
            {
                ActorDef actorDef = null;

                switch (shipKey)
                {
                    case "MerchantCaravel":
                    case "MerchantGalleon":
                    case "MerchantFrigate":
                        {
                            string key = "Merchant";
                            actorDef = ShipFactory.Factory.CreateShipDefForShipType(key, x, y, angle);
                            ship = new MerchantShip(actorDef, shipKey);
                        }
                        break;
                    case "Pirate":
                        {
                            actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);
                            ship = new PirateShip(actorDef, shipKey);
                        }
                        break;
                    case "Navy":
                        {
                            actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);
                            ship = new NavyShip(actorDef, shipKey);
                        }
                        break;
                    case "Escort":
                        {
                            actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);
                            ship = new EscortShip(actorDef, shipKey);
                        }
                        break;
                    case "SilverTrain":
                        {
                            actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);
                            ship = new SilverTrain(actorDef, shipKey);
                        }
                        break;
                    case "TreasureFleet":
                        {
                            actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);
                            ship = new TreasureFleet(actorDef, shipKey);
                        }
                        break;
                    default:
                        throw new ArgumentException("Invalid shipKey provided to ShipActor.GetNpcShip: " + shipKey);
                }
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed ShipActor ReusableCache.");
#endif
            }

            return ship;
        }

        public ShipActor(ActorDef def, string key)
            : base(def)
        {
            mCategory = -1; // Initialized in subclasses.
            mAdvanceable = true;
            mInUse = true;
            mPoolIndex = -1;
            mKey = key;
            mIsHintAttached = false;
            mCannonSoundEnabled = true;
            mOdometer = 0;
            mWakePeriod = Wake.DefaultWakePeriod;
            mWakeCount = -1;
            mDrag = 1;
            mSpeedModifier = 1;
            mControlModifier = 1;
            mAshBitmap = Ash.ASH_DEFAULT;
            mDeathBitmap = DeathBitmaps.ALIVE;
            mMiscBitmap = 0;
            mRicochetCount = 0;
            mBow = null;
            mHull = null;
            mStern = null;
            mPort = null;
            mStarboard = null;
            mOverboard = null;
            mShipDetails = null;
            mCannonDetails = null;
            mWake = null;
            mShipHitGlows = new List<ShipHitGlow>();
            mPursuers = new SPHashSet<IPursuer>();
            mSinkingClip = null;
            mBurningClip = null;
            mRicochetBonus = 0;
            mPlayerCannonInfamyBonus = 0;
            mMutinyReduction = 1;

            mAngVelUpright = 0.5f;
            mNumCostumeImages = ShipDetails.NUM_NPC_COSTUME_IMAGES;
            mCostumeUprightIndex = mNumCostumeImages / 2;
            mCostumeIndex = mCostumeUprightIndex;
            mCostume = null;
            mDeathCostume = new SPSprite();
            mWardrobe = null;
            mCostumeImages = null;
            mCurrentCostumeImages = null;
            mCostumeStack = new List<List<SPImage>>();

            // Save fixtures
            for (int i = 0; i < def.fixtureDefCount; ++i)
                SaveFixture(def.fixtures[i], i);

            WakeFactor = kShipActorWakeFactor;
            X = PX;
            Y = PY;
        }

        #region Fields
        protected bool mInUse;
        protected uint mReuseKey;
        protected int mPoolIndex;

        protected bool mIsHintAttached;
        protected bool mCannonSoundEnabled;
        protected float mOdometer;
        protected float mWakePeriod;
        protected float mWakeFactor;
        protected float mSailForce;
        protected float mSailForceMax;
        protected float mTurnForceMax;
        protected float mDrag;
        protected float mSpeedModifier;
        protected float mControlModifier;
        protected uint mAshBitmap;
        protected uint mDeathBitmap;
        protected uint mMiscBitmap;
        protected uint mRicochetCount;
        protected int mWakeCount;
        protected int mRicochetBonus;
        protected int mPlayerCannonInfamyBonus;
        protected int mMutinyReduction;
        protected Fixture mBow; // Front
        protected Fixture mHull; // Center
        protected Fixture mStern; // Rear
        protected Fixture mPort; // Left
        protected Fixture mStarboard; // Right
        protected Fixture mOverboard; // Where to drop overboard crew members. Must point to one of the other fixtures.
        protected ShipDetails mShipDetails;
        protected CannonDetails mCannonDetails;
        //Lantern mLantern;
        protected List<ShipHitGlow> mShipHitGlows;
        protected SPMovieClip mSinkingClip;
        protected SPMovieClip mBurningClip;
        protected Wake mWake;

        protected SPHashSet<IPursuer> mPursuers;

        // Costume
        protected float mAngVelUpright;
        protected int mCostumeIndex;
        protected int mCostumeUprightIndex;
        protected int mNumCostumeImages;
        protected SPSprite mCostume;
        protected SPSprite mDeathCostume;
        protected SPSprite mWardrobe;
        protected List<SPImage> mCostumeImages;
        protected List<SPImage> mCurrentCostumeImages;
        protected List<List<SPImage>> mCostumeStack;
        #endregion

        #region Properties
        public uint ReuseKey { get { return mReuseKey; } protected set { mReuseKey = value; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public Fixture Bow { get { return mBow; } set { mBow = value; } }
        public Fixture Hull { get { return mHull; } set { mHull = value; } }
        public Fixture Stern { get { return mStern; } set { mStern = value; } }
        public Fixture Port { get { return mPort; } set { mPort = value; } }
        public Fixture Starboard { get { return mStarboard; } set { mStarboard = value; } }
        public ShipDetails ShipDetails { get { return mShipDetails; } set { mShipDetails = value; } }
        public CannonDetails CannonDetails { get { return mCannonDetails; } set { mCannonDetails = value; } }
        public float SailForce { get { return mSailForce; } }
        public float Drag { get { return mDrag; } set { mDrag = value; } }
        public float SpeedModifier { get { return mSpeedModifier; } set { mSpeedModifier = value; } }
        public float ControlModifier { get { return mControlModifier; } set { mControlModifier = value; } }
        public float WakeFactor { get { return mWakeFactor; } set { mWakeFactor = value; } }
        protected virtual int WakeCategory { get { return (int)PFCat.WAKES; } }
        public uint AshBitmap { get { return mAshBitmap; } set { mAshBitmap = value; } }
        public uint DeathBitmap { get { return mDeathBitmap; } set { mDeathBitmap = value; } }
        public uint MiscBitmap { get { return mMiscBitmap; } set { mMiscBitmap = value; } }
        public uint RicochetCount { get { return mRicochetCount; } set { mRicochetCount = value; } }
        public virtual int InfamyBonus { get { return 0; } }
        public float CenterX
        {
            get
            {
                float cx = X;

                if (mBody != null)
                {
                    AABB aabb;
                    mHull.GetAABB(out aabb, 0);
                    cx = ResManager.M2PX(aabb.GetCenter().X);
                }

                return cx;
            }
        }
        public float CenterY
        {
            get
            {
                float cy = Y;

                if (mBody != null)
                {
                    AABB aabb;
                    mHull.GetAABB(out aabb, 0);
                    cy = ResManager.M2PY(aabb.GetCenter().Y);
                }

                return cy;
            }
        }
        public Vector2 OverboardLocation
        {
            get
            {
                Vector2 loc = Vector2.Zero;

                if (mBody != null)
                {
                    AABB aabb;
                    mOverboard.GetAABB(out aabb, 0);
                    loc = aabb.GetCenter();
                }

                return loc;
            }
        }
        public float SinkingClipFps { get { return 8f; } }
        public float BurningClipFps { get { return 8f; } }
        public int RicochetBonus { get { return mRicochetBonus; } set { mRicochetBonus = value; } }
        public int SunkByPlayerCannonInfamyBonus { get { return mPlayerCannonInfamyBonus; } set { mPlayerCannonInfamyBonus = value; } }
        public int MutinyReduction { get { return mMutinyReduction; } set { mMutinyReduction = value; } }
        public virtual bool IsBrigFull { get { return mShipDetails.IsBrigFull; } }
        public bool IsHintAttached { get { return mIsHintAttached; } set { mIsHintAttached = value; } }
        public string HintName { get; set; }
        public virtual float NetDragFactor { get { return 0.25f; } }
        public static float DefaultSailForceMax { get { return 150f; } }
        #endregion

        #region Methods
        public virtual void SetupShip()
        {
            if (mWakeCount == -1)
		        mWakeCount = Wake.DefaultWakeBufferSize;

            mWake = Wake.WakeWithCategory(WakeCategory, mWakeCount);
            mScene.AddProp(mWake);
        }

        public virtual void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;

            mIsHintAttached = false;
            mCannonSoundEnabled = true;
            mOdometer = 0;
            mWakeCount = -1;
            mDrag = 1;
            mSpeedModifier = 1;
            mControlModifier = 1;
            mAshBitmap = Ash.ASH_DEFAULT;
            mDeathBitmap = DeathBitmaps.ALIVE;
            mMiscBitmap = 0;
            mRicochetCount = 0;
            mShipDetails = null;
            mCannonDetails = null;
            mWake = null;
            mSinkingClip = null;
            mBurningClip = null;
            mRicochetBonus = 0;
            mPlayerCannonInfamyBonus = 0;
            mMutinyReduction = 1;

            mAngVelUpright = 0.5f;
            mNumCostumeImages = ShipDetails.NUM_NPC_COSTUME_IMAGES;
            mCostumeUprightIndex = mNumCostumeImages / 2;
            mCostumeIndex = mCostumeUprightIndex;
            mWardrobe = null;
            mCostumeImages = null;
            mCurrentCostumeImages = null;

            if (mCostumeStack == null)
                mCostumeStack = new List<List<SPImage>>();

            WakeFactor = kShipActorWakeFactor;

            Alpha = 1f;
            Visible = true;

            mInUse = true;
        }

        public virtual void Hibernate()
        {
            if (!InUse)
                return;

            RemoveAllPursuers();

            if (mSinkingClip != null)
            {
                mScene.Juggler.RemoveObject(mSinkingClip);
                mSinkingClip = null;
            }

            if (mBurningClip != null)
            {
                mScene.Juggler.RemoveObject(mBurningClip);
                mBurningClip = null;
            }

            if (mWardrobe != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mWardrobe);
                mWardrobe = null;
            }

            if (mShipHitGlows != null)
            {
                foreach (ShipHitGlow glow in mShipHitGlows)
                    mScene.RemoveProp(glow);
                mShipHitGlows.Clear();
            }

            if (mWake != null)
            {
                mWake.SafeDestroy();
                mWake = null;
            }

            if (mShipDetails != null)
            {
                if (mShipDetails.PoolIndex != -1)
                    mShipDetails.Hibernate();
                mShipDetails = null;
            }

            if (mCannonDetails != null)
            {
                if (mCannonDetails.PoolIndex != -1)
                    mCannonDetails.Hibernate();
                mCannonDetails = null;
            }

            mCurrentCostumeImages = null;
            mCostumeImages = null;

            if (mCostume != null)
                mCostume.RemoveAllChildren();
            if (mDeathCostume != null)
                mDeathCostume.RemoveAllChildren();
            if (mCostumeStack != null)
                mCostumeStack.Clear();
            RemoveAllChildren();

            mBody.SetActive(false);
            ClearContacts();

            mInUse = false;
            CheckinReusable(this);
        }

        protected virtual List<SPImage> SetupCostumeForTexturesStartingWith(string texturePrefix, bool cached)
        {
            List<SPTexture> costumeTextures = mScene.TexturesStartingWith(texturePrefix, cached);

            if (costumeTextures.Count == 1)
                mNumCostumeImages = costumeTextures.Count;
            mCostumeUprightIndex = mNumCostumeImages / 2;
            mCostumeIndex = mCostumeUprightIndex;

            List<SPImage> images = new List<SPImage>(mNumCostumeImages);

            for (int i = 0, frameIndex = mCostumeIndex, frameIncrement = -1; i < mNumCostumeImages; ++i)
            {
                SPImage image = new SPImage(costumeTextures[frameIndex]);
                image.ScaleX = (i < mCostumeIndex) ? -1f : 1f;
                image.X = -24 * image.ScaleX; // -18 * image.ScaleX;
                image.Y = -2 * mShipDetails.RudderOffset;
                image.Visible = (i == mCostumeIndex);
                images.Add(image);

                if (frameIndex == 0)
                    frameIncrement = 1;
                frameIndex += frameIncrement;
            }

            return images;
        }

        protected void SetImages(List<SPImage> images, SPSprite costume)
        {
            costume.RemoveAllChildren();
	
	        foreach (SPImage image in images)
            {
                image.Visible = false;
		        costume.AddChild(image);
            }
    
	        mCurrentCostumeImages = images;
    
            if (mCostumeIndex >= 0 && mCostumeIndex < mCurrentCostumeImages.Count)
            {
                SPImage image = mCurrentCostumeImages[mCostumeIndex];
                image.Visible = true;
            }
    
            UpdateCostumeWithAngularVelocity((mBody != null) ? mBody.GetAngularVelocity() : 0);
        }

        protected void EnqueueCostumeImages(List<SPImage> images)
        {
            if (images.Count != mNumCostumeImages)
                throw new ArgumentException("Enqueued costume images count must equal NumCostumeImages.");

            if (mCostumeStack.Contains(images))
                mCostumeStack.Remove(images);
            mCostumeStack.Add(images);
            SetImages(images, mCostume);
        }

        protected void DequeueCostumeImages(List<SPImage> images)
        {
            mCostumeStack.Remove(images);

            if (mCurrentCostumeImages == images)
            {
                List<SPImage> nextImages = mCostumeStack[mCostumeStack.Count - 1];
                SetImages(nextImages, mCostume);
            }
        }

        protected virtual void SaveFixture(Fixture fixture, int index)
        {
            switch (index)
            {
                case 0: mBow = fixture; break;
                case 1: mHull = fixture; break;
                case 2: mStern = fixture; mOverboard = fixture; break;
                case 3: mPort = fixture; break;
                case 4: mStarboard = fixture; break;
                default: break;
            }
        }

        public Vector2 ClosestPositionTo(Vector2 pos)
        {
            if (mBody == null)
                return Vector2.Zero;

            AABB aabb;
            mBow.GetAABB(out aabb, 0); Vector2 bowCenter = aabb.GetCenter();
            mHull.GetAABB(out aabb, 0); Vector2 hullCenter = aabb.GetCenter();
            mStern.GetAABB(out aabb, 0); Vector2 sternCenter = aabb.GetCenter();

            float bowLenSq = Vector2.DistanceSquared(bowCenter, pos);
            float hullLenSq = Vector2.DistanceSquared(hullCenter, pos);
            float sternLenSq = Vector2.DistanceSquared(sternCenter, pos);

            if (bowLenSq < hullLenSq && bowLenSq < sternLenSq)
                return bowCenter;
            else if (hullLenSq < bowLenSq && hullLenSq < sternLenSq)
                return hullCenter;
            else
                return sternCenter;
        }

        public Fixture PortOrStarboard(ShipDetails.ShipSide side)
        {
            return (side == AwesomePirates.ShipDetails.ShipSide.Port) ? mPort : mStarboard;
        }

        protected virtual void SailWithForce(float force)
        {
            if (mBody == null)
                return;
            AABB aabb;
            mBow.GetAABB(out aabb, 0);
            Vector2 bowCenter = aabb.GetCenter();
            Vector2 bodyCenter = mBody.GetWorldCenter();
            Vector2 delta = bowCenter - bodyCenter;
            delta.Normalize();
            mBody.ApplyForce(force * delta, bodyCenter);
        }

        protected virtual void TurnWithForce(float force)
        {
            if (mBody == null)
                return;
            float dir = (force < 0f) ? 1f : -1f;
            Vector2 turnVec = new Vector2(0f, Math.Abs(force));
            turnVec = Vector2.Transform(turnVec, Matrix.CreateRotationZ(mBody.GetAngle() + dir * SPMacros.PI_HALF));

            AABB aabb;
            mBow.GetAABB(out aabb, 0);
            Vector2 bowCenter = aabb.GetCenter();

            mBody.ApplyForce(turnVec, bowCenter);
        }

        public virtual void Sink()
        {
            if (mSinkingClip == null)
                mSinkingClip = new SPMovieClip(mScene.TexturesStartingWith("ship-sinking_"), SinkingClipFps);
            if (mDeathCostume == null)
                mDeathCostume = new SPSprite();
            mSinkingClip.X = -mSinkingClip.Width / 2;
            mSinkingClip.Y = -2 * mShipDetails.RudderOffset;
            mSinkingClip.CurrentFrame = 0;
            mSinkingClip.Loop = false;
            mSinkingClip.Play();
            mDeathCostume.Visible = true;
            mDeathCostume.AddChild(mSinkingClip);
            mWardrobe.AddChild(mDeathCostume);
            mScene.Juggler.AddObject(mSinkingClip);
        }

        public virtual void Burn()
        {
            if (mBurningClip == null)
                mBurningClip = new SPMovieClip(mScene.TexturesStartingWith("ship-burn_"), BurningClipFps);
            if (mDeathCostume == null)
                mDeathCostume = new SPSprite();
            mBurningClip.X = -mBurningClip.Width / 2;
            mBurningClip.Y = -2 * mShipDetails.RudderOffset;
            mBurningClip.CurrentFrame = 0;
            mBurningClip.Loop = false;
            mBurningClip.Play();
            mDeathCostume.Visible = true;
            mDeathCostume.AddChild(mBurningClip);
            mWardrobe.AddChild(mDeathCostume);
            mScene.Juggler.AddObject(mBurningClip);
            mScene.PlaySound("ShipBurn");
        }

        public virtual void AddPrisoner(string name)
        {
            mShipDetails.AddPrisoner(name);
        }

        public virtual Prisoner AddRandomPrisoner()
        {
            return mShipDetails.AddRandomPrisoner();
        }

        protected virtual void DropLoot() { }

        public virtual void DisplayExplosionGlow()
        {
            GameController gc = GameController.GC;

            if (gc.TimeOfDay >= TimeOfDay.Dusk && gc.TimeOfDay <= TimeOfDay.DawnTransition)
            {
                if (mShipHitGlows == null)
                    mShipHitGlows = new List<ShipHitGlow>();

                foreach (ShipHitGlow glow in mShipHitGlows)
                {
                    if (glow.IsCompleted)
                    {
                        glow.ReRun();
                        return;
                    }
                }

                mShipHitGlows.Add(ShipHitGlow.ShipHitGlowAt(PX, PY));
            }
        }

        public virtual void DamageShip(int damage) { }

        public virtual void DamageShipWithCannonball(Cannonball cannonball)
        {
            if (cannonball == null)
                return;

            DamageShip(cannonball.DamageFromImpact);
            DisplayExplosionGlow();
        }

        public void ApplyPerpendicularImpulseToCannonball(float force, Cannonball cannonball)
        {
            if (mBody == null || cannonball.B2Body == null)
		        return;
	        Vector2 impulse = cannonball.B2Body.GetLinearVelocity();
	        impulse.Normalize();
	
            // Apply a slight backforce to account for angled shots needing to fall shorter.
	        float impulseAdjustment = 0.325f * Math.Abs(force);
	        impulseAdjustment *= impulseAdjustment;
	
            Vector2 backImpulse = impulse;
            backImpulse *= -impulseAdjustment;
	        cannonball.B2Body.ApplyLinearImpulse(backImpulse, cannonball.B2Body.GetPosition());
	
            // Now apply perp force
            impulse *= Math.Abs(force);
            Box2DUtils.RotateVector(ref impulse, ((force < 0) ? -SPMacros.PI_HALF : SPMacros.PI_HALF));
            cannonball.B2Body.ApplyLinearImpulse(impulse, cannonball.B2Body.GetPosition());
        }

        public virtual Cannonball FireCannon(ShipDetails.ShipSide side, float trajectory)
        {
            PlayFireCannonSound();
	
	        //Cannonball cannonball = CannonFactory.Factory.CreateCannonballForShip(this, side, trajectory);
            Cannonball cannonball = Cannonball.CannonballForShip(this, null, side, trajectory, null);
	        mScene.AddActor(cannonball);
            cannonball.SetupCannonball();

            AnimateCannonSmoke(cannonball.PX, cannonball.PY, (Rotation + ((side == ShipDetails.ShipSide.Port) ? -SPMacros.PI_HALF : SPMacros.PI_HALF)));
	        return cannonball;
        }

        public virtual void AnimateCannonSmoke(float x, float y, float rotation)
        {
            CannonFire smoke = PointMovie.PointMovieWithType(PointMovie.PointMovieType.CannonFire, x, y) as CannonFire;
            smoke.CannonRotation = rotation;

            Vector2 smokeVel = new Vector2(0f, -60f);
            smokeVel = Vector2.Transform(smokeVel, Matrix.CreateRotationZ(smoke.CannonRotation));
            smoke.SetLinearVelocity(smokeVel.X, smokeVel.Y);
        }

        public virtual void PlayFireCannonSound() { }

        public virtual void PlaySunkSound()
        {
            //mScene.PlaySound("Sunk");
        }

        protected virtual void UpdateCostumeWithAngularVelocity(float angVel)
        {
            if (mNumCostumeImages == 0)
                return;
            int index = mCostumeIndex;
            float absAngVel = Math.Abs(angVel);

            // -1.1 -> 1.1
            if (absAngVel < mAngVelUpright) index = mCostumeUprightIndex;
            else if (absAngVel > (mAngVelUpright + 0.6f)) index = 0;
            else if (absAngVel > (mAngVelUpright + 0.35f) && absAngVel < (mAngVelUpright + 0.5f)) index = 1;
            else if (absAngVel > (mAngVelUpright + 0.2f) && absAngVel < (mAngVelUpright + 0.25f)) index = 2;
            else return;

            SPImage image = mCurrentCostumeImages[mCostumeIndex];
            image.Visible = false;

            if (index != mCostumeUprightIndex && angVel > 0)
                index = mNumCostumeImages - (index + 1);
            index = Math.Max(0, Math.Min(mNumCostumeImages - 1, index));

            image = mCurrentCostumeImages[index];
            image.Visible = true;
            mCostumeIndex = index;
        }

        public override void AdvanceTime(double time)
        {
            if (mShipHitGlows != null)
            {
                foreach (ShipHitGlow glow in mShipHitGlows)
                {
                    glow.X = PX;
                    glow.Y = PY;
                }
            }
        }

        protected virtual void TickWakeOdometer(float sailForce)
        {
            if (mWake == null)
                return;
            if (mWakeFactor == 0)
                throw new InvalidOperationException("Wake Factor cannot be zero or DBZ."); // TODO: protect with private accessor

            mOdometer += sailForce / mWakeFactor;
	
	        if (mOdometer >= mWakePeriod)
            {
		        mOdometer = 0.0f;
                //mWake.NextRippleAt(X, Y, Rotation);

#if false
                CircleShape bowShape = mBow.GetShape() as CircleShape;
                Vector2 bowPos = mBody.GetWorldPoint(bowShape._p);

                mWake.NextRippleAt(ResManager.M2PX(bowPos.X), ResManager.M2PY(bowPos.Y), Rotation);
#else
                CircleShape bowShape = mBow.GetShape() as CircleShape;
                CircleShape sternShape = mStern.GetShape() as CircleShape;

                Vector2 bowPos = mBody.GetWorldPoint(bowShape._p), sternPos = mBody.GetWorldPoint(sternShape._p);
                Vector2 shipVector = bowPos - sternPos; // Points from stern to bow (ie in the direction of the ship's forward movement)
                //shipVector = bowPos + shipVector / 5;
                shipVector = bowPos - shipVector / 8;
                mWake.NextRippleAt(ResManager.M2PX(shipVector.X), ResManager.M2PY(shipVector.Y), Rotation);
#endif
	        }
        }

        public virtual void OnRaceUpdate(RaceEvent ev) { }

        public void AddPursuer(IPursuer pursuer)
        {
            if (mPursuers == null)
                mPursuers = new SPHashSet<IPursuer>();
            mPursuers.Add(pursuer);
        }

        public void RemovePursuer(IPursuer pursuer)
        {
            if (mPursuers != null)
                mPursuers.Remove(pursuer);
        }

        public void RemoveAllPursuers()
        {
            if (mPursuers == null)
                return;

            SPHashSet<IPursuer> pursuers = mPursuers;
            mPursuers = null;

            foreach (IPursuer pursuer in pursuers.EnumerableSet)
                pursuer.PursueeDestroyed(this);

            pursuers.Clear();
            mPursuers = pursuers;
        }

        public override void SafeRemove()
        {
            if (mRemoveMe)
                return;
            base.SafeRemove();
            RemoveAllPursuers();
        }

        protected override void ZeroOutFixtures()
        {
            base.ZeroOutFixtures();

            mBow = null;
            mHull = null;
            mStern = null;
            mPort = null;
            mStarboard = null;
            mOverboard = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        RemoveAllPursuers();
                        mPursuers = null;

                        if (mSinkingClip != null)
                        {
                            mScene.Juggler.RemoveObject(mSinkingClip);
                            mSinkingClip.Dispose();
                            mSinkingClip = null;
                        }

                        if (mBurningClip != null)
                        {
                            mScene.Juggler.RemoveObject(mBurningClip);
                            mBurningClip.Dispose();
                            mBurningClip = null;
                        }

                        if (mWardrobe != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mWardrobe);
                            mWardrobe = null;
                        }

                        if (mShipHitGlows != null)
                        {
                            foreach (ShipHitGlow glow in mShipHitGlows)
                                mScene.RemoveProp(glow);
                            mShipHitGlows = null;
                        }

                        if (mWake != null)
                        {
                            mWake.SafeDestroy();
                            mWake = null;
                        }

                        if (mShipDetails != null)
                        {
                            if (mShipDetails.PoolIndex != -1)
                                mShipDetails.Hibernate();
                            mShipDetails = null;
                        }

                        if (mCannonDetails != null)
                        {
                            if (mCannonDetails.PoolIndex != -1)
                                mCannonDetails.Hibernate();
                            mCannonDetails = null;
                        }

                        if (mCostumeImages != null)
                        {
                            foreach (SPImage image in mCostumeImages)
                                image.Dispose();
                            mCostumeImages = null;
                        }

                        mCurrentCostumeImages = null;
                        mCostume = null;
                        mDeathCostume = null;
                        mCostumeImages = null;
                        mCostumeStack = null;
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
