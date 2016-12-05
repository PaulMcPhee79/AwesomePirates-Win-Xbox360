using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class SKPupActor : LootActor, IReusable
    {
        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            uint[] pupKeys = SKPup.AllKeys;
            sCache = new ReusableCache(pupKeys.Length);

            int cacheSize = 5;
            IReusable reusable = null;

            foreach (uint pupKey in pupKeys)
            {
                sCache.AddKey(cacheSize, pupKey);

                for (int i = 0; i < cacheSize; ++i)
                {
                    reusable = SKPupActorWithKey(pupKey, 0, 0, ResManager.P2M(28), 15);
                    reusable.Hibernate();
                    sCache.AddReusable(reusable);
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

        public static SKPupActor SKPupActorWithKey(uint pupKey, float x, float y, float radius, float duration)
        {
            uint reuseKey = pupKey;
            SKPupActor actor = CheckoutReusable(reuseKey) as SKPupActor;

            if (actor != null)
            {
                actor.mDuration = duration;
                actor.Reuse();

                Body body = actor.B2Body;
                body.SetLinearVelocity(Vector2.Zero);
                body.SetAngularVelocity(0);
                body.SetTransform(new Vector2(x, y), 0);
                body.SetActive(true);
                body.ApplyLinearImpulse(new Vector2(0.05f, 0.05f), body.GetPosition()); // Hack to ignite Box2D contacts on motionless bodies.

                actor.X = actor.PX;
                actor.Y = actor.PY;
                actor.Rotation = -actor.B2Rotation;
            }
            else
            {
                ActorDef actorDef = MiscFactory.Factory.CreateLootDefinition(x, y, radius);
                actor = new SKPupActor(actorDef, pupKey, duration);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed SKPupActor ReusableCache.");
#endif
            }

            return actor;
        }

        public SKPupActor(ActorDef def, uint pupKey, float duration)
            : base(def, (int)PFCat.PICKUPS, duration)
        {
            mAdvanceable = true;
            mPupKey = pupKey;
            mInUse = true;
            mPoolIndex = -1;
            mPup = null;
            mLootedEvent = new NumericValueChangedEvent(NumericValueChangedEvent.CUST_EVENT_TYPE_SK_PUP_LOOTED, mPupKey, mPupKey);
            mCurrentLootTimes = new double[2];
            mTotalLootTimes = new double[2];
            SetupActorCostume();
        }

        #region Fields
        protected bool mInUse;
        protected int mPoolIndex;

        private uint mPupKey;
        private PupProp mPup;
        private SPImage mPupIcon;
        private SPSprite mCostume;
        private SPSprite mFlipCostume;
        private NumericValueChangedEvent mLootedEvent;

        private Vector2 mLootOrigin;
        private Vector2 mLootDest;
        private double[] mCurrentLootTimes;
        private double[] mTotalLootTimes;
        #endregion

        #region Properties
        public uint ReuseKey { get { return mPupKey; } }
        public bool InUse { get { return mInUse; } }
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }
        #endregion

        #region Methods
        protected override void SetupActorCostume()
        {
            base.SetupActorCostume();

            if (mFlipCostume == null)
            {
                mFlipCostume = new SPSprite();
                AddChild(mFlipCostume);
            }

            if (mCostume == null)
            {
                mCostume = new SPSprite();
                mFlipCostume.AddChild(mCostume);
            }

            mCostume.Alpha = 1f;
            mCostume.X = mCostume.Y = 0f;
            mCostume.ScaleX = mCostume.ScaleY = 1f;

            // Ash
            if (mPupIcon == null)
            {
                Vector2 iconOffset = SKPup.IconOffsetForKey(mPupKey);
                mPupIcon = new SPImage(mScene.TextureByName(SKPup.TextureNameForKey(mPupKey)));
                mPupIcon.X = -mPupIcon.Width / 2 + iconOffset.X;
                mPupIcon.Y = -mPupIcon.Height / 2 + iconOffset.Y;
            }

            if (mPup == null)
            {
                mPup = new PupProp(Category, mPupIcon);
                mCostume.AddChild(mPup);
            }
            
            mPup.StartAnimation(3f, PupProp.PupPropAnimStyle.Rotate);
        }

        public void Reuse()
        {
            if (InUse)
                return;

            mTurnID = GameController.GC.ThisTurn.TurnID;
            mRemoveMe = false;
            mPreparingForNewGame = false;
            mLooted = false;

            Alpha = 1f;
            Visible = true;
            SetupActorCostume();

            mInUse = true;
        }

        public void Hibernate()
        {
            if (!InUse)
                return;

            if (mCostume != null)
                mScene.Juggler.RemoveTweensWithTarget(mCostume);

            if (mPup != null)
                mPup.StopAnimation();

            mBody.SetActive(false);
            ClearContacts();

            mInUse = false;
            CheckinReusable(this);
        }

        public override void Flip(bool enable)
        {
            mFlipCostume.ScaleX = (enable) ? -1 : 1;
        }

        public override void AdvanceTime(double time)
        {
            base.AdvanceTime(time);

            if (!mLooted)
                return;

            if (mCurrentLootTimes[0] != mTotalLootTimes[0])
            {
                mCurrentLootTimes[0] = Math.Min(mTotalLootTimes[0], mCurrentLootTimes[0] + time);
                float ratio = (float)(mCurrentLootTimes[0] / mTotalLootTimes[0]);
                float transition = SPTransitions.EaseOut(ratio);
                mCostume.X = mLootOrigin.X + (mLootDest.X - mLootOrigin.X) * transition;
                mCostume.Y = mLootOrigin.Y + (mLootDest.Y - mLootOrigin.Y) * transition;
                mCostume.ScaleX = mCostume.ScaleY = 1f + (1.5f - 1f) * transition;
            }

            if (mCurrentLootTimes[1] != mTotalLootTimes[1])
            {
                mCurrentLootTimes[1] = Math.Min(mTotalLootTimes[1], mCurrentLootTimes[1] + time);
                float ratio = (float)(mCurrentLootTimes[1] / mTotalLootTimes[1]);
                float transition = SPTransitions.EaseIn(ratio);
                mCostume.Alpha = 1f + (0f - 1f) * transition;

                if (mCurrentLootTimes[1] == mTotalLootTimes[1])
                    OnLooted(null);
            }
        }

        public override void PlayLootSound()
        {
            string soundName = SKPup.SoundNameForKey(mPupKey);

            if (soundName != null)
                mScene.PlaySound(soundName);
        }

        public override void Loot(PlayableShip ship)
        {
            if (mLooted || ship is SkirmishShip == false)
                return;

            mLooted = true;
            mScene.Juggler.RemoveTweensWithTarget(this); // Remove delayed invocation
            PlayLootSound();

            mScene.SpriteLayerManager.RemoveChild(this, mCategory);
            mCategory = (int)PFCat.DECK;
            mScene.SpriteLayerManager.AddChild(this, mCategory);
            Alpha = 1f;

            SkirmishShip skShip = ship as SkirmishShip;
            Vector2 dest = skShip.DeckLoc;
            Vector2 origin = new Vector2(X, Y);
            if (mScene.Flipped)
                origin.X = mScene.ViewWidth - origin.X; // SKShipDeck does not flip.
            Vector2 path = Vector2.Subtract(dest, origin);
            float duration = Math.Max(1f, path.Length() / 300f);
#if true
            mCurrentLootTimes[0] = 0;
            mTotalLootTimes[0] = duration;
            mLootOrigin.X = mCostume.X;
            mLootOrigin.Y = mCostume.Y;
            mLootDest.X = path.X;
            mLootDest.Y = path.Y;

            mCurrentLootTimes[1] = 0;
            mTotalLootTimes[1] = duration + 1f;
#else
            SPTween tween = new SPTween(mCostume, duration, SPTransitions.SPEaseOut);
            tween.AnimateProperty("X", path.X);
            tween.AnimateProperty("Y", path.Y);
            tween.AnimateProperty("ScaleX", 1.5f);
            tween.AnimateProperty("ScaleY", 1.5f);
            mScene.Juggler.AddObject(tween);

            tween = new SPTween(mCostume, duration + 1f, SPTransitions.SPEaseIn);
            tween.AnimateProperty("Alpha", 0);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnLooted);
            mScene.Juggler.AddObject(tween);
#endif
            mScene.SKManager.CachedIndex = skShip.SKPlayerIndex;
            //DispatchEvent(mLootedEvent);
            mScene.OnSKPupLooted(mLootedEvent);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mPup != null)
                        {
                            mPup.Dispose();
                            mPup = null;
                        }

                        if (mCostume != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mCostume);
                            mCostume = null;
                        }

                        mPupIcon = null;
                        mCostume = null;
                        mFlipCostume = null;
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
