using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class ThisTurn : SPEventDispatcher, IDisposable
    {
        public enum AdventureState
        {
            Normal = 0,
            StopShips,
            Overboard,
            Eaten,
            Dead
        }

        public const string CUST_EVENT_TYPE_MUTINY_COUNTDOWN_CHANGED = "mutinyCountdownChanged";
        public const string CUST_EVENT_TYPE_MUTINY_VALUE_CHANGED = "mutinyValueChanged";
        public const string CUST_EVENT_TYPE_INFAMY_VALUE_CHANGED = "infamyValueChanged";

        private const int kMutinyThreshold = 6;
        private const int THIS_TURN_TUTORIAL_MODE = 0x1;
        private const int THIS_TURN_IS_GAME_OVER = 0x2;

        public ThisTurn()
        {
            mWasGameProgressMade = false;
            mTurnID = 0;
		    mSettings = 0;
            mMutiny = 0;
            MutinyCountdown = new Countdown(10, 10);
            mPotionMultiplier = 1.0f;
            mInfamyMultiplier = 10;
            mInfamy = 0;
        
            mStatsCommitted = false;
            mCannonballsShot = 0;
            mCannonballsHit = 0;
            mShipsSunk = 0;
            mDaysAtSea = 0;
        
            mAdventureState = AdventureState.Normal;

            mMutinyChangedEvent = null;
            mInfamyChangedEvent = null;
        }

        #region Fields
        private bool mIsDisposed = false;
        private bool mWasGameProgressMade;
        private uint mTurnID;
	    private int mSettings;
        private int mMutiny;
        private Countdown mMutinyCountdown;
        private float mPotionMultiplier;
        private int mInfamyMultiplier;
        private int mInfamy;

        // Stats
        private bool mStatsCommitted;
        private uint mCannonballsShot;
	    private uint mCannonballsHit;
        private uint mShipsSunk;
        private float mDaysAtSea;
        
        // Modes/States
        private AdventureState mAdventureState;

        // Events
        private NumericRatioChangedEvent mMutinyChangedEvent;
        private NumericValueChangedEvent mInfamyChangedEvent;
        #endregion

        #region Properties
        public bool WasGameProgressMade { get { return mWasGameProgressMade; } set { mWasGameProgressMade = value; } }
        public uint TurnID { get { return mTurnID; } set { mTurnID = value; } }
        public int Settings { get { return mSettings; } set { mSettings = value; } }
        public bool IsGameOver
        {
            get { return ((mSettings & THIS_TURN_IS_GAME_OVER) == THIS_TURN_IS_GAME_OVER); }
            set
            {
                if (value) mSettings |= THIS_TURN_IS_GAME_OVER;
                else mSettings &= ~THIS_TURN_IS_GAME_OVER;
            }
        }
        public bool TutorialMode
        {
            get { return ((mSettings & THIS_TURN_TUTORIAL_MODE) == THIS_TURN_TUTORIAL_MODE); }
            set
            {
                if (value) mSettings |= THIS_TURN_TUTORIAL_MODE;
                else mSettings &= ~THIS_TURN_TUTORIAL_MODE;
            }
        }
        public static int MutinyThreshold { get { return kMutinyThreshold; } }
        public bool PlayerShouldDie { get { return (mMutiny == kMutinyThreshold && mMutinyCountdown.Counter == mMutinyCountdown.CounterMax); } }
        public int Mutiny
        {
            get { return mMutiny; }
            set
            {
                if (value > kMutinyThreshold)
                    ResetMutinyCountdown();
    
	            int adjustedValue = Math.Max(0,Math.Min(kMutinyThreshold, value));
    
	            int delta = adjustedValue - mMutiny;
	            mMutiny = adjustedValue;

                if (mMutinyChangedEvent == null)
                    mMutinyChangedEvent = new NumericRatioChangedEvent(CUST_EVENT_TYPE_MUTINY_VALUE_CHANGED, adjustedValue, 0, kMutinyThreshold, delta);
                else
                    mMutinyChangedEvent.UpdateValues(adjustedValue, 0, kMutinyThreshold, delta);

                DispatchEvent(mMutinyChangedEvent);
    
                if (mMutiny == 0)
                    ResetMutinyCountdown();
            }
        }
        public Countdown MutinyCountdown 
        {
            get { return mMutinyCountdown; }
            set
            {
                if (mMutinyCountdown != value)
                {
                    if (mMutinyCountdown != null)
                        mMutinyCountdown.RemoveEventListener(CUST_EVENT_TYPE_MUTINY_COUNTDOWN_CHANGED, (NumericRatioChangedEventHandler)OnMutinyCountdownChanged);
                    mMutinyCountdown = value;

                    if (mMutinyCountdown != null)
                        mMutinyCountdown.AddEventListener(CUST_EVENT_TYPE_MUTINY_COUNTDOWN_CHANGED, (NumericRatioChangedEventHandler)OnMutinyCountdownChanged);
                }
            }
        }
        public float PotionMultiplier { get { return mPotionMultiplier; } set { mPotionMultiplier = value; } }
        public int InfamyMultiplier { get { return mInfamyMultiplier; } set { mInfamyMultiplier = value; } }
        public int Infamy
        {
            get { return mInfamy; }
            set
            {
                int oldVal = mInfamy;
                mInfamy = value;

                if (mInfamyChangedEvent == null)
                    mInfamyChangedEvent = new NumericValueChangedEvent(CUST_EVENT_TYPE_INFAMY_VALUE_CHANGED, value, oldVal);
                else
                    mInfamyChangedEvent.UpdateValues(value, oldVal);

                DispatchEvent(mInfamyChangedEvent);
            }
        }
        public uint DifficultyMultiplier { get { return Math.Max(1, GameController.GC.TimeKeeper.Day); } }
        public AdventureState AdvState { get { return mAdventureState; } set { mAdventureState = value; } }
        public uint CannonballsShot { get { return mCannonballsShot; } set { mCannonballsShot = value; } }
        public uint CannonballsHit { get { return mCannonballsHit; } set { mCannonballsHit = value; } }
        public uint ShipsSunk { get { return mShipsSunk; } set { mShipsSunk = value; } }
        public float CannonAccuracy
        {
            get
            {
                float accuracy = 0;

                if (mCannonballsShot != 0)
                    accuracy = mCannonballsHit / (float)mCannonballsShot;

                return accuracy;
            }
        }
        public float DaysAtSea { get { return mDaysAtSea; } set { mDaysAtSea = value; } }
        #endregion

        #region Methods
        public ThisTurn copy()
        {
            ThisTurn copy = new ThisTurn();
            copy.WasGameProgressMade = WasGameProgressMade;
            copy.TurnID = TurnID;
            copy.Settings = Settings;
            copy.Mutiny = Mutiny;
            copy.MutinyCountdown = MutinyCountdown;
            copy.PotionMultiplier = PotionMultiplier;
            copy.InfamyMultiplier = InfamyMultiplier;
            copy.Infamy = Infamy;
            copy.mStatsCommitted = mStatsCommitted;
            copy.CannonballsShot = CannonballsShot;
            copy.CannonballsHit = CannonballsHit;
            copy.ShipsSunk = ShipsSunk;
            copy.DaysAtSea = DaysAtSea;
            copy.AdvState = AdvState;
            return copy;
        }

        public void CommitStats()
        {
            if (mStatsCommitted)
                return;

            GameController gc = GameController.GC;
            gc.GameStats.CannonballsShot += mCannonballsShot;
            gc.GameStats.CannonballsHit += mCannonballsHit;
            gc.GameStats.DaysAtSea += mDaysAtSea;
            mStatsCommitted = true;
        }

        private void ResetStats()
        {
            mCannonballsShot = 0;
            mCannonballsHit = 0;
            mShipsSunk = 0;
            mDaysAtSea = 0;
            mStatsCommitted = false;
        }

        public void PrepareForNewTurn()
        {
            IsGameOver = false;
            mWasGameProgressMade = false;
            ++mTurnID;
            mMutiny = 0;
            mMutinyCountdown.SoftReset();
            mPotionMultiplier = Potion.NotorietyFactorForPotion(GameController.GC.GameStats.PotionForKey(Potion.POTION_NOTORIETY));
            mInfamy = 0;
            mAdventureState = AdventureState.Normal;
            ResetStats();
        }

        public void AddMutiny(int amount)
        {
            PlayerShip ship = GameController.GC.PlayerShip;

            if (IsGameOver || (amount > 0 && ship != null && ship.IsFlyingDutchman))
                return;
            Mutiny += amount;
        }

        public void ReduceMutinyCountdown(float amount)
        {
            int reduction = (int)amount;
    
            for (int i = 0; i < reduction; ++i)
            {
                if (mMutiny == 0)
                    break;
                mMutinyCountdown.Decrement();
            }
    
            if (mMutiny > 0)
                mMutinyCountdown.ReduceBy(amount - reduction);
        }

        public void ResetMutinyCountdown()
        {
            mMutinyCountdown.Reset();
        }

        private void OnMutinyCountdownChanged(NumericRatioChangedEvent ev)
        {
            DispatchEvent(ev);

            if ((int)ev.Value == 0)
                AddMutiny(-1);
        }

        public int AddInfamy(float amount)
        {
            if (IsGameOver)
                return 0;

            float adjustedAmount = amount * mInfamyMultiplier * mPotionMultiplier;
            return AddInfamyUnfiltered(adjustedAmount);
        }

        public int AddInfamyUnfiltered(float amount)
        {
            int roundedAmount = (int)amount;
            Infamy += roundedAmount;
            return roundedAmount;
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
                    MutinyCountdown = null;
                    mMutinyChangedEvent = null;
                    mInfamyChangedEvent = null;
                }

                mIsDisposed = true;
            }
        }

        ~ThisTurn()
        {
            Dispose(false);
        }
        #endregion
    }
}
