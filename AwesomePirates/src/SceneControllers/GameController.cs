
//#define WINDOWS_TRIAL_MODE
//#define NV_PERF_HUD

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;
using System.Threading;
using System.Globalization;
using SparrowXNA;
using System.Diagnostics;

#if WINDOWS
using System.Windows.Forms;
#endif

// AirServer - for App Videos without a handycam: http://www.airserverapp.com/
// Spriter - animation tool from http://www.brashmonkey.com/

namespace AwesomePirates
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    class GameController : Microsoft.Xna.Framework.Game
    {
        private const string kEventQDeviceSelected = "DeviceSelected";
        private const string kEventQDeviceCancelled = "DeviceCancelled";
        private const string kEventQLoadCompleted = "LoadCompleted";
        private const string kEventQLoadFailed = "LoadFailed";
        private const string kEventQSaveCompleted = "SaveCompleted";
        private const string kEventQSaveFailed = "SaveFailed";

        private static volatile bool s_GlobalDeviceSelected = false;
        private static volatile bool s_LocalDeviceSelected = false;
        private static volatile bool s_LocalDeviceCancelled = false;
        private static volatile bool s_LocalLoadCompleted = false;
        private static volatile bool s_LocalLoadFailed = false;
        private static volatile bool s_LocalSaveCompleted = false;
        private static volatile bool s_LocalSaveFailed = false;
        private static readonly object s_lock = new object();
        private static Queue<SPEvent> s_GlobalDeviceEventQueue = new Queue<SPEvent>(5);
        private static Dictionary<string, Queue<PlayerIndexEvent>> s_LocalEventQueues = new Dictionary<string, Queue<PlayerIndexEvent>>()
        {
            { kEventQDeviceSelected, new Queue<PlayerIndexEvent>(5) },
            { kEventQDeviceCancelled, new Queue<PlayerIndexEvent>(5) },
            { kEventQLoadCompleted, new Queue<PlayerIndexEvent>(5) },
            { kEventQLoadFailed, new Queue<PlayerIndexEvent>(5) },
            { kEventQSaveCompleted, new Queue<PlayerIndexEvent>(5) },
            { kEventQSaveFailed, new Queue<PlayerIndexEvent>(5) }
        };
        private static GameController instance = null;

        public GameController(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (args[0].ToLower() == "trial")
                {
                    Guide.SimulateTrialMode = true;
                    Components.Add(mTrialModeCounter = new TrialModeCounter(this));
                }
                else if (args[0].ToLower() == "cheekylb_884201-_-monitor")
                {
                    mIsLogging = true;
                }
            }

            if (instance == null)
                instance = this;

            SetupGameController();
        }

        private GameController()
        {

        }

        private void SetupGameController()
        {
            SPTextField.SetNewLine("^");
            SPTextField.PrimeTextCacheWithCapacity(64);
            Actor.PrimeContactCacheWithCapacity(128);
            SpynDoctor.TopScore.IsLogging = mIsLogging;

            DisplayMode currentDisplayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            float currentAspectRatio = currentDisplayMode.AspectRatio;
            int preferredWidth = currentDisplayMode.Width, preferredHeight = currentDisplayMode.Height;
            //int preferredWidth = 1280, preferredHeight = 720;
            //float currentAspectRatio = preferredWidth / (float)preferredHeight;

#if XBOX
            // XBox is different. We don't have to match the aspect ratio exactly because the XBox will scale
            // the output for us, letterboxing to different degrees. Our aim is to hit the best supported
            // aspect ratio (anything above 1.33, currently), such that letterboxing is minimized.
            foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                Debug.WriteLine("A/R: {0} W: {1} H: {2}", mode.AspectRatio, mode.Width, mode.Height);
            }

    #if true

            if (Math.Abs((4f / 3f) - currentAspectRatio) < Math.Abs((16f / 9f) - currentAspectRatio))
            {
                // 4:3
                preferredWidth = 880;
                preferredHeight = 660;
                ResManager.OUTPUT_WIDTH = 960;
                ResManager.OUTPUT_HEIGHT = 720;

                ResManager.RES_BACKBUFFER_WIDTH = preferredWidth;
                ResManager.RES_BACKBUFFER_HEIGHT = preferredHeight;
                ResManager.RESM.IsCustRes = true;

                ResManager.RESM.GameFactorArea = 1.2f;
                ResManager.RESM.GameFactorHeight = 1.1f;
                ResManager.RESM.GameFactorWidth = 1.1f;
            }
            else
            {
                // 16:9
                preferredWidth = 1024;
                preferredHeight = 600;
                ResManager.OUTPUT_WIDTH = 1152;
                ResManager.OUTPUT_HEIGHT = 648;

                ResManager.RES_BACKBUFFER_WIDTH = preferredWidth;
                ResManager.RES_BACKBUFFER_HEIGHT = preferredHeight;
                ResManager.RESM.IsCustRes = true;

                ResManager.RESM.GameFactorArea = 1.25f;
                ResManager.RESM.GameFactorHeight = 1.1f;
                ResManager.RESM.GameFactorWidth = 1.25f;
            }

            
            
#else
            if (currentAspectRatio < 1.33f || preferredWidth > ResManager.RES_BACKBUFFER_WIDTH || preferredHeight > ResManager.RES_BACKBUFFER_HEIGHT)
            {
                if (currentAspectRatio < 1.33f)
                    currentAspectRatio = 1.33f;
                
                /*
                float widthFactor = preferredWidth / (float)ResManager.RES_BACKBUFFER_WIDTH;
                float heightFactor = preferredHeight / (float)ResManager.RES_BACKBUFFER_HEIGHT;

                if (widthFactor > heightFactor)
                {
                    preferredWidth = ResManager.RES_BACKBUFFER_WIDTH;
                    preferredHeight = (int)Math.Round(preferredWidth / currentAspectRatio);
                }
                else
                {
                    preferredHeight = ResManager.RES_BACKBUFFER_HEIGHT;
                    preferredWidth = (int)Math.Round(preferredHeight * currentAspectRatio);
                }
                */
                
                if (preferredWidth > ResManager.RES_BACKBUFFER_WIDTH)
                {
                    preferredWidth = ResManager.RES_BACKBUFFER_WIDTH;
                    preferredHeight = (int)Math.Round(preferredWidth / currentAspectRatio);
                }
                else
                {
                    preferredHeight = (int)Math.Round(preferredWidth / currentAspectRatio);
                }
            }
#endif
#else
            // NOTE: On Windows at high resolutions, we're probably better off drawing the ocean to a RenderTexture at 1280x720 (or a similar size 
            //       that matches the current aspect ratio) and then drawing the RenderedTexture scaled up to the backbuffer. Anything above 1280x720
            //       is a lot of work for the GFX card due to the complex water shader, so although the rendered texture effectively draws the ocean
            //       twice, the most expensive part (the shader) is minimized. Profile both methods with IsFixedTimeStep = false.
            preferredWidth = 1440;  // 1024; // 960; // 1136; // 1280; // 1680; // 1280; // 1280; // 1152;
            preferredHeight = 900;  // 768; // 640; // 640; // 720; // 1050; // 720; // 720; // 648;

            ResManager.RES_BACKBUFFER_WIDTH = preferredWidth;
            ResManager.RES_BACKBUFFER_HEIGHT = preferredHeight;
            ResManager.RESM.IsCustRes = true;

            ResManager.RESM.GameFactorArea = 1.25f;
            ResManager.RESM.GameFactorHeight = 1.1f;
            ResManager.RESM.GameFactorWidth = 1.25f;
#endif

            mPaused = false;
            mGameSaved = true;

#if XBOX || DEBUG || SYSTEM_LINK_SESSION
            // We want a trial mode switch forced in the first Update even if it is not in trial mode. This switch will startup restricted functionality (online leaderboards).
            mIsTrialMode = mWasTrialMode = true; // Guide.IsTrialMode;
#elif WINDOWS && WINDOWS_TRIAL_MODE
                mIsTrialMode = mWasTrialMode = true;
#else
                mIsTrialMode = mWasTrialMode = false;
#endif

            mGraphics = new GraphicsDeviceManager(this);
            mGraphics.PreferredBackBufferWidth = ResManager.RES_BACKBUFFER_WIDTH;
            mGraphics.PreferredBackBufferHeight = ResManager.RES_BACKBUFFER_HEIGHT;
            mGraphics.PreferMultiSampling = true;
            mGraphics.PreferredBackBufferFormat = SurfaceFormat.Color;
            //mGraphics.IsFullScreen = true;
            mGraphics.PreparingDeviceSettings += Graphics_PreparingDeviceSettings;
            IsMouseVisible = true;

            FastMath.PrimeAtan2Lut();
            MenuButton.EffecterSetup = SetupHighlightEffecter;
            mCachedResources = new Dictionary<string, object>();
            mAiKnob = new AiKnob();
            ActorAi.SetupAiKnob(mAiKnob);
            mPlayerShip = null;
            mThisTurn = new ThisTurn();
            mTextureManager = new TextureManager("data/atlases/", "atlases/", Content);
            mObjectivesManager = new ObjectivesManager(null, null);
            mAchievementManager = new AwesomePirates.AchievementManager();
            mMasteryManager = new AwesomePirates.MasteryManager(new CCMastery());
            mPlayerDetails = new PlayerDetails(GameStats);
            mAudioPlayers = new Dictionary<string, AudioPlayer>(1);
            mAudioPlayerDump = new List<AwesomePirates.AudioPlayer>(1);
            mXAudioPlayers = new Dictionary<string, XACTAudioPlayer>(1);
            mTimeKeeper = new TimeKeeper(TimeOfDay.Dawn, 0);
            mAchievementManager.TimeOfDay = mTimeKeeper.TimeOfDay;

