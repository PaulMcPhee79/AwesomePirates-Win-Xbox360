using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;
//using System.Runtime.CompilerServices;

namespace AwesomePirates
{
    class ObjectivesManager : SPEventDispatcher, IDisposable
    {
        public const string CUST_EVENT_TYPE_OBJECTIVES_RANKUP_COMPLETED = "objectivesRankupCompletedEvent";

        public const uint OBJ_TYPE_REQUIREMENTS = 1;
        public const uint OBJ_TYPE_PLANKING = 2;
        public const uint OBJ_TYPE_SINKING = 3;
        public const uint OBJ_TYPE_TIME_OF_DAY = 4;
        public const uint OBJ_TYPE_SCORE = 5;
        public const uint OBJ_TYPE_RICOCHET = 6;
        public const uint OBJ_TYPE_LOOT = 7;
        public const uint OBJ_TYPE_RED_CROSS = 8;
        public const uint OBJ_TYPE_BLUE_CROSS = 9;
        public const uint OBJ_TYPE_SHOT_MISSED = 10;
        public const uint OBJ_TYPE_TRAWLING_NET = 11;
        public const uint OBJ_TYPE_ASH_PICKED_UP = 12;
        public const uint OBJ_TYPE_SPELL_USED = 13;
        public const uint OBJ_TYPE_MUNITION_USED = 14;
        public const uint OBJ_TYPE_SHOT_FIRED = 15;
        public const uint OBJ_TYPE_PLAYER_HIT = 16;
        public const uint OBJ_TYPE_VOODOO_GADGET_EXPIRED = 17;
        public const uint OBJ_TYPE_BLAST_VICTIMS = 18;

        public ObjectivesManager(List<ObjectivesRank> ranks, SceneController scene)
        {
            mRanks = ranks;
            mIsGameOver = false;
            mShadowRank = null;
            mProgressMarkerRank = null;
            CurrentRank = ObjectivesRank.GetCurrentRankFromRanks(ranks);
            mScene = scene;
            mView = null;
            CreateView();
        }

        #region Fields
        private bool mIsDisposed = false;
        private bool mIsGameOver;
        private ObjectivesRank mCurrentRank;
        private ObjectivesRank mShadowRank;
        private ObjectivesRank mProgressMarkerRank;
        private List<ObjectivesRank> mRanks;
        private ObjectivesView mView;
        private SceneController mScene;

        // Cached state details
        private uint mRedCrossCount;
        private uint mShotCount;
        private uint mRicochetCount;
        private uint mPlayerHitCount;
        private uint mSpellUseCount;
        private uint mMunitionUseCount;
        private uint mFleetID;
        private uint mFleetIDCount;
        private uint mNavyShipsSunkCount;
        private uint mPirateShipsSunkCount;
        private uint mExpiredTempestCount;
        private uint mActiveSpellsMunitionsBitmap;
        private int mLivePowderKegs;
        #endregion

        #region Properties
        public bool IsMaxRank { get { return (Rank == ObjectivesRank.MaxRank); } }
        public bool IsCurrentRankCompleted { get { return (mCurrentRank != null && mCurrentRank.IsCompleted && !mCurrentRank.IsMaxRank); } }
        public uint Rank { get { return (mCurrentRank != null) ? mCurrentRank.Rank : 0; } }
        public string RankLabel { get { return RankLabelForRank(Rank); } }
        public string RankTitle { get { return ObjectivesRank.TitleForRank(Rank); } }
        public ObjectivesRank SyncedObjectivesRank
        {
            get
            {
                ObjectivesRank objRank = new ObjectivesRank(Rank);
                objRank.SyncWithObjectivesRank(CurrentRank);
                return objRank;
            }
        }
        public int ScoreMultiplier { get { return ObjectivesRank.MultiplierForRank(Rank); } }
        public uint RequiredNpcShipType { get { return ((IsMaxRank || mCurrentRank == null) ? 0 : mCurrentRank.RequiredNpcShipType); } }
        public uint RequiredAshType { get { return ((IsMaxRank || mCurrentRank == null) ? 0 : mCurrentRank.RequiredAshType); } }
        private List<ObjectivesRank> Ranks { get { return mRanks; } set { mRanks = value; } }
        private ObjectivesRank CurrentRank
        {
            get { return mCurrentRank; }
            set
            {
                if (mCurrentRank != value)
                {
                    mCurrentRank = value;
                    mShadowRank = null;
                    mProgressMarkerRank = null;
        
                    if (mCurrentRank != null) 
                    {
                        mShadowRank = new ObjectivesRank(mCurrentRank.Rank);
                        mShadowRank.SyncWithObjectivesRank(mCurrentRank);

                        mProgressMarkerRank = new ObjectivesRank(mCurrentRank.Rank);
                        mProgressMarkerRank.SyncWithObjectivesRank(mCurrentRank);
                    }
                }
            }
        }
        #endregion

