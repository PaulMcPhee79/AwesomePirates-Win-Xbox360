//#define TEST_GUEST_BUG

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using SparrowXNA;

namespace AwesomePirates
{
    class SceneController : IDisposable
    {
        public const string RESOURCE_CACHE_COMBAT_TEXT = "ResCacheCombatText";
        public const string RESOURCE_CACHE_SKCOMBAT_TEXT = "ResCacheSKCombatText";
        public const string RESOURCE_CACHE_MANAGERS = "ResCacheManagers";
        public const uint PRIZE_STATS = 0x1;

        public const float kSafeAreaMin = 0.8f;
        public const float kSafeAreaMax = 1f;
        public const int kSafeAreaMaxIncrements = 12;
        public const float kSafeAreaDecrement = 0.2f / kSafeAreaMaxIncrements;
        public const float kSafeAreaIncrement = 0.205f / kSafeAreaMaxIncrements; // .005 overflow ensures we don't stop just short of min or max.

        public SceneController()
        {
            mHasPauseMenu = false;
            mScenePaused = false;
            mSettingsApplied = false;
            mFlipped = false;
            mAmbienceShouldPlay = false;
            mIsExitViewAvailable = false;
            mTimeSlowed = false;
            mLocked = false;
            mDestructLock = false;

            mPauseMenu = null;
            mPauseFrame = null;
            mPauseButtonsProxy = null;
            mFlipControlsButton = null;
            mSpriteLayerManager = null;
            mCacheManagers = null;
            mCustomDrawer = null;
            mCustomHudDrawer = null;

            mSKManager = new SKManager();
            mGameModeManager = new GameModeManager();
            mInputManager = new InputManager();

            mViewWidth = ResManager.RESM.Width;
            mViewHeight = ResManager.RESM.Height;
            mSafeAreaFactor = 1f;

            // Actors
            mActors = new List<Actor>(60);
            mAdvActors = new List<Actor>(20);
            mActorsAddQueue = new List<Actor>(10);
            mActorsRemoveQueue = new List<Actor>(10);

            // Props
            mProps = new List<Prop>(100);
            mAdvProps = new List<Prop>(50);
            mPropsAddQueue = new List<Prop>(10);
            mPropsRemoveQueue = new List<Prop>(10);
            
            // Jugglers
            mJuggler = new SPJuggler(256);
            mSpamJuggler = new SPJuggler(256);
            mHudJuggler = new SPJuggler(64);
            mPauseJuggler = new SPJuggler(32);
            mSpecialJuggler = new SPJuggler(32);

            // Base Sprite
            //mBaseSprite = new CroppedProp(0, new Rectangle(0, 0, ResManager.RES_BACKBUFFER_WIDTH, ResManager.RES_BACKBUFFER_HEIGHT));
            mBaseSprite = new CroppedProp(0, new Rectangle(0, 0, (int)mViewWidth, (int)mViewHeight));
            mBaseSprite.ScaleX = ResManager.RESM.ScaleX;
            mBaseSprite.ScaleY = ResManager.RESM.ScaleY;
        }

        #region Fields
        private bool mIsDisposed = false;
        protected bool mHasPauseMenu;
        protected bool mScenePaused;
        protected bool mSettingsApplied;
        protected bool mAmbienceShouldPlay;
        protected bool mIsExitViewAvailable;
        protected bool mTimeSlowed;
        protected bool mFlipped;
        protected bool mLocked;
        protected bool mDestructLock;

        protected string mSceneKey;

        private float mViewWidth;
        private float mViewHeight;
        private float mSafeAreaFactor;

        protected CroppedProp mBaseSprite;

        // Gamer Picture
        private SPImage mGamerPicture;
        private Prop mGamerPictureProp;

        // Guide
        protected GuideProp mGuideProp;

        // Exit/Purchase Menu
        protected ExitView mExitView;

        // Pause menu
        private MenuButton mPauseButton;
        private MenuButton mQuitButton;
        private MenuButton mResumeButton;
        private MenuButton mRetryButton;
        private MenuButton mFlipControlsButton;

        protected SKTallyView mSKTallyView;
        private Prop mPauseProp;
        private Prop mPauseMenu;
        private SPSprite mPauseFrame;
        private ButtonsProxy mPauseButtonsProxy;
        private SPJuggler mPauseJuggler;
        private SPJuggler mSpecialJuggler;

        protected CustomDrawer mCustomDrawer;
        protected CustomDrawer mCustomHudDrawer;

        protected List<Actor> mActors;
        protected List<Actor> mAdvActors;
        protected List<Actor> mActorsAddQueue;
        protected List<Actor> mActorsRemoveQueue;

