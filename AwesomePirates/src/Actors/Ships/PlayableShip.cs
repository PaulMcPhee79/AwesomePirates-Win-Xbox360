#define ENABLE_OVERHEATED_CANNONS

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;
using System.Diagnostics;


namespace AwesomePirates
{
    class PlayableShip : ShipActor
    {
        public const string CUST_EVENT_TYPE_PLAYER_SHIP_SINKING = "playerShipSinkingEvent";
        public const string CUST_EVENT_TYPE_MONTY_SKIPPERED = "montySkipperedEvent";

        protected const double kCannonSpamInterval = 0.4;
        protected const double kCannonSpamCapacity = 8;

        public PlayableShip(ActorDef def, string key)
            : base(def, key)
        {
            mCategory = (int)PFCat.PLAYABLE_SHIPS;
            mCamouflaged = false;
		    mFlyingDutchman = false;
            mLaunching = true;
		    mSinking = false;
		    mMotorBoatingSob = false;
		    mTimeTravelling = false;
		    mSuspendedMode = false;
		    mDroppingKegs = false;
            mPlankEnabled = true;
            mFailedMotorboating = false;
		    mDutchmanCostumeImages = null;
		    mCamoCostumeImages = null;
            mAshProc = new AshProc();
		    mOffscreenArrow = null;
            mRaceUpdateIndex = -1;
            mDashDialFlashTimer = 0.0;
            mDutchmanTimer = 0.0;
            mTripCounter = 2;
            mPowderKegTimer = 0.0;
		    mCannonRange = (float)Math.Sqrt(mScene.ViewWidth * mScene.ViewWidth + mScene.ViewHeight * mScene.ViewHeight) / ResManager.PPM;
            mGravityFactor = 2f;
		    mSpeedNormalizer = 1;
		    mKegsRemaining = 0;
		    mNet = null;
		    mBrandySlick = null;
		    //mResOffset = nil;
            mCannonInfamyBonus = CannonballInfamyBonus.GetCannonballInfamyBonus();
        
            mRecentHitCount = 0;
            mRecentShotCount = 0;
            mCannonsOverheated = false;
            mCannonSpamCapacitor = 0;
		
		    mSpeedRatingBonus = 0;
		    mControlRatingBonus = 0;
		
			mCrewAiming = new RayCastClosest(mBody, (96f / ResManager.PPM) / mCannonRange);
            mRayCastCallback = mCrewAiming.ReportFixture;
        }

        public override void Draw(GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            base.Draw(gameTime, support, parentTransform);
        }

        #region Fields
        protected bool mCamouflaged;
        protected bool mFlyingDutchman;
        protected bool mLaunching;
        protected bool mSinking;
        protected bool mMotorBoatingSob;
        protected bool mTimeTravelling;
        protected bool mSuspendedMode;
        protected bool mDroppingKegs;
        protected bool mPlankEnabled;
        protected bool mFailedMotorboating;

        protected int mRecentHitCount;
        protected int mRecentShotCount;

        protected int mRaceUpdateIndex;

        protected bool mCannonsOverheated;
        protected double mCannonSpamCapacitor;

        protected double mDutchmanTimer;
        protected double mTripCounter;
        protected double mPowderKegTimer;
        protected double mDashDialFlashTimer;

        protected float mCannonRange;
        protected float mGravityFactor;
        protected float mSpeedNormalizer;
        protected float mDragDuration;
        protected uint mKegsRemaining;
        protected CannonballInfamyBonus mCannonInfamyBonus;

        protected int mSpeedRatingBonus;
        protected int mControlRatingBonus;

        protected AshProc mAshProc;
        protected RayCastCallback mRayCastCallback;
        protected RayCastClosest mCrewAiming;
        protected OffscreenArrow mOffscreenArrow;

        protected NetActor mNet;
        protected BrandySlickActor mBrandySlick;

        protected List<SPImage> mDutchmanCostumeImages;
        protected List<SPImage> mCamoCostumeImages;

        protected VibrationDescriptor mVibrationDamageDescriptor = new VibrationDescriptor(0.75f, 0.6, 0f, 0);
        #endregion

