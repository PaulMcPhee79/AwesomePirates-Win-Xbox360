using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using System.Diagnostics;

namespace AwesomePirates
{
    // Using GamerServices: http://msmvps.com/blogs/mykre/archive/2008/03/01/using-gamer-services-in-your-xna-game.aspx
    // XNA Storage: http://msdn.microsoft.com/en-us/library/bb200105.aspx#ID2EWD

    // Note: On Windows, don't pass a PlayerIndex so that all files are saved in the "All Players" folder. Then we can manually
    // manage individual saved games within this "All Players" folder based on the player's name.

    class GameSettings
    {
        // String keys
        public const string SOFTWARE_SETTINGS_VERSION_STRING = "Version_1.0";

        // Boolean setting keys
        public const string PLAYED_BEFORE = "PlayedBefore";
        public const string MONTY_INTRODUCED = "MontyIntroduced";
        public const string DONE_TUTORIAL = "DoneTutorial";
        public const string DONE_TUTORIAL2 = "DoneTutorial2";
        public const string DONE_TUTORIAL3 = "DoneTutorial3";
        public const string DONE_TUTORIAL4 = "DoneTutorial4";
        public const string DONE_TUTORIAL5 = "DoneTutorial5";
        public const string PLANKING_TIPS = "PlankingTips";
        public const string VOODOO_TIPS = "VoodooTips";
        public const string PICKUP_MOLTEN_TIPS = "PickupMoltenTips";
        public const string PICKUP_CRIMSON_TIPS = "PickupCrimsonTips";
        public const string PICKUP_VENOM_TIPS = "PickupVenomTips";
        public const string PICKUP_ABYSSAL_TIPS = "AbyssalShotTips";
        public const string BRANDY_SLICK_TIPS = "BrandySlickTips";
        public const string PLAYER_SHIP_TIPS = "PlayerShipTips";
        public const string TREASURE_FLEET_TIPS = "TreasureFleetTips";
        public const string SILVER_TRAIN_TIPS = "SilverTrainTips";
        public const string SAFE_AREA_INIT = "SafeAreaInit";

        // Int setting keys
        public const string NUM_RUNS_THIS_VERSION = "NumRunsThisVersion";
        public const string NUM_RATING_PROMPTS_THIS_VERSION = "NumRatingPromptsThisVersion";
        public const string POTION_TIPS_INTRO = "PotionTipsIntro";
        public const string SAFE_AREA_INCREMENTS = "SafeAreaIncrements";
        public const string MUSIC_VOLUME = "MusicVolume";
        public const string SFX_VOLUME = "SfxVolume";
        public const string PIRATE_SHIP_TIPS = "PirateShipTips";
        public const string NAVY_SHIP_TIPS = "NavyShipTips";

        // Time settings keys
        public const string RATING_PROMPT_ALARM = "RatingPromptAlarm";
        public const string GC_ACHIEVEMENTS_ALARM = "GCAchievementsAlarm";

        private const string kSaveFileName = "GameSettings.txt";

        private static GameSettings instance = null;

        private GameSettings()
        {
            
        }

        private void SetupGameSettings()
        {
            mDidLastSaveFail = false;
            mHasPerformedInitialLoad = false;
            mDelayedSaveRequired = false;
            mSoftwareVersion = null;
            mBoolSettings = new Dictionary<string, bool>()
            {
                { PLAYED_BEFORE, true }, 
                { MONTY_INTRODUCED, false },
                { DONE_TUTORIAL, false },
                { DONE_TUTORIAL2, false },
                { DONE_TUTORIAL3, false },
                { DONE_TUTORIAL4, false },
                { DONE_TUTORIAL5, false },
                { PLANKING_TIPS, false },
                { VOODOO_TIPS, false },
                { PICKUP_MOLTEN_TIPS, false },
                { PICKUP_CRIMSON_TIPS, false },
                { PICKUP_VENOM_TIPS, false },
                { PICKUP_ABYSSAL_TIPS, false },
                { BRANDY_SLICK_TIPS, false },
                { PLAYER_SHIP_TIPS, false },
                { TREASURE_FLEET_TIPS, false },
                { SILVER_TRAIN_TIPS, false },
                { SAFE_AREA_INIT, false }
            };
            mIntSettings = new Dictionary<string, int>()
            {
                { NUM_RUNS_THIS_VERSION, 0 },
                { POTION_TIPS_INTRO, 0 },
                { SAFE_AREA_INCREMENTS, SceneController.kSafeAreaMaxIncrements },
                { MUSIC_VOLUME, 10 },
                { SFX_VOLUME, 10 },
                { PIRATE_SHIP_TIPS, 0 },
                { NAVY_SHIP_TIPS, 0 }
            };
        }

