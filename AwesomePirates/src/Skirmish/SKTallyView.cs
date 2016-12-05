using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class SKTallyView : Prop
    {
        public const string CUST_EVENT_TYPE_SK_GAME_SUMMARY_RETRY = "skGameSummaryRetryEvent";
        public const string CUST_EVENT_TYPE_SK_GAME_SUMMARY_MENU = "skGameSummaryMenuEvent";

        private enum SequenceState
        {
            None = 0,
            Treasure,
            Cheer
        }

        private static readonly int[] s_CoinCoords = new int[] { 42, 48, 86, 48, 138, 48, 190, 48, 242, 48, 98, 26, 162, 26, 128, 14 };

        public SKTallyView(int category)
            : base(category)
        {
            mAdvanceable = true;
            mDeathSequenceActive = false;
            mSeqState = SequenceState.None;
            mSequenceTimer = 0.0;
            mButtonsSprite = null;
            mCanvas = null;
            SetupProp();
        }

        #region Fields
        private SequenceState mSeqState;
        private double mSequenceTimer;

        private ButtonsProxy mButtonsProxy;
        private MenuButton mRetryButton;
        private MenuButton mMenuButton;
        private SPSprite mButtonsSprite;

        private SPSprite mScrollSprite;
        private SPSprite mTreasureSprite;
        private SPSprite mStampSprite;
        private SPSprite mCanvasContent;
        private SPSprite mCanvas;
        private SPSprite mCanvasScaler;

        private SPImage[] mTitles;
        private SPImage[] mAvatars;
        private SPImage[] mTrophies;
        private ShadowTextField[] mScoreTexts;
        private GuideProp[] mFocusGuides;
        private SPSprite[] mPlaceRows;

        // Gameover sequence
        private bool mDeathSequenceActive;
        private Prop mDeathSkull;
        #endregion

        #region Properties
        private uint InputFocus { get { return InputManager.HAS_FOCUS_SK_GAMEOVER; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvas != null)
                return;

            mCanvasScaler = new SPSprite();
            AddChild(mCanvasScaler);

            mCanvas = new SPSprite();
            mCanvasScaler.AddChild(mCanvas);

            // Scroll
            SPTexture scrollTexture = GuiHelper.CachedScrollTextureByName("scroll-quarter-large", mScene);
            SPImage scrollImage = new SPImage(scrollTexture);
            mScrollSprite = new SPSprite();
            mScrollSprite.AddChild(scrollImage);
            mScrollSprite.ScaleX = mScrollSprite.ScaleY = 600.0f / mScrollSprite.Width;
            mScrollSprite.X = 180;
            mScrollSprite.Y = 64;
            mCanvas.AddChild(mScrollSprite);

            mCanvasContent = new SPSprite();
            mCanvas.AddChild(mCanvasContent);

            mTitles = new SPImage[]
            {
                new SPImage(mScene.TextureByName("sk-text-free-for-all")),
                new SPImage(mScene.TextureByName("sk-text-2v2"))
            };

            foreach (SPImage image in mTitles)
                mCanvas.AddChild(image);

            mAvatars = new SPImage[]
            {
                new SPImage(mScene.TextureByName("sk-crew-0")),
                new SPImage(mScene.TextureByName("sk-crew-1")),
                new SPImage(mScene.TextureByName("sk-crew-2")),
                new SPImage(mScene.TextureByName("sk-crew-3"))
            };

            mTrophies = new SPImage[]
            {
                new SPImage(mScene.TextureByName("sk-trophy-0")),
                new SPImage(mScene.TextureByName("sk-trophy-1")),
                new SPImage(mScene.TextureByName("sk-trophy-2")),
                new SPImage(mScene.TextureByName("sk-trophy-3"))
            };

            mScoreTexts = new ShadowTextField[4];
            for (SKTeamIndex i = SKTeamIndex.Red; i <= SKTeamIndex.Yellow; ++i)
            {
                ShadowTextField textField = new ShadowTextField(Category, 260, 48, 36, "", mScene.FontKey);
                textField.ShadowColor = SPUtils.ColorFromColor(0x505050);
                textField.SetTextAlignment(SPTextField.SPHAlign.Left, SPTextField.SPVAlign.Center);
                mScoreTexts[(int)i] = textField;
                mCanvasContent.AddChild(textField);
            }

            mFocusGuides = new GuideProp[] { new GuideProp(Category), new GuideProp(Category), new GuideProp(Category), new GuideProp(Category) };
            mPlaceRows = new SPSprite[] { new SPSprite(), new SPSprite(), new SPSprite(), new SPSprite() };
            for (int i = 0; i < mPlaceRows.Length; ++i)
            {
                SPSprite placeRow = mPlaceRows[i];
                placeRow.AddChild(mAvatars[i]);
                placeRow.AddChild(mTrophies[i]);
                placeRow.AddChild(mScoreTexts[i]);
                placeRow.AddChild(mFocusGuides[i]);
            }

            foreach (SPSprite sprite in mPlaceRows)
                mCanvasContent.AddChild(sprite);

            mTreasureSprite = CreateTreasure();
            mTreasureSprite.Visible = false;
            mCanvasContent.AddChild(mTreasureSprite);

            mStampSprite = new SPSprite();
            mStampSprite.Visible = false;
            mCanvasContent.AddChild(mStampSprite);

            CreateMenuButtons();

            mDeathSkull = new Prop(Category);
            SPImage skullImage = new SPImage(mScene.TextureByName("sk-skull"));
            skullImage.X = -skullImage.Width / 2;
            skullImage.Y = -skullImage.Height / 2;
            mDeathSkull.AddChild(skullImage);

            mCanvasScaler.X = mScene.ViewWidth / 2 - X;
            mCanvasScaler.Y = mScene.ViewHeight / 2 - Y;
            mCanvas.X = -(mScrollSprite.X + mScrollSprite.Width / 2);
            mCanvas.Y = -(mScrollSprite.Y + mScrollSprite.Height / 2);
            mCanvasScaler.ScaleX = mCanvasScaler.ScaleY = mScene.ScaleForUIView(mScrollSprite, 1f, 0.75f);
            Y = -(mScene.ViewHeight - mCanvasScaler.ScaleY * mScrollSprite.Height) / 4;

            Visible = false;
        }

        public SPSprite CreateTreasure()
        {
            SPSprite treasureSprite = new SPSprite();

            // Single winner
            SPSprite singleWinner = new SPSprite();
            singleWinner.X = 308;
            singleWinner.Y = 258;
            treasureSprite.AddChild(singleWinner);

            SPImage scales = new SPImage(mScene.TextureByName("sk-treasure-left"));
            singleWinner.AddChild(scales);

            SPImage bag = new SPImage(mScene.TextureByName("sk-treasure-right"));
            bag.X = 250;
            bag.Y = 2;
            singleWinner.AddChild(bag);

            SPTexture coinsTexture = mScene.TextureByName("sk-treasure-mid");
            for (int i = 0; i < s_CoinCoords.Length; i+=2)
            {
                SPImage coins = new SPImage(coinsTexture);
                coins.X = s_CoinCoords[i];
                coins.Y = s_CoinCoords[i + 1];
                singleWinner.AddChild(coins);
            }

            // Tied game
            SPSprite tiedGame = new SPSprite();
            tiedGame.X = 220;
            tiedGame.Y = 62;
            treasureSprite.AddChild(tiedGame);

            scales = new SPImage(mScene.TextureByName("sk-treasure-left"));
            tiedGame.AddChild(scales);

            bag = new SPImage(mScene.TextureByName("sk-treasure-right"));
            bag.X = 644 - tiedGame.X;
            bag.Y = 88 - tiedGame.Y;
            tiedGame.AddChild(bag);

            return treasureSprite;
        }

        private void CreateMenuButtons()
        {
            if (mButtonsSprite != null)
                return;

            mButtonsSprite = new SPSprite();
            mCanvas.AddChild(mButtonsSprite);

            mRetryButton = new MenuButton(null, mScene.TextureByName("retry-button"));
            mRetryButton.X = 308;
            mRetryButton.Y = 424;
            mRetryButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnRetryButtonPressed);
            mButtonsSprite.AddChild(mRetryButton);

            mMenuButton = new MenuButton(null, mScene.TextureByName("menu-button"));
            mMenuButton.X = 524;
            mMenuButton.Y = 424;
            mMenuButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnMenuButtonPressed);
            mButtonsSprite.AddChild(mMenuButton);

            mButtonsProxy = new ButtonsProxy(InputFocus, Globals.kNavHorizontal);
            mButtonsProxy.AddButton(mRetryButton);
            mButtonsProxy.AddButton(mMenuButton);
            mScene.SubscribeToInputUpdates(mButtonsProxy);

            mButtonsSprite.Visible = false;
        }

        protected void OnRetryButtonPressed(SPEvent ev)
        {
            mScene.PlaySound("Button");
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_GAME_SUMMARY_RETRY));
        }

        protected void OnMenuButtonPressed(SPEvent ev)
        {
            mScene.PlaySound("Button");
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_SK_GAME_SUMMARY_MENU));
        }

        private void NextState()
        {
            SequenceState state = mSeqState;

            switch (state)
            {
                case SequenceState.None:
                    mSequenceTimer = 1.25;
                    state = SequenceState.Treasure;
                    break;
                case SequenceState.Treasure:
                    mSequenceTimer = 1.5;
                    state = SequenceState.Cheer;
                    mScene.PlaySound("SKTreasure");
                    break;
                case SequenceState.Cheer:
                    mSequenceTimer = 0.0;
                    state = SequenceState.None;
                    mScene.PlaySound("CrowdCheer");
                    RevealMinorPlaces(0.5f, 4f);
                    break;
            }

            mSeqState = state;
        }

        private void UpdateForMode(GameMode mode)
        {
            Debug.Assert(mode != GameMode.Career, "SKTallyView only supports multiplayer modes.");

            PlayerIndex playerIndex = PlayerIndex.One;
            SKTeamIndex teamIndex = SKTeamIndex.Red;
            SKTeam team = null;
            List<PlayerIndex> teamMembers = null;
            List<SKTeam> teamLadder = mScene.SKManager.TeamLadder;

            // Prepare visibilities
            foreach (SPImage image in mTitles)
                image.Visible = false;
            foreach (SPImage image in mAvatars)
                image.Visible = false;
            foreach (SPSprite sprite in mPlaceRows)
            {
                sprite.X = sprite.Y = 0f;
                sprite.Alpha = 1f;
                sprite.Visible = false;
            }

            mTreasureSprite.Visible = false;

            Debug.Assert(teamLadder.Count <= mAvatars.Length, "SKManager's TeamLadder is in an invalid state.");

            // Retrieve from stamp
            for (int i = 0; i < mAvatars.Length; ++i)
                mPlaceRows[i].AddChild(mAvatars[i]);
            foreach (SPSprite sprite in mPlaceRows)
                mCanvasContent.AddChild(sprite);

            int validTeams = 0, prevScore = 0, position = 0;
            if (mode == GameMode.SKFFA)
            {
                // Title
                SPImage title = mTitles[0];
                title.X = 362;
                title.Y = 90;
                title.Visible = true;

                // Canvas Content
                for (int i = 0; i < teamLadder.Count; ++i)
                {
                    if (i >= mAvatars.Length)
                        break;

                    team = teamLadder[i];
                    teamMembers = team.TeamMembers;

                    if (teamMembers == null || teamMembers.Count <= 0)
                        continue;

                    if (team.Score != prevScore)
                        position = validTeams;
                    prevScore = team.Score;

                    playerIndex = teamMembers[0];
                    teamIndex = mScene.SKManager.TeamIndexForIndex(playerIndex);

                    // Avatar
                    SPImage avatar = mAvatars[i];
                    avatar.X = ((validTeams & 1) == 0) ? 228 : 270;
                    avatar.Y = 166 + validTeams * 56;
                    avatar.Texture = mScene.TextureByName(SKHelper.AvatarTextureNameForPlayerIndex(playerIndex));
                    avatar.Visible = true;
                    mPlaceRows[i].AddChild(avatar);

                    // Trophy
                    SPImage trophy = mTrophies[i];
                    trophy.X = 340;
                    trophy.Y = 180 + validTeams * 54;
                    trophy.Texture = mScene.TextureByName(SKHelper.TrophyTextureNameForPosition(position));

                    // Score
                    ShadowTextField textField = mScoreTexts[i];
                    textField.X = trophy.X + 52;
                    textField.Y = trophy.Y + (trophy.Height - textField.Height) / 2 + 2;
                    textField.Text = GuiHelper.CommaSeparatedValue(team.Score);
                    textField.FontColor = SKHelper.ColorForTeamIndex(teamIndex);

                    // Focus Guide
                    GuideProp guide = mFocusGuides[i];
                    guide.X = textField.X + textField.Width + 30f + 24f;
                    guide.Y = trophy.Y + trophy.Height / 2 + 4;
                    guide.ScaleX = guide.ScaleY = 1f;
                    guide.ScaleX = guide.ScaleY = 48f / guide.Width;
                    guide.PlayerIndexMap = (1 << (int)playerIndex);

                    mPlaceRows[i].Visible = true;
                    ++validTeams;
                }
            }
            else
            {
                // Title
                SPImage title = mTitles[1];
                title.X = 434;
                title.Y = 96;
                title.Visible = true;

                // Canvas Content
                for (int i = 0; i < teamLadder.Count; ++i)
                {
                    if (i >= mAvatars.Length)
                        break;

                    int guideMap = 0;
                    team = teamLadder[i];
                    teamMembers = team.TeamMembers;

                    if (teamMembers == null || teamMembers.Count <= 0)
                        continue;

                    if (team.Score != prevScore)
                        position = validTeams;
                    prevScore = team.Score;

                    for (int j = 0; j < teamMembers.Count; ++j)
                    {
                        int avatarIndex = 2 * i + j;
                        if (avatarIndex >= mAvatars.Length)
                            break;

                        playerIndex = teamMembers[j];
                        teamIndex = mScene.SKManager.TeamIndexForIndex(playerIndex);
                        guideMap |= (1 << (int)playerIndex);

                        // Avatar
                        SPImage avatar = mAvatars[avatarIndex];
                        avatar.X = ((j & 1) == 0) ? 208 : 270;
                        avatar.Y = 192 + i * 112;
                        avatar.Texture = mScene.TextureByName(SKHelper.AvatarTextureNameForPlayerIndex(playerIndex));
                        avatar.Visible = true;
                        mPlaceRows[i].AddChild(avatar);
                    }

                    // Trophy
                    SPImage trophy = mTrophies[i];
                    trophy.X = 340;
                    trophy.Y = 206 + i * 110;
                    trophy.Texture = mScene.TextureByName(SKHelper.TrophyTextureNameForPosition(position));

                    // Score
                    ShadowTextField textField = mScoreTexts[i];
                    textField.X = trophy.X + 52;
                    textField.Y = trophy.Y + (trophy.Height - textField.Height) / 2 + 2;
                    textField.Text = GuiHelper.CommaSeparatedValue(team.Score);
                    textField.FontColor = SKHelper.ColorForTeamIndex(teamIndex);

                    // Focus Guide
                    GuideProp guide = mFocusGuides[i];
                    guide.X = textField.X + textField.Width + 20f + 36f;
                    guide.Y = trophy.Y + trophy.Height / 2 + 4;
                    guide.ScaleX = guide.ScaleY = 1f;
                    guide.ScaleX = guide.ScaleY = 72f / guide.Width;
                    guide.PlayerIndexMap = guideMap;

                    mPlaceRows[i].Visible = true;
                    ++validTeams;
                }
            }
        }

        public void Show()
        {
            if (Visible || mCanvas == null)
                return;

            UpdateForMode(mScene.GameMode);
            mButtonsSprite.Visible = false;
            Visible = true;
        }

        public void Hide()
        {
            if (mCanvas == null)
                return;

            CancelAnimations();
            StopGameOverSequence();
            mSeqState = SequenceState.None;
            Visible = false;
        }

        private void CancelAnimations()
        {
            mScene.Juggler.RemoveTweensWithTarget(mStampSprite);
            mScene.Juggler.RemoveTweensWithTarget(mTreasureSprite);
            foreach (SPSprite sprite in mPlaceRows)
            {
                mScene.Juggler.RemoveTweensWithTarget(sprite);
                sprite.Alpha = 0f;
                sprite.Visible = false;
            }
        }

        public float DisplayGameOverSequence()
        {
            if (mDeathSkull == null)
                return 0f;

            StopGameOverSequence();

            mDeathSkull.X = mScene.ViewWidth / 2;
            mDeathSkull.Y = mScene.ViewHeight / 2;
            mDeathSkull.Alpha = 1f;
            mDeathSkull.ScaleX = mDeathSkull.ScaleY = 0.01f;

            SPTween scaleTween = new SPTween(mDeathSkull, 2f, SPTransitions.SPEaseIn);
            scaleTween.AnimateProperty("ScaleX", 1.75f);
            scaleTween.AnimateProperty("ScaleY", 1.75f);
            mScene.Juggler.AddObject(scaleTween);

            SPTween alphaTween = new SPTween(mDeathSkull, 1f);
            alphaTween.AnimateProperty("Alpha", 0f);
            alphaTween.Delay = scaleTween.TotalTime;
            alphaTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnGameOverSequenceCompleted);
            mScene.Juggler.AddObject(alphaTween);

            mScene.AddProp(mDeathSkull);
            mScene.PlaySound("Death"); // Sound plays for ~4s
            mDeathSequenceActive = true;
            return 0.9f * (float)(scaleTween.TotalTime + alphaTween.TotalTime);
        }

        private void StopGameOverSequence()
        {
            if (mDeathSequenceActive)
            {
                mScene.Juggler.RemoveTweensWithTarget(mDeathSkull);
                mScene.RemoveProp(mDeathSkull, false);
                mDeathSequenceActive = false;
            }
        }

        private void OnGameOverSequenceCompleted(SPEvent ev)
        {
            mScene.RemoveProp(mDeathSkull, false);
            mDeathSequenceActive = false;
        }

        public void DisplayGameOverScroll()
        {
            if (Visible || mCanvas == null)
                return;

            CancelAnimations();
            mButtonsProxy.ResetNav();
            mButtonsSprite.Visible = true;

            GameMode mode = mScene.GameMode;
            UpdateForMode(mode);

            foreach (SPSprite sprite in mPlaceRows)
            {
                sprite.Alpha = 0f;
                sprite.Visible = false;
            }

            mStampSprite.ScaleX = mStampSprite.ScaleY = 1f;

            List<SKTeam> teamLadder = mScene.SKManager.TeamLadder;
            Vector2 stampOrigin = mScoreTexts[0].BoundsInSpace(mCanvasContent).Center;
            int numWinners = 0, winningScore = -1;
            int maxPlace = mScene.SKManager.NumParticipatingTeams;
            int ladderCount = Math.Min(maxPlace, teamLadder.Count);

            if (mode == GameMode.SKFFA)
            {
                Debug.Assert(ladderCount <= 4, "SKManager's TeamLadder is in an invalid state.");
                ladderCount = Math.Min(ladderCount, 4);

                for (int i = 0; i < ladderCount; ++i)
                {
                    if (winningScore != -1 && teamLadder[i].Score != winningScore)
                        break;
                    winningScore = teamLadder[i].Score;
                    mStampSprite.AddChild(mPlaceRows[i]);
                    mPlaceRows[i].Alpha = 1f;
                    mPlaceRows[i].Visible = true;
                    if (i > 0) stampOrigin.Y += 28f;
                    ++numWinners;
                }
            }
            else
            {
                Debug.Assert(ladderCount <= 2, "SKManager's TeamLadder is in an invalid state.");
                ladderCount = Math.Min(ladderCount, 2);

                for (int i = 0; i < ladderCount; ++i)
                {
                    if (winningScore != -1 && teamLadder[i].Score != winningScore)
                        break;
                    winningScore = teamLadder[i].Score;
                    mStampSprite.AddChild(mAvatars[2*i]);
                    mStampSprite.AddChild(mAvatars[2*i+1]);
                    mStampSprite.AddChild(mPlaceRows[i]);
                    mPlaceRows[i].Alpha = 1f;
                    mPlaceRows[i].Visible = true;
                    if (i > 0) stampOrigin.Y += 56f;
                    ++numWinners;
                }
            }

            mStampSprite.Origin = stampOrigin;
            mStampSprite.Visible = false;

            int numStampChildren = mStampSprite.NumChildren;
            for (int i = 0; i < numStampChildren; ++i)
            {
                SPDisplayObject stampChild = mStampSprite.ChildAtIndex(i);
                stampChild.X -= stampOrigin.X;
                stampChild.Y -= stampOrigin.Y;
            }

            // Treasure
            mTreasureSprite.Y = 0;
            mTreasureSprite.Alpha = 1f;
            mTreasureSprite.Visible = true;

            if (numWinners == 1)
            {
                if (mode == GameMode.SK2v2)
                    mTreasureSprite.Y = 22;

                mTreasureSprite.ChildAtIndex(0).Visible = true;
                mTreasureSprite.ChildAtIndex(1).Visible = false;
            }
            else
            {
                mTreasureSprite.ChildAtIndex(0).Visible = false;
                mTreasureSprite.ChildAtIndex(1).Visible = true;
            }

            StampAnimationWithStamp(mStampSprite, 0.1f, 0.5f);
            mScene.PushFocusState(InputManager.FOCUS_STATE_PF_SK_GAMEOVER);
            Visible = true;
        }

        protected void StampAnimationWithStamp(SPSprite stamp, float duration, float delay)
        {
            float oldScaleX = stamp.ScaleX, oldScaleY = stamp.ScaleY;

            stamp.ScaleX = 3.0f;
            stamp.ScaleY = 3.0f;

            mScene.Juggler.RemoveTweensWithTarget(stamp);

            SPTween tween = new SPTween(stamp, duration);
            tween.AnimateProperty("ScaleX", oldScaleX);
            tween.AnimateProperty("ScaleY", oldScaleY);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_STARTED, (SPEventHandler)OnStamping);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnStamped);
            tween.Delay = delay;
            mScene.Juggler.AddObject(tween);
        }

        protected void ShakeCanvas(SPSprite canvas)
        {
            mScene.Juggler.RemoveTweensWithTarget(canvas);

            float delay = 0.0f;
            float xTarget, yTarget;
            float xAccum = 0, yAccum = 0;
            GameController gc = GameController.GC;

            for (int i = 0; i < 6; ++i)
            {
                if (i < 5)
                {
                    xTarget = gc.NextRandom(-40, 40);
                    yTarget = gc.NextRandom(-40, 40);

                    xAccum += xTarget;
                    yAccum += yTarget;

                    // Don't let it shake too far from center
                    if (Math.Abs(xAccum) > 60)
                    {
                        xTarget = -xTarget;
                        xAccum += xTarget;
                    }

                    if (Math.Abs(yAccum) > 60)
                    {
                        yTarget = -yTarget;
                        yAccum += yTarget;
                    }
                }
                else
                {
                    // Move it back to original position
                    xTarget = 0;
                    yTarget = 0;
                }

                SPTween tween = new SPTween(canvas, 0.05f);
                tween.AnimateProperty("X", canvas.X + xTarget);
                tween.AnimateProperty("Y", canvas.Y + yTarget);
                tween.Delay = delay;
                delay += (float)tween.TotalTime;
                mScene.Juggler.AddObject(tween);
            }
        }

        protected virtual void OnStamping(SPEvent ev)
        {
            SPTween tween = ev.CurrentTarget as SPTween;

            if (tween != null)
            {
                SPDisplayObject target = tween.Target as SPDisplayObject;

                if (target != null)
                {
                    target.Visible = true;
                    mScene.PlaySound("Stamp");
                }
            }
        }

        private void OnStamped(SPEvent ev)
        {
            ShakeCanvas(mCanvas);
            NextState();
        }

        private void RevealMinorPlaces(float duration, float delay)
        {
            GameMode mode = mScene.GameMode;
            SPTween tween = new SPTween(mTreasureSprite, duration);
            tween.AnimateProperty("Alpha", 0f);
            tween.Delay = delay;
            mScene.Juggler.AddObject(tween);

            // Calc num winners
            List<SKTeam> teamLadder = mScene.SKManager.TeamLadder;
            int numWinners = 0, winningScore = -1;
            foreach (SKTeam team in teamLadder)
            {
                if (winningScore != -1 && team.Score != winningScore)
                    break;
                winningScore = team.Score;
                ++numWinners;
            }

            // Prepare max place
            int maxPlace = mScene.SKManager.NumParticipatingTeams;
            float totalDelay = delay + duration / 2;

            Debug.Assert((mode == GameMode.SKFFA && maxPlace <= 4) || (mode == GameMode.SK2v2 && maxPlace <= 2), "SKManager's TeamLadder is in an invalid state.");
            if (mode == GameMode.SKFFA && maxPlace > 4)
                maxPlace = 4;
            else if (mode == GameMode.SK2v2 && maxPlace > 2)
                maxPlace = 2;

            // Reveal non-winners
            for (int i = numWinners; i < maxPlace; ++i)
            {
                mPlaceRows[i].Visible = true;
                tween = new SPTween(mPlaceRows[i], duration);
                tween.AnimateProperty("Alpha", 1f);
                tween.Delay = totalDelay;
                mScene.Juggler.AddObject(tween);
                totalDelay += duration / 2;
            }
        }

        public override void AdvanceTime(double time)
        {
            if (mSeqState == SequenceState.None)
                return;

            mSequenceTimer -= time;

            if (mSequenceTimer <= 0)
                NextState();
        }

        public void AddToView(SPDisplayObject displayObject, float xPercent, float yPercent)
        {
            if (displayObject != null && mScrollSprite != null && mCanvas != null)
            {
                displayObject.X = mScrollSprite.X + mScrollSprite.Width * xPercent;
                displayObject.Y = mScrollSprite.Y + mScrollSprite.Height * yPercent;
                mCanvas.AddChild(displayObject);
            }
        }

        public void RemoveFromView(SPDisplayObject displayObject)
        {
            if (displayObject != null && mCanvas != null)
                mCanvas.RemoveChild(displayObject);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mStampSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mStampSprite);
                            mStampSprite = null;
                        }

                        if (mTreasureSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mTreasureSprite);
                            mTreasureSprite = null;
                        }

                        if (mPlaceRows != null)
                        {
                            foreach (SPSprite sprite in mPlaceRows)
                                mScene.Juggler.RemoveTweensWithTarget(sprite);
                            mPlaceRows = null;
                        }

                        if (mFocusGuides != null)
                        {
                            foreach (GuideProp guide in mFocusGuides)
                                guide.Dispose();
                            mFocusGuides = null;
                        }

                        if (mButtonsProxy != null)
                        {
                            mScene.UnsubscribeToInputUpdates(mButtonsProxy);
                            mButtonsProxy = null;
                        }

                        if (mRetryButton != null)
                        {
                            mRetryButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnRetryButtonPressed);
                            mRetryButton.Dispose();
                            mRetryButton = null;
                        }

                        if (mMenuButton != null)
                        {
                            mMenuButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnMenuButtonPressed);
                            mMenuButton.Dispose();
                            mMenuButton = null;
                        }

                        if (mDeathSkull != null)
                        {
                            if (mDeathSequenceActive)
                            {
                                mScene.Juggler.RemoveTweensWithTarget(mDeathSkull);
                                mScene.RemoveProp(mDeathSkull);
                                mDeathSequenceActive = false;
                            }
                            else
                                mDeathSkull.Dispose();

                            mDeathSkull = null;
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