        #region Properties
        public bool AssistedAiming { get { return true; } }
        public bool IsCamouflaged { get { return mCamouflaged; } }
        public bool IsFlyingDutchman { get { return mFlyingDutchman; } }
        public virtual AshProc AshProc { get { return mAshProc; } set { mAshProc = value; } }
        public bool MotorBoating { get { return mMotorBoatingSob; } set { mMotorBoatingSob = value; } }
        public bool SuspendedMode { get { return mSuspendedMode; } }
        public bool Launching { get { return mLaunching; } set { mLaunching = value; } }
        public bool Sinking { get { return mSinking; } set { mSinking = value; } }
        public uint KegsRemaining { get { return mKegsRemaining; } }
        public CannonballInfamyBonus CannonInfamyBonus
        {
            get { return mCannonInfamyBonus; }
            set
            {
                if (mCannonInfamyBonus != null && mCannonInfamyBonus.PoolIndex != -1)
                    mCannonInfamyBonus.Hibernate();
                mCannonInfamyBonus = value;
            }
        }
        public double CannonSpamCapacitor { get { return mCannonSpamCapacitor; } }
        public int SpeedRatingBonus { get { return mSpeedRatingBonus; } set { mSpeedRatingBonus = value; } }
        public int ControlRatingBonus { get { return mControlRatingBonus; } set { mControlRatingBonus = value; } }
        public uint ProcType
        {
            get
            {
                uint value = 0;

                if (mAshProc.IsActive)
                    value = mAshProc.Proc;
                return value;
            }
        }
        public NetActor Net  { get { return mNet; } }
        public BrandySlickActor BrandySlick { get { return mBrandySlick; } }
        protected virtual string NormalShotType { get { return (mFlyingDutchman) ? "dutchman-shot_" : "single-shot_"; } }
        protected float RecentCannonAccuracy { get { return ((mRecentShotCount == 0) ? 0 : mRecentHitCount / (float)mRecentShotCount); } }
        protected virtual uint KegStyleKey { get { return PowderKegActor.kCareerPowderKegStyleKey; } }
        protected virtual Color NetColor { get { return Color.Gray; } }
        protected virtual string BrandyFlameTexName { get { return "brandy-flame_"; } }
        protected virtual PlayerCannon RightCannon { get { return null; } }
        protected virtual PlayerCannon LeftCannon { get { return null; } }
        protected virtual Helm Helm { get { return null; } }
        public virtual double ReloadInterval { get { return (mCannonDetails != null) ? mCannonDetails.ReloadInterval : 0.5; } }
        #endregion

        #region Methods
        public override void SetupShip()
        {
            mCannonDetails.ShotType = "single-shot_";

            if (mWakeCount == -1)
            {
                mWakeCount = Wake.DefaultWakeBufferSize;

                if (mMotorBoatingSob)
                    mWakePeriod = Wake.DefaultWakePeriod / 4.5f;
            }

            base.SetupShip();

            CalcSailForces();

            if (mWake != null)
            {
                if (mMotorBoatingSob)
                    mWake.RipplePeriod = Wake.MinRipplePeriod;
                else
                    mWake.RipplePeriod = Math.Min(Wake.DefaultRipplePeriod, Wake.DefaultRipplePeriod * Math.Max(Wake.MinRipplePeriod, ShipActor.DefaultSailForceMax / Math.Max(1, mSailForceMax)));
            }

            // Costume
            mCostume = new SPSprite();
            mWardrobe = new SPSprite();
            mWardrobe.AddChild(mCostume);
            AddChild(mWardrobe);

            SetupCostumeImages();

            EnqueueCostumeImages(mCostumeImages);

            mOffscreenArrow = CreateOffscreenArrow();
            mScene.AddProp(mOffscreenArrow);

            PlayerCannon rightCannon = RightCannon, leftCannon = LeftCannon;

            if (rightCannon != null && leftCannon != null)
            {
                rightCannon.ReloadInterval = ReloadInterval;
                rightCannon.Overheat(false);
                //rightCannon.AddEventListener(PlayerCannonFiredEvent.CUST_EVENT_TYPE_PLAYER_CANNON_FIRED, (PlayerCannonFiredEventHandler)OnPlayerCannonFired);

                leftCannon.AddEventListener(PlayerCannonFiredEvent.CUST_EVENT_TYPE_PLAYER_CANNON_FIRED, (PlayerCannonFiredEventHandler)OnPlayerCannonFired);
                leftCannon.ReloadInterval = ReloadInterval;
                leftCannon.Overheat(false);
            }
            
            UpdateCannonInfamyBonus();
            SetupCustomizations();
        }

        protected virtual OffscreenArrow CreateOffscreenArrow()
        {
            return new OffscreenArrow(new Vector4(-20.0f, -20.0f, 980.0f, 590.0f));
        }

        protected virtual void SetupCostumeImages()
        {
            if (mMotorBoatingSob)
                mCostumeImages = SetupCostumeForTexturesStartingWith("ship-pf-speedboat_", false);
            else
            {
                mCostumeImages = SetupCostumeForTexturesStartingWith("ship-pf-sloop_", false);
                mDutchmanCostumeImages = SetupCostumeForTexturesStartingWith("ship-pf-dutchman_", false);
                mCamoCostumeImages = SetupCostumeForTexturesStartingWith("ship-pf-navy_", true);
            }
        }

