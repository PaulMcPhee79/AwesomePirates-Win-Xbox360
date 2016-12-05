using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class ActorAi : SPEventDispatcher, IDisposable
    {
        public const string CUST_EVENT_TYPE_CLOSE_BUT_NO_CIGAR_STATE_REACHED = "closeButNoCigarStateReached";
        public const string CUST_EVENT_TYPE_TREASURE_FLEET_SPAWNED = "treasureFleetSpawnedEvent";
        public const string CUST_EVENT_TYPE_TREASURE_FLEET_ATTACKED = "treasureFleetAttackedEvent";
        public const string CUST_EVENT_TYPE_SILVER_TRAIN_SPAWNED = "silverTrainSpawnedEvent";
        public const string CUST_EVENT_TYPE_SILVER_TRAIN_ATTACKED = "silverTrainAttackedEvent";

        public const int kSpawnPlanesCount = 6;
        public const int kPlaneIdNorth = 0;
        public const int kPlaneIdEast = 1;
        public const int kPlaneIdSouth = 2;
        public const int kPlaneIdWest = 3;
        public const int kPlaneIdTown = 4;
        public const int kPlaneIdCove = 5;
        
        private const int THINK_SPECIAL = 0;
        private const int THINK_NAVY = 1;
        private const int THINK_PIRATE = 2;
        private const int THINK_MERCHANT = 3;
        private const int THINK_SHARK = 4;
        private const int THINK_CYCLE = 5;
        private const int THINK_TANK_COUNT = 6;

        // Game AI settings
        private const double kActorAiThinkInterval = 2.0;

        // Game AI sub-state attribute defaults
        private const int kDefaultChanceMax = 1000;

        private const int kDefaultMerchantShipsMin = 3;
        private const int kDefaultMerchantShipsMax = 5;
        private const int kDefaultMerchantShipsChance = (int)(0.6f * kDefaultChanceMax);

        private const int kDefaultPirateShipsMax = 0;
        private const int kDefaultPirateShipsChance = (int)(0.15f * kDefaultChanceMax);

        private const int kDefaultNavyShipsMax = 0;
        private const int kDefaultNavyShipsChance = 0;

        private const int kDefaultSpecialShipsChance = 0;

        // Misc
        private const int kMaxSharks = 2;
        private const float kFleetTimeoutDuration = TimeKeeper.DAY_CYCLE_IN_SEC / 2.0f;

        // Playfield data
        private static float[] kSpawnAngles = new float[6] { SPMacros.PI / 2, 0, -SPMacros.PI / 2, SPMacros.PI, -0.75f * SPMacros.PI, SPMacros.PI / 4 };

        private static int[] kSeaLaneNorth = new int[3] { 1, 2, 4 };	// 0
        private static int[] kSeaLaneEast = new int[3] { 0, 3, 4 };	    // 1
        private static int[] kSeaLaneSouth = new int[2] { 0, 3 };	    // 2
        private static int[] kSeaLaneWest = new int[2] { 1, 2 };		// 3
        private static int[] kSeaLaneTown = new int[2] { 0, 1 };		// 4
        private static int[] kSeaLaneCove = new int[1] { 4 };		    // 5
        private static int[] kSeaLaneCounts = new int[kSpawnPlanesCount] { 3, 3, 2, 2, 2, 1 };
        private static int[][] kSeaLanes = new int[kSpawnPlanesCount][] { kSeaLaneNorth, kSeaLaneEast, kSeaLaneSouth, kSeaLaneWest, kSeaLaneTown, kSeaLaneCove };

        private static int s_IncrementalID = 123; // Random string postfix for ID purposes.

        public ActorAi(PlayfieldController scene)
        {
            mIsDisposed = false;
            mLocked = false;
            mSuspendedMode = false;
		    mShipsPaused = false;
		    mInFuture = false;
		    mScene = scene;
		    mDifficultyFactor = 1.0f;
		    mPirateSpawnTimer = 0;
		    mNavySpawnTimer = 0;
            mCamouflageTimer = 0.0;
            mAshPickupSpawnTimer = GameController.GC.NextRandom((int)(0.33f * TimeKeeper.DAY_CYCLE_IN_SEC), (int)(0.95f * TimeKeeper.DAY_CYCLE_IN_SEC));
            mAshPickupQueue = null;
		    mAiKnob = null;
		    mRandomInt = 0;
            mFleetID = 1;
		    mSpawnPlanes = null;	
		    mVacantSpawnPlanes = null;
		    mOccupiedSpawnPlanes = null;
            CreatePlayingField();
		    mFleet = null;
            mFleetList = new List<PrimeShip>(1);
		    mTempests = new List<TempestActor>(3);
            mHandOfDavys = new List<HandOfDavy>(3);
            mMerchantShips = new List<MerchantShip>(20);
            mNavyShips = new List<NavyShip>(10);
            mPirateShips = new List<PirateShip>(10);
            mPlayerShips = new List<PlayerShip>(1);
            mSkirmishShips = new List<SkirmishShip>(4);
            mEscortShips = new List<EscortShip>(4);
            mAllNpcShips = new List<ShipActor>(60);
            mSharks = new List<Shark>(10);
            mPeople = new List<OverboardActor>(30);
            mAshPickups = new List<AshPickupActor>(3);
            mPups = new List<SKPupActor>(3);
            mPupQueue = new WeightedKeyCycler(SKPup.LootKeys, SKPup.LootWeightings);
            mPupQueue.Randomize();
		    mShipTypes = ShipFactory.Factory.AllNpcShipTypes;

            mOnScreenTargetsCache = new List<ShipActor>(40);

            mThinkTank = new double[THINK_TANK_COUNT];
            ResetThinkTank();
            mThinking = false;
        }

        public static void SetupAiKnob(AiKnob aiKnob)
        {
            aiKnob.merchantShipsMin = kDefaultMerchantShipsMin;
            aiKnob.merchantShipsMax = kDefaultMerchantShipsMax;
            aiKnob.pirateShipsMax = kDefaultPirateShipsMax;
            aiKnob.navyShipsMax = kDefaultNavyShipsMax;

            aiKnob.merchantShipsChance = kDefaultMerchantShipsChance;
            aiKnob.pirateShipsChance = kDefaultPirateShipsChance;
            aiKnob.navyShipsChance = kDefaultNavyShipsChance;
            aiKnob.specialShipsChance = kDefaultSpecialShipsChance;

            aiKnob.fleetShouldSpawn = false;
            aiKnob.fleetTimer = kFleetTimeoutDuration;

            aiKnob.difficulty = 0;
            aiKnob.difficultyIncrement = 1;
            aiKnob.difficultyFactor = 1.01f; // To ensure (int) casts don't go to zero.
            aiKnob.aiModifier = 1.0f;
            aiKnob.stateCeiling = 5;
            aiKnob.state = 0;
        }

        #region Fields
        private bool mIsDisposed;
        private bool mLocked;
        private bool mSuspendedMode;
	    private bool mShipsPaused;
	    private bool mInFuture;
	    private int mRandomInt;
        private uint mFleetID;
	    private float mDifficultyFactor;
        
        // Timers
	    private double mPirateSpawnTimer;
	    private double mNavySpawnTimer;
        private double mCamouflageTimer;

	    private AiKnob mAiKnob;

        private bool mThinking;
        private double[] mThinkTank;

        // Ash Pickups
        private float mAshPickupSpawnTimer;
        private List<uint> mAshPickupQueue;

        // SKPups
        private float mPupSpawnTimer;
        private KeyCycler mPupQueue;

	    // Voodoo Objects
        private List<TempestActor> mTempests;
        private List<HandOfDavy> mHandOfDavys;

	    // Ships
	    private PrimeShip mFleet;
        private List<PrimeShip> mFleetList;
        private List<MerchantShip> mMerchantShips;
        private List<NavyShip> mNavyShips;
        private List<PirateShip> mPirateShips;
        private List<PlayerShip> mPlayerShips;
        private List<SkirmishShip> mSkirmishShips;
        private List<EscortShip> mEscortShips;
        private List<ShipActor> mAllNpcShips;
        private List<Shark> mSharks;
        private List<OverboardActor> mPeople;
        private List<AshPickupActor> mAshPickups;
        private List<SKPupActor> mPups;
        private List<string> mShipTypes;

        // Caches
        private List<ShipActor> mOnScreenTargetsCache;

	    private CCPoint mTownEntrance;
	    private CCPoint mTownDock;
	    private CCPoint mCoveDock;
	    private CCPoint mSilverTrainDest;
	    private CCPoint mTreasureFleetSpawn;
	    private List<List<CCPoint>> mSpawnPlanes;
        private List<List<CCPoint>> mVacantSpawnPlanes;
        private List<List<CCPoint>> mOccupiedSpawnPlanes;

	    private PlayfieldController mScene;
        #endregion

        #region Properties
        public AiKnob AiKnob
        {
            get { return mAiKnob; }
            set
            {
                mAiKnob = value;
	
	            if (mAiKnob != null)
                    UpdateAiKnobState();
            }
        }
        public float DifficultyFactor { get { return mDifficultyFactor; } set { mDifficultyFactor = Math.Max(1f, value); } }
        public bool ShipsPaused { get { return mShipsPaused; } set { mShipsPaused = value; } }
        public bool InFuture { get { return mInFuture; } set { mInFuture = value; } }
        public bool IsPlayfieldClear
        {
            get
            {
                return IsPlayfieldClearOfNpcShips && mPeople.Count == 0;
            }
        }
        public bool IsPlayfieldClearOfNpcShips
        {
            get
            {
                return mAllNpcShips.Count == 0;
            }
        }
        public List<TempestActor> Tempests { get { return mTempests; } }
        public List<HandOfDavy> HandOfDavys { get { return mHandOfDavys; } }
        public Actor Fleet { get { return mFleet; } }
        private float DamageMutinyScaleFactor { get { return 1f; } }
        private List<ShipActor> AllShips
        {
            get
            {
                List<ShipActor> ships = new List<ShipActor>(AllNpcShips.Cast<ShipActor>());
                ships.AddRange(mPlayerShips.Cast<ShipActor>());
                return ships;
            }
        }
        private List<NpcShip> AllNpcShips
        {
            get
            {
                List<NpcShip> ships = new List<NpcShip>(25);
                ships.AddRange(mMerchantShips.Cast<NpcShip>());
                ships.AddRange(mNavyShips.Cast<NpcShip>());
                ships.AddRange(mPirateShips.Cast<NpcShip>());
                ships.AddRange(mEscortShips.Cast<NpcShip>());

                if (mFleet != null)
                    ships.Add(mFleet);

                return ships;
            }
        }
        private List<PursuitShip> AllPursuitShips
        {
            get
            {
                List<PursuitShip> ships = new List<PursuitShip>(15);
                ships.AddRange(mNavyShips.Cast<PursuitShip>());
                ships.AddRange(mPirateShips.Cast<PursuitShip>());
                ships.AddRange(mEscortShips.Cast<PursuitShip>());
                return ships;
            }
        }
        private OverboardActor OverboardPlayer
        {
            get
            {
                OverboardActor actor = null;
    
                foreach (OverboardActor person in mPeople)
                {
                    if (person.IsPlayer)
                    {
                        actor = person;
                        break;
                    }
                }
    
                return actor;
            }
        }
        #endregion

        #region Methods
        private void UpdateAiKnobState()
        {
            if (mAiKnob == null || mAiKnob.difficulty < mAiKnob.stateCeiling)
		        return;
	        float oldAiModifier = mAiKnob.aiModifier;
	
	        mAiKnob.difficulty -= mAiKnob.stateCeiling;
	        ++mAiKnob.state;
	
	        switch (mAiKnob.state)
            {
		        case 0:
			        break;
		        case 1:
			        mAiKnob.pirateShipsChance += (int)(0.025f * kDefaultChanceMax); // 0.15.0.175
			        mAiKnob.stateCeiling = 15;
			        break;
		        case 2:
			        mAiKnob.pirateShipsChance += (int)(0.025f * kDefaultChanceMax); // 0.175.0.2
			        ++mAiKnob.navyShipsMax; // 0.1
			        mAiKnob.navyShipsChance += (int)(0.15f * kDefaultChanceMax); // 0.0.15
			        mAiKnob.stateCeiling = 15;
			        break;
		        case 3:
			        mAiKnob.navyShipsChance += (int)(0.1f * kDefaultChanceMax); // 0.15.0.25
			        mAiKnob.specialShipsChance = kDefaultChanceMax; // 0.1.0
			        mAiKnob.stateCeiling = 15;
			        break;
		        case 4:
			        ++mAiKnob.merchantShipsMin; // 2.3
			        ++mAiKnob.merchantShipsMax; // 4.5
			        mAiKnob.stateCeiling = 15;
			        break;
		        case 5:
		        case 6:
		        case 7:
		        case 8:
			        mAiKnob.aiModifier += 0.05f; // 1.00.1.20
			        mAiKnob.stateCeiling = 20;
			        break;
                case 9:
                    ++mAiKnob.merchantShipsMin; // 3.4
			        ++mAiKnob.merchantShipsMax; // 5.6
                    ++mAiKnob.pirateShipsMax; // 0.1
                    mAiKnob.aiModifier += 0.05f; // 1.20.1.25
			        mAiKnob.stateCeiling = 20;
                    break;
		        case 10:
			        mAiKnob.pirateShipsChance += (int)(0.05f * kDefaultChanceMax); // 0.2.0.25
			        mAiKnob.stateCeiling = 25;
			        break;
		        case 11:
                    mAiKnob.aiModifier += 0.05f; // 1.25.1.3
			        mAiKnob.stateCeiling = 25;
                    break;
		        case 12:
                    mAiKnob.aiModifier += 0.05f; // 1.3.1.35
                    mAiKnob.stateCeiling = 25;
                    break;
		        case 13:
			        ++mAiKnob.merchantShipsMax; // 6.7
                    ++mAiKnob.navyShipsMax; // 1.2
                    mAiKnob.aiModifier += 0.05f; // 1.35.1.40
			        mAiKnob.stateCeiling = 25;
                    break;
		        case 14:
			        mAiKnob.aiModifier += 0.05f; // 1.4.1.45
			        mAiKnob.stateCeiling = 25;
			        break;
		        case 15:
			        mAiKnob.stateCeiling = 25;
			        break;
		        case 16:
			        mAiKnob.pirateShipsChance += (int)(0.05f * kDefaultChanceMax); // 0.25.0.3
			        mAiKnob.navyShipsChance += (int)(0.1f * kDefaultChanceMax); // 0.25.0.35
			        mAiKnob.stateCeiling = 25;
			        break;
		        case 17:
                    ++mAiKnob.merchantShipsMin; // 4.5
                    ++mAiKnob.merchantShipsMax; // 7.8
                    mAiKnob.aiModifier += 0.05f; // 1.45.1.50
			        mAiKnob.stateCeiling = 25;
                    break;
		        case 18:
                    ++mAiKnob.pirateShipsMax; // 1.2
                    ++mAiKnob.navyShipsMax; // 2.3
                    mAiKnob.aiModifier += 0.05f; // 1.50.1.55
			        mAiKnob.stateCeiling = 25;
                    break;
		        case 19:
			        mAiKnob.aiModifier += 0.05f; // 1.55.1.60
			        mAiKnob.stateCeiling = 25;
			        break;
		        case 20:
			        mAiKnob.pirateShipsChance += (int)(0.05f * kDefaultChanceMax); // 0.3.0.35
			        mAiKnob.navyShipsChance += (int)(0.05f * kDefaultChanceMax); // 0.35.0.4
			        mAiKnob.stateCeiling = 15;
			        break;
                case 21:
                    ++mAiKnob.merchantShipsMin; // 5.6
                    ++mAiKnob.merchantShipsMax; // 8.9
                    goto case 25;
                case 22:
                case 23:
                case 24:
                case 25:
                    mAiKnob.aiModifier += 0.05f; // 1.60.1.85
			        mAiKnob.stateCeiling = 25;
                    break;
                case 26:
                    ++mAiKnob.merchantShipsMin; // 6.7
                    ++mAiKnob.merchantShipsMax; // 9.10
                    ++mAiKnob.pirateShipsMax; // 2.3
                    mAiKnob.stateCeiling = 15;
                    break;
		        default:
			        mAiKnob.aiModifier += 0.07f; // 1.85....
			        mAiKnob.stateCeiling = 25;
			        break;
	        }

            mAiKnob.stateCeiling = (int)(mAiKnob.stateCeiling * (1.0f / mDifficultyFactor));
	
	        //NSLog(@"AiState: %d Ceiling: %d", mAiKnob.state, mAiKnob.stateCeiling);

            DispatchEvent(new NumericValueChangedEvent(NumericValueChangedEvent.CUST_EVENT_TYPE_AI_STATE_VALUE_CHANGED, mAiKnob.state, mAiKnob.state-1));
	
	        // Prevent inifnite loops and absurd values
	        if (mAiKnob.stateCeiling <= 0)
		        mAiKnob.stateCeiling = 15;
	        else if (mAiKnob.stateCeiling > 50)
		        mAiKnob.stateCeiling = 50;
	        if (mAiKnob.difficulty < 0)
		        mAiKnob.difficulty = 0;
	        else if (mAiKnob.difficulty > 50)
		        mAiKnob.difficulty = 50;
	
	        if (mAiKnob.difficulty >= mAiKnob.stateCeiling)
		        UpdateAiKnobState();
	        else if (oldAiModifier != mAiKnob.aiModifier)
                DispatchEvent(new NumericValueChangedEvent(NumericValueChangedEvent.CUST_EVENT_TYPE_AI_KNOB_VALUE_CHANGED, mAiKnob.aiModifier, oldAiModifier));

#if SK_BOTS || IOS_SCREENS
            //For testing
            if (mAiKnob.state < 45)
            {
                mAiKnob.difficulty = mAiKnob.stateCeiling;
                UpdateAiKnobState();
            }
#endif
        }

        public void AdvanceAiKnobToState(int state)
        {
            while (mAiKnob.state < state)
            {
                mAiKnob.difficulty = mAiKnob.stateCeiling;
                UpdateAiKnobState();
            }
        }

        public void EnableSuspendedMode(bool enable)
        {
            StopThinking();

            if (!enable)
                Think();
            mSuspendedMode = enable;
        }

        private void TurnAiKnob()
        {
            if (mAiKnob != null && !mInFuture && !GameController.GC.ThisTurn.IsGameOver)
            {
		        mAiKnob.difficulty += (int)(mAiKnob.difficultyIncrement * mAiKnob.difficultyFactor);
                UpdateAiKnobState();
	        }
        }

        private void ResetThinkTank()
        {
            for (int i = 0; i < THINK_TANK_COUNT - 1; ++i)
                mThinkTank[i] = 0.3 + i * 0.3;
            mThinkTank[THINK_CYCLE] = kActorAiThinkInterval;
        }

        private void Think()
        {
            mThinking = true;
        }

        private void Think(double time)
        {
            if (!mThinking)
                return;
    
            GameController gc = GameController.GC;
    
            for (int i = 0; i < THINK_TANK_COUNT; ++i)
            {
                mThinkTank[i] -= time;
        
                if (mThinkTank[i] <= 0)
                {
                    mThinkTank[i] += kActorAiThinkInterval;
            
                    if (gc.ThisTurn.AdvState != ThisTurn.AdventureState.Normal && i != THINK_SHARK)
                        continue;
            
                    switch (i)
                    {
                        case THINK_SPECIAL:
                            ThinkSpecialShips();
                            break;
                        case THINK_NAVY:
                            ThinkNavyShips();
                            break;
                        case THINK_PIRATE:
                            ThinkPirateShips();
                            break;
                        case THINK_MERCHANT:
                            ThinkMerchantShips();
                            break;
                        case THINK_SHARK:
                            ThinkSharks();
                            break;
                        case THINK_CYCLE:
                            TurnAiKnob();
                            mPirateSpawnTimer += kActorAiThinkInterval;
                            mNavySpawnTimer += kActorAiThinkInterval;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void StopThinking()
        {
            mThinking = false;
        }

        private void ResetFleetTimer()
        {
            if (mAiKnob != null)
            {
                mAiKnob.fleetShouldSpawn = false;
                mAiKnob.fleetTimer = kFleetTimeoutDuration;
            }
        }

        private void ResetAshPickupTimer()
        {
            GameController gc = GameController.GC;

            if (mScene.GameMode == GameMode.Career)
            {
#if IOS_SCREENS
                mAshPickupSpawnTimer = 5;
#else
                mAshPickupSpawnTimer = gc.NextRandom((int)(0.33f * TimeKeeper.DAY_CYCLE_IN_SEC), (int)(0.95f * TimeKeeper.DAY_CYCLE_IN_SEC));
#endif
            }
            else
                mAshPickupSpawnTimer = gc.NextRandom(3, 6) * 0.05f * TimeKeeper.DAY_CYCLE_IN_SEC;
        }

        private void ResetSKPupTimer()
        {
            if (mScene.GameMode != GameMode.Career)
                mPupSpawnTimer = GameController.GC.NextRandom(2, 4) * 0.05f * TimeKeeper.DAY_CYCLE_IN_SEC;
        }

        private void AdvanceFleetTimer(double time)
        {
            if (mAiKnob != null && !mAiKnob.fleetShouldSpawn)
            {
                mAiKnob.fleetTimer -= time;

                if (mAiKnob.fleetTimer <= 0)
                    mAiKnob.fleetShouldSpawn = true;
            }
        }

        private void AdvanceAshPickupTimer(double time)
        {
            mAshPickupSpawnTimer -= (float)time;
    
            if (mAshPickupSpawnTimer <= 0)
            {
                GameController gc = GameController.GC;
                if (mScene.GameMode == GameMode.Career)
                {
                    mAshPickupSpawnTimer = Math.Max(0.5f * TimeKeeper.DAY_CYCLE_IN_SEC,
                        gc.TimeKeeper.TimeRemainingToday + gc.NextRandom((int)(0.05f * TimeKeeper.DAY_CYCLE_IN_SEC), (int)(0.95f * TimeKeeper.DAY_CYCLE_IN_SEC)));

                    if (!GameController.GC.ThisTurn.IsGameOver && !mScene.RaceEnabled)
                        SpawnAshPickupActor();
                }
                else
                {
                    mAshPickupSpawnTimer = gc.NextRandom(4, 7) * 0.05f * TimeKeeper.DAY_CYCLE_IN_SEC;

                    if (mSkirmishShips.Count > 0)
                        SpawnAshPickupActor();
                }
            }
        }

        private void AdvanceSKPupTimer(double time)
        {
            if (mScene.GameMode != GameMode.Career)
            {
                mPupSpawnTimer -= (float)time;

                if (mPupSpawnTimer <= 0)
                {
                    ResetSKPupTimer();

                    if (mSkirmishShips.Count > 0)
                        SpawnSKPupActor();
                }
            }
        }

        public void AdvanceTime(double time)
        {
            if (mSuspendedMode)
                return;
            mRandomInt = GameController.GC.NextRandom(0, kDefaultChanceMax);
            AdvanceFleetTimer(time);
            AdvanceAshPickupTimer(time);
            AdvanceSKPupTimer(time);
            Think(time);
    
            if (mCamouflageTimer > 0.0)
            {
                mCamouflageTimer -= time;

                if (mCamouflageTimer <= 0.0)
                    DeactivateCamouflage();
            }
        }

        public uint RandomAshKey()
        {
            if (mAshPickupQueue == null)
            {
                mAshPickupQueue = Ash.ProcableAshKeys;
        
                // Randomize
                int count = mAshPickupQueue.Count;
        
                for (int i = 0; i < count; ++i)
                {
                    int randIndex = GameController.GC.NextRandom(0, count - 1);
                    uint key = mAshPickupQueue[randIndex];
                    mAshPickupQueue.RemoveAt(randIndex);
                    mAshPickupQueue.Insert(0, key);
                }
        
                // Move objectives ash to the front of the queue
                uint requiredAshType = mScene.ObjectivesManager.RequiredAshType;
        
                if (requiredAshType != 0)
                {
                    int index = 0;
            
                    foreach (uint key in mAshPickupQueue)
                    {
                        if (key == requiredAshType)
                            break;
                        ++index;
                    }
            
                    if (index > 0 && index < mAshPickupQueue.Count)
                    {
                        uint key = mAshPickupQueue[index];
                        mAshPickupQueue.RemoveAt(index);
                        mAshPickupQueue.Insert(0, key);
                    }
                }
            }
    
            uint ashKey = Ash.ASH_DEFAULT;
    
            if (mAshPickupQueue != null && mAshPickupQueue.Count > 0)
            {
                uint key = mAshPickupQueue[0];
        
                if (mAshPickupQueue.Count >= 2)
                {
                    mAshPickupQueue.RemoveAt(0);
                    mAshPickupQueue.Insert(GameController.GC.NextRandom(1, mAshPickupQueue.Count), key);
                }
            
                ashKey = key;
            }
    
            return ashKey;
        }

        private Vector2 RandomPickupLocation(Vector4? padding = null)
        {
            GameController gc = GameController.GC;
            Vector2 pickupLoc = Vector2.Zero;
            Vector2 townLoc = new Vector2(ResManager.P2MX(-126), ResManager.P2MY(-198));
            Vector2 beachLoc = new Vector2(ResManager.P2MX(mScene.ViewWidth + 416), ResManager.P2MY(mScene.ViewHeight + 248));
            Vector2 temp;

            Vector4 spawnPadding = (padding == null || !padding.HasValue) ? Vector4.Zero : padding.Value;
            int left = (int)ResManager.P2MX(spawnPadding.X + 32), right = (int)ResManager.P2MX(mScene.ViewWidth - (spawnPadding.Y + 32));
            int upper = (int)ResManager.P2MY(spawnPadding.Z + 32), lower = (int)ResManager.P2MY(mScene.ViewHeight - (spawnPadding.W + 128));
            
            for (int i = 0; i < 10; ++i)
            {
                pickupLoc = new Vector2(gc.NextRandom(left, right), gc.NextRandom(lower, upper));

                Vector2.Subtract(ref pickupLoc, ref townLoc, out temp);
                if (temp.LengthSquared() < 834f) // 924x924 = 462 radius => 462 / 16 = 28.875 Box2D radius => 28.875^2 ~= 834
                    continue;

                Vector2.Subtract(ref beachLoc, ref pickupLoc, out temp);
                if (temp.LengthSquared() < 2438f) // 1580x1580 = 790 radius => 790 / 16 = 49.375 Box2D radius => 49.375^2 ~= 2438
                    continue;

                return pickupLoc;
            }

            // Give up after 10 attempts and just spawn the treasure in the center
            return new Vector2(ResManager.P2MX(mScene.ViewWidth) / 2, ResManager.P2MY(mScene.ViewHeight) / 2);
        }

        private void ThinkSpecialShips()
        {
            if (mShipsPaused || mInFuture || mAiKnob == null)
		        return;
	
	        if (mFleet == null)
            {
		        if (mAiKnob.fleetShouldSpawn && mRandomInt < mAiKnob.specialShipsChance)
                {
                    float shipChance = 0.5f;
                    ShipActor ship = null;
            
                    if (mScene.ObjectivesManager.RequiredNpcShipType == ObjectivesDescription.SHIP_TYPE_SILVER_TRAIN)
                        ship = SpawnSilverTrain();
                    else if (mScene.ObjectivesManager.RequiredNpcShipType == ObjectivesDescription.SHIP_TYPE_TREASURE_FLEET)
                        ship = SpawnTreasureFleet();
                    else if (mRandomInt < shipChance * mAiKnob.specialShipsChance)
                        ship = SpawnSilverTrain();
                    else
                        ship = SpawnTreasureFleet();
            
                    if (ship != null)
                    {
                        if (ship is SilverTrain)
                            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SILVER_TRAIN_SPAWNED));
                        else
                            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_TREASURE_FLEET_SPAWNED));
                    }
			
			        if (mFleet != null)
                        ResetFleetTimer();
		        }
	        }
        }

        private void ThinkMerchantShips()
        {
            if (mShipsPaused || mAiKnob == null)
		        return;
	
	        if (mMerchantShips.Count >= mAiKnob.merchantShipsMax)
		        return;
	        if (mRandomInt < mAiKnob.merchantShipsChance || mMerchantShips.Count < mAiKnob.merchantShipsMin)
            {
		        // Chance to spawn different types of merchant ships
		        int merchantType = mRandomInt, breakEarly = Math.Min(2,mAiKnob.merchantShipsMax - mMerchantShips.Count);
		        int typeIncrement = mAiKnob.merchantShipsChance / 3;
		
                SpawnMerchantShip(merchantType);
		
		        while (breakEarly > 0 || mMerchantShips.Count < mAiKnob.merchantShipsMin)
                {
			        merchantType += typeIncrement;
			
			        if (merchantType > mAiKnob.merchantShipsChance)
				        merchantType -= mAiKnob.merchantShipsChance;
			        if (SpawnMerchantShip(merchantType) == null)
				        break; // No free spawn places
			        --breakEarly;
		        }
	        }
        }

        private void ThinkNavyShips()
        {
            if (mScene.GameMode != GameMode.Career)
                return;

            PlayerShip ship = (mPlayerShips.Count > 0) ? mPlayerShips[0] : null;
	
	        if (GameController.GC.ThisTurn.IsGameOver || mAiKnob == null || ship == null || ship.MotorBoating || mShipsPaused  || mInFuture || mNavyShips.Count >= mAiKnob.navyShipsMax)
		        return;
	        if (mRandomInt < mAiKnob.navyShipsChance || mNavySpawnTimer > 45) {
		        if (SpawnNavyShip() != null)
			        mNavySpawnTimer = 0;
	        }
        }

        private void ThinkPirateShips()
        {
            if (mScene.GameMode != GameMode.Career)
                return;

            if (GameController.GC.ThisTurn.IsGameOver || mAiKnob == null || mShipsPaused || mInFuture || mPirateShips.Count >= mAiKnob.pirateShipsMax)
		        return;
	        if (mRandomInt < mAiKnob.pirateShipsChance || mPirateSpawnTimer > 45)
            {
		        if (SpawnPirateShip() != null)
			        mPirateSpawnTimer = 0;
	        }
        }

        private void ThinkSharks()
        {
            foreach (OverboardActor person in mPeople)
            {
		        if (person.Predator == null && !person.IsPreparingForNewGame)
                {
			        Shark shark = FindPredatorForPrey(person);
			
			        if (shark == null)
                    {
				        if (mPeople.Count >= 3 * mSharks.Count)
					        SpawnShark();
				        break;
			        }
		        }
	        }
	
	        if (mSharks.Count < kMaxSharks && mRandomInt < kDefaultChanceMax)
                SpawnShark();
        }

        private Shark FindPredatorForPrey(OverboardActor prey)
        {
            // Broken in original version, so leave it broken. It works, but not how originally intended.
            Shark predator = null;
	
	        if (prey.Edible)
            {
		        foreach (Shark shark in mSharks)
                {
			        if (shark.Prey == null && !shark.MarkedForRemoval)
                    {
				        shark.Prey = prey;
				        prey.Predator = shark;
				        break;
			        }
		        }
	        }
            return predator; // Always returns null due to bug in original version.
        }

        private void DockNpcShips<T>(List<T> ships) where T : NpcShip
        {
            for (int i = ships.Count-1; i >= 0; --i)
                ships[i].Dock();
        }

        public void DockAllShips()
        {
            if (mFleet != null)
                mFleet.Dock();
            DockNpcShips(mEscortShips);
            DockNpcShips(mMerchantShips);
            DockNpcShips(mNavyShips);
            DockNpcShips(mPirateShips);
        }

        public void PrepareForNewGame()
        {
            for (int i = mHandOfDavys.Count - 1; i >= 0; --i)
                mHandOfDavys[i].Despawn();

            // Force reset of ash queue
            mAshPickupQueue = null;

            mPupQueue.Randomize();

            // Reset State
            InFuture = false;
            ShipsPaused = false;

            // Reset Timers
            mPirateSpawnTimer = 0.0;
            mNavySpawnTimer = 0.0;
            mCamouflageTimer = 0.0;
            ResetFleetTimer();
            ResetAshPickupTimer();
            ResetSKPupTimer();
            ResetThinkTank();
            Think();
        }

        public void PrepareForGameOver()
        {
            List<PursuitShip> pursuitShips = AllPursuitShips;

            foreach (PursuitShip pursuitShip in pursuitShips)
                pursuitShip.EndPursuit();
        }

        public void PrepareForMontyMutiny()
        {
            List<PursuitShip> pursuitShips = AllPursuitShips;

            foreach (PursuitShip pursuitShip in pursuitShips)
                pursuitShip.EndPursuit();
        }

        public void SinkAllShipsWithDeathBitmap(uint deathBitmap, SKTeamIndex sinkerID)
        {
            NpcShip ship = null;

            for (int i = mAllNpcShips.Count - 1; i >= 0; --i)
            {
                ship = mAllNpcShips[i] as NpcShip;

                if (!ship.Docking)
                {
                    ship.DeathBitmap = deathBitmap;
                    ship.SinkerID = sinkerID;
                    ship.Sink();
                }
            }

            for (int i = mSkirmishShips.Count - 1; i >= 0; --i)
            {
                SkirmishShip skShip = mSkirmishShips[i];

                if (skShip.TeamIndex != sinkerID)
                    skShip.DamageShip(10);
            }
            

	        for (int i = mPeople.Count - 1; i >= 0; --i)
            {
		        OverboardActor person = mPeople[i];
                person.DeathBitmap = deathBitmap;
                //person.KillerID = sinkerID; // The ship that puts the person in the water is rewarded, not the killer.
                person.EnvironmentalDeath();
	        }
        }

        public ShipActor RequestNewMerchantEnemy(ShipActor ship)
        {
            return ClosestTargetTo(ship.X, ship.Y, mMerchantShips);
        }

#if false
        public ShipActor OnScreenTarget<T>(List<T> targets) where T : ShipActor
        {
            if (targets == null)
                return null;

            ShipActor target = null;
	        SPRectangle rect = new SPRectangle(30, 30, ResManager.RESW - 60, ResManager.RESH - 90); // 90 to give 30 clearance above ship deck
	
	        foreach (ShipActor ship in targets)
            {
		        if (!ship.MarkedForRemoval && rect.Contains(ship.X, ship.Y))
                {
			        if (ship is NpcShip)
                    {
				        NpcShip npcShip = ship as NpcShip;
				
				        if (npcShip.Docking)
					        continue;
			        }
			        target = ship;
			        break;
		        }
	        }

	        return target;
        }

        public List<ShipActor> OnScreenTargets<T>(IEnumerable<T> targets) where T : ShipActor
        {
            if (targets == null)
                return null;
            
            List<ShipActor> ships = new List<ShipActor>();
            SPRectangle rect =
                (mScene.GameMode == GameMode.Career)
                ? new SPRectangle(30, 30, ResManager.RESW - 60, ResManager.RESH - 90) // 90 to give 30 clearance above ship deck
                : new SPRectangle(30, 30, ResManager.RESW - 60, ResManager.RESH - 60);

            foreach (ShipActor ship in targets)
            {
                if (!ship.MarkedForRemoval && rect.Contains(ship.X, ship.Y))
                {
                    if (ship is NpcShip)
                    {
                        NpcShip npcShip = ship as NpcShip;

                        if (npcShip.Docking)
                            continue;
                    }
                    ships.Add(ship);
                }
            }

            return ships;
        }

        public ShipActor ClosestTargetTo<T>(float x, float y, IEnumerable<T> targets) where T : ShipActor
        {
            if (targets == null)
                return null;

            float closest = 99999999.9f, distSq;
            Vector2 dist = Vector2.Zero;
	        ShipActor target = null;
	
            foreach (ShipActor ship in targets)
            {
		        if (ship.MarkedForRemoval)
			        continue;
		        dist.X = x - ship.X;
		        dist.Y = y - ship.Y;
                distSq = dist.LengthSquared();
		
		        if (closest > distSq)
                {
			        closest = distSq;
			        target = ship;
		        }
	        }
	        return target;
        }
#else
        private ShipActor OnScreenTarget(List<ShipActor> targets)
        {
            if (targets == null)
                return null;

            ShipActor target = null;
            SPRectangle rect =
                (mScene.GameMode == GameMode.Career)
                ? new SPRectangle(30, 30, ResManager.RESW - 60, ResManager.RESH - 90) // 90 to give 30 clearance above ship deck
                : new SPRectangle(30, 30, ResManager.RESW - 60, ResManager.RESH - 60);

            foreach (ShipActor ship in targets)
            {
                if (!ship.MarkedForRemoval && rect.Contains(ship.X, ship.Y))
                {
                    if ((ship as NpcShip).Docking)
                        continue;
                    target = ship;
                    break;
                }
            }

            return target;
        }

        private List<ShipActor> OnScreenTargets(List<ShipActor> targets)
        {
            if (targets == null)
                return null;

            List<ShipActor> ships = mOnScreenTargetsCache;
            ships.Clear();

            SPRectangle rect =
                (mScene.GameMode == GameMode.Career)
                ? new SPRectangle(30, 30, ResManager.RESW - 60, ResManager.RESH - 90) // 90 to give 30 clearance above ship deck
                : new SPRectangle(30, 30, ResManager.RESW - 60, ResManager.RESH - 60);

            foreach (ShipActor ship in targets)
            {
                if (!ship.MarkedForRemoval && rect.Contains(ship.X, ship.Y))
                {
                    if ((ship as NpcShip).Docking)
                        continue;
                    ships.Add(ship);
                }
            }

            return ships;
        }

        private ShipActor ClosestTargetTo(float x, float y, List<ShipActor> targets)
        {
            if (targets == null)
                return null;

            float closest = 99999999.9f, distSq;
            Vector2 dist = Vector2.Zero;
            ShipActor target = null;

            foreach (ShipActor ship in targets)
            {
                if (ship.MarkedForRemoval)
                    continue;
                dist.X = x - ship.X;
                dist.Y = y - ship.Y;
                distSq = dist.LengthSquared();

                if (closest > distSq)
                {
                    closest = distSq;
                    target = ship;
                }
            }
            return target;
        }

        private ShipActor ClosestTargetTo(float x, float y, List<MerchantShip> targets)
        {
            if (targets == null)
                return null;

            float closest = 99999999.9f, distSq;
            Vector2 dist = Vector2.Zero;
            ShipActor target = null;

            foreach (MerchantShip ship in targets)
            {
                if (ship.MarkedForRemoval)
                    continue;
                dist.X = x - ship.X;
                dist.Y = y - ship.Y;
                distSq = dist.LengthSquared();

                if (closest > distSq)
                {
                    closest = distSq;
                    target = ship;
                }
            }
            return target;
        }
#endif

        private bool IsVoodooTargetTaken(ShipActor target)
        {
            if (target == null || target.IsPreparingForNewGame)
                return true;
    
	        bool result = false;
	
	        if (target is NpcShip)
            {
		        NpcShip npcShip = target as NpcShip;
		        result = npcShip.InWhirlpoolVortex || npcShip.InDeathsHands;
	        }
	
	        if (!result)
            {
		        foreach (HandOfDavy hod in mHandOfDavys)
                {
			        if (hod.Target == target)
                    {
				        result = true;
				        break;
			        }
		        }
	        }
	
	        if (!result)
            {
                foreach (TempestActor tempest in mTempests)
                {
			        if (tempest.Target == target)
                    {
				        result = true;
				        break;
			        }
		        }
	        }
	        return result;
        }

        public ShipActor RequestNewVoodooTarget()
        {
            ShipActor target = OnScreenTarget(mAllNpcShips);

	        if (target != null && IsVoodooTargetTaken(target))
		        target = null;
	        return target;
        }

        public ShipActor RequestClosestTarget(Actor actor)
        {
            ShipActor target = null;
            List<ShipActor> closestTargets = OnScreenTargets(mAllNpcShips);

            for (int i = closestTargets.Count-1; i >= 0; --i)
            {
                ShipActor ship = closestTargets[i];

                if (IsVoodooTargetTaken(ship))
                    closestTargets.RemoveAt(i);
            }

            target = ClosestTargetTo(actor.X, actor.Y, closestTargets);
            return target;
        }

        private void SetRandomPursuitLocation(PursuitShip ship)
        {
            GameController gc = GameController.GC;
            ship.Destination.SetDestX(ResManager.P2MX(gc.NextRandom(208, (int)ResManager.RITMFX(800))));
            ship.Destination.SetDestY(ResManager.P2MX(gc.NextRandom(144, (int)ResManager.RITMFY(448))));
        }

        private void SetRandomPursuitSpawnLocation(PursuitShip ship)
        {
            GameController gc = GameController.GC;
            ship.Destination.SetDestX(ResManager.P2MX(gc.NextRandom(256, (int)ResManager.RITMFX(704))));
            ship.Destination.SetDestY(ResManager.P2MX(gc.NextRandom(180, (int)ResManager.RITMFY(400))));
        }

        private int GetSpawnPlaneCount(int index)
        {
            int count = 0;

            switch (index)
            {
                case kPlaneIdNorth: count = 4; break;
                case kPlaneIdEast: count = 4; break;
                case kPlaneIdSouth: count = 5; break;
                case kPlaneIdWest: count = 5; break;
                default: Debug.WriteLine(@"Invalid spawn plane Id in ActorAi.GetSpawnPlaneCount:"); break;
            }
            return count;
        }

        private void CheckinSpawn(IPathFollower ship)
        {
            if (ship == null || ship.Destination == null || !ship.Destination.IsExclusive)
                return;

            List<CCPoint> startVacant = null, startOccupied = null;

            if (ship.Destination.SeaLaneA != null)
            {
                startVacant = mVacantSpawnPlanes[ship.Destination.Start];
                startOccupied = mOccupiedSpawnPlanes[ship.Destination.Start];
                startVacant.Add(ship.Destination.SeaLaneA);
                startOccupied.Remove(ship.Destination.SeaLaneA);
                ship.Destination.SeaLaneA = null;
            }
        }

        private void CheckinShip(IPathFollower ship)
        {
            if (ship == null || ship.Destination == null || !ship.Destination.IsExclusive)
                return;

            List<CCPoint> startVacant = null, finishVacant = null, startOccupied = null, finishOccupied = null;

            if (ship.Destination.SeaLaneA != null)
            {
                startVacant = mVacantSpawnPlanes[ship.Destination.Start];
                startOccupied = mOccupiedSpawnPlanes[ship.Destination.Start];
                startVacant.Add(ship.Destination.SeaLaneA);
                startOccupied.Remove(ship.Destination.SeaLaneA);
            }

            if (ship.Destination.SeaLaneB != null)
            {
                finishVacant = mVacantSpawnPlanes[ship.Destination.Finish];
                finishOccupied = mOccupiedSpawnPlanes[ship.Destination.Finish];
                finishVacant.Add(ship.Destination.SeaLaneB);
                finishOccupied.Remove(ship.Destination.SeaLaneB);
            }
        }

        // Note: It's possible for Escort Ships to be heading to town but not have town checked out. They will not collide with
        // other ships coming from town, however, so we ignore it out of convenience.
        private void CheckoutShip(IPathFollower ship)
        {
            if (ship == null || ship.Destination == null)
                return;

            // Town has an intermediate point through which ships must travel. This edge case is hacked in here.
	        if ((ship.Destination.SeaLaneA == mTownDock || ship.Destination.SeaLaneB == mTownDock) && !ship.Destination.FinishIsDest)
		        ship.Destination.SeaLaneC = mTownEntrance; // Mark as edge case. This also sets the current destination.dest to seaLaneC!

	        if (!ship.Destination.IsExclusive)
		        return;

            List<CCPoint> startVacant = mVacantSpawnPlanes[ship.Destination.Start];
            List<CCPoint> finishVacant = mVacantSpawnPlanes[ship.Destination.Finish];
            List<CCPoint> startOccupied = mOccupiedSpawnPlanes[ship.Destination.Start];
            List<CCPoint> finishOccupied = mOccupiedSpawnPlanes[ship.Destination.Finish];

	        if (ship.Destination.SeaLaneA != null)
            {
                startOccupied.Add(ship.Destination.SeaLaneA);
                startVacant.Remove(ship.Destination.SeaLaneA);
	        }
	
	        if (ship.Destination.SeaLaneB != null)
            {
                finishOccupied.Add(ship.Destination.SeaLaneB);
                finishVacant.Remove(ship.Destination.SeaLaneB);
	        }

            foreach (List<CCPoint> spawnPlane in mSpawnPlanes)
            {
                if (spawnPlane.Count == 0)
                    throw new Exception("Bad ActorAi spawnplane state.");
            }
        }

        private bool IsSeaLaneAvailableAtIndex(int index)
        {
            bool result = false;
	
	        if (index < mVacantSpawnPlanes.Count)
            {
		        List<CCPoint> spawnPlane = mVacantSpawnPlanes[index];
		        result = (spawnPlane.Count > 0);
	        }
	        return result;
        }

        private int FindAvailableSeaLaneIndex(int index, int maxIndex)
        {
            int availableIndex = -1, count = kSeaLaneCounts[index], seaLaneIndex;
	        int[] seaLane = kSeaLanes[index];
            int rnd = GameController.GC.NextRandom(0, count - 1);
	
	        for (int i = 0; i < count; ++i)
            {
		        seaLaneIndex = seaLane[rnd];
		
		        if (seaLaneIndex <= maxIndex)
                {
			        if (IsSeaLaneAvailableAtIndex(seaLaneIndex))
                    {
				        availableIndex = seaLaneIndex;
				        break;
			        }
		        }
		
		        if (++rnd == count)
			        rnd = 0;
	        }
	        return availableIndex;
        }

        private Destination CreateRandomDestinationFromPlanes(List<List<CCPoint>> planes, int startIndex, int finishIndex)
        {
            //if (startIndex > finishIndex || planes == null || finishIndex >= planes.Count)
            //    throw new ArgumentException("Cannot create random destination from given arguments.");

            GameController gc = GameController.GC;
            List<CCPoint> startPlane = planes[startIndex];
	        List<CCPoint> finishPlane = planes[finishIndex];
            CCPoint fromPoint = startPlane[gc.NextRandom(0, startPlane.Count - 1)];
            CCPoint toPoint = finishPlane[gc.NextRandom(0, finishPlane.Count - 1)];
            Destination dest = Destination.GetDestination();
	        dest.SeaLaneA = fromPoint;
	        dest.SeaLaneB = toPoint;
	        dest.Start = startIndex;
	        dest.Finish = finishIndex;
	        dest.SpawnPlaneIndex = dest.Start;
	        return dest;
        }

        private void FillDestination(Destination dest, CCPoint from, CCPoint to)
        {
            dest.SeaLaneA = from;
            dest.SeaLaneB = to;
        }

        private int[] mSpawnPlaneIndexBuffer = new int[64];
        private Destination FetchRandomVacantDestination(int spawnPlaneIndexMax, int destPlaneIndexMax)
        {
            int limit = Math.Min(spawnPlaneIndexMax,mVacantSpawnPlanes.Count-1);
	        int i = 0, count = 0;
	        List<CCPoint> spawnPlane = null;
	        Destination dest = null;

            if (mSpawnPlaneIndexBuffer.Length < (limit+1))
                mSpawnPlaneIndexBuffer = new int[limit+1];
	
	        // Collect spawn plane indexes from spawn planes with vacant locations into spawnPlaneIndexes
	        for (i = 0; i <= limit; ++i)
            {
		        spawnPlane = mVacantSpawnPlanes[i];
		
		        if (spawnPlane.Count > 0)
                    mSpawnPlaneIndexBuffer[count++] = i;
	        }
	
	        if (count > 0)
            {
		        int seaLaneIndex = -1;
                int rnd = GameController.GC.NextRandom(0, count - 1);
		
		        for (i = 0; i < count - 1; ++i) // "< count - 1" because sealanes go both ways. Therefore an available route MUST be found before the final iteration.
                {
                    seaLaneIndex = FindAvailableSeaLaneIndex(mSpawnPlaneIndexBuffer[rnd], destPlaneIndexMax);
			
			        if (seaLaneIndex != -1)
				        break;
			        if (++rnd == count)
				        rnd = 0;
		        }
		
		        if (seaLaneIndex != -1) // Travelling from startIndex to finishIndex (finish index found from an valid and available destination in kSeaLanes.
                    dest = CreateRandomDestinationFromPlanes(mVacantSpawnPlanes, mSpawnPlaneIndexBuffer[rnd], seaLaneIndex);
	        }
	        return dest;
        }

        private Destination FetchRandomDestination(int spawnPlaneIndexMax)
        {
            GameController gc = GameController.GC;
            int rndStartPlane, rndFinishPlane, limit = Math.Min(spawnPlaneIndexMax, mSpawnPlanes.Count - 1);
            rndStartPlane = gc.NextRandom(0, limit);

            if (limit <= 0)
                throw new Exception("Infinite loop detected.");
	
	        do
                rndFinishPlane = gc.NextRandom(0, limit);
	        while (rndFinishPlane == rndStartPlane);
	
	        return CreateRandomDestinationFromPlanes(mSpawnPlanes, rndStartPlane, rndFinishPlane);
        }

        private bool IsTownVacant()
        {
            List<CCPoint> spawnPlane = mVacantSpawnPlanes[kPlaneIdTown];
	        return (spawnPlane.Count > 0);
        }

        private bool IsTreasureFleetSpawnVacant()
        {
            List<CCPoint> spawnPlane = mVacantSpawnPlanes[kPlaneIdNorth];
	        return (spawnPlane.Contains(mTreasureFleetSpawn));
        }

        private bool IsSilverTrainDestVacant()
        {
            List<CCPoint> spawnPlane = mVacantSpawnPlanes[kPlaneIdNorth];
            return (spawnPlane.Contains(mSilverTrainDest));
        }

        private Destination FetchTownSpawnDestination()
        {
            Destination dest = null;
	
	        if (IsTownVacant())
            {
		        int seaLaneIndex = FindAvailableSeaLaneIndex(kPlaneIdTown, kPlaneIdEast);
		
		        if (seaLaneIndex != -1)
			        dest = CreateRandomDestinationFromPlanes(mVacantSpawnPlanes, kPlaneIdTown, seaLaneIndex);
	        }
	        return dest;
        }

        private Destination FetchTreasureFleetDestination()
        {
            Destination dest = null;
	
	        if (IsTownVacant() && IsTreasureFleetSpawnVacant())
            {
                dest = Destination.GetDestination();
		        dest.Start = kPlaneIdNorth;
		        dest.Finish = kPlaneIdTown;
		        dest.SpawnPlaneIndex = dest.Start;
                FillDestination(dest, mTreasureFleetSpawn, mTownDock);
	        }
            return dest;
        }

        private Destination FetchSilverTrainDestination()
        {
            Destination dest = null;
	
	        if (IsTownVacant() && IsSilverTrainDestVacant())
            {
                dest = Destination.GetDestination();
		        dest.Start = kPlaneIdTown;
		        dest.Finish = kPlaneIdNorth;
		        dest.SpawnPlaneIndex = dest.Start;
                FillDestination(dest, mTownDock, mSilverTrainDest);
	        }
            return dest;
        }

        public void RequestTargetForPursuer(IPursuer pursuer)
        {
            if (mLocked)
                return;
    
            if (pursuer is PirateShip)
            {
                PirateShip pirateShip = pursuer as PirateShip;
		        pirateShip.Enemy = RequestNewMerchantEnemy(pirateShip);
            }
            else if (pursuer is NavyShip)
            {
                NavyShip navyShip = pursuer as NavyShip;
		
		        if (navyShip.Destination.SeaLaneC == mTownEntrance)
                    navyShip.Destination.SetFinishAsDest();
		
		        PlayerShip playerShip = (mPlayerShips.Count > 0) ? mPlayerShips[0] : null;
		
		        if (playerShip != null && !playerShip.IsCamouflaged && !playerShip.MarkedForRemoval)
			        navyShip.Enemy = playerShip;
		        else
			        navyShip.DuelState = PursuitShip.PursuitState.SailingToDock;
            }
            else if (pursuer is EscortShip)
            {
                EscortShip escortShip = pursuer as EscortShip;
                escortShip.DuelState = PursuitShip.PursuitState.Escorting;
            }
            else if (pursuer is TempestActor)
            {
                TempestActor tempest = pursuer as TempestActor;
                tempest.Target = RequestClosestTarget(tempest);
            }
            else if (pursuer is HandOfDavy)
            {
                HandOfDavy hod = pursuer as HandOfDavy;
                hod.Target = RequestNewVoodooTarget() as NpcShip;
            }
        }

        public void ActorDepartedPort(Actor actor)
        {
            if (mLocked || actor == null || actor is IPathFollower == false)
		        return;
	        IPathFollower ship = actor as IPathFollower;
	
	        // Release spawn point to keep traffic moving
	        if (ship.Destination != null && ship.Destination.SeaLaneA != null)
                CheckinSpawn(ship);
        }

        public void ActorArrivedAtDestination(Actor actor)
        {
            if (mLocked || actor == null || actor is IPathFollower == false)
		        return;
	        IPathFollower ship = actor as IPathFollower;

            if (ship.Destination == null)
                return;
	
	        if (ship.Destination.SeaLaneC == mTownEntrance)
            {
		        if (actor is EscortShip)
                {
			        EscortShip escortShip = actor as EscortShip;
			
			        if (escortShip.Enemy != null && escortShip.DuelState != PursuitShip.PursuitState.SailingToDock)
                    {
                        SetRandomPursuitLocation(escortShip);
				        return;
			        }
		        }

                ship.Destination.SetFinishAsDest();
		
		        // PrimeShip Escorts (they will handle this themselves if their escortee is dead)
		        if (ship == mFleet)
                {
                    if (mFleet.LeftEscort != null && mFleet.LeftEscort.Destination != null)
                        mFleet.LeftEscort.Destination.SetFinishAsDest();
                    if (mFleet.RightEscort != null && mFleet.RightEscort.Destination != null)
                        mFleet.RightEscort.Destination.SetFinishAsDest();
		        }
		
		        if (ship.Destination.SeaLaneB == mTownDock)
                {
			        // Mark as non-collidable to prevent town harbour from clogging up
			        ship.IsCollidable = false;
			
			        if (ship == mFleet)
                    {
                        if (mFleet.LeftEscort != null)
				            mFleet.LeftEscort.IsCollidable = false;
                        if (mFleet.RightEscort != null)
				            mFleet.RightEscort.IsCollidable = false;
			        }
		        }
		        return;
	        }
	
	        if (actor is PursuitShip)
            {
		        PursuitShip pursuitShip = actor as PursuitShip;
		
		        if (pursuitShip.DuelState != PursuitShip.PursuitState.SailingToDock)
                    SetRandomPursuitLocation(pursuitShip);
		        else
                    pursuitShip.Dock();
	        }
            else
            {
                ship.Dock();
	        }
        }

        public void PrisonerOverboard(Prisoner prisoner, ShipActor ship)
        {
            if (mLocked)
		        return;

            if (ship == null && mScene.GameMode == GameMode.Career)
		        ship = (mPlayerShips.Count > 0) ? mPlayerShips[0] : null;
            if (ship == null)
                return;
	
	        Vector2 shipLoc = ship.OverboardLocation;
	        
	        if (prisoner == null)
            {
                prisoner = new Prisoner("Prisoner0");
                prisoner.Gender = (mRandomInt < 0.67f * kDefaultChanceMax) ? Gender.Male : Gender.Female;
                prisoner.TextureName = "prisoner0";
	        }
	
	        OverboardActor person = SpawnPersonOverboardAt(shipLoc.X, shipLoc.Y, prisoner);
            FindPredatorForPrey(person);

            if (mScene.GameMode != GameMode.Career && ship is NpcShip)
                person.KillerID = (ship as NpcShip).SinkerID;
        }

        private PrimeShip SpawnSilverTrainAt(float x, float y, float angle, Destination dest, bool escorts)
        {
            if (mFleet != null)
                throw new InvalidOperationException("Attempt to spawn SilverTrain while fleet is already active.");
	
	        if (mLocked || dest == null)
		        return null;
	
	        string shipKey = "SilverTrain";
            ShipDetails shipDetails = ShipDetails.GetNpcShipDetails(shipKey);
            //ActorDef actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);
            //mFleet = new SilverTrain(actorDef, shipKey);
            mFleet = ShipActor.GetNpcShip(shipKey, x, y, angle) as SilverTrain;
            mFleetList.Add(mFleet);
            mAllNpcShips.Add(mFleet);

	        mFleet.ShipDetails = shipDetails;
	        mFleet.AiModifier = mAiKnob.aiModifier;
            mFleet.SetupShip();
	        mFleet.Destination = dest;
	
            mScene.AddActor(mFleet);
            CheckoutShip(mFleet);
	
	        if (escorts)
                SpawnSilverTrainEscortShips();
	        return mFleet;
        }

        private PrimeShip SpawnSilverTrain()
        {
            if (mLocked)
		        return null;
	        PrimeShip ship = null;
	        Destination dest = FetchSilverTrainDestination();
	        
	        if (dest != null)
            {
                dest.AdjustedSeaLaneC = new CCPoint(ResManager.P2MX(240), ResManager.P2MY(200));
		        ship = SpawnSilverTrainAt(dest.Loc.X, dest.Loc.Y, kSpawnAngles[dest.SpawnPlaneIndex], dest, true);
                ship.FleetID = mFleetID++;
            }
    
	        return ship;
        }

        private PrimeShip SpawnTreasureFleetAt(float x, float y, float angle, Destination dest, bool escorts)
        {
            if (mFleet != null)
                throw new InvalidOperationException("Attempt to spawn TreasureFleet while fleet is already active.");

            if (mLocked || dest == null)
                return null;

            string shipKey = "TreasureFleet";
            ShipDetails shipDetails = ShipDetails.GetNpcShipDetails(shipKey);
            //ActorDef actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);
            //mFleet = new TreasureFleet(actorDef, shipKey);
            mFleet = ShipActor.GetNpcShip(shipKey, x, y, angle) as TreasureFleet;
            mFleetList.Add(mFleet);
            mAllNpcShips.Add(mFleet);

            mFleet.ShipDetails = shipDetails;
            mFleet.AiModifier = mAiKnob.aiModifier;
            mFleet.SetupShip();
            mFleet.Destination = dest;

            mScene.AddActor(mFleet);
            CheckoutShip(mFleet);

            if (escorts)
                SpawnTreasureFleetEscortShips();
            
            return mFleet;
        }

        private PrimeShip SpawnTreasureFleet()
        {
            if (mLocked)
                return null;
            PrimeShip ship = null;
            Destination dest = FetchTreasureFleetDestination();

            if (dest != null)
            {
                dest.AdjustedSeaLaneC = new CCPoint(ResManager.P2MX(140), ResManager.P2MY(140));
                ship = SpawnTreasureFleetAt(dest.Loc.X, dest.Loc.Y, kSpawnAngles[dest.SpawnPlaneIndex], dest, true);
                ship.FleetID = mFleetID++;
            }

            return ship;
        }

        private void SpawnSilverTrainEscortShips()
        {
            if (mFleet == null || mLocked)
		        return;
	        Destination dest = Destination.DestinationWithDestination(mFleet.Destination);
            dest.SetLocX(ResManager.P2MX(-110));
            dest.SetLocY(ResManager.P2MY(-138));
	        dest.IsExclusive = false; // Prevents checking in of destination points we don't own.
	        mFleet.LeftEscort = SpawnEscortShip(dest);
	        mFleet.LeftEscort.Escortee = mFleet;
	
	        dest = Destination.DestinationWithDestination(mFleet.Destination);
            dest.SetLocX(ResManager.P2MX(-136));
            dest.SetLocY(ResManager.P2MY(-108));
	        dest.IsExclusive = false;
	        mFleet.RightEscort = SpawnEscortShip(dest);
	        mFleet.RightEscort.Escortee = mFleet;
	
            mScene.AddActor(mFleet.LeftEscort);
            mScene.AddActor(mFleet.RightEscort);
            AddActor(mFleet.LeftEscort);
            AddActor(mFleet.RightEscort);
        }

        private void SpawnTreasureFleetEscortShips()
        {
            if (mFleet == null || mLocked)
                return;
            Destination dest = Destination.DestinationWithDestination(mFleet.Destination);
            dest.SetLocX(dest.Loc.X + 6);
            dest.SetLocY(dest.Loc.Y - 1.5f);
            dest.IsExclusive = false; // Prevents checking in of destination points we don't own.
            mFleet.LeftEscort = SpawnEscortShip(dest);
            mFleet.LeftEscort.Escortee = mFleet;
            mFleet.LeftEscort.WillEnterTown = true;

            dest = Destination.DestinationWithDestination(mFleet.Destination);
            dest.SetLocX(dest.Loc.X + 6);
            dest.SetLocY(dest.Loc.Y + 1.5f);
            dest.IsExclusive = false;
            mFleet.RightEscort = SpawnEscortShip(dest);
            mFleet.RightEscort.Escortee = mFleet;
            mFleet.RightEscort.WillEnterTown = true;

            mScene.AddActor(mFleet.LeftEscort);
            mScene.AddActor(mFleet.RightEscort);
            AddActor(mFleet.LeftEscort);
            AddActor(mFleet.RightEscort);
        }

        private EscortShip SpawnEscortShipAt(float x, float y, float angle, Destination dest)
        {
            if (mLocked || dest == null)
		        return null;
	
	        string shipKey = "Escort";
            ShipDetails shipDetails = ShipDetails.GetNpcShipDetails(shipKey);

            // Ensure turn-fights have a clear victor
	        if (mFleet != null && mFleet.LeftEscort != null)
		        shipDetails.ControlRating += 2;

            //ActorDef actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);
            //EscortShip ship = new EscortShip(actorDef, shipKey);
            EscortShip ship = ShipActor.GetNpcShip(shipKey, x, y, angle) as EscortShip;
	        ship.ShipDetails = shipDetails;
            ship.CannonDetails = CannonDetails.GetCannonDetails("Perisher");
	        ship.AiModifier = mAiKnob.aiModifier;
            ship.SetupShip();
	        ship.Destination = dest;
            return ship;
        }

        private EscortShip SpawnEscortShip(Destination dest)
        {
            if (mLocked || dest == null)
		        return null;
	
	        return SpawnEscortShipAt(dest.Loc.X, dest.Loc.Y, kSpawnAngles[dest.SpawnPlaneIndex], dest);
        }

        private MerchantShip SpawnMerchantShipAt(string shipKey, float x, float y, float angle, Destination dest)
        {
            if (mLocked || dest == null)
                return null;

            ShipDetails shipDetails = ShipDetails.GetNpcShipDetails(shipKey);
            //ActorDef actorDef = ShipFactory.Factory.CreateShipDefForShipType("Merchant", x, y, angle);
            //MerchantShip ship = new MerchantShip(actorDef, shipKey);
            MerchantShip ship = ShipActor.GetNpcShip(shipKey, x, y, angle) as MerchantShip;
            ship.ShipDetails = shipDetails;
            ship.CannonDetails = CannonDetails.GetCannonDetails("Perisher");
            ship.AiModifier = mAiKnob.aiModifier;
            ship.InFuture = mInFuture;
            ship.SetupShip();
            ship.Destination = dest;
            mScene.AddActor(ship);
            AddActor(ship);
            return ship;
        }

        private MerchantShip SpawnMerchantShip(int type)
        {
            if (mLocked)
		        return null;
	        MerchantShip ship = null;
	        Destination dest = FetchRandomVacantDestination(kPlaneIdTown, kPlaneIdTown);
	        string shipKey = null;
	
	        if (type < mAiKnob.merchantShipsChance * 0.4f)
		        shipKey = "MerchantCaravel";
	        else if (type < mAiKnob.merchantShipsChance * 0.75f)
		        shipKey = "MerchantGalleon";
	        else
		        shipKey = "MerchantFrigate";
	
	        if (dest != null)
		        ship = SpawnMerchantShipAt(shipKey, dest.Loc.X, dest.Loc.Y, kSpawnAngles[dest.SpawnPlaneIndex], dest);
	        return ship;
        }

        private NavyShip SpawnNavyShipAt(float x, float y, float angle, Destination dest)
        {
            if (mLocked || dest == null)
                return null;

            string shipKey = "Navy";
            ShipDetails shipDetails = ShipDetails.GetNpcShipDetails(shipKey);
            //ActorDef actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);

            //NavyShip ship = new NavyShip(actorDef, shipKey);
            NavyShip ship = ShipActor.GetNpcShip(shipKey, x, y, angle) as NavyShip;
            ship.ShipDetails = shipDetails;
            ship.CannonDetails = CannonDetails.GetCannonDetails("Perisher");
            ship.AiModifier = mAiKnob.aiModifier;
            ship.SetupShip();
            ship.Destination = dest;
            mScene.AddActor(ship);
            AddActor(ship);
            return ship;
        }

        private NavyShip SpawnNavyShip()
        {
            if (mLocked)
		        return null;
	        NavyShip ship = null;
	        Destination dest = FetchTownSpawnDestination();

            if (dest != null)
            {
                ship = SpawnNavyShipAt(dest.Loc.X, dest.Loc.Y, kSpawnAngles[dest.SpawnPlaneIndex], dest);

                if (ship != null && GameSettings.GS.ValueForKey(GameSettings.NAVY_SHIP_TIPS) < 5)
                {
                    GameSettings.GS.SetValueForKey(GameSettings.NAVY_SHIP_TIPS, GameSettings.GS.ValueForKey(GameSettings.NAVY_SHIP_TIPS) + 1);

                    string hintName = GameSettings.NAVY_SHIP_TIPS + s_IncrementalID++;
                    mScene.DisplayHintByName(hintName, ship.X, ship.Y, ship.Height, ship, false);
                    mScene.Juggler.DelayInvocation(this, 15f, delegate { mScene.HideHintByName(hintName); });
                    ship.HintName = hintName;
                    ship.IsHintAttached = true;
                }
            }

	        return ship;
        }

        private PirateShip SpawnPirateShipAt(float x, float y, float angle, Destination dest)
        {
            if (mLocked || dest == null)
                return null;

            string shipKey = "Pirate";
            ShipDetails shipDetails = ShipDetails.GetNpcShipDetails(shipKey);
            //ActorDef actorDef = ShipFactory.Factory.CreateShipDefForShipType(shipKey, x, y, angle);

            //PirateShip ship = new PirateShip(actorDef, shipKey);
            PirateShip ship = ShipActor.GetNpcShip(shipKey, x, y, angle) as PirateShip;
            ship.ShipDetails = shipDetails;
            ship.CannonDetails = CannonDetails.GetCannonDetails("Perisher");
            ship.AiModifier = mAiKnob.aiModifier;
            ship.SetupShip();
            ship.Destination = dest;
            SetRandomPursuitSpawnLocation(ship);
            mScene.AddActor(ship);
            AddActor(ship);
            return ship;
        }

        private PirateShip SpawnPirateShip()
        {
            if (mLocked)
		        return null;
	        PirateShip ship = null;
	        Destination dest = FetchRandomVacantDestination(kPlaneIdWest, kPlaneIdWest);

            if (dest != null)
            {
                ship = SpawnPirateShipAt(dest.Loc.X, dest.Loc.Y, kSpawnAngles[dest.SpawnPlaneIndex], dest);

                if (ship != null && GameSettings.GS.ValueForKey(GameSettings.PIRATE_SHIP_TIPS) < 5)
                {
                    GameSettings.GS.SetValueForKey(GameSettings.PIRATE_SHIP_TIPS, GameSettings.GS.ValueForKey(GameSettings.PIRATE_SHIP_TIPS) + 1);

                    string hintName = GameSettings.PIRATE_SHIP_TIPS + s_IncrementalID++;
                    mScene.DisplayHintByName(hintName, ship.X, ship.Y, ship.Height, ship, false);
                    mScene.Juggler.DelayInvocation(this, 15f, delegate { mScene.HideHintByName(hintName); });
                    ship.HintName = hintName;
                    ship.IsHintAttached = true;
                }
            }

	        return ship;
        }

        public void EnactMontysMutiny()
        {
            ShipActor ship = (mPlayerShips.Count > 0) ? mPlayerShips[0] : null;
            
            if (ship == null)
                return;
	
	        Vector2 shipLoc = ship.OverboardLocation;
	        Prisoner prisoner = new Prisoner(null);
            prisoner.Gender = Gender.Male;
            prisoner.InfamyBonus = 500000;
	
	        OverboardActor person = SpawnPersonOverboardAt(shipLoc.X, shipLoc.Y, prisoner);
            person.HasRepellent = true;
            person.IsPlayer = true;
            FindPredatorForPrey(person);
        }

        public void MarkPlayerAsEdible()
        {
            OverboardActor actor = OverboardPlayer;

            if (actor != null)
                actor.HasRepellent = false;
        }

        public TempestActor SummonTempest(float duration, Color cloudColor, bool audible)
        {
	        if (mLocked)
		        return null;

	        List<CCPoint> spawnPlane = mSpawnPlanes[kPlaneIdWest];
	        CCPoint loc = ((mTempests.Count & 1) == 1) ? spawnPlane[1] : spawnPlane[0];
            return SummonTempestAt(loc.X, loc.Y, duration, cloudColor, audible);
        }

        public TempestActor SummonTempestAt(float x, float y, float duration, Color cloudColor, bool audible)
        {
	        if (mLocked)
		        return null;

            TempestActor tempest = TempestActor.GetTempestActor(x, y, 0, duration, cloudColor, audible);
            mTempests.Add(tempest);
            mScene.AddActor(tempest);
            return tempest;
        }

        private void OnHandOfDavyDismissed(SPEvent ev)
        {
            HandOfDavy hod = ev.CurrentTarget as HandOfDavy;
            mScene.RemoveProp(hod);
            hod.Target = null;
            mHandOfDavys.Remove(hod);
        }

        public HandOfDavy SummonHandOfDavyWithDuration(float duration)
        {
            if (mLocked)
                return null;

            if (mScene.GameMode == GameMode.Career)
            {
                Idol hodIdol = mScene.IdolForKey(Idol.VOODOO_SPELL_HAND_OF_DAVY);

                if (hodIdol == null)
                    return null;

                int maxHodAllowed = Idol.CountForIdol(hodIdol);

                if (mHandOfDavys.Count >= maxHodAllowed)
                    return null;
            }

            HandOfDavy hod = HandOfDavy.GetHandOfDavy(duration);
            mHandOfDavys.Add(hod);
            mScene.AddProp(hod);

            if (!hod.HasEventListenerForType(HandOfDavy.CUST_EVENT_TYPE_HAND_OF_DAVY_DISMISSED))
                hod.AddEventListener(HandOfDavy.CUST_EVENT_TYPE_HAND_OF_DAVY_DISMISSED, (SPEventHandler)OnHandOfDavyDismissed);
            return hod;
        }

        private Shark SpawnSharkAt(float x, float y, float angle, Destination dest)
        {
            if (mLocked || dest == null)
		        return null;
	        //ActorDef actorDef = MiscFactory.Factory.CreateSharkDef(x, y, angle);
	        //Shark shark = new Shark(actorDef, "Shark");
            Shark shark = Shark.SharkAt(x, y, angle, "Shark");
	        dest.IsExclusive = false;
	        shark.Destination = dest;
	
            mScene.AddActor(shark);
            AddActor(shark);
            return shark;
        }

        private Shark SpawnShark()
        {
            if (mLocked)
		        return null;
	        Shark shark = null;
	        Destination dest = FetchRandomDestination(kPlaneIdWest);

	        if (dest != null)
		        shark = SpawnSharkAt(dest.Loc.X, dest.Loc.Y, kSpawnAngles[dest.SpawnPlaneIndex], dest);
	        return shark;
        }

        private OverboardActor SpawnPersonOverboardAt(float x, float y, Prisoner prisoner)
        {
            if (mLocked)
		        return null;
	        Destination dest = FetchRandomDestination(kPlaneIdWest);
            dest.SetLocX(x);
            dest.SetLocY(y);
            dest.SetDestX(x);
            dest.SetDestY(y);
            dest.IsExclusive = false;

	        //ActorDef actorDef = MiscFactory.Factory.CreatePersonOverboardDef(dest.Loc.X, dest.Loc.Y, SPMacros.SP_D2R(mRandomInt));
	        //OverboardActor person = new OverboardActor(actorDef, "Prisoner");
            OverboardActor person = OverboardActor.OverboardActorAt(dest.Loc.X, dest.Loc.Y, SPMacros.SP_D2R(mRandomInt), "Prisoner");
	        person.Prisoner = prisoner;
	        person.Destination = dest;

            mScene.AddActor(person);
            AddActor(person);

            if (mPeople.Count == 20)
                mScene.AchievementManager.GrantSmorgasbordAchievement();
            return person;
        }

        private AshPickupActor SpawnAshPickupActorAt(float x, float y, uint ashKey, float duration)
        {
            AshPickupActor actor = AshPickupActor.AshPickupActorWithKey(ashKey, x, y, ResManager.P2M(28), duration);
            mScene.AddActor(actor);
            AddActor(actor);
            return actor;
        }

        private AshPickupActor SpawnAshPickupActor()
        {
#if IOS_SCREENS
            uint ashKey = Ash.ASH_MOLTEN;
#else
            uint ashKey = RandomAshKey();
#endif
            string settingKey = Ash.GameSettingForKey(ashKey);
            Vector2 loc = Vector2.Zero;

            // Restrict spawn area if hints are to be displayed.
            if (settingKey != null && !GameSettings.GS.SettingForKey(settingKey))
                loc = RandomPickupLocation(new Vector4(96, 96, 0, 32));
            else
                loc = RandomPickupLocation();

            return SpawnAshPickupActorAt(loc.X, loc.Y, ashKey, 30);
        }

        private SKPupActor SpawnSKPupActorAt(float x, float y, uint pupKey, float duration)
        {
            SKPupActor actor = SKPupActor.SKPupActorWithKey(pupKey, x, y, ResManager.P2M(34), duration);
            mScene.AddActor(actor);
            AddActor(actor);
            return actor;
        }

        private SKPupActor SpawnSKPupActor()
        {
            // NOTE:
            // Current min time until pup needs to be available again (either used or expired) is 75sec.
            // Current max time a pup can be in use is 45sec. Maintain this safety net or
            // implement measures to secure against things like two Sea of Lava spells occurring at once.
            // Based on: SKPup.LootKeys.Count, duration, SKPup.DurationForKey and mPupSpawnTimer in ResetSKPupTimer.

            Vector2 loc = RandomPickupLocation();
            uint pupKey = mPupQueue.NextKey();
            return SpawnSKPupActorAt(loc.X, loc.Y, pupKey, 15);
        }

        public void ActivateCamouflageForDuration(float duration)
        {
            mCamouflageTimer = duration;
    
	        PlayerShip playerShip = (mPlayerShips.Count > 0) ? mPlayerShips[0] : null;

	        if (playerShip == null || playerShip.IsCamouflaged)
		        return;
            playerShip.ActivateCamouflage();
	
	        foreach (NavyShip ship in mNavyShips)
                ship.PlayerCamouflageActivated(true);
	
	        foreach (EscortShip ship in mEscortShips)
                ship.PlayerCamouflageActivated(true);
        }

        public void DeactivateCamouflage()
        {
            PlayerShip playerShip = (mPlayerShips.Count > 0) ? mPlayerShips[0] : null;
	
	        if (playerShip == null || !playerShip.IsCamouflaged)
		        return;
            playerShip.DeactivateCamouflage();

            foreach (NavyShip ship in mNavyShips)
                ship.PlayerCamouflageActivated(false);

            foreach (EscortShip ship in mEscortShips)
                ship.PlayerCamouflageActivated(false);
        }

        public SkirmishShip SkirmishShipForIndex(PlayerIndex playerIndex)
        {
            SkirmishShip ship = null;

            if (mSkirmishShips != null)
            {
                foreach (SkirmishShip skShip in mSkirmishShips)
                {
                    if (skShip.SKPlayerIndex == playerIndex)
                    {
                        ship = skShip;
                        break;
                    }
                }
            }

            return ship;
        }

        public void AddActor(Actor actor)
        {
            if (actor is IPathFollower)
                CheckoutShip(actor as IPathFollower);

            if (actor is NpcShip)
                mAllNpcShips.Add(actor as ShipActor);

            if (actor is MerchantShip)
                mMerchantShips.Add(actor as MerchantShip);
            else if (actor is PirateShip)
                mPirateShips.Add(actor as PirateShip);
            else if (actor is NavyShip)
                mNavyShips.Add(actor as NavyShip);
            else if (actor is Shark)
                mSharks.Add(actor as Shark);
            else if (actor is OverboardActor)
                mPeople.Add(actor as OverboardActor);
            else if (actor is EscortShip)
                mEscortShips.Add(actor as EscortShip);
            else if (actor is AshPickupActor)
            {
                mAshPickups.Add(actor as AshPickupActor);
                //actor.AddEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_ASH_PICKUP_LOOTED, (NumericValueChangedEventHandler)mScene.OnAshPickupLooted);
                //actor.DispatchEvent(SPEvent.SPEventWithType(AshPickupActor.CUST_EVENT_TYPE_ASH_PICKUP_SPAWNED));
            }
            else if (actor is SKPupActor)
            {
                mPups.Add(actor as SKPupActor);
                //actor.AddEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_SK_PUP_LOOTED, (NumericValueChangedEventHandler)mScene.OnSKPupLooted);
            }
            else if (actor is PlayerShip)
                mPlayerShips.Add(actor as PlayerShip);
            else if (actor is SkirmishShip)
            {
#if DEBUG
                if (SkirmishShipForIndex((actor as SkirmishShip).SKPlayerIndex) != null)
                    throw new InvalidOperationException("Duplicate SkirmishShips SKPlayerIndexes in ActorAi.");
#endif
                mSkirmishShips.Add(actor as SkirmishShip);
            }
        }

        public void RemoveActor(Actor actor)
        {
            if (mLocked || actor == null || actor is Cannonball) // Short-circuit for most common case (Cannonball)
		        return;
	
	        if (actor is IPathFollower)
                CheckinShip(actor as IPathFollower);

	        if (actor is NpcShip)
            {
		        NpcShip ship = actor as NpcShip;

                if (ship.IsHintAttached)
                    mScene.HideHintByName(ship.HintName);
		
		        if (ship.DeathBitmap == DeathBitmaps.PLAYER_CANNON && !ship.IsPreparingForNewGame && ship.Destination.FinishIsDest && ship.Destination.SeaLaneB == mTownDock)
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_CLOSE_BUT_NO_CIGAR_STATE_REACHED));

                mAllNpcShips.Remove(ship);
	        }

            if (actor == mFleet)
            {
                if (!actor.IsPreparingForNewGame)
                {
                    if (mAiKnob.fleetTimer < 10)
                        mAiKnob.fleetTimer = 10; // Don't want them spawning over the top of each other if they've docked

                    if (actor is TreasureFleet)
                        DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_TREASURE_FLEET_ATTACKED));
                    else if (actor is SilverTrain)
                        DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SILVER_TRAIN_ATTACKED));
                }

                mFleetList.Clear();
                mFleet = null;
            }
            else if (actor is TempestActor && mTempests.IndexOf(actor as TempestActor) != -1)
            {
                TempestActor tempest = actor as TempestActor;
                mTempests.Remove(tempest);
            }
            else if (actor is MerchantShip)
                mMerchantShips.Remove(actor as MerchantShip);
            else if (actor is PirateShip)
                mPirateShips.Remove(actor as PirateShip);
            else if (actor is NavyShip)
                mNavyShips.Remove(actor as NavyShip);
            else if (actor is Shark)
                mSharks.Remove(actor as Shark);
            else if (actor is OverboardActor)
                mPeople.Remove(actor as OverboardActor);
            else if (actor is EscortShip)
            {
                EscortShip escortShip = actor as EscortShip;

                if (mFleet != null && mFleet.FleetID == escortShip.FleetID && !escortShip.IsPreparingForNewGame)
                {
                    if (mFleet is TreasureFleet)
                        DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_TREASURE_FLEET_ATTACKED));
                    else if (mFleet is SilverTrain)
                        DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SILVER_TRAIN_ATTACKED));
                }

                mEscortShips.Remove(escortShip);
            }
            else if (actor is AshPickupActor)
            {
                mAshPickups.Remove(actor as AshPickupActor);
                //actor.RemoveEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_ASH_PICKUP_LOOTED, (NumericValueChangedEventHandler)mScene.OnAshPickupLooted);
            }
            else if (actor is SKPupActor)
            {
                mPups.Remove(actor as SKPupActor);
                //actor.RemoveEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_SK_PUP_LOOTED, (NumericValueChangedEventHandler)mScene.OnSKPupLooted);
            }
            else if (actor is PlayerShip)
                mPlayerShips.Remove(actor as PlayerShip);
            else if (actor is SkirmishShip)
                mSkirmishShips.Remove(actor as SkirmishShip);
        }

        private void CreatePlayingField()
        {
            if (mSpawnPlanes != null)
		        return;
	        mSpawnPlanes = new List<List<CCPoint>>(kSpawnPlanesCount);
	        mVacantSpawnPlanes = new List<List<CCPoint>>(kSpawnPlanesCount);
	        mOccupiedSpawnPlanes = new List<List<CCPoint>>(kSpawnPlanesCount);
	
	        ResOffset offset = ResManager.RESM.ItemOffsetWithAlignment(ResManager.ResAlignment.LowerRight);
	
	        // Spawn Planes - North
	        int numPoints = 4;
	        float xOrigin = 1024.0f + offset.X;
	        float yOrigin = 72.0f;
	        float step = ResManager.RITMFY(72.0f);
	        CCPoint point = null;
	        List<CCPoint> spawnPlane = new List<CCPoint>(numPoints);
	
	        for (int i = 0; i < numPoints; ++i)
            {
		        point = new CCPoint(ResManager.P2MX(xOrigin), ResManager.P2MY(yOrigin + i * step));
                spawnPlane.Add(point);
		
		        if (i == 1)
                {
			        mTreasureFleetSpawn = point;
			        mSilverTrainDest = point;
		        }
	        }
	
            mSpawnPlanes.Add(spawnPlane);
	
	        // East
	        numPoints = 4;
	        xOrigin = 112.0f;
	        yOrigin = 704.0f + offset.Y;
	        step = ResManager.RITMFX(112.0f);
	        spawnPlane = new List<CCPoint>(numPoints);
	
	        for (int i = 0; i < numPoints; ++i)
            {
		        point = new CCPoint(ResManager.P2MX(xOrigin + i * step), ResManager.P2MY(yOrigin));
                spawnPlane.Add(point);
	        }
	
	        mSpawnPlanes.Add(spawnPlane);
	
	        // South
	        numPoints = 5;
	        xOrigin = -64.0f;
	        yOrigin = mScene.ViewHeight;
	        step = ResManager.RITMFY(72.0f);
	        spawnPlane = new List<CCPoint>(numPoints);
	
	        for (int i = 0; i < numPoints; ++i)
            {
                point = new CCPoint(ResManager.P2MX(xOrigin), ResManager.P2MY(yOrigin - i * step));
                spawnPlane.Add(point);
	        }
	
	        mSpawnPlanes.Add(spawnPlane);
	
	        // West
	        numPoints = 5;
	        xOrigin = mScene.ViewWidth;
	        yOrigin = -64.0f;
	        step = ResManager.RITMFX(128.0f);
	        spawnPlane = new List<CCPoint>(numPoints);
	
	        for (int i = 0; i < numPoints; ++i)
            {
                point = new CCPoint(ResManager.P2MX(xOrigin - i * step), ResManager.P2MY(yOrigin));
                spawnPlane.Add(point);
	        }
	
	        mSpawnPlanes.Add(spawnPlane);
	
	        // Town Spawn
	        numPoints = 1;
	        spawnPlane = new List<CCPoint>(numPoints);
	        mTownDock = new CCPoint(ResManager.P2MX(-58.0f), ResManager.P2MY(-64.0f));
            spawnPlane.Add(mTownDock);
            mSpawnPlanes.Add(spawnPlane);
	
	        // Town Entrance
	        mTownEntrance = new CCPoint(ResManager.P2MX(140.0f), ResManager.P2MY(120.0f));
	
	        // Cove Spawn
	        numPoints = 1;
	        spawnPlane = new List<CCPoint>(numPoints);
	        mCoveDock = new CCPoint(ResManager.P2MX(mScene.ViewWidth), ResManager.P2MY(380.0f + offset.Y));
            spawnPlane.Add(mCoveDock);
            mSpawnPlanes.Add(spawnPlane);
	
	        // Setup vacant/occupied collections
	        foreach (List<CCPoint> plane in mSpawnPlanes)
            {
                mVacantSpawnPlanes.Add(new List<CCPoint>(plane));
                mOccupiedSpawnPlanes.Add(new List<CCPoint>(plane.Count));
	        }
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
                    mLocked = true;

                    if (mTempests.Count > 0)
                    {
		                foreach (TempestActor tempest in mTempests)
			                tempest.Dispose();
	                }
	
	                if (mHandOfDavys.Count > 0)
                    {
		                foreach (HandOfDavy hod in mHandOfDavys)
                        {
			                hod.Target = null;
                            hod.RemoveEventListener(HandOfDavy.CUST_EVENT_TYPE_HAND_OF_DAVY_DISMISSED, (SPEventHandler)OnHandOfDavyDismissed);
                            mScene.RemoveProp(hod);
		                }
	                }
	
	                if (mFleet != null)
                    {
                        mScene.RemoveActor(mFleet);
                        mFleet = null;
	                }

                    foreach (Actor actor in mAllNpcShips)
		                mScene.RemoveActor(actor);

	                foreach (Actor actor in mSharks)
                        mScene.RemoveActor(actor);
	
	                foreach (Actor actor in mPeople)
                        mScene.RemoveActor(actor);
    
                    foreach (Actor actor in mAshPickups)
                    {
                        actor.RemoveEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_ASH_PICKUP_LOOTED, (NumericValueChangedEventHandler)mScene.OnAshPickupLooted);
		                mScene.RemoveActor(actor);
	                }
	
	                foreach (Actor actor in mPlayerShips)
                        mScene.RemoveActor(actor);

                    foreach (Actor actor in mSkirmishShips)
                        mScene.RemoveActor(actor);
    
                    mAshPickupQueue = null;
                    mFleetList = null;
                    mNavyShips = null;
                    mMerchantShips = null;
	                mPirateShips = null;
                    mEscortShips = null;
                    mSharks = null;
                    mPeople = null;
                    mAshPickups = null;
                    mTempests = null;
                    mHandOfDavys = null;
                    mPlayerShips = null;
                    mSkirmishShips = null;
                    mShipTypes = null;
                    mTreasureFleetSpawn = null;
                    mSilverTrainDest = null;
                    mTownEntrance = null;
                    mTownDock = null;
                    mCoveDock = null;
                    mSpawnPlanes = null;
                    mVacantSpawnPlanes = null;
                    mOccupiedSpawnPlanes = null;
    
                    mScene = null;

                    mLocked = false;
                }

                mIsDisposed = true;
            }
        }

        ~ActorAi()
        {
            Dispose(false);
        }
        #endregion
    }
}
