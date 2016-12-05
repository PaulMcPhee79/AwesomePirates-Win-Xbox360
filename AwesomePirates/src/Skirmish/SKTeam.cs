using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    public enum SKTeamIndex
    {
        Red = 0,
        Blue,
        Green,
        Yellow
    }

    class SKTeam : SPEventDispatcher
    {
        public const string CUST_EVENT_TYPE_SKTEAM_MEMBER_ADDED = "skTeamMemberAddedEvent";
        public const string CUST_EVENT_TYPE_SKTEAM_SHIP_REQUEST = "skTeamShipRequestEvent";
        public const string CUST_EVENT_TYPE_SKTEAM_MEMBER_REMOVED = "skTeamMemberRemovedEvent";
        public const string CUST_EVENT_TYPE_SKTEAM_SHIP_RETIRE_REQUEST = "skTeamShipRetireRequestEvent";
        public const string CUST_EVENT_TYPE_SKTEAM_DEAD = "skTeamDeadEvent";

        public SKTeam(int size)
        {
            mPreGame = true;
            mSize = Math.Max(1, size);
            mPlayerIndexes = new List<PlayerIndex>(mSize);
            mScore = 0;
            mHealth = mHealthMax = 100 + (mSize-1) * 50;
            mBoost = mBoostMax = 10000 + (mSize-1) * 5000;
            mBoostRegen = 5 + (mSize-1) * 2;
            mLadderPosition = -1;
        }

        #region Fields
        private bool mPreGame;
        private int mSize;
        private int mScore;
        private int mHealth;
        private int mHealthMax;
        private int mBoost;
        private int mBoostRegen;
        private int mBoostMax;
        private int mLadderPosition;
        private List<PlayerIndex> mPlayerIndexes;
        #endregion

        #region Properties
        public bool PreGame { get { return mPreGame; } set { mPreGame = value; } }
        public bool IsFull { get { return (mPlayerIndexes != null) ? mPlayerIndexes.Count == mSize : true; } }
        public bool IsAlive { get { return mHealth > 0; } }
        public int GuideMap
        {
            get
            {
                int map = 0;

                if (mPlayerIndexes != null)
                {
                    foreach (PlayerIndex playerIndex in mPlayerIndexes)
                        map |= (1 << (int)playerIndex);
                }

                return map;
            }
        }
        public int NumMembers { get { return (mPlayerIndexes != null) ? mPlayerIndexes.Count : 0; } }
        public int MembersMax { get { return mSize; } }
        public int Score { get { return mScore; } }
        public int Health { get { return mHealth; } }
        public int HealthMax { get { return mHealthMax; } }
        public int Boost { get { return mBoost; } }
        public int BoostMax { get { return mBoostMax; } }
        public int LadderPosition { get { return mLadderPosition; } set { mLadderPosition = value; } }
        public int ScoreMultiplier { get { return 1 + (MembersMax - NumMembers); } }
        public List<PlayerIndex> TeamMembers { get { return mPlayerIndexes; } } // This should be internal within an AwesomePirates.Skirmish namespace.
        #endregion

        #region Methods
        public bool HasTeamMember(PlayerIndex playerIndex)
        {
            //return (mPlayerIndexes != null && mPlayerIndexes.Contains(playerIndex, PlayerIndexComparer.Instance));
            return PlayerIndexComparer.Contains(mPlayerIndexes, playerIndex);
        }

        public int IndexForTeamMember(PlayerIndex playerIndex)
        {
            return (mPlayerIndexes != null) ? mPlayerIndexes.IndexOf(playerIndex) : -1;
        }

        public void AddTeamMember(PlayerIndex playerIndex)
        {
            if (!IsFull && !HasTeamMember(playerIndex))
            {
                mPlayerIndexes.Add(playerIndex);
                DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_SKTEAM_MEMBER_ADDED, playerIndex));
                DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_SKTEAM_SHIP_REQUEST, playerIndex));
            }
        }

        public void RemoveTeamMember(PlayerIndex playerIndex)
        {
            if (mPlayerIndexes != null)
            {
                mPlayerIndexes.Remove(playerIndex);
                DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_SKTEAM_MEMBER_REMOVED, playerIndex));
                DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_SKTEAM_SHIP_RETIRE_REQUEST, playerIndex));
            }
        }

        public void AddScore(int amount)
        {
            if (!PreGame)
                mScore = Math.Max(0, mScore + amount);
        }

        public void AddHealth(int amount)
        {
            if (!PreGame)
            {
                mHealth = Math.Min(mHealthMax, Math.Max(0, mHealth + amount));

                if (mHealth == 0)
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SKTEAM_DEAD));
            }
        }

        public void AddBoost(int amount)
        {
            if (!PreGame)
                mBoost = Math.Min(mBoostMax, Math.Max(0, mBoost + amount));
        }

        public bool SpendBoost()
        {
            if (mBoost > 9)
            {
                AddBoost(-10);
                return true;
            }
            else
                return false;
        }

        public void RegenBoost()
        {
            AddBoost(mBoostRegen);
        }
        #endregion
    }
}
