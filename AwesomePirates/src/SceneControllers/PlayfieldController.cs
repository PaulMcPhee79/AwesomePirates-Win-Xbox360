using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using System.Diagnostics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class PlayfieldController : SceneController
    {
        public enum PfState
        {
            Hibernating = 0,
            Title,
            Menu,
            Playing,
            EndOfTurn,
            DelayedRetry,
            DelayedQuit
        }

        public enum TutorialState
        {
            None = 0,
            Primary,
            Secondary,
            Tertiary,
            Quaternary,
            Quinary         // Senary, Septenary, Octonary, Nonary, Denary
        }

        private const int kFirstMateMutinied = 1;
        private const int kEtherealPotionNotice = 2;

        public PlayfieldController()
        {
            GameController gc = GameController.GC;

            mAmbienceShouldPlay = false;
            mDidRetry = mDidQuit = false;
            mRaceEnabled = false;
            mMontyShouldMutiny = false;
            mTimeSlowed = false;
            mSuspendedMode = false;
            mResettingScene = false;
            mLaunchTimer = 0.01f;
            mPrizesBitmap = 0;
            mSceneKey = "Playfield";
            mView = null;
            mRestorableIndex = null;

            mActorBrains = null;
            mGuvnor = null;
            mVoodooManager = null;
            //mGuvnor = null; // TODO

            // Physics
            mGravity = new Vector2(0f, 0f);
            mVelocityIterations = 1;
            mPositionIterations = 1;
            mStepDuration = 1.0 / GameController.GC.Fps;
            mStepAccumulator = 0;
            mTimeRatio = 1f;
            mWorld = new World(mGravity, true);
            mWorld.ContinuousPhysics = false;
            mContactListener = new ActorContactListener();
            mWorld.ContactListener = mContactListener;

            mTutorialState = TutorialState.None;
            mState = mPreviousState = PfState.Playing;
        }

        #region Fields
        private bool mSuspendedMode;
        private bool mDidRetry;
        private bool mDidQuit;
        private bool mRaceEnabled;
        private bool mMontyShouldMutiny;
        private bool mResettingScene;
        private bool mPlayerDidChange;
        private bool mPlayerLoginChanged;

        private double mLaunchTimer; // Delays user interaction until we have fully launched
        private PfState mState;
        private PfState mPreviousState;
        private TutorialState mTutorialState;

        // Award flags (so we don't reward the player more than once per turn)
        private uint mPrizesBitmap;

        private PlayerIndex? mRestorableIndex;
        private StorageDialog mStorageDialog;
        private PlayfieldView mView;

        private ActorAi mActorBrains;
        private TownAi mGuvnor;
        private VoodooManager mVoodooManager;
        private MenuController mMenuController;

        // Physics
        private World mWorld;
        private ActorContactListener mContactListener;
        private double mStepDuration;
        private double mStepAccumulator;
        private float mTimeRatio;
        private Vector2 mGravity;
        private int mVelocityIterations;
        private int mPositionIterations;
        #endregion

        #region Properties
        public override bool TouchableDefault { get { return false; } }
        public override bool IsModallyBlocked { get { return mStorageDialog != null; } }
        public bool DidRetry { get { return mDidRetry; } }
        public bool DidQuit { get { return mDidQuit; } }
        public override int TopCategory { get { return (int)PFCat.HUD; } }
        public override int HelpCategory { get { return (int)PFCat.GUIDE; } }
        public override int PauseCategory { get { return (int)PFCat.SEA; } }
        public override uint AllPrizesBitmap { get { return PRIZE_STATS; } }
        public PfState State { get { return mState; } }
        public override GameMode GameMode
        {
            get { return base.GameMode; }
            set
            {
                if (State == PfState.Menu)
                    base.GameMode = value;
#if DEBUG
                else
                    throw new InvalidOperationException("Attempt to change PlayfieldController GameMode when State != PfState.Menu");
#endif
            }
        }
        public TutorialState TutState
        {
            get { return mTutorialState; }
            private set
            {
                if (mTutorialState != TutorialState.None && value == TutorialState.None)
                    PopFocusState(InputManager.FOCUS_STATE_PF_TUTORIAL);
                else if (mTutorialState == TutorialState.None && value != TutorialState.None)
                    PushFocusState(InputManager.FOCUS_STATE_PF_TUTORIAL);
                mTutorialState = value;
            }
        }
        public bool RaceEnabled
        {
            get { return mRaceEnabled; }
            set
            {
                mRaceEnabled = (value) ? (mState != PfState.Playing) : false;
    
                if (mRaceEnabled)
                {
                    GameController.GC.GameStats.ShipName = "Speedboat";
                    GameMode = GameMode.Career;
                    SetState(PfState.Playing);
                }
                else
                {
                    GameController.GC.GameStats.ShipName = "Man o' War";
                }
            }
        }
        public World World { get { return mWorld; } }
        public TownAi Guvnor { get { return mGuvnor; } }
        public ActorAi ActorBrains { get { return mActorBrains; } }
        public VoodooManager VoodooManager { get { return mVoodooManager; } }
        #endregion

        #region Methods
        public override void SetupController()
        {
            if (mView != null)
                return;

            LoadContent();
            base.SetupController();

            Actor.ActorsScene = this;
            SetupCaches();
            AchievementManager.AchPotShotDistSq *= ViewWidth / 1280f;
            mCustomDrawer = new CustomDrawer();
            Juggler.AddObject(mCustomDrawer);

            mCustomHudDrawer = new CustomDrawer();
            HudJuggler.AddObject(mCustomHudDrawer);
            PauseJuggler.AddObject(mCustomHudDrawer);

            GameController gc = GameController.GC;
            mSpriteLayerManager = new SpriteLayerManager(mBaseSprite, 23);
            mSpriteLayerManager.ChildAtCategory((int)PFCat.BLOOD).Effecter = new SPEffecter(EffectForKey("Refraction"), gc.BloodDraw);
            mSpriteLayerManager.ChildAtCategory((int)PFCat.POOLS).Effecter = new SPEffecter(EffectForKey("Refraction"), gc.PoolDraw);

            SetupGamerPic();
            gc.ProfileManager.RefreshGamerPictures(TextureByName("gamer-picture"));

            List<int> touchableLayers = new List<int>() { (int)PFCat.SEA, (int)PFCat.SURFACE, (int)PFCat.BUILDINGS, (int)PFCat.DIALOGS, (int)PFCat.DECK, (int)PFCat.HUD };
            mSpriteLayerManager.SetTouchableLayers(touchableLayers);

            SetupTownAi();
            SetupActorAi();

            mView = new PlayfieldView(this);
            mView.SetupView();

            ObjectivesManager.SetScene(this);

            mMenuController = new MenuController(this);
            mMenuController.SetupController();

            mVoodooManager = new AwesomePirates.VoodooManager(-1, gc.GameStats.Trinkets, gc.GameStats.Gadgets);
            SetupVoodooManagerListeners();
            gc.TimeKeeper.DayShouldIncrease = true;
            AchievementManager.LoadCombatTextWithCategory((int)PFCat.COMBAT_TEXT, 40, mSceneKey);
            SKManager.LoadCombatTextWithCategory((int)PFCat.COMBAT_TEXT, 40);

            SetState(PfState.Title);
            Flip(true);

            SetupReusables();
        }

        private void LoadContent()
        {
            GameController gc = GameController.GC;

            //TM.AddAtlas("playfield-atlas.xml");
            //TM.AddAtlas("objectives-atlas.xml");
            //TM.AddAtlas("achievements-atlas.xml");
            //TM.AddAtlas("ghost-railing-atlas.xml");
            //TM.AddAtlas("7-Man-o'-War-railing-atlas.xml");
            //TM.AddAtlas("8-Speedboat-railing-atlas.xml");
            //TM.AddAtlas("refraction-atlas.xml");
            //TM.AddAtlas("refraction-sml-atlas.xml");
            //TM.AddAtlas("refractables-atlas.xml");
            //TM.AddAtlas("fancy-text-atlas.xml");
            //TM.AddAtlas("help-atlas.xml");
            //TM.AddAtlas("gameover-atlas.xml");
            //TM.AddAtlas("uiview-atlas.xml");
            //TM.AddAtlas("controls-atlas.xml");
            //TM.AddAtlas("title-atlas.xml");
            //TM.AddAtlas("masteries-atlas.xml");
            //TM.AddAtlas("skirmish-atlas.xml");

            //for (int i = 0; i < 4; ++i)
            //    TM.AddAtlas("ocean" + i + "_N-atlas.xml");

            //mEffects = new Dictionary<string, Effect>()
            //{
            //    { "OceanShader", gc.Content.Load<Effect>("Effects/OceanShader") },
            //    { "SkyShader", gc.Content.Load<Effect>("Effects/SkyShader") },
            //    { "Refraction", gc.Content.Load<Effect>("Effects/Refraction") },
            //    { "Highlight", gc.Content.Load<Effect>("Effects/Highlight") },
            //    { "Potion", gc.Content.Load<Effect>("Effects/Potion") },
            //    { "AggregatePotion", gc.Content.Load<Effect>("Effects/AggregatePotion") }
            //};
        }

        private void SetupReusables()
        {
            PointMovie.SetupReusables();
            Wake.SetupReusables();
            SharkWater.SetupReusables();
            ShipHitGlow.SetupReusables();
            OverboardActor.SetupReusables();
            Shark.SetupReusables();
            Destination.SetupReusables();
            CannonballImpactLog.SetupReusables();
            CannonballGroup.SetupReusables();
            CannonballInfamyBonus.SetupReusables();
            Cannonball.SetupReusables();
            CannonDetails.SetupReusables();
            ShipDetails.SetupReusables();
            ShipActor.SetupReusables();
            PoolActor.SetupReusables();
            PowderKegActor.SetupReusables();
            AshPickupActor.SetupReusables();
            SKPupActor.SetupReusables();
            HandOfDavy.SetupReusables();
            TempestActor.SetupReusables();
            BrandySlickActor.SetupReusables();
            NetActor.SetupReusables();
            BlastProp.SetupReusables();
            LootProp.SetupReusables();
            SpynDoctor.TopScoreEntry.SetupReusables();
        }

        protected override void SetupCaches()
        {
            if (mCacheManagers != null)
                return;

            GameController gc = GameController.GC;

            mCacheManagers = gc.CachedResourceForKey(RESOURCE_CACHE_MANAGERS) as Dictionary<uint, CacheManager>;

            if (mCacheManagers != null)
            {
                foreach (KeyValuePair<uint, CacheManager> kvp in mCacheManagers)
                    kvp.Value.ReassignResourceServersToScene(this);
                return;
            }

            mCacheManagers = new Dictionary<uint, CacheManager>()
            {
                { CacheManager.CACHE_CANNONBALL, new CannonballCache() },
                { CacheManager.CACHE_LOOT_PROP, new LootPropCache() },
                { CacheManager.CACHE_NPC_SHIP, new NpcShipCache() },
                { CacheManager.CACHE_POINT_MOVIE, new PointMovieCache() },
                { CacheManager.CACHE_SHARK, new SharkCache() },
                { CacheManager.CACHE_POOL_ACTOR, new PoolActorCache() },
                { CacheManager.CACHE_BLAST_PROP, new BlastCache() },
                { CacheManager.CACHE_WAKE, new WakeCache() },
                { CacheManager.CACHE_MISC, new MiscCache() }
            };

            foreach (KeyValuePair<uint, CacheManager> kvp in mCacheManagers)
            {
                if (kvp.Value is CannonballCache)
                {
                    CannonballCache cache = kvp.Value as CannonballCache; 
                    List<string> shotTypes = Ash.AllTexturePrefixes;
                    cache.FillResourcePoolForSceneWithShotTypes(this, shotTypes);
                }
                else
                    kvp.Value.FillResourcePoolForScene(this);
            }

            gc.CacheResourceForKey(mCacheManagers, RESOURCE_CACHE_MANAGERS);
        }

        protected override void SetupSaveOptions()
        {
            if (AchievementManager != null)
            {
                AchievementManager.ProcessDelayedSaves();
                AchievementManager.DelaySavingAchievements = true;
            }
        }

        protected override void GameDeactivated(object sender, EventArgs e)
        {
            base.GameDeactivated(sender, e);

            if (mMenuController != null)
                mMenuController.GoToMainMenu();
        }

        protected void SetState(PfState state)
        {
            if (state == mState)
                return;
            mPreviousState = mState;
            EnableSlowedTime(false);
    
            GameController gc = GameController.GC;
    
            // Clean up previous state
            switch (mPreviousState)
            {
                case PfState.Hibernating:
                    break;
                case PfState.Title:
                    PopFocusState(InputManager.FOCUS_STATE_TITLE);
                    break;
                case PfState.Menu:
                    PopFocusState(InputManager.FOCUS_STATE_MENU);
                    break;
                case PfState.Playing:
                    PopFocusState(InputManager.FOCUS_STATE_PF_HELP);
                    PopFocusState(InputManager.FOCUS_STATE_PF_PLAYFIELD);
                    ShowPauseButton(false);
                    EnableSuspendedSceneMode(false);
                    EnableSuspendedPlayerMode(false);
            
                    TutState = TutorialState.None;

                    SKManager.GameCountdown = AwesomePirates.SKManager.SKGameCountdown.None;
                    mView.SKGameCountdownCancelled();
                    mView.HideSKTutorialView();
                    mView.DismissTutorial();
                    mView.EnableWeather(true);

                    gc.ThisTurn.IsGameOver = true;
                    gc.TimeKeeper.DayShouldIncrease = false;

                    mVoodooManager.PrepareForGameOver();
                    mGuvnor.PrepareForGameOver();
                    mActorBrains.PrepareForGameOver();
                    ActorAi.SetupAiKnob(gc.AiKnob);
                    break;
                case PfState.EndOfTurn:
                    PopFocusState(InputManager.FOCUS_STATE_PF_GAMEOVER);
                    PopFocusState(InputManager.FOCUS_STATE_PF_SK_GAMEOVER);
                    break;
                case PfState.DelayedRetry:
                    break;
                case PfState.DelayedQuit:
                    break;
                default:
                    break;
            }
    
            mState = state;
    
            // Apply new state
            switch (mState)
            {
                case PfState.Hibernating:
                    break;
                case PfState.Title:
                    PushFocusState(InputManager.FOCUS_STATE_TITLE);
                    mMenuController.State = MenuController.MenuState.Title;
                    break;
                case PfState.Menu:
                    StopAmbientSounds();
                    HideSKTallyView();
                    mMenuController.State = MenuController.MenuState.TransitionIn;
                    mView.TransitionToMenu();
                    mActorBrains.ShipsPaused = false;
                    break;
                case PfState.Playing:
                    PushFocusState(InputManager.FOCUS_STATE_PF_PLAYFIELD);
                    ShowPauseButton(true);
                    PrepareForNewGame();
                    break;
                case PfState.EndOfTurn:
                    gc.ProfileManager.UpdatePresenceModeForPlayer(GamerPresenceMode.GameOver,
                        GameMode == AwesomePirates.GameMode.Career ? ControlsManager.CM.MainPlayerIndex : null);
                    break;
                case PfState.DelayedRetry:
                    mView.DestroyPlayerShip();
                    mView.EnableCombatInterface(false);
                    break;
                case PfState.DelayedQuit:
                    mView.DestroyPlayerShip();
                    mView.EnableCombatInterface(false);
                    break;
                default:
                    break;
            }
        }

        private void PrepareForNewGame()
        {
            GameController gc = GameController.GC;
    
            foreach (Actor actor in mActors)
                actor.PrepareForNewGame();

            gc.PrepareForNewGame();
            gc.TimeKeeper.DayShouldIncrease = GameMode == AwesomePirates.GameMode.Career && !mRaceEnabled;

            if (GameMode != AwesomePirates.GameMode.Career)
                SKManager.BeginGame(GameMode);
            AchievementManager.GameMode = GameMode;

            SetupActorAi();
            SetupTownAi();
            mView.TransitionFromMenu();
            mVoodooManager.PrepareForNewGame();
            mMenuController.State = MenuController.MenuState.TransitionOut;
            mPrizesBitmap = 0;
            mStepAccumulator = 0;
            mDidRetry = mDidQuit = false;
            mMontyShouldMutiny = false;
            EnableSlowedTime(false);
            mAmbienceShouldPlay = true;

            if (mPlayerDidChange)
            {
                AchievementManager.ResetCombatTextCache();
                Cannonball.PurgeReusables();
                Cannonball.SetupReusables();
                mPlayerDidChange = false;
            }

            if (GameMode == AwesomePirates.GameMode.Career)
            {
                // Tutorial
                TutState = IntendedTutorialState;

                if (mTutorialState != TutorialState.None)
                {
                    EnableSuspendedSceneMode(true);
                    mJuggler.DelayInvocation(this, 1f, BeginTutorial);
                }
                else
                {
                    if (mActorBrains != null)
                        mActorBrains.EnableSuspendedMode(false);
                    mView.CreatePlayerShip();
                }

                if (gc.LiveLeaderboard != null)
                {
                    gc.LiveLeaderboard.GoSlow(true);
                    if (gc.LiveLeaderboard.IsStopped)
                        gc.LiveLeaderboard.TryStart();
                }
                gc.ProfileManager.UpdatePresenceModeForPlayer(GamerPresenceMode.SinglePlayer);

#if IOS_SCREENS
                for (int i = 0; i < 6; ++i)
                    gc.PlayerShip.AddRandomPrisoner();
#endif
            }
            else
            {
                SKManager.GameCountdown = AwesomePirates.SKManager.SKGameCountdown.PreGame;

                if (mActorBrains != null)
                    mActorBrains.EnableSuspendedMode(true);

                ControlsManager.CM.SetDefaultPlayerIndex(null);
                mView.DisplaySKTutorialView();

                if (gc.LiveLeaderboard != null)
                {
                    gc.LiveLeaderboard.GoSlow(true);
                    if (gc.LiveLeaderboard.IsRunning)
                        gc.LiveLeaderboard.Stop(false);
                }
            }

            System.GC.Collect();
            gc.ResetElapsedTime();
        }

        public override void WillGainSceneFocus()
        {
            base.WillGainSceneFocus();
            Actor.ActorsScene = this;
            AttachEventListeners();
            GameController.GC.TimeKeeper.TimerActive = !mSuspendedMode;
        }

        public override void WillLoseSceneFocus()
        {
            base.WillLoseSceneFocus();
            DetachEventListeners();
            GameController.GC.TimeKeeper.TimerActive = false;
        }

        public override void AttachEventListeners()
        {
            base.AttachEventListeners();

            GameController gc = GameController.GC;

            gc.ThisTurn.AddEventListener(ThisTurn.CUST_EVENT_TYPE_INFAMY_VALUE_CHANGED, (NumericValueChangedEventHandler)OnInfamyChanged);
            gc.ThisTurn.AddEventListener(ThisTurn.CUST_EVENT_TYPE_INFAMY_VALUE_CHANGED, (NumericValueChangedEventHandler)AchievementManager.OnInfamyChanged);
            gc.ThisTurn.AddEventListener(ThisTurn.CUST_EVENT_TYPE_MUTINY_VALUE_CHANGED, (NumericRatioChangedEventHandler)OnMutinyChanged);
            //gc.TimeKeeper.AddEventListener(TimeOfDayChangedEvent.CUST_EVENT_TYPE_TIME_OF_DAY_CHANGED, (TimeOfDayChangedEventHandler)OnTimeOfDayChangedEvent);
            gc.TimeKeeper.Subscribe(new Action<TimeOfDayChangedEvent>(OnTimeOfDayChangedEvent));

            mVoodooManager.AddActionEventListener(VoodooWheel.CUST_EVENT_TYPE_VOODOO_MENU_CLOSING, new Action<SPEvent>(OnVoodooMenuClosing));
            AchievementManager.AddActionEventListener(AchievementManager.CUST_EVENT_TYPE_PLAYER_EATEN, new Action<SPEvent>(OnPlayerEaten));
            ObjectivesManager.AddEventListener(ObjectivesManager.CUST_EVENT_TYPE_OBJECTIVES_RANKUP_COMPLETED, (BinaryEventHandler)OnObjectivesRankupCompleted);
            mMenuController.AddActionEventListener(MenuController.CUST_EVENT_TYPE_MENU_PLAY_SHOULD_BEGIN, new Action<SPEvent>(OnPlayPressed));

            SKManager.AddActionEventListener(SKManager.CUST_EVENT_TYPE_SK_PRE_GAME_COUNTDOWN_STARTED, new Action<SPEvent>(OnSKGameCountdownStarted));
            SKManager.AddActionEventListener(SKManager.CUST_EVENT_TYPE_SK_PRE_GAME_COUNTDOWN_COMPLETED, new Action<SPEvent>(OnSKGameCountdownCompleted));
            SKManager.AddActionEventListener(SKManager.CUST_EVENT_TYPE_SK_FINALE_COUNTDOWN_STARTED, new Action<SPEvent>(OnSKGameCountdownStarted));
            SKManager.AddActionEventListener(SKManager.CUST_EVENT_TYPE_SK_FINALE_COUNTDOWN_COMPLETED, new Action<SPEvent>(OnSKGameCountdownCompleted));
            SKManager.AddActionEventListener(SKManager.CUST_EVENT_TYPE_SK_GAME_OVER, new Action<SPEvent>(OnSKGameOver));
            SKManager.AddEventListener(SKManager.CUST_EVENT_TYPE_SK_PLAYER_SHIP_SINKING, (PlayerIndexEventHandler)OnSKPlayerShipSinking);

            mView.AttachEventListeners();
            mMenuController.AttachEventListeners();
        }

        public override void DetachEventListeners()
        {
            base.DetachEventListeners();

            GameController gc = GameController.GC;

            gc.ThisTurn.RemoveEventListener(ThisTurn.CUST_EVENT_TYPE_INFAMY_VALUE_CHANGED, (NumericValueChangedEventHandler)OnInfamyChanged);
            gc.ThisTurn.RemoveEventListener(ThisTurn.CUST_EVENT_TYPE_INFAMY_VALUE_CHANGED, (NumericValueChangedEventHandler)AchievementManager.OnInfamyChanged);
            gc.ThisTurn.RemoveEventListener(ThisTurn.CUST_EVENT_TYPE_MUTINY_VALUE_CHANGED, (NumericRatioChangedEventHandler)OnMutinyChanged);
            //gc.TimeKeeper.RemoveEventListener(TimeOfDayChangedEvent.CUST_EVENT_TYPE_TIME_OF_DAY_CHANGED, (TimeOfDayChangedEventHandler)OnTimeOfDayChangedEvent);
            gc.TimeKeeper.Unsubscibe(this);

            if (mVoodooManager != null)
                mVoodooManager.RemoveEventListener(VoodooWheel.CUST_EVENT_TYPE_VOODOO_MENU_CLOSING);
            if (AchievementManager != null)
                AchievementManager.RemoveEventListener(AchievementManager.CUST_EVENT_TYPE_PLAYER_EATEN);
            if (ObjectivesManager != null)
                ObjectivesManager.RemoveEventListener(ObjectivesManager.CUST_EVENT_TYPE_OBJECTIVES_RANKUP_COMPLETED, (BinaryEventHandler)OnObjectivesRankupCompleted);
            if (mMenuController != null)
            {
                mMenuController.RemoveEventListener(MenuController.CUST_EVENT_TYPE_MENU_PLAY_SHOULD_BEGIN);
                mMenuController.DetachEventListeners();
            }

            SKManager.RemoveEventListener(SKManager.CUST_EVENT_TYPE_SK_PRE_GAME_COUNTDOWN_STARTED);
            SKManager.RemoveEventListener(SKManager.CUST_EVENT_TYPE_SK_PRE_GAME_COUNTDOWN_COMPLETED);
            SKManager.RemoveEventListener(SKManager.CUST_EVENT_TYPE_SK_FINALE_COUNTDOWN_STARTED);
            SKManager.RemoveEventListener(SKManager.CUST_EVENT_TYPE_SK_FINALE_COUNTDOWN_COMPLETED);
            SKManager.RemoveEventListener(SKManager.CUST_EVENT_TYPE_SK_GAME_OVER);
            SKManager.RemoveEventListener(SKManager.CUST_EVENT_TYPE_SK_PLAYER_SHIP_SINKING, (PlayerIndexEventHandler)OnSKPlayerShipSinking);

            if (mView != null)
                mView.DetachEventListeners();
        }

        public override void ApplyGameSettings()
        {
            base.ApplyGameSettings();

            if (mMenuController != null)
                mMenuController.ApplyGameSettings();

            if (IsMusicMuted)
                PauseSound("Music");
        }

        protected void TryStartLiveLeaderboard()
        {
            if (State != PfState.Playing || GameMode == AwesomePirates.GameMode.Career)
            {
                GameController gc = GameController.GC;
                if (gc.LiveLeaderboard != null && gc.LiveLeaderboard.IsStopped)
                    gc.LiveLeaderboard.TryStart();
            }
        }

        public override void TrialModeDidChange(bool isTrial)
        {
            base.TrialModeDidChange(isTrial);

            if (mMenuController != null)
                mMenuController.TrialModeDidChange(isTrial);
            TryStartLiveLeaderboard();
        }

        public override void OnlineScoresStopped()
        {
            base.OnlineScoresStopped();
            TryStartLiveLeaderboard();
        }

        public override void SplashScreenDidHide()
        {
            base.SplashScreenDidHide();

            if (mMenuController != null)
                mMenuController.SplashScreenDidHide();
        }

        public override void SaveWillCommence()
        {
            base.SaveWillCommence();

            if (mMenuController != null)
                mMenuController.SaveWillCommence();
        }

        public override void OnGamerPicsRefreshed(SPEvent ev)
        {
            base.OnGamerPicsRefreshed(ev);

            if (mStorageDialog != null)
                mStorageDialog.OnGamerPicsRefreshed(ev);
            if (mMenuController != null)
                mMenuController.OnGamerPicsRefreshed(ev);
        }

        public override void PlayerSaveDeviceSelected(PlayerIndex playerIndex)
        {
            base.PlayerSaveDeviceSelected(playerIndex);

            if (mMenuController != null)
                mMenuController.RefreshHiScoreView();
        }

        public override void LocalLoadFailed(PlayerIndex playerIndex)
        {
            Debug.Assert(mStorageDialog == null, "LocalLoadFailed but StorageDialog is not null. Should never be more than one StorageDialog required concurrently.");
            if (mStorageDialog != null)
                return;

            DisplayStorageDialog(StorageDialog.StorageDialogType.Load, playerIndex);
        }

        public override void LocalSaveFailed(PlayerIndex playerIndex)
        {
            Debug.Assert(mStorageDialog == null, "LocalSaveFailed but StorageDialog is not null. Should never be more than one StorageDialog required concurrently.");
            if (mStorageDialog != null)
                return;

            FileManager.FM.DestroyPlayerSaveDevice(playerIndex);
            FileManager.FM.AddSaveDeviceForPlayer(playerIndex);

            if (mMenuController != null)
                mMenuController.LocalSaveFailed(playerIndex);

            if (FileManager.FM.RequestRepeatedQueuedSave())
                DisplayStorageDialog(StorageDialog.StorageDialogType.Save, playerIndex);
        }

        private void DisplayStorageDialog(StorageDialog.StorageDialogType dialogType, PlayerIndex playerIndex)
        {
            if (mStorageDialog != null)
                return;

            DisplayPauseMenu();

            if (ControlsManager.CM.DefaultPlayerIndex.HasValue)
                mRestorableIndex = ControlsManager.CM.DefaultPlayerIndex.Value;
            ControlsManager.CM.SetDefaultPlayerIndex(playerIndex);
            GameController.GC.ProfileManager.RefreshGamerPictures(TextureByName("gamer-picture"));

            mStorageDialog = new StorageDialog(HelpCategory, dialogType, playerIndex);
            mStorageDialog.Y = -(50 + ResManager.RESM.ItemOffsetWithAlignment(ResManager.ResAlignment.Center).Y);
            mStorageDialog.AddEventListener(StorageDialog.CUST_EVENT_TYPE_STORAGE_DIALOG_COMPLETED, (PlayerIndexEventHandler)OnStorageDialogCompleted);
            mStorageDialog.AddEventListener(StorageDialog.CUST_EVENT_TYPE_STORAGE_DIALOG_CANCELLED, (PlayerIndexEventHandler)OnStorageDialogCancelled);
            PushFocusState(InputManager.FOCUS_STATE_SYS_STORAGE, true);
            AddProp(mStorageDialog);

            if (mMenuController != null)
                mMenuController.StorageDialogDidShow();
        }

        private void HideStorageDialog()
        {
            if (mStorageDialog == null)
                return;

            ControlsManager.CM.SetDefaultPlayerIndex(mRestorableIndex);
            mRestorableIndex = null;

            if (mStorageDialog.DialogType == StorageDialog.StorageDialogType.Save)
                FileManager.FM.ConfirmRepeatedQueuedSaveRequest(false);
            RemoveProp(mStorageDialog);
            mStorageDialog = null;
            PopFocusState(InputManager.FOCUS_STATE_SYS_STORAGE, true);

            if (mMenuController != null)
                mMenuController.StorageDialogDidHide();
        }

        private void OnStorageDialogCompleted(PlayerIndexEvent ev)
        {
            HideStorageDialog();
        }

        private void OnStorageDialogCancelled(PlayerIndexEvent ev)
        {
            if (ev != null && mStorageDialog != null)
                GameController.GC.ProfileManager.PlayerSaveDeviceCancelled(ev.PlayerIndex);

            HideStorageDialog();
        }

        public override void PlayerChanged()
        {
            base.PlayerChanged();

            if (mMenuController != null)
                mMenuController.PlayerChanged();
            mPlayerDidChange = true;
        }

        public override void PlayerLoggedIn(PlayerIndex playerIndex)
        {
            PlayerLoginChanged(playerIndex);
            TryStartLiveLeaderboard();

            if (mMenuController != null)
                mMenuController.PlayerLoggedIn(playerIndex);
        }

        public override void PlayerLoggedOut(PlayerIndex playerIndex)
        {
            PlayerLoginChanged(playerIndex);

            if (mMenuController != null)
                mMenuController.PlayerLoggedOut(playerIndex);
        }

        private void PlayerLoginChanged(PlayerIndex playerIndex)
        {
            if (MainPlayerIndex != playerIndex)
                return;

            if (mState == PfState.Menu)
            {
                if (mMenuController != null)
                    mMenuController.GoToMainMenu();
            }
            else if (mState == PfState.Playing && GameMode == AwesomePirates.GameMode.Career)
                mPlayerLoginChanged = true;
        }

        public override void ControllerEngaged(PlayerIndex playerIndex)
        {
            TryStartLiveLeaderboard();
        }

        public override void DefaultControllerDisconnected()
        {
            base.DefaultControllerDisconnected();

            if (mState == PfState.Menu)
            {
                if (mMenuController != null && ControlsManager.CM.DefaultPlayerIndex.HasValue)
                    mMenuController.GoToMainMenu();
                HideExitView();
            }
            else if (mState == PfState.Playing)
            {
                ControlsManager.CM.MainPlayerIndex = null;
                DisplayPauseMenu();
            }

            if (mStorageDialog != null)
            {
                if (mState == PfState.Menu)
                    mRestorableIndex = null;
                HideStorageDialog();
            }
        }

        public override void PlayAmbientSounds()
        {
            if (mState != PfState.Playing)
                return;
            //float volume = 1.0f;
	        string key = null;
	
	        if (mRaceEnabled)
		        key = "Engine";
	        else
		        key = "Ambience";

            PlaySound(key);
            //AudioPlayer.PlaySoundWithKey(key, volume, 2f);
        }

        public void StopAmbientSounds()
        {
            string key = null;
	
	        if (mRaceEnabled)
		        key = "Engine";
	        else
		        key = "Ambience";

            StopSound(key, mDidRetry ?
                Microsoft.Xna.Framework.Audio.AudioStopOptions.Immediate :
                Microsoft.Xna.Framework.Audio.AudioStopOptions.AsAuthored);
        }

        public override void Flip(bool enable)
        {
            base.Flip(enable);

            VoodooManager.Flip(enable);
            AchievementManager.Flip(enable);
            ObjectivesManager.Flip(enable);
            mView.Flip(enable);

            foreach (Prop prop in mProps)
                prop.Flip(enable);
            foreach (Actor actor in mActors)
                actor.Flip(enable);
            for (int category = (int)PFCat.SEA; category < (int)PFCat.HUD; ++category)
            {
                if (category == (int)PFCat.SK_HUD || category == (int)PFCat.DIALOGS || category == (int)PFCat.COMBAT_TEXT)
                    continue;
                SpriteLayerManager.FlipChild(enable, category, ViewWidth);
            }
        }

        public override void EnableScreenshotMode(bool enable)
        {
            if (mView != null)
                mView.EnableScreenshotMode(enable);
        }

        public void AdoptWaterColor(SPQuad quad)
        {
            if (mView != null)
                mView.AdoptWaterColor(quad);
        }

        private TutorialState IntendedTutorialState
        {
            get
            {
                if (RaceEnabled)
                    return TutorialState.None;

                TutorialState state = TutorialState.None;
                GameSettings gs = GameSettings.GS;

                if (!gs.SettingForKey(GameSettings.DONE_TUTORIAL))
                    state = TutorialState.Primary;
                else if (!gs.SettingForKey(GameSettings.DONE_TUTORIAL2))
                    state = TutorialState.Secondary;
                else if (!gs.SettingForKey(GameSettings.DONE_TUTORIAL3))
                    state = TutorialState.Tertiary;
                else if (ObjectivesManager != null && ObjectivesManager.Rank >= 3 && !gs.SettingForKey(GameSettings.DONE_TUTORIAL4))
                    state = TutorialState.Quaternary;
                else if (ObjectivesManager != null && ObjectivesManager.Rank >= 10 && !gs.SettingForKey(GameSettings.DONE_TUTORIAL5))
                    state = TutorialState.Quinary;

                return state;
            }
        }

        private string TutorialKeyForState(TutorialState state)
        {
            string key = null;

            switch (state)
            {
                case TutorialState.Primary: key = "Primary"; break;
                case TutorialState.Secondary: key = "Secondary"; break;
                case TutorialState.Tertiary: key = "Tertiary"; break;
                case TutorialState.Quaternary: key = "Quaternary"; break;
                case TutorialState.Quinary: key = "Quinary"; break;
                case TutorialState.None: break;
            }

            return key;
        }

        private string TutorialSettingForState(TutorialState state)
        {
            string key = null;

            switch (state)
            {
                case TutorialState.Primary: key = GameSettings.DONE_TUTORIAL; break;
                case TutorialState.Secondary: key = GameSettings.DONE_TUTORIAL2; break;
                case TutorialState.Tertiary: key = GameSettings.DONE_TUTORIAL3; break;
                case TutorialState.Quaternary: key = GameSettings.DONE_TUTORIAL4; break;
                case TutorialState.Quinary: key = GameSettings.DONE_TUTORIAL5; break;
                case TutorialState.None: break;
            }

            return key;
        }

        public void BeginTutorial()
        {
            if (mTutorialState == TutorialState.None)
                return;

            mView.DisplayTutorialForKey(TutorialKeyForState(TutState), 0, -1);
            EnableSuspendedPlayerMode(true);
            mVoodooManager.BubbleMenuToTop();
        }

        public void FinishTutorial()
        {
            mView.DismissTutorial();
	
	        GameController gc = GameController.GC;
            string settingKey = TutorialSettingForState(TutState);
    
            if (settingKey != null)
                GameSettings.GS.SetSettingForKey(settingKey, true);

            EnableSuspendedSceneMode(false);
            EnableSuspendedPlayerMode(false);
            mView.CreatePlayerShip();
    
            if (mTutorialState == TutorialState.Primary && !GameSettings.GS.SettingForKey(GameSettings.PLAYER_SHIP_TIPS))
            {
                GameSettings.GS.SetSettingForKey(GameSettings.PLAYER_SHIP_TIPS, true);
                PlayerShip playerShip = gc.PlayerShip;
        
                if (playerShip != null)
                {
                    mView.DisplayHintByName(GameSettings.PLAYER_SHIP_TIPS, gc.PlayerShip.X, gc.PlayerShip.Y, 0.75f * gc.PlayerShip.Height, gc.PlayerShip, false);
                    Juggler.DelayInvocation(this, 10f, delegate
                    {
                        if (mView != null)
                            mView.HideHintByName(GameSettings.PLAYER_SHIP_TIPS);
                    });
                }
            }
    
            GameSettings.GS.SaveSettings();
            TutState = TutorialState.None;
        }

        public override void DisplayTickerHint(string text)
        {
            if (text != null)
                AddProp(new HintTicker((int)PFCat.DIALOGS, text));
        }

        public override void DisplayHintByName(string name, float x, float y, float radius, SPDisplayObject target, bool exclusive)
        {
            if (name != null && mView != null)
                mView.DisplayHintByName(name, x, y, radius, target, exclusive);
        }

        public override void HideHintByName(string name)
        {
            if (name != null && mView != null)
                mView.HideHintByName(name);
        }

        public override int ObjectivesCategoryForViewType(ObjectivesView.ViewType type)
        {
            int category = 0;

            switch (type)
            {
                case ObjectivesView.ViewType.View: category = (int)PFCat.DIALOGS; break;
                case ObjectivesView.ViewType.Completed: category = (int)PFCat.SURFACE; break;
                case ObjectivesView.ViewType.Current: category = (int)PFCat.HUD; break;
                default: break;
            }

            return category;
        }

        public override void RequestTargetForPursuer(IPursuer pursuer)
        {
            if (mActorBrains != null)
                mActorBrains.RequestTargetForPursuer(pursuer);
        }

        public void ActorArrivedAtDestination(Actor actor)
        {
            if (mActorBrains != null)
                mActorBrains.ActorArrivedAtDestination(actor);
        }

        public void ActorDepartedPort(Actor actor)
        {
            if (mActorBrains != null)
                mActorBrains.ActorDepartedPort(actor);
        }

        public void PrisonerOverboard(Prisoner prisoner, ShipActor ship)
        {
            if (mActorBrains == null)
                return;

            GameController gc = GameController.GC;
            mActorBrains.PrisonerOverboard(prisoner, ship);

            if (GameMode == AwesomePirates.GameMode.Career)
            {
                if (prisoner != null && ship == null)
                {
                    AchievementManager.PrisonerPushedOverboard();
                    gc.PlayerShip.ShipDetails.PrisonerPushedOverboard(prisoner);
                }

                if (prisoner != null && ship == null)
                {
                    if (!GameSettings.GS.SettingForKey(GameSettings.PLANKING_TIPS))
                        GameSettings.GS.SetSettingForKey(GameSettings.PLANKING_TIPS, true);
                    HideHintByName(GameSettings.PLANKING_TIPS);
                }
            }
        }

        public override void AdvanceTime(double time)
        {
            if (SPMacros.SP_IS_FLOAT_EQUAL((float)time, 0f))
                return;

            GameController gc = GameController.GC;
            double slowedTime = (mTimeSlowed) ? time * TimeSlowedFactor : time;

            if (mState == PfState.Hibernating)
            {
                return;
            }

            if (mPlayerLoginChanged)
            {
                mPlayerLoginChanged = false;
                if (mState == PfState.Playing && GameMode == AwesomePirates.GameMode.Career)
                {
                    ControlsManager.CM.SetDefaultPlayerIndex(null);
                    DismissPauseMenu();
                    SetState(PfState.Menu);
                }
            }

            if (!mScenePaused)
            {
                mView.AdvanceFpsCounter(time);
                AchievementManager.FillCombatTextCache();

                // Step Physics (fixed time step)
                double stepIncrement = Math.Min(slowedTime, mStepDuration);
                mStepAccumulator += slowedTime;

                mLocked = true;
                do
                {
                    mWorld.Step((float)stepIncrement, mVelocityIterations, mPositionIterations);
                    //mWorld.ClearForces(); // Not required unless we're using a non-fixed timestep in Game.Update.
                    mStepAccumulator -= stepIncrement;

                    foreach (Actor actor in mActors)
                    {
                        if (!actor.MarkedForRemoval)
                            actor.RespondToPhysicalInputs();
                    }
                } while (mStepAccumulator >= stepIncrement);
                mLocked = false;

                mMenuController.AdvanceTime(time);
                gc.TimeKeeper.AdvanceTime(slowedTime);
                base.AdvanceTime(time);

                mActorBrains.AdvanceTime(slowedTime);
                mVoodooManager.AdvanceTime(slowedTime);
                mView.AdvanceTime(slowedTime);

                if (GameMode == AwesomePirates.GameMode.Career)
                {
                    mGuvnor.AdvanceTime(slowedTime);
                    CheckForMontysMutiny();
                }
                else
                    SKManager.AdvanceTime(time);
            }
            else
            {
                base.AdvanceTime(time);

                if (mStorageDialog != null)
                    mStorageDialog.AdvanceTime(time);
            }
        }

        private void CheckForMontysMutiny()
        {
            GameController gc = GameController.GC;
    
            if (gc.ThisTurn.AdvState == ThisTurn.AdventureState.StopShips && gc.PlayerShip.Monty == PlayerShip.MontyState.FirstMate)
            {
                if (mActorBrains.IsPlayfieldClearOfNpcShips)
                {
                    if (!gc.PlayerShip.IsPlankingEnqueued)
                    {
                        gc.PlayerShip.EnablePlank(false);
            
                        if (mActorBrains.IsPlayfieldClear)
                        {
                            mVoodooManager.PrepareForGameOver();
                            gc.PlayerShip.Monty = PlayerShip.MontyState.Skipper;
                        }
                    }
                }
            }
    
            if (mMontyShouldMutiny)
                BeginMontysMutinySequence();
        }

        private void BeginMontysMutinySequence()
        {
            GameController gc = GameController.GC;

            if (gc.PlayerShip == null)
                return;

            mActorBrains.EnactMontysMutiny();

            // Looks like the test is opposite because view is flipped.
            int dir = (gc.PlayerShip.X > ViewWidth / 2) ? 1 : -1;

            mView.DisplayFirstMateAlert(new List<string>()
            {
                "Man overboard!",
                "That's a most unfortunate accident you've had there, Cap'n!",
                "As First Mate, I've always warned that the deck can get slippery at this time of day.",
                "Hold on while I bring her around to pick you up.",
                "On second thought, I think Captain Montgomery has a nice ring to it, wouldn't you say?",
                "What goes around comes around...so long, swabby!"
            }, kFirstMateMutinied, dir, 2f);
    
            gc.ThisTurn.AdvState = ThisTurn.AdventureState.Overboard;
            mMontyShouldMutiny = false;
        }

        public override void RemoveActor(Actor actor, bool shouldDispose = true)
        {
            if (mActorBrains != null)
                mActorBrains.RemoveActor(actor);

            base.RemoveActor(actor, shouldDispose);
        }

        private void EnableSuspendedSceneMode(bool enable)
        {
            if (mSuspendedMode == enable)
                return;
            GameController.GC.TimeKeeper.TimerActive = !enable;

            if (mActorBrains != null)
                mActorBrains.EnableSuspendedMode(enable);
            if (mGuvnor != null)
                mGuvnor.EnableSuspendedMode(enable);
            if (mVoodooManager != null)
                mVoodooManager.EnableSuspendedMode(enable);
            AchievementManager.EnableSuspendedMode(enable);
            mSuspendedMode = enable;
        }

        private void EnableSuspendedPlayerMode(bool enable)
        {
            PlayerShip playerShip = GameController.GC.PlayerShip;

            if (playerShip != null)
                playerShip.EnableSuspendedMode(enable);
        }

        public SkirmishShip SkirmishShipForIndex(PlayerIndex playerIndex)
        {
            if (mView != null)
                return mView.SkirmishShipForIndex(playerIndex);
            else
                return null;
        }

        private void OnSKGameCountdownStarted(SPEvent ev)
        {
            if (mView != null)
                mView.SKGameCountdownStarted();
        }

        private void OnSKGameCountdownCompleted(SPEvent ev)
        {
            if (mView != null)
                mView.SKGameCountdownCompleted();

            if (SKManager.GameCountdown == AwesomePirates.SKManager.SKGameCountdown.PreGame)
            {
                if (mActorBrains != null)
                {
                    mActorBrains.EnableSuspendedMode(false);
                    mActorBrains.AdvanceAiKnobToState(25);
                }
            }
            else
            {
                if (mState == PfState.Playing)
                    PrepareForGameOver();
            }
        }

        public void OnBeginPressed(SPEvent ev)
        {
            if (!mSettingsApplied || mState != PfState.Title)
                return;
            PlaySound("Button");
            SetState(PfState.Menu);
#if XBOX
            PlayerIndex? playerIndex = ControlsManager.CM.PrevQueryPlayerIndex;
            ControlsManager.CM.ControllerDidEngage(playerIndex.Value);
            GameController.GC.ProfileManager.SwitchToPlayerIndex(playerIndex.Value);
#endif
        }

        public void OnPlayPressed(SPEvent ev)
        {
            GameController gc = GameController.GC;
            PlayerIndex? playerIndex = ControlsManager.CM.PrevQueryPlayerIndex;
            if (GameMode == AwesomePirates.GameMode.Career && playerIndex.HasValue && gc.ProfileManager.WouldSwitchToPlayerIndex(playerIndex.Value))
            {
                if (gc.AddDelayedCall(new Action(Play)))
                    gc.ProfileManager.SwitchToPlayerIndex(playerIndex.Value);
            }
            else
                Play();
        }

        public void Play()
        {
            if (mState == PfState.Playing)
                return;

            if (ReassignDefaultController())
            {
                RaceEnabled = false;
                SetState(PfState.Playing);
            }
        }

        protected void SetGameOver(bool value)
        {
            if (value)
                SetState(PfState.EndOfTurn);
            GameController.GC.ThisTurn.IsGameOver = value;
        }

        protected void SetupVoodooManagerListeners()
        {
            mVoodooManager.AddActionEventListener(VoodooManager.CUST_EVENT_TYPE_POWDER_KEG_DROPPING, new Action<SPEvent>(OnPowderKegDropping));
            mVoodooManager.AddActionEventListener(VoodooManager.CUST_EVENT_TYPE_NET_DEPLOYED, new Action<SPEvent>(OnNetDeployed));
            mVoodooManager.AddActionEventListener(VoodooManager.CUST_EVENT_TYPE_BRANDY_SLICK_DEPLOYED, new Action<SPEvent>(OnBrandySlickDeployed));
            mVoodooManager.AddActionEventListener(VoodooManager.CUST_EVENT_TYPE_TEMPEST_SUMMONED, new Action<SPEvent>(OnTempestSummoned));
            mVoodooManager.AddActionEventListener(VoodooManager.CUST_EVENT_TYPE_WHIRLPOOL_SUMMONED, new Action<SPEvent>(OnWhirlpoolSummoned));
            mVoodooManager.AddActionEventListener(VoodooManager.CUST_EVENT_TYPE_HAND_OF_DAVY_SUMMONED, new Action<SPEvent>(OnHandOfDavySummoned));
            mVoodooManager.AddActionEventListener(VoodooManager.CUST_EVENT_TYPE_CAMOUFLAGE_ACTIVATED, new Action<SPEvent>(OnCamouflageActivated));
            mVoodooManager.AddActionEventListener(VoodooManager.CUST_EVENT_TYPE_FLYING_DUTCHMAN_ACTIVATED, new Action<SPEvent>(OnFlyingDutchmanActivated));
            mVoodooManager.AddActionEventListener(VoodooManager.CUST_EVENT_TYPE_SEA_OF_LAVA_SUMMONED, new Action<SPEvent>(OnSeaOfLavaSummoned));
        }

        protected void RemoveVoodooManagerListeners()
        {
            mVoodooManager.RemoveEventListener(VoodooManager.CUST_EVENT_TYPE_POWDER_KEG_DROPPING);
            mVoodooManager.RemoveEventListener(VoodooManager.CUST_EVENT_TYPE_NET_DEPLOYED);
            mVoodooManager.RemoveEventListener(VoodooManager.CUST_EVENT_TYPE_BRANDY_SLICK_DEPLOYED);
            mVoodooManager.RemoveEventListener(VoodooManager.CUST_EVENT_TYPE_TEMPEST_SUMMONED);
            mVoodooManager.RemoveEventListener(VoodooManager.CUST_EVENT_TYPE_WHIRLPOOL_SUMMONED);
            mVoodooManager.RemoveEventListener(VoodooManager.CUST_EVENT_TYPE_HAND_OF_DAVY_SUMMONED);
            mVoodooManager.RemoveEventListener(VoodooManager.CUST_EVENT_TYPE_CAMOUFLAGE_ACTIVATED);
            mVoodooManager.RemoveEventListener(VoodooManager.CUST_EVENT_TYPE_FLYING_DUTCHMAN_ACTIVATED);
            mVoodooManager.RemoveEventListener(VoodooManager.CUST_EVENT_TYPE_SEA_OF_LAVA_SUMMONED);
        }

        public override void AwardPrizes(uint prizes)
        {
            if (GameMode != AwesomePirates.GameMode.Career)
                return;

            GameController gc = GameController.GC;
    
            if ((prizes & PRIZE_STATS) == PRIZE_STATS && (mPrizesBitmap & PRIZE_STATS) != PRIZE_STATS)
            {
                gc.ThisTurn.DaysAtSea = Math.Max(0,((int)gc.TimeKeeper.Day)-1) + gc.TimeKeeper.TimePassedToday / TimeKeeper.TimePerDay;
                gc.ThisTurn.CommitStats();
            }

            mPrizesBitmap |= prizes;
        }

        public void OnPowderKegDropping(SPEvent ev)
        {
            PlayerShip ship = GameController.GC.PlayerShip;

            if (ship != null)
            {
                uint kegsCount = (uint)Idol.CountForIdol(IdolForKey(Idol.GADGET_SPELL_TNT_BARRELS));

                ship.DropPowderKegs(kegsCount);
                AchievementManager.Kabooms = 0;
            }
        }

        public void OnNetDeployed(SPEvent ev)
        {
            float duration = (float)Idol.DurationForIdol(IdolForKey(Idol.GADGET_SPELL_NET));
            float netScale = Idol.ScaleForIdol(IdolForKey(Idol.GADGET_SPELL_NET));

            if ((MasteryManager.MasteryBitmap & CCMastery.ROGUE_ENTANGLEMENT) != 0)
                netScale *= 1.3f;

            PlayerShip ship = GameController.GC.PlayerShip;

            if (ship != null)
                ship.DeployNet(netScale, duration);
        }

        public void OnBrandySlickDeployed(SPEvent ev)
        {
            GameController gc = GameController.GC;
	        float duration = (float)Idol.DurationForIdol(IdolForKey(Idol.GADGET_SPELL_BRANDY_SLICK));
	        BrandySlickActor brandySlick = gc.PlayerShip.DeployBrandySlick(duration);
    
            if (brandySlick != null && !GameSettings.GS.SettingForKey(GameSettings.BRANDY_SLICK_TIPS))
                mView.DisplayHintByName(GameSettings.BRANDY_SLICK_TIPS, brandySlick.X, brandySlick.Y, 20, brandySlick, false);
        }

        public void OnTempestSummoned(SPEvent ev)
        {
            Idol tempestIdol = IdolForKey(Idol.VOODOO_SPELL_TEMPEST);
            if (tempestIdol == null)
                return;

            SummonTempestWithDuration((float)Idol.DurationForIdol(tempestIdol));
        }

        protected void SummonTempestWithDuration(float duration)
        {
            if (mActorBrains == null)
                return;

            if (GameMode == AwesomePirates.GameMode.Career)
            {
                mActorBrains.SummonTempest(duration, Color.White, true);
                mActorBrains.SummonTempest(duration, Color.White, false);

                if ((MasteryManager.MasteryBitmap & CCMastery.VOODOO_TWISTED_SISTERS) != 0)
                    mActorBrains.SummonTempest(duration, Color.White, false);
            }
            else
            {
                SKTeamIndex teamIndex = SKManager.TeamIndexForIndex(SKManager.CachedIndex);
                Color cloudColor = SKHelper.ColorForTeamIndex(teamIndex);

                TempestActor tempest = mActorBrains.SummonTempest(duration, cloudColor, true);
                if (tempest != null)
                    tempest.OwnerID = teamIndex;

                tempest = mActorBrains.SummonTempest(duration, cloudColor, false);
                if (tempest != null)
                    tempest.OwnerID = teamIndex;
            }
        }

        public void OnWhirlpoolSummoned(SPEvent ev)
        {
            float duration = (float)Idol.DurationForIdol(IdolForKey(Idol.VOODOO_SPELL_WHIRLPOOL));
            SummonWhirlpoolWithDuration(duration);
        }

        private void SummonWhirlpoolWithDuration(float duration)
        {
            if (mView != null && mView.Sea != null)
                mView.Sea.SummonWhirlpoolWithDuration(duration);
        }

        public void OnPrisonersChanged(NumericValueChangedEvent ev)
        {
            if (ev.IntValue > 0 && !GameSettings.GS.SettingForKey(GameSettings.PLANKING_TIPS))
            {
                Vector2 hintLoc = mView.PlankHintLoc;
                mView.DisplayHintByName(GameSettings.PLANKING_TIPS, hintLoc.X, hintLoc.Y, 0, null, true);
            }
        }

        public void OnHandOfDavySummoned(SPEvent ev)
        {
            float duration = (float)Idol.DurationForIdol(IdolForKey(Idol.VOODOO_SPELL_HAND_OF_DAVY));
            SummonHandOfDavyWithDuration(duration);
        }

        protected void SummonHandOfDavyWithDuration(float duration)
        {
            if (mActorBrains == null)
                return;

            if (GameMode == AwesomePirates.GameMode.Career)
            {
                mActorBrains.SummonHandOfDavyWithDuration(duration);
                mActorBrains.SummonHandOfDavyWithDuration(duration);

                if ((MasteryManager.MasteryBitmap & CCMastery.VOODOO_DAVYS_FURY) != 0)
                    mActorBrains.SummonHandOfDavyWithDuration(duration);
            }
            else
            {
                SKTeamIndex teamIndex = SKManager.TeamIndexForIndex(SKManager.CachedIndex);

                HandOfDavy hod = mActorBrains.SummonHandOfDavyWithDuration(duration);
                if (hod != null)
                    hod.OwnerID = teamIndex;

                hod = mActorBrains.SummonHandOfDavyWithDuration(duration);
                if (hod != null)
                    hod.OwnerID = teamIndex;
            }
        }

        public void OnCamouflageActivated(SPEvent ev)
        {
            ActivateCamouflageForDuration((float)Idol.DurationForIdol(IdolForKey(Idol.GADGET_SPELL_CAMOUFLAGE)));
        }

        protected void ActivateCamouflageForDuration(float duration)
        {
            if (mActorBrains == null)
                return;

            GameController gc = GameController.GC;
            if (gc.PlayerShip.IsFlyingDutchman)
                gc.PlayerShip.DeactivateFlyingDutchman();
            mActorBrains.ActivateCamouflageForDuration(duration);
        }

        public void OnFlyingDutchmanActivated(SPEvent ev)
        {
            float duration = (float)Idol.DurationForIdol(IdolForKey(Idol.VOODOO_SPELL_FLYING_DUTCHMAN));
            ActivateFlyingDutchmanForDuration(GameController.GC.PlayerShip, duration);
        }

        protected void ActivateFlyingDutchmanForDuration(PlayableShip ship, float duration)
        {
            if (ship == null)
                return;

            if (GameMode == AwesomePirates.GameMode.Career)
            {
                GameController gc = GameController.GC;

                if (mActorBrains != null && ship.IsCamouflaged)
                    mActorBrains.DeactivateCamouflage();
                if ((MasteryManager.MasteryBitmap & CCMastery.VOODOO_SPECTER_OF_SALVATION) != 0)
                    gc.ThisTurn.ReduceMutinyCountdown(4 * gc.ThisTurn.MutinyCountdown.CounterMax);
            }

            ship.ActivateFlyingDutchman(duration);
        }

        private void IgniteAllIgnitableActors()
        {
            foreach (Actor actor in mActors)
            {
		        if (actor is IIgnitable)
                    (actor as IIgnitable).Ignite();
	        }
        }

        public void OnSeaOfLavaSummoned(SPEvent ev)
        {
            float duration = (float)Idol.DurationForIdol(IdolForKey(Idol.VOODOO_SPELL_SEA_OF_LAVA)) / 2.0f;
            SummonSeaOfLavaWithDuration(duration);
        }

        protected void SummonSeaOfLavaWithDuration(float duration)
        {
            if (mView == null || mView.Sea == null)
                return;

            mView.Sea.TransitionToLavaOverTime(duration);
        }

        public void OnSeaOfLavaPeaked(SPEvent ev)
        {
            if (mView == null || mView.Sea == null || mActorBrains == null)
                return;

            float duration;
            PlayableShip ship;

            if (GameMode == AwesomePirates.GameMode.Career)
            {
                ship = GameController.GC.PlayerShip;

                if (ship != null)
                {
                    mActorBrains.SinkAllShipsWithDeathBitmap(DeathBitmaps.SEA_OF_LAVA, SKTeamIndex.Red);
                    ship.DespawnNetOverTime(1.0f);
                }

                duration = (float)Idol.DurationForIdol(IdolForKey(Idol.VOODOO_SPELL_SEA_OF_LAVA)) / 2f;
            }
            else
            {
                if (mActorBrains == null)
                    return;
                ship = mActorBrains.SkirmishShipForIndex(mView.Sea.OwnerID);

                if (ship != null)
                {
                    mActorBrains.SinkAllShipsWithDeathBitmap(DeathBitmaps.SEA_OF_LAVA, (ship as SkirmishShip).TeamIndex);
                    ship.DespawnNetOverTime(1.0f);
                }

                duration = SKPup.DurationForKey(SKPup.PUP_SEA_OF_LAVA) / 2f;
            }
    
            foreach (Actor actor in mActors)
            {
                if (actor is AcidPoolActor)
                    (actor as AcidPoolActor).DespawnOverTime(1.0f);
            }

            IgniteAllIgnitableActors();
            mView.Sea.TransitionFromLavaOverTime(duration, 1.0f);
        }

        public void OnDeckVoodooIdolPressed(SPEvent ev)
        {
            // VoodooWheel will constrain this to valid coordinates.
            if (ViewWidth / ViewHeight < 1.5f)
                mVoodooManager.ShowMenuAt(ViewWidth / 1.75f, ViewHeight / 2);
            else
                mVoodooManager.ShowMenuAt(ViewWidth / 2, ViewHeight / 2);
            HideHintByName(GameSettings.VOODOO_TIPS);
            EnableSlowedTime(true);
            PushFocusState(InputManager.FOCUS_STATE_PF_VOODOO_WHEEL);

            if (!GameSettings.GS.SettingForKey(GameSettings.VOODOO_TIPS))
                GameSettings.GS.SetSettingForKey(GameSettings.VOODOO_TIPS, true);
        }

        public void OnTreasureFleetSpawned(SPEvent ev)
        {
            if (mActorBrains != null && mActorBrains.Fleet != null && !GameSettings.GS.SettingForKey(GameSettings.TREASURE_FLEET_TIPS))
            {
                SPDisplayObject target = mActorBrains.Fleet as SPDisplayObject;
                mView.DisplayHintByName(GameSettings.TREASURE_FLEET_TIPS, target.X - 30, target.Y + 36, 0, target, false);
                Juggler.DelayInvocation(this, 25, delegate { HideHintByName(GameSettings.TREASURE_FLEET_TIPS); });
            }
        }

        public void OnTreasureFleetAttacked(SPEvent ev)
        {
            if (!GameSettings.GS.SettingForKey(GameSettings.TREASURE_FLEET_TIPS))
                GameSettings.GS.SetSettingForKey(GameSettings.TREASURE_FLEET_TIPS, true);

            HideHintByName(GameSettings.TREASURE_FLEET_TIPS);
        }

        public void OnSilverTrainSpawned(SPEvent ev)
        {
            if (mActorBrains != null && mActorBrains.Fleet != null && !GameSettings.GS.SettingForKey(GameSettings.SILVER_TRAIN_TIPS))
            {
                SPDisplayObject target = mActorBrains.Fleet as SPDisplayObject;
                mView.DisplayHintByName(GameSettings.SILVER_TRAIN_TIPS, target.X + 10, target.Y + 56, 0, target, false);
                Juggler.DelayInvocation(this, 30, delegate { HideHintByName(GameSettings.SILVER_TRAIN_TIPS); });
            }
        }

        public void OnSilverTrainAttacked(SPEvent ev)
        {
            if (!GameSettings.GS.SettingForKey(GameSettings.SILVER_TRAIN_TIPS))
                GameSettings.GS.SetSettingForKey(GameSettings.SILVER_TRAIN_TIPS, true);

            HideHintByName(GameSettings.SILVER_TRAIN_TIPS);
        }

        protected void OnVoodooMenuClosing(SPEvent ev)
        {
            EnableSlowedTime(false);
            PopFocusState(InputManager.HAS_FOCUS_VOODOO_WHEEL);
        }

        public override void EnableSlowedTime(bool enable)
        {
            base.EnableSlowedTime(enable);
            mView.EnableSlowedTime(enable);
        }

        protected void FadeOutShipLayer()
        {
            if (mSpriteLayerManager == null || mJuggler == null)
                return;

            SPDisplayObject shipLayer = mSpriteLayerManager.ChildAtCategory((int)PFCat.NPC_SHIPS);

            if (shipLayer != null)
            {
                SPTween tween = new SPTween(shipLayer, 0.7f);
                tween.AnimateProperty("Alpha", 0.01f);
                mJuggler.AddObject(tween);
            }
        }

        protected void TransitionToTurnOver()
        {
            if (mGuvnor != null && mView != null && mView.PlayerShip != null)
                mGuvnor.RemoveTarget(mView.PlayerShip);
        }

        public void OnAshPickupLooted(NumericValueChangedEvent ev)
        {
            uint ashKey = ev.UintValue;
            Ash ash = new Ash(ashKey);

            if (GameMode == AwesomePirates.GameMode.Career)
                GameController.GC.PlayerShip.AshProc = Ash.AshProcForAsh(ash);
            else if (mActorBrains != null)
            {
                SkirmishShip ship = mActorBrains.SkirmishShipForIndex(SKManager.CachedIndex);

                if (ship != null)
                    ship.AshProc = Ash.SKAshProcForAsh(ash);
            }
        }

        public void OnSKPupLooted(NumericValueChangedEvent ev)
        {
            if (mActorBrains == null)
                return;

            uint pupKey = ev.UintValue;
            SkirmishShip ship = mActorBrains.SkirmishShipForIndex(SKManager.CachedIndex);

            if (ship == null)
                return;

            switch (pupKey)
            {
                case SKPup.PUP_SEA_OF_LAVA:
                    if (mView != null && mView.Sea != null)
                    {
                        mView.Sea.OwnerID = SKManager.CachedIndex;
                        SummonSeaOfLavaWithDuration(SKPup.DurationForKey(pupKey) / 2f);
                    }
                    break;
                case SKPup.PUP_GHOST_SHIP:
                    ActivateFlyingDutchmanForDuration(ship, SKPup.DurationForKey(pupKey));
                    break;
                case SKPup.PUP_HAND_OF_DAVY:
                    SummonHandOfDavyWithDuration(SKPup.DurationForKey(pupKey));
                    break;
                case SKPup.PUP_TEMPEST:
                    SummonTempestWithDuration(SKPup.DurationForKey(pupKey));
                    break;
                case SKPup.PUP_NET:
                    ship.DeployNet(1f, SKPup.DurationForKey(pupKey));
                    break;
                case SKPup.PUP_BRANDY_SLICK:
                    ship.DeployBrandySlick(SKPup.DurationForKey(pupKey));
                    break;
                case SKPup.PUP_POWDER_KEG:
                    ship.DropPowderKegs((uint)SKPup.AmountForKey(pupKey));
                    break;
                case SKPup.PUP_HEALTH:
                    SKTeam team = SKManager.TeamForIndex(ship.TeamIndex);

                    if (team != null)
                        team.AddHealth(SKPup.AmountForKey(pupKey));
                    break;
            }
        }

        public void OnRaceTrackConquered(SPEvent ev)
        {
            float delay = mView.TravelForwardInTime();
            mJuggler.DelayInvocation(AchievementManager, delay + 1f, AchievementManager.Grant88MphAchievement);
        }

        public void OnCloseButNoCigarStateReached(SPEvent ev)
        {
            if (AchievementManager != null)
                AchievementManager.GrantCloseButNoCigarAchievement();
        }

        public void GameOverSequenceDidComplete()
        {
            // TODO
            //if (mState == PfState.EndOfTurn)
            //    mView.ShowTwitter();
        }

        public void OnGameOverRetryPressed(SPEvent ev)
        {
            //GameController.GC.ProcessEndOfTurn(); // Is this needed? I don't see why it would be needed at this point.
            mDidRetry = true;
            SetState(PfState.Menu);
            SetState(PfState.Playing);
        }

        public void OnGameOverMenuPressed(SPEvent ev)
        {
            ControlsManager.CM.SetDefaultPlayerIndex(null);
            SetState(PfState.Menu);
        }

        public override void CreateSKTallyView()
        {
            base.CreateSKTallyView();

            if (mSKTallyView != null)
            {
                mSKTallyView.AddEventListener(SKTallyView.CUST_EVENT_TYPE_SK_GAME_SUMMARY_RETRY, (SPEventHandler)OnSKGameOverRetryPressed);
                mSKTallyView.AddEventListener(SKTallyView.CUST_EVENT_TYPE_SK_GAME_SUMMARY_MENU, (SPEventHandler)OnSKGameOverMenuPressed);
            }
        }

        public void OnSKGameOverRetryPressed(SPEvent ev)
        {
            mDidRetry = true;
            SetState(PfState.Menu);
            SetState(PfState.Playing);
        }

        public void OnSKGameOverMenuPressed(SPEvent ev)
        {
            ControlsManager.CM.SetDefaultPlayerIndex(null);
            SetState(PfState.Menu);
        }

        public void OnTimeOfDayChangedEvent(TimeOfDayChangedEvent ev)
        {
            GameController gc = GameController.GC;

            if (mView != null)
                mView.OnTimeOfDayChanged(ev);
            if (AchievementManager != null)
                AchievementManager.OnTimeOfDayChanged(ev);

            if (gc.ThisTurn.IsGameOver || GameMode != AwesomePirates.GameMode.Career)
		        return;
    
            ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_TIME_OF_DAY);
    
            switch (ev.TimeOfDay)
            {
                case TimeOfDay.SunriseTransition:
                {
                    if (!mRaceEnabled && gc.ThisTurn.AdvState == ThisTurn.AdventureState.Normal)
                    {
                        float fadeInDuration = 2.0f;
                        mView.ShowDayIntroForDay(ev.Day, fadeInDuration);
                        mView.HideDayIntroOverTime(fadeInDuration, 5.0f + fadeInDuration);
                    }
                }
                    break;
                case TimeOfDay.Dusk:
                {
                    if (ev.Day == TimeKeeper.MaxDay && !mRaceEnabled && gc.ThisTurn.AdvState == ThisTurn.AdventureState.Normal)
                    {
                        if (mGuvnor != null)
                        {
                            mGuvnor.RemoveTarget(gc.PlayerShip);
                            mGuvnor.StopThinking();
                        }

                        if (mActorBrains != null)
                            mActorBrains.PrepareForMontyMutiny();

                        gc.ThisTurn.AdvState = ThisTurn.AdventureState.StopShips;
                    }
                }
                    break;
                default:
                    break;
            }
        }

        public void OnMontySkippered(SPEvent ev)
        {
            PlaySound("Splash");
            mMontyShouldMutiny = true;
        }

        public void OnPlayerEaten(SPEvent ev)
        {
            GameController gc = GameController.GC;

            if (gc.ThisTurn.AdvState == ThisTurn.AdventureState.Overboard)
            {
                gc.ThisTurn.AdvState = ThisTurn.AdventureState.Eaten;
                PrepareForGameOver();
            }
        }

        private void PrepareForGameOver()
        {
            GameController gc = GameController.GC;
    
            if (!gc.ThisTurn.IsGameOver)
            {
                SetGameOver(true);
                AwardPrizes(PRIZE_STATS);
                mView.PrepareForGameOver();
                mGuvnor.PrepareForGameOver();
                mActorBrains.PrepareForGameOver();
                ObjectivesManager.PrepareForGameOver();
                mJuggler.DelayInvocation(this, 0.8f, TransitionToTurnOver);
                //mJuggler.DelayInvocation(mView, 1f, mView.DisplayGameOverSequence);

                int masteryLevel = MasteryManager.CurrentModel.MasteryLevel;
                float levelXPFraction = MasteryManager.CurrentModel.LevelXPFraction;

                if (GameMode == AwesomePirates.GameMode.Career)
                {
                    mJuggler.DelayInvocation(this, 1f, delegate
                    {
                        if (mView != null)
                            mView.DisplayGameOverSequence(masteryLevel, levelXPFraction);
                    });
                }
                else
                {
                    mJuggler.DelayInvocation(this, 1f, delegate
                    {
                        CreateSKTallyView();
                        mJuggler.DelayInvocation(this, mSKTallyView.DisplayGameOverSequence() + 0.5f, delegate
                        {
                            mSKTallyView.DisplayGameOverScroll();
                        });
                    });
                }
	        }
        }

        private void OnInfamyChanged(NumericValueChangedEvent ev)
        {
            ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_SCORE);
        }

        private void OnMutinyChanged(NumericRatioChangedEvent ev)
        {
            GameController gc = GameController.GC;
    
	        if (!gc.ThisTurn.IsGameOver && GameMode == AwesomePirates.GameMode.Career)
            {
                int delta = (int)ev.Delta;
        
                if (delta < 0)
                {
                    ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_BLUE_CROSS);
                    PlaySound("MutinyFall");
                }
                else if (delta > 0 || gc.ThisTurn.PlayerShouldDie)
                {
                    ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_RED_CROSS);
                    PlaySound("MutinyRise");

                    if (gc.ThisTurn.Mutiny == 2 && !GameSettings.GS.SettingForKey(GameSettings.VOODOO_TIPS))
                    {
                        Vector2 hintLoc = mView.IdolHintLoc;
                        mView.DisplayHintByName(GameSettings.VOODOO_TIPS, hintLoc.X, hintLoc.Y, 0, null, true);
                    }
                }

                if (!gc.ThisTurn.IsGameOver && gc.ThisTurn.PlayerShouldDie)
                    PrepareForGameOver();
	        }
        }

        public void OnButtonTriggered(SPEvent ev)
        {
            // Do nothing for now
        }

        public void OnTutorialCompleted(SPEvent ev)
        {
            FinishTutorial();
        }

        public void OnFirstMateDecision(SPEvent ev)
        {
            FirstMate mate = ev.CurrentTarget as FirstMate;
            mate.RetireToCabin();
        }

        public void OnFirstMateRetiredToCabin(SPEvent ev)
        {
            GameController gc = GameController.GC;
            FirstMate mate = ev.CurrentTarget as FirstMate;

            switch (mate.UserData)
            {
                case kFirstMateMutinied:
                    gc.PlayerShip.Monty = PlayerShip.MontyState.Mutineer;
                    mActorBrains.MarkPlayerAsEdible();
                    break;
                case kEtherealPotionNotice:
                    gc.GameStats.EnforcePotionRequirements();

                    if (mState == PfState.EndOfTurn || mState == PfState.DelayedRetry || mState == PfState.DelayedQuit)
                        ContinueEndOfTurn();
                    break;
                default:
                    break;
            }

            mate.RemoveEventListener(FirstMate.CUST_EVENT_TYPE_FIRST_MATE_DECISION, (SPEventHandler)OnFirstMateDecision);
            mate.RemoveEventListener(FirstMate.CUST_EVENT_TYPE_FIRST_MATE_RETIRED, (SPEventHandler)OnFirstMateRetiredToCabin);
            RemoveProp(mate);
        }

        public void OnPlayerShipSinking(SPEvent ev)
        {
            PrepareForGameOver();
        }

        public void OnSKPlayerShipSinking(PlayerIndexEvent ev)
        {
            if (mView != null)
                mView.OnSKPlayerShipSinking(ev);

            if (mState == PfState.Playing)
            {
                int numAliveTeams = SKManager.NumAliveTeams;

                // Guard against two ships from the same team triggering this twice in a row.
                if (numAliveTeams == 1 && SKManager.GameCountdown == AwesomePirates.SKManager.SKGameCountdown.None)
                    SKManager.GameCountdown = AwesomePirates.SKManager.SKGameCountdown.Finale;
            }
        }

        public void OnSKGameOver(SPEvent ev)
        {
            if (mState == PfState.Playing)
            {
                SKManager.GameCountdown = AwesomePirates.SKManager.SKGameCountdown.None;
                mView.SKGameCountdownCancelled();
                PrepareForGameOver();
            }
        }

        private void DisplayEtherealPotionNoticeWithMsgs(List<string> msgs)
        {
            mView.DisplayEtherealAlert(msgs, kEtherealPotionNotice, 1, 0.5f);
        }

        public void OnObjectivesRankupCompleted(BinaryEvent ev)
        {
            uint rank = ObjectivesManager.Rank;

            do
            {
                if (ev.BinaryValue)
                {
                    if (rank == Potion.RequiredRankForTwoPotions)
                        DisplayEtherealPotionNoticeWithMsgs(new List<string>() { "Captain, you can now use two potions at once!" });
                    else if (Potion.IsPotionUnlockedAtRank((int)rank))
                    {
                        uint potionKey = Potion.UnlockedPotionKeyForRank((int)rank);
                        string potionName = Potion.NameForKey(potionKey);

                        if (potionName != null)
                        {
                            string msg = (rank == Potion.MinPotionRank) ?
                                "Valerie at your service, Captain. Potions are now available at the main menu." :
                                string.Format("Captain, Vial of {0} is now available!", potionName);
                            DisplayEtherealPotionNoticeWithMsgs(new List<string>() { msg });
                        }
                        else
                            break;
                    }
                    else
                        break;

                    return;
                }
            } while (false);

            ContinueEndOfTurn();
        }

        protected void ContinueEndOfTurn()
        {
            if (MasteryManager != null && MasteryManager.CurrentModel != null)
            {
                MasteryManager.CurrentModel.AddXP(GameController.GC.ThisTurn.Infamy);
                MasteryManager.CurrentModel.AttemptLevelUp();
            }

            if (mState == PfState.DelayedRetry)
                ContinueDelayedRetry();
            else if (mState == PfState.DelayedQuit)
                ContinueDelayedQuit();
            else
                mView.DisplayGameSummary();
        }

        protected void ContinueDelayedRetry()
        {
            GameController.GC.ProcessEndOfTurn();
            SetState(PfState.Menu);
            SetState(PfState.Playing);
        }

        protected void ContinueDelayedQuit()
        {
            ControlsManager.CM.SetDefaultPlayerIndex(null);
            SetState(PfState.Menu);
        }

        public override void DisplaySKTallyView()
        {
            ControlsManager.CM.SetDefaultPlayerIndex(null);
            base.DisplaySKTallyView();
        }

        public override void HideSKTallyView()
        {
            if (GameMode != AwesomePirates.GameMode.Career)
                ControlsManager.CM.SetDefaultPlayerIndex(null);
            base.HideSKTallyView();
        }

        public override void DisplayExitView()
        {
            if (mExitView != null)
                return;

            DisplayPauseMenu();
            if (mMenuController != null)
                mMenuController.ExitMenuWillShow();

            base.DisplayExitView();
        }

        public override void HideExitView()
        {
            if (mExitView == null)
                return;

            base.HideExitView();

            if (mMenuController != null)
                mMenuController.ExitMenuDidHide();
        }

        public override void DisplayPauseMenu()
        {
            if (!mScenePaused && mState == PfState.Playing)
            {
#if false
                if (ControlsManager.CM.NumConnectedControllers > 1)
                {
                    ReassignDefaultController();

                    CCPoint guidePos = TitleSubview.GuidePositionForScene(TitleSubview.GuidePosition.MidLower, this);
                    if (guidePos != null)
                        ShowGuideProp(ControlsManager.CM.PlayerIndexMap, guidePos.X, guidePos.Y, 2f);
                }
#endif
                if (mView != null)
                    mView.PauseMenuDisplayed();
                PushFocusState(InputManager.FOCUS_STATE_PF_PAUSE);
                base.DisplayPauseMenu();
            }
        }

        public override void DismissPauseMenu()
        {
            if (mView != null)
                mView.PauseMenuDismissed();
            PopFocusState(InputManager.FOCUS_STATE_PF_PAUSE);
            HideGuideProp();
            base.DismissPauseMenu();
        }

        public override void Resume()
        {
            ControlsManager cm = ControlsManager.CM;
            PlayerIndex? playerIndex = cm.PrevQueryPlayerIndex;

            if (playerIndex.HasValue)
            {
                cm.MainPlayerIndex = playerIndex;
                base.Resume();
            }
        }

        public override void Retry()
        {
            if (!mScenePaused || !mHasPauseMenu)
		        return;
    
            mDidRetry = true;
            AwardPrizes(AllPrizesBitmap);
            base.Retry();
    
            SetState(PfState.DelayedRetry);
            ObjectivesManager.ProcessEndOfTurn();
        }

        public override void Quit()
        {
            if (!mScenePaused || !mHasPauseMenu)
		        return;

            mDidQuit = true;
            AwardPrizes(AllPrizesBitmap);
            base.Quit();
    
            SetState(PfState.DelayedQuit);
            ObjectivesManager.ProcessEndOfTurn();
        }

        protected void SetupActorAi()
        {
            if (mActorBrains == null)
            {
                mActorBrains = new ActorAi(this);
                mActorBrains.AiKnob = GameController.GC.AiKnob;
                mActorBrains.AddEventListener(ActorAi.CUST_EVENT_TYPE_TREASURE_FLEET_SPAWNED, (SPEventHandler)OnTreasureFleetSpawned);
                mActorBrains.AddEventListener(ActorAi.CUST_EVENT_TYPE_TREASURE_FLEET_ATTACKED, (SPEventHandler)OnTreasureFleetAttacked);
                mActorBrains.AddEventListener(ActorAi.CUST_EVENT_TYPE_SILVER_TRAIN_SPAWNED, (SPEventHandler)OnSilverTrainSpawned);
                mActorBrains.AddEventListener(ActorAi.CUST_EVENT_TYPE_SILVER_TRAIN_ATTACKED, (SPEventHandler)OnSilverTrainAttacked);
                mActorBrains.AddEventListener(ActorAi.CUST_EVENT_TYPE_CLOSE_BUT_NO_CIGAR_STATE_REACHED, (SPEventHandler)OnCloseButNoCigarStateReached);

                if (mGuvnor != null)
                    mActorBrains.AddEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_AI_KNOB_VALUE_CHANGED, (NumericValueChangedEventHandler)mGuvnor.OnAiModifierChanged);
            }
    
            ActorAi.SetupAiKnob(GameController.GC.AiKnob);
            mActorBrains.DifficultyFactor = 5f;
            mActorBrains.PrepareForNewGame();
        }

        protected void SetupTownAi()
        {
            if (mGuvnor == null)
                mGuvnor = new TownAi(this);
            mGuvnor.AiModifier = GameController.GC.AiKnob.aiModifier;
            mGuvnor.PrepareForNewGame();
        }

        protected void DestroyActorAi()
        {
            if (mActorBrains == null)
                return;

            mActorBrains.RemoveEventListener(ActorAi.CUST_EVENT_TYPE_TREASURE_FLEET_SPAWNED, (SPEventHandler)OnTreasureFleetSpawned);
            mActorBrains.RemoveEventListener(ActorAi.CUST_EVENT_TYPE_TREASURE_FLEET_ATTACKED, (SPEventHandler)OnTreasureFleetAttacked);
            mActorBrains.RemoveEventListener(ActorAi.CUST_EVENT_TYPE_SILVER_TRAIN_SPAWNED, (SPEventHandler)OnSilverTrainSpawned);
            mActorBrains.RemoveEventListener(ActorAi.CUST_EVENT_TYPE_SILVER_TRAIN_ATTACKED, (SPEventHandler)OnSilverTrainAttacked);
            mActorBrains.RemoveEventListener(ActorAi.CUST_EVENT_TYPE_CLOSE_BUT_NO_CIGAR_STATE_REACHED, (SPEventHandler)OnCloseButNoCigarStateReached);

            if (mGuvnor != null)
                mActorBrains.RemoveEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_AI_KNOB_VALUE_CHANGED, (NumericValueChangedEventHandler)mGuvnor.OnAiModifierChanged);

            mActorBrains.StopThinking();
            mActorBrains = null;
        }

        protected void DestroyTownAi()
        {
            if (mGuvnor == null)
                return;
            mGuvnor.Dispose();
            mGuvnor = null;
        }

        public override void DestroyScene()
        {
            GameController gc = GameController.GC;

            DetachEventListeners();
            mJuggler.RemoveAllObjects();
            mSpamJuggler.RemoveAllObjects();
            
            DestroyActorAi();
            //DestroyTownAi();

            if (mView != null)
            {
                mView.DestroyView();
                mView = null;
            }

            if (mMenuController != null)
            {
                mMenuController.Dispose();
                mMenuController = null;
            }

            if (ObjectivesManager != null)
                ObjectivesManager.SetScene(null);

            if (mActors != null)
            {
                foreach (Actor actor in mActors)
                {
                    actor.CheckinPooledResources();
                    actor.SafeRemove();
                }
            }

            RemoveQueuedActors();

            if (mProps != null)
            {
                foreach (Prop prop in mProps)
                    prop.CheckinPooledResources();
            }

            if (AchievementManager != null)
            {
                AchievementManager.UnloadCombatTextWithOwner(SceneKey);
                AchievementManager.EnableSuspendedMode(false);
            }

            if (mVoodooManager != null)
            {
                RemoveVoodooManagerListeners();
                mVoodooManager.Dispose();
                mVoodooManager = null;
            }

            GameController.GC.ThisTurn.TutorialMode = false;

            if (mCacheManagers != null)
            {
                foreach (KeyValuePair<uint, CacheManager> kvp in mCacheManagers)
                    kvp.Value.Dispose();
                gc.CacheResourceForKey(null, RESOURCE_CACHE_MANAGERS);
                mCacheManagers = null;
            }

            base.DestroyScene();
            Actor.RelinquishActorsScene(this);
        }
        #endregion
    }
}
