using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;
using SPImage = SparrowXNA.SPQuad;

namespace AwesomePirates
{
    class PlayerCannon : Prop
    {
        private const int kSmokeBufferSize = 6;
        private const double kDefaultReloadDelay = 0.5;

        public PlayerCannon(int direction, bool visual = true)
            : base(-1)
        {
            if (direction == 0)
                direction = 1;
            mVisual = visual;
            mDirection = direction / Math.Abs(direction); // Clamp to +/-1
            mElevation = -0.125f * SPMacros.PI;
            mReloadInterval = kDefaultReloadDelay;
            mActivated = true;
            mReloading = false;
            mOverheated = false;
            mWasRequestedToFire = false;
            mFireVolume = 0.7f;
            mFireKey = (direction == 1) ? Keys.D : Keys.A;
            mWasFireKeyDown = false;

            //if (direction == 1)
            //    mFireButtons = new[] { Buttons.RightShoulder, Buttons.RightTrigger, Buttons.B };
            //else
                mFireButtons = new[] { Buttons.A }; // { Buttons.LeftShoulder, Buttons.LeftTrigger, Buttons.A };
            mPrevFireButtonState = ButtonState.Released;
            mFiredEvent = new PlayerCannonFiredEvent(this);

            mBarrelDutchmanTexture = null;
            mBracketDutchmanTexture = null;
            mWheelDutchmanTexture = null;
            mFlashDutchmanTexture = null;

            mOrigin = new Vector2();
            mRecoilTweens = null;
            SetupNormalTextures();
        }

        #region Fields
        private bool mVisual;
        private bool mActivated;
        private bool mOverheated;
        private bool mWasRequestedToFire;
        private bool mReloading;
        private double mReloadInterval;
        private double mReloadTimer;
        private float mElevation;
        private int mDirection;
        private float mFireVolume;
        private Keys mFireKey;
        private bool mWasFireKeyDown;
        private Buttons[] mFireButtons;
        private ButtonState mPrevFireButtonState;
        private PlayerCannonFiredEvent mFiredEvent;

        // Normal textures
        private SPTexture mBarrelTexture;
        private SPTexture mOverheatedBarrelTexture;
        private SPTexture mBracketTexture;
        private SPTexture mWheelTexture;
        private SPTexture mFlashTexture;

        // Flying Dutchman textures
        private SPTexture mBarrelDutchmanTexture;
        private SPTexture mBracketDutchmanTexture;
        private SPTexture mWheelDutchmanTexture;
        private SPTexture mFlashDutchmanTexture;

        private Vector2 mOrigin;
        private SPSprite mBarrel;
        private SPImage mBarrelImage;
        private SPSprite mBracket;
        private SPImage mBracketImage;
        private SPImage mFrontWheelImage;
        private SPSprite mFrontWheel;
        private SPImage mRearWheelImage;
        private SPSprite mRearWheel;
        private SPSprite mMuzzleFlashFrame;
        private SPMovieClip mMuzzleFlash;
        private SPSprite mRecoilContainer;

        private RingBuffer mSmokeClouds;
        private List<SPTween> mRecoilTweens;
        
        #endregion

        #region Properties
        public bool Activated { get { return mActivated; } set { mActivated = value; } }
        public bool Reloading { get { return mReloading; } }
        public bool Overheated { get { return mOverheated; } }
        public bool WasRequestedToFire { get { return mWasRequestedToFire; } set { mWasRequestedToFire = value; } }
        public double ReloadTimer { get { return mReloadTimer; } }
        public double ReloadInterval { get { return mReloadInterval; } set { mReloadInterval = value; } }
        public float Elevation
        {
            get { return mElevation; }
            set
            {
                mElevation = Math.Min(0, Math.Max(-0.25f * SPMacros.PI, value)); // Clamp from -0.25Pi -> 0
                if (mBarrel != null) mBarrel.Rotation = mElevation;
            }
        } 
        public int Direction { get { return mDirection; } }
        #endregion