#if false
            mTimeKeeper.AddEventListener(TimeOfDayChangedEvent.CUST_EVENT_TYPE_TIME_OF_DAY_CHANGED, (TimeOfDayChangedEventHandler)delegate(TimeOfDayChangedEvent ev)
            {
                Debug.WriteLine("Time of day changed to {0}", ev.TimeOfDay);

                if (ev.TimeOfDay == TimeOfDay.SunriseTransition)
                    Debug.WriteLine(mTimeKeeper.IntroForDay(ev.Day));
            });
#endif

            mAchievementManager.ProfileManager.AddEventListener(GameStats.CUST_EVENT_TYPE_PLAYER_CHANGED, (SPEventHandler)OnPlayerChanged);
            mAchievementManager.ProfileManager.AddEventListener(ProfileManager.CUST_EVENT_TYPE_PLAYER_LOGGED_IN, (PlayerIndexEventHandler)OnPlayerLoggedIn);
            mAchievementManager.ProfileManager.AddEventListener(ProfileManager.CUST_EVENT_TYPE_PLAYER_LOGGED_OUT, (PlayerIndexEventHandler)OnPlayerLoggedOut);
            mAchievementManager.ProfileManager.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, (PlayerIndexEventHandler)OnLocalLoadCompleted);
            mAchievementManager.ProfileManager.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, (PlayerIndexEventHandler)OnLocalLoadFailed);

            mRandom = new Random();

            mClearColor = Color.Black; // new Color(34, 34, 34, 255);  //new Color(54, 177, 171, 255); //new Color(34, 34, 34, 255); // Charcoal

            //TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 60.0f); // Aim for 60 updates per second
            IsFixedTimeStep = true; // false = Update and Draw called in continuous loop synched to screen refresh rate.

            Content.RootDirectory = "Content";

            ControlsManager.CM.AddEventListener(ControlsManager.CUST_EVENT_TYPE_DEFAULT_CONTROLLER_DISCONNECTED, (SPEventHandler)OnDefaultControllerDisconnected);
            ControlsManager.CM.AddEventListener(ControlsManager.CUST_EVENT_TYPE_CONTROLLER_DID_ENGAGE, (PlayerIndexEventHandler)OnControllerEngaged);
            ControlsManager.CM.AddEventListener(ControlsManager.CUST_EVENT_TYPE_EXIT_BUTTON_PRESSED, (SPEventHandler)OnExitButtonPressed);

            string numberFormatSeparator = NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
            if (numberFormatSeparator != null && numberFormatSeparator.Length > 0)
                Globals.NumberGroupSeparator = numberFormatSeparator[0];

            string numberDecimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
            if (numberDecimalSeparator != null && numberDecimalSeparator.Length > 0)
                Locale.NumberDecimalSeparator = numberDecimalSeparator[0];

            //MediaPlayer.MediaStateChanged += new EventHandler<EventArgs>(MediaPlayer_MediaStateChanged);
            //MediaPlayer.ActiveSongChanged += new EventHandler<EventArgs>(MediaPlayer_ActiveSongChanged);
        }

        #region Fields
        private bool mIsDestroyed = false;
        private bool mPaused;
        private bool mGameSaved;
        private bool mWasTrialMode;
        private bool mIsTrialMode;
        private bool mIsLogging = false;
        private bool mExiting = false;
        private volatile bool mBypassingFileManagerEvents = false;
        private Boolean? mSavedTimerActivity = null;
        private int mStartupCount = -1;

        private GraphicsDeviceManager mGraphics;
        private SPStage mStage;
        private SPCamera mCamera;

        private TopScoreManager mTopScoreManager;
        private TextureManager mTextureManager;
        private ObjectivesManager mObjectivesManager;
        private AchievementManager mAchievementManager;
        private MasteryManager mMasteryManager;
        private Dictionary<string, AudioPlayer> mAudioPlayers;
        private Dictionary<string, XACTAudioPlayer> mXAudioPlayers;
        private List<AudioPlayer> mAudioPlayerDump;
        private TimeKeeper mTimeKeeper;
        private AiKnob mAiKnob;

        private SceneController mCurrentScene;
        private List<Action> mDelayedCalls;
        private Dictionary<string, object> mCachedResources;
        private ThisTurn mThisTurn;
        private PlayerDetails mPlayerDetails;
        private PlayerShip mPlayerShip;

        private Random mRandom;
        private Color mClearColor;

        private TrialModeCounter mTrialModeCounter;
        #endregion

        #region Properties
        public static GameController GC
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameController();
                    instance.SetupGameController();
                }
                return instance;
            }
        }
        public GraphicsDeviceManager DeviceManager { get { return mGraphics; } }
        public SPStage Stage { get { return mStage; } }
        public SPCamera Camera { get { return mCamera; } }
        public AudioPlayer AudioPlayer { get { return mCurrentScene != null ? mCurrentScene.AudioPlayer : null; } }
        public XACTAudioPlayer XAudioPlayer { get { return mCurrentScene != null ? mCurrentScene.XAudioPlayer : null; } }
        public TopScoreManager LiveLeaderboard { get { return mTopScoreManager; } }
        public TextureManager TextureManager { get { return mTextureManager; } }
        public ObjectivesManager ObjectivesManager { get { return mObjectivesManager; } }
        public AchievementManager AchievementManager { get { return mAchievementManager; } }
        public MasteryManager MasteryManager { get { return mMasteryManager; } }
        public ProfileManager ProfileManager { get { return (mAchievementManager != null) ? mAchievementManager.ProfileManager : null; } }
        public TimeKeeper TimeKeeper { get { return mTimeKeeper; } }
        public TimeOfDay TimeOfDay { get { return mTimeKeeper.TimeOfDay; } }
        public AiKnob AiKnob { get { return mAiKnob; } }
        public ThisTurn ThisTurn { get { return mThisTurn; } }
        public GameStats GameStats { get { return (AchievementManager != null) ? AchievementManager.Stats : null; } }
        public HiScoreTable HiScores { get { return (ProfileManager != null) ? ProfileManager.HiScores : null; } }
        public PlayerDetails PlayerDetails { get { return mPlayerDetails; } }
        public PlayerShip PlayerShip { get { return mPlayerShip; } set { mPlayerShip = value; } }
        public int NumDelayedCalls { get { return (mDelayedCalls != null) ? mDelayedCalls.Count : 0; } }
        public float Fps { get { return 60f; } }
        public bool Paused
        {
            get { return mPaused; }
            set
            {
                if (mPaused == value)
                    return;

                if (value)
                {
                    if (AudioPlayer != null)
                        AudioPlayer.Pause();

                    if (XAudioPlayer != null)
                        XAudioPlayer.Pause();

                    if (mTimeKeeper != null)
                    {
                        mSavedTimerActivity = mTimeKeeper.TimerActive;
                        mTimeKeeper.TimerActive = false;
                    }
                }
                else
                {
                    if (mSavedTimerActivity != null)
                    {
                        if (mTimeKeeper != null)
                            mTimeKeeper.TimerActive = (bool)mSavedTimerActivity;
                        mSavedTimerActivity = null;
                    }

                    if (AudioPlayer != null)
                        AudioPlayer.Resume();

                    if (XAudioPlayer != null)
                        XAudioPlayer.Resume();
                }

                mPaused = value;
            }
        }
        public bool GameSaved { get { return mGameSaved; } set { mGameSaved = value; } }
        public bool BypassingFileManagerEvents { get { return mBypassingFileManagerEvents; } set { mBypassingFileManagerEvents = value; } }
        public bool WasTrialMode { get { return mWasTrialMode; } }
        public bool IsTrialMode
        {
            get
            {
                if (mTrialModeCounter != null)
                    return mTrialModeCounter.IsTrialMode;
                else
                    return mIsTrialMode;
            }
            private set 
            {
                mWasTrialMode = mIsTrialMode;
                mIsTrialMode = value;

                if (mWasTrialMode && !mIsTrialMode)
                    TransitionFromTrialMode();

                if (mWasTrialMode != mIsTrialMode && mCurrentScene != null)
                    mCurrentScene.TrialModeDidChange(mIsTrialMode);
            }
        }
        public bool IsLogging { get { return mIsLogging; } }
        #endregion

        #region Methods
        private void Graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.PresentationInterval = PresentInterval.One; // Aim for 60 FPS
            e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 4; // Free on XBOX
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

#if DEBUG && NV_PERF_HUD
            // NVPerfHUD setup
            foreach (GraphicsAdapter adapter in GraphicsAdapter.Adapters)
            {
                if (adapter.Description.Contains("PerfHUD"))
                {
                    e.GraphicsDeviceInformation.Adapter = adapter;
                    GraphicsAdapter.UseReferenceDevice = true;
                    break;
                }
            }
