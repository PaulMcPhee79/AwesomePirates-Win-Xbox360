using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class PlayerDetails : SPEventDispatcher
    {
        public PlayerDetails(GameStats gameStats)
        {
            mGameStats = gameStats;
            ShipDetails = ShipFactory.Factory.CreateShipDetailsForType(mGameStats.ShipName);
            CannonDetails = CannonFactory.Factory.CreateCannonDetailsForType(mGameStats.CannonName);
            mGameStats.AddEventListener(AwesomePirates.GameStats.CUST_EVENT_TYPE_SHIP_TYPE_CHANGED, (SPEventHandler)OnShipTypeChanged);
            mGameStats.AddEventListener(AwesomePirates.GameStats.CUST_EVENT_TYPE_CANNON_TYPE_CHANGED, (SPEventHandler)OnCannonTypeChanged);
        }
        
        #region Fields
        private GameStats mGameStats;
        private ShipDetails mShipDetails;
        private CannonDetails mCannonDetails;
        #endregion

        #region Properties
        public string Name { get { return mGameStats.Alias; } set { mGameStats.Alias = value; } }
        public uint Abilities { get { return mGameStats.Abilities; } }
        public GameStats GameStats { get { return mGameStats; } }

        public int HiScore { get { return mGameStats.HiScore; } set { mGameStats.HiScore = value; } }
        public float PlayerRating { get { return 1f; } }
        public int ScoreMultiplier { get { return GameController.GC.ObjectivesManager.ScoreMultiplier; } }
        public ShipDetails ShipDetails { get { return mShipDetails; } set { mShipDetails = value; } }
        public CannonDetails CannonDetails { get { return mCannonDetails; } set { mCannonDetails = value; } }
        #endregion

        #region Methods
        public void Reset()
        {
            mShipDetails.Reset();
        }

        private void AcquireNewShip()
        {
            ShipDetails details = ShipFactory.Factory.CreateShipDetailsForType(mGameStats.ShipName);

            if (mShipDetails != null)
                details.Reset();
            ShipDetails = details;
        }

        private void AcquireNewCannon()
        {
            CannonDetails = CannonFactory.Factory.CreateCannonDetailsForType(mGameStats.CannonName);
        }

        private void OnShipTypeChanged(SPEvent ev)
        {
            AcquireNewShip();
        }

        private void OnCannonTypeChanged(SPEvent ev)
        {
            AcquireNewCannon();
        }

        public void OnPlayerChanged(SPEvent ev)
        {
            Cleanup();

            mGameStats = GameController.GC.GameStats;
            AcquireNewShip();
            AcquireNewCannon();
            mGameStats.AddEventListener(AwesomePirates.GameStats.CUST_EVENT_TYPE_SHIP_TYPE_CHANGED, (SPEventHandler)OnShipTypeChanged);
            mGameStats.AddEventListener(AwesomePirates.GameStats.CUST_EVENT_TYPE_CANNON_TYPE_CHANGED, (SPEventHandler)OnCannonTypeChanged);
        }

        public void Cleanup()
        {
            if (mGameStats != null)
            {
                mGameStats.RemoveEventListener(AwesomePirates.GameStats.CUST_EVENT_TYPE_SHIP_TYPE_CHANGED, (SPEventHandler)OnShipTypeChanged);
                mGameStats.RemoveEventListener(AwesomePirates.GameStats.CUST_EVENT_TYPE_CANNON_TYPE_CHANGED, (SPEventHandler)OnCannonTypeChanged);
                mGameStats = null;
            }
        }
        #endregion
    }
}