        #region Methods
        private void SetupNormalTextures()
        {
            if (!mVisual)
                return;

            GameController gc = GameController.GC;
	        CannonDetails cannonDetails = gc.PlayerDetails.CannonDetails;
	
	        mBarrelTexture = mScene.TextureByName(cannonDetails.TextureNameBarrel);
            mOverheatedBarrelTexture = mScene.TextureByName("overheated-barrel");
	        mBracketTexture = mScene.TextureByName(cannonDetails.TextureNameBase);
	        mWheelTexture = mScene.TextureByName(cannonDetails.TextureNameWheel);
	        mFlashTexture = mScene.TextureByName(cannonDetails.TextureNameFlash);
        }

        public void SetupDutchmanTextures()
        {
            if (!mVisual)
                return;

            CannonDetails cannonDetails = CannonFactory.Factory.CreateSpecialCannonDetailsForType("FlyingDutchman");
	
	        mBarrelDutchmanTexture = mScene.TextureByName(cannonDetails.TextureNameBarrel);
	        mBracketDutchmanTexture = mScene.TextureByName(cannonDetails.TextureNameBase);
	        mWheelDutchmanTexture = mScene.TextureByName(cannonDetails.TextureNameWheel);
            mFlashDutchmanTexture = mScene.TextureByName(cannonDetails.TextureNameFlash);
        }

        public void LoadFromDictionary(Dictionary<string, object> dictionary, List<string> keys)
        {
            if (!mVisual)
                return;

            int i = 0;
	        string key = keys[i++];
	        Dictionary<string, object> dict = dictionary[key] as Dictionary<string, object>;
	        mOrigin.X = 2 * Globals.ConvertToSingle(dict["x"]);
	        mOrigin.Y = 2 * Globals.ConvertToSingle(dict["y"]);
	        ScaleX = Globals.ConvertToSingle(dict["scaleX"]);
            DecorateWithCannonDetails(GameController.GC.PlayerDetails.CannonDetails, new Dictionary<string,SPTexture>()
            {
                { "Barrel", mBarrelTexture },
                { "Bracket", mBracketTexture },
                { "Wheel", mWheelTexture },
                { "Flash", mFlashTexture }
            });

	        Elevation = SPMacros.SP_D2R(-22.5f);
        }

