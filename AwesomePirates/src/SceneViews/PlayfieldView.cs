
//#define SHOW_FPS
//#define SHOW_RESOLUTION

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class PlayfieldView : SceneView, IInteractable
    {
        public const string CUST_EVENT_TYPE_PLAYFIELD_TUTORIAL_COMPLETED = "playfieldTutorialCompletedEvent";

        private const string kTutorialPlistPath = "data/plists/Tutorial.plist";
        private static readonly int[] kFontSizes = new int[] { 20, 32, 40, 48, 64 };

        public PlayfieldView(PlayfieldController controller)
        {
            mController = controller;
            mInFuture = false;
            mWakesTweener = new FloatTweener(1f, SPTransitions.SPLinear);
            mSkirmishShips = new Dictionary<PlayerIndex, SkirmishShip>(4, PlayerIndexComparer.Instance);

            mViewParser = new ViewParser(mController, mController, (SPEventHandler)mController.OnButtonTriggered, kTutorialPlistPath);
            mViewParser.Category = (int)PFCat.DECK;
            mViewParser.FontKey = mController.FontKey;
        }

        #region Fields
        private bool mInFuture;
        private Sea mSea;
        private FloatTweener mWakesTweener;
        private Prop mDayIntro;
        private GameSummary mGameSummary;
        private Weather mWeather;
        private ShipDeck mShipDeck;
        private SKShipDeckContainer mSKShipDeckContainer;
        private SKCountdownProp mSKCountdownProp;
        private SKTutorialView mSKTutorialView;
        private BeachActor mBeach;
        private TownActor mTown;
        private TownDock mTownDock;
        private PlayerShip mPlayerShip;
        private Dictionary<PlayerIndex, SkirmishShip> mSkirmishShips;
        private Dictionary<string, MenuDetailView> mHints;
        private List<MenuDetailView> mHintsGarbage;
        private ViewParser mViewParser;
        private TutorialBooklet mTutorialBooklet;
        private RaceTrackActor mRaceTrack;
        private Dictionary<string, object> mRaceTrackDictionary;

        // Time Travel
        private FutureManager mFutureManager;
        private SPJuggler mTimeTravelJuggler;
        private SPHashSet<RaceTrackActor> mJunkedRaceTrackActors;

        private PlayfieldController mController; // Should be weak? We'll be disposed of by the scene, so probably doesn't matter.

        // Testing
        private double mFpsElapsedTime;
        private int mFpsFrameCount;
        private SPTextField mFpsText;
        private Prop mFpsView;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_PAUSE_BUTTON; } }
        public Vector2 PlankHintLoc
        {
            get
            {
                if (mController != null && mController.SpriteLayerManager != null && mShipDeck != null && mShipDeck.Plank != null)
                {
                    SPRectangle plankBounds = mShipDeck.Plank.BoundsInSpace(mController.SpriteLayerManager.ChildAtCategory((int)PFCat.SEA));
                    return new Vector2(plankBounds.X + plankBounds.Width / 3, plankBounds.Y - 16);
                }
                else
                    return Vector2.Zero;
            }
        }
        public Vector2 IdolHintLoc
        {
            get
            {
                if (mController != null && mController.SpriteLayerManager != null && mShipDeck != null && mShipDeck.VoodooIdol != null)
                {
                    SPRectangle idolBounds = mShipDeck.VoodooIdol.BoundsInSpace(mController.SpriteLayerManager.ChildAtCategory((int)PFCat.SEA));
                    return new Vector2(idolBounds.X + idolBounds.Width / 2, idolBounds.Y - 16);
                }
                else
                    return Vector2.Zero;
            }
        }
        public Sea Sea { get { return mSea; } }
        public PlayerShip PlayerShip
        {
            get { return mPlayerShip; }
            set
            {
                if (value != mPlayerShip)
                {
		            if (mPlayerShip != null)
                    {
                        if (mRaceTrack != null)
                            mRaceTrack.RemoveRacer(mPlayerShip);
                        mPlayerShip.RemoveEventListener(PlayerShip.CUST_EVENT_TYPE_PLAYER_SHIP_SINKING, (SPEventHandler)mController.OnPlayerShipSinking);
                        mPlayerShip.RemoveEventListener(PlayerShip.CUST_EVENT_TYPE_MONTY_SKIPPERED, (SPEventHandler)mController.OnMontySkippered);

                        if (mTimeTravelJuggler != null)
                            mTimeTravelJuggler.RemoveTweensWithTarget(mPlayerShip);
                        mController.Juggler.RemoveTweensWithTarget(mPlayerShip);
                        mPlayerShip.Dispose();
                        mPlayerShip = null;
		            }
        
		            mPlayerShip = value;
                    GameController.GC.PlayerShip = mPlayerShip;

                    if (mPlayerShip != null)
                    {
                        mPlayerShip.AddEventListener(PlayerShip.CUST_EVENT_TYPE_PLAYER_SHIP_SINKING, (SPEventHandler)mController.OnPlayerShipSinking);
                        mPlayerShip.AddEventListener(PlayerShip.CUST_EVENT_TYPE_MONTY_SKIPPERED, (SPEventHandler)mController.OnMontySkippered);
                    }
	            }
            }
        }
        #endregion

        #region Methods
        public override void SetupView()
        {
            base.SetupView();

            GameController gc = GameController.GC;
            SpriteFont font = null;
            foreach (int fontSize in kFontSizes)
            {
                font = GameController.GC.Content.Load<SpriteFont>("fonts/CheekyFont-" + fontSize);
                font.LineSpacing = (int)(font.LineSpacing * 0.95f);
                SPTextField.RegisterFont(mController.FontKey, fontSize, font);
            }

            font = GameController.GC.Content.Load<SpriteFont>("fonts/HUDFont-40");
            font.Spacing = -6f;
            SPTextField.RegisterFont("HUDFont", 40, font);

            font = GameController.GC.Content.Load<SpriteFont>("fonts/" + SceneController.LeaderboardFontKey + "-32");
            SPTextField.RegisterFont(SceneController.LeaderboardFontKey, 32, font);

            CreateSea();

            // Ship Deck
            ShipDetails shipDetails = gc.PlayerDetails.ShipDetails;
            mShipDeck = new ShipDeck(shipDetails);
            Dictionary<string, object> dictionary = PlistParser.DictionaryFromPlist("data/plists/Decks.plist");
            string shipKey = shipDetails.Type;
            List<string> keys = new List<string>() { shipKey, shipKey, shipKey, "RightCannon", "LeftCannon", shipKey };
            mShipDeck.LoadFromDictionary(dictionary, keys);
            mController.AddProp(mShipDeck);

            // Events
            mShipDeck.AddEventListener(ShipDeck.CUST_EVENT_TYPE_DECK_VOODOO_IDOL_PRESSED, (SPEventHandler)mController.OnDeckVoodooIdolPressed);
            mController.AchievementManager.AddEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_COMBO_MULTIPLIER_CHANGED,
                (NumericValueChangedEventHandler)mShipDeck.ComboDisplay.OnComboMultiplierChanged);

            // Achievement View
            mAchievementPanel = new AwesomePirates.AchievementPanel((int)PFCat.BUILDINGS);
            mController.AchievementManager.View = mAchievementPanel;
            //mController.AddProp(mAchievementPanel);

            // Beach (needs to be added before PlayerCannons so that aiming reticles appear above cove)
            mBeach = new BeachActor(StaticFactory.Factory.CreateBeachActorDef());
            mBeach.Category = (int)PFCat.LAND;
            mBeach.SetupBeach();
            mController.AddActor(mBeach);

            // Town (needs to be added before PlayerCannons so that aiming reticles appear above town house and cannon fixtures)
            mTown = new TownActor(StaticFactory.Factory.CreateTownActorDef());
            mTown.SetupTown();
            mController.AddActor(mTown);

            // Town Dock
            mTownDock = new TownDock(MiscFactory.Factory.CreateTownDockDefinition(ResManager.P2MX(-208f), ResManager.P2MY(-208f), 0));
            mController.AddActor(mTownDock);

            // Town Ai
            TownAi townAi = mController.Guvnor;
            townAi.AddCannon(mTown.LeftCannon);
            townAi.AddCannon(mTown.RightCannon);

