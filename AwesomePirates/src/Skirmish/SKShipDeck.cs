using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class SKShipDeck : Prop
    {
        private const int kScoreFontSize = 28;

        public SKShipDeck(int category, GameMode mode, SKTeamIndex teamIndex, PlayerIndex playerIndex = PlayerIndex.One)
            : base(category)
        {
            if (mode != GameMode.SKFFA && mode != GameMode.SK2v2)
                throw new ArgumentException("SKShipDeck only supports multiplayer game modes.");

            mTeamIndex = teamIndex;
            mSKPlayerIndex = playerIndex;
            mPreviousScore = 0;
            mMode = mode;
            mAwaitingPlayer = true;
            mPreviousLadderPosition = -1;
            mFrame = null;
            mKeys = new List<PlayerIndex>(2);
            SetupProp();

            SKTeam team = mScene.SKManager.TeamForIndex(TeamIndex);
            if (team != null)
            {
                team.AddEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_MEMBER_ADDED, (PlayerIndexEventHandler)OnTeamMemberAdded);
                team.AddEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_MEMBER_REMOVED, (PlayerIndexEventHandler)OnTeamMemberRemoved);
            }

            mAwaitingPlayer = false;
            AwaitingPlayer = true;
        }

        #region Fields
        private bool mAwaitingPlayer;
        private int mPreviousLadderPosition;
        private int mPreviousScore;
        private GameMode mMode;
        private SKTeamIndex mTeamIndex;
        private PlayerIndex mSKPlayerIndex;
        private SXGauge mBoostGauge;
        private SXGauge mHealthGauge;
        private SPImage mBgTint;
        private SPImage mJoinPrompt;
        private SPImage mTrophy;
        private SPImage mFrame;
        private SPTextField mScoreText;

        private SPImage mAvatarFFA;
        private SPSprite mAvatar2v2;
        private Dictionary<PlayerIndex, SPImage> mAvatars2v2;
        private GuideProp mGuideProp;

        private List<PlayerIndex> mKeys;
        private Dictionary<PlayerIndex, PlayerCannon[]> mCannons;
        private Dictionary<PlayerIndex, Helm> mHelms;
        private Dictionary<PlayerIndex, bool> mBoosts;
        private Dictionary<PlayerIndex, bool> mCombatControlsEnabled;
        #endregion

        #region Properties
        public bool AwaitingPlayer
        {
            get
            {
                return mAwaitingPlayer;
            }
            set
            {
                if (mAwaitingPlayer == value)
                    return;

                if (value)
                {
                    if (mScoreText != null)
                        mScoreText.Visible = false;
                    EnableJoinPromptFlashing(true);
                }
                else
                {
                    if (mScoreText != null)
                        mScoreText.Visible = true;
                    EnableJoinPromptFlashing(false);

                    SKTeam team = mScene.SKManager.TeamForIndex(TeamIndex);
                    if (team != null && team.NumMembers == 0)
                        mScoreText.Text = "";
                }

                mAwaitingPlayer = value;
            }
        }
        public SKTeamIndex TeamIndex { get { return mTeamIndex; } }
        public PlayerIndex SKPlayerIndex { get { return mSKPlayerIndex; } }
        public int PlayerIndexMap { get { return (mGuideProp != null) ? mGuideProp.PlayerIndexMap : (1 << (int)SKPlayerIndex); } }
        public float BoostPercent
        {
            get { return (mBoostGauge != null) ? mBoostGauge.Ratio : 0f; }
            set
            {
                if (mBoostGauge != null)
                    mBoostGauge.Ratio = value;
            }
        }
        public float HealthPercent
        {
            get { return (mHealthGauge != null) ? mHealthGauge.Ratio : 0f; }
            set
            {
                if (mHealthGauge != null)
                {
                    mHealthGauge.Ratio = value;

                    if (value < 0.33f)
                        mHealthGauge.Color = Color.Red;
                    else if (value < 0.66f)
                        mHealthGauge.Color = Color.Yellow;
                    else
                        mHealthGauge.Color = SPUtils.ColorFromColor(0x00ff00);
                }
            }
        }
        public Vector2 DeckLoc { get { return (mFrame != null && mFrame.Parent != null) ? mFrame.BoundsInSpace(mFrame.Parent).Center : Vector2.Zero; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mFrame != null)
                return;

            int posIndex = (int)mSKPlayerIndex;
            mFrame = new SPImage(mScene.TextureByName("sk-hud-frame"));

            mBgTint = new SPImage(mScene.TextureByName(GetBoostTextureName(mMode, TeamIndex)));
            mBgTint.X = GetBoostOffsetX(TeamIndex);
            mBgTint.Color = SKHelper.ColorForTeamIndex(TeamIndex);
            mBgTint.Alpha = 0.2f;
            AddChild(mBgTint);

            mBoostGauge = new SXGauge(mScene.TextureByName(GetBoostTextureName(mMode, TeamIndex)), SXGauge.SXGaugeOrientation.Horizontal);
            mBoostGauge.X = GetBoostOffsetX(TeamIndex);
            mBoostGauge.Color = SKHelper.ColorForTeamIndex(TeamIndex);
            mBoostGauge.Alpha = 0.65f;
            AddChild(mBoostGauge);

            mHealthGauge = new SXGauge(mScene.TextureByName("sk-health-bar"), SXGauge.SXGaugeOrientation.Horizontal);
            mHealthGauge.X = 37f;
            mHealthGauge.Y = 49f;
            mHealthGauge.Color = SPUtils.ColorFromColor(0x00ff00);
            AddChild(mHealthGauge);

            AddChild(mFrame);

            if (mMode == GameMode.SKFFA)
            {
                mAvatarFFA = new SPImage(mScene.TextureByName(SKHelper.AvatarTextureNameForPlayerIndex(SKPlayerIndex)));
                mAvatarFFA.X = -6f;
                mAvatarFFA.Y = 32f;
                AddChild(mAvatarFFA);

                mJoinPrompt = new SPImage(mScene.TextureByName("sk-a-to-join"));
                mJoinPrompt.X = (mFrame.Width - mJoinPrompt.Width) / 2 + GetOverlayOffsetX(mMode, TeamIndex);
                mJoinPrompt.Y = 2f;
                AddChild(mJoinPrompt);
            }
            else
            {
                mAvatars2v2 = new Dictionary<PlayerIndex, SPImage>(2, PlayerIndexComparer.Instance);
                mAvatar2v2 = new SPSprite();
                AddChild(mAvatar2v2);

                mGuideProp = new GuideProp(Category);
                mGuideProp.X = (TeamIndex == SKTeamIndex.Red) ? -100 : 100 + mFrame.Width;
                mGuideProp.Y = 60;
                mGuideProp.ScaleX = mGuideProp.ScaleY = 0.8f;
                mAvatar2v2.AddChild(mGuideProp);

                mJoinPrompt = new SPImage(mScene.TextureByName((TeamIndex == SKTeamIndex.Red) ? "sk-b-to-join" : "sk-x-to-join"));
                mJoinPrompt.X = (mFrame.Width - mJoinPrompt.Width) / 2 + GetOverlayOffsetX(mMode, TeamIndex);
                mJoinPrompt.Y = 2f;
                AddChild(mJoinPrompt);
            }

            mJoinPrompt.Visible = false;

            mTrophy = new SPImage(mScene.TextureByName(SKHelper.TrophyTextureNameForPosition(0)));
            mTrophy.X = 8;
            mTrophy.Y = -16;
            mTrophy.Visible = true;
            AddChild(mTrophy);

            mScoreText = SPTextField.CachedSPTextField(170, 40, "0", "HUDFont", kScoreFontSize);
            mScoreText.X = (mFrame.Width - mScoreText.Width) / 2 + GetOverlayOffsetX(mMode, TeamIndex);
            mScoreText.HAlign = SPTextField.SPHAlign.Right;
            mScoreText.VAlign = SPTextField.SPVAlign.Bottom;
            AddChild(mScoreText);
        }

        private void EnableJoinPromptFlashing(bool enable)
        {
            if (mJoinPrompt == null)
                return;

            mScene.Juggler.RemoveTweensWithTarget(mJoinPrompt);

            if (enable)
            {
                mJoinPrompt.Alpha = 0f;
                mJoinPrompt.Visible = true;

                SPTween tween = new SPTween(mJoinPrompt, 0.65f);
                tween.AnimateProperty("Alpha", 1f);
                tween.Loop = SPLoopType.Reverse;
                mScene.Juggler.AddObject(tween);
            }
            else
                mJoinPrompt.Visible = false;
        }

        public void EnableCombatControls(bool enable)
        {
            if (mKeys != null)
            {
                foreach (PlayerIndex key in mKeys)
                    EnableCombatControls(enable, key);
            }
        }

        public void EnableCombatControls(bool enable, PlayerIndex playerIndex)
        {
            if (mCombatControlsEnabled != null && mCombatControlsEnabled.ContainsKey(playerIndex))
                mCombatControlsEnabled[playerIndex] = enable;
        }

        public PlayerCannon CannonOnSide(ShipDetails.ShipSide side, PlayerIndex playerIndex)
        {
            return (side == ShipDetails.ShipSide.Port) ? LeftCannon(playerIndex) : RightCannon(playerIndex);
        }

        public ShipDetails.ShipSide SideForCannon(PlayerCannon cannon)
        {
            return (cannon.Direction == 1) ? ShipDetails.ShipSide.Starboard : ShipDetails.ShipSide.Port;
        }

        public PlayerCannon RightCannon(PlayerIndex playerIndex)
        {
            if (mCannons != null && mCannons.ContainsKey(playerIndex))
                return mCannons[playerIndex][0];
            else
                return null;
        }

        public PlayerCannon LeftCannon(PlayerIndex playerIndex)
        {
            if (mCannons != null && mCannons.ContainsKey(playerIndex))
                return mCannons[playerIndex][1];
            else
                return null;
        }

        public Helm Helm(PlayerIndex playerIndex)
        {
            if (mHelms != null && mHelms.ContainsKey(playerIndex))
                return mHelms[playerIndex];
            else
                return null;
        }

        public bool IsBoosting(PlayerIndex playerIndex)
        {
            if (mBoosts != null && mBoosts.ContainsKey(playerIndex))
                return mBoosts[playerIndex];
            else
                return false;
        }

        public void SetScore(int score)
        {
            if (mScoreText == null)
                return;

            if (score < 100000000 && mScoreText.FontSize != kScoreFontSize)
                mScoreText.FontSize = kScoreFontSize;
            else if (score > 100000000 && mScoreText.FontSize == kScoreFontSize)
                mScoreText.FontSize = (int)(0.9f * kScoreFontSize);

            Globals.CommaSeparatedValue(score, mScoreText.CachedBuilder);
            mPreviousScore = score;
            mScoreText.ForceCompilation();
        }

        public void SetPlayerDead(PlayerIndex playerIndex)
        {
            if (mMode == GameMode.SKFFA)
            {
                if (mAvatarFFA != null)
                    mAvatarFFA.Texture = mScene.TextureByName("sk-crew-dead");
            }
            else
            {
                if (mAvatars2v2 != null)
                {
                    if (mAvatars2v2.ContainsKey(playerIndex))
                        mAvatars2v2[playerIndex].Texture = mScene.TextureByName("sk-crew-dead");
                }
            }
        }

        public void UpdateTrophy(int pos)
        {
            if (mTrophy != null)
            {
                SPTexture texture = mScene.TextureByName(SKHelper.TrophyTextureNameForPosition(pos));

                if (texture != null)
                    mTrophy.Texture = texture;
            }
        }

        private void EquipPlayer(PlayerIndex playerIndex)
        {
            // Cannons
            if (mCannons == null)
                mCannons = new Dictionary<PlayerIndex, PlayerCannon[]>(2, PlayerIndexComparer.Instance);

            mCannons.Add(playerIndex, new PlayerCannon[2]
            {
                new PlayerCannon(1, false),
                new PlayerCannon(-1, false)
            });

            // Helm
            if (mHelms == null)
                mHelms = new Dictionary<PlayerIndex, Helm>(2, PlayerIndexComparer.Instance);

            mHelms.Add(playerIndex, new Helm(0.05f * SPMacros.PI, false));

            // Boost
            if (mBoosts == null)
                mBoosts = new Dictionary<PlayerIndex, bool>(2, PlayerIndexComparer.Instance);

            mBoosts.Add(playerIndex, false);

            // Controls Enabled
            if (mCombatControlsEnabled == null)
                mCombatControlsEnabled = new Dictionary<PlayerIndex, bool>(2, PlayerIndexComparer.Instance);

            mCombatControlsEnabled.Add(playerIndex, false);
        }

        private void UnequipPlayer(PlayerIndex playerIndex)
        {
            if (mCannons != null && mCannons.ContainsKey(playerIndex))
            {
                PlayerCannon[] playerCannons = mCannons[playerIndex];
                mCannons.Remove(playerIndex);
                foreach (PlayerCannon playerCannon in playerCannons)
                    playerCannon.Dispose();
            }

            if (mHelms != null && mHelms.ContainsKey(playerIndex))
            {
                Helm helm = mHelms[playerIndex];
                mHelms.Remove(playerIndex);
                helm.Dispose();
            }

            if (mBoosts != null)
                mBoosts.Remove(playerIndex);

            if (mCombatControlsEnabled != null)
                mCombatControlsEnabled.Remove(playerIndex);
        }

        private void ActivateTeamMember(PlayerIndex playerIndex)
        {
            if (mKeys == null || PlayerIndexComparer.Contains(mKeys, playerIndex))
                return;

            mKeys.Add(playerIndex);

            if (mMode == GameMode.SKFFA)
            {
                EquipPlayer(playerIndex);
                AwaitingPlayer = false;
            }
            else if (mAvatars2v2 != null)
            {
                EquipPlayer(playerIndex);

                SPImage avatarImage = new SPImage(mScene.TextureByName(SKHelper.AvatarTextureNameForPlayerIndex(playerIndex)));

                if (TeamIndex == SKTeamIndex.Red)
                    avatarImage.X = (mAvatars2v2.Count == 0) ? -72f : -37f;
                else
                    avatarImage.X = mFrame.Width + ((mAvatars2v2.Count == 0) ? 6f : -30f);

                avatarImage.Y = (mAvatars2v2.Count == 0) ? -10f : 38f;
                mAvatar2v2.AddChild(avatarImage);
                mAvatars2v2.Add(playerIndex, avatarImage);

                SKTeam team = mScene.SKManager.TeamForIndex(TeamIndex);
                if (team != null)
                {
                    mGuideProp.PlayerIndexMap = team.GuideMap;
                    AwaitingPlayer = mAvatars2v2.Count < team.MembersMax;
                }
            }
        }

        private void DeactivateTeamMember(PlayerIndex playerIndex)
        {
            if (mKeys == null || !PlayerIndexComparer.Contains(mKeys, playerIndex))
                return;

            if (mMode == GameMode.SKFFA)
            {
                UnequipPlayer(playerIndex);
                AwaitingPlayer = true;
            }
            else if (mAvatars2v2 != null)
            {
                UnequipPlayer(playerIndex);

                if (mAvatars2v2.ContainsKey(playerIndex))
                {
                    SPImage avatarImage = mAvatars2v2[playerIndex];
                    RemoveChild(avatarImage);
                    mAvatars2v2.Remove(playerIndex);
                    avatarImage.Dispose();
                }

                SKTeam team = mScene.SKManager.TeamForIndex(TeamIndex);
                if (team != null)
                {
                    mGuideProp.PlayerIndexMap = team.GuideMap;
                    AwaitingPlayer = mAvatars2v2.Count < team.MembersMax;
                }
            }

            mKeys.Remove(playerIndex);
        }

        private void OnTeamMemberAdded(PlayerIndexEvent ev)
        {
            if (ev != null)
                ActivateTeamMember(ev.PlayerIndex);
        }

        private void OnTeamMemberRemoved(PlayerIndexEvent ev)
        {
            if (ev != null)
                DeactivateTeamMember(ev.PlayerIndex);
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            ControlsManager cm = ControlsManager.CM;
            SKTeam team = mScene.SKManager.TeamForIndex(TeamIndex);

            foreach (PlayerIndex key in mKeys)
            {
                GamePadState gps = cm.GamePadStateForPlayer(key);

                // Helm
                Helm helm = mHelms[key];
                helm.Update(gps);

                if (!mCombatControlsEnabled[key])
                    continue;

                // Cannons
                PlayerCannon[] cannons = mCannons[key];
                foreach (PlayerCannon cannon in cannons)
                    cannon.Update(gps);

                // Boost
                mBoosts[key] = false;
                if (team != null && team.Boost > 9)
                {
                    if (gps.IsButtonDown(Buttons.LeftTrigger) || gps.IsButtonDown(Buttons.RightTrigger))
                        mBoosts[key] = team.SpendBoost();
                }

                if (!mBoosts[key])
                    team.RegenBoost();
            }
        }

        public override void AdvanceTime(double time)
        {
            foreach (PlayerIndex key in mKeys)
            {
                mHelms[key].AdvanceTime(time);

                PlayerCannon[] cannons = mCannons[key];
                foreach (PlayerCannon cannon in cannons)
                    cannon.AdvanceTime(time);
            }

            SKTeam team = mScene.SKManager.TeamForIndex(TeamIndex);

            if (team != null && team.NumMembers > 0)
            {
                if (team.Score != mPreviousScore)
                    SetScore(team.Score);

                BoostPercent = team.Boost / (float)team.BoostMax;
                HealthPercent = team.Health / (float)team.HealthMax;

                if (mTrophy != null)
                {
                    int ladderPos = team.LadderPosition;
                    if (ladderPos != -1)
                    {
                        if (ladderPos != mPreviousLadderPosition)
                        {
                            mTrophy.Texture = mScene.TextureByName(SKHelper.TrophyTextureNameForPosition(ladderPos));
                            mTrophy.Visible = true;
                            mPreviousLadderPosition = ladderPos;
                        }
                    }
                    else
                        mTrophy.Visible = false;
                }
            }
            else
            {
                if (mTrophy != null)
                    mTrophy.Visible = false;
                if (BoostPercent != 0)
                    BoostPercent = 0;
                if (HealthPercent != 0)
                    HealthPercent = 0;
            }
        }

        private static string GetBoostTextureName(GameMode mode, SKTeamIndex teamIndex)
        {
            string textureName = null;

            switch (mode)
            {
                case GameMode.SKFFA:
                    {
                        switch (teamIndex)
                        {
                            case SKTeamIndex.Red:
                                textureName = "sk-boost-left";
                                break;
                            case SKTeamIndex.Blue:
                            case SKTeamIndex.Green:
                                textureName = "sk-boost-mid";
                                break;
                            case SKTeamIndex.Yellow:
                                textureName = "sk-boost-right";
                                break;
                        }
                    }
                    break;
                case GameMode.SK2v2:
                    {
                        switch (teamIndex)
                        {
                            case SKTeamIndex.Red:
                                textureName = "sk-boost-left";
                                break;
                            case SKTeamIndex.Blue:
                                textureName = "sk-boost-right";
                                break;
                        }
                    }
                    break;
            }

            return textureName;
        }

        private static float GetBoostOffsetX(SKTeamIndex teamIndex)
        {
            float offsetX = 0f;

            switch (teamIndex)
            {
                case SKTeamIndex.Red:
                    offsetX = 1f;
                    break;
                case SKTeamIndex.Blue:
                case SKTeamIndex.Green:
                    offsetX = 31f;
                    break;
                case SKTeamIndex.Yellow:
                    offsetX = 31f;
                    break;
            }

            return offsetX;
        }

        private static float GetOverlayOffsetX(GameMode mode, SKTeamIndex teamIndex)
        {
            float offsetX = 0f;

            switch (mode)
            {
                case GameMode.SKFFA:
                    {
                        switch (teamIndex)
                        {
                            case SKTeamIndex.Red:
                                offsetX = -8f;
                                break;
                            case SKTeamIndex.Blue:
                            case SKTeamIndex.Green:
                                offsetX = 0f;
                                break;
                            case SKTeamIndex.Yellow:
                                offsetX = 8f;
                                break;
                        }
                    }
                    break;
                case GameMode.SK2v2:
                    {
                        switch (teamIndex)
                        {
                            case SKTeamIndex.Red:
                                offsetX = -8f;
                                break;
                            case SKTeamIndex.Blue:
                                offsetX = 8f;
                                break;
                        }
                    }
                    break;
            }

            return offsetX;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        SKTeam team = mScene.SKManager.TeamForIndex(TeamIndex);
                        if (team != null)
                        {
                            team.RemoveEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_MEMBER_ADDED, (PlayerIndexEventHandler)OnTeamMemberAdded);
                            team.RemoveEventListener(SKTeam.CUST_EVENT_TYPE_SKTEAM_MEMBER_REMOVED, (PlayerIndexEventHandler)OnTeamMemberRemoved);
                        }

                        if (mScoreText != null)
                        {
                            mScoreText.Dispose();
                            mScoreText = null;
                        }

                        if (mJoinPrompt != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mJoinPrompt);
                            mJoinPrompt = null;
                        }

                        if (mCannons != null)
                        {
                            foreach (KeyValuePair<PlayerIndex, PlayerCannon[]> kvp in mCannons)
                            {
                                foreach (PlayerCannon playerCannon in kvp.Value)
                                    playerCannon.Dispose();
                            }

                            mCannons = null;
                        }

                        if (mHelms != null)
                        {
                            foreach (KeyValuePair<PlayerIndex, Helm> kvp in mHelms)
                                kvp.Value.Dispose();

                            mHelms = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion
    }
}
