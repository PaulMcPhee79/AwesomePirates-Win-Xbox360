using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class LootActor : Actor
    {
        public LootActor(ActorDef def, int category, float duration)
            : base(def)
        {
            mCategory = category;
            mAdvanceable = true;
            mLooted = false;
            mInfamyBonus = 0;
            X = PX;
            Y = PY;
            mDuration = duration;
        }
        
        #region Fields
        protected bool mLooted;
        protected int mInfamyBonus;
        protected double mDuration;
        protected SPTween mExpireTween;
        #endregion

        #region Properties
        public int InfamyBonus { get { return mInfamyBonus; } set { mInfamyBonus = value; } }
        #endregion

        #region Methods
        protected virtual void SetupActorCostume()
        {
            if (mExpireTween == null)
            {
                mExpireTween = new SPTween(this, 10);
                mExpireTween.AnimateProperty("Alpha", 0);
                mExpireTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnExpired));
            }
        }

        public override void RespondToPhysicalInputs()
        {
            if (IsPreparingForNewGame)
                return;
	        foreach (Actor actor in mContacts.EnumerableSet)
            {
		        if (actor is PlayableShip)
                {
                    Loot(actor as PlayableShip);
			        break;
		        }
	        }
        }

        public override void AdvanceTime(double time)
        {
            if (mDuration > 0.0)
            {
                mDuration -= time;
        
                if (mDuration <= 0.0 && !mPreparingForNewGame)
                    ExpireOverTime(10);
            }
        }

        protected void ExpireOverTime(float duration)
        {
            if (mLooted)
		        return;
            mScene.Juggler.RemoveTweensWithTarget(this);

            if (mExpireTween != null && SPMacros.SP_IS_FLOAT_EQUAL((float)mExpireTween.TotalTime, duration))
            {
                mExpireTween.Reset();
                mScene.Juggler.AddObject(mExpireTween);
            }
            else
            {
                SPTween tween = new SPTween(this, duration);
                tween.AnimateProperty("Alpha", 0);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnExpired);
                mScene.Juggler.AddObject(tween);
            }
        }

        public virtual void PlayLootSound() { }

        public virtual void Loot(PlayableShip ship)
        {
            if (mLooted)
                return;
            mLooted = true;
            mScene.Juggler.RemoveTweensWithTarget(this);
            PlayLootSound();

            mScene.SpriteLayerManager.RemoveChild(this, mCategory);
            mCategory = (int)PFCat.HUD;
            mScene.SpriteLayerManager.AddChild(this, mCategory);
            Alpha = 1f;

            SPTween tween = new SPTween(this, 1);
            tween.AnimateProperty("Alpha", 0);
            tween.AnimateProperty("ScaleX", 3);
            tween.AnimateProperty("ScaleY", 3);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnLooted);
            mScene.Juggler.AddObject(tween);
        }

        protected virtual void OnExpired(SPEvent ev)
        {
            mScene.RemoveActor(this);
        }

        protected virtual void OnLooted(SPEvent ev)
        {
            mScene.RemoveActor(this);
        }

        public override void PrepareForNewGame()
        {
            if (mPreparingForNewGame)
                return;
            mPreparingForNewGame = true;
            ExpireOverTime(mNewGamePreparationDuration);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mExpireTween != null)
                        {
                            mExpireTween.RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mExpireTween = null;
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