#endif
        }

        public void SetMediaPlayerVolume(int value)
        {
            //float volume = MathHelper.Clamp(value, 0f, 10f);
            //double logged = Math.Log(volume + 1) / Math.Log(11);
            //MediaPlayer.Volume = MathHelper.Clamp((float)logged, 0f, 1f);
        }

        // Async callback
        public void OnGlobalSaveDeviceSelected(SPEvent ev)
        {
            lock (s_lock)
            {
                s_GlobalDeviceEventQueue.Enqueue(ev);
                s_GlobalDeviceSelected = true;
            }
        }

        private void GlobalSaveDeviceWasSelected(SPEvent ev)
        {
            if (GameSettings.GS.DidLastSaveFail)
                GameSettings.GS.SaveSettings();
        }

        // Async callback
        public void OnLocalSaveDeviceSelected(PlayerIndexEvent ev)
        {
            if (mBypassingFileManagerEvents)
                return;

            //Debug.WriteLine("OnLocalSaveDeviceSelected: Thread ID {0}", Thread.CurrentThread.ManagedThreadId);
            lock (s_lock)
            {
                s_LocalEventQueues[kEventQDeviceSelected].Enqueue(ev);
                s_LocalDeviceSelected = true;
            }
        }

        private void LocalSaveDeviceWasSelected(PlayerIndexEvent ev)
        {
            if (ProfileManager != null && ev != null)
                ProfileManager.PlayerSaveDeviceSelected(ev.PlayerIndex);
            if (mCurrentScene != null)
                mCurrentScene.PlayerSaveDeviceSelected(ev.PlayerIndex);
        }

        // Async callback
        public void OnLocalSaveDeviceCancelled(PlayerIndexEvent ev)
        {
            if (mBypassingFileManagerEvents)
                return;

            lock (s_lock)
            {
                s_LocalEventQueues[kEventQDeviceCancelled].Enqueue(ev);
                s_LocalDeviceCancelled = true;
            }
        }

        private void LocalSaveDeviceWasCancelled(PlayerIndexEvent ev)
        {
            if (ProfileManager != null && ev != null)
                ProfileManager.PlayerSaveDeviceCancelled(ev.PlayerIndex);
           // ProcessDelayedCalls();
        }

        // Potentially an Async callback
        public void OnLocalLoadCompleted(PlayerIndexEvent ev)
        {
            if (mBypassingFileManagerEvents)
                return;

            lock (s_lock)
            {
                s_LocalEventQueues[kEventQLoadCompleted].Enqueue(ev);
                s_LocalLoadCompleted = true;
            }
        }

        private void LocalLoadCompleted(PlayerIndexEvent ev)
        {
            if (mCurrentScene != null && ev != null)
                mCurrentScene.LocalLoadCompleted(ev.PlayerIndex);
        }

        // Potentially an Async callback
        public void OnLocalLoadFailed(PlayerIndexEvent ev)
        {
            if (mBypassingFileManagerEvents)
                return;

            lock (s_lock)
            {
                s_LocalEventQueues[kEventQLoadFailed].Enqueue(ev);
                s_LocalLoadFailed = true;
            }
        }

        private void LocalLoadFailed(PlayerIndexEvent ev)
        {
            if (mCurrentScene != null && ev != null)
                mCurrentScene.LocalLoadFailed(ev.PlayerIndex);
        }

        // Async callback
        public void OnLocalSaveCompleted(PlayerIndexEvent ev)
        {
            if (mBypassingFileManagerEvents)
                return;

            lock (s_lock)
            {
                s_LocalEventQueues[kEventQSaveCompleted].Enqueue(ev);
                s_LocalSaveCompleted = true;
            }
        }

        private void LocalSaveCompleted(PlayerIndexEvent ev)
        {
            if (mCurrentScene != null && ev != null)
                mCurrentScene.LocalSaveCompleted(ev.PlayerIndex);
        }

        // Async callback
        public void OnLocalSaveFailed(PlayerIndexEvent ev)
        {
            if (mBypassingFileManagerEvents)
                return;

            lock (s_lock)
            {
                s_LocalEventQueues[kEventQSaveFailed].Enqueue(ev);
                s_LocalSaveFailed = true;
            }
        }

        private void LocalSaveFailed(PlayerIndexEvent ev)
        {
            if (mCurrentScene != null && ev != null)
                mCurrentScene.LocalSaveFailed(ev.PlayerIndex);
        }

        public void OnDefaultControllerDisconnected(SPEvent ev)
        {
            if (mCurrentScene != null)
                mCurrentScene.DefaultControllerDisconnected();
        }

        public void OnPlayerChanged(SPEvent ev)
        {
            if (mPlayerDetails != null)
                mPlayerDetails.OnPlayerChanged(ev);

            if (ProfileManager.IsUsingGlobalPlayerStats)
            {
                MasteryManager.SetCurrentModel(MasteryManager.kDefaultMasteryModelKey);
                MasteryManager.RefreshMasteryBitmap();
            }
            else
            {
                int playerIndex = (int)ProfileManager.MainPlayerIndex;
                if (!MasteryManager.ContainsModel(playerIndex))
                    MasteryManager.AddModel(playerIndex, GameStats.Masteries);
                MasteryManager.SetCurrentModel(playerIndex);
                MasteryManager.RefreshMasteryBitmap();
            }

            if (mCurrentScene != null)
                mCurrentScene.PlayerChanged();

            // Don't process delayed events while ProfileManager is prompting for a device.
            //if (!ProfileManager.PromptingIndex.HasValue)
            //    ProcessDelayedCalls();
        }

        public void OnPlayerLoggedIn(PlayerIndexEvent ev)
        {
            if (ev == null)
                return;

            if (mTopScoreManager != null && ControlsManager.CM.HasControllerEngaged(ev.PlayerIndex))
                mTopScoreManager.AddPotentialHost(SignedInGamer.SignedInGamers[ev.PlayerIndex]);
            if (mCurrentScene != null)
                mCurrentScene.PlayerLoggedIn(ev.PlayerIndex);
        }

        public void OnPlayerLoggedOut(PlayerIndexEvent ev)
        {
            if (ev == null)
                return;

            if (MasteryManager != null)
                MasteryManager.RemoveModel((int)ev.PlayerIndex);
            if (mCurrentScene != null)
                mCurrentScene.PlayerLoggedOut(ev.PlayerIndex);
            if (mTopScoreManager != null)
                mTopScoreManager.RemovePotentialHost(ev.PlayerIndex);
        }

        public void OnControllerEngaged(PlayerIndexEvent ev)
        {
            if (ev == null)
                return;

            if (mTopScoreManager != null && ControlsManager.CM.HasControllerEngaged(ev.PlayerIndex))
                mTopScoreManager.AddPotentialHost(SignedInGamer.SignedInGamers[ev.PlayerIndex]);
            if (mCurrentScene != null)
                mCurrentScene.ControllerEngaged(ev.PlayerIndex);
        }

        private void TransitionFromTrialMode()
        {
            if (mTopScoreManager == null)
            {
                mTopScoreManager = new TopScoreManager();
                mTopScoreManager.AddEventListener(TopScoreManager.CUST_EVENT_TYPE_ONLINE_SCORES_STOPPED, (SPEventHandler)OnOnlineScoresStopped);
            }

            ControlsManager cm = ControlsManager.CM;
            foreach (SignedInGamer gamer in SignedInGamer.SignedInGamers)
            {
                if (cm.HasControllerEngaged(gamer.PlayerIndex))
                    mTopScoreManager.AddPotentialHost(gamer);
            }
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            //mGraphics.GraphicsDevice.PresentationParameters.MultiSampleCount = 4;
            FileManager fm = FileManager.FM;
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_GLOBAL_SAVE_DEVICE_SELECTED, (SPEventHandler)OnGlobalSaveDeviceSelected);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_SELECTED, (PlayerIndexEventHandler)OnLocalSaveDeviceSelected);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_CANCELLED, (PlayerIndexEventHandler)OnLocalSaveDeviceCancelled);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, (PlayerIndexEventHandler)OnLocalLoadCompleted);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, (PlayerIndexEventHandler)OnLocalLoadFailed);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_COMPLETED, (PlayerIndexEventHandler)OnLocalSaveCompleted);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_FAILED, (PlayerIndexEventHandler)OnLocalSaveFailed);

#if XBOX || DEBUG || SYSTEM_LINK_SESSION
            Components.Add(new GamerServicesComponent(this));