        #region Methods
        public string RankLabelForRank(uint rank)
        {
            string label = null;

            if (rank == 0)
                label = "Unranked";
            else
                label = "Rank " + rank;
            return label;
        }

        public ObjectivesRank SyncedObjectivesForRank(uint rank)
        {
            ObjectivesRank objRank = new ObjectivesRank(rank);
            objRank.SyncWithObjectivesRank(ObjectivesRank.GetRankFromRanks(rank, Ranks));
            return objRank;
        }

        public void SetScene(SceneController scene)
        {
            // Remove from old scene
            DestroyView();

            // Add to new scene
            mScene = scene;

            if (mScene != null)
            {
                CreateView();
                mView.PopulateWithObjectivesRank(CurrentRank);
            }
        }

        public void SetupWithRanks(List<ObjectivesRank> ranks)
        {
            Ranks = ranks;
            CurrentRank = ObjectivesRank.GetCurrentRankFromRanks(ranks);

            if (mView != null)
                mView.PopulateWithObjectivesRank(CurrentRank);
        }

        public void EnableTouchBarrier(bool enable)
        {
            if (mView != null)
                mView.EnableTouchBarrier(enable);
        }

        public void Flip(bool enable)
        {
            if (mView != null)
                mView.Flip(enable);
        }

        public void PrepareForNewGame()
        {
            RefreshCachedStateDetails();
    
            if (mCurrentRank.IsCompleted)
                CurrentRank = ObjectivesRank.GetCurrentRankFromRanks(Ranks);
            mCurrentRank.PrepareForNewGame();
            mShadowRank.PrepareForNewGame();
            mProgressMarkerRank.PrepareForNewGame();
            ProgressObjectiveWithEventType(OBJ_TYPE_REQUIREMENTS);
            mView.PopulateWithObjectivesRank(CurrentRank);
            mView.FillCompletedCacheWithRank(CurrentRank);
            mIsGameOver = false;
        }

        public void PrepareForGameOver()
        {
            mIsGameOver = true;
        }

        public void TestRankup()
        {
            CurrentRank.ForceCompletion();
            CurrentRank = ObjectivesRank.GetCurrentRankFromRanks(Ranks);
        }

        private void RefreshCachedStateDetails()
        {
            mRedCrossCount = 0;
            mShotCount = 0;
            mRicochetCount = 0;
            mPlayerHitCount = 0;
            mSpellUseCount = 0;
            mMunitionUseCount = 0;
            mFleetID = 0;
            mFleetIDCount = 0;
            mNavyShipsSunkCount = 0;
            mPirateShipsSunkCount = 0;
            mExpiredTempestCount = 0;
            mActiveSpellsMunitionsBitmap = 0;
            mLivePowderKegs = 0;
        }

        private void CreateView()
        {
            if (mView != null || mScene == null)
                return;
    
            mView = new ObjectivesView(mScene.ObjectivesCategoryForViewType(ObjectivesView.ViewType.View));
            mView.AddEventListener(ObjectivesView.CUST_EVENT_TYPE_OBJECTIVES_CURRENT_PANEL_DISMISSED, (SPEventHandler)OnCurrentPanelDismissed);
            mView.AddEventListener(ObjectivesView.CUST_EVENT_TYPE_OBJECTIVES_RANKUP_PANEL_DISMISSED, (SPEventHandler)OnRankupPanelDismissed);
            mScene.AddProp(mView);
        }