        protected List<Prop> mProps;
        protected List<Prop> mAdvProps;
        protected List<Prop> mPropsAddQueue;
        protected List<Prop> mPropsRemoveQueue;

        protected SPJuggler mJuggler;
        protected SPJuggler mSpamJuggler;
        protected SPJuggler mHudJuggler;

        protected List<string> mContentKeys;
        protected SKManager mSKManager;
        protected GameModeManager mGameModeManager;
        protected InputManager mInputManager;
        protected SpriteLayerManager mSpriteLayerManager;
        protected Dictionary<uint, CacheManager> mCacheManagers;
        #endregion

        #region Properties
        public bool IsMusicMuted { get { return GameSettings.GS.ValueForKey(GameSettings.MUSIC_VOLUME) == 0; } }
        public bool IsExitViewAvailable { get { return mIsExitViewAvailable; } set { mIsExitViewAvailable = value; } }
        public virtual bool IsModallyBlocked { get { return false; } }
        public string SceneKey { get { return mSceneKey; } }
        public string FontKey { get { return mSceneKey + Globals.CC_FONT_NAME; } }
        public static string LeaderboardFontKey { get { return "LBFont"; } }
        public virtual bool TouchableDefault { get { return true; } }
        public bool Flipped { get { return mFlipped; } }
        public float Fps { get { return GameController.GC.Fps; } }
        public float ViewWidth { get { return mViewWidth; } }
        public float ViewHeight { get { return mViewHeight; } }
        public float ViewScale { get { return (mBaseSprite != null) ? mBaseSprite.ScaleX : 1; } }
        public float ViewAspectRatio { get { return mViewWidth / mViewHeight; } }
        public Vector2 GuidePropDimensions
        {
            get
            {
                if (mGuideProp == null)
                    CreateGuideProp();
                return new Vector2(mGuideProp.Width, mGuideProp.Height);
            }
        }
        public SPJuggler Juggler { get { return mJuggler; } }
        public SPJuggler SpamJuggler { get { return mSpamJuggler; } }
        public SPJuggler HudJuggler { get { return mHudJuggler; } }
        public SPJuggler PauseJuggler { get { return mPauseJuggler; } }
        public SPJuggler SpecialJuggler { get { return mSpecialJuggler; } }
        public virtual double TimeSlowedFactor { get { return 0.2; } }
        public virtual int TopCategory { get { return 0; } }
        public virtual int HelpCategory { get { return 0; } }
        public virtual int PauseCategory { get { return 0; } }
        public virtual uint AllPrizesBitmap { get { return 0; } }
        public PlayerIndex MainPlayerIndex { get { return GameController.GC.ProfileManager.MainPlayerIndex; } }
        public SKManager SKManager { get { return mSKManager; } }
        public virtual GameMode GameMode { get { return mGameModeManager.Mode; } set { mGameModeManager.Mode = value; } }
        protected TextureManager TM { get { return GameController.GC.TextureManager; } }
        public ObjectivesManager ObjectivesManager { get { return GameController.GC.ObjectivesManager; } }
        public AchievementManager AchievementManager { get { return GameController.GC.AchievementManager; } }
        public MasteryManager MasteryManager { get { return GameController.GC.MasteryManager; } }
        public SpriteLayerManager SpriteLayerManager { get { return mSpriteLayerManager; } }
        public CustomDrawer CustomDrawer { get { return mCustomDrawer; } }
        public CustomDrawer CustomHudDrawer { get { return mCustomHudDrawer; } }
        public AudioPlayer AudioPlayer { get { return GameController.GC.AudioPlayerByName(mSceneKey); } }
        public XACTAudioPlayer XAudioPlayer { get { return GameController.GC.XAudioPlayerByName(mSceneKey); } }
        public List<Potion> ActivePotions { get { return GameStats.ActivatedPotionsFromPotions(GameController.GC.GameStats.Potions); } }
        public SPSprite BaseSprite { get { return mBaseSprite; } }
        public float SafeAreaFactor
        {
            get { return mSafeAreaFactor; }
            set
            {
                if (mBaseSprite != null)
                {
                    float factor = value;
                    if (factor < kSafeAreaMin) factor = kSafeAreaMin;
                    if (factor > kSafeAreaMax) factor = kSafeAreaMax;

                    int safeWidth = (int)(ResManager.RES_BACKBUFFER_WIDTH * factor);
                    int safeHeight = (int)(ResManager.RES_BACKBUFFER_HEIGHT * factor);

                    mBaseSprite.X = (ResManager.RES_BACKBUFFER_WIDTH - safeWidth) / 2;
                    mBaseSprite.Y = (ResManager.RES_BACKBUFFER_HEIGHT - safeHeight) / 2;
                    mBaseSprite.ScaleX = ResManager.RESM.ScaleX * factor;
                    mBaseSprite.ScaleY = ResManager.RESM.ScaleY * factor;

                    mBaseSprite.ViewableRegion = new Rectangle((int)mBaseSprite.X, (int)mBaseSprite.Y, safeWidth, safeHeight);
                    mSafeAreaFactor = factor;
                }
            }
        }
        public int SafeAreaIncrements { get { return (int)((mSafeAreaFactor + kSafeAreaDecrement / 2 - kSafeAreaMin) / kSafeAreaDecrement); } }
        public SPDisplayObject GamerPic
        {
            get
            {
                if (mGamerPicture != null)
                {
                    if (ControlsManager.CM.MainPlayerIndex.HasValue)
                    {
                        SPTexture texture = GameController.GC.ProfileManager.GamerPictureForPlayer(ControlsManager.CM.MainPlayerIndex.Value);
                        if (texture != null)
                        {
                            mGamerPicture.Texture = texture;
                            mGamerPicture.Visible = true;
                        }
                        else
                            mGamerPicture.Visible = false;
                    }
                    else
                        mGamerPicture.Visible = false;
                }

                return mGamerPictureProp;
            }
        }
        #endregion