        protected virtual void SetupCustomizations() { }

        protected override List<SPImage> SetupCostumeForTexturesStartingWith(string texturePrefix, bool cached)
        {
            if (!mMotorBoatingSob)
                return base.SetupCostumeForTexturesStartingWith(texturePrefix, cached);

            mNumCostumeImages = 11;
            mCostumeUprightIndex = mNumCostumeImages / 2;

            List<SPTexture> costumeTextures = mScene.TexturesStartingWith(texturePrefix, cached);
            List<SPImage> images = new List<SPImage>(mNumCostumeImages);
            mCostumeIndex = mCostumeUprightIndex;

            for (int i = 0, frameIndex = mCostumeIndex, frameIncrement = -1; i < mNumCostumeImages; ++i)
            {
                SPImage image = new SPImage(costumeTextures[frameIndex]);
		        image.ScaleX = (i < mCostumeIndex) ? 1f : -1f;
		        image.X = -18 * image.ScaleX;
		        image.Y = -2 * mShipDetails.RudderOffset;
		        image.Visible = (i == mCostumeIndex);
                images.Add(image);
		
		        if (frameIndex == 0)
			        frameIncrement = 1;
		        frameIndex += frameIncrement;
	        }
	
	        return images;
        }

        public void EnableSuspendedMode(bool enable) { mSuspendedMode = enable; }

        public override void Flip(bool enable)
        {
            mOffscreenArrow.Flip(enable);
        }

        protected override void UpdateCostumeWithAngularVelocity(float angVel)
        {
            if (!mMotorBoatingSob)
            {
                base.UpdateCostumeWithAngularVelocity(angVel);
                return;
            }

            if (mNumCostumeImages == 0)
                return;

            int index = mCostumeIndex;
            float absAngVel = Math.Abs(angVel);

            // -3.55 -> 3.55
            if (absAngVel < mAngVelUpright) index = mCostumeUprightIndex;
            else if (absAngVel > (mAngVelUpright + 2.4f)) index = 0;
            else if (absAngVel > (mAngVelUpright + 1.9f) && absAngVel < (mAngVelUpright + 2.25f)) index = 1;
            else if (absAngVel > (mAngVelUpright + 1.4f) && absAngVel < (mAngVelUpright + 1.75f)) index = 2;
            else if (absAngVel > (mAngVelUpright + 0.8f) && absAngVel < (mAngVelUpright + 1.25f)) index = 3;
            else if (absAngVel > (mAngVelUpright + 0.3f) && absAngVel < (mAngVelUpright + 0.65f)) index = 4;
            else return;

            SPImage image = mCurrentCostumeImages[mCostumeIndex];
            image.Visible = false;

            if (index != mCostumeUprightIndex && angVel > 0)
                index = mNumCostumeImages - (index + 1);

            image = mCurrentCostumeImages[index];
            image.Visible = true;
            mCostumeIndex = index;
        }

        public override Prisoner AddRandomPrisoner()
        {
            ++mScene.AchievementManager.Hostages;
            return base.AddRandomPrisoner();
        }

        protected virtual void ActivateCannonProc()
        {
	        mCannonDetails.ShotType = mAshProc.TexturePrefix;
            UpdateCannonInfamyBonus();
        }

        protected virtual void DeactivateCannonProc()
        {
            mCannonDetails.ShotType = NormalShotType;
            UpdateCannonInfamyBonus();
        }

        public void PlayCannonProcSound()
        {
            // Do nothing
        }

        protected virtual void UpdateCannonInfamyBonus()
        {
            mCannonInfamyBonus.RicochetBonus = mCannonDetails.RicochetBonus;
	
	        if (mAshProc != null && mAshProc.IsActive)
            {
		        mCannonInfamyBonus.ProcType = mAshProc.Proc;
		        mCannonInfamyBonus.ProcMultiplier = (int)mAshProc.Multiplier;
		        mCannonInfamyBonus.ProcAddition = (int)mAshProc.Addition;
		        mCannonInfamyBonus.RicochetAddition = (int)mAshProc.RicochetAddition;
		        mCannonInfamyBonus.RicochetMultiplier = mAshProc.RicochetMultiplier;
                mCannonInfamyBonus.MiscBitmap = (IsFlyingDutchman) ? Ash.ASH_DUTCHMAN_SHOT : 0;
	        }
            else
            {
		        mCannonInfamyBonus.ProcType = 0;
		        mCannonInfamyBonus.ProcMultiplier = 1;
		        mCannonInfamyBonus.ProcAddition = 0;
		        mCannonInfamyBonus.RicochetAddition = 0;
		        mCannonInfamyBonus.RicochetMultiplier = 1;
                mCannonInfamyBonus.MiscBitmap = (IsFlyingDutchman) ? Ash.ASH_DUTCHMAN_SHOT : 0;
	        }
        }