        public static GameSettings GS
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameSettings();
                    instance.SetupGameSettings();
                }
                return instance;
            }
        }

        #region Fields
        private bool mDidLastSaveFail;
        private bool mHasPerformedInitialLoad;
        private bool mDelayedSaveRequired;
        private string mSoftwareVersion;

        private Dictionary<string, bool> mBoolSettings;
        private Dictionary<string, int> mIntSettings;
        #endregion

        #region Properties
        public bool DidLastSaveFail { get { return mDidLastSaveFail; } }
        public bool HasPerformedInitialLoad { get { return mHasPerformedInitialLoad; } }
        public bool DelayedSaveRequired { get { return mDelayedSaveRequired; } }
        public bool IsInitialVersion { get { return SOFTWARE_SETTINGS_VERSION_STRING.Equals("Version_1.0"); } }
        public bool IsNewVersion { get { return (mSoftwareVersion != null && !mSoftwareVersion.Equals(SOFTWARE_SETTINGS_VERSION_STRING)); } }
        #endregion

        #region Methods
        private void SetBoolSettings(Dictionary<string, bool> dictionary)
        {
            if (dictionary != mBoolSettings)
            {
                foreach (KeyValuePair<string, bool> kvp in dictionary)
                    mBoolSettings[kvp.Key] = kvp.Value;
            }
        }

        private void SetIntSettings(Dictionary<string, int> dictionary)
        {
            if (dictionary != mIntSettings)
            {
                foreach (KeyValuePair<string, int> kvp in dictionary)
                    mIntSettings[kvp.Key] = kvp.Value;
            }
        }

        private void SetAnnoyingTipsCompleted()
        {
            SetSettingForKey(MONTY_INTRODUCED, true);
            SetSettingForKey(DONE_TUTORIAL, true);
            SetSettingForKey(DONE_TUTORIAL2, true);
            SetSettingForKey(DONE_TUTORIAL3, true);
            SetSettingForKey(DONE_TUTORIAL4, true);
            SetSettingForKey(DONE_TUTORIAL5, true);
            SetSettingForKey(PLANKING_TIPS, true);
            SetSettingForKey(VOODOO_TIPS, true);
            SetSettingForKey(PICKUP_MOLTEN_TIPS, true);
            SetSettingForKey(PICKUP_CRIMSON_TIPS, true);
            SetSettingForKey(PICKUP_VENOM_TIPS, true);
            SetSettingForKey(PICKUP_ABYSSAL_TIPS, true);
            SetSettingForKey(BRANDY_SLICK_TIPS, true);
            SetSettingForKey(PLAYER_SHIP_TIPS, true);
            SetSettingForKey(TREASURE_FLEET_TIPS, true);
            SetSettingForKey(SILVER_TRAIN_TIPS, true);
            SetSettingForKey(POTION_TIPS_INTRO, true);
            SetValueForKey(POTION_TIPS_INTRO, 2);
            SetValueForKey(PIRATE_SHIP_TIPS, 5);
            SetValueForKey(NAVY_SHIP_TIPS, 5);
        }

        public void ResetTutorialPrompts()
        {
            SetSettingForKey(DONE_TUTORIAL, false);
            SetSettingForKey(DONE_TUTORIAL2, false);
            SetSettingForKey(DONE_TUTORIAL3, false);
            SetSettingForKey(DONE_TUTORIAL4, false);
            SetSettingForKey(DONE_TUTORIAL5, false);
            SetSettingForKey(PLANKING_TIPS, false);
            SetSettingForKey(VOODOO_TIPS, false);
            SetSettingForKey(PICKUP_MOLTEN_TIPS, false);
            SetSettingForKey(PICKUP_CRIMSON_TIPS, false);
            SetSettingForKey(PICKUP_VENOM_TIPS, false);
            SetSettingForKey(PICKUP_ABYSSAL_TIPS, false);
            SetSettingForKey(BRANDY_SLICK_TIPS, false);
            SetSettingForKey(PLAYER_SHIP_TIPS, false);
            SetSettingForKey(TREASURE_FLEET_TIPS, false);
            SetSettingForKey(SILVER_TRAIN_TIPS, false);
            SetSettingForKey(POTION_TIPS_INTRO, false);
            SetValueForKey(POTION_TIPS_INTRO, 0);
            SetValueForKey(PIRATE_SHIP_TIPS, 0);
            SetValueForKey(NAVY_SHIP_TIPS, 0);
        }

        public bool SettingForKey(string key)
        {
            return mBoolSettings[key];
        }

        public void SetSettingForKey(string key, bool value)
        {
            mBoolSettings[key] = value;
            mDelayedSaveRequired = true;
        }

        public int ValueForKey(string key)
        {
            return mIntSettings[key];
        }

        public void SetValueForKey(string key, int value)
        {
            mIntSettings[key] = value;
            mDelayedSaveRequired = true;
        }

        protected virtual GameSettings Clone()
        {
            GameSettings clone = MemberwiseClone() as GameSettings;
            clone.mBoolSettings = new Dictionary<string,bool>(mBoolSettings);
            clone.mIntSettings = new Dictionary<string,int>(mIntSettings);
            return clone;
        }

        public void LoadSettings()
        {
            mHasPerformedInitialLoad = true;

            try
            {
                if (FileManager.FM.IsReadyGlobal() && FileManager.FM.FileExistsGlobal(FileManager.kSharedStorageContainerName, kSaveFileName))
                {
                    FileManager.FM.LoadGlobal(FileManager.kSharedStorageContainerName, kSaveFileName, stream =>
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                bool parseError = false;
                                string[] delim = new string[] { "=" };
                                while (!reader.EndOfStream)
                                {
                                    string line = reader.ReadLine();
                                    if (line != null)
                                    {
                                        string[] tokens = line.Split(delim, StringSplitOptions.None);
                                        if (tokens != null && tokens.Length == 2)
                                        {
                                            for (int i = 0; i < tokens.Length; ++i)
                                                tokens[i] = tokens[i].Trim();

                                            if (mBoolSettings.ContainsKey(tokens[0]))
                                            {
                                                if (parseError)
                                                    continue;

                                                bool boolValue;
                                                if (bool.TryParse(tokens[1], out boolValue))
                                                    SetSettingForKey(tokens[0], boolValue);
                                                else
                                                {
                                                    parseError = true;
                                                    SetAnnoyingTipsCompleted();
                                                }
                                            }
                                            else if (mIntSettings.ContainsKey(tokens[0]))
                                            {
                                                int intValue;
                                                if (int.TryParse(tokens[1], out intValue))
                                                    SetValueForKey(tokens[0], intValue);
                                            }
                                        }
                                    }
                                }
                            }

                            mDelayedSaveRequired = false;
                            Debug.WriteLine("GameSettings load complete.");
                        });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public void SaveSettings()
        {
            try
            {
                GameSettings clone = Clone();
                FileManager.FM.QueueGlobalSaveAsync(FileManager.kSharedStorageContainerName, kSaveFileName, stream =>
                {
                    try
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            foreach (KeyValuePair<string, bool> kvp in clone.mBoolSettings)
                                writer.WriteLine(kvp.Key + " = " + kvp.Value);
                            foreach (KeyValuePair<string, int> kvp in clone.mIntSettings)
                                writer.WriteLine(kvp.Key + " = " + kvp.Value);
                        }

                        Debug.WriteLine("GameSettings save complete.");
                    }
                    catch (Exception eInner)
                    {
                        Debug.WriteLine(eInner.Message);
                    }
                });

                mDelayedSaveRequired = false;
                mDidLastSaveFail = false;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        #endregion
    }
}