        #region Methods
        public virtual void SetupController()
        {
            Prop.PropsScene = this;
            SetupSaveOptions();
            //ApplyGameSettings();

            GameController.GC.Activated += new EventHandler<EventArgs>(GameActivated);
            GameController.GC.Deactivated += new EventHandler<EventArgs>(GameDeactivated);
        }

        public virtual void SetupGamerPic()
        {
            if (mGamerPicture != null || mGamerPictureProp != null)
                return;

            // Gamer Picture
            mGamerPicture = new SPImage(TextureByName("gamer-picture"));
            mGamerPictureProp = new Prop(HelpCategory);
            mGamerPictureProp.AddChild(mGamerPicture);
            GameController.GC.ProfileManager.AddEventListener(ProfileManager.CUST_EVENT_TYPE_GAMER_PICS_WILL_REFRESH, (SPEventHandler)OnGamerPicsRefreshed);
            GameController.GC.ProfileManager.AddEventListener(ProfileManager.CUST_EVENT_TYPE_GAMER_PICS_REFRESHED, (SPEventHandler)OnGamerPicsRefreshed);
        }

        protected virtual void GameActivated(object sender, EventArgs e)
        {
            if (GameController.GC.ProfileManager != null)
                GameController.GC.ProfileManager.RefreshGamerPictures(TextureByName("gamer-picture"));
        }

        protected virtual void GameDeactivated(object sender, EventArgs e) { }

        protected virtual void SetupCaches() { }

        protected virtual void SetupSaveOptions()
        {
            if (AchievementManager != null)
            {
                AchievementManager.ProcessDelayedSaves();
                AchievementManager.DelaySavingAchievements = false;
            }
        }

        public virtual void WillGainSceneFocus()
        {
            // In case another scene took it in the meantime
            Prop.PropsScene = this;
        }

        public virtual void WillLoseSceneFocus() { }

        public virtual void AttachEventListeners()
        {
            // Make sure base classes aren't adding listeners more than once
            DetachEventListeners();
        }

        public virtual void DetachEventListeners() { }

        public virtual void ApplyGameSettings()
        {
            if (mSettingsApplied)
                return;

            //if (AudioPlayer != null)
            //{
            //    AudioPlayer.SfxVolume = GameSettings.GS.ValueForKey(GameSettings.SFX_VOLUME) / AudioPlayer.kMaxVolumeKnob;
            //    AudioPlayer.MusicVolume = GameSettings.GS.ValueForKey(GameSettings.MUSIC_VOLUME) / AudioPlayer.kMaxVolumeKnob;
            //}

            SetSfxVolume(GameSettings.GS.ValueForKey(GameSettings.SFX_VOLUME));
            SetMusicVolume(GameSettings.GS.ValueForKey(GameSettings.MUSIC_VOLUME));

            int safeAreaIncrements = GameSettings.GS.ValueForKey(GameSettings.SAFE_AREA_INCREMENTS);
            SafeAreaFactor = kSafeAreaMin + kSafeAreaIncrement * safeAreaIncrements;
            mSettingsApplied = true;
        }

        public virtual void TrialModeDidChange(bool isTrial)
        {
            ObjectivesManager.TrialModeDidChange(isTrial);
        }

        public virtual void SplashScreenDidHide() { }

