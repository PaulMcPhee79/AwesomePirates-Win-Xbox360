using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class PowderKegActor : Actor, IIgnitable, IReusable
    {
        public const uint kCareerPowderKegStyleKey = 4;
        public static readonly uint[] kSKPowderKegStyleKeys = new uint[] { 0, 1, 2, 3 };
        private static ReusableCache sCache = null;
        private static bool sCaching = false;
        private static readonly string[] sKegTextureNames = new string[]
        {
            "sk-powder-keg-p0",
            "sk-powder-keg-p1",
            "sk-powder-keg-p2",
            "sk-powder-keg-p3",
            "powder-keg"
        };

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            sCache = new ReusableCache(5);

            int cacheSize = 24;
            uint reuseKey = kCareerPowderKegStyleKey;
            PowderKegActor keg = null;
            sCache.AddKey(cacheSize, reuseKey);

            for (int i = 0; i < cacheSize; ++i)
            {
                keg = PowderKegActorAt(kCareerPowderKegStyleKey, -200, -200, 0);
                keg.ReuseKey = reuseKey;
                keg.Hibernate();
                sCache.AddReusable(keg);
            }

            foreach (uint key in kSKPowderKegStyleKeys)
            {
                sCache.AddKey(cacheSize, key);

                for (int i = 0; i < cacheSize; ++i)
                {
                    keg = PowderKegActorAt(key, -200, -200, 0);
                    keg.ReuseKey = key;
                    keg.Hibernate();
                    sCache.AddReusable(keg);
                }
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

        public static PowderKegActor PowderKegActorAt(uint styleKey, float x, float y, float angle)
        {
            PowderKegActor actor = CheckoutReusable(styleKey) as PowderKegActor;

            if (actor != null)
            {
                actor.Reuse();

                Body body = actor.B2Body;
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);
                body.SetTransform(new Vector2(x, y), angle);
                body.SetActive(true);
                body.ApplyLinearImpulse(new Vector2(0.05f, 0.05f), body.GetPosition()); // Hack to ignite Box2D contacts on motionless bodies.

                actor.X = actor.PX;
                actor.Y = actor.PY;
                actor.Rotation = -actor.B2Rotation;
            }
            else
            {
                actor = new PowderKegActor(MiscFactory.Factory.CreatePowderKegDef(x, y, angle), styleKey);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed PowderKegActor ReusableCache.");
#endif
            }

            return actor;
        }

        private static readonly string[] kExplosionSounds = { "Explosion1", "Explosion2", "Explosion3" };

        public PowderKegActor(ActorDef def, uint styleKey)
            : base(def)
        {
            mCategory = (int)PFCat.POINT_MOVIES;
		    mAdvanceable = true;
		    mDetonated = false;
            mStyleKey = styleKey;
            mInUse = true;
            mPoolIndex = -1;
		    mCostume = null;
            mBobTween = null;
            SetupActorCostume();
        }

        #region Fields
        private bool mInUse;
        private uint mReuseKey;
        private int mPoolIndex;

        private bool mDetonated;
        private uint mStyleKey;
        private SPSprite mCostume;
        private SPTween mBobTween;
        #endregion

        #region Propterties
        public uint ReuseKey { get { return mReuseKey; } protected set { mReuseKey = value; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }

        public bool Ignited { get { return mDetonated; } }
        public SKTeamIndex OwnerID { get; set; }
        #endregion

        #region Methods
        private void SetupActorCostume()
        {
            if (mCostume == null)
            {
                mBobTween = null; // Make sure not to reuse bob tween on old costume
                mCostume = new SPSprite();

                SPImage image = new SPImage(mScene.TextureByName(PowderKegActor.KegTextureNameForKey(mStyleKey)));
                image.X = -image.Width / 2;
                image.Y = -image.Height / 2;
                mCostume.AddChild(image);
            }

            AddChild(mCostume);
            
	        X = PX;
	        Y = PY;
	        Rotation = -mBody.GetAngle();
	
	        // Make the keg appear to bob up and down in the water
            if (mBobTween == null)
            {
                mBobTween = new SPTween(mCostume, 0.75f);
                mBobTween.AnimateProperty("ScaleX", 0.8f);
                mBobTween.AnimateProperty("ScaleY", 0.8f);
                mBobTween.Loop = SPLoopType.Reverse;
            }

            mScene.Juggler.AddObject(mBobTween);
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;

            mDetonated = false;
            Visible = true;
            Alpha = 1f;
            SetupActorCostume();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mCostume != null)
                mScene.Juggler.RemoveTweensWithTarget(mCostume);
            RemoveAllChildren();

            mBody.SetActive(false);
            ClearContacts();

            mInUse = false;
            CheckinReusable(this);
        }

        private void RetireActorCostume()
        {
            if (mCostume != null)
                mScene.Juggler.RemoveTweensWithTarget(mCostume);
            RemoveAllChildren();
        }

        public override void AdvanceTime(double time)
        {
            X = PX;
            Y = PY;
        }

        public void Ignite()
        {
            Detonate();
        }

        public bool Detonate()
        {
            if (mDetonated || MarkedForRemoval)
		        return false;
	        mDetonated = true;
	
	        foreach (Actor actor in mContacts.EnumerableSet)
            {
		        if (actor is BrandySlickActor)
                {
			        BrandySlickActor slick = actor as BrandySlickActor;
                    slick.Ignite();
		        }
                else if (actor is OverboardActor)
                {
			        OverboardActor person = actor as OverboardActor;
			
			        if (!person.Dying)
                        person.EnvironmentalDeath();
		        }
                else if (actor is PowderKegActor)
                {
			        PowderKegActor powderKeg = actor as PowderKegActor;
                    powderKeg.Detonate();
		        }
	        }
	
            DisplayHitEffect(PointMovie.PointMovieType.Explosion);
            DisplayHitEffect(PointMovie.PointMovieType.Splash);
            PlayDetonateSound();
            RetireActorCostume();
    
            if (TurnID == GameController.GC.ThisTurn.TurnID && mScene.GameMode == GameMode.Career)
                mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_VOODOO_GADGET_EXPIRED, Idol.GADGET_SPELL_TNT_BARRELS);
            mScene.RemoveActor(this);
	        return true;
        }

        private void DisplayHitEffect(PointMovie.PointMovieType effectType)
        {
            PointMovie.PointMovieWithType(effectType, X, Y);
        }

        private void PlayDetonateSound()
        {
            //mScene.PlaySound(kExplosionSounds[GameController.GC.NextRandom(0, 2)], 0.5f);
            //mScene.AudioPlayer.PlayRandomSoundWithKeyPrefix("Explosion", 1, 3, 0.5f);
            //mScene.PlaySound("Splash");
            mScene.PlaySound("KegDetonate");
        }

        public override void RespondToPhysicalInputs()
        {
            if (MarkedForRemoval)
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
				        ship.DeathBitmap = DeathBitmaps.POWDER_KEG;
                        ship.SinkerID = OwnerID;
                        ship.Sink();
			        }

                    if (mScene.GameMode == GameMode.Career)
			            ++mScene.AchievementManager.Kabooms; // Players may feel cheated if they see a keg explode but miss their achievement, so leave this outside conditional.
                    Detonate();
			        break;
		        }
                else if (actor is SkirmishShip)
                {
                    SkirmishShip ship = actor as SkirmishShip;
                    ship.DamageShip(5);
                    Detonate();
                }
	        }
        }

        private bool IgnoresContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            bool ignores = false;
    
            if (other is NpcShip)
            {
		        NpcShip ship = other as NpcShip;
		
		        if (fixtureOther == ship.Feeler)
			        ignores = true;
                //else if (other is MerchantShip)
                //{
                //    MerchantShip merchantShip = other as MerchantShip;
			
                //    if (fixtureOther == merchantShip.Defender)
                //        ignores = true;
                //}
	        }
            else if (other is SkirmishShip)
            {
                SkirmishShip ship = other as SkirmishShip;
                if (ship.TeamIndex == OwnerID)
                    ignores = true;
            }
            else if (other is BrandySlickActor == false && other is OverboardActor == false && other is PowderKegActor == false)
            {
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

        private static string KegTextureNameForKey(uint key)
        {
            string textureName = null;

            if (key < sKegTextureNames.Length)
                textureName = sKegTextureNames[(int)key];

            return textureName;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mCostume != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mCostume);

                            for (int i = 0; i < mCostume.NumChildren; ++i)
                                mCostume.ChildAtIndex(i).Dispose();

                            mCostume = null;
                        }
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