#endif
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            try
            {
                mTextureManager.AddAtlas("7-Man-o'-War-railing-atlas.xml");
                mTextureManager.AddAtlas("8-Speedboat-railing-atlas.xml");
                mTextureManager.AddAtlas("achievements-atlas.xml");
                mTextureManager.AddAtlas("controls-atlas.xml");
                mTextureManager.AddAtlas("fancy-text-atlas.xml");
                mTextureManager.AddAtlas("gameover-atlas.xml");
                mTextureManager.AddAtlas("ghost-railing-atlas.xml");
                mTextureManager.AddAtlas("help-atlas.xml");
                mTextureManager.AddAtlas("masteries-atlas.xml");
                mTextureManager.AddAtlas("menu-atlas.xml");
                mTextureManager.AddAtlas("objectives-atlas.xml");
                mTextureManager.AddAtlas("playfield-atlas.xml");
                mTextureManager.AddAtlas("refraction-atlas.xml");
                mTextureManager.AddAtlas("refraction-sml-atlas.xml");
                mTextureManager.AddAtlas("refractables-atlas.xml");
                mTextureManager.AddAtlas("skirmish-atlas.xml");
                mTextureManager.AddAtlas("title-atlas.xml");
                mTextureManager.AddAtlas("uiview-atlas.xml");
                
#if IOS_SCREENS
                mTextureManager.AddAtlas("waves-atlas.xml");
#endif

                for (int i = 0; i < 4; ++i)
                    mTextureManager.AddAtlas("ocean" + i + "_N-atlas.xml");

                Dictionary<string, Effect> effects = new Dictionary<string, Effect>()
                {
                    { "AggregatePotion", Content.Load<Effect>("Effects/AggregatePotion") },
                    { "ColoredQuad", Content.Load<Effect>("Effects/ColoredQuad") },
                    { "Highlight", Content.Load<Effect>("Effects/Highlight") },
                    { "OceanShader", Content.Load<Effect>("Effects/OceanShader") },
                    { "Potion", Content.Load<Effect>("Effects/Potion") },
                    { "Refraction", Content.Load<Effect>("Effects/Refraction") },
                    { "RenderColoredQuad", Content.Load<Effect>("Effects/RenderColoredQuad") },
                    { "RenderTexturedQuad", Content.Load<Effect>("Effects/RenderTexturedQuad") },
                    { "SkyShader", Content.Load<Effect>("Effects/SkyShader") },
                    { "TexturedQuad", Content.Load<Effect>("Effects/TexturedQuad") },
                };

                foreach (KeyValuePair<string, Effect> kvp in effects)
                    mTextureManager.AddEffect(kvp.Key, kvp.Value);

                XACTAudioPlayer xaudioPlayer = new XACTAudioPlayer
                (
#if XBOX
                "XboxContent/XACT/awesome.xgs",
                "XboxContent/XACT/Wave Bank.xwb",
                "XboxContent/XACT/Sound Bank.xsb",
#else
                "WinContent/XACT/awesome.xgs",
                new string[] { "WinContent/XACT/Wave Bank.xwb" },
                new string[] { "WinContent/XACT/Sound Bank.xsb" },
#endif
 new string[] { "Default", "Music", "Sfx" },
                new string[]
                    {
                        "AbyssalBlast", "Achievement", "Ambience", "AshAbyssal", "AshMolten", "AshNoxious", "AshSavage", "BrandyPour",
                        "Button", "Camo", "CannonOverheat", "CrewCelebrate", "CrowdCheer", "Death", "Electricity", "Engine", "Explosion",
                        "Fire", "FlyingDutchman", "GhostlyTempest", "HandOfDavy", "Heartbeat", "KegDetonate", "KegDrop", "Locked",
                        "MenuAmbience", "MutinyFall", "MutinyRise", "NetCast", "NpcCannon", "PageTurn", "PlayerCannon", "PotionClink",
                        "ScreamMan", "ScreamWoman", "SeaOfLava", "SharkAttack", "ShipBurn", "SKPupHod", "SKPupKeg", "SKPupTempest",
                        "SKTreasure", "Splash", "Stamp", "StampLoud", "TownCannon", "Whirlpool"
                    }
                );
                AddXAudioPlayer(xaudioPlayer, "Playfield"); //mCurrentScene.SceneKey);

                mStage = new SPStage(
                    GraphicsDevice,
                    mTextureManager.EffectForKey("TexturedQuad"),
                    mTextureManager.EffectForKey("ColoredQuad"),
                    mGraphics.PreferredBackBufferWidth,
                    mGraphics.PreferredBackBufferHeight,
                    32768);
                mCamera = new SPCamera(new Vector2(mGraphics.GraphicsDevice.Viewport.Width, mGraphics.GraphicsDevice.Viewport.Height));
                mCamera.Update();

                // Splash Screen
                mSplashSprite = CreateSplashSprite();
                mSplashSprite.Alpha = 0;
                Stage.AddChild(mSplashSprite);
                mSplashJuggler = new SPJuggler(4);
                ShowSplashScreen(0.5f);
            }
            catch (Exception)
            {
#if WINDOWS
                ExitWithCode(kErrCodeMisssingFiles);
#endif
            }
                // Shed loading lag
                ResetElapsedTime();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            Content.Unload();
        }

        private void PerformPostSetup()
        {
            if (mCurrentScene != null)
                return;

            mCurrentScene = new PlayfieldController();
            mCurrentScene.SetupController();
            mCurrentScene.WillGainSceneFocus();
            mCurrentScene.AddToStageAtIndex(0);

            System.GC.Collect();

            // Shed loading lag
            ResetElapsedTime();
        }

        private SPSprite mSplashSprite;
        private SPJuggler mSplashJuggler;

        private SPSprite CreateSplashSprite()
        {
            // Splash
            SPSprite splashSprite = new SPSprite();

            SPQuad splashBg = new SPQuad(ResManager.RES_BACKBUFFER_WIDTH, ResManager.RES_BACKBUFFER_HEIGHT);
            splashBg.Color = Color.Black;
            splashSprite.AddChild(splashBg);

            SPImage splashImage = new SPImage(new SPTexture(Content.Load<Texture2D>("atlases/company-texture")));
            splashImage.X = -splashImage.Width / 2;
            splashImage.Y = -splashImage.Height / 2;

            SPSprite splashScaler = new SPSprite();
            splashScaler.X = splashBg.Width / 2;
            splashScaler.Y = splashBg.Height / 2;
            splashScaler.ScaleX = splashScaler.ScaleY = Math.Max(1f, ResManager.RESM.HudScale);
            splashScaler.AddChild(splashImage);
            splashSprite.AddChild(splashScaler);

            return splashSprite;
        }

        private void ShowSplashScreen(float fadeInDuration)
        {
            Debug.Assert(mSplashSprite != null && mSplashJuggler != null, "ShowSplashScreen: Splash component is null.");
            mSplashJuggler.RemoveTweensWithTarget(mSplashSprite);

            SPTween splashTweenIn = new SPTween(mSplashSprite, fadeInDuration, SPTransitions.SPEaseOut);
            splashTweenIn.AnimateProperty("Alpha", 1f);
            splashTweenIn.Delay = 0.5f;
            splashTweenIn.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnSplashScreenShown);
            mSplashJuggler.AddObject(splashTweenIn);
        }

        private void HideSplashScreen(float fadeOutDuration, float delay = 0f)
        {
            Debug.Assert(mSplashSprite != null && mSplashJuggler != null, "HideSplashScreen: Splash component is null.");
            mSplashJuggler.RemoveTweensWithTarget(mSplashSprite);

            SPTween splashTweenOut = new SPTween(mSplashSprite, fadeOutDuration);
            splashTweenOut.AnimateProperty("Alpha", 0f);
            splashTweenOut.Delay = delay;
            splashTweenOut.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnSplashScreenHidden);
            mSplashJuggler.AddObject(splashTweenOut);
        }

        private void OnSplashScreenShown(SPEvent ev)
        {
            mStartupCount = 2;
            PerformPostSetup();
        }

        private void OnSplashScreenHidden(SPEvent ev)
        {
            if (mSplashSprite != null)
            {
                mSplashSprite.RemoveFromParent();
                mSplashSprite.Dispose();
                mSplashSprite = null;
            }

            if (mCurrentScene != null)
                mCurrentScene.SplashScreenDidHide();
        }

        public int NextRandom(int max)
        {
            return NextRandom(0, max);
        }

        public int NextRandom(int min, int max)
        {
            if (mRandom != null)
                return mRandom.Next(min, max + 1);
            else
                return min;
        }

        private void ProcessSaveDeviceEvents()
        {
            Queue<PlayerIndexEvent> eventQ = null;

            if (s_GlobalDeviceSelected)
            {
                SPEvent ev = null;

                lock (s_lock)
                {
                    if (s_GlobalDeviceSelected)
                    {
                        if (s_GlobalDeviceEventQueue.Count > 0)
                            ev = s_GlobalDeviceEventQueue.Dequeue();
                        s_GlobalDeviceSelected = s_GlobalDeviceEventQueue.Count > 0;
                    }
                }

                if (ev != null)
                    GlobalSaveDeviceWasSelected(ev);
            }

            if (s_LocalDeviceSelected)
            {
                PlayerIndexEvent ev = null;

                lock (s_lock)
                {
                    if (s_LocalDeviceSelected)
                    {
                        eventQ = s_LocalEventQueues[kEventQDeviceSelected];
                        if (eventQ.Count > 0)
                            ev = eventQ.Dequeue();
                        s_LocalDeviceSelected = eventQ.Count > 0;
                    }
                }

                if (ev != null)
                    LocalSaveDeviceWasSelected(ev);
            }

            if (s_LocalDeviceCancelled)
            {
                PlayerIndexEvent ev = null;

                lock (s_lock)
                {
                    if (s_LocalDeviceCancelled)
                    {
                        eventQ = s_LocalEventQueues[kEventQDeviceCancelled];
                        if (eventQ.Count > 0)
                            ev = eventQ.Dequeue();
                        s_LocalDeviceCancelled = eventQ.Count > 0;
                    }
                }

                if (ev != null)
                    LocalSaveDeviceWasCancelled(ev);
            }

            if (s_LocalLoadCompleted)
            {
                PlayerIndexEvent ev = null;

                lock (s_lock)
                {
                    if (s_LocalLoadCompleted)
                    {
                        eventQ = s_LocalEventQueues[kEventQLoadCompleted];
                        if (eventQ.Count > 0)
                            ev = eventQ.Dequeue();
                        s_LocalLoadCompleted = eventQ.Count > 0;
                    }
                }

                if (ev != null)
                    LocalLoadCompleted(ev);
            }

            if (s_LocalLoadFailed)
            {
                PlayerIndexEvent ev = null;

                lock (s_lock)
                {
                    if (s_LocalLoadFailed)
                    {
                        eventQ = s_LocalEventQueues[kEventQLoadFailed];
                        if (eventQ.Count > 0)
                            ev = eventQ.Dequeue();
                        s_LocalLoadFailed = eventQ.Count > 0;
                    }
                }

                if (ev != null)
                    LocalLoadFailed(ev);
            }

            if (s_LocalSaveCompleted)
            {
                PlayerIndexEvent ev = null;

                lock (s_lock)
                {
                    if (s_LocalSaveCompleted)
                    {
                        eventQ = s_LocalEventQueues[kEventQSaveCompleted];
                        if (eventQ.Count > 0)
                            ev = eventQ.Dequeue();
                        s_LocalSaveCompleted = eventQ.Count > 0;
                    }
                }

                if (ev != null)
                    LocalSaveCompleted(ev);
            }

            if (s_LocalSaveFailed)
            {
                PlayerIndexEvent ev = null;

                lock (s_lock)
                {
                    if (s_LocalSaveFailed)
                    {
                        eventQ = s_LocalEventQueues[kEventQSaveFailed];
                        if (eventQ.Count > 0)
                            ev = eventQ.Dequeue();
                        s_LocalSaveFailed = eventQ.Count > 0;
                    }
                }

                if (ev != null)
                    LocalSaveFailed(ev);
            }
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (mSplashSprite != null)
            {
                Debug.Assert(mSplashJuggler != null, "Splash Juggler cannot be null during startup.");
                mSplashJuggler.AdvanceTime(gameTime.ElapsedGameTime.TotalSeconds);

                if (mStartupCount < 0)
                {
                    base.Update(gameTime);
                    return;
                }
                else if (mStartupCount > 0)
                {
                    if (--mStartupCount == 0)
                        HideSplashScreen(0.5f, 0.5f);
                }
            }

            ProcessSaveDeviceEvents();

            if (mDelayedCalls != null && mDelayedCalls.Count > 0 && (mCurrentScene == null || !mCurrentScene.IsModallyBlocked))
            {
                if (IsActive && !ProfileManager.PromptingIndex.HasValue)
                    ProcessDelayedCalls();
            }
            else if (IsActive)
                HandleInput(gameTime);
            else
            {
                if (mCurrentScene != null)
                    mCurrentScene.DisplayPauseMenu();
                ControlsManager.CM.StopAllGamePadVibrations();
            }

            double totalSeconds = gameTime.ElapsedGameTime.TotalSeconds;
            if (mTopScoreManager != null)
                mTopScoreManager.AdvanceTime(totalSeconds);

            if (FileManager.FM.HasFreshSavesQueued && mCurrentScene != null)
                mCurrentScene.SaveWillCommence();
            FileManager.FM.Update();

#if XBOX || DEBUG
            IsTrialMode = Guide.IsTrialMode;
#else
            IsTrialMode = false;
#endif

#if WINDOWS && DEBUG
            mCamera.Update();
#endif
            mStage.RenderSupport.ViewMatrix = mCamera.Transform;

#if WINDOWS
            if (!mExiting)
                mStage.ProcessTouches(ControlsManager.CM.MouseState, gameTime.TotalGameTime.TotalSeconds, mCamera.InverseTransform);
#endif
            foreach (KeyValuePair<string, AudioPlayer> kvp in mAudioPlayers)
                kvp.Value.AdvanceTime(totalSeconds);
            foreach (KeyValuePair<string, XACTAudioPlayer> kvp in mXAudioPlayers)
                kvp.Value.AdvanceTime(totalSeconds);

            if (!GameSettings.GS.HasPerformedInitialLoad && FileManager.FM.IsReadyGlobal())
            {
                GameSettings.GS.LoadSettings();
                ProfileManager.Initialize();
                ProfileManager.LoadHiScores();

                if (mCurrentScene != null)
                    mCurrentScene.ApplyGameSettings();
                if (MasteryManager != null && GameStats != null && GameStats.Masteries != null)
                {
                    MasteryManager.AddModel(MasteryManager.kDefaultMasteryModelKey, GameStats.Masteries);
                    MasteryManager.SetCurrentModel(0x1);
                    MasteryManager.RefreshMasteryBitmap();

                    if (AchievementManager != null)
                        AchievementManager.ResetCombatTextCache();
                }
            }

            if (!mExiting)
            {
                if (mCurrentScene != null)
                    mCurrentScene.AdvanceTime(totalSeconds);
                mStage.AdvanceTime(totalSeconds);
            }

            base.Update(gameTime);
        }

        private void HandleInput(GameTime gameTime)
        {
            if (mExiting)
                return;

            ControlsManager cm = ControlsManager.CM;
            cm.Update(gameTime);

            if (mCurrentScene != null)
                mCurrentScene.Update(cm.GamePadStateForPlayer(), cm.KeyboardState);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(mClearColor);
            mStage.Draw(gameTime, mStage.RenderSupport);

            base.Draw(gameTime);
        }

        public void PrepareForNewGame()
        {
            ProcessEndOfTurn();
            GameSaved = false;

            TimeKeeper.Reset();
            AchievementManager.PrepareForNewGame();
            ObjectivesManager.PrepareForNewGame();
            PlayerDetails.Reset();
            ThisTurn.PrepareForNewTurn();
            ThisTurn.InfamyMultiplier = ObjectivesManager.ScoreMultiplier;
        }

        public void SaveProgress()
        {
            if (AchievementManager != null)
                AchievementManager.SaveProgress();
        }

        public bool ProcessEndOfTurn()
        {
            bool didSaveProgress = false;

            if (GameSaved)
                return didSaveProgress;

            GameSaved = true;

            if (GameSettings.GS.DelayedSaveRequired)
                GameSettings.GS.SaveSettings();

            if (ThisTurn != null)
            {
                if (AchievementManager != null)
                    AchievementManager.SaveScore(ThisTurn.Infamy);

                if (ThisTurn.WasGameProgressMade)
                {
                    ThisTurn.WasGameProgressMade = false;

                    if (!ProfileManager.DidPlayerChooseNotToSave(ProfileManager.MainPlayerIndex))
                    {
                        SaveProgress();
                        didSaveProgress = true;
                    }

                    if (mTopScoreManager != null)
                    {
                        SignedInGamer gamer = SignedInGamer.SignedInGamers[ProfileManager.MainPlayerIndex];
#if SYSTEM_LINK_SESSION
                        if (gamer != null && !gamer.IsGuest)
#else
                        if (gamer != null && gamer.IsSignedInToLive && gamer.Privileges.AllowOnlineSessions && !gamer.IsGuest)
#endif
                        {
                            mTopScoreManager.AddScore(ThisTurn.Infamy, ProfileManager.GamerTag);
                            if (mTopScoreManager.ShouldSave)
                                mTopScoreManager.Save();
                        }
                    }
                }
            }

            return didSaveProgress;
        }

        public XACTAudioPlayer XAudioPlayerByName(string key)
        {
            if (key == null || mXAudioPlayers == null)
                return null;

            XACTAudioPlayer audioPlayer = null;
            if (mXAudioPlayers.ContainsKey(key))
                audioPlayer = mXAudioPlayers[key];
            return audioPlayer;
        }

        private void AddXAudioPlayer(XACTAudioPlayer audioPlayer, string key)
        {
            if (key != null && mXAudioPlayers != null)
                mXAudioPlayers[key] = audioPlayer;
        }

        public AudioPlayer AudioPlayerByName(string key)
        {
            if (key == null || mAudioPlayers == null)
                return null;

            AudioPlayer audioPlayer = null;
            if (mAudioPlayers.ContainsKey(key))
                audioPlayer = mAudioPlayers[key];
            return audioPlayer;
        }

        private void AddAudioPlayer(AudioPlayer audioPlayer, string key)
        {
            if (key != null && mAudioPlayers != null)
                mAudioPlayers[key] = audioPlayer;
        }

        protected void RemoveAudioPlayerByName(string key)
        {
            if (key != null && mAudioPlayers != null)
                mAudioPlayers.Remove(key);
        }

        protected void DestroyAudioPlayer(AudioPlayer audioPlayer)
        {
            if (audioPlayer != null)
            {
                audioPlayer.RemoveAllSounds();
                audioPlayer.DestroyAudioPlayer();
            }
        }

        protected void DestroyAndRemoveAudioPlayerByName(string key)
        {
            AudioPlayer audioPlayer = AudioPlayerByName(key);
            DestroyAudioPlayer(audioPlayer);
            RemoveAudioPlayerByName(key);
        }

        protected void DestroyAndRemoveAllAudioPlayers()
        {
            foreach (KeyValuePair<string, AudioPlayer> kvp in mAudioPlayers)
                DestroyAudioPlayer(kvp.Value);
            foreach (AudioPlayer audioPlayer in mAudioPlayerDump)
                DestroyAudioPlayer(audioPlayer);
            mAudioPlayerDump.Clear();
        }

        public void MarkAudioPlayerForDestructionByName(string key)
        {
            AudioPlayer audioPlayer = AudioPlayerByName(key);

            if (audioPlayer != null)
            {
                mAudioPlayerDump.Add(audioPlayer);
                mAudioPlayers.Remove(key);
                audioPlayer.FadeAndMarkForDestruction();
            }
        }

        public bool AddDelayedCall(Action action)
        {
            if (mDelayedCalls == null)
                mDelayedCalls = new List<Action>(5);

            // Only allow a single delayed call for now.
            if (mDelayedCalls.Count == 0)
            {
                mDelayedCalls.Add(action);
                return true;
            }
            else
                return false;
        }

        public void ProcessDelayedCalls()
        {
            if (mDelayedCalls != null)
            {
                // Force ownership of events to main player
                ControlsManager.CM.PrevQueryPlayerIndex = ProfileManager.MainPlayerIndex;

                // Call delayed events
                foreach (Action action in mDelayedCalls)
                    action();

                mDelayedCalls.Clear();
            }
        }

        public void PurgeDelayedCalls()
        {
            if (mDelayedCalls != null)
                mDelayedCalls.Clear();
        }

        public void CacheResourceForKey(object resource, string key)
        {
            if (mCachedResources == null)
                return;

            if (resource == null && key == null)
            {
                mCachedResources.Clear();
                return;
            }

            if (resource == null)
            {
                if (mCachedResources.ContainsKey(key))
                    mCachedResources.Remove(key);
            }
            else
                mCachedResources.Add(key, resource);
        }

        public object CachedResourceForKey(string key)
        {
            if (key == null || mCachedResources == null)
                return null;

            object resource = null;
            if (mCachedResources.ContainsKey(key))
                resource = mCachedResources[key];
            return resource;
        }

        public void SetupHighlightEffecter(SPDisplayObject displayObject)
        {
            if (mCurrentScene != null)
            {
                Effect effect = mCurrentScene.EffectForKey("Highlight");

                if (effect != null)
                {
                    if (displayObject is MenuButton)
                    {
                        MenuButton button = displayObject as MenuButton;
                        button.SelectedEffecter = new SPEffecter(effect, HighlightDraw);
                    }
                    else
                        displayObject.Effecter = new SPEffecter(effect, HighlightDraw);
                }
            }
        }

        public void HighlightDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (mCurrentScene != null && mCurrentScene.CustomHudDrawer != null)
            {
                mCurrentScene.CustomHudDrawer.HighlightDraw(displayObject, gameTime, support, parentTransform);
                support.EndBatch();
                support.BeginBatch();
            }
        }

        public void PotionDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (mCurrentScene != null && mCurrentScene.CustomHudDrawer != null)
            {
                mCurrentScene.CustomHudDrawer.RefractionFactor = 0.1f;
                mCurrentScene.CustomHudDrawer.PotionDrawSml(displayObject, gameTime, support, parentTransform);
            }
        }

        public void AggregatePotionDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (mCurrentScene != null && mCurrentScene.CustomHudDrawer != null)
            {
                mCurrentScene.CustomHudDrawer.RefractionFactor = 0.1f;
                mCurrentScene.CustomHudDrawer.AggregatePotionDrawSml(displayObject, gameTime, support, parentTransform);
            }
        }

        private static readonly Vector4 kPoolDisplacementFactor = new Vector4(0.2f, 0.15f, 0.2f, 0.15f);
        public void PoolDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (mCurrentScene != null && mCurrentScene.CustomHudDrawer != null)
            {
                mCurrentScene.CustomDrawer.RefractionFactor = 0.125f;
                mCurrentScene.CustomDrawer.DisplacementFactor = kPoolDisplacementFactor;
                mCurrentScene.CustomDrawer.RefractionDrawSml(displayObject, gameTime, support, parentTransform);
            }
        }

        private static readonly Vector4 kBloodDisplacementFactor = new Vector4(0.2f, 0.15f, 0.2f, 0.15f);
        public void BloodDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            if (mCurrentScene != null && mCurrentScene.CustomHudDrawer != null)
            {
                mCurrentScene.CustomDrawer.RefractionFactor = 0.1f;
                mCurrentScene.CustomDrawer.DisplacementFactor = kBloodDisplacementFactor;
                mCurrentScene.CustomDrawer.RefractionDrawSml(displayObject, gameTime, support, parentTransform);
            }
        }

        public const uint kErrCodeMisssingFiles = 1001;
        public void ExitWithCode(uint errCode)
        {
#if WINDOWS
            string text = null, caption = null;
            switch (errCode)
            {
                case kErrCodeMisssingFiles:
                    text = "Either replace any missing files in the application's directory or reinstall the application.\nProgram will now exit.";
                    caption = "Missing files detected!";
                    break;
            }

            if (text != null && caption != null)
            {
                MessageBox.Show(text, caption, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
#endif
            Exit();
        }

        public void OnExitButtonPressed(SPEvent ev)
        {
            if (mCurrentScene != null && mCurrentScene.IsExitViewAvailable)
                mCurrentScene.DisplayExitView();
        }

        private void OnOnlineScoresStopped(SPEvent ev)
        {
            if (mExiting)
            {
                if (mTopScoreManager != null && mTopScoreManager.ShouldSave)
                    mTopScoreManager.Save(false);
                Exit();
            }
            else if (mCurrentScene != null)
                mCurrentScene.OnlineScoresStopped();
        }

        public void BeginExit()
        {
            if (mTopScoreManager != null)
            {
                if (!mTopScoreManager.IsStopped)
                {
                    mExiting = true;
                    mTopScoreManager.Stop(true);
                    return;
                }
                else if (mTopScoreManager.ShouldSave)
                    mTopScoreManager.Save(false);
            }

            Exit();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
            Destroy();
        }

        protected void Destroy()
        {
            if (!mIsDestroyed)
            {
                try
                {
                    mGraphics.PreparingDeviceSettings -= Graphics_PreparingDeviceSettings;
                    //MediaPlayer.MediaStateChanged -= MediaPlayer_MediaStateChanged;
                    //MediaPlayer.ActiveSongChanged -= MediaPlayer_ActiveSongChanged;

                    MenuButton.EffecterSetup = null;
                    ControlsManager.CM.RemoveEventListener(ControlsManager.CUST_EVENT_TYPE_DEFAULT_CONTROLLER_DISCONNECTED, (SPEventHandler)OnDefaultControllerDisconnected);
                    ControlsManager.CM.RemoveEventListener(ControlsManager.CUST_EVENT_TYPE_CONTROLLER_DID_ENGAGE, (PlayerIndexEventHandler)OnControllerEngaged);
                    ControlsManager.CM.RemoveEventListener(ControlsManager.CUST_EVENT_TYPE_EXIT_BUTTON_PRESSED, (SPEventHandler)OnExitButtonPressed);

                    FileManager fm = FileManager.FM;
                    fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_GLOBAL_SAVE_DEVICE_SELECTED, (SPEventHandler)OnGlobalSaveDeviceSelected);
                    fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_SELECTED, (PlayerIndexEventHandler)OnLocalSaveDeviceSelected);
                    fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_CANCELLED, (PlayerIndexEventHandler)OnLocalSaveDeviceCancelled);
                    fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, (PlayerIndexEventHandler)OnLocalLoadCompleted);
                    fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, (PlayerIndexEventHandler)OnLocalLoadFailed);
                    fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_COMPLETED, (PlayerIndexEventHandler)OnLocalSaveCompleted);
                    fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_FAILED, (PlayerIndexEventHandler)OnLocalSaveFailed);
                    fm.Destroy();
                    fm = null;

                    if (mCurrentScene != null)
                    {
                        mCurrentScene.DestroyScene();
                        mCurrentScene.Dispose();
                        mCurrentScene = null;
                    }

                    if (mStage != null)
                    {
                        mStage.Dispose();
                        mStage = null;
                    }

                    if (mTextureManager != null)
                    {
                        mTextureManager.Dispose();
                        mTextureManager = null;
                    }

                    if (mObjectivesManager != null)
                    {
                        mObjectivesManager.Dispose();
                        mObjectivesManager = null;
                    }

                    if (mAchievementManager != null)
                    {
                        if (mAchievementManager.ProfileManager != null)
                        {
                            mAchievementManager.ProfileManager.RemoveEventListener(GameStats.CUST_EVENT_TYPE_PLAYER_CHANGED, (SPEventHandler)OnPlayerChanged);
                            mAchievementManager.ProfileManager.RemoveEventListener(ProfileManager.CUST_EVENT_TYPE_PLAYER_LOGGED_IN, (PlayerIndexEventHandler)OnPlayerLoggedIn);
                            mAchievementManager.ProfileManager.RemoveEventListener(ProfileManager.CUST_EVENT_TYPE_PLAYER_LOGGED_OUT, (PlayerIndexEventHandler)OnPlayerLoggedOut);
                            mAchievementManager.ProfileManager.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, (PlayerIndexEventHandler)OnLocalLoadCompleted);
                            mAchievementManager.ProfileManager.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, (PlayerIndexEventHandler)OnLocalLoadFailed);
                        }
                        mAchievementManager.Dispose();
                        mAchievementManager = null;
                    }

                    if (mTopScoreManager != null)
                    {
                        mTopScoreManager.RemoveEventListener(TopScoreManager.CUST_EVENT_TYPE_ONLINE_SCORES_STOPPED, (SPEventHandler)OnOnlineScoresStopped);
                        mTopScoreManager = null;
                    }

                    if (mAudioPlayers != null)
                    {
                        DestroyAndRemoveAllAudioPlayers();
                        mAudioPlayers = null;
                    }

                    if (mXAudioPlayers != null)
                    {
                        foreach (KeyValuePair<string, XACTAudioPlayer> kvp in mXAudioPlayers)
                            kvp.Value.Dispose();
                        mXAudioPlayers = null;
                    }

                    if (mThisTurn != null)
                    {
                        mThisTurn.Dispose();
                        mThisTurn = null;
                    }

                    if (mPlayerDetails != null)
                    {
                        mPlayerDetails.Cleanup();
                        mPlayerDetails = null;
                    }

                    UnloadContent();

                    mDelayedCalls = null;
                    mCachedResources = null;
                    mAudioPlayerDump = null;
                    mCamera = null;
                    mTimeKeeper = null;
                    mGraphics = null;
                    mPlayerShip = null;
                    mAiKnob = null;
                    mRandom = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
                finally
                {
                    mIsDestroyed = true;
                }
            }
        }
        #endregion
    }
}




