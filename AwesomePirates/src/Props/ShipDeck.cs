using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;
namespace AwesomePirates
{
    class ShipDeck : Prop, IInteractable
    {
        public const string CUST_EVENT_TYPE_DECK_VOODOO_IDOL_PRESSED = "deckVoodooIdolPressed";
        public const string CUST_EVENT_TYPE_DECK_TWITTER_BUTTON_PRESSED = "twitterButtonPressed";

        private const float kControlsAlpha = 0.65f;

        public ShipDeck(ShipDetails shipDetails)
            : base(PFCat.DECK)
        {
            mAdvanceable = true;
		    mFlyingDutchman = false;
            mTwitterEnabled = false;
            mCombatControlsEnabled = false;

            mIdolButtons = new Buttons[] { Buttons.X, Buttons.LeftStick };
		
            mFlyingDutchmanVoodooTexture = mScene.TextureByName("ghost-deck-idol");
            mFlyingDutchmanRailingTexture = mScene.TextureByName("ghost-railing");
		    mFlyingDutchmanRailingTexture.Repeat = true;
        
            mRailing = null;
            mSpeedboatRailing = null;
		    mHelm = new Helm(0.05f * SPMacros.PI);
		    mPlank = new AwesomePirates.Plank(shipDetails);
            GameController.GC.SetupHighlightEffecter(mPlank);
            mPlank.ActiveEffecter = mPlank.Effecter;
            Plank.Effecter = null;
		    mRightCannon = new PlayerCannon(1);
            mLeftCannon = new PlayerCannon(-1);
#if IOS_SCREENS
            mCannonContainer = new SPSprite();
#endif
		    mComboDisplay = new AwesomePirates.ComboDisplay();
            mVoodooPlankContainer = new SPSprite();
            mVoodooSprite = new SPSprite();
            mTwitterSprite = new SPSprite();
            mControls = new SPSprite[4];
            mControlsSprite = new SPSprite();
            mTwitterButton = null;
            mPotion = null;
            mPotionEffecter = null;
            mPotionTemp = null;
            mTimeDial = null;
            mSpeedDial = null;
            mLapDial = null;
		    //Touchable = true;
        }
        
        #region Fields
        private bool mFlyingDutchman;
        private bool mTwitterEnabled;
        private bool mCombatControlsEnabled;

        private Buttons[] mIdolButtons;

        private SPQuad mRailing;
        private SPQuad mSpeedboatRailing;

        private SPTexture mFlyingDutchmanVoodooTexture;
        private SPTexture mFlyingDutchmanRailingTexture;

        private Helm mHelm;
        private Plank mPlank;
        private SPSprite mVoodooPlankContainer;
        private PlayerCannon mRightCannon;
        private PlayerCannon mLeftCannon;
#if IOS_SCREENS
        private SPSprite mCannonContainer;
#endif
        private ComboDisplay mComboDisplay;

        private SPSprite mPotion;
        private SPEffecter mPotionEffecter;
        private List<SPSprite> mPotionTemp;

        private SPButton mVoodooIdol;
        private SPButton mTwitterButton;

        private SPSprite mVoodooSprite;
        private SPSprite mTwitterSprite;

        private SPSprite[] mControls;
        private SPSprite mControlsSprite;