        public void DestroyView()
        {
            if (mView == null)
                return;
    
            mView.RemoveEventListener(ObjectivesView.CUST_EVENT_TYPE_OBJECTIVES_CURRENT_PANEL_DISMISSED, (SPEventHandler)OnCurrentPanelDismissed);
            mView.RemoveEventListener(ObjectivesView.CUST_EVENT_TYPE_OBJECTIVES_RANKUP_PANEL_DISMISSED, (SPEventHandler)OnRankupPanelDismissed);
            mScene.Juggler.RemoveTweensWithTarget(mView);
            mScene.RemoveProp(mView);
            mView.Dispose();
            mView = null;
        }

        private ObjectivesRank GetNextRankFromRanks(List<ObjectivesRank> ranks)
        {
            ObjectivesRank nextRank = null;
    
            if (ranks != null && mCurrentRank != null) {
                int currentIndex = ranks.IndexOf(mCurrentRank);
        
                if (currentIndex < ranks.Count-1)
                    nextRank = ranks[currentIndex+1];
            }
    
            if (nextRank == null)
                nextRank = ranks[ranks.Count-1];
    
            return nextRank;
        }

        public void TrialModeDidChange(bool isTrial)
        {
            mView.PopulateWithObjectivesRank(mCurrentRank);
        }

        // Current Panel
        public void ShowCurrentPanel()
        {
            mView.PopulateWithObjectivesRank(mCurrentRank);
            mView.ShowCurrentPanel();
        }

        public void HideCurrentPanel()
        {
            mView.HideCurrentPanel();
        }

        public void EnableCurrentPanelButtons(bool enable)
        {
            mView.EnableCurrentPanelButtons(enable);
        }

        public void AddToCurrentPanel(SPDisplayObject displayObject, float xPercent, float yPercent)
        {
            if (displayObject != null && mView != null)
                mView.AddToCurrentPanel(displayObject, xPercent, yPercent);
        }

        public void RemoveFromCurrentPanel(SPDisplayObject displayObject)
        {
            if (displayObject != null && mView != null)
                mView.RemoveFromCurrentPanel(displayObject);
        }

        public SPSprite MaxRankSprite()
        {
            return mView.MaxRankSprite();
        }

        private void OnCurrentPanelDismissed(SPEvent ev)
        {
            mView.HideCurrentPanel();
        }

        // Completed Panel
        public void TestCompletedObjectivesPanel()
        {
            mView.EnqueueCompletedObjectivesDescription(mCurrentRank.ObjectiveDescAtIndex(0));
        }

        private void ProcessRecentlyCompletedObjectives()
        {
            for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
            {
                if (mCurrentRank.IsObjectiveCompletedAtIndex(i) && !mShadowRank.IsObjectiveCompletedAtIndex(i))
                    mView.EnqueueCompletedObjectivesDescription(mCurrentRank.ObjectiveDescAtIndex(i));
            }
        }

        // Rankup Panel
        private bool WasProgressMade()
        {
            bool progressMade = false;
    
            for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
            {
                if (mCurrentRank.IsObjectiveCompletedAtIndex(i) && !mProgressMarkerRank.IsObjectiveCompletedAtIndex(i))
                {
                    progressMade = true;
                    break;
                }
            }
    
            return progressMade;
        }

        public void ProcessEndOfTurn()
        {
            mIsGameOver = true;
    
            if (WasProgressMade())
                GameController.GC.ThisTurn.WasGameProgressMade = true;
    
            if (IsCurrentRankCompleted)
            {
                CurrentRank = ObjectivesRank.GetCurrentRankFromRanks(mRanks);
                mView.ShowRankupPanelWithRank(CurrentRank.Rank);
                mScene.AchievementManager.ResetCombatTextCache();
            }
            else
            {
                DispatchEvent(new BinaryEvent(CUST_EVENT_TYPE_OBJECTIVES_RANKUP_COMPLETED, false));
            }
        }

        public void TestRankupPanel()
        {
            mView.ShowRankupPanelWithRank(CurrentRank.Rank);
        }

        private void OnRankupPanelDismissed(SPEvent ev)
        {
            mView.HideRankupPanel();
            DispatchEvent(new BinaryEvent(CUST_EVENT_TYPE_OBJECTIVES_RANKUP_COMPLETED, true));
        }