        public virtual void SaveWillCommence() { }

        public virtual void OnGamerPicsRefreshed(SPEvent ev)
        {
            if (mGamerPicture != null)
            {
                if (ControlsManager.CM.MainPlayerIndex.HasValue)
                {
                    SPTexture texture = GameController.GC.ProfileManager.GamerPictureForPlayer(ControlsManager.CM.MainPlayerIndex.Value);
                    if (texture != null)
                        mGamerPicture.Texture = texture;
                    else
                        mGamerPicture.Visible = false;
                }
                else
                    mGamerPicture.Visible = false;
            }
        }

#if TEST_GUEST_BUG
        protected bool mGuestBugLocalLoadCompleted = false;
        protected bool mGuestBugShown = false;
        protected void TestGuestBug()
        {
            if (mGuestBugShown == false)
            {
                if (Guide.IsVisible == false)
                {
                    Guide.ShowSignIn(4, true);
                    mGuestBugShown = true;
                }
            }
        }
#endif
        public virtual void LocalLoadCompleted(PlayerIndex playerIndex)
        {
#if TEST_GUEST_BUG
            mGuestBugLocalLoadCompleted = true;
#endif
        }

        public virtual void PlayerSaveDeviceSelected(PlayerIndex playerIndex) { }
        public virtual void LocalLoadFailed(PlayerIndex playerIndex) { }
        public virtual void LocalSaveCompleted(PlayerIndex playerIndex) { }
        public virtual void LocalSaveFailed(PlayerIndex playerIndex) { }
        public virtual void OnlineScoresStopped() { }

        public bool CanPurchase(PlayerIndex playerIndex)
        {
            SignedInGamer gamer = SignedInGamer.SignedInGamers[playerIndex];
            return gamer != null && gamer.Privileges.AllowPurchaseContent;
        }

        public bool CanCommunicate(PlayerIndex playerIndex)
        {
            SignedInGamer gamer = SignedInGamer.SignedInGamers[playerIndex];
            return gamer != null && gamer.Privileges.AllowCommunication != GamerPrivilegeSetting.Blocked;
        }

        public virtual void PlayerChanged() { }
        public virtual void PlayerLoggedIn(PlayerIndex playerIndex) { }
        public virtual void PlayerLoggedOut(PlayerIndex playerIndex) { }

        public virtual void DefaultControllerDisconnected() { }
        public virtual void ControllerEngaged(PlayerIndex playerIndex) { }

        public virtual bool ReassignDefaultController()
        {
            bool reassigned = false;
            ControlsManager cm = ControlsManager.CM;
            PlayerIndex? playerIndex = cm.PrevQueryPlayerIndex;

            if (playerIndex.HasValue)
            {
                cm.SetDefaultPlayerIndex(playerIndex);
                reassigned = true;
            }

            return reassigned;
        }

        public virtual void PlayAmbientSounds() { }

        public virtual void Flip(bool enable)
        {
            mFlipped = enable;
        }

        public virtual void EnableScreenshotMode(bool enable) { }

        public SPRectangle BoundsInSceneSpace(SPDisplayObject displayObject)
        {
            return displayObject.BoundsInSpace(mBaseSprite);
        }

        public void AddToStageAtIndex(int index)
        {
            GameController.GC.Stage.AddChildAtIndex(mBaseSprite, index);
        }

        public void RemoveFromStage()
        {
            mBaseSprite.RemoveFromParent();
        }

        public float ScaleForUIView(SPDisplayObject view, float threshold, float ceilingFactor)
        {
            float scale = 1f;

            if (view != null)
            {
                float sX = view.Width / ViewWidth, sY = view.Height / ViewHeight;
                if ((sX > threshold || sY > threshold) || (sX < threshold && sY < threshold))
                    scale = ceilingFactor * threshold / Math.Max(sX, sY);
            }

            return scale;
        }

        public virtual int ObjectivesCategoryForViewType(ObjectivesView.ViewType type)
        {
            return 0;
        }

        public Idol IdolForKey(uint key)
        {
            return GameController.GC.GameStats.IdolForKey(key);
        }

        public Effect EffectForKey(string key)
        {
            return TM.EffectForKey(key);
        }

        public void CacheTexture(SPTexture texture, string name)
        {
            TM.CacheTexture(texture, name);
        }

        public void CacheTextures(List<SPTexture> textures, string name)
        {
            TM.CacheTextures(textures, name);
        }

        public SPTexture TextureByName(string name, bool cached = true)
        {
            return TM.TextureByName(name, cached);
        }

        public List<SPTexture> TexturesStartingWith(string name, bool cached = true)
        {
            return TM.TexturesStartingWith(name, cached);
        }