        private DashDial mTimeDial;
        private DashDial mSpeedDial;
        private DashDial mLapDial;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_DECK; } }
        public bool RaceEnabled { get { return (mTimeDial != null || mSpeedDial != null || mLapDial != null); } }
        public bool CombatControlsEnabled { get { return mCombatControlsEnabled; } set { mCombatControlsEnabled = value; } }
        public SPButton VoodooIdol { get { return mVoodooIdol; } }
        public Helm Helm { get { return mHelm; } set { mHelm = value; } }
        public Plank Plank { get { return mPlank; } set { mPlank = value; } }
        public PlayerCannon RightCannon { get { return mRightCannon; } set { mRightCannon = value; } }
        public PlayerCannon LeftCannon { get { return mLeftCannon; } set { mLeftCannon = value; } }
        public ComboDisplay ComboDisplay { get { return mComboDisplay; } set { mComboDisplay = value; } }
        #endregion

        #region Methods
        public void LoadFromDictionary(Dictionary<string, object> dictionary, List<string> keys)
        {
            if (mRailing != null)
                return;
    
	        GameController gc = GameController.GC;
	        int i = 0;
	
	        Dictionary<string, object> dict = dictionary["Types"] as Dictionary<string, object>;
	        string key = keys[i++] as string;
	        dict = dict[key] as Dictionary<string, object>;
	        dict = dict["Textures"] as Dictionary<string, object>;
	
            // Railings
	        string railingTextureName = dict["railingTexture"] as string;
	        SPTexture railingTexture = mScene.TextureByName(railingTextureName);
	        railingTexture.Repeat = true;
	
	        mRailing = new SPQuad(railingTexture);
	        mRailing.X = 0;
	        mRailing.Y = mScene.ViewHeight-70;
	        mRailing.Width = mScene.ViewWidth;
	        mRailing.Touchable = false;
	
	        float xRepeat = mRailing.Width / railingTexture.Width;
            mRailing.SetTexCoord(new Vector2(xRepeat, 0), 1);
            mRailing.SetTexCoord(new Vector2(0, 1), 2);
            mRailing.SetTexCoord(new Vector2(xRepeat, 1), 3);
    
            SPTexture speedboatRailingTexture = mScene.TextureByName("8-Speedboat-railing");
            speedboatRailingTexture.Repeat = true;
            mSpeedboatRailing = new SPQuad(speedboatRailingTexture);
            mSpeedboatRailing.X = 0;
            mSpeedboatRailing.Y = mScene.ViewHeight-70;
            mSpeedboatRailing.Width = mScene.ViewWidth;
            mSpeedboatRailing.Touchable = true;
    
            xRepeat = mSpeedboatRailing.Width / speedboatRailingTexture.Width;
            mSpeedboatRailing.SetTexCoord(new Vector2(xRepeat, 0), 1);
            mSpeedboatRailing.SetTexCoord(new Vector2(0, 1), 2);
            mSpeedboatRailing.SetTexCoord(new Vector2(xRepeat, 1), 3);
	
            AddChild(mRailing);

#if IOS_SCREENS
            mCannonContainer.AddChild(mLeftCannon);
            AddChild(mCannonContainer);
#else
            AddChild(mLeftCannon);
            AddChild(mRightCannon);
#endif

            //mVoodooPlankContainer.AddChild(mPlank);
            //AddChild(mVoodooPlankContainer);
            AddChild(mHelm);
            AddChild(mComboDisplay);

            // Helm
            ++i;
            mHelm.Scale = new Vector2(0.8f, 0.8f);
            mHelm.X = mScene.ViewWidth - mHelm.Width / 2;
            mHelm.Y = mScene.ViewHeight - mHelm.Height / 2;
            AddChild(mHelm);
	
            // Voodoo Sprite
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.LowerCenter);
            mPlank.LoadFromDictionary(dictionary, new List<string>() { keys[i++] });
            mPlank.X = ResManager.RESX(mPlank.X); mPlank.Y = ResManager.RESY(mPlank.Y);
            mVoodooPlankContainer.AddChild(mPlank);

            mVoodooIdol = new SPButton(mScene.TextureByName("deck-idol"));
            mVoodooIdol.X = -mVoodooIdol.Width / 2;
            mVoodooIdol.Touchable = true;
            mVoodooIdol.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnDeckVoodooIdolPressed);

            mVoodooSprite.X = ResManager.RESX(468); mVoodooSprite.Y = ResManager.RESY(544);
            mVoodooSprite.AddChild(mVoodooIdol);
            mVoodooPlankContainer.AddChild(mVoodooSprite);
            ResManager.RESM.PopOffset();

            // Cannons
            mRightCannon.LoadFromDictionary(dictionary, new List<string>() { keys[i++] });
            mLeftCannon.LoadFromDictionary(dictionary, new List<string>() { keys[i++] });

#if IOS_SCREENS
            mCannonContainer.ScaleX = mCannonContainer.ScaleY = 1.25f;
            SPRectangle cannonBounds = mLeftCannon.BoundsInSpace(this);
            mCannonContainer.Y = mScene.ViewHeight - (cannonBounds.Y + cannonBounds.Height);

            mLeftCannon.X += 2 * (112 - 76);
            mLeftCannon.Elevation = SPMacros.SP_D2R(-11.25f);
#else
            mRightCannon.Elevation = -0.125f * SPMacros.PI;
            mLeftCannon.Elevation = -0.125f * SPMacros.PI;
