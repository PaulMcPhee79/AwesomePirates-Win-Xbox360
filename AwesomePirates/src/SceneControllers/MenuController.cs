using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.GamerServices;
using SparrowXNA;

namespace AwesomePirates
{
    class MenuController : SPEventDispatcher, IDisposable
    {
        public enum MenuState
        {
            Title = 0,
            TransitionIn,
            In,
            TransitionOut,
            Out
        }

        private enum AlertState
        {
            None = 0,
            LoginUnavailable,
            LoginIncomplete,
            Offline,
            DataIntegrity,
            BuyFailed,
            InviteFailed
        }

        private enum QueryState
        {
            None = 0,
            TutorialPrompts,
            ResetProgress
        }

        public const string CUST_EVENT_TYPE_MENU_PLAY_SHOULD_BEGIN = "menuPlayShouldBegin";
        public const float kMenuTransitionDuration = 0.75f;
        public const uint kIgnorePlayerSwitchButtonTag = 98765;

        private const string MENU_AMBIENCE = "MenuAmbience";
        
        public MenuController(PlayfieldController scene)
        {
            mScene = scene;

            mDataIntegrityChecked = false;
            mDisplayInitialized = false;
            mSettingsApplied = false;
            mPrevMasteryBitmap = 0;
            mState = MenuState.In;

            mAlertState = AlertState.None;
            mQueryState = QueryState.None;
        }
        
        #region Fields
        protected bool mIsDisposed = false;
        private bool mDataIntegrityChecked;
        private bool mDisplayInitialized;
        private bool mSettingsApplied;
        private uint mPrevMasteryBitmap;
        private MenuState mState;
        private MenuView mView;

        private PlayfieldController mScene;

        private AlertState mAlertState;
        private QueryState mQueryState;
        #endregion