        public void SetSfxVolume(int volume)
        {
            XAudioPlayer.SetVolumeForCategory(volume / XACTAudioPlayer.kXACTMaxVolumeKnob, "Sfx");
        }

        public void SetMusicVolume(int volume)
        {
            //float adjustedVolume = MathHelper.Clamp(volume, 0f, XACTAudioPlayer.kXACTMaxVolumeKnob);
            //double logged = Math.Log(volume + 1) / Math.Log(XACTAudioPlayer.kXACTMaxVolumeKnob + 1);
            //XAudioPlayer.SetVolumeForCategory(MathHelper.Clamp((float)logged, 0f, 1f), "Music");

            XAudioPlayer.SetVolumeForCategory(volume / XACTAudioPlayer.kXACTMaxVolumeKnob, "Music");
        }

        public void PlaySound(string name)
        {
            XAudioPlayer.Play(name);
        }

        public void PauseSound(string category = null)
        {
            if (category == null)
                XAudioPlayer.Pause();
            else
                XAudioPlayer.PauseCategory(category);
        }

        public void ResumeSound(string category = null)
        {
            if (category == null)
                XAudioPlayer.Resume();
            else
                XAudioPlayer.ResumeCategory(category);
        }

        public void StopSoundCategory(string name)
        {
            XAudioPlayer.StopCategory(name);
        }

        public void StopSound(string name, AudioStopOptions options = AudioStopOptions.AsAuthored)
        {
            XAudioPlayer.Stop(name, options);
        }

        public CacheManager CacheManagerForKey(uint key)
        {
            if (mCacheManagers == null)
                return null;

            CacheManager cacheManager;
            mCacheManagers.TryGetValue(key, out cacheManager);
            return cacheManager;
        }

        public void SubscribeToInputUpdates(IInteractable client, bool modal = false)
        {
            if (mInputManager != null)
                mInputManager.Subscribe(client, modal);
        }

        public void UnsubscribeToInputUpdates(IInteractable client, bool modal = false)
        {
            if (mInputManager != null)
                mInputManager.Unsubscibe(client, modal);
        }

        public bool HasInputFocus(uint focus)
        {
            return mInputManager != null && mInputManager.HasFocus(focus);
        }

        public void PushFocusState(uint focusState, bool modal = false)
        {
            if (mInputManager != null)
                mInputManager.PushFocusState(focusState, modal);
        }

        public void PopFocusState(uint focusState = InputManager.FOCUS_STATE_NONE, bool modal = false)
        {
            if (mInputManager != null)
                mInputManager.PopFocusState(focusState, modal);
        }

        public void PopToFocusState(uint focusState, bool modal = false)
        {
            if (mInputManager != null)
                mInputManager.PopToFocusState(focusState, modal);
        }

        public Potion PotionForKey(uint key)
        {
            return GameController.GC.GameStats.PotionForKey(key);
        }

        public bool IsPotionActiveForKey(uint key)
        {
            Potion potion = PotionForKey(key);
            return (potion != null && potion.IsActive);
        }

        public void ActivatePotionForKey(bool activate, uint key)
        {
            GameController.GC.GameStats.ActivatePotion(activate, key);
        }

        public virtual void DisplayTickerHint(string text) { }

        public virtual void DisplayHintByName(string name, float x, float y, float radius, SPDisplayObject target, bool exclusive) { }

        public virtual void HideHintByName(string name) { }

        public virtual void EnableSlowedTime(bool enable)
        {
            mTimeSlowed = enable;
        }

        public double ScaleTime(double time)
        {
            return (mTimeSlowed) ? time * TimeSlowedFactor : time;
        }

        public virtual void RequestTargetForPursuer(IPursuer pursuer) { }

        public virtual void AwardPrizes(uint prizes) { }