#region Sparrow Usage Examples
// Sparrow usage examples
#if false
using MyImage = SparrowXNA.SPImage;
            // Create a new SpriteBatch, which can be used to draw textures.
            mStage = new SPStage(mGraphics.PreferredBackBufferWidth, mGraphics.PreferredBackBufferHeight, GraphicsDevice);
            mCamera = new SPCamera(new Vector2(mGraphics.GraphicsDevice.Viewport.Width, mGraphics.GraphicsDevice.Viewport.Height));
#if false
            AtlasData atlasData = Content.Load<AtlasData>("atlases/playfield-atlas");
            Texture2D atlasTexture2D = Content.Load<Texture2D>(atlasData.ImagePath);
            SPTextureAtlas atlas = new SPTextureAtlas(atlasData, new SPTexture(atlasTexture2D));

            SPTexture texture = atlas.TextureByName("7-Man-o'-War-helm");
            MyImage image = new MyImage(texture);
            image.Origin = new Vector2(400, 250);
            image.Pivot = new Vector2(image.Width / 2, image.Height / 2);
            mStage.AddChild(image);

            //Debug.WriteLine("Width: {0} Height: {1}", image.Width, image.Height);
            //Debug.WriteLine(new Vector2(image.Width / 2, image.Height / 2).ToString());

            SPTween tween = new SPTween(image, 2f);
            tween.AnimateProperty("Rotation", SPMacros.SP_D2R(360f));
            tween.Loop = SPLoopType.Repeat;
            mStage.Juggler.AddObject(tween);

            texture = atlas.TextureByName("7-Perisher-barrel");
            image = new MyImage(texture);
            image.Origin = new Vector2(600, 400);
            mStage.AddChild(image);

            List<SPTexture> abyssalFrames = atlas.TexturesStartingWith("abyssal-shot_");

            SPMovieClip abyssalClip = new SPMovieClip(abyssalFrames, 12f);
            abyssalClip.Origin = new Vector2(100, 48);
            mStage.AddChild(abyssalClip);
            mStage.Juggler.AddObject(abyssalClip);