        private void DecorateWithCannonDetails(CannonDetails cannonDetails, Dictionary<string, SPTexture> texDict)
        {
            if (!mVisual)
                return;

            Dictionary<string, object> dictIter = null;
	        Dictionary<string, object> deckSettings = cannonDetails.DeckSettings;

	        Vector2 offset = PointFromDictionary(deckSettings["Offset"] as Dictionary<string, object>);
	        Vector2 pivot = PointFromDictionary(deckSettings["Pivot"] as Dictionary<string, object>);
	        Vector2 barrel = PointFromDictionary(deckSettings["Barrel"] as Dictionary<string, object>);
	        Vector2 flash = PointFromDictionary(deckSettings["Flash"] as Dictionary<string, object>);
	        Vector2 smoke = PointFromDictionary(deckSettings["Smoke"] as Dictionary<string, object>);
	        
	        dictIter = deckSettings["Flash"] as Dictionary<string, object>;
	        float flashScale = Globals.ConvertToSingle(dictIter["scale"]);
	
	        dictIter = deckSettings["Smoke"] as Dictionary<string, object>;
	        float smokeScale = Globals.ConvertToSingle(dictIter["scale"]);
	
	        dictIter = deckSettings["Axles"] as Dictionary<string, object>;
	        Vector2 axleFront = PointFromDictionary(dictIter["Front"] as Dictionary<string, object>);
	        Vector2 axleRear = PointFromDictionary(dictIter["Rear"] as Dictionary<string, object>);
	
	        // Build the cannon
	        SPTexture texture = null;
	
	        if (mRecoilContainer == null)
            {
		        mRecoilContainer = new SPSprite();
                AddChild(mRecoilContainer);
	        }
	
	        // Barrel
            if (mBarrelImage != null) mBarrelImage.RemoveFromParent();

	        mBarrelImage = new SPImage(texDict["Barrel"]);
	        mBarrelImage.X = barrel.X;
	        mBarrelImage.Y = barrel.Y;
	
	        if (mBarrel == null)
            {
		        mBarrel = new SPSprite();
		        mBarrel.Touchable = false;
                mRecoilContainer.AddChild(mBarrel);
	        }
	
	        float oldRotation = mBarrel.Rotation;
	        mBarrel.Rotation = 0;
	        mBarrel.X = pivot.X;
	        mBarrel.Y = pivot.Y;
            mBarrel.AddChild(mBarrelImage);
	        mBarrel.Rotation = oldRotation;
	
	        // Bracket
            if (mBracketImage != null) mBracketImage.RemoveFromParent();
	        mBracketImage = new SPImage(texDict["Bracket"]);

	        if (mBracket == null)
            {
		        mBracket = new SPSprite();
		        mBracket.Touchable = false;
                mRecoilContainer.AddChild(mBracket);
	        }
	
            mBracket.AddChild(mBracketImage);
	
	        // Front Wheel
	        texture = texDict["Wheel"];
	
            if (mFrontWheelImage != null) mFrontWheelImage.RemoveFromParent();
	        mFrontWheelImage = new SPImage(texture);
	        mFrontWheelImage.X = -mFrontWheelImage.Width / 2;
	        mFrontWheelImage.Y = -mFrontWheelImage.Height / 2;
	
	        if (mFrontWheel == null)
            {
		        mFrontWheel = new SPSprite();
		        mFrontWheel.Touchable = false;
                mRecoilContainer.AddChild(mFrontWheel);
	        }
	
	        mFrontWheel.X = axleFront.X;
	        mFrontWheel.Y = axleFront.Y;
            mFrontWheel.AddChild(mFrontWheelImage);
	
	        // Rear Wheel
            if (mRearWheelImage != null) mRearWheelImage.RemoveFromParent();
	        mRearWheelImage = new SPImage(texture);
	        mRearWheelImage.X = -mRearWheelImage.Width / 2;
	        mRearWheelImage.Y = -mRearWheelImage.Height / 2;
	
	        if (mRearWheel == null)
            {
		        mRearWheel = new SPSprite();
		        mRearWheel.Touchable = false;
                mRecoilContainer.AddChild(mRearWheel);
	        }
	
	        mRearWheel.X = axleRear.X;
	        mRearWheel.Y = axleRear.Y;
            mRearWheel.AddChild(mRearWheelImage);
	
	        // Flash
	        if (mMuzzleFlash == null)
            {
		        mMuzzleFlash = new SPMovieClip(mScene.TextureByName(cannonDetails.TextureNameFlash), 10);
                mMuzzleFlash.AddEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)OnFlashClipCompleted);
	        }
            else
            {
                mMuzzleFlash.SetFrameAtIndex(texDict["Flash"], 0);
	        }
	
	        mMuzzleFlash.Y = -mMuzzleFlash.Height / 2;
	
	        if (mMuzzleFlashFrame == null)
            {
		        mMuzzleFlashFrame = new SPSprite();
		        mMuzzleFlashFrame.Touchable = false;
                mMuzzleFlashFrame.AddChild(mMuzzleFlash);
                mBarrel.AddChildAtIndex(mMuzzleFlashFrame, 0);
	        }
	
	        mMuzzleFlashFrame.X = flash.X;
	        mMuzzleFlashFrame.Y = flash.Y;
	        mMuzzleFlashFrame.ScaleX = mMuzzleFlashFrame.ScaleY = flashScale;
	        mMuzzleFlashFrame.Visible = false;
	
