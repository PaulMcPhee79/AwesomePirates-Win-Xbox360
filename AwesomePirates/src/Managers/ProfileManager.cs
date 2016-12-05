using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class ProfileManager : SPEventDispatcher
    {
        public const string CUST_EVENT_TYPE_PLAYER_LOGGED_IN = "playerLoggedInEvent";
        public const string CUST_EVENT_TYPE_PLAYER_LOGGED_OUT = "playerLoggedOutEvent";
        public const string CUST_EVENT_TYPE_GAMER_PICS_WILL_REFRESH = "gamerPicsWillRefreshEvent";
        public const string CUST_EVENT_TYPE_GAMER_PICS_REFRESHED = "gamerPicsRefreshedEvent";

        private const int kHiScoreMaxPosition = 100;
        private const int kHiScoreFillMin = 0;
        private const int kHiScoreFillMax = 10000000;

        private const string kSaveFilename = "SavedGame.dat";
        private const string kLocalSaveContainerName = "Awesome Pirates Save";
        private const string kGlobalSaveContainerName = "Awesome Pirates Shared Save";

        public ProfileManager(List<object> achDefs)
        {
            mAchievementDefs = achDefs;
            mHiScoreTable = new HiScoreTable(SceneController.LeaderboardFontKey, kHiScoreMaxPosition);
            mHiScoreTable.PreFill(kHiScoreFillMin, kHiScoreFillMax);
            mGlobalStats = new GameStats(GameStats.DefaultAlias);

            mProfiles = new PlayerProfile[4]
            {
                new PlayerProfile(mGlobalStats, PlayerIndex.One),
                new PlayerProfile(mGlobalStats, PlayerIndex.Two),
                new PlayerProfile(mGlobalStats, PlayerIndex.Three),
                new PlayerProfile(mGlobalStats, PlayerIndex.Four)
            };

            mMainPlayerIndex = PlayerIndex.One; // Prep for property setter cascade.
            MainPlayerIndex = PlayerIndex.One;
            mPromptingIndex = null;

#if DEBUG || XBOX || SYSTEM_LINK_SESSION
            SignedInGamer.SignedIn += SignedInGamer_SignedIn;
            SignedInGamer.SignedOut += SignedInGamer_SignedOut;
#endif
        }

        #region Fields
        private bool mIsDisposed = false;
        private bool mInitialSwitchPerformed = false;
        private bool mDidLastSaveFail = false;
        private GameStats mGlobalStats;
        private HiScoreTable mHiScoreTable;
        private List<object> mAchievementDefs;

        private PlayerIndex? mPromptingIndex;
        private PlayerIndex mMainPlayerIndex;
        private PlayerProfile[] mProfiles;
        private SPTexture mDefaultGamerPicture;
        #endregion

        #region Properties
        public bool DidLastSaveFail { get { return mDidLastSaveFail; } }
        public bool IsUsingGlobalPlayerStats { get { return PlayerStats == mGlobalStats; } }
        public string GamerTag { get { return MainPlayerProfile.GamerTag; } }
        public SignedInGamer SigGamer { get { return MainPlayerProfile.SigGamer; } }
        public GameStats PlayerStats { get { return MainPlayerProfile.PlayerStats; } }
        public HiScoreTable HiScores { get { return mHiScoreTable; } }
        public PlayerIndex? PromptingIndex { get { return mPromptingIndex; } }
        public PlayerIndex MainPlayerIndex
        {
            get { return mMainPlayerIndex; }
            private set
            {
                PlayerStats.RemoveEventListener(AchievementEarnedEvent.CUST_EVENT_TYPE_ACHIEVEMENT_EARNED, (AchievementEarnedEventHandler)OnAchievementEarned);
                mMainPlayerIndex = value;

                PlayerStats.AddEventListener(AchievementEarnedEvent.CUST_EVENT_TYPE_ACHIEVEMENT_EARNED, (AchievementEarnedEventHandler)OnAchievementEarned);
                GameController.GC.ObjectivesManager.SetupWithRanks(PlayerStats.Objectives);
                DispatchEvent(SPEvent.SPEventWithType(GameStats.CUST_EVENT_TYPE_PLAYER_CHANGED));
            }
        }
        public PlayerProfile MainPlayerProfile { get { return mProfiles[(int)MainPlayerIndex]; } }
        public List<string> ProfileNames
        {
            get
            {
                List<string> names = new List<string>(mProfiles.Length);
                foreach (PlayerProfile profile in mProfiles)
                {
                    if (profile != null && profile.GamerTag != null)
                        names.Add(profile.GamerTag);
                }

                return names;
            }
        }
        public List<string> OnlineProfileNames
        {
            get
            {
                List<string> names = new List<string>(mProfiles.Length);
                foreach (PlayerProfile profile in mProfiles)
                {
                    if (profile != null && profile.GamerTag != null)
                    {
                        SignedInGamer gamer = SignedInGamer.SignedInGamers[profile.PlayerIndex];
#if DEBUG && SYSTEM_LINK_SESSION
                        if (gamer != null && !gamer.IsGuest)
#else
                        if (gamer != null && gamer.Privileges.AllowOnlineSessions && !gamer.IsGuest)
#endif
                            names.Add(profile.GamerTag);

                    }
                }

                return names;
            }
        }
        #endregion

        #region Methods
        private void SignedInGamer_SignedIn(object sender, SignedInEventArgs args)
        {
            PlayerIndex playerIndex = args.Gamer.PlayerIndex;
            mProfiles[(int)playerIndex].GamerTag = SignedInGamer.SignedInGamers[playerIndex] != null ? SignedInGamer.SignedInGamers[playerIndex].Gamertag : null;
            DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_PLAYER_LOGGED_IN, playerIndex));

            if (!PromptingIndex.HasValue && !mProfiles[(int)playerIndex].ChoseNotToSave && mInitialSwitchPerformed &&
                    playerIndex == MainPlayerIndex && !FileManager.FM.HasDeviceLocal(playerIndex))
            {
                if (FileManager.FM.AddSaveDeviceForPlayer(playerIndex))
                {
                    mPromptingIndex = playerIndex;
                    FileManager.FM.PromptForDeviceLocal(playerIndex);
                }
            }

            RefreshGamerPictures(mDefaultGamerPicture);
        }

        private void SignedInGamer_SignedOut(object sender, SignedOutEventArgs args)
        {
            PlayerIndex playerIndex = args.Gamer.PlayerIndex;
            mProfiles[(int)playerIndex].GamerTag = null;
            DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_PLAYER_LOGGED_OUT, playerIndex));
            FileManager.FM.DestroyPlayerSaveDevice(playerIndex);
            mProfiles[(int)playerIndex].Reset(mGlobalStats);

            if (playerIndex == MainPlayerIndex)
                MainPlayerIndex = playerIndex;

            mPromptingIndex = null;
            RefreshGamerPictures(mDefaultGamerPicture);
        }

        public bool IsPlayerUsingGlobalStats(PlayerIndex playerIndex)
        {
            PlayerProfile profile = mProfiles[(int)playerIndex];
            return profile != null && profile.PlayerStats != null && profile.PlayerStats == mGlobalStats;
        }

        public PlayerProfile ProfileForTag(string gamertag)
        {
            PlayerProfile p = null;
            foreach (PlayerProfile profile in mProfiles)
            {
                if (profile != null && profile.GamerTag != null && profile.GamerTag == gamertag)
                {
                    SignedInGamer gamer = profile.SigGamer;
                    if (gamer != null && !gamer.IsGuest)
                    {
                        p = profile;
                        break;
                    }
                }
            }

            return p;
        }

        public void RefreshGamerPictures(SPTexture texture)
        {
            if (texture != null)
                mDefaultGamerPicture = texture;

            if (mDefaultGamerPicture != null && mProfiles != null)
            {
                foreach (PlayerProfile profile in mProfiles)
                    profile.PrepareForGamerPictureRefresh(mDefaultGamerPicture);
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_GAMER_PICS_WILL_REFRESH));

                foreach (PlayerProfile profile in mProfiles)
                    profile.RefreshGamerPicture(mDefaultGamerPicture);
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_GAMER_PICS_REFRESHED));
            }
        }

        public SPTexture PureGamerPictureForPlayer(PlayerIndex playerIndex)
        {
            PlayerProfile profile = mProfiles[(int)playerIndex];
            return profile.GamerPicture;
        }

        public SPTexture GamerPictureForPlayer(PlayerIndex playerIndex)
        {
            PlayerProfile profile = mProfiles[(int)playerIndex];

            if (profile.PlayerStats == mGlobalStats)
                return mDefaultGamerPicture;
            else
                return profile.GamerPicture;
        }

        public void UpdatePresenceModeForPlayer(GamerPresenceMode mode, PlayerIndex? playerIndex = null)
        {
            if (mProfiles == null)
                return;

            if (playerIndex.HasValue)
                mProfiles[(int)playerIndex.Value].PresenceMode = mode;
            else
            {
                foreach (PlayerProfile profile in mProfiles)
                    profile.PresenceMode = mode;
            }
        }

        public void PlayerSaveDeviceSelected(PlayerIndex playerIndex)
        {
            ControlsManager.CM.ControllerDidEngage(playerIndex);
            mPromptingIndex = null;

            if (!mProfiles[(int)playerIndex].ProgressLoaded)
                LoadProgress(playerIndex);

            // Don't set this if the load failed. The load may have failed and then the player
            // instead chose to use the global save, so we should allow them to save on the
            // shared profile.
            if (mProfiles[(int)playerIndex].ProgressLoaded)
                mProfiles[(int)playerIndex].HasPreviouslyChosenSaveDevice = true;
        }

        public void PlayerSaveDeviceCancelled(PlayerIndex playerIndex)
        {
            ControlsManager.CM.ControllerDidEngage(playerIndex);
            PlayerProfile profile = mProfiles[(int)playerIndex];
            mPromptingIndex = null;

            if (profile.HasPreviouslyChosenSaveDevice)
            {
                profile.ChoseNotToSave = true;
            }
            else
            {
                //profile.GamerTag = null; // Leave their name intact for leaderboard entries.
                profile.Reset(mGlobalStats);

                if (playerIndex == MainPlayerIndex)
                    MainPlayerIndex = playerIndex;
            }
        }

        public void PlayerLoadDidFail(PlayerIndex playerIndex)
        {
            if (mPromptingIndex != null)
                mPromptingIndex = playerIndex;
        }

        public bool DidPlayerChooseNotToSave(PlayerIndex playerIndex)
        {
            return mProfiles[(int)playerIndex].ChoseNotToSave;
        }

        public bool WouldSwitchToPlayerIndex(PlayerIndex playerIndex)
        {
            return (!mInitialSwitchPerformed || MainPlayerIndex != playerIndex);
        }

        public bool SwitchToPlayerIndex(PlayerIndex playerIndex)
        {
            bool switched = false;

            if (WouldSwitchToPlayerIndex(playerIndex))
            {
                if (!PromptingIndex.HasValue && !mProfiles[(int)playerIndex].ChoseNotToSave && SignedInGamer.SignedInGamers[playerIndex] != null &&
                        !SignedInGamer.SignedInGamers[playerIndex].IsGuest && !FileManager.FM.HasDeviceLocal(playerIndex))
                {
                    if (FileManager.FM.AddSaveDeviceForPlayer(playerIndex))
                    {
                        mPromptingIndex = playerIndex;
                        MainPlayerIndex = playerIndex;
                        FileManager.FM.PromptForDeviceLocal(playerIndex);
                    }
                    else
                        MainPlayerIndex = playerIndex;
                }
                else
                    MainPlayerIndex = playerIndex;

                mInitialSwitchPerformed = switched = true;
            }

            return switched;
        }

        public void Initialize()
        {
            // Global load (default shared profile)
            try
            {
                if (FileManager.FM.IsReadyGlobal() && FileManager.FM.FileExistsGlobal(kGlobalSaveContainerName, kSaveFilename))
                {
                    FileManager.FM.LoadGlobal(kGlobalSaveContainerName, kSaveFilename, stream =>
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                            mGlobalStats.DecodeWithReader(reader);
                        Debug.WriteLine("Global GameStats initialize completed.");
                    });
                }
            }
            catch (Exception e)
            {
                // TODO: Notify player of unexpected saved game problem via alert box
                Debug.WriteLine("An unexpected error occurred when attempting to initialize global GameStats. " + e.Message);
            }
            finally
            {
                GameController.GC.ResetElapsedTime();
            }
        }

        public void PrepareForNewGame()
        {

        }

        public void LoadProgress(PlayerIndex playerIndex)
        {
#if WINDOWS
            return;
#else
            GameController gc = GameController.GC;
            PlayerProfile profile = mProfiles[(int)playerIndex];

            if (!profile.ChoseNotToSave && SignedInGamer.SignedInGamers[playerIndex] != null && !SignedInGamer.SignedInGamers[playerIndex].IsGuest)
            {
                GameStats playerStats = new GameStats(profile.SigGamer.Gamertag);

                // Local load
                try
                {
                    if (FileManager.FM.IsReadyLocal(playerIndex) && !FileManager.FM.FileExistsLocal(playerIndex, kLocalSaveContainerName, kSaveFilename))
                    {
                        // Safe to save progress because there is no previous progress on the storage device.
                        profile.Reset(playerStats);
                        profile.ProgressLoaded = true;
                        playerStats.Alias = profile.SigGamer.Gamertag; // Gamertag may have been changed through XBox Live control panel.

                        if (profile.PlayerIndex == MainPlayerIndex)
                        {
                            playerStats.AddEventListener(AchievementEarnedEvent.CUST_EVENT_TYPE_ACHIEVEMENT_EARNED, (AchievementEarnedEventHandler)OnAchievementEarned);
                            gc.ObjectivesManager.SetupWithRanks(playerStats.Objectives);
                            DispatchEvent(SPEvent.SPEventWithType(GameStats.CUST_EVENT_TYPE_PLAYER_CHANGED));
                        }

                        DispatchEvent(new PlayerIndexEvent(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, playerIndex));
                    }
                    else
                    {
                        FileManager.FM.LoadLocal(playerIndex, kLocalSaveContainerName, kSaveFilename, stream =>
                        {
                            using (BinaryReader reader = new BinaryReader(stream))
                                playerStats.DecodeWithReader(reader);

                            if (profile.PlayerIndex == MainPlayerIndex)
                                profile.PlayerStats.RemoveEventListener(AchievementEarnedEvent.CUST_EVENT_TYPE_ACHIEVEMENT_EARNED, (AchievementEarnedEventHandler)OnAchievementEarned);

                            profile.Reset(playerStats);
                            profile.ProgressLoaded = true;
                            playerStats.Alias = profile.SigGamer.Gamertag; // Gamertag may have been changed through XBox Live control panel.

                            if (profile.PlayerIndex == MainPlayerIndex)
                            {
                                playerStats.AddEventListener(AchievementEarnedEvent.CUST_EVENT_TYPE_ACHIEVEMENT_EARNED, (AchievementEarnedEventHandler)OnAchievementEarned);
                                gc.ObjectivesManager.SetupWithRanks(playerStats.Objectives);
                                DispatchEvent(SPEvent.SPEventWithType(GameStats.CUST_EVENT_TYPE_PLAYER_CHANGED));
                            }

                            DispatchEvent(new PlayerIndexEvent(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, playerIndex));
                            Debug.WriteLine("Local GameStats load completed.");
                        });
                    }
                }
                catch (Exception e)
                {
                    DispatchEvent(new PlayerIndexEvent(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, playerIndex));
                    Debug.WriteLine("Local GameStats load failed: " + e.Message);
                }
                finally
                {
                    gc.ResetElapsedTime();
                }
            }
#endif
        }

        public void SaveProgress(PlayerIndex playerIndex)
        {
            PlayerProfile profile = mProfiles[(int)playerIndex];
            GameStats playerStats = profile.PlayerStats;
            SignedInGamer gamer = SignedInGamer.SignedInGamers[playerIndex];

            if (playerStats != mGlobalStats && gamer != null && !gamer.IsGuest)
            {
                if (!profile.ChoseNotToSave)
                {
                    // Local save
                    try
                    {
                        // Commented out: Better to let it fail and then we can handle all problems via one failed code path.
                        //if (FileManager.FM.IsReadyLocal(playerIndex))
                        //{
                        // Safeguard: If we failed to load progress, but valid progress exists on the storage device, then don't
                        // overwrite our valid saved data with whatever small amount we have achieved during this turn.
                        if (!profile.ProgressLoaded && FileManager.FM.IsReadyLocal(playerIndex) && FileManager.FM.FileExistsLocal(playerIndex, kLocalSaveContainerName, kSaveFilename))
                            return;
                        profile.ProgressLoaded = true; // Make sure future safeguards don't prevent saving games this turn.

                        GameStats saveStats = playerStats.Clone();
                        FileManager.FM.QueueLocalSaveAsync(playerIndex, kLocalSaveContainerName, kSaveFilename, stream =>
                        {
                            // No need to wrap in try/catch - EasyStorage::SaveDeviceAsync already does that
                            // and dispatches the appropriate events.
                            using (BinaryWriter writer = new BinaryWriter(stream))
                                saveStats.EncodeWithWriter(writer);

                            Debug.WriteLine("Local GameStats save completed.");
                        });
                        //}
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Local GameStats save failed: " + e.Message);
                    }
                }
                else
                {
                    Debug.WriteLine("Save aborted: player chose not to save.");
                }
            }
            else
            {
                // Global save (default shared profile)
                try
                {
                    if (playerStats == mGlobalStats)
                    {
                        GameStats saveStats = playerStats.Clone();
                        FileManager.FM.QueueGlobalSaveAsync(kGlobalSaveContainerName, kSaveFilename, stream =>
                        {
                            try
                            {
                                using (BinaryWriter writer = new BinaryWriter(stream))
                                    saveStats.EncodeWithWriter(writer);

                                Debug.WriteLine("Global GameStats save completed.");
                            }
                            catch (Exception eInner)
                            {
                                Debug.WriteLine("Global GameStats save failed: " + eInner.Message);
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Global GameStats save failed: " + e.Message);
                }
            }
        }

        public void DeleteProgress()
        {

        }

        public void LoadHiScores()
        {
            if (mHiScoreTable != null)
            {
                mHiScoreTable.Load();

                if (mHiScoreTable.NumScores == 0)
                    mHiScoreTable.PreFill(kHiScoreFillMin, kHiScoreFillMax);
            }
        }

        public void SaveHiScores()
        {
            if (mHiScoreTable != null)
                mHiScoreTable.Save();
        }

        public void ResetStats()
        {
            GameStats playerStats = PlayerStats;
            if (playerStats != null)
                playerStats.ResetAllStats();
        }

        public void SaveScore(int score)
        {
            GameStats playerStats = PlayerStats;
            if (score <= 0 || playerStats == null || mHiScoreTable == null)
                return;

            GameController.GC.ThisTurn.WasGameProgressMade = true;

            if (score > playerStats.HiScore)
                playerStats.HiScore = score;
            if (mHiScoreTable != null && mHiScoreTable.InsertScore(score, GamerTag) != -1)
                mHiScoreTable.Save();
        }

        public void QueueUpdateAchievement(int achievementIndex, double percentComplete)
        {
            GameController gc = GameController.GC;
            GameStats playerStats = PlayerStats;
            if (gc.ThisTurn.IsGameOver || playerStats == null || mAchievementDefs == null || achievementIndex >= mAchievementDefs.Count)
                return;

            uint achBit = GameStats.AchievementBitForIndex(achievementIndex);

            if (!AchievementEarned(achBit))
            {
                gc.ThisTurn.WasGameProgressMade = true;
                playerStats.UpdateAchievementPercentComplete(percentComplete, achBit, achievementIndex);

                // TODO: Queue online achievement service update.
            }
        }

        public void SaveAchievement(int achievementIndex, double percentComplete)
        {
            GameStats playerStats = PlayerStats;
            if (playerStats == null || mAchievementDefs == null || achievementIndex >= mAchievementDefs.Count)
                return;
            uint achBit = GameStats.AchievementBitForIndex(achievementIndex);
            playerStats.UpdateAchievementPercentComplete(percentComplete, achBit, achievementIndex);
        }

        public void SubmitQueuedUpdateAchievements()
        {
            // TODO: Submit to online service via service manager.
        }

        public bool AchievementEarned(uint key)
        {
            bool result = false;
            GameStats playerStats = PlayerStats;

            if (playerStats != null)
                result = (playerStats.GetAchievementBit(key) != 0);
	        return result;
        }

        public void OnAchievementEarned(AchievementEarnedEvent ev)
        {
            DispatchEvent(ev);
        }

        public SPDisplayObject StatsCellForIndex(int index, SceneController scene)
        {
            SPSprite sprite = new SPSprite();

            SPImage bgImage = new SPImage(scene.TextureByName(((index & 1) == 0) ? "tableview-cell-light" : "tableview-cell-dark"));
            bgImage.ScaleX = 560f / bgImage.Width;
            bgImage.ScaleY = 64f / bgImage.Height;
            sprite.AddChild(bgImage);

            SPTextField descText = new SPTextField(300, 40, StatsCellDescTextForIndex(index), scene.FontKey, 26);
            descText.X = 14;
            descText.Y = (bgImage.Height - descText.Height) / 2;
            descText.HAlign = SPTextField.SPHAlign.Left;
            descText.VAlign = SPTextField.SPVAlign.Center;
            descText.Color = Color.Black;
            sprite.AddChild(descText);

            SPTextField valueText = new SPTextField(220, 40, StatsCellValueTextForIndex(index, 28, scene), scene.FontKey, 28);
            valueText.X = bgImage.Width - (valueText.Width + 14);
            valueText.Y = (bgImage.Height - valueText.Height) / 2;
            valueText.HAlign = SPTextField.SPHAlign.Right;
            valueText.VAlign = SPTextField.SPVAlign.Center;
            valueText.Color = Color.Black;
            sprite.AddChild(valueText);

            if (index < GameStats.NumProfileStats-1)
            {
                SPImage separatorImage = new SPImage(scene.TextureByName("tableview-cell-divider"));
                separatorImage.ScaleX = bgImage.ScaleX;
                separatorImage.Y = bgImage.Y + bgImage.Height - separatorImage.Height;
                sprite.AddChild(separatorImage);
            }

            return sprite;
        }

        private string StatsCellDescTextForIndex(int index)
        {
            string text = null;

            switch (index)
            {
                case 0: text = "Highest Score"; break;
                case 1: text = "Cannonballs Fired"; break;
                case 2: text = "Cannon Accuracy"; break;
                case 3: text = "2x Ricochets"; break;
                case 4: text = "3x Ricochets"; break;
                case 5: text = "4x Ricochets"; break;
                case 6: text = "5x Ricochets"; break;
                case 7: text = "6x Ricochets"; break;
                case 8: text = "Merchant Ships Sunk"; break;
                case 9: text = "Rival Pirate Ships Sunk"; break;
                case 10: text = "Navy Ships Sunk"; break;
                case 11: text = "Silver Trains Sunk"; break;
                case 12: text = "Treasure Fleets Sunk"; break;
                case 13: text = "Rival Pirates Captured"; break;
                case 14: text = "Plankings"; break;
                case 15: text = "Shark Attacks"; break;
                case 16: text = "Days at Sea"; break;
                default: break;
            }

            return text;
        }

        private string StatsCellValueTextForIndex(int index, int fontSize, SceneController scene)
        {
            GameStats playerStats = PlayerStats;

            if (playerStats == null)
                return null;

            string text = null;

            switch (index)
            {
                case 0: text = GuiHelper.CommaSeparatedValue(playerStats.HiScore); break;
                case 1: text = GuiHelper.CommaSeparatedValue(playerStats.CannonballsShot); break;
                case 2: text = Locale.SanitizedFloat(100f * playerStats.CannonballAccuracy, "F2", scene.FontKey, fontSize) + "%"; break;
                case 3: goto case 7;
                case 4: goto case 7;
                case 5: goto case 7;
                case 6: goto case 7;
                case 7:
                    // Must index from 1 to 5 (eg row-2 for row == 3)
                    text = GuiHelper.CommaSeparatedValue(playerStats.NumRicochetsForHops((uint)index - 2)); break;
                case 8: text = GuiHelper.CommaSeparatedValue(playerStats.MerchantShipsSunk); break;
                case 9: text = GuiHelper.CommaSeparatedValue(playerStats.PirateShipsSunk); break;
                case 10: text = GuiHelper.CommaSeparatedValue(playerStats.NavyShipsSunk); break;
                case 11: text = GuiHelper.CommaSeparatedValue(playerStats.SilverTrainsSunk); break;
                case 12: text = GuiHelper.CommaSeparatedValue(playerStats.TreasureFleetsSunk); break;
                case 13: text = GuiHelper.CommaSeparatedValue(playerStats.Hostages); break;
                case 14: text = GuiHelper.CommaSeparatedValue(playerStats.Plankings); break;
                case 15: text = GuiHelper.CommaSeparatedValue(playerStats.SharkAttacks); break;
                case 16: text = Locale.SanitizedFloat(playerStats.DaysAtSea, "F2", scene.FontKey, fontSize); break;
                default: break;
            }

            return text;
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
#if XBOX
                    SignedInGamer.SignedIn -= SignedInGamer_SignedIn;
                    SignedInGamer.SignedOut -= SignedInGamer_SignedOut;
#endif
                    mGlobalStats = null;
                    mHiScoreTable = null;
                    mProfiles = null;
                }

                mIsDisposed = true;
            }
        }

        ~ProfileManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