#if IOS_SCREENS
            SPImage driftwood = new SPImage(mController.TextureByName("pause-driftwood"));
            driftwood.X = mController.ViewWidth - driftwood.Width;
            driftwood.Y = 0;

            Prop driftwoodProp = new Prop(PFCat.SEA);
            driftwoodProp.AddChild(driftwood);
            mController.AddProp(driftwoodProp);
#endif

            // Weather
#if false
            mWeather = new Weather((int)PFCat.CLOUDS, 2);
            mWeather.CloudAlpha = 0.4f;
#endif
            mController.SubscribeToInputUpdates(this);

#if SHOW_FPS || SHOW_RESOLUTION
    #if SHOW_FPS
            mFpsText = SPTextField.CachedSPTextField(128, 64, "FPS", mController.FontKey, 40);
    #else
            //mFpsText = SPTextField.CachedSPTextField(500, 64, string.Format("W: {0} H: {1}", ResManager.RES_BACKBUFFER_WIDTH, ResManager.RES_BACKBUFFER_HEIGHT), mController.FontKey, 40);
            mFpsText = SPTextField.CachedSPTextField(500, 64, string.Format("W: {0} H: {1}", gc.DeviceManager.PreferredBackBufferWidth, gc.DeviceManager.PreferredBackBufferHeight), mController.FontKey, 40);
    #endif
            mFpsText.HAlign = SPTextField.SPHAlign.Left;
            mFpsText.VAlign = SPTextField.SPVAlign.Top;
            mFpsText.Color = Color.Yellow;

            mFpsView = new Prop((int)PFCat.HUD);
            mFpsView.X = 80;
            mFpsView.Y = 80;
            mFpsView.AddChild(mFpsText);
            mController.AddProp(mFpsView);