	        // Smoke
	        if (mSmokeClouds == null)
            {
		        mSmokeClouds = new RingBuffer(kSmokeBufferSize);
	
		        for (int i = 0; i < kSmokeBufferSize; ++i)
                {
			        CannonSmoke cannonSmoke = new CannonSmoke(smoke.X, smoke.Y);
			        cannonSmoke.ScaleX = cannonSmoke.ScaleY = smokeScale;
                    mSmokeClouds.AddItem(cannonSmoke);
                    mBarrel.AddChild(cannonSmoke);
		        }
	        }
	
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.LowerLeft);
	        X = ResManager.RESX(mOrigin.X + ((ScaleX > 0) ? offset.X : -offset.X));
	        Y = ResManager.RESY(mOrigin.Y + offset.Y);
            ResManager.RESM.PopOffset();
        }

        private Vector2 PointFromDictionary(Dictionary<string, object> dict)
        {
            Vector2 point = new Vector2();
            point.X = 2 * Globals.ConvertToSingle(dict["x"]);
            point.Y = 2 * Globals.ConvertToSingle(dict["y"]);
            return point;
        }

        public void Fire(bool silent = false)
        {
            if (mReloading || !mActivated)
                return;
            if (mOverheated)
            {
                mScene.PlaySound("CannonOverheat");
                return;
            }

            if (!silent)
                mScene.PlaySound("PlayerCannon");

            if (mVisual)
            {
                mMuzzleFlash.CurrentFrame = 0;
                mMuzzleFlashFrame.Visible = true;
                mMuzzleFlash.Play();
                mScene.Juggler.AddObject(mMuzzleFlash);
                Recoil();
            }

            Reload();
        }

#if IOS_SCREENS
        public void FireIOS()
        {
            mMuzzleFlash.CurrentFrame = 0;
            mMuzzleFlashFrame.Visible = true;
            mMuzzleFlash.Play();
            mScene.Juggler.AddObject(mMuzzleFlash);
            Recoil();
        }