#else
            SpriteFont font = Content.Load<SpriteFont>("CheekyFont1");
            SPTextField.RegisterFont("MyFont", 64, font);

            Texture2D hauntTex = Content.Load<Texture2D>("images/haunt");
            SPTexture hauntSPTex = new SPSubTexture(new SPRectangle(512, 640, 1024, 768), new SPTexture(hauntTex));

            MyImage hauntImage = new MyImage(hauntSPTex);
            mStage.AddChild(hauntImage);

#if true
            mStage.Juggler.DelayInvocation(this, 2.0, delegate
            {
                hauntSPTex = new SPSubTexture(new SPRectangle(256, 192, 512, 384), hauntSPTex);

                SPSprite sprite = new SPSprite();
                //sprite.Origin = new Vector2(-1 * 512, -1 * 384);
                //sprite.Scale = new Vector2(2f, 2f);
                //sprite.Pivot = new Vector2(512, 384);
                //sprite.Rotation = SPMacros.SP_D2R(22.5f);
                mStage.AddChild(sprite);

                Texture2D potTex = Content.Load<Texture2D>("images/potion-pot");
                //SPTexture cauldronTexture = new SPSubTexture(new SPRectangle(0, 0, 320, 348), new SPTexture(hauntTex));
                SPTexture cauldronTexture = new SPTexture(potTex);
                cauldronTexture.Repeat = true;
                SPQuad cauldronImage = new SPQuad(cauldronTexture);
#if false
                cauldronImage.Repeat = true;
#else
                float widAdjust = 2, hgtAdjust = 1;
                cauldronImage.SetTexCoord(new Vector2(widAdjust, 0), 1);
                cauldronImage.SetTexCoord(new Vector2(0, hgtAdjust), 2);
                cauldronImage.SetTexCoord(new Vector2(widAdjust, hgtAdjust), 3);
#endif
                cauldronImage.Width *= widAdjust;
                cauldronImage.Height *= hgtAdjust;
                cauldronImage.Origin = new Vector2(320, 370);
                sprite.AddChild(cauldronImage);

                MyImage hauntImageSmall = new MyImage(hauntSPTex);
                hauntImageSmall.X = 512;
                hauntImageSmall.Y = 384;
                hauntImageSmall.Pivot = new Vector2(512 / 2, 384 / 2);
                hauntImageSmall.Scale = new Vector2(0.75f, 0.75f);
                sprite.AddChild(hauntImageSmall);

                SPTween tween = new SPTween(hauntImageSmall, 2.5f);
                tween.AnimateProperty("Rotation", SPMacros.SP_D2R(360f));
                tween.Loop = SPLoopType.Repeat;
                mStage.Juggler.AddObject(tween);

                tween = new SPTween(hauntImageSmall, 1f);
                tween.AnimateProperty("ScaleX", 0.1f);
                tween.AnimateProperty("ScaleY", 0.1f);
                tween.AnimateProperty("Alpha", 0.5f);
                tween.Loop = SPLoopType.Reverse;
                mStage.Juggler.AddObject(tween);

                SPTextField textField = new SPTextField("Cheeky Mammoth", "MyFont", 48);
                textField.Origin = new Vector2(50, 50);
                textField.Rotation = SPMacros.SP_D2R(22.5f);
                //textField.Scale = new Vector2(0.75f, 0.75f);
                textField.Color = Color.LightBlue;
                sprite.AddChild(textField);

                textField = new SPTextField("Cheeky Mammoth", "MyFont");
                textField.Origin = new Vector2(50, 50);
                textField.Rotation = SPMacros.SP_D2R(45f);
                //textField.Scale = new Vector2(0.75f, 0.75f);
                textField.Color = Color.LightBlue;
                sprite.AddChild(textField);

                AtlasData atlasData = Content.Load<AtlasData>("playfield-atlas");
                Texture2D atlasTexture2D = Content.Load<Texture2D>(atlasData.ImagePath);
                SPTextureAtlas atlas = new SPTextureAtlas(atlasData, new SPTexture(atlasTexture2D));

                SPTexture buttonTexture = atlas.TextureByName("achievements-button");
                SPButton button = new SPButton(buttonTexture);
                button.Origin = new Vector2(700, 100);
                sprite.AddChild(button);

                SPButton button2 = new SPButton(buttonTexture);
                button2.Origin = new Vector2(700, 300);
                sprite.AddChild(button2);

#if true
                Delegate del = new SPEventHandler(delegate(SPEvent ev)
                {
                    SPButton targetButton = ev.Target as SPButton;
                    targetButton.Color = (targetButton.Color == Color.White) ? Color.Red : Color.White;
                    //cauldronImage.Color = (cauldronImage.Color == Color.White) ? Color.Red : Color.White;
                });

                button.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, del);
                button2.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, del);
                //button.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, del);