#endif



            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.LowerLeft);
            mComboDisplay.LoadFromDictionary(dictionary, new List<string>() { keys[i++] });
            mComboDisplay.X = ResManager.RESX(mComboDisplay.X); mComboDisplay.Y = ResManager.RESY(mComboDisplay.Y);
            ResManager.RESM.PopOffset();
	
	        if ((gc.PlayerDetails.Abilities & Idol.VOODOO_SPELL_FLYING_DUTCHMAN) == Idol.VOODOO_SPELL_FLYING_DUTCHMAN)
            {
                mRightCannon.SetupDutchmanTextures();
                mLeftCannon.SetupDutchmanTextures();
	        }

            // Voodoo Sprite
            float helmCannonDist = (mHelm.X - mHelm.Width / 2) - (mRightCannon.X + 200);
            mPlank.X = -mPlank.Width / 2 + helmCannonDist / 4;
            mPlank.Y = 4 + (mVoodooIdol.Height - mPlank.Height);

            mVoodooSprite.X = -helmCannonDist / 4;
            mVoodooSprite.Y = 0;

            mVoodooPlankContainer.X = (mHelm.X - mHelm.Width / 2) - helmCannonDist / 2; mVoodooPlankContainer.Y = mScene.ViewHeight - mVoodooSprite.Height;
            AddChild(mVoodooPlankContainer);
            ResManager.RESM.PopOffset();

            // Controls Sprite
            mControlsSprite.Visible = false;
            AddChild(mControlsSprite);

                // Cannons
            int controlIndex = 0;
            SPImage controlImage = new SPImage(mScene.TextureByName("large_face_a"));
            controlImage.X = -controlImage.Width / 2;
            controlImage.Y = mRailing.Y - controlImage.Height / 2;
            controlImage.Alpha = kControlsAlpha;

            SPSprite controlSprite = new SPSprite();
            controlSprite.X = (mLeftCannon.X - controlImage.Width / 2) + (mRightCannon.X - mLeftCannon.X) / 2 + controlImage.Width / 2;
            controlSprite.AddChild(controlImage);

            mControlsSprite.AddChild(controlSprite);
            mControls[controlIndex++] = controlSprite;

                // Voodoo Idol
            controlImage = new SPImage(mScene.TextureByName("large_face_x"));
            controlImage.X = -controlImage.Width / 2;
            controlImage.Y = mRailing.Y - controlImage.Height / 2;
            controlImage.Alpha = kControlsAlpha;

            controlSprite = new SPSprite();
            controlSprite.X = -controlImage.Width / 2 + mVoodooPlankContainer.X + mVoodooSprite.X + controlImage.Width / 2;
            controlSprite.AddChild(controlImage);

            mControlsSprite.AddChild(controlSprite);
            mControls[controlIndex++] = controlSprite;

                // Plank
            controlImage = new SPImage(mScene.TextureByName("large_face_b"));
            controlImage.X = -controlImage.Width / 2;
            controlImage.Y = mRailing.Y - controlImage.Height / 2;
            controlImage.Alpha = kControlsAlpha;

            controlSprite = new SPSprite();
            controlSprite.X = -controlImage.Width / 2 + mVoodooPlankContainer.X + mPlank.X + mPlank.Width / 2 + controlImage.Width / 2;
            controlSprite.AddChild(controlImage);

            // Setup prisoner loot prop location based on plank.
            LootProp.CommonLootDestination = new Vector2(controlSprite.X, controlImage.Y + controlImage.Height / 2);

            mControlsSprite.AddChild(controlSprite);
            mControls[controlIndex++] = controlSprite;

                // Helm
            controlImage = new SPImage(mScene.TextureByName("large_thumbstick_left"));
            controlImage.X = -controlImage.Width / 2;
            controlImage.Y = mHelm.Y - controlImage.Height / 2;

            controlSprite = new SPSprite();
            controlSprite.X = mHelm.X - controlImage.Width / 2 + controlImage.Width / 2;
            controlSprite.AddChild(controlImage);

            mControlsSprite.AddChild(controlSprite);
            mControls[controlIndex++] = controlSprite;

            // Begin retracted
            Y = mHelm.Height;
            Visible = false;
        }

        public void SetHidden(bool hidden)
        {
            mHelm.Visible = !hidden;
            mPlank.Visible = !hidden;
            mVoodooIdol.Visible = !hidden;
            mRightCannon.Visible = !hidden;
            mLeftCannon.Visible = !hidden;
            mComboDisplay.Visible = !hidden;
            mRailing.Visible = !hidden;
            ShowControls(false);
        }

        public void ShowControls(bool show)
        {
            mControlsSprite.Visible = show;
        }

        public override void Flip(bool enable)
        {
            float flipScaleX = (enable) ? -1 : 1;

            if (mVoodooSprite != null)
                mVoodooSprite.ScaleX = flipScaleX;
            if (mTwitterSprite != null)
                mTwitterSprite.ScaleX = flipScaleX;
            if (mLeftCannon != null)
                mLeftCannon.Flip(enable);
            if (mRightCannon != null)
                mRightCannon.Flip(enable);
            if (mTimeDial != null)
                mTimeDial.Flip(enable);
            if (mSpeedDial != null)
                mSpeedDial.Flip(enable);
            if (mLapDial != null)
                mLapDial.Flip(enable);
            foreach (SPSprite sprite in mControls)
                sprite.ScaleX = flipScaleX;
        }

        public void ExtendOverTime(float duration)
        {
            mScene.Juggler.RemoveTweensWithTarget(this);

            if (mLeftCannon != null)
                mLeftCannon.Overheat(false);
            if (mRightCannon != null)
                mRightCannon.Overheat(false);

            SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("Y", 0);
            mScene.Juggler.AddObject(tween);

            Visible = true;
        }

        public void RetractOverTime(float duration)
        {
            mScene.Juggler.RemoveTweensWithTarget(this);

            SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("Y", mHelm.Height);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnRetracted);
            mScene.Juggler.AddObject(tween);
        }

        private void OnRetracted(SPEvent ev)
        {
            Visible = false;
        }

        public PlayerCannon CannonOnSide(ShipDetails.ShipSide side)
        {
            return (side == ShipDetails.ShipSide.Port) ? mLeftCannon : mRightCannon;
        }

        public ShipDetails.ShipSide SideForCannon(PlayerCannon cannon)
        {
            return (cannon.Direction == 1) ? ShipDetails.ShipSide.Starboard : ShipDetails.ShipSide.Port;
        }

        public void ActivateSpeedboatWithDialDefs(List<object> dialDefs)
        {
            mPlank.Visible = false;
	        mRightCannon.Visible = false;
	        mLeftCannon.Visible = false;
	        mComboDisplay.Visible = false;
            mVoodooIdol.Visible = false;
            mRailing.Visible = false;

            if (mPotion != null)
                mPotion.Visible = false;

            // Leave the helm control hint visible
            for (int i = 0; i < mControls.Length - 1; ++i)
                mControls[i].Visible = false;
    
            mSpeedboatRailing.RemoveFromParent();
            AddChildAtIndex(mSpeedboatRailing, 0);
            mHelm.ActivateSpeedboat();
    
            if (RaceEnabled)
            {
                // Undo possible mph flashing red
                mSpeedDial.MidTextColor = DashDial.FontColor;
                return;
            }
    
	        if (dialDefs.Count < 3 || RaceEnabled)
		        return;
    
	        // Time Dial
	        mTimeDial = new DashDial();
            mTimeDial.LoadFromDictionary(dialDefs[0] as Dictionary<string, object>, null);
            AddChild(mTimeDial);
	
	        // Speed Dial
	        mSpeedDial = new DashDial();
            mSpeedDial.LoadFromDictionary(dialDefs[1] as Dictionary<string, object>, null);
            AddChild(mSpeedDial);
	
	        // Lap Dial
	        mLapDial =  new DashDial();
	        mLapDial.LoadFromDictionary(dialDefs[2] as Dictionary<string, object>, null);
	        AddChild(mLapDial);
    
            if (mScene.Flipped)
            {
                mTimeDial.Flip(true);
                mSpeedDial.Flip(true);
                mLapDial.Flip(true);
            }
        }

        public void DeactivateSpeedboat()
        {
            mTimeDial.RemoveFromParent();
            mTimeDial = null;

            mSpeedDial.RemoveFromParent();
            mSpeedDial = null;

            mLapDial.RemoveFromParent();
            mLapDial = null;
    
            mSpeedboatRailing.RemoveFromParent();
            mHelm.DeactivateSpeedboat();
    
            mPlank.Visible = true;
            mRightCannon.Visible = true;
            mLeftCannon.Visible = true;
            mComboDisplay.Visible = true;
            mVoodooIdol.Visible = true;
            mRailing.Visible = true;

            if (mPotion != null)
                mPotion.Visible = true;

            for (int i = 0; i < mControls.Length - 1; ++i)
                mControls[i].Visible = true;
        }

        public void SetRaceTime(string text)
        {
            mTimeDial.SetMidText(text);
        }

        public void SetLapTime(string text)
        {
            mTimeDial.SetBtmText(text);
        }

        public void SetMph(float mph, string format = "F3")
        {
            mSpeedDial.SetMidText(Locale.SanitizedFloat(mph, format, mSpeedDial.MidFontName, mSpeedDial.MidFontSize));
        }

        public void SetLap(string text)
        {
            mLapDial.SetMidText(text);
        }

        public void FlashFailedMphDial()
        {
            if (mSpeedDial != null)
            {
                if (mSpeedDial.MidTextColor == DashDial.FontColor)
                    mSpeedDial.MidTextColor = new Color(0xff, 00, 00);
                else
                    mSpeedDial.MidTextColor = DashDial.FontColor;
            }
        }

        public void TravelForwardInTime()
        {
            mTimeDial.SetMidText("1985");
            mTimeDial.SetBtmText("JULY 5TH");
        }

        private void SwapFlyingDutchmanTextures()
        {
            SPTexture swap = mRailing.Texture;
	        mRailing.Texture = mFlyingDutchmanRailingTexture;
	        mFlyingDutchmanRailingTexture = swap;

            float xRepeat = mRailing.Width / mRailing.Texture.Width;
            mRailing.SetTexCoord(new Vector2(xRepeat, 0), 1);
            mRailing.SetTexCoord(new Vector2(0, 1), 2);
            mRailing.SetTexCoord(new Vector2(xRepeat, 1), 3);
    
            swap = mVoodooIdol.UpState;
            mVoodooIdol.UpState = mFlyingDutchmanVoodooTexture;
            mVoodooIdol.DownState = mFlyingDutchmanVoodooTexture;
            mFlyingDutchmanVoodooTexture = swap;
        }

        public void ActivateFlyingDutchman()
        {
            if (!mFlyingDutchman)
            {
#if IOS_SCREENS
                mCannonContainer.ScaleX = mCannonContainer.ScaleY = 1f;
                mCannonContainer.Y = 0;
                mLeftCannon.X -= 2 * (112 - 76);
#endif

		        mFlyingDutchman = true;
                SwapFlyingDutchmanTextures();
                mHelm.ActivateFlyingDutchman();
                mPlank.ActivateFlyingDutchman();
                mRightCannon.ActivateFlyingDutchman();
                mLeftCannon.ActivateFlyingDutchman();
                mComboDisplay.ActivateFlyingDutchman();

#if IOS_SCREENS
                mLeftCannon.Elevation = -0.125f * SPMacros.PI;
                mCannonContainer.ScaleX = mCannonContainer.ScaleY = 1.25f;
                SPRectangle cannonBounds = mLeftCannon.BoundsInSpace(this);
                mCannonContainer.Y = mScene.ViewHeight - (cannonBounds.Y + cannonBounds.Height);

                mLeftCannon.X += 2 * (112 - 76);
                mLeftCannon.Elevation = SPMacros.SP_D2R(-11.25f);
#endif
	        }
        }

        public void DeactivateFlyingDutchman()
        {
            if (mFlyingDutchman)
            {
#if IOS_SCREENS
                mCannonContainer.ScaleX = mCannonContainer.ScaleY = 1f;
                mCannonContainer.Y = 0;
                mLeftCannon.X -= 2 * (112 - 76);
#endif

		        mFlyingDutchman = false;
                SwapFlyingDutchmanTextures();
                mHelm.DeactivateFlyingDutchman();
                mPlank.DeactivateFlyingDutchman();
                mRightCannon.DeactivateFlyingDutchman();
                mLeftCannon.DeactivateFlyingDutchman();
                mComboDisplay.DeactivateFlyingDutchman();

#if IOS_SCREENS
                mLeftCannon.Elevation = -0.125f * SPMacros.PI;
                mCannonContainer.ScaleX = mCannonContainer.ScaleY = 1.25f;
                SPRectangle cannonBounds = mLeftCannon.BoundsInSpace(this);
                mCannonContainer.Y = mScene.ViewHeight - (cannonBounds.Y + cannonBounds.Height);

                mLeftCannon.X += 2 * (112 - 76);
                mLeftCannon.Elevation = SPMacros.SP_D2R(-11.25f);
#endif
	        }
        }

        public void SetupPotions()
        {
            if (mPotion != null)
                DestroyPotions();
    
            List<Potion> activePotions = mScene.ActivePotions;
    
            if (activePotions.Count == 0)
                return;

            if (mPotionTemp == null)
                mPotionTemp = new List<SPSprite>(2);
            else
                mPotionTemp.Clear();

            mPotion = new SPSprite();
            AddChildAtIndex(mPotion, ChildIndex(mRailing) + 1);
    
            // Populate with active potions
            int i = 0;
            foreach (Potion potion in activePotions)
            {
                SPSprite potionSprite = GuiHelper.PotionSpriteWithPotion(potion, GuiHelper.GuiHelperSize.Sml, mScene);
                potionSprite.X = potionSprite.Width / 2 + 0.9f * i * potionSprite.Width;
                mPotion.AddChild(potionSprite);
                mPotionTemp.Add(potionSprite);
                ++i;
            }

            if (mPotionEffecter == null)
                mPotionEffecter = new SPEffecter(mScene.EffectForKey("AggregatePotion"), GameController.GC.AggregatePotionDraw);
    
            mPotion.X = mScene.ViewWidth - mPotion.Width;
            mPotion.Y = mScene.ViewHeight - mPotion.Height / 2;
            mPotion.AddChild(GuiHelper.AggregatePotionSprites(mPotionTemp, mPotionEffecter));
        }

        public void DestroyPotions()
        {
            if (mPotion != null)
            {
                mPotion.RemoveFromParent();
                mPotion = null;
            }
        }

        public void DidGainFocus() { }

        public void WillLoseFocus()
        {
            if (mVoodooIdol != null)
                mVoodooIdol.AutomatedButtonRelease(false);
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (!mCombatControlsEnabled)
                return;

            if (mPlank.Visible)
                mPlank.Update(gpState);
            
            mHelm.Update(gpState);
            //mHelm.Update(kbState);

            if (mRightCannon.Visible)
            {
                mRightCannon.Update(kbState);
                mRightCannon.Update(gpState);
            }

            if (mLeftCannon.Visible)
            {
                mLeftCannon.Update(kbState);
                mLeftCannon.Update(gpState);
            }

            // Voodoo Idol button press
            ControlsManager cm = ControlsManager.CM;

            if (mVoodooIdol.Visible)
            {
                if (cm.DidButtonsDepress(mIdolButtons))
                    mVoodooIdol.AutomatedButtonDepress();
                if (cm.DidButtonsRelease(mIdolButtons))
                    mVoodooIdol.AutomatedButtonRelease();
            }
        }

        public override void AdvanceTime(double time)
        {
            mComboDisplay.AdvanceTime(time);
            mHelm.AdvanceTime(time);
            mRightCannon.AdvanceTime(time);
            mLeftCannon.AdvanceTime(time);
        }

        private void OnDeckVoodooIdolPressed(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_DECK_VOODOO_IDOL_PRESSED));
        }

        public void ShowFlipControlsButton(bool show)
        {
            if (!RaceEnabled && mVoodooPlankContainer != null)
                mVoodooPlankContainer.Visible = !show;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mScene.UnsubscribeToInputUpdates(this);
                        mScene.Juggler.RemoveTweensWithTarget(this);

                        if (mVoodooIdol != null)
                        {
                            mVoodooIdol.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnDeckVoodooIdolPressed);
                            mVoodooIdol = null;
                        }

                        if (mTwitterButton != null)
                        {
                            // TODO
                        }

                        if (mPlank != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mPlank);
                            mPlank = null;
                        }

                        if (mTwitterSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mTwitterSprite);
                            mTwitterSprite = null;
                        }

                        if (mVoodooPlankContainer != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mVoodooPlankContainer);
                            mVoodooPlankContainer = null;
                        }

                        if (mRightCannon != null)
                        {
                            mRightCannon.Dispose();
                            mRightCannon = null;
                        }

                        if (mLeftCannon != null)
                        {
                            mLeftCannon.Dispose();
                            mLeftCannon = null;
                        }

                        if (mComboDisplay != null)
                        {
                            mComboDisplay.Dispose();
                            mComboDisplay = null;
                        }

                        mRailing = null;
                        mSpeedboatRailing = null;
                        mFlyingDutchmanVoodooTexture = null;
                        mFlyingDutchmanRailingTexture = null;
                        mHelm = null;
                        mVoodooSprite = null;
                        mVoodooPlankContainer = null;
                        mPotion = null;
                        mTimeDial = null;
                        mSpeedDial = null;
                        mLapDial = null;
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