        private void CalcSailForces()
        {
            if (mBody == null)
                return;

            float speedRating = (mShipDetails.SpeedRating + mSpeedRatingBonus) * mSpeedModifier;
            float controlRating = (mShipDetails.ControlRating + mControlRatingBonus) * mControlModifier;
            // Max details.speedRating will be 6 which would return a unity scaling factor * 1.6f. +0.16f adds 10% of best ship to all ships so as to shift entire scale upwards.
            mSpeedNormalizer = (mMotorBoatingSob ? 2.0f : 2.4f) * ((16 + speedRating) / 20.0f) + (2.0f / 10.0f);
            mSailForceMax = 10 * mBody.GetMass() * mSpeedNormalizer;
            mSailForce = mSailForceMax;
            mTurnForceMax = 1.65f * mBody.GetMass() * ((21 + controlRating) / 20.0f);
            mTurnForceMax *= (mMotorBoatingSob ? 1.15f : 1.5f);
        }

        protected float CannonTrajectoryFromDetails(CannonDetails details) { return 1f; }

        protected virtual void PlayerCannonWasRequestedToFire(PlayerCannon cannon)
        {
            PlayerCannon leftCannon = LeftCannon, rightCannon = RightCannon;

            if (leftCannon == null || rightCannon == null || Launching || Sinking)
                return;

            if (leftCannon.Reloading || rightCannon.Reloading || leftCannon.Overheated || rightCannon.Overheated)
                return;

            int cannonFireMap = FireAssistedCannons();
            bool manual = cannonFireMap == 0, silentCannon = false;

            if ((cannonFireMap & (1 << (int)ShipDetails.ShipSide.Port)) == (1 << (int)ShipDetails.ShipSide.Port))
            {
                leftCannon.Fire(silentCannon);
                silentCannon = true;
            }
            else if (!manual)
                leftCannon.Reload(); // Suppress events from leftCannon while right is reloading

            if ((cannonFireMap & (1 << (int)ShipDetails.ShipSide.Starboard)) == (1 << (int)ShipDetails.ShipSide.Starboard))
                rightCannon.Fire(silentCannon);
            else if (!manual)
                rightCannon.Reload(); // Suppress events from rightCannon while left is reloading

            if (manual)
            {
                int numShots = 1;
                bool hasProcced = mAshProc.IsActive;
                Cannonball cannonball = null;
                CannonballGroup grp = CannonballGroup.GetCannonballGroup(1);
                mScene.AddProp(grp);

                if (hasProcced && mAshProc.Proc == Ash.ASH_MOLTEN)
                {
                    numShots += 2;

                    if (mScene.GameMode == GameMode.Career && (mScene.MasteryManager.MasteryBitmap & CCMastery.CANNON_SCORCHED_HORIZON) != 0)
                        numShots += 2;
                }

                for (ShipDetails.ShipSide side = ShipDetails.ShipSide.Port; side <= ShipDetails.ShipSide.Starboard; ++side)
                {
                    if (numShots > 1)
                    {
                        float perpForce = 0;
                        cannonball = null;

                        for (int i = 0; i < numShots; ++i)
                        {
                            if (i == 0) perpForce = 0;
                            else if (i == 1) perpForce = 3.5f;
                            else if (i == 2) perpForce = -3.5f;
                            else if (i == 3) perpForce = 5.0f;
                            else if (i == 4) perpForce = -5.0f;
                            else if (i == 5) perpForce = 6.5f;
                            else if (i == 6) perpForce = -6.5f;
                            else perpForce = 0;

                            cannonball = FireCannon(side, cannon.Elevation / 3f);

                            if (i != 0)
                                ApplyPerpendicularImpulseToCannonball(perpForce, cannonball);
                            cannonball.HasProcced = hasProcced;

                            if (grp != null && cannonball != null)
                                grp.AddCannonball(cannonball);
                        }
                    }
                    else
                    {
                        cannonball = FireCannon(side, cannon.Elevation / 3f);

                        if (grp != null && cannonball != null)
                            grp.AddCannonball(cannonball);
                    }
                }

                leftCannon.Fire(true);
                rightCannon.Fire(false);
            }

            if (mAshProc.IsActive && mAshProc.ConsumeCharge() == 0)
            {
                DeactivateCannonProc();
                mAshProc.Deactivate();
            }

#if ENABLE_OVERHEATED_CANNONS
            mCannonSpamCapacitor += cannon.ReloadInterval + kCannonSpamInterval;

            if (!IsFlyingDutchman && mCannonSpamCapacitor > kCannonSpamCapacity && RecentCannonAccuracy < 0.65f)
                DisableOverheatedCannons(true);
#endif

#if IOS_SCREENS
            if ((cannonFireMap & (1 << (int)ShipDetails.ShipSide.Starboard)) == (1 << (int)ShipDetails.ShipSide.Starboard) &&
                (cannonFireMap & (1 << (int)ShipDetails.ShipSide.Port)) != (1 << (int)ShipDetails.ShipSide.Port))
                leftCannon.FireIOS();
#endif
        }