#else
                
                // Is this dangerous? Do we need a strong reference to anonymous callbacks?
                // See comments on this blog: http://jacksondunstan.com/articles/335
                button.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, new SPEventHandler(delegate(SPEvent ev)
                {
                    cauldronImage.Color = (cauldronImage.Color == Color.White) ? Color.Red : Color.White;
                }));

                //button.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED);
#endif


                /*
                BasicEffect effect = new BasicEffect(GraphicsDevice);
                effect.World = Matrix.Identity;
                effect.View = Matrix.Identity;


                // Can we ask the Stage for the default projection, world and view matrices to make this process easier?
                Matrix projection = Matrix.CreateOrthographicOffCenter(0, 1024, 768, 0, 0, 1);
                Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
                effect.Projection = halfPixelOffset * projection;

                effect.TextureEnabled = true;
                effect.Texture = null;
                effect.Alpha = 0.5f;
                effect.VertexColorEnabled = true; // This seems like a fairly rare requirement. Should it be false for efficiency?
                effect.LightingEnabled = false;

                // Add new one to bottom of stack
                button.Effecter = new SPBasicEffecter(effect, null);
                */


                SPRenderTexture renderTexture = null;

                for (int i = 0, j = 0; i < 0; ++i)
                {
                    mStage.Juggler.DelayInvocation(this, 1.3 + i * 1.2, delegate
                    {
                        SPDisplayObject renderObject = sprite;
                        /*
                        SPTextField textField = new SPTextField("Cheeky Mammoth", "MyFont");
                        textField.Origin = new Vector2(50, 50);
                        textField.Scale = new Vector2(0.75f, 0.75f);
                        textField.Color = Color.LightBlue;
                        sprite.AddChild(textField);
                        */
                        SPRectangle bounds = renderObject.Bounds;

                        //Debug.WriteLine(bounds.ToString());

                        //if (renderTexture == null)
                            renderTexture = new SPRenderTexture(GraphicsDevice, bounds.Width, bounds.Height); // Cropped to origin

                        //SPRenderTexture renderTexture = new SPRenderTexture(GraphicsDevice, bounds.X + bounds.Width, bounds.Y + bounds.Height); // Not cropped
#if true
                        renderTexture.BundleDrawCalls(delegate(SPRenderSupport support)
                        {
                            

                            //renderObject.Draw(null, support, Matrix.CreateTranslation(-bounds.X, -bounds.Y , 0) * Matrix.CreateScale(0.5f, 0.5f, 1f));
                            renderObject.Draw(null, support, Matrix.CreateTranslation(-bounds.X, -bounds.Y, 0));
                            //renderObject.Draw(null, support,  Matrix.CreateScale(0.35f, 0.35f, 1f));
                            //renderObject.Draw(null, support, Matrix.Identity);

                            /*
                            SpriteBatch sb = new SpriteBatch(GraphicsDevice);
                            sb.Begin();
                            sb.DrawString(font, "Cheeky Mammoth", new Vector2(50, 50), Color.LightBlue, 0f, Vector2.Zero, new Vector2(0.75f, 0.75f), SpriteEffects.None, 0f);
                            sb.End();
                             * */
                        });
#else

                        renderTexture.DrawObject(renderObject);
#endif
                        MyImage renderImage = new MyImage(renderTexture.SPTexture);
                        renderImage.Origin = new Vector2(j * 30, j * 15);
                        sprite.AddChild(renderImage);

                        ++j;
                    });
                }
            });