#endif
        }

        public override void AttachEventListeners()
        {
            base.AttachEventListeners();

            mController.SKManager.AddEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_SHIP_REQUEST, (PlayerIndexEventHandler)OnSKShipRequested);
        }

        public override void DetachEventListeners()
        {
            base.DetachEventListeners();

            //if (mHud != null)
            //    mHud.DetachEventListeners(); // TODO: Uncomment when we reattach in transitionFromMenu

            mController.SKManager.RemoveEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_SHIP_REQUEST, (PlayerIndexEventHandler)OnSKShipRequested);
        }

        private void CreateSea()
        {
            if (mSea != null)
                return;

            mSea = new Sea();
            mSea.AddActionEventListener(Sea.CUST_EVENT_TYPE_SEA_OF_LAVA_PEAKED, new Action<SPEvent>(mController.OnSeaOfLavaPeaked));
            mController.AddProp(mSea);
        }

        public void AdoptWaterColor(SPQuad quad)
        {
            if (mSea != null)
                mSea.AdoptWaterColor(quad);
        }

        public void OnTimeOfDayChanged(TimeOfDayChangedEvent ev)
        {
            if (mSea != null)
                mSea.OnTimeOfDayChanged(ev);
            if (mBeach != null)
                mBeach.OnTimeOfDayChanged(ev);
            if (mTown != null)
                mTown.OnTimeOfDayChanged(ev);

            // Alter wake alphas
            SPDisplayObject wakeLayer = mController.SpriteLayerManager.ChildAtCategory((int)PFCat.WAKES);
            if (ev == null || wakeLayer == null)
                return;

            float alphaTarget = 1f, tweenDuration = ev.TimeRemaining;

            switch (ev.TimeOfDay)
            {
                case TimeOfDay.DuskTransition:
                    alphaTarget = 0.7f;
                    wakeLayer.Alpha += Math.Abs(alphaTarget - wakeLayer.Alpha) * (1f - ev.ProportionRemaining);
                    break;
                case TimeOfDay.Dusk:
                    alphaTarget = 0.7f;
                    tweenDuration = 0f;
                    break;
                case TimeOfDay.EveningTransition:
                    alphaTarget = 0.4f;
                    wakeLayer.Alpha += Math.Abs(alphaTarget - wakeLayer.Alpha) * (1f - ev.ProportionRemaining);
                    break;
                case TimeOfDay.Evening:
                    alphaTarget = 0.4f;
                    tweenDuration = 0f;
                    break;
                case TimeOfDay.Midnight: goto case TimeOfDay.Evening;
                case TimeOfDay.DawnTransition:
                    alphaTarget = 1f;
                    wakeLayer.Alpha += Math.Abs(alphaTarget - wakeLayer.Alpha) * (1f - ev.ProportionRemaining);
                    break;
                default:
                    alphaTarget = 1f;
                    tweenDuration = 0f;
                    break;
            }

            mWakesTweener.Reset(wakeLayer.Alpha, alphaTarget, tweenDuration);
        }

        public void EnableScreenshotMode(bool enable)
        {
            if (mSea != null)
                mSea.EnableScreenshotMode(enable);
        }

        public void DisplaySKTutorialView()
        {
            if (mSKTutorialView == null)
            {
                mSKTutorialView = new SKTutorialView((int)PFCat.SURFACE);
                mSKTutorialView.AddEventListener(SKTutorialView.CUST_EVENT_TYPE_SK_TUTORIAL_VIEW_HIDDEN, (SPEventHandler)OnSKTutorialHidden);
            }

            mController.RemoveProp(mSKTutorialView, false);
            mController.AddProp(mSKTutorialView);
            mSKTutorialView.Show();
        }

        public void HideSKTutorialView()
        {
            if (mSKTutorialView == null)
                return;
            mSKTutorialView.Hide();
        }

        public void OnSKTutorialHidden(SPEvent ev)
        {
            if (mSKTutorialView != null)
                mController.RemoveProp(mSKTutorialView, false);
        }

        public void DisplayHintByName(string name, float x, float y, float radius, SPDisplayObject target, bool exclusive)
        {
            if (mHints == null)
                mHints = new Dictionary<string,MenuDetailView>();
            if (name == null || mHints.ContainsKey(name) || (mHints.Count > 0 && exclusive))
                return;
            int hintCategory = (int)PFCat.SURFACE;
            HintPackage package = null;
            SPImage hintImage = null;
    
            if (name.Equals(GameSettings.DONE_TUTORIAL2))
            {
                hintCategory = (int)PFCat.DECK;
                package = HintHelper.PointerHintWithScene(mController, new Vector2(x, y), radius, "", true);
            }
            else if (name.Equals(GameSettings.PLANKING_TIPS))
            {
                package = HintHelper.PointerHintWithScene(mController, new Vector2(x, y), radius, "Press", true);
                hintImage = new SPImage(mController.TextureByName("large_face_b"));
                hintImage.X = x;
                hintImage.Y = y;

                foreach (Prop prop in package.FlipProps)
                {
                    if (prop.Tag == HintHelper.kHintTextPropTag)
                    {
                        hintImage.X = 64;
                        hintImage.Y = -16;
                        prop.AddChild(hintImage);
                        break;
                    }
                }
            }
            else if (name.Equals(GameSettings.VOODOO_TIPS))
            {
                package = HintHelper.PointerHintWithScene(mController, new Vector2(x, y), radius, "Press", true);
                hintImage = new SPImage(mController.TextureByName("large_face_x"));

                foreach (Prop prop in package.FlipProps)
                {
                    if (prop.Tag == HintHelper.kHintTextPropTag)
                    {
                        hintImage.X = 64;
                        hintImage.Y = -16;
                        prop.AddChild(hintImage);
                        break;
                    }
                }
            }
            else if (name.Equals(GameSettings.BRANDY_SLICK_TIPS))
            {
                package = HintHelper.PointerHintWithScene(mController, new Vector2(x, y), radius, "Shoot to ignite!", true);
            }
            else if (name.Equals(GameSettings.PLAYER_SHIP_TIPS))
            {
                package = HintHelper.PointerHintWithScene(mController, new Vector2(x, y), target, radius, "This is your ship", false);
            }
            else if (name.Equals(GameSettings.TREASURE_FLEET_TIPS))
            {
                package = HintHelper.PointerHintWithScene(mController, new Vector2(x, y), target, radius, "Treasure Fleet", false);
            }
            else if (name.Equals(GameSettings.SILVER_TRAIN_TIPS))
            {
                package = HintHelper.PointerHintWithScene(mController, new Vector2(x, y), target, radius, "Silver Train", false);
            }
            else if (name.StartsWith(GameSettings.PIRATE_SHIP_TIPS))
            {
                package = HintHelper.HintWithScene(mController, new Vector2(x, y), target, radius, "Rival Pirate", SPUtils.ColorFromColor(0xb00000));
            }
            else if (name.StartsWith(GameSettings.NAVY_SHIP_TIPS))
            {
                package = HintHelper.HintWithScene(mController, new Vector2(x, y), target, radius, "Navy Ship", SPUtils.ColorFromColor(0xb00000));
            }
            else
            {
                package = HintHelper.PointerHintWithScene(mController, new Vector2(x, y), radius, "Testing", true);
            }
    
            MenuDetailView hint = new MenuDetailView(hintCategory);
            mHints[name] = hint;
    
            foreach (Prop prop in package.Props)
            {
                hint.AddMiscProp(prop);
                hint.AddChild(prop);
            }
    
            foreach (SPTween tween in package.LoopingTweens)
            {
                mController.Juggler.AddObject(tween);
                hint.AddLoopingTween(tween);
            }
    
            foreach (Prop prop in package.FlipProps)
                hint.AddFlipProp(prop);

            mController.AddProp(hint);
        }

        public void HideHintByName(string name)
        {
            if (name == null || mHints == null || !mHints.ContainsKey(name))
                return;
            MenuDetailView hint = mHints[name];
    
            if (hint != null)
            {
                if (mHintsGarbage == null)
                    mHintsGarbage = new List<MenuDetailView>();
                mHintsGarbage.Add(hint);
                mController.Juggler.RemoveTweensWithTarget(hint);
                mController.RemoveProp(hint);
                mHints.Remove(name);
            }
        }

        public void HideAllHints()
        {
            if (mHints != null)
            {
                List<string> keys = new List<string>(mHints.Keys);

                foreach (string key in keys)
                    HideHintByName(key);
            }
        }

        private void DestroyHints()
        {
            if (mHints != null)
            {
                Dictionary<string, MenuDetailView>.KeyCollection keys = mHints.Keys;

                foreach (string key in keys)
                {
                    MenuDetailView hint = mHints[key];
                    mController.Juggler.RemoveTweensWithTarget(hint);
                    mController.RemoveProp(hint);
                }

                mHints = null;
                mHintsGarbage = null;
            }
        }

        public void DisplayTutorialForKey(string key, int fromIndex, int toIndex)
        {
            if (mTutorialBooklet != null)
                DismissTutorial();

            mTutorialBooklet = LoadTutorialBookletForKey(key, fromIndex, toIndex);
            mTutorialBooklet.Alpha = 0;
            mTutorialBooklet.NavigationDisabled = true;
            AddEventListener(CUST_EVENT_TYPE_PLAYFIELD_TUTORIAL_COMPLETED, (SPEventHandler)mController.OnTutorialCompleted);
            mTutorialBooklet.TurnToPage(fromIndex);
            mTutorialBooklet.InputFocus = InputManager.HAS_FOCUS_TUTORIAL;
            mController.AddProp(mTutorialBooklet);

            SPTween tween = new SPTween(mTutorialBooklet, 1f);
            tween.AnimateProperty("Alpha", 1);
            mController.Juggler.AddObject(tween);
        }

        public void DismissTutorial()
        {
            if (mTutorialBooklet == null)
                return;
            mTutorialBooklet.RemoveEventListener(BookletSubview.CUST_EVENT_TYPE_BOOKLET_PAGE_TURNED, (SPEventHandler)OnBookletPageTurned);
            mTutorialBooklet.RemoveEventListener(TutorialBooklet.CUST_EVENT_TYPE_TUTORIAL_DONE_PRESSED, (SPEventHandler)OnTutorialCompleted);
            RemoveEventListener(CUST_EVENT_TYPE_PLAYFIELD_TUTORIAL_COMPLETED, (SPEventHandler)mController.OnTutorialCompleted);
            mController.Juggler.RemoveTweensWithTarget(mTutorialBooklet);
            mController.RemoveProp(mTutorialBooklet);
            mTutorialBooklet = null;
        }

        private TutorialBooklet LoadTutorialBookletForKey(string key, int fromIndex, int toIndex)
        {
            if (mViewParser == null)
            {
                mViewParser = new ViewParser(mController, mController, (SPEventHandler)mController.OnButtonTriggered, kTutorialPlistPath);
                mViewParser.Category = (int)PFCat.DECK;
                mViewParser.FontKey = mController.FontKey;
            }

            TutorialBooklet booklet = null;
            Dictionary<string, object> dict = mViewParser.ViewData;

            try
            {
                if (!dict.ContainsKey(key))
                    return null;
                dict = dict[key] as Dictionary<string, object>;

                List<object> pages = dict["Pages"] as List<object>;

                if (pages == null)
                    return null;

                if (fromIndex == -1) fromIndex = 0;
                if (toIndex == -1) toIndex = pages.Count - 1;
                toIndex = Math.Min(toIndex, pages.Count - 1);

                booklet = new TutorialBooklet((int)PFCat.DECK, key, fromIndex, toIndex);
                booklet.NumPages = pages.Count;
                booklet.AddEventListener(BookletSubview.CUST_EVENT_TYPE_BOOKLET_PAGE_TURNED, (SPEventHandler)OnBookletPageTurned);
                booklet.AddEventListener(TutorialBooklet.CUST_EVENT_TYPE_TUTORIAL_DONE_PRESSED, (SPEventHandler)OnTutorialCompleted);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to load booklet subview for key: " + key + ". " + e.Message);
            }

            return booklet;
        }

        public bool IsValidTutorialBookletPageIndex(int pageIndex)
        {
            return !GameSettings.GS.SettingForKey(GameSettings.DONE_TUTORIAL);
        }

        public override void Flip(bool enable)
        {
            base.Flip(enable);

            float flipScaleX = (enable) ? -1 : 1;

            if (mDayIntro != null)
                mDayIntro.ScaleX = flipScaleX;

            if (mHints != null)
            {
                foreach (KeyValuePair<string, MenuDetailView> kvp in mHints)
                    kvp.Value.Flip(enable);
            }

            ApplyTutorialFlipConstraints(enable);
        }

        private void ApplyTutorialFlipConstraints(bool isFlipped)
        {
            // Do nothing for now
        }

        private void OnBookletPageTurned(SPEvent ev)
        {
            BookletSubview subview = ev.CurrentTarget as BookletSubview;
            MenuDetailView page = mViewParser.ParseSubviewByName("Pages", subview.BookKey, subview.PageIndex);
            subview.CurrentPage = page;

            ApplyTutorialFlipConstraints(mController.Flipped);

            if (mTutorialBooklet == null)
                return;
    
            switch (mController.TutState)
            {
                case PlayfieldController.TutorialState.Primary:
                case PlayfieldController.TutorialState.Secondary:
                case PlayfieldController.TutorialState.Tertiary:
                case PlayfieldController.TutorialState.Quaternary:
                case PlayfieldController.TutorialState.Quinary:
                case PlayfieldController.TutorialState.None:
                default:
                    ProcessBookletPageTurned(subview, page, mTutorialBooklet.PageIndex);
                    break;
            }
        }

        private void ProcessBookletPageTurned(BookletSubview subview, MenuDetailView page, int bookletIndex)
        {
            ResOffset offset = null;

            switch (mController.TutState)
            {
                case PlayfieldController.TutorialState.Quaternary:
                    {
                        switch (bookletIndex)
                        {
                            case 0:
                                {
                                    offset = new ResOffset(0, 0, 0, ResManager.CUSTY);
                                    DisplayHintByName(GameSettings.DONE_TUTORIAL2, 90 + offset.X, 604 + offset.Y, 20, null, false);
                                    mShipDeck.ComboDisplay.SetComboMultiplierAnimated(mController.AchievementManager.ComboMultiplierMax);
                                }
                                break;
                            case 1:
                                {
                                    HideHintByName(GameSettings.DONE_TUTORIAL2);
                                    mShipDeck.ComboDisplay.SetComboMultiplierAnimated(0);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case PlayfieldController.TutorialState.Quinary:
                    {
                        switch (bookletIndex)
                        {
                            case 0:
                                {
                                    offset = ResManager.RESM.ItemOffsetWithAlignment(ResManager.ResAlignment.Center);

                                    // Add potion
                                    SPSprite potionSprite = GuiHelper.PotionSpriteWithPotion(Potion.PotencyPotion(), GuiHelper.GuiHelperSize.Lge, mController);
                                    potionSprite.X = 136 + offset.X;
                                    potionSprite.Y = 198 + offset.Y;
                                    page.AddChild(potionSprite);

                                    potionSprite = GuiHelper.PotionSpriteWithPotion(Potion.LongevityPotion(), GuiHelper.GuiHelperSize.Lge, mController);
                                    potionSprite.X = 816 + offset.X;
                                    potionSprite.Y = 198 + offset.Y;
                                    page.AddChild(potionSprite);
                                }
                                break;
                        }
                    }
                    break;
                case PlayfieldController.TutorialState.Primary:
                case PlayfieldController.TutorialState.Secondary:
                case PlayfieldController.TutorialState.Tertiary:
                case PlayfieldController.TutorialState.None:
                default:
                    break;
            }
        }

        private void OnTutorialCompleted(SPEvent ev)
        {
            mController.VoodooManager.HideMenu();
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_PLAYFIELD_TUTORIAL_COMPLETED));
        }

        public void EnableSlowedTime(bool enable)
        {
            mSea.EnableSlowedTime(enable);
        }

        public void TravelBackInTime()
        {
            if (!mInFuture)
                return;
            mTimeTravelJuggler.RemoveAllObjects();
            mController.ActorBrains.InFuture = false;
            mController.ActorBrains.ShipsPaused = false;
            mTown.TravelBackInTime(3);
            mBeach.TravelBackInTime(3);
            mInFuture = false;
        }

        public float TravelForwardInTime()
        {
            if (mFutureManager != null)
            {
                mFutureManager.Dispose();
                mFutureManager = null;
            }
    
            if (mTimeTravelJuggler == null)
                mTimeTravelJuggler = new SPJuggler();
            mTimeTravelJuggler.RemoveAllObjects();
    
	        float delay = 0.0f;
	        mFutureManager = new FutureManager();
            mFutureManager.SparkElectricityOnSprite(mPlayerShip);
            mController.StopAmbientSounds();
            delay += FutureManager.ElectricityDuration;

	        // Flame Paths + Ship Disappearance
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mFutureManager != null) mFutureManager.IgniteFlamePathsAtSprite(mPlayerShip); });
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mPlayerShip != null) mPlayerShip.TravelThroughTime(0.25f); });
	        delay += FutureManager.FlamePathDuration + FutureManager.FlamePathExtinguishDuration;
	
	        // Morph into 1985
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mController != null && mController.ActorBrains != null) mController.ActorBrains.DockAllShips(); });
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mController != null && mController.ActorBrains != null) mController.ActorBrains.ShipsPaused = true; });
    
            if (mJunkedRaceTrackActors == null)
                mJunkedRaceTrackActors = new SPHashSet<RaceTrackActor>();
            mJunkedRaceTrackActors.Add(mRaceTrack);
            FadeOutRaceTrack(mRaceTrack, 2, delay, mTimeTravelJuggler);
            DestroyRaceTrack();
    
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mTown != null) mTown.TravelForwardInTime(3); });
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mBeach != null) mBeach.TravelForwardInTime(3); });
	        delay += 3.0f;
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mShipDeck != null) mShipDeck.TravelForwardInTime(); });
	
	        // Sparks as we break into future realm
	        for (int i = 0; i < 3; ++i)
            {
                mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mFutureManager != null) mFutureManager.SparkElectricityAt(530, 304); });
		
		        if (i < 2)
			        delay += FutureManager.ElectricityDuration * 1.1f;
	        }
	
	        // Ship reappearance
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mPlayerShip != null) mPlayerShip.EmergeInPresentAt(530, 304, 0.25f); });
            mTimeTravelJuggler.DelayInvocation(mController, delay, mController.PlayAmbientSounds);
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mController != null && mController.ActorBrains != null) mController.ActorBrains.InFuture = true; });
	        delay += 0.25f;
            mTimeTravelJuggler.DelayInvocation(this, delay, delegate { if (mController != null && mController.ActorBrains != null) mController.ActorBrains.ShipsPaused = false; });
    
            mInFuture = true;
	        return delay;
        }

        private void FadeInRaceTrack(RaceTrackActor raceTrack, float duration, float delay)
        {
            if (raceTrack == null)
		        return;
            if (mTimeTravelJuggler == null)
                mTimeTravelJuggler = new SPJuggler();
    
	        SPTween tween = new SPTween(raceTrack, duration);
            tween.AnimateProperty("Alpha", 1);
            tween.Delay = delay;
            mTimeTravelJuggler.AddObject(tween);
        }

        private void FadeOutRaceTrack(RaceTrackActor raceTrack, float duration, float delay, SPJuggler juggler)
        {
            if (raceTrack == null || juggler == null)
                return;

            SPTween tween = new SPTween(raceTrack, duration);
            tween.AnimateProperty("Alpha", 0);
            tween.Delay = delay;
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnRaceTrackFadedOut);
            juggler.AddObject(tween);
        }

        private void OnRaceTrackFadedOut(SPEvent ev)
        {
            SPTween tween = ev.CurrentTarget as SPTween;
            RaceTrackActor actor = tween.Target as RaceTrackActor;
    
            if (actor != null)
            {
                if (mController != null)
                    mController.RemoveActor(actor);
                if (mJunkedRaceTrackActors != null)
                    mJunkedRaceTrackActors.Remove(actor);
            }
        }

        private void DestroyRaceTrack()
        {
            if (mRaceTrack == null)
                return;
            if (mPlayerShip != null)
                mRaceTrack.RemoveRacer(mPlayerShip);
            if (mController != null)
                mRaceTrack.RemoveEventListener(RaceTrackActor.CUST_EVENT_TYPE_88MPH, (SPEventHandler)mController.OnRaceTrackConquered);
            mRaceTrack = null;
        }

        public void EnableWeather(bool enable)
        {
            if (mWeather != null)
                mWeather.Enabled = enable;
        }

        public void EnableCombatInterface(bool enable)
        {
            if (!enable)
            {
                mController.VoodooManager.HideMenu();
                mController.AchievementManager.HideCombatText();
            }
    
            mController.VoodooManager.Touchable = enable;

            if (mShipDeck != null)
                mShipDeck.CombatControlsEnabled = enable;

            if (mSKShipDeckContainer != null)
                mSKShipDeckContainer.EnableCombatControls(enable);
        }

        public void ShowDayIntroForDay(uint day, float duration)
        {
            if (mDayIntro != null)
            {
                mController.Juggler.RemoveTweensWithTarget(mDayIntro);
                mController.RemoveProp(mDayIntro);
                mDayIntro = null;
            }
    
            mDayIntro = new Prop(PFCat.SURFACE);
            mDayIntro.Alpha = 0;
    
            SPSprite dayIntroCanvas = new SPSprite();
            mDayIntro.AddChild(dayIntroCanvas);

            SPImage dayImage = new SPImage(mController.TextureByName("fancy-day"));
            dayIntroCanvas.AddChild(dayImage);

            SPImage numberImage = new SPImage(mController.TextureByName("fancy-" + day));
            numberImage.X = dayImage.Width + numberImage.Width;
            dayIntroCanvas.AddChild(numberImage);
    
            mDayIntro.X = mController.ViewWidth / 2;
            mDayIntro.Y = (mController.ViewHeight - mDayIntro.Height) / 2;
    
            dayIntroCanvas.X = -dayIntroCanvas.Width / 2;
            mController.AddProp(mDayIntro);
    
            string text = GameController.GC.TimeKeeper.IntroForDay(day);
    
            if (text != null)
            {
                text = "'" + text + "'";
                SPTextField textField = new SPTextField(512, 56, text, mController.FontKey, 40);
                textField.X = (mDayIntro.Width - textField.Width) / 2;
                textField.Y = mDayIntro.Height + textField.Height / 3;
                textField.HAlign = SPTextField.SPHAlign.Center;
                textField.VAlign = SPTextField.SPVAlign.Top;
                textField.Color = SPUtils.ColorFromColor(0xfcc30e);
                dayIntroCanvas.AddChild(textField);
            }
    
            if (mController.Flipped)
                mDayIntro.ScaleX = -1;
    
            SPTween tween = new SPTween(mDayIntro, duration);
            tween.AnimateProperty("Alpha", 1);
            mController.Juggler.AddObject(tween);
        }

        public void HideDayIntroOverTime(float duration, float delay = 0f)
        {
            if (mDayIntro == null)
                return;
            //[mController.juggler removeTweensWithTarget:mDayIntro]; // This would kill our showDayIntroForDay tween.
    
            SPTween tween = new SPTween(mDayIntro, duration);
            tween.AnimateProperty("Alpha", 0);
            tween.Delay = delay;
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnDayIntroHidden);
            mController.Juggler.AddObject(tween);
        }

        public void DestroyDayIntroOverTime(float duration)
        {
            if (mDayIntro == null)
                return;
            mController.Juggler.RemoveTweensWithTarget(mDayIntro);
    
            SPTween tween = new SPTween(mDayIntro, duration * mDayIntro.Alpha);
            tween.AnimateProperty("Alpha", 0);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnDayIntroHidden);
            mController.Juggler.AddObject(tween);
            mDayIntro = null;
        }

        public void OnDayIntroHidden(SPEvent ev)
        {
            SPTween tween = ev.CurrentTarget as SPTween;
            Prop prop = tween.Target as Prop;
            mController.RemoveProp(prop);

            if (prop == mDayIntro)
                mDayIntro = null;
        }

        public void SKGameCountdownStarted()
        {
            if (mSKCountdownProp != null)
                return;

            mSKCountdownProp = new SKCountdownProp((int)PFCat.SURFACE, mController.SKManager.GameCountdownValue);
            mController.AddProp(mSKCountdownProp);

            HideSKTutorialView();
        }

        public void SKGameCountdownCompleted()
        {
            SKManager.SKGameCountdown countdown = mController.SKManager.GameCountdown;
            if (mSKShipDeckContainer != null && countdown == SKManager.SKGameCountdown.PreGame)
                mSKShipDeckContainer.AwaitingPlayers = false;

            if (mSKCountdownProp != null)
            {
                if (countdown == SKManager.SKGameCountdown.PreGame)
                    mSKCountdownProp.PlayConclusionSequence();
                else
                    mController.RemoveProp(mSKCountdownProp);
                mSKCountdownProp = null;
            }
        }

        public void SKGameCountdownCancelled()
        {
            if (mSKCountdownProp != null)
            {
                mController.RemoveProp(mSKCountdownProp);
                mSKCountdownProp = null;
            }
        }

        public void PauseMenuDisplayed()
        {
            if (mShipDeck != null)
                mShipDeck.ShowControls(true);
        }

        public void PauseMenuDismissed()
        {
            if (mShipDeck != null)
                mShipDeck.ShowControls(false);
        }

        private void SetupShipDeck(GameMode gameMode)
        {
            GameController gc = GameController.GC;

            if (gameMode == GameMode.Career)
            {
                ShipDetails shipDetails = gc.PlayerDetails.ShipDetails;
                shipDetails.RemoveEventListener(ShipDetails.CUST_EVENT_TYPE_PRISONERS_VALUE_CHANGED, (NumericValueChangedEventHandler)mShipDeck.Plank.OnPrisonersChanged);
                shipDetails.RemoveEventListener(ShipDetails.CUST_EVENT_TYPE_PRISONERS_VALUE_CHANGED, (NumericValueChangedEventHandler)mController.OnPrisonersChanged);

                shipDetails.AddEventListener(ShipDetails.CUST_EVENT_TYPE_PRISONERS_VALUE_CHANGED, (NumericValueChangedEventHandler)mShipDeck.Plank.OnPrisonersChanged);
                shipDetails.AddEventListener(ShipDetails.CUST_EVENT_TYPE_PRISONERS_VALUE_CHANGED, (NumericValueChangedEventHandler)mController.OnPrisonersChanged);

                mShipDeck.Plank.ShipDetails = shipDetails;
                mShipDeck.ComboDisplay.DeactivateProc();
                mShipDeck.DeactivateFlyingDutchman();
                mShipDeck.SetupPotions();
                mShipDeck.Helm.ResetRotation();
                mController.SubscribeToInputUpdates(mShipDeck);
                mShipDeck.ExtendOverTime(MenuController.kMenuTransitionDuration);
                //[self hideTwitter];
            }
            else
            {
                if (mSKShipDeckContainer != null)
                    mController.RemoveProp(mSKShipDeckContainer);

                mSKShipDeckContainer = new SKShipDeckContainer((int)PFCat.SK_HUD, gameMode);

                if (gameMode == GameMode.SKFFA)
                {
                    SKTeamIndex teamIndex = SKTeamIndex.Red;
                    for (PlayerIndex playerIndex = PlayerIndex.One; playerIndex <= PlayerIndex.Four; ++playerIndex, ++teamIndex)
                    {
                        SKShipDeck skShipDeck = new SKShipDeck(mSKShipDeckContainer.Category, gameMode, teamIndex, playerIndex);
                        mSKShipDeckContainer.AddShipDeck(skShipDeck);
                    }
                }
                else
                {
                    for (SKTeamIndex teamIndex = SKTeamIndex.Red; teamIndex <= SKTeamIndex.Blue; ++teamIndex)
                    {
                        SKShipDeck skShipDeck = new SKShipDeck(mSKShipDeckContainer.Category, gameMode, teamIndex);
                        mSKShipDeckContainer.AddShipDeck(skShipDeck);
                    }
                }

                mSKShipDeckContainer.Compile();
                mController.SubscribeToInputUpdates(mSKShipDeckContainer);
                mController.AddProp(mSKShipDeckContainer);
                mSKShipDeckContainer.ExtendOverTime(MenuController.kMenuTransitionDuration);
            }
        }

        public void TransitionFromMenu()
        {
            GameController gc = GameController.GC;
            GameMode gameMode = mController.GameMode;
    
            // Achievement Panel
            MoveAchievementPanelToCategory((int)PFCat.BUILDINGS);
    
            // Ship Deck
            SetupShipDeck(gameMode);

            if (mInFuture)
                TravelBackInTime();
    
            // Race Track
            if (mController.RaceEnabled)
            {
                if (mRaceTrackDictionary == null)
                    mRaceTrackDictionary = PlistParser.DictionaryFromPlist("data/plists/RaceTrack.plist");
        
                if (mRaceTrack == null)
                {
                    int laps = 5;
                    ActorDef actorDef = MiscFactory.Factory.CreateRaceTrackDefWithDictionary(mRaceTrackDictionary);
                    mRaceTrack = new RaceTrackActor(actorDef, laps);
                    mRaceTrack.Alpha = 0;
                    mRaceTrack.SetupRaceTrackWithDictionary(mRaceTrackDictionary);
                    mRaceTrack.AddEventListener(RaceTrackActor.CUST_EVENT_TYPE_88MPH, (SPEventHandler)mController.OnRaceTrackConquered);
                    mController.AddActor(mRaceTrack);
                }
        
                mRaceTrack.PrepareForNewRace();
                FadeInRaceTrack(mRaceTrack, 2, 0);
                mShipDeck.ActivateSpeedboatWithDialDefs(mRaceTrackDictionary["DashDials"] as List<object>);
            }
            else
            {
                if (mRaceTrack != null)
                {
                    FadeOutRaceTrack(mRaceTrack, 2, 0, mController.Juggler);
                    DestroyRaceTrack();
                }
        
                if (mShipDeck.RaceEnabled)
                    mShipDeck.DeactivateSpeedboat();
            }
    
            // Sea
            mSea.PrepareForNewGame();
    
            // HUD
            if (gameMode == GameMode.Career)
            {
#if IOS_SCREENS
                mHud = new AwesomePirates.Hud((int)PFCat.SURFACE, 0, 2, Color.Black);
#else
                mHud = new AwesomePirates.Hud((int)PFCat.SURFACE, 0, 8, Color.White);
#endif
                mHud.SetInfamyValue(gc.ThisTurn.Infamy);
                mHud.AttachEventListeners();
                mHud.EnableScoredMode(!mController.RaceEnabled);
                mController.AddProp(mHud);
            }

            mController.AchievementManager.BroadcastComboMultiplier();
            
            // For debugging
            //[mController.actorBrains addEventListener:@selector(onAiChanged:) atObject:mHud forType:CUST_EVENT_TYPE_AI_STATE_VALUE_CHANGED];
	        //[mHud setAiValue:gc.aiKnob->state];
    
            // Self
            EnableCombatInterface(true);
    
            // Misc
            mShipDeck.VoodooIdol.Visible = !mShipDeck.RaceEnabled;
        }

        public void TransitionToMenu()
        {
            GameMode gameMode = mController.GameMode;

            mBeach.ClearDepartures();

            // Race Track
            if (mRaceTrack != null)
            {
                FadeOutRaceTrack(mRaceTrack, 2, 0, mController.Juggler);
                DestroyRaceTrack();
            }

            // Time Travel
            if (mTimeTravelJuggler != null)
                mTimeTravelJuggler.RemoveAllObjects();
            if (mFutureManager != null)
            {
                mFutureManager.Dispose();
                mFutureManager = null;
            }
    
            if (mJunkedRaceTrackActors != null)
            {
                foreach (RaceTrackActor raceTrack in mJunkedRaceTrackActors.EnumerableSet)
                    FadeOutRaceTrack(raceTrack, 2, 0, mController.Juggler);
                mJunkedRaceTrackActors.Clear();
            }

            // Ship Deck
            if (gameMode == GameMode.Career)
            {
                mShipDeck.RetractOverTime(MenuController.kMenuTransitionDuration);
                mController.UnsubscribeToInputUpdates(mShipDeck);
            }
            else if (mSKShipDeckContainer != null)
            {
                mSKShipDeckContainer.RetractOverTime(MenuController.kMenuTransitionDuration);
                mController.UnsubscribeToInputUpdates(mSKShipDeckContainer);
            }
    
            // HUD
            if (mHud != null)
            {
                mController.RemoveProp(mHud);
                mHud = null;
            }

            // Player Ship
            if (gameMode == GameMode.Career)
                DestroyPlayerShip();
            else
            {
                for (PlayerIndex i = PlayerIndex.One; i <= PlayerIndex.Four; ++i)
                    DestroySkirmishShip(i);
            }
    
            // Self
            EnableCombatInterface(false);
            DestroyDayIntroOverTime(0.5f);
            DismissTutorial();
            HideAllHints();
            DestroyGameSumary();
        }

        public void CreatePlayerShip()
        {
            if (PlayerShip != null)
                DestroyPlayerShip();
    
            GameController gc = GameController.GC;
            ResOffset offset = ResManager.RESM.ItemOffsetWithAlignment(ResManager.ResAlignment.LowerRight);
            string shipActorType = (mController.RaceEnabled) ? "Speedboat" : "PlayerShip";
            ActorDef actorDef = ShipFactory.Factory.CreatePlayerShipDefForShipType(shipActorType, ResManager.P2MX(938 + offset.X), ResManager.P2MY(410 + offset.Y), SPMacros.SP_D2R(45));
            PlayerShip = new AwesomePirates.PlayerShip(actorDef);
    
            // ---- Keep in this order
            ShipDetails shipDetails = gc.PlayerDetails.ShipDetails;
            mPlayerShip.ShipDetails = shipDetails;
            mPlayerShip.ShipDeck = mShipDeck;
            mPlayerShip.CannonDetails = gc.PlayerDetails.CannonDetails;
            // ----
    
            if (mController.RaceEnabled)
                mPlayerShip.MotorBoating = true;
            mPlayerShip.SetupShip();

            mBeach.EnqueueDepartingShip(mPlayerShip);
            mController.ActorBrains.AddActor(mPlayerShip);

            if (mController.RaceEnabled)
                mRaceTrack.AddRacer(mPlayerShip);
            else
                mController.Guvnor.AddTarget(mPlayerShip);
        }

        public void DestroyPlayerShip()
        {
            if (mPlayerShip == null)
                return;
            mController.RemoveActor(mPlayerShip);
            mController.Guvnor.RemoveTarget(mPlayerShip);
            PlayerShip = null;
        }

        public SkirmishShip CreateSkirmishShip(PlayerIndex playerIndex)
        {
            GameController gc = GameController.GC;
            ResOffset offset = ResManager.RESM.ItemOffsetWithAlignment(ResManager.ResAlignment.LowerRight);
            ActorDef actorDef = ShipFactory.Factory.CreateSkirmishShipDefForShipType("SkirmishShip", ResManager.P2MX(938 + offset.X), ResManager.P2MY(410 + offset.Y), SPMacros.SP_D2R(45));
            SKTeamIndex teamIndex = mController.SKManager.TeamIndexForIndex(playerIndex);

#if SK_BOTS
            SkirmishShip ship = new SKBotShip(actorDef, playerIndex, teamIndex);
            SKBotCoupler botCoupler = new SKBotCoupler((int)PFCat.PLAYABLE_SHIPS, playerIndex, ship as SKBotShip);
            mController.SubscribeToInputUpdates(botCoupler);
            mController.AddProp(botCoupler);
#else
            SkirmishShip ship = new SkirmishShip(actorDef, playerIndex, teamIndex);
#endif

            // ---- Keep in this order
            ship.ShipDetails = ShipFactory.Factory.CreateShipDetailsForType("Man o' War");
            ship.ShipDeck = mSKShipDeckContainer.ShipDeckForIndex(teamIndex);
            ship.CannonDetails = CannonFactory.Factory.CreateCannonDetailsForType("Perisher");
            // ----

            ship.SetupShip();
            mController.ActorBrains.AddActor(ship);
            return ship;
        }

        public void DestroySkirmishShip(PlayerIndex playerIndex)
        {
            if (mSkirmishShips == null || !mSkirmishShips.ContainsKey(playerIndex))
                return;

            SkirmishShip ship = mSkirmishShips[playerIndex];
            mController.RemoveActor(ship);
            mSkirmishShips.Remove(playerIndex);
        }

        public SkirmishShip SkirmishShipForIndex(PlayerIndex playerIndex)
        {
            if (mSkirmishShips != null && mSkirmishShips.ContainsKey(playerIndex))
                return mSkirmishShips[playerIndex];
            else
                return null;
        }

        private void OnSKShipRequested(PlayerIndexEvent ev)
        {
            if (ev == null || mSkirmishShips == null || mSkirmishShips.ContainsKey(ev.PlayerIndex))
                return;

            SkirmishShip ship = CreateSkirmishShip(ev.PlayerIndex);
            mSkirmishShips.Add(ev.PlayerIndex, ship);
            mBeach.EnqueueDepartingShip(ship);
        }

        public void OnSKPlayerShipSinking(PlayerIndexEvent ev)
        {
            if (ev == null || mSkirmishShips == null || !mSkirmishShips.ContainsKey(ev.PlayerIndex))
                return;

            SkirmishShip ship = mSkirmishShips[ev.PlayerIndex];
            mSkirmishShips.Remove(ev.PlayerIndex);
            ship.Sink();
        }

        public void AdvanceFpsCounter(double time)
        {
#if SHOW_FPS
            if (mFpsText == null)
                return;

            mFpsElapsedTime += time;
            ++mFpsFrameCount;

            if (mFpsElapsedTime >= 2.0)
            {
                int fps = (int)(mFpsFrameCount / mFpsElapsedTime);
                mFpsText.Text = "" + fps;
                mFpsElapsedTime -= 2.0;
                mFpsFrameCount -= 2 * fps;
            }
#else
            return;
#endif
        }

        public override void AdvanceTime(double time)
        {
            if (mWeather != null)
                mWeather.AdvanceTime(time);
            if (mTimeTravelJuggler != null)
                mTimeTravelJuggler.AdvanceTime(time);
            if (mHintsGarbage != null && mHintsGarbage.Count > 0)
            {
                foreach (Prop prop in mHintsGarbage)
                    mController.RemoveProp(prop);
                mHintsGarbage.Clear();
            }

            if (mSKCountdownProp != null)
                mSKCountdownProp.SetCountdownValue(mController.SKManager.GameCountdownValue);

            SPDisplayObject wakeLayer = mController.SpriteLayerManager.ChildAtCategory((int)PFCat.WAKES);
            if (mWakesTweener != null && wakeLayer != null)
            {
                mWakesTweener.AdvanceTime(time);
                if (!mWakesTweener.Delaying && wakeLayer.Alpha != mWakesTweener.TweenedValue)
                    wakeLayer.Alpha = mWakesTweener.TweenedValue;
            }

            base.AdvanceTime(time);
        }

        public void DisplayFirstMateAlert(List<string> msgs, int userData, int dir, float delay)
        {
            DisplayHelpAlert(msgs, "first-mate", userData, dir, delay);
        }

        public void DisplayEtherealAlert(List<string> msgs, int userData, int dir, float delay)
        {
            DisplayHelpAlert(msgs, "ethereal-help", userData, dir, delay);
        }

        private void DisplayHelpAlert(List<string> msgs, string textureName, int userData, int dir, float delay)
        {
            FirstMate mate = new FirstMate(mController.HelpCategory, msgs, textureName, dir);
            mate.UserData = userData;
            mate.AddEventListener(FirstMate.CUST_EVENT_TYPE_FIRST_MATE_DECISION, (SPEventHandler)mController.OnFirstMateDecision);
            mate.AddEventListener(FirstMate.CUST_EVENT_TYPE_FIRST_MATE_RETIRED, (SPEventHandler)mController.OnFirstMateRetiredToCabin);
            mController.AddProp(mate);
            mate.BeginAnnouncements(delay);
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
#if true
            if (ControlsManager.CM.DidButtonRelease(Buttons.Start))
                    mController.DisplayPauseMenu();
#else

            if (mController.GameMode == GameMode.Career)
            {
                if (ControlsManager.CM.DidButtonRelease(Buttons.Start))
                    mController.DisplayPauseMenu();
            }
            else
            {
                for (PlayerIndex i = PlayerIndex.One; i <= PlayerIndex.Four; ++i)
                {
                    if (ControlsManager.CM.DidButtonRelease(Buttons.Start, i))
                    {
                        mController.DisplayPauseMenu();
                        break;
                    }
                }
            }
#endif
        }

        public void PrepareForGameOver()
        {
            if (mPlayerShip != null)
                mPlayerShip.PrepareForGameOver();

            if (mSkirmishShips != null)
            {
                foreach (KeyValuePair<PlayerIndex, SkirmishShip> kvp in mSkirmishShips)
                    kvp.Value.PrepareForGameOver();
            }

            mSkirmishShips.Clear();

            EnableCombatInterface(false);
            HideDayIntroOverTime(0.5f);

            DismissTutorial();
            HideAllHints();
        }

        public void DisplayGameOverSequence(int masteryLevel, float levelXPFraction)
        {
            if (mGameSummary != null)
                return;
            MoveAchievementPanelToCategory((int)PFCat.DECK);
    
            mGameSummary = new GameSummary((int)PFCat.DIALOGS, masteryLevel, levelXPFraction);
    
            mGameSummary.HideSummaryScroll();
            mGameSummary.AddEventListener(GameSummary.CUST_EVENT_TYPE_GAME_SUMMARY_RETRY, (SPEventHandler)mController.OnGameOverRetryPressed);
            mGameSummary.AddEventListener(GameSummary.CUST_EVENT_TYPE_GAME_SUMMARY_MENU, (SPEventHandler)mController.OnGameOverMenuPressed);
            mController.AddProp(mGameSummary);

            float delay = mGameSummary.DisplayGameOverSequence();
            mController.Juggler.DelayInvocation(mController.ObjectivesManager, delay, mController.ObjectivesManager.ProcessEndOfTurn);
        }

        public void DisplayGameSummary()
        {
            if (mGameSummary == null)
                return;

            float delay = 0.25f;
            mGameSummary.AttachGamerPic(mController.GamerPic);
            mController.Juggler.DelayInvocation(mGameSummary, delay, mGameSummary.DisplaySummaryScroll);

            GameController.GC.ProcessEndOfTurn();

            delay += 0.5f;
            mController.Juggler.DelayInvocation(mGameSummary, delay, mGameSummary.DisplayStamps);
            delay += mGameSummary.StampsDuration;
            mController.Juggler.DelayInvocation(mGameSummary, delay, mGameSummary.DisplayMasterySequence);
            //mController.Juggler.DelayInvocation(mController, delay + mGameSummary.StampsDelay, mController.GameOverSequenceDidComplete);
        }

        public void DestroyGameSumary()
        {
            if (mGameSummary != null)
            {
                mGameSummary.DetachGamerPic(mController.GamerPic);
                mGameSummary.RemoveEventListener(GameSummary.CUST_EVENT_TYPE_GAME_SUMMARY_RETRY, (SPEventHandler)mController.OnGameOverRetryPressed);
                mGameSummary.RemoveEventListener(GameSummary.CUST_EVENT_TYPE_GAME_SUMMARY_MENU, (SPEventHandler)mController.OnGameOverMenuPressed);
                mController.Juggler.RemoveTweensWithTarget(mGameSummary);
                mController.RemoveProp(mGameSummary); // Disposes
                mGameSummary = null;
            }
        }

        public void EnableSummaryButtonForKey(bool enable, string key)
        {
            if (mGameSummary != null)
                mGameSummary.EnableMenuButton(enable, key);
        }

        public override void DestroyView()
        {
            DestroyHints();
            DestroyGameSumary();

            if (mSea != null)
            {
                mSea.RemoveEventListener(Sea.CUST_EVENT_TYPE_SEA_OF_LAVA_PEAKED);
                mController.RemoveProp(mSea);
                mSea = null;
            }

            if (mWeather != null)
            {
                mWeather.Dispose();
                mWeather = null;
            }

            if (mShipDeck != null)
            {
                mShipDeck.Dispose();
                mShipDeck = null;
            }

            if (mDayIntro != null)
            {
                mController.Juggler.RemoveTweensWithTarget(mDayIntro);
                mController.RemoveProp(mDayIntro);
                mDayIntro = null;
            }

            if (mFpsText != null)
            {
                mFpsText.Dispose();
                mFpsText = null;
            }

            if (mSKTutorialView != null)
            {
                mSKTutorialView.AddEventListener(SKTutorialView.CUST_EVENT_TYPE_SK_TUTORIAL_VIEW_HIDDEN, (SPEventHandler)OnSKTutorialHidden);
                mController.RemoveProp(mSKTutorialView);
                mSKTutorialView = null;
            }

            mController.UnsubscribeToInputUpdates(this);

            foreach (int fontSize in kFontSizes)
                SPTextField.DeregisterFont(mController.FontKey, fontSize);
            base.DestroyView();
        }
        #endregion
    }
}