        #region Properties
        public MenuState State
        {
            get { return mState; }
            set
            {
                if (value == mState)
                    return;

                GameController gc = GameController.GC;
                MenuState previousState = mState;

                // Clean up previous state
                switch (previousState)
                {
                    case MenuState.Title:
                        mView.HideTitle();
                        break;
                    case MenuState.TransitionIn:
                        break;
                    case MenuState.In:
                        break;
                    case MenuState.TransitionOut:
                        break;
                    case MenuState.Out:
                        break;
                    default:
                        break;
                }

                mState = value;

                // Apply new state
                switch (mState)
                {
                    case MenuState.Title:
                        PlayAmbientSounds();
                        mView.ShowTitle();
                        break;
                    case MenuState.TransitionIn:
                        {
                            int hiScore = gc.GameStats.HiScore;

                            if (gc.ThisTurn.Infamy > hiScore)
                                gc.GameStats.HiScore = gc.ThisTurn.Infamy;

                            mView.Visible = true;
                            mView.Touchable = false;
                            mView.UpdateObjectivesLog();
                            mView.TransitionInOverTime(kMenuTransitionDuration);

                            if (previousState != MenuState.Title && previousState != MenuState.In && !mScene.DidRetry)
                                PlayAmbientSounds();
                        }
                        break;
                    case MenuState.In:
                        {
                            if (mDataIntegrityChecked == false)
                            {
                                mDataIntegrityChecked = true;

                                // TODO: Game Data Validation
                                //if (gc.IsGameDataValid == false)
                                //    [self setAlertState:kAlertStateDataIntegrity];
                            }

                            // Note: Must save before returning input focus to menu - otherwise we may save to the
                            //       wrong PlayerIndex if someone presses Start immediately.
                            if (mScene.GameMode == GameMode.Career)
                                gc.ProcessEndOfTurn();

                            if (GameSettings.GS.DelayedSaveRequired)
                                GameSettings.GS.SaveSettings();

                            RefreshHiScoreView();
                            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU);

                            mView.EnableExitPrompt(true);
                            mView.Touchable = true;

                            if (gc.LiveLeaderboard != null)
                            {
                                gc.LiveLeaderboard.GoSlow(false);
                                if (gc.LiveLeaderboard.IsStopped)
                                    gc.LiveLeaderboard.TryStart();
                            }
                            gc.ProfileManager.UpdatePresenceModeForPlayer(GamerPresenceMode.AtMenu);
                            //mScene.EnablePerformanceSavingMode(true); // TODO: if needed

                            CheckForDisplayAdjustment();
                        }
                        break;
                    case MenuState.TransitionOut:
                        {
                            mView.EnableExitPrompt(false);
                            mView.Touchable = false;
                            mView.PopAllSubviews();
                            mView.TransitionOutOverTime(kMenuTransitionDuration);
                            StopAmbientSoundsOverTime(2f);
                        }
                        break;
                    case MenuState.Out:
                        mView.Visible = false;
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region Methods
        public void SetupController()
        {
            if (mView != null)
                return;

            mView = new MenuView((int)PFCat.HUD, this);
            mView.PushSubviewForKey("Menu");
            RefreshHiScoreView();
            mScene.AddProp(mView);
        }

        public void AttachEventListeners()
        {
            if (mView != null)
            {
                mView.AddEventListener(MenuView.CUST_EVENT_TYPE_MENU_VIEW_DID_TRANSITION_IN, (SPEventHandler)ViewDidTransitionIn);
                mView.AddEventListener(MenuView.CUST_EVENT_TYPE_MENU_VIEW_DID_TRANSITION_OUT, (SPEventHandler)ViewDidTransitionOut);
                mView.AddEventListener(MenuView.CUST_EVENT_TYPE_MENU_VIEW_START_TO_PLAY, (SPEventHandler)ModeSelect);
                mView.AttachEventListeners();
            }
        }

        public void DetachEventListeners()
        {
            if (mView != null)
            {
                mView.RemoveEventListener(MenuView.CUST_EVENT_TYPE_MENU_VIEW_DID_TRANSITION_IN, (SPEventHandler)ViewDidTransitionIn);
                mView.RemoveEventListener(MenuView.CUST_EVENT_TYPE_MENU_VIEW_DID_TRANSITION_OUT, (SPEventHandler)ViewDidTransitionOut);
                mView.RemoveEventListener(MenuView.CUST_EVENT_TYPE_MENU_VIEW_START_TO_PLAY, (SPEventHandler)ModeSelect);
                mView.DetachEventListeners();
            }
        }

        public void ApplyGameSettings()
        {
            if (mSettingsApplied)
                return;

            mSettingsApplied = true;

            // Play on first time through so that it can sit in the paused state.
            if (mState == MenuState.Title || mState == MenuState.In)
                mScene.PlaySound(MENU_AMBIENCE);
            
            RefreshHiScoreView();
        }

        public void TrialModeDidChange(bool isTrial)
        {
            if (mView != null)
                mView.UpdateObjectivesLog();
        }

        public void SplashScreenDidHide()
        {
            if (mView != null)
                mView.SplashScreenDidHide();
        }

        public void LocalSaveFailed(PlayerIndex playerIndex)
        {
            if (mView != null)
                mView.HideSavingProgressPrompt();
        }

        public void StorageDialogDidShow()
        {
            if (mView != null)
                mView.EnableExitPrompt(false);
        }

        public void StorageDialogDidHide()
        {
            if (mScene.State == PlayfieldController.PfState.Menu && mView != null && mView.SubviewStackHeight == 1)
                mView.EnableExitPrompt(true);
        }

        public void OnGamerPicsRefreshed(SPEvent ev)
        {
            if (mView != null)
                mView.OnGamerPicsRefreshed(ev);
        }

        public void SaveWillCommence()
        {
            if (mView != null)
                mView.DisplaySavingProgressPrompt();
        }

        public void PlayerLoggedIn(PlayerIndex playerIndex)
        {
            if (mView != null)
                mView.PlayerLoggedIn(playerIndex);
        }

        public void PlayerLoggedOut(PlayerIndex playerIndex)
        {
            if (mView != null)
                mView.PlayerLoggedOut(playerIndex);
        }

        public void PlayerChanged()
        {
            if (mView != null)
                mView.UpdateObjectivesLog();
        }

        public void GoToMainMenu()
        {
            if (mView != null)
                mView.PopAllSubviews();
        }

        public void CheckForDisplayAdjustment()
        {
            if (!mDisplayInitialized)
            {
                mDisplayInitialized = GameSettings.GS.SettingForKey(GameSettings.SAFE_AREA_INIT);

                if (!mDisplayInitialized)
                    DisplayAdjustment();
            }
        }

        public void ExitMenuWillShow()
        {
            if (mView != null)
                mView.PushSubviewForKey("Exit");
        }

        public void ExitMenuDidHide()
        {
            if (mView != null)
            {
                mView.PopSubview();
                if (!mScene.HasInputFocus(InputManager.HAS_FOCUS_MENU_ALL))
                    mView.DeactivateCurrentSubview();
            }
        }

        public void AdvanceTime(double time)
        {
            mView.AdvanceTime(time);
        }

        private void PlayAmbientSounds()
        {
            if (mSettingsApplied && !mScene.IsMusicMuted)
                mScene.PlaySound(MENU_AMBIENCE);
                //mScene.PlaySound(MENU_AMBIENCE, 1, 2);
        }

        private void StopAmbientSoundsOverTime(float duration)
        {
            mScene.StopSound(MENU_AMBIENCE);
            //mScene.StopSound((MENU_AMBIENCE, duration);
        }

        public void RefreshHiScoreView()
        {
            if (mView != null)
                mView.UpdateHiScoreText();
        }

        private void ViewDidTransitionIn(SPEvent ev)
        {
            State = MenuState.In;
        }

        private void ViewDidTransitionOut(SPEvent ev)
        {
            State = MenuState.Out;
        }

        public void OnButtonTriggered(SPEvent ev)
        {
            MenuButton button = ev.CurrentTarget as MenuButton;

            if (button != null && button.ActionSelector != null)
            {
                if (button.SfxKey != null)
                    mScene.PlaySound(button.SfxKey);
                    //mScene.PlaySound(button.SfxKey, button.SfxVolume);

                GameController gc = GameController.GC;
                PlayerIndex? playerIndex = ControlsManager.CM.PrevQueryPlayerIndex;
                if (button.Tag != kIgnorePlayerSwitchButtonTag && playerIndex.HasValue && gc.ProfileManager.WouldSwitchToPlayerIndex(playerIndex.Value))
                {
                    ControlsManager.CM.ControllerDidEngage(playerIndex.Value);

                    if (gc.AddDelayedCall(button.ActionSelector))
                        gc.ProfileManager.SwitchToPlayerIndex(playerIndex.Value);
                }
                else
                    button.ActionSelector();
            }
        }

        private void ForceCloseModeSelectView()
        {
            // Make sure we close from two menu layers deep (just in case).
            if (mView.IsModeSelectViewShowing)
                CloseModeSelect();
            if (mView.IsModeSelectViewShowing)
                CloseModeSelect();
        }

        public void SinglePlayerGameRequested(SPEvent ev)
        {
            if (mScene.State == PlayfieldController.PfState.Menu)
            {
                ForceCloseModeSelectView();
                mScene.GameMode = GameMode.Career;
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MENU_PLAY_SHOULD_BEGIN));
            }
        }