#endif
#endif
            // Next steps:
                    // 2. FIXME: Can't use RenderTarget2D as a Texture2D while it is set as the device's target (throws Exception). So BundleDrawCalls and DrawObject will have to use
                    //    new RenderTarget2D's and return them as SPTextures so that people can't accidentally break things (they have to explicitly cast to potentially break them).
                    //    However, this fix is slow for repeated Rendering. Should we try a Begin/End batching system?
                    //  2.1 Texture Filtering options, MipMap settings etc in SPRenderSupport.
                        // Possible problems: http://forums.create.msdn.com/forums/p/74891/455775.aspx
                        // Built-in States (good): http://blogs.msdn.com/b/shawnhar/archive/2010/04/02/state-objects-in-xna-game-studio-4-0.aspx
                        // Filtering: http://blogs.msdn.com/b/shawnhar/archive/2009/09/08/texture-filtering.aspx
                    // 4. SPSound (SPMovieClip frame sounds)
                        // XACT (might add support for XACT later, if needed): http://msdn.microsoft.com/en-us/library/ff827592
                        // Sound compression with XACT: http://blogs.msdn.com/b/mitchw/archive/2007/04/27/audio-compression-using-xact.aspx
                        // Concurrent Sound limit: http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.instanceplaylimitexception

            // SoundEffect, SoundEffectInstance (I think similar to SPChannel), DynamicSoundEffectInstance, SoundEffectProcessor
                        // 3D: AudioEmitter, AudioListener


            //
            //
            // Main Classes: SoundEffect, SoundEffectInstance, MediaPlayer, Song
            //
            //
                    
                    // 4. Must detect display mode for back buffer dimensions: http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.graphics.graphicsdevice.displaymode.aspx
                    // 5. Vector Fonts: https://devel.nuclex.org/framework/wiki/VectorFonts
                    // 6. Gamepad Input
                        // http://msdn.microsoft.com/en-us/library/bb203899
                    // 8. Console tool to convert Sparrow texture atlas format to XNA format.
                    // 9. Shader Tutorials: http://rbwhitaker.wikidot.com/hlsl-tutorials
                        // 9.1: http://digitalerr0r.wordpress.com/tutorials/
                        // Keywords: http://msdn.microsoft.com/en-us/library/bb509647(VS.85).aspx
                        // Intrinsic Functions: http://msdn.microsoft.com/en-us/library/ff471376(v=vs.85).aspx
                    // 12. Should we be generating mipmaps in Properties window of the project's Contents, or do we do that at run-time?
                    // 13. Resizing, Aspect Ratio and other tips: http://msdn.microsoft.com/en-us/library/bb203873.aspx
                    // 14. Use Service Providers in your game code to decouple. E.g. Most of the methods in the Scene class could be registered as a service. Then
                    //     consumers could simply call Game.Services.GetService(typeOf(TextureProvider)).TextureByName("name"). Of course, this service provider
                    //     could have its interface wrapped to sweeten the syntax to: CCService.TextureByName("name"). Now the service provider can change and the
                    //     consumer won't know or care about the change: http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.game.services.aspx
                    // 15. Save game: http://msdn.microsoft.com/en-us/library/bb199073
                        // Karvonite: http://www.karvonite.com/
                        // BinaryFormatter: http://stackoverflow.com/questions/1299071/serializing-net-dictionary
                    // 16. Local High Score: http://create.msdn.com/en-US/education/catalog/starterkit/ninjacademy
                    // 17. Multiplayer (XBOX): http://msdn.microsoft.com/en-us/library/bb975801
                        // http://msdn.microsoft.com/en-us/library/bb975645(v=xnagamestudio.31)
                    // Game:
                        // 1. Lower pitch on sounds when game slows. Change pitch slightly for cannon fires, splashes and explosions.
                        // 2. FMOD: http://www.youtube.com/watch?v=qPhMWf3j_ZA
                        // 3. Drag finger in water effect for ship wakes.
                        // 4. Indie Distributors: http://blog.wolfire.com/2009/01/indie-friendly-online-distributors/
                        // 5. XNA redistributable fonts from Microsoft: http://msdn.microsoft.com/en-us/library/bb447673(v=xnagamestudio.40)
                        // 6. Wake Shader: http://www.emanueleferonato.com/2011/01/19/creation-of-realistic-flash-water-ripples-with-as3/

                    // 19. Water:
                        // Shader Tutorial: http://create.msdn.com/en-US/education/talks/2010_01
                        // i. Using 3D vertex and pixel shaders for water (get one of the good ones).
                        // ii. Increase view distance and place grid of animating water vertices far enough back on the Z-plane so that crests don't overlap 2D sections of view.
                        // iii. Orthogonal projection will clamp remove any perceivable Z distance between the water and the ships/land when rendered.
                        // iv. This prevents having to render the water to texture each frame.
                        // Check out Pirates: Sea Battle 2 on App Store for translucent water with reefs below it.
                    // 20. Add flickering point light for Cove flame. Add 2D shadows for static objects. Add bloom for sunrise/sunset/moonlight. Add cabin glow point lights.
                    // 21. TV Resolutions: http://msdn.microsoft.com/en-us/library/bb203938.aspx
                        // May have to scale Ricochet Cone based on playfield dimensions so large screens with similar NpcShip paths don't get too few ricochets.
        
                    // 21. Port XNA code to iOS/Android http://andrewrussell.net/exen/

                // BUGS:
                    // 1. Render textures corrupt when moving window between screens. Just replace with full texture scrolls. Symptom of other problems?
                    // 2. Remove resx language files from EasyStorage, otherwise we have to support those languages throughout the game to be accepted.
                    // 3. 

                // NOTES:
                    // 1. Connect to XBox360: http://msdn.microsoft.com/en-us/library/bb975643.aspx
                    // 2. Submission Checklist: http://xboxforums.create.msdn.com/forums/p/54108/328473.aspx#328473

#endif
#endregion