        protected virtual void OnPlayerCannonFired(PlayerCannonFiredEvent ev)
        {
            if (ev != null)
                PlayerCannonWasRequestedToFire(ev.Cannon);
        }

        protected Vector2 CannonVectorForSide(ShipDetails.ShipSide side, Vector2 from)
        {
            Vector2 to = new Vector2(0f, mCannonRange); // The diagonal length of the playfield
            float angle = ((mBody != null) ? mBody.GetAngle() : 0) + ((side == ShipDetails.ShipSide.Port) ? SPMacros.PI_HALF : -SPMacros.PI_HALF);
            //Vector2.Transform(to, Matrix.CreateRotationZ(angle));
            Box2DUtils.RotateVector(ref to, angle);
            to.X += from.X;
            to.Y += from.Y;
            return to;
        }

        protected virtual int FireAssistedCannons()
        {
            if (mBody == null)
                return 0;

            int cannonFireMap = 0;
            CannonballGroup grp = null;

            CircleShape bowShape = mBow.GetShape() as CircleShape;
            CircleShape sternShape = mStern.GetShape() as CircleShape;

            for (ShipDetails.ShipSide side = ShipDetails.ShipSide.Port; side <= ShipDetails.ShipSide.Starboard; ++side)
            {
                CircleShape cannonShape = PortOrStarboard(side).GetShape() as CircleShape;

                mCrewAiming.ResetFixture();
                Vector2 cannonPos = mBody.GetWorldPoint(cannonShape._p);
                Vector2 from = cannonPos;
                Vector2 to = CannonVectorForSide(side, from);
                mScene.World.RayCast(mRayCastCallback, from, to);

                Vector2 bowPos = mBody.GetWorldPoint(bowShape._p), sternPos = mBody.GetWorldPoint(sternShape._p);
                Vector2 shipVector = bowPos - sternPos; // Points from stern to bow (ie in the direction of the ship's forward movement)

                // If the shot is not aimed perfectly, allow for a cone of leniency left and right of the real target.
                if (mCrewAiming.Fixture == null)
                {
                    for (int retry = 0; retry < 2 && mCrewAiming.Fixture == null; ++retry)
                    {
                        Vector2 shipVectorAdjust = shipVector * (retry + 1); // 1f, 2f
                        Vector2 toAdjust = to;
                        toAdjust += shipVectorAdjust;
                        mScene.World.RayCast(mRayCastCallback, from, toAdjust);

                        if (mCrewAiming.Fixture == null)
                        {
                            toAdjust = to;
                            toAdjust -= shipVectorAdjust;
                            mScene.World.RayCast(mRayCastCallback, from, toAdjust);
#if true
                        }
                    }
                }
#else
                            if (mCrewAiming.Fixture == null)
                                Debug.WriteLine("XXXXXXXXX MISS");
                            else
                                Debug.WriteLine("$$$$$$$ STERN ADJUSTED HIT");
                        }
                        else
                        {
                            Debug.WriteLine("$$$$$$$ BOW ADJUSTED HIT");
                        }
                    }
	            }
                else
                {
                    Debug.WriteLine("$$$$$$$ DIRECT HIT");
                }
#endif

                Cannonball cannonball = null, prevCannonball = null;

                if (mCrewAiming.Fixture == null)
                    mCrewAiming.Fixture = mCrewAiming.GlancingFixture;