        public void FFAGameRequested(SPEvent ev)
        {
            if (mScene.State == PlayfieldController.PfState.Menu)
            {
                ForceCloseModeSelectView();
                mScene.GameMode = GameMode.SKFFA;
                mScene.PlayerChanged();
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MENU_PLAY_SHOULD_BEGIN));
            }
        }

        public void TwoVTwoGameRequested(SPEvent ev)
        {
            if (mScene.State == PlayfieldController.PfState.Menu)
            {
                ForceCloseModeSelectView();
                mScene.GameMode = GameMode.SK2v2;
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MENU_PLAY_SHOULD_BEGIN));
            }
        }

        public void OnOptionMenuSelection(SPEvent ev)
        {
            switch (ev.EventType)
            {
                case OptionsView.CUST_EVENT_TYPE_DISPLAY_ADJUST_REQUEST:
                    DisplayAdjustment();
                    break;
                case OptionsView.CUST_EVENT_TYPE_SFX_VOLUME_CHANGED:
                    {
                        if (ev is NumericValueChangedEvent == false)
                            break;

                        NumericValueChangedEvent sfxEv = ev as NumericValueChangedEvent;
                        //mScene.AudioPlayer.SfxVolume = sfxEv.IntValue / AudioPlayer.kMaxVolumeKnob;
                        mScene.SetSfxVolume(sfxEv.IntValue);
                        GameSettings.GS.SetValueForKey(GameSettings.SFX_VOLUME, sfxEv.IntValue);
                    }
                    break;
                case OptionsView.CUST_EVENT_TYPE_MUSIC_VOLUME_CHANGED:
                    {
                        if (ev is NumericValueChangedEvent == false)
                            break;

                        NumericValueChangedEvent musicEv = ev as NumericValueChangedEvent;
                        GameSettings.GS.SetValueForKey(GameSettings.MUSIC_VOLUME, musicEv.IntValue);
                        //mScene.AudioPlayer.MusicVolume = musicEv.IntValue / AudioPlayer.kMaxVolumeKnob;

                        mScene.SetMusicVolume(musicEv.IntValue);

                        if (musicEv.IntValue == 0)
                            mScene.PauseSound("Music");
                        else if (musicEv.IntValue > 0 && musicEv.OldIntValue == 0)
                            mScene.ResumeSound("Music");
                    }
                    break;
                case OptionsView.CUST_EVENT_TYPE_TUTORIAL_RESET_REQUEST:
                    SetQueryState(QueryState.TutorialPrompts);
                    break;
                case OptionsView.CUST_EVENT_TYPE_CREDITS_REQUEST:
                    Credits();
                    break;
            }
        }

        public void ModeSelect(SPEvent ev)
        {
            PlayerIndex? playerIndex = ControlsManager.CM.PrevQueryPlayerIndex;
            if (playerIndex.HasValue)
                ControlsManager.CM.ControllerDidEngage(playerIndex.Value);

            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_MODE_SELECT);
            mView.ShowModeSelect();
            mView.PushSubviewForKey("ModeSelect");
        }

        public void DisplayAdjustment()
        {
            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_DISPLAY_ADJUST);
            mView.PushSubviewForKey("Display");
            mView.PopulateDisplayAdjustmentView();
        }

        public void OnDisplayAdjustedUp(SPEvent ev)
        {
            if (mScene.SafeAreaFactor < SceneController.kSafeAreaMax)
            {
                mScene.SafeAreaFactor += SceneController.kSafeAreaIncrement;
                mScene.PlaySound("Button");
                GameSettings.GS.SetValueForKey(GameSettings.SAFE_AREA_INCREMENTS, mScene.SafeAreaIncrements);
            }
        }

        public void OnDisplayAdjustedDown(SPEvent ev)
        {
            if (mScene.SafeAreaFactor > SceneController.kSafeAreaMin)
            {
                mScene.SafeAreaFactor -= SceneController.kSafeAreaDecrement;
                mScene.PlaySound("Button");
                GameSettings.GS.SetValueForKey(GameSettings.SAFE_AREA_INCREMENTS, mScene.SafeAreaIncrements);
            }
        }

        public void OnDisplayAdjustmentCompleted(SPEvent ev)
        {
            CloseDisplayAdjustment();
        }

        public void Objectives()
        {
            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_OBJECTIVES_LOG);
            mView.UpdateObjectivesLog();
            mView.PushSubviewForKey("Objectives");
        }

        public void Mastery()
        {
            mPrevMasteryBitmap = mScene.MasteryManager.MasteryBitmap;
            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_MASTERY);
            mView.PopulateMasteryLog();
            mView.PushSubviewForKey("Mastery");
        }

        public void Achievements()
        {
            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_ACHIEVEMENTS);
            mView.PushSubviewForKey("Achievements");
            mView.PopulateAchievementsView();
        }

        public void Leaderboard()
        {
            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_LEADERBOARD);
            mView.PushSubviewForKey("Leaderboard");
            mView.PopulateLeaderboardView();
        }

        public void Info()
        {
            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_INFO);
            mView.PushSubviewForKey("Info");
        }

        public void Options()
        {
            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_OPTIONS);
            mView.PushSubviewForKey("Options");
            mView.PopulateOptionsView();
        }

        public void BuyNow()
        {
            bool purchaseFailed = true;

            if (ControlsManager.CM.PrevQueryPlayerIndex.HasValue && mScene.CanPurchase(ControlsManager.CM.PrevQueryPlayerIndex.Value))
            {
                if (GameController.GC.IsTrialMode && !Guide.IsVisible)
                {
                    purchaseFailed = false;

                    try
                    {
                        Guide.ShowMarketplace(ControlsManager.CM.PrevQueryPlayerIndex.Value);
                    }
                    catch (Exception)
                    {
                        purchaseFailed = true;
                    }
                }
            }

            if (purchaseFailed)
                SetAlertState(AlertState.BuyFailed);
        }

        public void InviteFriends()
        {
            bool inviteFailed = true;

            if (ControlsManager.CM.PrevQueryPlayerIndex.HasValue && mScene.CanCommunicate(ControlsManager.CM.PrevQueryPlayerIndex.Value) && !Guide.IsVisible)
            {
                try
                {
                    inviteFailed = false;
                    PlayerIndex playerIndex = ControlsManager.CM.PrevQueryPlayerIndex.Value;
                    SignedInGamer gamer = SignedInGamer.SignedInGamers[playerIndex];
                    FriendCollection friends = gamer.GetFriends();
                    Gamer[] recipients = null;

                    if (friends != null && friends.Count > 0)
                    {
                        recipients = new Gamer[Math.Min(friends.Count, 100)]; // Recipients length must be <= 100.
                        for (int i = 0; i < recipients.Length; ++i)
                            recipients[i] = friends[i];
                    }

                    // Message must be <= 200 characters in length. 
                    string message = "Ahoy, mateys! Come join me in \"Awesome Pirates\", lest I be forced to throw ye to the sharks! Go to the marketplace or search using Bing on the dashboard to download the free trial.";
                    Guide.ShowComposeMessage(ControlsManager.CM.PrevQueryPlayerIndex.Value, message, recipients);
                }
                catch (Exception)
                {
                    inviteFailed = true;
                }
            }

            if (inviteFailed)
                SetAlertState(AlertState.InviteFailed);
        }

        public void Credits()
        {
            BookletSubview subview = mView.BookletSubviewForKey("Credits");
            if (subview != null)
            {
                mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_CREDITS);
                mView.PushSubviewForKey("Credits");
                subview.InputFocus = InputManager.HAS_FOCUS_MENU_CREDITS;
            }
        }

        public void Potions()
        {
            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_POTIONS);
            mView.PushSubviewForKey("Potions");
            mView.PopulatePotionView();
        }

        public void PrevBookletPage()
        {
            TitleSubview subview = mView.CurrentSubview;

            if (subview != null && subview is BookletSubview)
                (subview as BookletSubview).PrevPage();
        }

        public void NextBookletPage()
        {
            TitleSubview subview = mView.CurrentSubview;

            if (subview != null && subview is BookletSubview)
                (subview as BookletSubview).NextPage();
        }

        public void StatsLogInfo()
        {
            mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_INFO_STATS);
            mView.PushSubviewForKey("StatsLog");
            mView.PopulateStatsView();
        }

        public void GameConceptsInfo()
        {
            BookletSubview subview = mView.BookletSubviewForKey("GameConcepts");
            if (subview != null)
            {
                mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_INFO_GAME_CONCEPTS);
                mView.PushSubviewForKey("GameConcepts");
                subview.InputFocus = InputManager.HAS_FOCUS_MENU_INFO_GAME_CONCEPTS;
            }
        }

        public void SpellsMunitionsInfo()
        {
            BookletSubview subview = mView.BookletSubviewForKey("SpellsAndMunitions");
            if (subview != null)
            {
                mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_INFO_SPELLS_MUNITIONS);
                mView.PushSubviewForKey("SpellsAndMunitions");
                subview.InputFocus = InputManager.HAS_FOCUS_MENU_INFO_SPELLS_MUNITIONS;
            }
        }

        public void SelectPotion()
        {
            mView.SelectCurrentPotion();
        }

        public void CloseSubview()
        {
            if (mView != null)
            {
                TitleSubview subview = mView.CurrentSubview;

                if (subview != null && subview.CloseSelector != null)
                    subview.CloseSelector.Invoke(this, null);
	        }
        }

        public void CloseModeSelect()
        {
            if (mView.CloseModeSelect())
            {
                mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_MODE_SELECT);
                mView.PopSubview();
            }
        }

        public void CloseDisplayAdjustment()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_DISPLAY_ADJUST);
            mView.PopSubview();
            mView.UnpopulateDisplayAdjustmentView();

            // Only save here if we're initializing the display for the first time. Otherwise, defer to options close save.
            if (!mDisplayInitialized)
            {
                GameSettings.GS.SetSettingForKey(GameSettings.SAFE_AREA_INIT, true);
                GameSettings.GS.SaveSettings();
            }
        }

        public void CloseObjectives()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_OBJECTIVES_LOG);
            mView.PopSubview();
        }

        public void CloseMastery()
        {
            if (mView.SendCloseToMasteryLog())
            {
                mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_MASTERY);
                mView.PopSubview();
                mView.UnpopulateMasteryLog();

                if (mScene.MasteryManager.CurrentModel.SaveRequired)
                {
                    ProfileManager profileManager = mScene.AchievementManager.ProfileManager;
                    profileManager.SaveProgress(profileManager.MainPlayerIndex);
                    mScene.MasteryManager.RefreshMasteryBitmap();
                    mScene.AchievementManager.ResetCombatTextCache();

                    // Re-cache cannonballs if ricochet cone size has changed.
                    if ((mPrevMasteryBitmap & CCMastery.CANNON_CANNONEER) != (mScene.MasteryManager.MasteryBitmap & CCMastery.CANNON_CANNONEER))
                    {
                        Cannonball.PurgeReusables();
                        Cannonball.SetupReusables();
                    }
                }
            }
        }

        public void CloseAchievements()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_ACHIEVEMENTS);
            mView.PopSubview();
            mView.UnpopulateAchievementsView();
        }

        public void CloseLeaderboard()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_LEADERBOARD);
            mView.PopSubview();
            mView.UnpopulateLeaderboardView();

            GameController gc = GameController.GC;
            if (gc.LiveLeaderboard != null && gc.LiveLeaderboard.ShouldSave)
                gc.LiveLeaderboard.Save();
        }

        public void CloseInfo()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_INFO);
            mView.PopSubview();
        }

        public void CloseStats()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_INFO_STATS);
            mView.PopSubview();
            mView.UnpopulateStatsView();
        }

        public void CloseGameConcepts()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_INFO_GAME_CONCEPTS);
            mView.PopSubview();
            mView.DestroySubviewForKey("GameConcepts");
        }

        public void CloseSpellsAndMunitions()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_INFO_SPELLS_MUNITIONS);
            mView.PopSubview();
            mView.DestroySubviewForKey("SpellsAndMunitions");
        }

        public void CloseOptions()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_OPTIONS);
            mView.PopSubview();
            mView.UnpopulateOptionsView();

            if (GameSettings.GS.DelayedSaveRequired)
                GameSettings.GS.SaveSettings();
        }

        public void ClosePotions()
        {
            if (mView.PotionWasSelected)
            {
                ProfileManager profileManager = mScene.AchievementManager.ProfileManager;
                profileManager.SaveProgress(profileManager.MainPlayerIndex);
                mScene.AchievementManager.ResetCombatTextCache();
            }

            if (GameSettings.GS.DelayedSaveRequired)
                GameSettings.GS.SaveSettings();

            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_POTIONS);
            mView.PopSubview();
            mView.UnpopulatePotionView();
        }

        public void CloseCredits()
        {
            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_CREDITS);
            mView.PopSubview();
            mView.DestroySubviewForKey("Credits");
        }

        private void ResetTutorialPrompts()
        {
            GameSettings.GS.ResetTutorialPrompts();
        }

        private void ResetObjectives()
        {
            GameController gc = GameController.GC;
            gc.GameStats.ResetObjectives();
            gc.GameStats.EnforcePotionConstraints();
            mScene.AchievementManager.SaveProgress();
            mScene.ObjectivesManager.SetupWithRanks(gc.GameStats.Objectives);
            mScene.AchievementManager.ResetCombatTextCache();
            mView.UpdateObjectivesLog();
        }

        private void ResetAchievements()
        {
            GameController.GC.GameStats.ResetAchievements();
            mScene.AchievementManager.SaveProgress();
        }

        private void SetAlertState(AlertState state)
        {
            if (mScene.State == PlayfieldController.PfState.Hibernating || mState != MenuState.In || (mAlertState != AlertState.None && state != AlertState.None))
                return;

            switch (state)
            {
                case AlertState.None:
                    {
                        if (mAlertState != AlertState.None)
                        {
                            mView.PopSubview();
                            mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_ALERT);
                        }
                    }
                    break;
                case AlertState.BuyFailed:
                    mView.SetAlertTitle("Purchase Failed", "You must be logged into Xbox Live with payment privileges.");
                    mView.PushSubviewForKey("Alert");
                    mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_ALERT);
                    break;
                case AlertState.InviteFailed:
                    mView.SetAlertTitle("Invite Failed", "You must be logged into Xbox Live with communication privileges.");
                    mView.PushSubviewForKey("Alert");
                    mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_ALERT);
                    break;
            }

            mAlertState = state;
        }

        private void SetQueryState(QueryState state)
        {
            if (mScene.State == PlayfieldController.PfState.Hibernating || mState != MenuState.In || (mQueryState != QueryState.None && state != QueryState.None))
                return;

            switch (state)
            {
                case QueryState.None:
                    if (mQueryState != QueryState.None)
                    {
                        mView.PopSubview();
                        mScene.PopFocusState(InputManager.FOCUS_STATE_MENU_QUERY);
                    }
                    break;
                case QueryState.TutorialPrompts:
                    mView.SetQueryTitle("Are You Sure?", "This will reset the tutorial and all helpful hints.");
                    mView.PushSubviewForKey("Query");
                    mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_QUERY);
                    break;
                case QueryState.ResetProgress:
                    mView.SetQueryTitle("Are You Sure?", "This will reset your Objectives, Mastery and Achievements progress.");
                    mView.PushSubviewForKey("Query");
                    mScene.PushFocusState(InputManager.FOCUS_STATE_MENU_QUERY);
                    break;
            }

            mQueryState = state;
        }

        public void OkAlert()
        {
            SetAlertState(AlertState.None);
        }

        public void YesQuery()
        {
            switch (mQueryState)
            {
                case QueryState.TutorialPrompts:
                    ResetTutorialPrompts();
                    break;
                case QueryState.ResetProgress:
                    ResetObjectives();
                    ResetAchievements();
                    break;
                default:
                    break;
            }

            SetQueryState(QueryState.None);
        }

        public void NoQuery()
        {
            SetQueryState(QueryState.None);
        }

        public void OnSpeedboatLaunchRequested(SPEvent ev)
        {
            if (ev.CurrentTarget != null && ev.CurrentTarget is MenuButton)
            {
                MenuButton button = ev.CurrentTarget as MenuButton;
                if (button.SfxKey != null)
                    mScene.PlaySound(button.SfxKey);
            }

            CloseAchievements();
            mScene.ReassignDefaultController();
            mScene.RaceEnabled = true;
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
                try
                {
                    if (disposing)
                    {
                        DetachEventListeners();

                        if (mView != null)
                        {
                            if (mScene != null)
                                mScene.Juggler.RemoveTweensWithTarget(mView);
                            mView.Dispose();
                            mView = null;
                        }

                        mScene = null;
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
                finally
                {
                    mIsDisposed = true;
                }
            }
        }

        ~MenuController()
        {
            Dispose(false);
        }
        #endregion
    }
}