        // Misc Helpers
        private bool WasFleetDestroyed(ShipActor ship)
        {
            bool fleetDestroyed = false;
            uint fleetID = 0;
    
            if (ship is PrimeShip)
            {
                PrimeShip primeShip = ship as PrimeShip;
                fleetID = primeShip.FleetID;
            }
            else if (ship is EscortShip)
            {
                EscortShip escortShip = ship as EscortShip;
                fleetID = escortShip.FleetID;
            }
    
            if (mFleetID == fleetID)
            {
                ++mFleetIDCount;
        
                if (mFleetIDCount >= 3)
                {
                    fleetDestroyed = true;
                    mFleetID = 0;
                    mFleetIDCount = 0;
                }
            }
            else
            {
                mFleetID = fleetID;
                mFleetIDCount = 1;
            }
    
            return fleetDestroyed;
        }

        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private bool IsBitSet(uint value, uint bit)
        {
            return ((value & bit) == bit);
        }

        // Objectives Events
        public void ProgressObjectiveWithRicochetVictims(SPHashSet<ShipActor> victims)
        {
            if (victims != null)
                ProgressObjectiveWithEventType(OBJ_TYPE_RICOCHET, victims.Count, null, victims);
        }

        public void ProgressObjectiveWithEventType(uint eventType)
        {
            ProgressObjectiveWithEventType(eventType, 0, 0, null, null);
        }

        public void ProgressObjectiveWithEventType(uint eventType, int count)
        {
            ProgressObjectiveWithEventType(eventType, 0, count, null, null);
        }

        public void ProgressObjectiveWithEventType(uint eventType, ShipActor ship)
        {
            ProgressObjectiveWithEventType(eventType, 0, 0, ship, null);
        }

        public void ProgressObjectiveWithEventType(uint eventType, uint tag)
        {
            ProgressObjectiveWithEventType(eventType, tag, 0, null, null);
        }

        public void ProgressObjectiveWithEventType(uint eventType, int count, ShipActor ship, SPHashSet<ShipActor> victims)
        {
            ProgressObjectiveWithEventType(eventType, 0, count, ship, victims);
        }