                if (mCrewAiming.Fixture != null)
                {
                    Body body = mCrewAiming.Fixture.GetBody();

                    if (body != null)
                    {
                        bool hasProcced = mAshProc.IsActive;
                        float perpForce = 0;
                        int numShots = 1;

                        cannonFireMap |= 1 << (int)side;

                        if (hasProcced && mAshProc.Proc == Ash.ASH_MOLTEN)
                        {
                            numShots += 2;

                            if ((mScene.MasteryManager.MasteryBitmap & CCMastery.CANNON_SCORCHED_HORIZON) != 0)
                                numShots += 2;
                        }

                        if (grp == null)
                        {
                            grp = CannonballGroup.GetCannonballGroup(1);
                            mScene.AddProp(grp);
                        }
                        //else // Uncomment to make sure double-shots require both to hit.
                        //    grp.hitQuota += 1;

                        for (int i = 0; i < numShots; ++i)
                        {
                            if (i == 0) perpForce = 0;
                            else if (i == 1) perpForce = 3.5f;
                            else if (i == 2) perpForce = -3.5f;
                            else if (i == 3) perpForce = 5.0f;
                            else if (i == 4) perpForce = -5.0f;
                            else if (i == 5) perpForce = 6.5f;
                            else if (i == 6) perpForce = -6.5f;
                            else perpForce = 0;

                            if (prevCannonball == null && cannonball != null)
                                prevCannonball = cannonball;

                            Vector2 target = body.GetPosition();
                            cannonball = Cannonball.CannonballForShip(this, shipVector, side, 1f, target);

                            if (prevCannonball == null)
                                cannonball.CalculateTrajectory(body);
                            else
                            {
                                cannonball.CopyTrajectoryFrom(prevCannonball);
                                cannonball.B2Body.SetLinearVelocity(prevCannonball.B2Body.GetLinearVelocity());
                            }

                            mScene.AddActor(cannonball);
                            cannonball.SetupCannonball();

                            AnimateCannonSmoke(cannonball.PX, cannonball.PY, (Rotation + ((side == AwesomePirates.ShipDetails.ShipSide.Port) ? -SPMacros.PI_HALF : SPMacros.PI_HALF)));

                            if (i != 0)
                                ApplyPerpendicularImpulseToCannonball(perpForce, cannonball);
                            cannonball.HasProcced = hasProcced;

                            if (grp != null && cannonball != null)
                                grp.AddCannonball(cannonball);
                            mCannonSoundEnabled = false;
                        }
                        mCannonSoundEnabled = true;
                        //NSLog(@"CREW-ASSISTED SHOT TAKEN.");
                    }
                }
            }
            
            return cannonFireMap;
        }

        protected void DisableOverheatedCannons(bool disable)
        {
#if ENABLE_OVERHEATED_CANNONS
            if (disable == mCannonsOverheated || mScene.GameMode != GameMode.Career)
                return;

            if (disable)
            {
                mCannonSpamCapacitor = Math.Min(mCannonSpamCapacitor, kCannonSpamCapacity);
                mScene.PlaySound("CannonOverheat");
                mScene.DisplayTickerHint("Tip: Reduce your rate of fire or increase your accuracy to prevent the cannons overheating.");
            }
            if (LeftCannon != null)
                LeftCannon.Overheat(disable);
            if (RightCannon != null)
                RightCannon.Overheat(disable);
            mCannonsOverheated = disable;
#endif
        }

        public override Cannonball FireCannon(ShipDetails.ShipSide side, float trajectory)
        {
            Cannonball cannonball = base.FireCannon(side, trajectory * CannonTrajectoryFromDetails(mCannonDetails) * (2.0f / mGravityFactor));
            cannonball.HasProcced = mAshProc.IsActive;
	        return cannonball;
        }

        public override void AdvanceTime(double time)
        {
            base.AdvanceTime(time);

            if (mFlyingDutchman)
            {
                if (mDutchmanTimer > 0.0)
                    mDutchmanTimer -= time;

                if (mDutchmanTimer <= 0.0)
                    DeactivateFlyingDutchman();
            }

            PlayerCannon leftCannon = LeftCannon;
            if (leftCannon != null && leftCannon.WasRequestedToFire)
            {
                leftCannon.WasRequestedToFire = false;
                PlayerCannonWasRequestedToFire(leftCannon);
            }
        }
        
        public override bool PreSolve(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
	        if (other is ShipActor)
		        return !mFlyingDutchman;

            return base.PreSolve(other, fixtureSelf, fixtureOther, contact);
        }

        protected override void TickWakeOdometer(float sailForce)
        {
            if (!mMotorBoatingSob)
            {
                base.TickWakeOdometer(sailForce);
                return;
            }

            if (mWake == null)
                return;
            if (mWakeFactor == 0)
#if DEBUG
                throw new InvalidOperationException("Wake Factor cannot be zero or DBZ."); // TODO: protect with private accessor
#else
                return;
#endif

            mOdometer += sailForce / mWakeFactor;

            if (mOdometer >= mWakePeriod)
            {
                mOdometer = 0.0f;

                CircleShape sternShape = mStern.GetShape() as CircleShape;
                Vector2 sternPos = mBody.GetWorldPoint(sternShape._p);
                mWake.NextRippleAt(ResManager.M2PX(sternPos.X), ResManager.M2PY(sternPos.Y), Rotation);
            }
        }

        public virtual void DropPowderKegs(uint quantity)
        {
            if (!mDroppingKegs && !mSinking)
            {
		        mDroppingKegs = true;
		        mKegsRemaining = quantity;
                DropPowderKeg();
	        }
        }

        protected virtual void DropPowderKeg()
        {
            if (mKegsRemaining > 0 && !mSinking && mBody != null)
            {
		        Vector2 loc = mBody.GetPosition();
                PowderKegActor keg = PowderKegActor.PowderKegActorAt(KegStyleKey, loc.X, loc.Y, 0);
                CustomizePowderKeg(keg);
                mScene.AddActor(keg);
                mScene.PlaySound("KegDrop");
                mPowderKegTimer = Math.Max(1, 2.0f / Math.Max(1, mSpeedNormalizer));
		        --mKegsRemaining;
	        }
        }

        protected virtual void CustomizePowderKeg(PowderKegActor keg) { }

        protected void DropNextPowderKeg()
        {
            if (mKegsRemaining > 0)
                DropPowderKeg();
            else
                mDroppingKegs = false;
        }

        public virtual NetActor DeployNet(float scale, float duration)
        {
            if (mBody == null)
		        return null;
	        Vector2 loc = mBody.GetPosition();
	        return DeployNet(loc.X, loc.Y, B2Rotation, scale, duration, false);
        }

        public virtual NetActor DeployNet(float x, float y, float rotation, float scale, float duration, bool ignited)
        {
            if (mNet != null)
#if DEBUG
                throw new InvalidOperationException("Playableship already has a net deployed.");
#else
                return null;
#endif

            if (!mSinking)
            {
                mNet = NetActor.GetNetActor(x, y, rotation, scale * ResManager.RESM.GameFactorArea, duration, NetColor);
                CustomizeNet(mNet);
                mNet.AddActionEventListener(NetActor.CUST_EVENT_TYPE_NET_DESPAWNED, new Action<SPEvent>(OnNetDespawned));

		        if (ignited)
                    mNet.Ignite();

                mScene.AddActor(mNet);
                mScene.PlaySound("NetCast");
	        }
	        return mNet;
        }

        protected virtual void CustomizeNet(NetActor net) { }

        public virtual void DespawnNetOverTime(float duration)
        {
            if (mNet != null)
                mNet.DespawnOverTime(duration);
        }

        protected virtual void OnNetDespawned(SPEvent ev)
        {
            if (mNet != null)
                mNet.RemoveEventListener(NetActor.CUST_EVENT_TYPE_NET_DESPAWNED);
            mNet = null;
        }

        public virtual BrandySlickActor DeployBrandySlick(float duration)
        {
            if (mBody == null)
                return null;

            Vector2 loc = mBody.GetPosition();
            return DeployBrandySlick(loc.X, loc.Y, B2Rotation, 1, duration, false);
        }

        public virtual BrandySlickActor DeployBrandySlick(float x, float y, float rotation, float scale, float duration, bool ignited)
        {
            if (mBrandySlick != null)
#if DEBUG
                throw new InvalidOperationException("Playableship already has a brandy slick deployed.");
#else
                return null;
#endif
            if (!mSinking)
            {
                mBrandySlick = BrandySlickActor.GetBrandySlickActor(x, y, rotation, 1.1f * scale * ResManager.RESM.GameFactorArea, duration, BrandyFlameTexName);
                CustomizeBrandySlick(mBrandySlick);
                mBrandySlick.AddActionEventListener(BrandySlickActor.CUST_EVENT_TYPE_BRANDY_SLICK_DESPAWNED, new Action<SPEvent>(OnBrandySlickDespawned));
		
		        if (ignited)
                    mBrandySlick.Ignite();
                mScene.AddActor(mBrandySlick);
                mScene.PlaySound("BrandyPour");
	        }
	        return mBrandySlick;
        }

        protected virtual void CustomizeBrandySlick(BrandySlickActor brandySlick) { }

        protected virtual void OnBrandySlickDespawned(SPEvent ev)
        {
            if (mBrandySlick != null)
                mBrandySlick.RemoveEventListener(BrandySlickActor.CUST_EVENT_TYPE_BRANDY_SLICK_DESPAWNED);
            mBrandySlick = null;
        }

        public virtual void ActivateCamouflage()
        {
            if (!mCamouflaged && !mSinking)
            {
                mCamouflaged = true;
                EnqueueCostumeImages(mCamoCostumeImages);
                mScene.PlaySound("Camo");
            }
        }

        public virtual void DeactivateCamouflage()
        {
            if (mCamouflaged && !MarkedForRemoval)
            {
                mCamouflaged = false;
                DequeueCostumeImages(mCamoCostumeImages);

                if (TurnID == GameController.GC.ThisTurn.TurnID)
                    mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_VOODOO_GADGET_EXPIRED, Idol.GADGET_SPELL_CAMOUFLAGE);
            }
        }

        public virtual void ActivateFlyingDutchman(float duration)
        {
            if (!mFlyingDutchman && !mSinking)
            {
                mFlyingDutchman = true;
                mDutchmanTimer = duration;
                DisableOverheatedCannons(false);
                EnqueueCostumeImages(mDutchmanCostumeImages);
                mScene.PlaySound("FlyingDutchman");

                if (!mAshProc.IsActive)
			        mCannonDetails.ShotType = "dutchman-shot_";
        
                if (mDragDuration > 0)
                {
                    mDragDuration = 0;
                    Drag = 1;
                }
            }
        }

        public virtual void DeactivateFlyingDutchman()
        {
            if (mFlyingDutchman && !MarkedForRemoval)
            {
                mFlyingDutchman = false;
                mDutchmanTimer = 0.0;
                DequeueCostumeImages(mDutchmanCostumeImages);

                if (!mAshProc.IsActive)
			        mCannonDetails.ShotType = "single-shot_";

                if (TurnID == GameController.GC.ThisTurn.TurnID)
                    mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_VOODOO_GADGET_EXPIRED, Idol.VOODOO_SPELL_FLYING_DUTCHMAN);

#if ENABLE_OVERHEATED_CANNONS
                // Shave off any extra that has been accumulated while in Flying Dutchman
                if (mCannonSpamCapacitor > kCannonSpamCapacity)
                    mCannonSpamCapacitor = kCannonSpamCapacity / 2;
#endif
            }
        }

        protected void ChanceAshProc()
        {
            if (mAshProc.SpecialProcEventKey != null && GameController.GC.NextRandom(1, 1000) <= (int)(1000 * mAshProc.SpecialChanceToProc))
                DispatchEvent(SPEvent.SPEventWithType(mAshProc.SpecialProcEventKey));
        }

        public void CannonballHitTarget(bool hit, bool ricochet, bool proc)
        {
#if ENABLE_OVERHEATED_CANNONS
            if (mCannonSpamCapacitor > (kCannonSpamCapacity / 4))
            {
                if (!ricochet)
                {
                    ++mRecentShotCount;

                    if (hit)
                    {
                        // Compensate Molten Shot for false negatives.
                        mRecentHitCount = Math.Min(mRecentShotCount, ProcType == Ash.ASH_MOLTEN ? mRecentHitCount + 2 : mRecentHitCount + 1);
                    }
                }
            }
            else
            {
                mRecentShotCount = 0;
                mRecentHitCount = 0;
            }
#endif
        }

        public virtual void PrepareForGameOver()
        {
            if (MarkedForRemoval)
                return;

            mScene.RemoveProp(mOffscreenArrow, false);
            mScene.Juggler.RemoveTweensWithTarget(mCostume);
    
            SPTween tween = new SPTween(mCostume, 1f);
            tween.AnimateProperty("Alpha", 0);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnCostumeFaded);
            mScene.Juggler.AddObject(tween);
        }

        private void OnCostumeFaded(SPEvent ev)
        {
            mScene.RemoveActor(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mNet != null)
                        {
                            mNet.RemoveEventListener(NetActor.CUST_EVENT_TYPE_NET_DESPAWNED);
                            mNet = null;
                        }

                        if (mBrandySlick != null)
                        {
                            mBrandySlick.RemoveEventListener(BrandySlickActor.CUST_EVENT_TYPE_BRANDY_SLICK_DESPAWNED);
                            mBrandySlick = null;
                        }

                        if (mOffscreenArrow != null)
                        {
                            mScene.RemoveProp(mOffscreenArrow, false);
                            mOffscreenArrow.Dispose();
                            mOffscreenArrow = null;
                        }

                        if (mDutchmanCostumeImages != null)
                        {
                            foreach (SPImage image in mDutchmanCostumeImages)
                                image.Dispose();
                            mDutchmanCostumeImages = null;
                        }

                        if (mCamoCostumeImages != null)
                        {
                            foreach (SPImage image in mCamoCostumeImages)
                                image.Dispose();
                            mCamoCostumeImages = null;
                        }

                        CannonInfamyBonus = null;
                        mAshProc = null;
                        mDutchmanCostumeImages = null;
                        mCamoCostumeImages = null;
                        mCrewAiming = null;
                        mRayCastCallback = null;
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
