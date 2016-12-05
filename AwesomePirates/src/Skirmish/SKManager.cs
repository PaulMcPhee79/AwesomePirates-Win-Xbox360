using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class SKManager : SPEventDispatcher
    {
        public enum SKGameCountdown
        {
            None = 0,
            PreGame,
            Finale
        }

        public const string CUST_EVENT_TYPE_SK_PRE_GAME_COUNTDOWN_STARTED = "skPreGameCoundownStartedEvent";
        public const string CUST_EVENT_TYPE_SK_PRE_GAME_COUNTDOWN_COMPLETED = "skPreGameCoundownCompletedEvent";
        public const string CUST_EVENT_TYPE_SK_FINALE_COUNTDOWN_STARTED = "skFinaleCoundownStartedEvent";
        public const string CUST_EVENT_TYPE_SK_FINALE_COUNTDOWN_COMPLETED = "skFinaleCoundownCompletedEvent";
        public const string CUST_EVENT_TYPE_SK_PLAYER_SHIP_SINKING = "skPlayerShipSinkingEvent";
        public const string CUST_EVENT_TYPE_SK_GAME_OVER = "skGameOverEvent";
        public const int kSKScoreMultiplier = 25;

        public SKManager()
        {
            mTeamsDidChange = true;
            mCountdownBegan = false;
            mGameCountdown = SKGameCountdown.None;
            mGameCountdownTimer = 0;
            mMode = GameMode.SKFFA;
            mPlayers = new List<PlayerIndex>(4);
            mTeams = new List<SKTeam>(4);
            mTeamLadder = new List<SKTeam>(4);
            mChallenger = null;
            mCombatText = null;
        }

        #region Fields
        private bool mTeamsDidChange;
        private bool mCountdownBegan;
        private SKGameCountdown mGameCountdown;
        private double mGameCountdownTimer;
        private PlayerIndex mCachedIndex; // Cached by event dispatchers to allow easier retro-fitting of multiplayer code.
        private GameMode mMode;
        private List<PlayerIndex> mPlayers;
        private List<SKTeam> mTeams;
        private List<SKTeam> mTeamLadder;
        private SKTeam mChallenger;
        private SKCombatText mCombatText;
        private PlayerIndexEvent mTeamDiedEvent;
        #endregion

        #region Properties
        public PlayerIndex CachedIndex { get { return mCachedIndex; } set { mCachedIndex = value; } }
        public SKGameCountdown GameCountdown
        {
            get { return mGameCountdown; }
            set
            {
                switch (value)
                {
                    case SKGameCountdown.None:
                        mGameCountdownTimer = 0.0;
                        break;
                    case SKGameCountdown.PreGame:
                        foreach (SKTeam team in mTeams)
                            team.PreGame = true;
                        mGameCountdownTimer = 5.0;
                        break;
                    case SKGameCountdown.Finale:
                        mGameCountdownTimer = 30.0;
                        break;
                }

                mCountdownBegan = false;
                mGameCountdown = value;
            }
        }
        public int GameCountdownValue { get { return (int)Math.Max(0, Math.Ceiling(mGameCountdownTimer)); } }
        public int NumTeams { get { return mTeams.Count; } }
        public int NumPlayers
        {
            get
            {
                int num = 0;

                foreach (SKTeam team in mTeams)
                    num += team.NumMembers;

                return num;
            }
        }
        public int NumParticipatingTeams
        {
            get
            {
                int num = 0;

                foreach (SKTeam team in mTeams)
                {
                    if (team.NumMembers > 0)
                        ++num;
                }

                return num;
            }
        }
        public int NumAliveTeams
        {
            get
            {
                int num = 0;

                foreach (SKTeam team in mTeams)
                {
                    if (team.NumMembers > 0 && team.Health > 0)
                        ++num;
                }

                return num;
            }
        }
        private SKTeam FirstLivingTeam
        {
            get
            {
                SKTeam livingTeam = null;
                foreach (SKTeam team in mTeams)
                {
                    if (team.NumMembers > 0 && team.Health > 0)
                    {
                        livingTeam = team;
                        break;
                    }
                }

                return livingTeam;
            }
        }
        // Don't like these, but other options create too much garbage, so clients should
        // use it carefully (i.e. don't modify it nor call SKManager functions that may
        // modify it either directly or indirectly through events).
        public List<SKTeam> Teams { get { return mTeams; } }
        public List<SKTeam> TeamLadder { get { return mTeamLadder; } }
        public List<PlayerIndex> Players
        {
            get
            {
                if (!mTeamsDidChange)
                    return mPlayers;

                mPlayers.Clear();

                if (mTeams != null)
                {

                    foreach (SKTeam team in mTeams)
                    {
                        List<PlayerIndex> teamMembers = team.TeamMembers;

                        if (teamMembers != null)
                        {
                            foreach (PlayerIndex playerIndex in teamMembers)
                                mPlayers.Add(playerIndex);
                        }
                    }
                }

                mTeamsDidChange = false;
                return mPlayers;
            }
        }
        #endregion

        #region Methods
        public void BeginGame(GameMode mode)
        {
            Debug.Assert(mode == GameMode.SKFFA || mode == GameMode.SK2v2, "SKManager only supports multiplayer game modes.");

            mMode = mode;
            mChallenger = null;

            foreach (SKTeam team in mTeams)
            {
                team.RemoveEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_SHIP_REQUEST, (PlayerIndexEventHandler)OnShipRequested);
                team.RemoveEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_SHIP_RETIRE_REQUEST, (PlayerIndexEventHandler)OnShipRetireRequested);
                team.RemoveEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_DEAD);
            }

            mTeams.Clear();
            mTeamLadder.Clear();
            mTeamsDidChange = true;

            switch (mode)
            {
                case GameMode.SKFFA:
                    {
                        for (PlayerIndex i = PlayerIndex.One; i <= PlayerIndex.Four; ++i)
                        {
                            SKTeam team = new SKTeam(1);
                            team.AddEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_SHIP_REQUEST, (PlayerIndexEventHandler)OnShipRequested);
                            team.AddEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_SHIP_RETIRE_REQUEST, (PlayerIndexEventHandler)OnShipRetireRequested);
                            team.AddActionEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_DEAD, new Action<SPEvent>(OnTeamDied));
                            mTeams.Add(team);
                            mTeamLadder.Add(team);
                        }
                    }
                    break;
                case GameMode.SK2v2:
                    {
                        for (PlayerIndex i = PlayerIndex.One; i <= PlayerIndex.Two; ++i)
                        {
                            SKTeam team = new SKTeam(2);
                            team.AddEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_SHIP_REQUEST, (PlayerIndexEventHandler)OnShipRequested);
                            team.AddEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_SHIP_RETIRE_REQUEST, (PlayerIndexEventHandler)OnShipRetireRequested);
                            team.AddActionEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_DEAD, new Action<SPEvent>(OnTeamDied));
                            mTeams.Add(team);
                            mTeamLadder.Add(team);
                        }
                    }
                    break;
            }
        }

        public SKTeam TeamForIndex(SKTeamIndex teamIndex)
        {
            return ((int)teamIndex < NumTeams) ? mTeams[(int)teamIndex] : null;
        }

        public SKTeamIndex TeamIndexForIndex(PlayerIndex playerIndex)
        {
            SKTeamIndex teamIndex = SKTeamIndex.Red;

            switch (mMode)
            {
                case GameMode.SKFFA:
                    if (mTeams[(int)playerIndex].HasTeamMember(playerIndex))
                        teamIndex = (SKTeamIndex)playerIndex;
                    break;
                case GameMode.SK2v2:
                    for (int i = 0; i < mTeams.Count; ++i)
                    {
                        if (mTeams[i].HasTeamMember(playerIndex))
                        {
                            teamIndex = (SKTeamIndex)i;
                            break;
                        }
                    }
                    break;
            }

            return teamIndex;
        }

        public SKTeam TeamForIndex(PlayerIndex playerIndex)
        {
            SKTeam team = null;

            switch (mMode)
            {
                case GameMode.SKFFA:
                    if (mTeams[(int)playerIndex].HasTeamMember(playerIndex))
                        team = mTeams[(int)playerIndex];
                    break;
                case GameMode.SK2v2:
                    foreach (SKTeam skTeam in mTeams)
                    {
                        if (skTeam.HasTeamMember(playerIndex))
                        {
                            team = skTeam;
                            break;
                        }
                    }
                    break;
            }

            return team;
        }

        public void AddToTeam(SKTeamIndex teamIndex, PlayerIndex playerIndex)
        {
            if ((int)teamIndex >= NumTeams)
                return;

            if (TeamForIndex(playerIndex) == null)
            {
                mTeams[(int)teamIndex].AddTeamMember(playerIndex);
                mTeamsDidChange = true;
                GameController.GC.ProfileManager.UpdatePresenceModeForPlayer(GamerPresenceMode.Multiplayer, playerIndex);
            }
        }

        public void RemoveFromTeam(PlayerIndex playerIndex)
        {
            SKTeam team = TeamForIndex(playerIndex);

            if (team != null)
            {
                team.RemoveTeamMember(playerIndex);
                mTeamsDidChange = true;
            }
        }

        private void OnShipRequested(PlayerIndexEvent ev)
        {
            if (ev != null)
                DispatchEvent(ev);
        }

        private void OnTeamDied(SPEvent ev)
        {
            if (ev != null)
            {
                if (ev.Target != null && ev.Target is SKTeam)
                {
                    SKTeam team = ev.Target as SKTeam;
                    List<PlayerIndex> teamMembers = team.TeamMembers;

                    if (teamMembers != null)
                    {
                        foreach (PlayerIndex teamMember in teamMembers)
                        {
                            if (mTeamDiedEvent == null)
                                mTeamDiedEvent = new PlayerIndexEvent(CUST_EVENT_TYPE_SK_PLAYER_SHIP_SINKING, teamMember);
                            else
                                mTeamDiedEvent.ReuseWithIndex(teamMember);
                            DispatchEvent(mTeamDiedEvent);
                        }
                    }
                }
            }

            int numAliveTeams = NumAliveTeams;

            if (numAliveTeams == 0)
            {
                mChallenger = null;
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_GAME_OVER));
            }
            else if (numAliveTeams == 1)
            {
                Debug.Assert(mChallenger == null, "SKChallenger should not be set at this stage of the game.");

                List<SKTeam> ladder = TeamLadder;
                SKTeam livingTeam = FirstLivingTeam;
                if (livingTeam == null || ladder.Count <= 1 || (livingTeam == ladder[0] && ladder[0].Score > ladder[1].Score))
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_GAME_OVER));
                else
                    mChallenger = livingTeam;
            }
        }

        private void OnShipRetireRequested(PlayerIndexEvent ev)
        {
            if (ev != null)
                DispatchEvent(ev);
        }

        public void EnemyShipSunk(ShipActor ship, SKTeamIndex teamIndex)
        {
            if (GameController.GC.ThisTurn.IsGameOver || ship.IsPreparingForNewGame)
                return;

            SKTeam scorer = TeamForIndex(teamIndex);
            if (scorer != null && scorer.IsAlive)
            {
                int infamyBonus;

                if (ship.DeathBitmap == DeathBitmaps.PLAYER_CANNON)
                    infamyBonus = (int)(Globals.CRIT_FACTOR * kSKScoreMultiplier * ship.SunkByPlayerCannonInfamyBonus * scorer.ScoreMultiplier); // we do only crits in multiplayer modes.
                else
                    infamyBonus = (int)(Globals.CRIT_FACTOR * kSKScoreMultiplier * ship.InfamyBonus);

                scorer.AddScore(infamyBonus);
                DisplayInfamyBonus(infamyBonus, ship.CenterX, ship.CenterY, CombatText.CTColorType.RedTeam + mTeams.IndexOf(scorer));
                UpdateLadderPositions(scorer);
            }
        }

        public void PrisonerKilled(OverboardActor prisoner, SKTeamIndex teamIndex)
        {
            if (GameController.GC.ThisTurn.IsGameOver || prisoner.IsPreparingForNewGame)
                return;

            SKTeam scorer = TeamForIndex(teamIndex);
            if (scorer != null && scorer.IsAlive)
            {
                int infamyBonus = (int)(Globals.CRIT_FACTOR * kSKScoreMultiplier * prisoner.InfamyBonus * scorer.ScoreMultiplier); // we do only crits in multiplayer modes.
                scorer.AddScore(infamyBonus);
                DisplayInfamyBonus(infamyBonus, prisoner.X, prisoner.Y, CombatText.CTColorType.RedTeam + mTeams.IndexOf(scorer));
                UpdateLadderPositions(scorer);
            }
        }

        public void DisplayInfamyBonus(int bonus, float x, float y, CombatText.CTColorType colorType)
        {
            mCombatText.DisplayCombatText(bonus, x, y, true, false, colorType);
        }

        public void LoadCombatTextWithCategory(int category, int bufferSize)
        {
            if (mCombatText == null)
                mCombatText = new SKCombatText(category, bufferSize);
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

        private void UpdateLadderPositions(SKTeam scorer)
        {
            if (scorer == null)
                return;

            SKTeam team = null;
            for (int i = 0; i < mTeamLadder.Count; ++i)
            {
                team = mTeamLadder[i];
                
                if (scorer == team)
                    break;

                if (scorer.Score > team.Score)
                {
                    mTeamLadder.Remove(scorer);
                    mTeamLadder.Insert(i, scorer);
                    break;
                }
            }

            int pos = 0, prevScore = -1;
            for (int i = 0; i < mTeamLadder.Count; ++i)
            {
                team = mTeamLadder[i];

                if (prevScore == -1 || team.Score == prevScore)
                    team.LadderPosition = pos;
                else
                    team.LadderPosition = ++pos;

                prevScore = team.Score;
            }

            if (mChallenger != null)
            {
                if (mTeamLadder.Count <= 1 || (mChallenger == mTeamLadder[0] && mTeamLadder[0].Score > mTeamLadder[1].Score))
                {
                    mChallenger = null;
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_GAME_OVER));
                }
            }
        }

        private void PreGameCountdownCompleted()
        {
            foreach (SKTeam team in mTeams)
                team.PreGame = false;
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_PRE_GAME_COUNTDOWN_COMPLETED));
        }

        private void FinaleCountdownCompleted()
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_FINALE_COUNTDOWN_COMPLETED));
        }

        public void AdvanceTime(double time)
        {
            FillCombatTextCache();

            if (mGameCountdown == SKGameCountdown.PreGame)
            {
                if (mGameCountdownTimer > 0)
                {
                    // Don't progress the countdown until bare minimum of players have joined.
                    if (mMode == GameMode.SKFFA && NumPlayers < 2)
                        return;
                    else if (mMode == GameMode.SK2v2)
                    {
                        foreach (SKTeam team in mTeams)
                        {
                            if (team.NumMembers == 0)
                                return;
                        }
                    }

                    if (!mCountdownBegan)
                    {
                        DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_PRE_GAME_COUNTDOWN_STARTED));
                        mCountdownBegan = true;
                    }

                    mGameCountdownTimer -= time;

                    if (mGameCountdownTimer <= 0)
                    {
                        mGameCountdownTimer = 0.0;
                        PreGameCountdownCompleted();
                    }
                }
                else
                    GameCountdown = SKGameCountdown.None;
            }
            else if (mGameCountdown == SKGameCountdown.Finale)
            {
                if (mGameCountdownTimer > 0)
                {
                    if (!mCountdownBegan)
                    {
                        DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_FINALE_COUNTDOWN_STARTED));
                        mCountdownBegan = true;
                    }

                    mGameCountdownTimer -= time;

                    if (mGameCountdownTimer <= 0)
                    {
                        mGameCountdownTimer = 0.0;
                        FinaleCountdownCompleted();
                    }
                }
                else
                    GameCountdown = SKGameCountdown.None;
            }
        }
        #endregion
    }
}