        public virtual void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mInputManager != null)
                mInputManager.Update(gpState, kbState);
        }

        public virtual void AdvanceTime(double time)
        {
#if TEST_GUEST_BUG
            if (mGuestBugLocalLoadCompleted)
                TestGuestBug();
#endif

            double slowedTime = (mTimeSlowed) ? time * TimeSlowedFactor : time;

            if (mScenePaused)
            {
                if (mPauseJuggler != null)
                    mPauseJuggler.AdvanceTime(time);
                if (mSpecialJuggler != null)
                    mSpecialJuggler.AdvanceTime(time);
                if (mExitView != null)
                    mExitView.AdvanceTime(time);
            }
            else
            {
                mLocked = true;

                if (mAmbienceShouldPlay)
                {
                    mAmbienceShouldPlay = false;
                    PlayAmbientSounds();
                }

                foreach (Actor actor in mAdvActors)
                {
                    if (actor.MarkedForRemoval == false)
                        actor.AdvanceTime(slowedTime);
                }

                foreach (Prop prop in mAdvProps)
                {
                    if (prop.MarkedForRemoval == false)
                        prop.AdvanceTime((prop.Slowable) ? slowedTime : time);
                }

                mJuggler.AdvanceTime(slowedTime);
                mSpamJuggler.AdvanceTime(slowedTime);
                mHudJuggler.AdvanceTime(time);
                mSpecialJuggler.AdvanceTime(time);

                mLocked = false;

                RemoveQueuedActors();
                AddQueuedActors();
                RemoveQueuedProps();
                AddQueuedProps();
            }
        }

        public virtual void AddActor(Actor actor)
        {
            if (mDestructLock || actor == null)
                return;
            if (mLocked)
                mActorsAddQueue.Add(actor);
            else
            {
                mActors.Add(actor);

                if (actor.Advanceable)
                    mAdvActors.Add(actor);
                mSpriteLayerManager.AddChild(actor, actor.Category);

                if (Flipped)
                    actor.Flip(true);
            }
        }

        public virtual void RemoveActor(Actor actor, bool shouldDispose = true)
        {
            if (mDestructLock || actor == null)
                return;
            actor.ShouldDispose = shouldDispose;
            actor.SafeRemove();
        }

        protected virtual void AddQueuedActors()
        {
            if (mActorsAddQueue.Count > 0)
            {
                foreach (Actor actor in mActorsAddQueue)
                    AddActor(actor);
                mActorsAddQueue.Clear();
            }
        }

        protected virtual void RemoveQueuedActors()
        {
            //mLocked = true; // Don't need to lock if we're not calling actor.RespondToPhysicalInputs

            foreach (Actor actor in mActors)
            {
                // Moved to physics step
                //if (!actor.MarkedForRemoval)
                //    actor.RespondToPhysicalInputs();

                if (actor.MarkedForRemoval)
                    mActorsRemoveQueue.Add(actor);
            }

            //mLocked = false;

            foreach (Actor actor in mActorsRemoveQueue)
            {
                mSpriteLayerManager.RemoveChild(actor, actor.Category);

                if (actor.Advanceable)
                    mAdvActors.Remove(actor);
                mActors.Remove(actor);

                if (actor.ShouldDispose)
                {
                    if (actor is IReusable)
                    {
                        IReusable reusable = actor as IReusable;

                        if (reusable.PoolIndex != -1)
                            reusable.Hibernate();
                        else
                            actor.Dispose();
                    }
                    else
                        actor.Dispose();
                }
            }
            mActorsRemoveQueue.Clear();
        }

        public virtual void AddProp(Prop prop)
        {
            if (mDestructLock || prop == null)
                return;
            if (mLocked)
                mPropsAddQueue.Add(prop);
            else
            {
                mProps.Add(prop);

                if (prop.Advanceable)
                    mAdvProps.Add(prop);
                mSpriteLayerManager.AddChild(prop, prop.Category);

                if (Flipped)
                    prop.Flip(true);
            }
        }

        public virtual void RemoveProp(Prop prop, bool shouldDispose = true)
        {
            if (mDestructLock || prop == null)
                return;
            if (mLocked)
            {
                prop.ShouldDispose = shouldDispose;
                mPropsRemoveQueue.Add(prop);
            }
            else
            {
                mSpriteLayerManager.RemoveChild(prop, prop.Category);

                if (prop.Advanceable)
                    mAdvProps.Remove(prop);
                mProps.Remove(prop);

                if (shouldDispose)
                {
                    if (prop is IReusable)
                    {
                        IReusable reusable = prop as IReusable;

                        if (reusable.PoolIndex != -1)
                            reusable.Hibernate();
                        else
                            prop.Dispose();
                    }
                    else
                        prop.Dispose();
                }
            }
        }

        protected virtual void AddQueuedProps()
        {
            if (mLocked)
                throw new InvalidOperationException("SceneController should not be locked when adding queued Props.");

            if (mPropsAddQueue.Count > 0)
            {
                foreach (Prop prop in mPropsAddQueue)
                    AddProp(prop);
                mPropsAddQueue.Clear();
            }
        }

        protected virtual void RemoveQueuedProps()
        {
            if (mLocked)
                throw new InvalidOperationException("SceneController should not be locked when removing queued Props.");

            if (mPropsRemoveQueue.Count > 0)
            {
                foreach (Prop prop in mPropsRemoveQueue)
                    RemoveProp(prop, prop.ShouldDispose);
                mPropsRemoveQueue.Clear();
            }
        }

        public virtual void CreateGuideProp()
        {
            if (mGuideProp != null)
                return;

            mGuideProp = new GuideProp(HelpCategory);
            mGuideProp.X = ViewWidth / 2;
            mGuideProp.Y = ViewHeight / 2;
            mGuideProp.Hide();
        }

        public virtual void ShowGuideProp(int playerIndexMap, float x, float y, float duration)
        {
            if (mGuideProp == null)
                CreateGuideProp();

            mGuideProp.X = x;
            mGuideProp.Y = y;
            mGuideProp.PlayerIndexMap = playerIndexMap;
            mGuideProp.ShowForDuration(duration);
            RemoveProp(mGuideProp, false);
            AddProp(mGuideProp);
        }

        public virtual void HideGuideProp()
        {
            if (mGuideProp != null)
            {
                mGuideProp.Hide();
                RemoveProp(mGuideProp, false);
            }
        }

        public virtual void DisplayExitView()
        {
            if (mExitView != null)
                return;

            PushFocusState(InputManager.FOCUS_STATE_SYS_EXIT, true);
            mExitView = new ExitView(HelpCategory, GameController.GC.IsTrialMode ? ExitView.ExitViewMode.Trial : ExitView.ExitViewMode.Normal);
            AddProp(mExitView);

            if (ControlsManager.CM.NumConnectedControllers > 1)
            {
                CCPoint guidePos = TitleSubview.GuidePositionForScene(TitleSubview.GuidePosition.MidLower, this);
                if (guidePos != null)
                    ShowGuideProp(ControlsManager.CM.PlayerIndexMap, guidePos.X, guidePos.Y, 2f);
            }
        }

        public virtual void HideExitView()
        {
            if (mExitView != null)
            {
                PopFocusState(InputManager.FOCUS_STATE_SYS_EXIT, true);
                RemoveProp(mExitView);
                mExitView = null;

                HideGuideProp();
            }
        }

        public virtual void CreateSKTallyView()
        {
            if (mSKTallyView != null)
                return;

            mSKTallyView = new SKTallyView(TopCategory);
            AddProp(mSKTallyView);
        }

        public virtual void DisplaySKTallyView()
        {
            if (mSKTallyView != null)
                mSKTallyView.Show();
        }

        public virtual void HideSKTallyView()
        {
            if (mSKTallyView != null)
                mSKTallyView.Hide();
        }

        public void ShowPauseButton(bool show)
        {
            mHasPauseMenu = show;
        }

        protected virtual void CreatePauseMenu()
        {
            if (mPauseMenu != null)
		        return;
            mPauseMenu = new Prop(TopCategory);
	        mPauseMenu.Touchable = true;
    
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            mPauseFrame = new SPSprite();
            //mPauseFrame.X = ResManager.RESX(0); mPauseFrame.Y = ResManager.RESY(0);
            mPauseMenu.AddChild(mPauseFrame);
            ResManager.RESM.PopOffset();

            mResumeButton = new MenuButton(null, TextureByName("pause-resume-button"));
            mResumeButton.X = 0; // 250;
            //mResumeButton.Y = 424;
            mResumeButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnGameResumed);
            mPauseFrame.AddChild(mResumeButton);

            mRetryButton = new MenuButton(null, TextureByName("pause-retry-button"));
            mRetryButton.X = 160; // 410;
	        //mRetryButton.Y = 424;
            mRetryButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnGameRetry);
            mPauseFrame.AddChild(mRetryButton);

            mQuitButton = new MenuButton(null, TextureByName("pause-quit-button"));
            mQuitButton.X = 320; // 570;
	        //mQuitButton.Y = 424;
            mQuitButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnGameQuit);
            mPauseFrame.AddChild(mQuitButton);

            //mFlipControlsButton = new MenuButton(null, TextureByName("flip-controls"));
            //mFlipControlsButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnFlipControls);
            //mPauseFrame.AddChild(mFlipControlsButton);
            //PositionFlipControlsButton();

            // Ready position for attachment to ObjectivesCurrentPanel.
            mPauseFrame.X = -mPauseFrame.Width / 2;
            mPauseFrame.Y = -1.5f * mPauseFrame.Height;

            mPauseButtonsProxy = new ButtonsProxy(InputManager.HAS_FOCUS_PAUSE_MENU, Globals.kNavHorizontal);
            mPauseButtonsProxy.AddButton(mResumeButton);
            mPauseButtonsProxy.AddButton(mRetryButton);
            mPauseButtonsProxy.AddButton(mQuitButton);
            SubscribeToInputUpdates(mPauseButtonsProxy);
        }

        protected virtual void PositionFlipControlsButton()
        {
            if (mFlipControlsButton == null)
                return;
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.CenterLeft); // Combines with parent's RACenter to make a RALowerCenter
            if (Flipped)
            {
                mFlipControlsButton.X = ResManager.RESX(256);
                mFlipControlsButton.Y = ResManager.RESY(576);
            } else {
                mFlipControlsButton.X = ResManager.RESX(368);
                mFlipControlsButton.Y = ResManager.RESY(576);
            }
            ResManager.RESM.PopOffset();
        }

        public virtual void DisplayPauseMenu()
        {
            if (mScenePaused)
		        return;
	        mScenePaused = true;
	        GameController.GC.Paused = true;
            CreatePauseMenu();

            if (GameMode == AwesomePirates.GameMode.Career)
            {
                ObjectivesManager.EnableCurrentPanelButtons(false);
                ObjectivesManager.AddToCurrentPanel(mPauseMenu, 0.5f, 1f);
                ObjectivesManager.ShowCurrentPanel();
            }
            else
            {
                CreateSKTallyView();
                mSKTallyView.AddToView(mPauseMenu, 0.5f, 1f);
                DisplaySKTallyView();
            }

            mPauseFrame.Touchable = true;
            mPauseButtonsProxy.ResetNav();
            //AddProp(mPauseMenu);
        }

        public virtual void DismissPauseMenu()
        {
            if (GameMode == AwesomePirates.GameMode.Career)
            {
                ObjectivesManager.HideCurrentPanel();
                ObjectivesManager.EnableCurrentPanelButtons(true);
                ObjectivesManager.RemoveFromCurrentPanel(mPauseMenu);
            }
            else
            {
                HideSKTallyView();
                mSKTallyView.RemoveFromView(mPauseMenu);
            }

            //RemoveProp(mPauseMenu);
            mPauseFrame.Touchable = true;
            mScenePaused = false;
            GameController.GC.Paused = false;
        }

        public virtual void Resume()
        {
            if (!mScenePaused || !mHasPauseMenu)
		        return;

            PlayPauseMenuButtonSound();
            DismissPauseMenu();
        }

        public virtual void Retry()
        {
            if (!mScenePaused || !mHasPauseMenu)
		        return;

            PlayPauseMenuButtonSound();
            DismissPauseMenu();
        }

        public virtual void Quit()
        {
            if (!mScenePaused || !mHasPauseMenu)
                return;

            PlayPauseMenuButtonSound();
            DismissPauseMenu();
        }

        protected virtual void PlayPauseMenuButtonSound()
        {
            //AudioPlayer.PlaySoundWithKey("Button");
        }

        private void OnGameResumed(SPEvent ev)
        {
            Resume();
        }

        private void OnGameRetry(SPEvent ev)
        {
            Retry();
        }

        private void OnGameQuit(SPEvent ev)
        {
            Quit();
        }

        private void OnFlipControls(SPEvent ev)
        {
            Flip(!Flipped);
            PlayPauseMenuButtonSound();
        }

        public virtual void DestroyScene()
        {
            Prop.RelinquishPropScene(this);
            mBaseSprite.RemoveFromParent();

            GameController.GC.ProfileManager.RemoveEventListener(ProfileManager.CUST_EVENT_TYPE_GAMER_PICS_WILL_REFRESH, (SPEventHandler)OnGamerPicsRefreshed);
            GameController.GC.ProfileManager.RemoveEventListener(ProfileManager.CUST_EVENT_TYPE_GAMER_PICS_REFRESHED, (SPEventHandler)OnGamerPicsRefreshed);
            GameController.GC.Activated -= new EventHandler<EventArgs>(GameActivated);
            GameController.GC.Deactivated -= new EventHandler<EventArgs>(GameDeactivated);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                if (disposing)
                {
                    if (mPauseButtonsProxy != null)
                    {
                        UnsubscribeToInputUpdates(mPauseButtonsProxy);
                        mPauseButtonsProxy = null;
                    }

                    if (mCustomDrawer != null)
                    {
                        mCustomDrawer.Dispose();
                        mCustomDrawer = null;
                    }

                    if (mCustomHudDrawer != null)
                    {
                        mCustomHudDrawer.Dispose();
                        mCustomHudDrawer = null;
                    }

                    if (mInputManager != null)
                    {
                        mInputManager.Dispose();
                        mInputManager = null;
                    }
                }

                mIsDisposed = true;
            }
        }

        ~SceneController()
        {
            Dispose(false);
        }
        #endregion
    }
}
