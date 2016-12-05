using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class AshPickupActor : LootActor, IReusable
    {
        public const string CUST_EVENT_TYPE_ASH_PICKUP_SPAWNED = "ashPickupSpawned";
        public const string CUST_EVENT_TYPE_ASH_PICKUP_EXPIRED = "ashPickupExpired";

        private static ReusableCache sCache = null;
        private static bool sCaching = false;

        public static void SetupReusables()
        {
            if (sCache != null)
                return;

            sCaching = true;
            List<uint> ashKeys = Ash.ProcableAshKeys;
            sCache = new ReusableCache(ashKeys.Count);

            int cacheSize = 5;
            IReusable reusable = null;

            foreach (uint ashKey in ashKeys)
            {
                sCache.AddKey(cacheSize, ashKey);

                for (int i = 0; i < cacheSize; ++i)
                {
                    reusable = AshPickupActorWithKey(ashKey, 0, 0, ResManager.P2M(28), 30);
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

        public static AshPickupActor AshPickupActorWithKey(uint ashKey, float x, float y, float radius, float duration)
        {
            uint reuseKey = ashKey;
            AshPickupActor actor = CheckoutReusable(reuseKey) as AshPickupActor;

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
                actor = new AshPickupActor(actorDef, ashKey, duration);
#if DEBUG
                if (!sCaching)
                    System.Diagnostics.Debug.WriteLine("Missed AshPickupActor ReusableCache.");
#endif
            }

            return actor;
        }

        public AshPickupActor(ActorDef def, uint ashKey, float duration)
            : base(def, (int)PFCat.PICKUPS, duration)
        {
            mAdvanceable = true;
            mAshKey = ashKey;
            mInUse = true;
            mPoolIndex = -1;
            mLootedEvent = new NumericValueChangedEvent(NumericValueChangedEvent.CUST_EVENT_TYPE_ASH_PICKUP_LOOTED, mAshKey, mAshKey);
            mCurrentLootTimes = new double[2];
            mTotalLootTimes = new double[2];
            SetupActorCostume();
        }
        
        #region Fields
        protected bool mInUse;
        protected int mPoolIndex;

        private uint mAshKey;
        private SPTextField mHint;
        private SPMovieClip mAshClip;
        private SPSprite mAshSprite;
        private PupProp mPup;
        private SPSprite mCostume;
        private SPSprite mFlipCostume;
        private NumericValueChangedEvent mLootedEvent;

        private Vector2 mLootOrigin;
        private Vector2 mLootDest;
        private double[] mCurrentLootTimes;
        private double[] mTotalLootTimes;
        #endregion

        #region Properties
        public uint ReuseKey { get { return mAshKey; } }
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
            if (mAshSprite == null)
                mAshSprite = new SPSprite();

            if (mAshClip == null)
            {
                string texturePrefix = Ash.TexturePrefixForKey(mAshKey);
                mAshClip = new SPMovieClip(mScene.TexturesStartingWith(texturePrefix), Cannonball.Fps);
                mAshClip.X = -mAshClip.Width / 2;
                mAshClip.Y = -mAshClip.Height / 2;
                mAshSprite.AddChild(mAshClip);
            }

            mScene.Juggler.AddObject(mAshClip);
            mAshSprite.ScaleX = mAshSprite.ScaleY = 25f / mAshClip.Width;

            if (mPup == null)
            {
                mPup = new PupProp(Category, mAshSprite);
                mCostume.AddChild(mPup);
            }

            mPup.StartAnimation(3f, PupProp.PupPropAnimStyle.RotateAndScale);

            // Hint
            if (!GameSettings.GS.SettingForKey(Ash.GameSettingForKey(mAshKey)))
            {
                mHint = new SPTextField(200, 48, Ash.HintForKey(mAshKey), mScene.FontKey, 32);
                mHint.X = (mPup.X - mPup.Width / 2) + (mPup.Width - mHint.Width) / 2;
                mHint.Y = mPup.Y + mPup.Height / 4;
                mHint.HAlign = SPTextField.SPHAlign.Center;
                mHint.VAlign = SPTextField.SPVAlign.Top;
                mHint.Color = new Color(0xfc, 0xc3, 0x0e);
                mCostume.AddChild(mHint);
            }
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

            if (mHint != null)
            {
                if (mLooted)
                {
                    string settingKey = Ash.GameSettingForKey(mAshKey);

                    if (settingKey != null && !GameSettings.GS.SettingForKey(settingKey))
                        GameSettings.GS.SetSettingForKey(settingKey, true);
                }

                mHint.RemoveFromParent();
                mHint.Dispose();
                mHint = null;
            }

            if (mCostume != null)
                mScene.Juggler.RemoveTweensWithTarget(mCostume);

            if (mAshClip != null)
                mScene.Juggler.RemoveObject(mAshClip);

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

            if (mScene.GameMode == GameMode.Career)
            {
                if (mCurrentLootTimes[0] != mTotalLootTimes[0])
                {
                    mCurrentLootTimes[0] = Math.Min(mTotalLootTimes[0], mCurrentLootTimes[0] + time);
                    float ratio = (float)(mCurrentLootTimes[0] / mTotalLootTimes[0]);
                    float transition = SPTransitions.Linear(ratio);
                    mCostume.Alpha = 1f + (0f - 1f) * transition;
                    mCostume.ScaleX = mCostume.ScaleY = 1f + (3f - 1f) * transition;

                    if (mCurrentLootTimes[0] == mTotalLootTimes[0])
                        OnLooted(null);
                }
            }
            else
            {
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
        }

        public override void PlayLootSound()
        {
            mScene.PlaySound(Ash.SoundNameForKey(mAshKey));
        }

        public override void Loot(PlayableShip ship)
        {
            if (mLooted)
		        return;
	        mLooted = true;
            mScene.Juggler.RemoveTweensWithTarget(this); // Remove expiration tween
            PlayLootSound();

            mScene.SpriteLayerManager.RemoveChild(this, mCategory);
            mCategory = (int)PFCat.DECK;
            mScene.SpriteLayerManager.AddChild(this, mCategory);
            Alpha = 1f;

#if true
            if (ship is SkirmishShip)
            {
                SkirmishShip skShip = ship as SkirmishShip;
                Vector2 dest = skShip.DeckLoc;
                Vector2 origin = new Vector2(X, Y);
                if (mScene.Flipped)
                    origin.X = mScene.ViewWidth - origin.X; // SKShipDeck does not flip.
                Vector2 path = Vector2.Subtract(dest, origin);
                float duration = Math.Max(1f, path.Length() / 300f);

                mCurrentLootTimes[0] = 0;
                mTotalLootTimes[0] = duration;
                mLootOrigin.X = mCostume.X;
                mLootOrigin.Y = mCostume.Y;
                mLootDest.X = path.X;
                mLootDest.Y = path.Y;

                mCurrentLootTimes[1] = 0;
                mTotalLootTimes[1] = duration + 1f;

                mScene.SKManager.CachedIndex = skShip.SKPlayerIndex;
            }
            else
            {
                mCurrentLootTimes[0] = 0;
                mTotalLootTimes[0] = 1;
            }
#else
            if (ship is SkirmishShip)
            {
                SkirmishShip skShip = ship as SkirmishShip;
                Vector2 dest = skShip.DeckLoc;
                Vector2 origin = new Vector2(X, Y);
                if (mScene.Flipped)
                    origin.X = mScene.ViewWidth - origin.X; // SKShipDeck does not flip.
                Vector2 path = Vector2.Subtract(dest, origin);
                float duration = Math.Max(1f, path.Length() / 300f);

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

                mScene.SKManager.CachedIndex = skShip.SKPlayerIndex;
            }
            else
            {
                SPTween tween = new SPTween(mCostume, 1);
                tween.AnimateProperty("Alpha", 0);
                tween.AnimateProperty("ScaleX", 3);
                tween.AnimateProperty("ScaleY", 3);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnLooted);
                mScene.Juggler.AddObject(tween);
            }
#endif
            //DispatchEvent(mLootedEvent);
            mScene.OnAshPickupLooted(mLootedEvent);
        }

        protected override void OnExpired(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_ASH_PICKUP_EXPIRED));
            base.OnExpired(ev);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mHint != null && mLooted)
                        {
                            string settingKey = Ash.GameSettingForKey(mAshKey);

                            if (settingKey != null && !GameSettings.GS.SettingForKey(settingKey))
                                GameSettings.GS.SetSettingForKey(settingKey, true);
                        }

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

                        if (mAshClip != null)
                        {
                            mScene.Juggler.RemoveObject(mAshClip);
                            mAshClip.Dispose();
                            mAshClip = null;
                        }

                        mAshSprite = null;
                        mHint = null;
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
