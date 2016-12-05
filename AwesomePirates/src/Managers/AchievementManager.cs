using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class AchievementManager : SPEventDispatcher, IDisposable
    {
        public static float AchPotShotDistSq = 900000f;

        public const string CUST_EVENT_TYPE_PLAYER_EATEN = "playerEatenEvent";

        public const int ACHIEVEMENT_INDEX_MIN = 0;
        public const int ACHIEVEMENT_INDEX_88_MPH = 0;
        public const int ACHIEVEMENT_INDEX_POT_SHOT = 1;
        public const int ACHIEVEMENT_INDEX_DEADEYE_DAVY = 2;
        public const int ACHIEVEMENT_INDEX_SMORGASBORD = 3;
        public const int ACHIEVEMENT_INDEX_CLOSE_BUT_NO_CIGAR = 4;
        public const int ACHIEVEMENT_INDEX_NO_PLACE_LIKE_HOME = 5;
        public const int ACHIEVEMENT_INDEX_ENTRAPMENT = 6;
        public const int ACHIEVEMENT_INDEX_FRIENDLY_FIRE = 7;
        public const int ACHIEVEMENT_INDEX_KABOOM = 8;
        public const int ACHIEVEMENT_INDEX_DEEP_FRIED = 9;
        public const int ACHIEVEMENT_INDEX_ROYAL_FLUSH = 10;
        public const int ACHIEVEMENT_INDEX_SLIMER = 11;
        public const int ACHIEVEMENT_INDEX_RICOCHET_MASTER = 12;
        public const int ACHIEVEMENT_INDEX_SCOURGE_OF_THE_7_SEAS = 13;
        public const int ACHIEVEMENT_INDEX_MASTER_PLANKER = 14;
        public const int ACHIEVEMENT_INDEX_ROBBIN_DA_HOOD = 15;
        public const int ACHIEVEMENT_INDEX_BOOM_SHAKALAKA = 16;
        public const int ACHIEVEMENT_INDEX_LIKE_A_RECORD_BABY = 17;
        public const int ACHIEVEMENT_INDEX_ROAD_TO_DAMASCUS = 18;
        public const int ACHIEVEMENT_INDEX_WELL_DONE = 19;
        public const int ACHIEVEMENT_INDEX_DAVY_JONES_LOCKER = 20;
        public const int ACHIEVEMENT_INDEX_COPS_AND_ROBBERS = 21;
        public const int ACHIEVEMENT_INDEX_STEAM_TRAIN = 22;
        public const int ACHIEVEMENT_INDEX_BETTER_CALL_SAUL = 23;
        public const int ACHIEVEMENT_INDEX_MAX = 23;

        public const uint ACHIEVEMENT_BIT_88_MPH = (1 << 0);
        public const uint ACHIEVEMENT_BIT_POT_SHOT = (1 << 1);
        public const uint ACHIEVEMENT_BIT_DEADEYE_DAVY = (1 << 2);
        public const uint ACHIEVEMENT_BIT_SMORGASBORD = (1 << 3);
        public const uint ACHIEVEMENT_BIT_CLOSE_BUT_NO_CIGAR = (1 << 4);
        public const uint ACHIEVEMENT_BIT_NO_PLACE_LIKE_HOME = (1 << 5);
        public const uint ACHIEVEMENT_BIT_ENTRAPMENT = (1 << 6);
        public const uint ACHIEVEMENT_BIT_FRIENDLY_FIRE = (1 << 7);
        public const uint ACHIEVEMENT_BIT_KABOOM = (1 << 8);
        public const uint ACHIEVEMENT_BIT_DEEP_FRIED = (1 << 9);
        public const uint ACHIEVEMENT_BIT_ROYAL_FLUSH = (1 << 10);
        public const uint ACHIEVEMENT_BIT_SLIMER = (1 << 11);
        public const uint ACHIEVEMENT_BIT_RICOCHET_MASTER = (1 << 12);
        public const uint ACHIEVEMENT_BIT_SCOURGE_OF_THE_7_SEAS = (1 << 13);
        public const uint ACHIEVEMENT_BIT_MASTER_PLANKER = (1 << 14);
        public const uint ACHIEVEMENT_BIT_ROBBIN_DA_HOOD = (1 << 15);
        public const uint ACHIEVEMENT_BIT_BOOM_SHAKALAKA = (1 << 16);
        public const uint ACHIEVEMENT_BIT_LIKE_A_RECORD_BABY = (1 << 17);
        public const uint ACHIEVEMENT_BIT_ROAD_TO_DAMASCUS = (1 << 18);
        public const uint ACHIEVEMENT_BIT_WELL_DONE = (1 << 19);
        public const uint ACHIEVEMENT_BIT_DAVY_JONES_LOCKER = (1 << 20);
        public const uint ACHIEVEMENT_BIT_COPS_AND_ROBBERS = (1 << 21);
        public const uint ACHIEVEMENT_BIT_STEAM_TRAIN = (1 << 22);
        public const uint ACHIEVEMENT_BIT_BETTER_CALL_SAUL = (1 << 23);
        public const int ACHIEVEMENT_COUNT = 24;
        public const uint k88_MPH_BUTTON_TAG = 0x1010;
        public const double kAchievementCompletePercent = 100.0;

        private const int kComboMax = 3;

        public AchievementManager()
        {
            mTimeOfDay = AwesomePirates.TimeOfDay.Dawn;
		    mSuspendedMode = false;
		    mDelaySavingAchievements = false;
		    mConsecutiveCannonballsHit = 0;
		    mFriendlyFires = 0;
		    mKabooms = 0;
            mSlimerCount = 0;
		    mComboMultiplier = 0;
		    mComboMultiplierMax = kComboMax;
		    mDisplayQueue = new List<Dictionary<string,object>>();
            mComboChangedEvent = null;
		    mView = null;
		    mCombatTextOwner = null;
		    mCombatText = null;
		
		    mAchievementDefs = PlistParser.ArrayFromPlist("data/plists/Achievements.plist");
		    mProfileManager = new AwesomePirates.ProfileManager(mAchievementDefs);
            mProfileManager.AddEventListener(AchievementEarnedEvent.CUST_EVENT_TYPE_ACHIEVEMENT_EARNED, (AchievementEarnedEventHandler)OnAchievementEarned);
        }
        
        #region Fields
        private bool mIsDisposed = false;
        private TimeOfDay mTimeOfDay;

        private bool mSuspendedMode;
        private bool mDelaySavingAchievements;
        private bool mDelayedSaveRequired;

        private uint mConsecutiveCannonballsHit;
        private uint mFriendlyFires;
        private uint mKabooms;
        private uint mSlimerCount;
        private int mComboMultiplier;
        private int mComboMultiplierMax;
        private NumericValueChangedEvent mComboChangedEvent;

        private List<object> mAchievementDefs;
        private List<Dictionary<string, object>> mDisplayQueue;

        private AchievementPanel mView;
        private string mCombatTextOwner;
        private CombatText mCombatText;

        private ProfileManager mProfileManager;
        #endregion

        #region Properties
        public bool DelaySavingAchievements { get { return mDelaySavingAchievements; } set { mDelaySavingAchievements = value; } }
        public GameStats Stats { get { return (mProfileManager != null) ? mProfileManager.PlayerStats : null; } }
        public AchievementPanel View
        {
            get { return mView; }
            set
            {
                if (mView == value)
		            return;
	            if (mView != null)
                {
                    mView.RemoveEventListener(AchievementPanel.CUST_EVENT_TYPE_ACHIEVEMENT_HIDDEN, (SPEventHandler)OnAchievementHidden);
                    mView = null;
	            }
	            mView = value;
                mView.AddEventListener(AchievementPanel.CUST_EVENT_TYPE_ACHIEVEMENT_HIDDEN, (SPEventHandler)OnAchievementHidden);
            }
        }
        public TimeOfDay TimeOfDay { get { return mTimeOfDay; } set { mTimeOfDay = value; } }
        public uint Kabooms
        {
            get { return mKabooms; }
            set
            {
                mKabooms = value;

                if (mKabooms >= 12 && !AchievementEarned(ACHIEVEMENT_BIT_KABOOM))
                    SaveAchievement(ACHIEVEMENT_INDEX_KABOOM, kAchievementCompletePercent);
            }
        }
        public uint SlimerCount
        {
            get { return mSlimerCount; }
            set
            {
                mSlimerCount = value;

                if (mSlimerCount >= 15 && !AchievementEarned(ACHIEVEMENT_BIT_SLIMER))
                    SaveAchievement(ACHIEVEMENT_INDEX_SLIMER, kAchievementCompletePercent);
            }
        }
        public int ComboMultiplierMax { get { return mComboMultiplierMax; } set { mComboMultiplierMax = kComboMax; } }
        public bool IsComboMultiplierMaxed { get { return mComboMultiplier == mComboMultiplierMax; } }
        public ProfileManager ProfileManager { get { return mProfileManager; } }
        public HiScoreTable HiScores { get { return (mProfileManager != null) ? mProfileManager.HiScores : null; } }
        public int NumAchievements { get { return ACHIEVEMENT_COUNT; } }
        public int NumAchievementsCompleted { get { return mProfileManager.PlayerStats.NumAchievementsCompleted; } }
        public uint Hostages { get { return mProfileManager.PlayerStats.Hostages; } set { mProfileManager.PlayerStats.Hostages = value; } }
        public float DaysAtSea { get { return mProfileManager.PlayerStats.DaysAtSea; } set { mProfileManager.PlayerStats.DaysAtSea = value; } }
        public GameMode GameMode { get; set; }
        #endregion

        #region Methods
        public void EnableSuspendedMode(bool enable)
        {
            mSuspendedMode = enable;
        }

        public void LoadCombatTextWithCategory(int category, int bufferSize, string owner)
        {
            if (mCombatText == null)
            {
                mCombatText = new CombatText(category, bufferSize);
                mCombatTextOwner = owner;
            }
        }

        public void FillCombatTextCache()
        {
            if (mCombatText != null)
                mCombatText.FillCombatSpriteCache();
        }

        public void ResetCombatTextCache()
        {
            if (mCombatText != null)
                mCombatText.ResetCombatSpriteCache();
        }

        public void UnloadCombatTextWithOwner(string owner)
        {
            if (mCombatText != null && mCombatTextOwner != null && owner.Equals(mCombatTextOwner))
            {
                mCombatText.Dispose();
                mCombatText = null;
                mCombatTextOwner = null;
            }
        }

        public void SetCombatTextColor(Color color)
        {
            if (mCombatText != null)
                mCombatText.TextColor = color;
        }

        public void HideCombatText()
        {
            mCombatText.HideAllText();
        }

        public void ResetStats()
        {
            mProfileManager.ResetStats();
        }

        public void PrepareForNewGame()
        {
            mConsecutiveCannonballsHit = 0;
	        mFriendlyFires = 0;
	        mKabooms = 0;
            mSlimerCount = 0;
            ResetComboMultiplier();
            mCombatText.PrepareForNewGame();
            mProfileManager.PrepareForNewGame();
        }

        public void SaveProgress()
        {
            mDelayedSaveRequired = false;
            mProfileManager.SaveProgress(mProfileManager.MainPlayerIndex);
            // TODO: Submit queued update achievements to online service.
        }

        public void ProcessDelayedSaves()
        {
            if (mDelayedSaveRequired)
                SaveProgress();
        }

        private void SaveAchievement(int achievementIndex, double percentComplete)
        {
            if (GameMode != AwesomePirates.GameMode.Career)
                return;

            if (DelaySavingAchievements)
            {
                mDelayedSaveRequired = true;
                mProfileManager.QueueUpdateAchievement(achievementIndex, percentComplete);
	        }
            else
            {
                SaveProgress();
                mProfileManager.SaveAchievement(achievementIndex, percentComplete);
            }
        }

        public void SaveScore(int score)
        {
            mProfileManager.SaveScore(score);
        }

        public void BroadcastComboMultiplier()
        {
            SetComboMultiplier(mComboMultiplier);
        }

        public void ResetComboMultiplier()
        {
            ComboMultiplierMax = kComboMax;
            SetComboMultiplier(0);
        }

        private void SetComboMultiplier(int value)
        {
            int oldValue = mComboMultiplier;
            mComboMultiplier = Math.Max(0, Math.Min(mComboMultiplierMax, value));

            if (mComboChangedEvent == null)
                mComboChangedEvent = new NumericValueChangedEvent(NumericValueChangedEvent.CUST_EVENT_TYPE_COMBO_MULTIPLIER_CHANGED, mComboMultiplier, oldValue);
            else
                mComboChangedEvent.UpdateValues(mComboMultiplier, oldValue);
            DispatchEvent(mComboChangedEvent);
        }

        public void OnInfamyChanged(NumericValueChangedEvent ev)
        {
            int value = ev.IntValue;

            if (value >= 10000000)
            {
                if (!AchievementEarned(ACHIEVEMENT_BIT_SCOURGE_OF_THE_7_SEAS))
                    SaveAchievement(ACHIEVEMENT_INDEX_SCOURGE_OF_THE_7_SEAS, kAchievementCompletePercent);
            }
        }

        public void PrisonerPushedOverboard()
        {
            GameController gc = GameController.GC;

            ++mProfileManager.PlayerStats.Plankings;
            gc.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_PLANKING);
            GrantMasterPlankerAchievement();
        }

        public void PrisonerKilled(OverboardActor prisoner)
        {
            GameController gc = GameController.GC;
            int totalInfamyBonus = 0;
	        float infamyBonus = 0;
            bool crit = IsComboMultiplierMaxed;
    
            if (gc.ThisTurn.IsGameOver || prisoner.IsPreparingForNewGame)
                return;
    
            if (prisoner.IsPlayer)
            {
                crit = true;
                infamyBonus = prisoner.InfamyBonus;
                totalInfamyBonus = gc.ThisTurn.AddInfamyUnfiltered(infamyBonus);
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_PLAYER_EATEN));
            }
            else
            {
                int basePrisonerScore = Globals.OVERBOARD_SCORE_BONUS;
                infamyBonus = basePrisonerScore * (crit ? Globals.CRIT_FACTOR : 1f);

                if (prisoner.DeathBitmap == DeathBitmaps.SHARK)
                    ++mProfileManager.PlayerStats.SharkAttacks;
                else if (prisoner.DeathBitmap == DeathBitmaps.ACID_POOL && prisoner.Prisoner != null && prisoner.Prisoner.Planked)
                {
                    ++mProfileManager.PlayerStats.AcidPlankings;
                    GrantBetterCallSaulAchievement();
                }
        
                if ((prisoner.DeathBitmap & DeathBitmaps.SHARK) != 0)
                    infamyBonus = infamyBonus * Potion.BloodlustFactorForPotion(mProfileManager.PlayerStats.PotionForKey(Potion.POTION_BLOODLUST));
                infamyBonus = gc.MasteryManager.ApplyScoreBonus(infamyBonus, prisoner);
                totalInfamyBonus = gc.ThisTurn.AddInfamy(infamyBonus);
            }

            DisplaySharkInfamyBonus(totalInfamyBonus, prisoner.X, prisoner.Y, true, crit);
        }

        public void GrantMasterPlankerAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_MASTER_PLANKER))
                SaveAchievement(ACHIEVEMENT_INDEX_MASTER_PLANKER, mProfileManager.PlayerStats.PercentComplete(ACHIEVEMENT_BIT_MASTER_PLANKER));
        }

        public void GrantSmorgasbordAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_SMORGASBORD))
                SaveAchievement(ACHIEVEMENT_INDEX_SMORGASBORD, kAchievementCompletePercent);
        }

        public void GrantCloseButNoCigarAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_CLOSE_BUT_NO_CIGAR))
                SaveAchievement(ACHIEVEMENT_INDEX_CLOSE_BUT_NO_CIGAR, kAchievementCompletePercent);
        }

        public void GrantNoPlaceLikeHomeAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_NO_PLACE_LIKE_HOME))
                SaveAchievement(ACHIEVEMENT_INDEX_NO_PLACE_LIKE_HOME, kAchievementCompletePercent);
        }

        public void GrantEntrapmentAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_ENTRAPMENT))
                SaveAchievement(ACHIEVEMENT_INDEX_ENTRAPMENT, kAchievementCompletePercent);
        }

        public void GrantDeepFriedAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_DEEP_FRIED))
                SaveAchievement(ACHIEVEMENT_INDEX_DEEP_FRIED, kAchievementCompletePercent);
        }

        public void GrantRoyalFlushAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_ROYAL_FLUSH))
                SaveAchievement(ACHIEVEMENT_INDEX_ROYAL_FLUSH, kAchievementCompletePercent);
        }

        public bool HasCopsAndRobbersAchievement()
        {
            return AchievementEarned(ACHIEVEMENT_BIT_COPS_AND_ROBBERS);
        }

        public void GrantCopsAndRobbersAchievement()
        {
            if (!HasCopsAndRobbersAchievement())
                SaveAchievement(ACHIEVEMENT_INDEX_COPS_AND_ROBBERS, kAchievementCompletePercent);
        }

        public void GrantSteamTrainAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_STEAM_TRAIN))
                SaveAchievement(ACHIEVEMENT_INDEX_STEAM_TRAIN, kAchievementCompletePercent);
        }

        public void GrantBetterCallSaulAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_BETTER_CALL_SAUL))
                SaveAchievement(ACHIEVEMENT_INDEX_BETTER_CALL_SAUL, kAchievementCompletePercent);
        }

        public void Grant88MphAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_88_MPH))
                SaveAchievement(ACHIEVEMENT_INDEX_88_MPH, kAchievementCompletePercent);
        }

        public void GrantRicochetAchievement(uint ricochetCount)
        {
            if (ricochetCount > 0)
            {
                mProfileManager.PlayerStats.AddRicochets(1, ricochetCount);

                if (ricochetCount == 5 && !AchievementEarned(ACHIEVEMENT_BIT_RICOCHET_MASTER))
                    SaveAchievement(ACHIEVEMENT_INDEX_RICOCHET_MASTER, kAchievementCompletePercent);
            }
        }

        public void GrantRobbinDaHoodAchievement()
        {
            if (!AchievementEarned(ACHIEVEMENT_BIT_ROBBIN_DA_HOOD))
                SaveAchievement(ACHIEVEMENT_INDEX_ROBBIN_DA_HOOD, mProfileManager.PlayerStats.PercentComplete(ACHIEVEMENT_BIT_ROBBIN_DA_HOOD));
        }

        private void GrantAchievementForDeathBitmap(uint deathBitmap)
        {
            int achIndex = 0;
            uint achBit = 0;
    
            switch (deathBitmap)
            {
                case DeathBitmaps.POWDER_KEG:
                    achBit = ACHIEVEMENT_BIT_BOOM_SHAKALAKA;
                    achIndex = ACHIEVEMENT_INDEX_BOOM_SHAKALAKA;
                    break;
                case DeathBitmaps.WHIRLPOOL:
                    achBit = ACHIEVEMENT_BIT_LIKE_A_RECORD_BABY;
                    achIndex = ACHIEVEMENT_INDEX_LIKE_A_RECORD_BABY;
                    break;
                case DeathBitmaps.DAMASCUS:
                    achBit = ACHIEVEMENT_BIT_ROAD_TO_DAMASCUS;
                    achIndex = ACHIEVEMENT_INDEX_ROAD_TO_DAMASCUS;
                    break;
                case DeathBitmaps.BRANDY_SLICK:
                    achBit = ACHIEVEMENT_BIT_WELL_DONE;
                    achIndex = ACHIEVEMENT_INDEX_WELL_DONE;
                    break;
                case DeathBitmaps.HAND_OF_DAVY:
                    achBit = ACHIEVEMENT_BIT_DAVY_JONES_LOCKER;
                    achIndex = ACHIEVEMENT_INDEX_DAVY_JONES_LOCKER;
                    break;
                default: break;
            }
    
            if (achBit != 0 && !AchievementEarned(achBit))
                SaveAchievement(achIndex, mProfileManager.PlayerStats.PercentComplete(achBit));
        }

        public void PlayerHitShip(ShipActor ship, float distSq, bool ricocheted)
        {
            if (ship == null)
                return;

            GameController gc = GameController.GC;
            ++mConsecutiveCannonballsHit;
    
	        if (!ricocheted)
            {
                ++gc.ThisTurn.CannonballsShot;
                ++gc.ThisTurn.CannonballsHit;
                gc.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_SHOT_FIRED);

                if (distSq > AchPotShotDistSq && !AchievementEarned(ACHIEVEMENT_BIT_POT_SHOT))
                    SaveAchievement(ACHIEVEMENT_INDEX_POT_SHOT, kAchievementCompletePercent);
	        }

            if (gc.PlayerShip != null && gc.PlayerShip.IsCamouflaged && ship is NavyShip)
            {
		        ++mFriendlyFires;
		
		        if (mFriendlyFires == 5 && !AchievementEarned(ACHIEVEMENT_BIT_FRIENDLY_FIRE))
                    SaveAchievement(ACHIEVEMENT_INDEX_FRIENDLY_FIRE, kAchievementCompletePercent);
	        }
	
	        if (mConsecutiveCannonballsHit == 100 && !AchievementEarned(ACHIEVEMENT_BIT_DEADEYE_DAVY))
                SaveAchievement(ACHIEVEMENT_INDEX_DEADEYE_DAVY, kAchievementCompletePercent);
        }

        public void PlayerMissed(uint procType)
        {
            GameController gc = GameController.GC;
    
	        if (!mSuspendedMode)
            {
		        mConsecutiveCannonballsHit = 0;
                ++gc.ThisTurn.CannonballsShot;

                if ((gc.MasteryManager.MasteryBitmap & CCMastery.CANNON_CAPTAINS_REPRIEVE) != 0)
                {
                    if (gc.NextRandom(0, 100) <= 50)
                        SetComboMultiplier(mComboMultiplier - 1);
                }
                else
                {
                    SetComboMultiplier(mComboMultiplier - 1);
                }
                
                gc.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_SHOT_FIRED);
                gc.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_SHOT_MISSED);
	        }
        }

        public void DisplayCannonInfamyBonus(int bonus, float x, float y, bool crit, bool twoBy = false)
        {
            mCombatText.DisplayCombatText(bonus, x, y, crit, twoBy, CombatText.CTColorType.CannonCrit); //(crit) ? CombatText.CTColorType.CannonCrit : CombatText.CTColorType.NonCrit);
        }

        public void DisplaySharkInfamyBonus(int bonus, float x, float y, bool crit, bool twoBy = false)
        {
            mCombatText.DisplayCombatText(bonus, x, y, crit, twoBy, CombatText.CTColorType.SharkCrit); //(crit) ? CombatText.CTColorType.SharkCrit : CombatText.CTColorType.NonCrit);
        }

        public void MerchantShipSunk(ShipActor ship)
        {
            if (ship.IsPreparingForNewGame)
                return;
            ++mProfileManager.PlayerStats.MerchantShipsSunk;
            EnemyShipSunk(ship);
        }

        public void PirateShipSunk(ShipActor ship)
        {
            if (ship.IsPreparingForNewGame)
                return;
            ++mProfileManager.PlayerStats.PirateShipsSunk;

            GameController gc = GameController.GC;
            if (gc.PlayerShip != null && gc.PlayerShip.IsCamouflaged)
            {
                mProfileManager.PlayerStats.ShipSunkWithDeathBitmap(DeathBitmaps.DAMASCUS);
                GrantAchievementForDeathBitmap(DeathBitmaps.DAMASCUS);
            }

            EnemyShipSunk(ship);
        }

        public void NavyShipSunk(ShipActor ship)
        {
            if (ship.IsPreparingForNewGame)
                return;
            ++mProfileManager.PlayerStats.NavyShipsSunk;
            EnemyShipSunk(ship);
        }

        public void EscortShipSunk(ShipActor ship)
        {
            if (ship.IsPreparingForNewGame)
                return;
            ++mProfileManager.PlayerStats.EscortShipsSunk;
            EnemyShipSunk(ship);
        }

        public void SilverTrainSunk(ShipActor ship)
        {
            if (ship.IsPreparingForNewGame)
                return;
            ++mProfileManager.PlayerStats.SilverTrainsSunk;

            if (ship.DeathBitmap == DeathBitmaps.SEA_OF_LAVA)
                GrantSteamTrainAchievement();

            EnemyShipSunk(ship);
        }

        public void TreasureFleetSunk(ShipActor ship)
        {
            if (ship.IsPreparingForNewGame)
                return;
            ++mProfileManager.PlayerStats.TreasureFleetsSunk;
            EnemyShipSunk(ship);
        }

        private void EnemyShipSunk(ShipActor ship)
        {
            GameController gc = GameController.GC;
    
            if (gc.ThisTurn.IsGameOver || ship.IsPreparingForNewGame)
                return;
    
            gc.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_SINKING, ship);
    
            // Death bitmap achievements
            uint achDeathBitmap = ship.DeathBitmap;
            mProfileManager.PlayerStats.ShipSunkWithDeathBitmap(achDeathBitmap);
            GrantAchievementForDeathBitmap(achDeathBitmap);
    
            // Mutiny Reduction
            ++gc.ThisTurn.ShipsSunk;

            float mutinyReduction = ship.MutinyReduction * Potion.ResurgenceFactorForPotion(mProfileManager.PlayerStats.PotionForKey(Potion.POTION_RESURGENCE));
            if ((ship.DeathBitmap & DeathBitmaps.VOODOO) != 0 && (gc.MasteryManager.MasteryBitmap & CCMastery.VOODOO_ENCHANTED_REJUVENATION) != 0)
                mutinyReduction = mutinyReduction * 2;
            gc.ThisTurn.ReduceMutinyCountdown(mutinyReduction);
    
            // Score
	        bool crit = IsComboMultiplierMaxed;
	        float infamyBonus = 0;
	
	        if ((ship.DeathBitmap & DeathBitmaps.PLAYER_CANNON) != 0)
            {
		        // Apply cannon kill multipliers
                infamyBonus = (ship.RicochetBonus + ship.SunkByPlayerCannonInfamyBonus) * (crit ? Globals.CRIT_FACTOR : 1f);
                SetComboMultiplier(mComboMultiplier+1);
	        }
            else
            {
		        // Apply Voodoo/Munition kill multipliers
                infamyBonus = ship.InfamyBonus * (crit ? Globals.CRIT_FACTOR : 1f);
	        }

            infamyBonus = gc.MasteryManager.ApplyScoreBonus(infamyBonus, ship);
	        int totalInfamyBonus = gc.ThisTurn.AddInfamy(infamyBonus);
            DisplayCannonInfamyBonus(totalInfamyBonus, ship.CenterX, ship.CenterY, true, crit);
        }

        // For testing purposes
        public void RandomAchievement()
        {
            mDisplayQueue.Add(mAchievementDefs[GameController.GC.NextRandom(ACHIEVEMENT_INDEX_MIN, ACHIEVEMENT_INDEX_MAX)] as Dictionary<string, object>);
        }

        public bool AchievementEarned(uint key)
        {
            return mProfileManager.AchievementEarned(key);
        }

        private void PlayAchievementSound()
        {
            GameController.GC.XAudioPlayer.Play("Achievement");
        }

        private void DisplayAchievement(int index)
        {
            if (index >= 0 && index < mAchievementDefs.Count)
                mDisplayQueue.Add(mAchievementDefs[index] as Dictionary<string,object>);
        }

        public void OnAchievementEarned(AchievementEarnedEvent ev)
        {
            DisplayAchievement((int)ev.Index);
        }

        public void OnTimeOfDayChanged(TimeOfDayChangedEvent ev)
        {
            mTimeOfDay = ev.TimeOfDay;
        }

        public void Flip(bool enable)
        {
            mView.Flip(enable);
        }

        public void AdvanceTime(double time)
        {
            PumpAchievementQueue();
        }

        private void DisplayView()
        {
            if (mView != null)
                mView.Display();
        }

        private void PumpAchievementQueue()
        {
            if (mDisplayQueue == null || mDisplayQueue.Count == 0 || mView == null || mView.Busy)
		        return;
	        Dictionary<string, object> achievement = mDisplayQueue[0] as Dictionary<string, object>;
	        mView.Title = achievement["name"] as string;
	        mView.Text = achievement["earnedDesc"] as string;
	
	        uint tier = Convert.ToUInt32(achievement["tier"]);
	        mView.Tier = tier;
            mView.Display();
            mDisplayQueue.RemoveAt(0);
            PlayAchievementSound();
        }

        public void OnAchievementHidden(SPEvent ev)
        {
            // May remove this function
        }

        public SPDisplayObject StatsCellForIndex(int index, SceneController scene)
        {
            SPDisplayObject cell = null;

            if (mProfileManager != null)
                cell = mProfileManager.StatsCellForIndex(index, scene);
            return cell;
        }

        public SPDisplayObject AchievementCellForIndex(int index, SceneController scene)
        {
            if (scene == null)
                return null;

            int achRow = index - 1;
            SPSprite sprite = new SPSprite();
            SPImage bgImage = null, iconImage = null, prizeImage, separatorImage = null, trailImage = null, xMarksImage = null, miscImage = null;
            SPTextField headerText = null, bodyText = null, percentCompleteText = null, pointsText = null, miscText = null;
            SXGauge gauge = null;
            SPButton button = null;
            MenuButton menuButton = null;

            switch (achRow)
            {
                case -1:
                    {
                        bgImage = new SPImage(scene.TextureByName("tableview-cell-light"));
                        bgImage.ScaleX = 560f / bgImage.Width;
                        sprite.AddChild(bgImage);

                        iconImage = new SPImage(scene.TextureByName("achievements-icon"));
                        iconImage.X = 16;
                        iconImage.Y = (bgImage.Height - iconImage.Height) / 2;
                        sprite.AddChild(iconImage);

                        uint totalAchPoints = mProfileManager.PlayerStats.TotalAchievementPoints(mAchievementDefs);
                        uint earnedAchPoints = mProfileManager.PlayerStats.EarnedAchievementPoints(mAchievementDefs);
                        headerText = new SPTextField(420, 40, String.Format("Treason Points: {0}/{1}", earnedAchPoints, totalAchPoints), scene.FontKey, 28);
                        headerText.X = 124;
                        headerText.Y = 40;
                        headerText.HAlign = SPTextField.SPHAlign.Left;
                        headerText.VAlign = SPTextField.SPVAlign.Top;
                        headerText.Color = Color.Black;
                        sprite.AddChild(headerText);
                    }
                    break;
                case ACHIEVEMENT_INDEX_88_MPH:
                    {
                        bool achEarned = AchievementEarned(GameStats.AchievementBitForIndex(achRow));

                        if (!UnlockedForIndex(achRow))
                        {
                            bgImage = new SPImage(scene.TextureByName(AchievementCellBgTextureNameForIndex(achRow)));
                            bgImage.ScaleX = 560f / bgImage.Width;
                            sprite.AddChild(bgImage);

                            iconImage = new SPImage(scene.TextureByName("locked"));
                            iconImage.X = 16;
                            iconImage.Y = (bgImage.Height - iconImage.Height) / 2;
                            sprite.AddChild(iconImage);

                            headerText = new SPTextField(350, 64, AchievementCellTitleTextForIndex(achRow), scene.FontKey, 24);
                            headerText.X = 120;
                            headerText.Y = iconImage.Y + 10;
                            headerText.HAlign = SPTextField.SPHAlign.Center;
                            headerText.VAlign = SPTextField.SPVAlign.Center;
                            headerText.Color = Color.Black;
                            sprite.AddChild(headerText);
                        }
                        else
                        {
                            bgImage = new SPImage(scene.TextureByName(AchievementCellBgTextureNameForIndex(achRow)));
                            bgImage.ScaleX = 560f / bgImage.Width;
                            sprite.AddChild(bgImage);

                            iconImage = new SPImage(scene.TextureByName(AchievementCellIconTextureNameForIndex(achRow)));
                            iconImage.X = 16;
                            iconImage.Y = (bgImage.Height - iconImage.Height) / 2;
                            sprite.AddChild(iconImage);

                            prizeImage = new SPImage(scene.TextureByName(AchievementCellPrizeTextureNameForIndex(achRow)));
                            prizeImage.X = bgImage.Width - (prizeImage.Width + 16);
                            prizeImage.Y = 0;
                            sprite.AddChild(prizeImage);

                            headerText = new SPTextField(350, 40, AchievementCellTitleTextForIndex(achRow), scene.FontKey, 24);
                            headerText.X = (bgImage.Width - headerText.Width) / 2;
                            headerText.Y = 0;
                            headerText.HAlign = SPTextField.SPHAlign.Center;
                            headerText.VAlign = SPTextField.SPVAlign.Top;
                            headerText.Color = (achEarned) ? SPUtils.ColorFromColor(0x147fe3) : Color.Black;
                            sprite.AddChild(headerText);

                            bodyText = new SPTextField(350, 64, AchievementCellBodyTextForIndex(achRow), scene.FontKey, 20);
                            bodyText.X = 94;
                            bodyText.Y = headerText.Y + 0.8f * headerText.Height;
                            bodyText.HAlign = SPTextField.SPHAlign.Left;
                            bodyText.VAlign = SPTextField.SPVAlign.Center;
                            bodyText.Color = Color.Black;
                            sprite.AddChild(bodyText);

                            pointsText = new SPTextField(prizeImage.Width, 32, AchievementCellPointsTextForIndex(achRow), scene.FontKey, 24);
                            pointsText.X = prizeImage.X;
                            pointsText.Y = prizeImage.Y + 0.85f * prizeImage.Height;
                            pointsText.HAlign = SPTextField.SPHAlign.Center;
                            pointsText.VAlign = SPTextField.SPVAlign.Top;
                            pointsText.Color = (achEarned) ? Color.Green : Color.Black;
                            sprite.AddChild(pointsText);

                            bgImage.ScaleY = 2f;

                            miscImage = new SPImage(scene.TextureByName("speedboat"));
                            miscImage.X = 8;
                            miscImage.Y = 0.6f * bgImage.Height;
                            sprite.AddChild(miscImage);

                            button = new SPButton(scene.TextureByName("speedboat-helm-icon"));
                            button.X = bgImage.Width - (button.Width + 16);
                            button.Y = 0.55f * bgImage.Height;
                            sprite.AddChild(button);

                            miscText = new SPTextField(160, 64, "to launch.", scene.FontKey, 26);
                            miscText.X = miscImage.X + miscImage.Width + 96;
                            miscText.Y = 0.6f * bgImage.Height;
                            miscText.HAlign = SPTextField.SPHAlign.Left;
                            miscText.VAlign = SPTextField.SPVAlign.Center;
                            miscText.Color = Color.Black;
                            sprite.AddChild(miscText);

                            menuButton = new MenuButton(null, scene.TextureByName("large_face_a"));
                            menuButton.Scale = new Vector2(0.8f, 0.8f);
                            menuButton.X = miscText.X - 1.15f * menuButton.Width;
                            menuButton.Y = miscText.Y + 4;
                            menuButton.SfxKey = "Button";
                            menuButton.Selected = true;
                            menuButton.Tag = k88_MPH_BUTTON_TAG;
                            sprite.AddChild(menuButton);
                        }
                    }
                    break;
                default:
                    {
                        bool achEarned = AchievementEarned(GameStats.AchievementBitForIndex(achRow));
                        bgImage = new SPImage(scene.TextureByName(AchievementCellBgTextureNameForIndex(achRow)));
                        bgImage.ScaleX = 560f / bgImage.Width;
                        sprite.AddChild(bgImage);

                        iconImage = new SPImage(scene.TextureByName(AchievementCellIconTextureNameForIndex(achRow)));
                        iconImage.X = 16;
                        iconImage.Y = (bgImage.Height - iconImage.Height) / 2;
                        sprite.AddChild(iconImage);

                        prizeImage = new SPImage(scene.TextureByName(AchievementCellPrizeTextureNameForIndex(achRow)));
                        prizeImage.X = bgImage.Width - (prizeImage.Width + 16);
                        prizeImage.Y = 0;
                        sprite.AddChild(prizeImage);

                        headerText = new SPTextField(350, 40, AchievementCellTitleTextForIndex(achRow), scene.FontKey, 24);
                        headerText.X = (bgImage.Width - headerText.Width) / 2;
                        headerText.Y = 0;
                        headerText.HAlign = SPTextField.SPHAlign.Center;
                        headerText.VAlign = SPTextField.SPVAlign.Top;
                        headerText.Color = (achEarned) ? SPUtils.ColorFromColor(0x147fe3) : Color.Black;
                        sprite.AddChild(headerText);

                        bodyText = new SPTextField(350, 64, AchievementCellBodyTextForIndex(achRow), scene.FontKey, 20);
                        bodyText.X = 94;
                        bodyText.Y = headerText.Y + 0.8f * headerText.Height;
                        bodyText.HAlign = SPTextField.SPHAlign.Left;
                        bodyText.VAlign = SPTextField.SPVAlign.Center;
                        bodyText.Color = Color.Black;
                        sprite.AddChild(bodyText);

                        pointsText = new SPTextField(prizeImage.Width, 32, AchievementCellPointsTextForIndex(achRow), scene.FontKey, 24);
                        pointsText.X = prizeImage.X;
                        pointsText.Y = prizeImage.Y + 0.85f * prizeImage.Height;
                        pointsText.HAlign = SPTextField.SPHAlign.Center;
                        pointsText.VAlign = SPTextField.SPVAlign.Top;
                        pointsText.Color = (achEarned) ? Color.Green : Color.Black;
                        sprite.AddChild(pointsText);

                        if (!IsBinaryForIndex(achRow))
                        {
                            double percentComplete = mProfileManager.PlayerStats.PercentComplete(GameStats.AchievementBitForIndex(achRow));

                            percentCompleteText = new SPTextField(iconImage.Width, 28, ((int)percentComplete).ToString("D") + "%", scene.FontKey, 20);
                            percentCompleteText.X = iconImage.X;
                            percentCompleteText.Y = iconImage.Y + iconImage.Height;
                            percentCompleteText.HAlign = SPTextField.SPHAlign.Center;
                            percentCompleteText.VAlign = SPTextField.SPVAlign.Top;
                            percentCompleteText.Color = (percentComplete < 1f) ? SPUtils.ColorFromColor(0xa40e0e) : Color.Green;
                            sprite.AddChild(percentCompleteText);

                            trailImage = new SPImage(scene.TextureByName("treasure-trail-grey"));
                            trailImage.X = 84;
                            trailImage.Y = 94;
                            sprite.AddChild(trailImage);

                            gauge = new SXGauge(scene.TextureByName("treasure-trail"), SXGauge.SXGaugeOrientation.Horizontal);
                            gauge.X = trailImage.X;
                            gauge.Y = trailImage.Y;
                            gauge.Ratio = (float)percentComplete / 100.0f;
                            sprite.AddChild(gauge);

                            xMarksImage = new SPImage(scene.TextureByName((achEarned) ? "x-marks-the-spot" : "x-marks-the-spot-grey"));
                            xMarksImage.X = 412;
                            xMarksImage.Y = 90;
                            sprite.AddChild(xMarksImage);
                        }
                    }
                    break;
            }

            if (sprite != null && bgImage != null && achRow != ACHIEVEMENT_INDEX_MAX)
            {
                separatorImage = new SPImage(scene.TextureByName("tableview-cell-divider"));
                separatorImage.ScaleX = bgImage.ScaleX;
                separatorImage.Y = bgImage.Y + bgImage.Height - separatorImage.Height;
                sprite.AddChild(separatorImage);
            }

            return sprite;
        }

        private string AchievementCellTitleTextForIndex(int achRow)
        {
            string text = null;
            
            if (mAchievementDefs != null && achRow < mAchievementDefs.Count)
            {
                if (achRow == ACHIEVEMENT_INDEX_88_MPH && !UnlockedForIndex(achRow))
                    text = "Unlock with Treason Points of at least 88.";
                else
                {
                    Dictionary<string, object> achDef = mAchievementDefs[achRow] as Dictionary<string, object>;

                    if (achDef != null)
                        text = achDef["name"] as string;
                }
            }

            return text;
        }

        private string AchievementCellBodyTextForIndex(int achRow)
        {
            string text = null;

            if (mAchievementDefs != null && achRow < mAchievementDefs.Count)
            {
                Dictionary<string, object> achDef = mAchievementDefs[achRow] as Dictionary<string, object>;

                if (achDef != null)
                {
                    uint achBit = GameStats.AchievementBitForIndex(achRow);
                    text = (AchievementEarned(achBit)) ? achDef["earnedDesc"] as string : achDef["unearnedDesc"] as string;
                }
            }

            return text;
        }

        private string AchievementCellPointsTextForIndex(int achRow)
        {
            string text = null;

            if (mAchievementDefs != null && achRow < mAchievementDefs.Count)
            {
                Dictionary<string, object> achDef = mAchievementDefs[achRow] as Dictionary<string, object>;

                if (achDef != null)
                {
                    int points = Convert.ToInt32(achDef["points"]);
                    text = points.ToString("D") + " pts";
                }
            }

            return text;
        }

        private string AchievementCellBgTextureNameForIndex(int achRow)
        {
            string texName = null;

            if (achRow >= 0 && mAchievementDefs != null && achRow < mAchievementDefs.Count)
            {
                uint achBit = GameStats.AchievementBitForIndex(achRow);

                if (AchievementEarned(achBit))
                    texName = ((achRow & 1) == 0) ? "tableview-cell-dark" : "tableview-cell-light";
                else
                    texName = "tableview-cell-grey";
            }

            return texName;
        }

        private string AchievementCellIconTextureNameForIndex(int achRow)
        {
            string texName = null;

            if (achRow >= 0 && mAchievementDefs != null && achRow < mAchievementDefs.Count)
            {
                if (achRow == ACHIEVEMENT_INDEX_88_MPH && !UnlockedForIndex(achRow))
                    texName = "locked";
                else
                {
                    uint achBit = GameStats.AchievementBitForIndex(achRow);
                    texName = (AchievementEarned(achBit)) ? "complete" : "incomplete";
                    texName = String.Format("tier{0}-{1}", AchievementTierForIndex(achRow), texName);
                }
            }

            return texName;
        }

        private string AchievementCellPrizeTextureNameForIndex(int achRow)
        {
            string texName = null;

            if (achRow >= 0 && mAchievementDefs != null && achRow < mAchievementDefs.Count)
            {
                uint achBit = GameStats.AchievementBitForIndex(achRow);
                texName = (AchievementEarned(achBit)) ? "ach-prize-" : "ach-prize-grey-";
                texName = String.Format("{0}{1}", texName, AchievementTierForIndex(achRow));
            }

            return texName;
        }

        private uint AchievementTierForIndex(int achRow)
        {
            uint tier = 2;

            if (achRow >= 0 && mAchievementDefs != null && achRow < mAchievementDefs.Count)
            {
                Dictionary<string, object> achDef = mAchievementDefs[achRow] as Dictionary<string, object>;

                if (achDef != null)
                    tier = Convert.ToUInt32(achDef["tier"]);

                tier = Math.Min(2, tier);
            }

            return tier;
        }

        private bool IsBinaryForIndex(int achRow)
        {
            bool isBinary = true;

            if (achRow >= 0 && mAchievementDefs != null && achRow < mAchievementDefs.Count)
            {
                Dictionary<string, object> achDef = mAchievementDefs[achRow] as Dictionary<string, object>;

                if (achDef != null)
                    isBinary = Convert.ToBoolean(achDef["binary"]);
            }

            return isBinary;
        }

        private bool UnlockedForIndex(int achRow)
        {
            bool unlocked = true;

            if (achRow >= 0 && mAchievementDefs != null && achRow == ACHIEVEMENT_INDEX_88_MPH)
                unlocked = mProfileManager.PlayerStats.EarnedAchievementPoints(mAchievementDefs) >= 88;

            return unlocked;
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
                    if (mProfileManager != null)
                    {
                        mProfileManager.RemoveEventListener(AchievementEarnedEvent.CUST_EVENT_TYPE_ACHIEVEMENT_EARNED, (AchievementEarnedEventHandler)OnAchievementEarned);
                        mProfileManager.Dispose();
                        mProfileManager = null;
                    }

                    if (mView != null)
                    {
                        mView.RemoveEventListener(AchievementPanel.CUST_EVENT_TYPE_ACHIEVEMENT_HIDDEN, (SPEventHandler)OnAchievementHidden);
                        mView = null;
                    }

                    if (mCombatText != null)
                    {
                        mCombatText.Dispose();
                        mCombatText = null;
                    }

                    mAchievementDefs = null;
                    mDisplayQueue = null;
                    mCombatTextOwner = null;
                }

                mIsDisposed = true;
            }
        }

        ~AchievementManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