        public void ProgressObjectiveWithEventType(uint eventType, uint tag, int count, ShipActor ship, SPHashSet<ShipActor> victims)
        {
            if (mScene.GameMode != GameMode.Career || CurrentRank == null || CurrentRank.IsMaxRank || mIsGameOver || (ship != null && ship.IsPreparingForNewGame))
                return;

            GameController gc = GameController.GC;
            TimeOfDay timeOfDay = gc.TimeOfDay;
            uint day = gc.TimeKeeper.Day, key = 0;

            mShadowRank.SyncWithObjectivesRank(mCurrentRank);

            switch (mCurrentRank.Rank)
            {
                case ObjectivesRank.RANK_UNRANKED:
                    {
                        key = 1;
            
                        if (eventType == OBJ_TYPE_PLANKING)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_SINKING && IsBitSet(ship.DeathBitmap, DeathBitmaps.POWDER_KEG))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_MUNITION_USED && tag == Idol.GADGET_SPELL_TNT_BARRELS)
                            mLivePowderKegs = (int)Idol.CountForIdol(new Idol(tag));
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.GADGET_SPELL_TNT_BARRELS && mLivePowderKegs > 0)
                        {
                            int localCount = mCurrentRank.ObjectiveCountAtIndex(1), localQuota = mCurrentRank.ObjectiveQuotaAtIndex(1);
                            int countRemaining = localQuota - localCount;
                
                            --mLivePowderKegs;
                
                            if (countRemaining > mLivePowderKegs)
                                mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        }
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+2) && timeOfDay == TimeOfDay.Sunset)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_SWABBY:
                    {
                        key = 4;

                        if (eventType == OBJ_TYPE_SINKING && (ship is TreasureFleet || ship is EscortShip))
                        {
                            if (WasFleetDestroyed(ship))
                                 mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        }
                        else if (eventType == OBJ_TYPE_RICOCHET && count >= ObjectivesDescription.ValueForKey(key+1))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_BLUE_CROSS)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_DECKHAND:
                    {
                        key = 7;
            
                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (ship is NavyShip)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            if (IsBitSet(ship.DeathBitmap, DeathBitmaps.WHIRLPOOL))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        }
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.VOODOO_SPELL_WHIRLPOOL)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        else if (eventType == OBJ_TYPE_RICOCHET && count >= 2)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_JACK_TAR:
                    {
                        key = 10;
            
                        if (eventType == OBJ_TYPE_SINKING && IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON))
                        {
                            if (ship.AshBitmap == Ash.ASH_MOLTEN)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        }
                        else if (eventType == OBJ_TYPE_ASH_PICKED_UP && tag == Ash.ASH_MOLTEN && !mCurrentRank.IsObjectiveCompletedAtIndex(0))
                            mCurrentRank.SetObjectiveCountAtIndex(0, 0);
                        else if (eventType == OBJ_TYPE_SHOT_MISSED && !mCurrentRank.IsObjectiveCompletedAtIndex(1))
                            mCurrentRank.SetObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+2) && timeOfDay == TimeOfDay.Sunrise)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_OLD_SALT:
                    {
                        key = 13;
            
                        if (eventType == OBJ_TYPE_TRAWLING_NET && count >= ObjectivesDescription.ValueForKey(key))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.GADGET_SPELL_NET)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 0);
                        else if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (ship is PirateShip)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        }
                        else if (eventType == OBJ_TYPE_SCORE && gc.ThisTurn.Infamy >= ObjectivesDescription.ValueForKey(key+2) && mRicochetCount == 0)
                           mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);
                        else if (eventType == OBJ_TYPE_RICOCHET && count >= 2)
                        {
                            ++mRicochetCount;
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 2);
 
                        }

                        break;
                    }
                case ObjectivesRank.RANK_HELMSMAN:
                    {
                        key = 16;
            
                        if (eventType == OBJ_TYPE_SINKING && (ship is SilverTrain || ship is EscortShip))
                        {
                            if (WasFleetDestroyed(ship))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        }
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+1) && timeOfDay == TimeOfDay.Sunrise && mRedCrossCount == 0)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_RED_CROSS)
                        {
                            ++mRedCrossCount;
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        }
                        else if (eventType == OBJ_TYPE_RICOCHET && count >= ObjectivesDescription.ValueForKey(key+2))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_SEA_DOG:
                    {
                        key = 19;
            
                        if (eventType == OBJ_TYPE_PLANKING)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_SINKING && IsBitSet(ship.DeathBitmap, DeathBitmaps.HAND_OF_DAVY))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.VOODOO_SPELL_HAND_OF_DAVY)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+2) && timeOfDay == TimeOfDay.Midnight)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_VILLAIN:
                    {
                        key = 22;
            
                        if (eventType == OBJ_TYPE_RICOCHET && count >= 2)
                        {
                            uint navyShipCount = 0;
                
                            foreach (ShipActor victim in victims.EnumerableSet)
                            {
                                if (victim is NavyShip)
                                    ++navyShipCount;
                            }
                
                            if (navyShipCount >= ObjectivesDescription.ValueForKey(key))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        }
                        else if (eventType == OBJ_TYPE_SINKING && IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON) && ship.AshBitmap == Ash.ASH_SAVAGE)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);
                        else if (eventType == OBJ_TYPE_ASH_PICKED_UP && tag == Ash.ASH_SAVAGE && !mCurrentRank.IsObjectiveCompletedAtIndex(2))
                            mCurrentRank.SetObjectiveCountAtIndex(0, 2);

                        break;
                    }
                case ObjectivesRank.RANK_BRIGAND:
                    {
                        key = 25;
            
                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (ship is NavyShip)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            if (IsBitSet(ship.DeathBitmap, DeathBitmaps.BRANDY_SLICK))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        }
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.GADGET_SPELL_BRANDY_SLICK)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        else if (eventType == OBJ_TYPE_SCORE && gc.ThisTurn.Infamy >= ObjectivesDescription.ValueForKey(key+2))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_LOOTER:
                    {
                        key = 28;
            
                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            if (ship is NavyShip)
                            {
                                ++mNavyShipsSunkCount;
                                mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                            }
                        }
                        else if (eventType == OBJ_TYPE_SHOT_MISSED && !mCurrentRank.IsObjectiveCompletedAtIndex(0))
                            mCurrentRank.SetObjectiveCountAtIndex(0, 0);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+1) && timeOfDay == TimeOfDay.Noon && mNavyShipsSunkCount == 0)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_RICOCHET && count >= 2)
                        {
                            uint pirateShipCount = 0;
                
                            foreach (ShipActor victim in victims.EnumerableSet)
                            {
                                if (victim is PirateShip)
                                    ++pirateShipCount;
                            }
                
                            if (pirateShipCount >= ObjectivesDescription.ValueForKey(key+2))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);
                        }

                        break;
                    }
                case ObjectivesRank.RANK_GALLOWS_BIRD:
                    {
                        key = 31;
            
                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (ship is PirateShip)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            if (IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON) && gc.PlayerShip.IsFlyingDutchman)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        }
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.VOODOO_SPELL_FLYING_DUTCHMAN)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+2) && timeOfDay == TimeOfDay.Sunrise)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_SCOUNDREL:
                    {
                        key = 34;
            
                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (ship is TreasureFleet || ship is EscortShip)
                            {
                                if (WasFleetDestroyed(ship))
                                    mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            }
                
                            if (IsBitSet(ship.DeathBitmap, DeathBitmaps.ACID_POOL))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        }
                        else if (eventType == OBJ_TYPE_ASH_PICKED_UP && tag == Ash.ASH_NOXIOUS && !mCurrentRank.IsObjectiveCompletedAtIndex(1))
                            mCurrentRank.SetObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_RICOCHET && count >= ObjectivesDescription.ValueForKey(key+2))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_ROGUE:
                    {
                        key = 37;
            
                        if (eventType == OBJ_TYPE_SINKING && IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON) && gc.PlayerShip.IsCamouflaged)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.GADGET_SPELL_CAMOUFLAGE)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 0);
                        else if (eventType == OBJ_TYPE_PLANKING)
                           mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_SCORE && gc.ThisTurn.Infamy >= ObjectivesDescription.ValueForKey(key+2))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_PILLAGER:
                    {
                        key = 40;
            
                        if (eventType == OBJ_TYPE_SINKING && IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_SHOT_MISSED && !mCurrentRank.IsObjectiveCompletedAtIndex(0))
                           mCurrentRank.SetObjectiveCountAtIndex(0, 0);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+1) && timeOfDay == TimeOfDay.Sunrise && mPlayerHitCount == 0)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_PLAYER_HIT)
                        {
                            ++mPlayerHitCount;
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        }
                        else if (eventType == OBJ_TYPE_RICOCHET && count >= 2)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_PLUNDERER:
                    {
                        key = 43;
            
                        if (eventType == OBJ_TYPE_RICOCHET && count >= 2)
                        {
                            uint navyShipCount = 0, pirateShipCount = 0;
                
                            foreach (ShipActor victim in victims.EnumerableSet)
                            {
                                if (victim is NavyShip)
                                    ++navyShipCount;
                                else if (victim is PirateShip)
                                    ++pirateShipCount;
                            }
                
                            if (navyShipCount > 0 && pirateShipCount > 0)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        }
                        else if (eventType == OBJ_TYPE_SINKING && IsBitSet(ship.DeathBitmap, DeathBitmaps.GHOSTLY_TEMPEST))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.VOODOO_SPELL_TEMPEST)
                        {
                            ++mExpiredTempestCount;
                
                            if (mExpiredTempestCount >= 2)
                                mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        }
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+2) && timeOfDay == TimeOfDay.Midnight)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_FREEBOOTER:
                    {
                        key = 46;
            
                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (ship is NavyShip)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            if (IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON) && ship.AshBitmap == Ash.ASH_SAVAGE)
                                 mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        }
                        else if (eventType == OBJ_TYPE_ASH_PICKED_UP && tag == Ash.ASH_MOLTEN && !mCurrentRank.IsObjectiveCompletedAtIndex(1))
                            mCurrentRank.SetObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_SCORE && gc.ThisTurn.Infamy >= ObjectivesDescription.ValueForKey(key+2))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_PRIVATEER:
                    {
                        key = 49;
            
                        if (eventType == OBJ_TYPE_SINKING && ship is PirateShip)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+1) && timeOfDay == TimeOfDay.Midnight && mRedCrossCount == 0)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_RED_CROSS)
                        {
                            ++mRedCrossCount;
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        }
                        else if (eventType == OBJ_TYPE_RICOCHET)
                        {
                            if (count >= 2)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);
                            else if (!mCurrentRank.IsObjectiveCompletedAtIndex(2))
                                mCurrentRank.SetObjectiveCountAtIndex(0, 2);
                        }

                        break;
                    }
                case ObjectivesRank.RANK_CORSAIR:
                    {
                        key = 52;
            
                        if (eventType == OBJ_TYPE_BLUE_CROSS)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+1) && timeOfDay == TimeOfDay.Sunrise && mNavyShipsSunkCount == 0)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_SINKING && ship is NavyShip)
                        {
                            ++mNavyShipsSunkCount;
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        }
                        else if (eventType == OBJ_TYPE_SCORE && gc.ThisTurn.Infamy >= ObjectivesDescription.ValueForKey(key+2) && mRicochetCount == 0)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);
                        else if (eventType == OBJ_TYPE_RICOCHET && count >= 2)
                        {
                            ++mRicochetCount;
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 2);
                        }

                        break;
                    }
                case ObjectivesRank.RANK_BUCCANEER:
                    {
                        key = 55;
            
                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON) && ship.AshBitmap == Ash.ASH_MOLTEN)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            if (ship is TreasureFleet || ship is EscortShip)
                            {
                                if (IsBitSet(ship.DeathBitmap, DeathBitmaps.POWDER_KEG))
                                {
                                    if (WasFleetDestroyed(ship))
                                        mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                                }
                                else
                                    mFleetIDCount = 0;
                            }
                        }
                        else if (eventType == OBJ_TYPE_ASH_PICKED_UP && tag == Ash.ASH_SAVAGE && !mCurrentRank.IsObjectiveCompletedAtIndex(0))
                            mCurrentRank.SetObjectiveCountAtIndex(0, 0);
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.GADGET_SPELL_TNT_BARRELS && mLivePowderKegs > 0 &&
                            !mCurrentRank.IsObjectiveCompletedAtIndex(1))
                        {
                            int localCount = mCurrentRank.ObjectiveCountAtIndex(1), localQuota = mCurrentRank.ObjectiveQuotaAtIndex(1);
                            int countRemaining = 3 * (localQuota - localCount) - (int)mFleetIDCount;
                
                            --mLivePowderKegs;
                
                            if (countRemaining > mLivePowderKegs)
                                mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        }
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+2) && timeOfDay == TimeOfDay.Sunrise
                                   && mSpellUseCount == 0 && mMunitionUseCount == 0)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);
                        else if (eventType == OBJ_TYPE_SPELL_USED)
                        {
                            ++mSpellUseCount;
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 2);
                        }
                        else if (eventType == OBJ_TYPE_MUNITION_USED)
                        {
                            ++mMunitionUseCount;
                          mCurrentRank.SetObjectiveFailedAtIndex(true, 2);
                        }

                        break;
                    }
                case ObjectivesRank.RANK_SEA_WOLF:
                    {
                        key = 58;

                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (!IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                
                            if (ship is PirateShip)
                            {
                                ++mPirateShipsSunkCount;
                                mCurrentRank.SetObjectiveFailedAtIndex(true, 2);
                            }
                        }
                        else if (eventType == OBJ_TYPE_SHOT_FIRED)
                        {
                            if (!mCurrentRank.IsObjectiveCompletedAtIndex(0))
                                mCurrentRank.SetObjectiveCountAtIndex(0, 0);
                        }
                        else if (eventType == OBJ_TYPE_SCORE && gc.ThisTurn.Infamy >= ObjectivesDescription.ValueForKey(key+1))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+2)
                                 && timeOfDay == TimeOfDay.Dusk && mPirateShipsSunkCount == 0)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_SWASHBUCKLER:
                    {
                        key = 61;

                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (IsBitSet(ship.DeathBitmap, DeathBitmaps.SEA_OF_LAVA))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            else if (IsBitSet(ship.DeathBitmap, DeathBitmaps.ABYSSAL_SURGE))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        }
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.VOODOO_SPELL_SEA_OF_LAVA)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 0);
                        else if (eventType == OBJ_TYPE_ASH_PICKED_UP && tag == Ash.ASH_ABYSSAL && !mCurrentRank.IsObjectiveCompletedAtIndex(1))
                            mCurrentRank.SetObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key+2) && timeOfDay == TimeOfDay.Sunrise)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);

                        break;
                    }
                case ObjectivesRank.RANK_CALICO_JACK:
                    {
                        key = 64;
            
                        if (eventType == OBJ_TYPE_SCORE && gc.ThisTurn.Infamy >= 5000000)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key) && timeOfDay == TimeOfDay.Sunrise)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 0);
                        else if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (ship is SilverTrain || ship is EscortShip)
                            {
                                if (IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON) && ship.AshBitmap == Ash.ASH_MOLTEN)
                                {
                                    if (WasFleetDestroyed(ship))
                                        mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                                }
                                else
                                    mFleetIDCount = 0;
                            }
                            else if (ship is PirateShip && IsBitSet(ship.DeathBitmap, DeathBitmaps.POWDER_KEG))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);
                        }
                        else if (eventType == OBJ_TYPE_MUNITION_USED && tag == Idol.GADGET_SPELL_TNT_BARRELS)
                            mLivePowderKegs = (int)Idol.CountForIdol(new Idol(tag));
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.GADGET_SPELL_TNT_BARRELS && mLivePowderKegs > 0)
                        {
                            int localCount = mCurrentRank.ObjectiveCountAtIndex(2), localQuota = mCurrentRank.ObjectiveQuotaAtIndex(2);
                            int countRemaining = localQuota - localCount;
                
                            --mLivePowderKegs;
                
                            if (countRemaining > mLivePowderKegs)
                                mCurrentRank.SetObjectiveFailedAtIndex(true, 2);
                        }

                        break;
                    }
                case ObjectivesRank.RANK_BLACK_BART:
                    {
                        key = 67;
            
                        if (eventType == OBJ_TYPE_SINKING)
                        {
                            if (IsBitSet(ship.DeathBitmap, DeathBitmaps.PLAYER_CANNON))
                            {
                                mCurrentRank.SetObjectiveFailedAtIndex(true, 0);
                    
                                if (ship.AshBitmap == Ash.ASH_SAVAGE && ship is NavyShip)
                                    mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);
                            }
                        }
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key) && timeOfDay == TimeOfDay.Midnight &&
                            !mCurrentRank.IsObjectiveFailedAtIndex(0))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                        else if (eventType == OBJ_TYPE_BLUE_CROSS && gc.PlayerShip.IsCamouflaged)
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);
                        else if (eventType == OBJ_TYPE_VOODOO_GADGET_EXPIRED && tag == Idol.GADGET_SPELL_CAMOUFLAGE)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 1);
                        else if (eventType == OBJ_TYPE_ASH_PICKED_UP && tag == Ash.ASH_SAVAGE && !mCurrentRank.IsObjectiveCompletedAtIndex(2))
                            mCurrentRank.SetObjectiveCountAtIndex(0, 2);

                        break;
                    }
                case ObjectivesRank.RANK_BARBAROSSA:
                    {
                        key = 70;
            
                        if (eventType == OBJ_TYPE_RICOCHET)
                        {
                            if (count >= 2)
                                mCurrentRank.IncreaseObjectiveCountAtIndex(0, 1);
                            if (count >= ObjectivesDescription.ValueForKey(key+2))
                                mCurrentRank.IncreaseObjectiveCountAtIndex(2, 1);
                        }
                        else if (eventType == OBJ_TYPE_TIME_OF_DAY && day == ObjectivesDescription.ValueForKey(key) && timeOfDay == TimeOfDay.Sunset)
                            mCurrentRank.SetObjectiveFailedAtIndex(true, 0);
                        else if (eventType == OBJ_TYPE_SCORE && gc.ThisTurn.Infamy >= ObjectivesDescription.ValueForKey(key+1))
                            mCurrentRank.IncreaseObjectiveCountAtIndex(1, 1);

                        break;
                    }
                case ObjectivesRank.RANK_CAPTAIN_KIDD:
                    break;
                default:
                    break;
            }

            ProcessRecentlyCompletedObjectives();
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
                    DestroyView();
                    mCurrentRank = null;
                    mShadowRank = null;
                    mProgressMarkerRank = null;
                    mRanks = null;
                }

                mIsDisposed = true;
            }
        }

        ~ObjectivesManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