#endif

        public void Overheat(bool enable)
        {
            if (enable == mOverheated)
                return;

            if (mVisual)
            {
                SPTexture swapTexture = mBarrelImage.Texture;
                mBarrelImage.Texture = mOverheatedBarrelTexture;
                mOverheatedBarrelTexture = swapTexture;
            }

            mOverheated = enable;
        }

        public void Reload()
        {
            mReloading = true;
            mReloadTimer = mReloadInterval;
        }

        private void Reloaded()
        {
            mReloading = false;
        }

        private void Recoil()
        {
            if (!mVisual)
                return;

            if (mRecoilTweens == null)
            {
                float distance = 40;
        
                SPTween tweenJolt = new SPTween(mRecoilContainer, 0.25f, SPTransitions.SPEaseOut);
                tweenJolt.AnimateProperty("X", -distance);
        
                float frontTargetValue = mFrontWheel.Rotation + SPMacros.TWO_PI * (-distance / (SPMacros.PI * mFrontWheel.Width));
                SPTween tweenFrontJolt = RollWheel(mFrontWheel, frontTargetValue, (float)tweenJolt.TotalTime, 0, SPTransitions.SPEaseOut);
        
                float rearTargetValue = mRearWheel.Rotation + SPMacros.TWO_PI * (-distance / (SPMacros.PI * mRearWheel.Width));
                SPTween tweenRearJolt = RollWheel(mRearWheel, rearTargetValue, (float)tweenJolt.TotalTime, 0, SPTransitions.SPEaseOut);
        
                SPTween tweenReturn = new SPTween(mRecoilContainer, mReloadInterval - (tweenJolt.TotalTime + 0.1f));
                tweenReturn.AnimateProperty("X", mRecoilContainer.X);
                tweenReturn.Delay = tweenJolt.TotalTime;
        
                frontTargetValue = frontTargetValue + SPMacros.TWO_PI * (distance / (SPMacros.PI * mFrontWheel.Width));
                SPTween tweenFrontReturn = RollWheel(mFrontWheel, frontTargetValue, (float)tweenReturn.TotalTime, (float)tweenReturn.Delay, SPTransitions.SPLinear);
        
                rearTargetValue = rearTargetValue + SPMacros.TWO_PI * (distance / (SPMacros.PI * mRearWheel.Width));
                SPTween tweenRearReturn = RollWheel(mRearWheel, rearTargetValue, (float)tweenReturn.TotalTime, (float)tweenReturn.Delay, SPTransitions.SPLinear);
        
                mRecoilTweens = new List<SPTween>()
                    {
                        tweenRearReturn,
                        tweenFrontReturn,
                        tweenReturn,
                        tweenRearJolt,
                        tweenFrontJolt,
                        tweenJolt
                    };
            }
    
            foreach (SPTween tween in mRecoilTweens)
            {
                tween.Reset();
                mScene.Juggler.AddObject(tween);
            }
        }

        private SPTween RollWheel(SPSprite wheel, float targetValue, float duration, float delay, string transition)
        {
            SPTween tween = new SPTween(wheel, duration, transition);
            tween.AnimateProperty("Rotation", targetValue);
            tween.Delay = delay;
            return tween;
        }

        public void Update(GamePadState state)
        {
            if (Reloading)
                return;

            foreach (Buttons button in mFireButtons)
            {
                if (state.IsButtonDown(button))
                {
                    //DispatchEvent(mFiredEvent);
                    mWasRequestedToFire = true;
                    break;
                }
            }
        }

        public void Update(KeyboardState state)
        {
#if WINDOWS || DEBUG
            if (!state.IsKeyDown(mFireKey) && mWasFireKeyDown)
                Fire();

            mWasFireKeyDown = state.IsKeyDown(mFireKey);
#endif
        }

        public void Update(MouseState state)
        {
            /*
            if (mFireButton == Buttons.RightTrigger)
            {
                if (state.RightButton == ButtonState.Released && mPrevFireButtonState == ButtonState.Pressed)
                    Fire();
                mPrevFireButtonState = state.RightButton;
            }
            else if (mFireButton == Buttons.LeftTrigger)
            {
                if (state.LeftButton == ButtonState.Released && mPrevFireButtonState == ButtonState.Pressed)
                    Fire();
                mPrevFireButtonState = state.LeftButton;
            }
             * */
        }

        public override void AdvanceTime(double time)
        {
            if (mReloadTimer > 0.0)
            {
                mReloadTimer -= time;

                if (mReloadTimer <= 0.0)
                    Reloaded();
            }
        }

        public void ActivateFlyingDutchman()
        {
            if (!mVisual)
                return;

            DecorateWithCannonDetails(CannonFactory.Factory.CreateSpecialCannonDetailsForType("FlyingDutchman"), new Dictionary<string, SPTexture>()
            {
                { "Barrel", mBarrelDutchmanTexture },
                { "Bracket", mBracketDutchmanTexture },
                { "Wheel", mWheelDutchmanTexture },
                { "Flash", mFlashDutchmanTexture }
            });
        }

        public void DeactivateFlyingDutchman()
        {
            if (!mVisual)
                return;

            DecorateWithCannonDetails(GameController.GC.PlayerDetails.CannonDetails, new Dictionary<string, SPTexture>()
            {
                { "Barrel", mBarrelTexture },
                { "Bracket", mBracketTexture },
                { "Wheel", mWheelTexture },
                { "Flash", mFlashTexture }
            });
        }

        private void OnFlashClipCompleted(SPEvent ev)
        {
            mScene.Juggler.RemoveObject(mMuzzleFlash);
            mMuzzleFlashFrame.Visible = false;

            CannonSmoke cannonSmoke = mSmokeClouds.NextItem as CannonSmoke;
            cannonSmoke.StartWithAngle(mBarrel.Rotation);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mMuzzleFlash != null)
                        {
                            mMuzzleFlash.RemoveEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)OnFlashClipCompleted);
                            mScene.Juggler.RemoveObject(mMuzzleFlash);
                            mMuzzleFlash = null;
                        }

                        if (mRecoilContainer != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mRecoilContainer);
                            mRecoilContainer = null;
                        }

                        mFiredEvent = null;
                        mMuzzleFlashFrame = null;
                        mBarrel = null;
                        mBarrelImage = null;
                        mBracket = null;
                        mBracketImage = null;
                        mFrontWheel = null;
                        mFrontWheelImage = null;
                        mRearWheel = null;
                        mRearWheelImage = null;
                        mBarrelTexture = null;
                        mOverheatedBarrelTexture = null;
                        mBracketTexture = null;
                        mWheelTexture = null;
                        mFlashTexture = null;
                        mMuzzleFlash = null;
                        mBarrelDutchmanTexture = null;
                        mBracketDutchmanTexture = null;
                        mWheelDutchmanTexture = null;
                        mFlashDutchmanTexture = null;
                        mRecoilTweens = null;
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
