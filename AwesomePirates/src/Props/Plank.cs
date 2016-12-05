using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class Plank : Prop
    {
        public enum PlankState
        {
            Inactive = 0,
            Active,
            DeadManWalking
        }

        public Plank(ShipDetails shipDetails)
            : base(-1)
        {
            if (shipDetails == null)
                throw new ArgumentNullException("Plank shipdetails is null.");

            mStateLocked = false;
		    mFlyingDutchman = false;
		    mPlankImage = null;
		    mShipDetails = shipDetails;
            mVictim = null;
		    mVictimImage = null;
            mSummonTween = null;
            mPushTween = null;
		    mVictimSprite = new SPSprite();
		    mVictimSprite.X = 32.0f;
		    mVictimSprite.Y = -72.0f;
		    mVictimSprite.Visible = false;
		    mVictimSprite.Touchable = false;
		    mFlyingDutchmanTexture = mScene.TextureByName("ghost-plank");

            mPlankKey = Keys.Space;
            mWasPlankKeyDown = false;
            mPlankButtons = new[] { Buttons.B };

            State = PlankState.Inactive;
            NextVictim();
        }
        
        #region Fields
        private bool mStateLocked;
        private PlankState mState;
        private bool mFlyingDutchman;
        private SPTexture mFlyingDutchmanTexture;
        private Prisoner mVictim;
        private ShipDetails mShipDetails;
        private SPImage mPlankImage;
        private SPImage mVictimImage;
        private SPSprite mVictimSprite;
        private Keys mPlankKey;
        private bool mWasPlankKeyDown;
        private Buttons[] mPlankButtons;
        private SPTween mSummonTween;
        private SPTween mPushTween;
        private SPEffecter mActiveEffecter;
        #endregion

        #region Properties
        public PlankState State
        {
            get { return mState; }
            set
            {
                if (mStateLocked)
		            return;
	
	            switch (value)
                {
		            case PlankState.Inactive:
			            Alpha = 0.5f;

                        if (mPlankImage != null)
                            mPlankImage.Effecter = null;
			            break;
		            case PlankState.Active:
			            Alpha = 1.0f;
#if !IOS_SCREENS
                        if (mPlankImage != null)
                            mPlankImage.Effecter = mActiveEffecter;
#endif
			            break;
		            case PlankState.DeadManWalking:
			            if (!PushVictim())
				            return;
			            break;
	            }
	            mState = value;
            }
        }
        public Prisoner Victim { get { return mVictim; } }
        public ShipDetails ShipDetails
        {
            get { return mShipDetails; }
            set
            {
                if (value != mShipDetails)
                    mShipDetails = value;
    
                mScene.Juggler.RemoveTweensWithTarget(mVictimSprite);
                mVictimSprite.Visible = false;
                mStateLocked = false;
                NextVictim();
            }
        }
        public SPEffecter ActiveEffecter
        {
            get { return mActiveEffecter; }
            set
            {
                mActiveEffecter = value;

                if (mActiveEffecter != null)
                    mActiveEffecter.Factor = 1.25f;
            }
        }
        private SPImage VictimImage { get { return mVictimImage; } set { mVictimImage = value; } }
        #endregion

        #region Methods
        public void LoadFromDictionary(Dictionary<string, object> dictionary, List<string> keys)
        {
            Dictionary<string, object> dict = dictionary["Plank"] as Dictionary<string, object>;
	        float x = 2 * Globals.ConvertToSingle(dict["x"]);
	        float y = 2 * Globals.ConvertToSingle(dict["y"]);
	        dict = dictionary["Types"] as Dictionary<string, object>;
	
	        int i = 0;
	        string key = keys[i++];
	        dict = dict[key] as Dictionary<string, object>;
	        dict = dict["Textures"] as Dictionary<string, object>;
	        string plank = dict["plankTexture"] as string;
	
	        SPTexture texture = mScene.TextureByName(plank);
	
	        if (mPlankImage == null)
            {
		        mPlankImage = new SPImage(texture);
		        mPlankImage.Touchable = false;
                AddChild(mPlankImage);
	        }
            else
            {
		        mPlankImage.Texture = texture;
	        }
	
	        X = x;
	        Y = y;
        }

        private void NextVictim()
        {
            if (!mStateLocked)
            {
		        mVictim = null;
		        mVictim = ShipDetails.PlankVictim;
                State = (Victim == null) ? PlankState.Inactive : PlankState.Active;
	        }
        }

        public void ActivateFlyingDutchman()
        {
            if (!mFlyingDutchman)
            {
		        mFlyingDutchman = true;
		        SPTexture swap = mPlankImage.Texture;
		        mPlankImage.Texture = mFlyingDutchmanTexture;
		        mFlyingDutchmanTexture = swap;
	        }
        }

        public void DeactivateFlyingDutchman()
        {
            if (mFlyingDutchman)
            {
		        mFlyingDutchman = false;
		        SPTexture swap = mPlankImage.Texture;
		        mPlankImage.Texture = mFlyingDutchmanTexture;
		        mFlyingDutchmanTexture = swap;
	        }
        }

        private bool PushVictim()
        {
            if (mVictim == null) // || !Touchable)
		        return false;
	        mStateLocked = true;

	        SPTexture texture = mScene.TextureByName(mVictim.TextureName);
	
	        if (VictimImage == null)
            {
		        VictimImage = new SPImage(texture);
		        mVictimImage.X = -mVictimImage.Width / 2;
		        mVictimImage.Y = -mVictimImage.Height / 2;
                mVictimSprite.AddChild(mVictimImage);
                AddChild(mVictimSprite);
	        }
            else
            {
		        mVictimImage.Texture = texture;
	        }
	        mVictimSprite.Alpha = 0.0f;
	        mVictimSprite.ScaleX = 1.0f;
	        mVictimSprite.ScaleY = 1.0f;
	        mVictimSprite.Visible = true;

            if (mSummonTween == null)
            {
                mSummonTween = new SPTween(mVictimSprite, 0.5f);
                mSummonTween.AnimateProperty("Alpha", 1);
                mSummonTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnVictimSummoned);
            }
            else
                mSummonTween.Reset();

            mScene.Juggler.AddObject(mSummonTween);
	        return true;
        }

        private void OnVictimSummoned(SPEvent ev)
        {
            if (mPushTween == null)
            {
                mPushTween = new SPTween(mVictimSprite, 0.75f);
                mPushTween.AnimateProperty("Alpha", 0);
                mPushTween.AnimateProperty("ScaleX", 0);
                mPushTween.AnimateProperty("ScaleY", 0);
                mPushTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnVictimPushed);
            }
            else
                mPushTween.Reset();

            mScene.Juggler.AddObject(mPushTween);
        }

        private void OnVictimPushed(SPEvent ev)
        {
            mVictim.Planked = true;
	
	        if (!GameController.GC.ThisTurn.IsGameOver)
                (mScene as PlayfieldController).PrisonerOverboard(mVictim, null);
            mStateLocked = false;
	        mVictimSprite.Visible = false;
            NextVictim();
        }

        public void Update(GamePadState state)
        {
            foreach (Buttons button in mPlankButtons)
            {
                if (state.IsButtonDown(button))
                {
                    State = PlankState.DeadManWalking;
                    break;
                }
            }
        }

        public void Update(KeyboardState state)
        {
            if (!state.IsKeyDown(mPlankKey) && mWasPlankKeyDown && State == PlankState.Active)
                State = PlankState.DeadManWalking;

            mWasPlankKeyDown = state.IsKeyDown(mPlankKey);
        }

        public void OnPrisonersChanged(NumericValueChangedEvent ev)
        {
            NextVictim();
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mVictimSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mVictimSprite);
                            mVictimSprite = null;
                        }

                        mActiveEffecter = null;
                        mVictimImage = null;
                        mFlyingDutchmanTexture = null;
                        mPlankImage = null;
                        mSummonTween = null;
                        mPushTween = null;
                        mVictim = null;
                        mShipDetails = null;
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
