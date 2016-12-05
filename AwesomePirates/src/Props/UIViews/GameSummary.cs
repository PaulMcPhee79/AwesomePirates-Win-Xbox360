using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class GameSummary : Prop
    {
        public const string CUST_EVENT_TYPE_GAME_SUMMARY_RETRY = "gameSummaryRetryEvent";
        public const string CUST_EVENT_TYPE_GAME_SUMMARY_MENU = "gameSummaryMenuEvent";

        public GameSummary(int category, int masteryLevel, float levelXPFraction)
            : base(category)
        {
            Touchable = true;
            mMasteryLevel = masteryLevel;
            mLevelXPFraction = levelXPFraction;
            mBestSprite = null;
            mRankSprites = new List<SPSprite>(3);
            mXPText = null;
            mXPPanel = null;
            mButtons = null;
            mButtonsProxy = null;
            SetupProp();
        }

        #region Fields
        protected ButtonsProxy mButtonsProxy;
        protected MenuButton mRetryButton;
        protected MenuButton mMenuButton;
        protected SPSprite mScrollSprite;
        protected SPSprite mCanvasSprite;
        protected SPSprite mCanvasScaler;

        protected Dictionary<string, SPButton> mButtons;

        private SPTextField mScoreText;
        private SPTextField mAccuracyText;
        private SPTextField mPlankingsText;
        private SPTextField mDaysAtSeaText;

        private SPSprite mBestSprite;
        private List<SPSprite> mRankSprites;
        private SPSprite mScoreSprite;
        private SPSprite mStatsSprite;
        private SPSprite mDeathSprite;

        private int mMasteryLevel;
        private float mLevelXPFraction;
        private SPTextField mXPText;
        private MasteryXPPanel mXPPanel;
        #endregion

        #region Properties
        protected virtual uint InputFocus { get { return InputManager.HAS_FOCUS_GAMEOVER; } }
        public float StampsDelay
        {
            get
            {
                float delay = 0;

                if (mScoreSprite != null) delay += 1.0f;
                if (mStatsSprite != null) delay += 1.0f;
                if (mBestSprite != null) delay += 1.0f;

                return delay;
            }
        }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            GameController gc = GameController.GC;

            //ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);

            mCanvasScaler = new SPSprite();
            mCanvasScaler.X = mScene.ViewWidth / 2 - X;
            mCanvasScaler.Y = mScene.ViewHeight / 2 - Y;
            AddChild(mCanvasScaler);

            mCanvasSprite = new SPSprite();
            mCanvasScaler.AddChild(mCanvasSprite);

            // Background scroll
            SPTexture scrollTexture = GuiHelper.CachedScrollTextureByName("scroll-quarter-large", mScene);
            SPImage scrollImage = new SPImage(scrollTexture);
            mScrollSprite = new SPSprite();
            mScrollSprite.AddChild(scrollImage);

            mScrollSprite.ScaleX = mScrollSprite.ScaleY = 640.0f / mScrollSprite.Width;
            mScrollSprite.X = ResManager.RESX(180);
            mScrollSprite.Y = ResManager.RESY(64);
            mCanvasSprite.AddChild(mScrollSprite);
    
            // Shady
            SPSprite shadySprite = new SPSprite();
            shadySprite.X = ResManager.RESX(232);
            shadySprite.Y = ResManager.RESY(200);
    
            SPImage shadyImage = new SPImage(mScene.TextureByName("shady-end-of-turn"));
            shadySprite.AddChild(shadyImage);
            mCanvasSprite.AddChild(shadySprite);
    
            // Buttons
            AddMenuButtons();
    
            // Score
            SPSprite scoreSprite = new SPSprite();
            scoreSprite.Touchable = false;
            SPTextField youScoredText = new SPTextField(140, 64, "Score  ", mScene.FontKey, 48);
            youScoredText.X = 48;
            youScoredText.HAlign = SPTextField.SPHAlign.Left;
            youScoredText.VAlign = SPTextField.SPVAlign.Top;
            youScoredText.Color = Color.Black;
            scoreSprite.AddChild(youScoredText);
  
            int score = gc.ThisTurn.Infamy;
            string scoreString = GuiHelper.CommaSeparatedValue(score);

            mScoreText = new SPTextField(300, 56, scoreString, mScene.FontKey, 40);
            mScoreText.HAlign = SPTextField.SPHAlign.Left;
            mScoreText.VAlign = SPTextField.SPVAlign.Top;
            mScoreText.X = youScoredText.X + youScoredText.Width;
            mScoreText.Y = youScoredText.Y + 14;
            mScoreText.Color = Color.Black;
            scoreSprite.AddChild(mScoreText);

            scoreSprite.X = -scoreSprite.Width / 2;
            scoreSprite.Y = -scoreSprite.Height / 2;
    
            mScoreSprite = new SPSprite();
            mScoreSprite.X = mScrollSprite.X + mScrollSprite.Width / 2; //mScene.ViewWidth / 2;
            mScoreSprite.Y = ResManager.RESY(174 + scoreSprite.Height / 2);
            mScoreSprite.Visible = false;
            mScoreSprite.AddChild(scoreSprite);

            // Rope
            float scoreTextWidth = youScoredText.TextBounds.Width + Math.Max(0.8f * mScoreText.Width, mScoreText.TextBounds.Width) - 20;
            SPImage ropeImage = new SPImage(mScene.TextureByName("horiz-rope"));
            ropeImage.ScaleX = scoreTextWidth / ropeImage.Width;
            ropeImage.ScaleY = 0.9f;
            ropeImage.X = mScoreSprite.X + scoreSprite.X + youScoredText.X + 10;
            ropeImage.Y = mScoreSprite.Y + scoreSprite.Y + mScoreText.Y + mScoreText.Height;
            mCanvasSprite.AddChild(ropeImage);

            mCanvasSprite.AddChild(mScoreSprite); // Add after rope so that it appears above it when stamped.

            // Rank
            int[] ranks = new int[3]
            {
                gc.HiScores.RankForScore(score),
                gc.LiveLeaderboard != null ? gc.LiveLeaderboard.FriendsRankForGamer(gc.ProfileManager.SigGamer, score) : -1,
                gc.LiveLeaderboard != null ? gc.LiveLeaderboard.GlobalRankForScore(gc.ProfileManager.SigGamer, score) : -1
            };

            for (int i = 0; i < ranks.Length; ++i)
            {
                if (ranks[i] < 1 || (i < 2 && ranks[i] > 100) || (i == 2 && ranks[i] > 500))
                    continue;

                SPSprite rankSprite = new SPSprite();
                rankSprite.Rotation = SPMacros.PI / 12f;
                rankSprite.Visible = false;

                string rankText = ranks[i].ToString() + Globals.SuffixForRank(ranks[i]);
                SPTextField rankLabel = new SPTextField(120, 56, rankText, mScene.FontKey, 38);
                rankLabel.X -= rankLabel.Width / 2;
                rankLabel.HAlign = SPTextField.SPHAlign.Center;
                rankLabel.VAlign = SPTextField.SPVAlign.Center;
                rankLabel.Color = i == 0 ? SPUtils.ColorFromColor(0x4246fe) : i == 1 ? SPUtils.ColorFromColor(0x7000cc) : SPUtils.ColorFromColor(0xd60000);
                rankSprite.AddChild(rankLabel);
                mRankSprites.Add(rankSprite);
                mCanvasSprite.AddChild(rankSprite);
            }

            float rankX = 410 + (3 - mRankSprites.Count) * 54;
            for (int i = 0; i < mRankSprites.Count; ++i, rankX += 108)
            {
                mRankSprites[i].X = ResManager.RESX(rankX);
                mRankSprites[i].Y = ResManager.RESY(120);
            }
    
            // New Best
            int hiScore = gc.PlayerDetails.HiScore;
            if (score > hiScore)
            {
                SPImage bestImage = new SPImage(mScene.TextureByName("new-best"));
                bestImage.X = -bestImage.Width / 2;
                bestImage.Y = -bestImage.Height / 2;
        
                mBestSprite = new SPSprite();
                mBestSprite.X = ResManager.RESX(730); // ResManager.RESX(480);  //ResManager.RESX(272);
                mBestSprite.Y = ResManager.RESY(145); // ResManager.RESY(132);  //ResManager.RESY(140);
                mBestSprite.Rotation = SPMacros.PI / 6f;
                mBestSprite.Visible = false;
                mBestSprite.AddChild(bestImage);
                mCanvasSprite.AddChild(mBestSprite);
            }

            // Mastery XP
            if (score > 0 && mMasteryLevel < MasteryModel.kMaxMasteryLevel)
            {
                mXPText = new SPTextField(350, 56, scoreString, mScene.FontKey, 40);
                mXPText.HAlign = SPTextField.SPHAlign.Left;
                mXPText.VAlign = SPTextField.SPVAlign.Top;
                mXPText.X = mScoreSprite.X + scoreSprite.X + mScoreText.X;
                mXPText.Y = mScoreSprite.Y+ scoreSprite.Y + mScoreText.Y;
                mXPText.Color = Color.DarkGreen;
                mXPText.Visible = false;
                mCanvasSprite.AddChild(mXPText);

                mXPPanel = new MasteryXPPanel(Category, mLevelXPFraction);
                mXPPanel.X = ResManager.RESX(366);
                mXPPanel.Y = ResManager.RESY(304);
                mXPPanel.Alpha = 0f;
                mCanvasSprite.AddChild(mXPPanel);
            }
    
            // Stats
            SPSprite statsSprite = new SPSprite();
            statsSprite.Touchable = false;
    
                // Accuracy
            SPTextField accuracyLabel = new SPTextField(176, 40, "Accuracy", mScene.FontKey, 30);
            accuracyLabel.HAlign = SPTextField.SPHAlign.Right;
            accuracyLabel.VAlign = SPTextField.SPVAlign.Top;
            accuracyLabel.Color = Color.Black;
            statsSprite.AddChild(accuracyLabel);

            int cannonAccuracy = (int)(100.0f * gc.ThisTurn.CannonAccuracy);
            mAccuracyText = new SPTextField(90, 40, Locale.SanitizeText(cannonAccuracy.ToString("D") + "%", mScene.FontKey, 30), mScene.FontKey, 30);
            mAccuracyText.HAlign = SPTextField.SPHAlign.Left;
            mAccuracyText.VAlign = SPTextField.SPVAlign.Top;
            mAccuracyText.X = accuracyLabel.X + accuracyLabel.Width + 32;
            mAccuracyText.Y = accuracyLabel.Y;
            mAccuracyText.Color = Color.Black;
            statsSprite.AddChild(mAccuracyText);
    
                // Plankings
            SPTextField plankingsLabel = new SPTextField(176, 40, "Ships Sunk", mScene.FontKey, 30);
            plankingsLabel.HAlign = SPTextField.SPHAlign.Right;
            plankingsLabel.VAlign = SPTextField.SPVAlign.Top;
            plankingsLabel.Y = accuracyLabel.Y + 48;
            plankingsLabel.Color = Color.Black;
            statsSprite.AddChild(plankingsLabel);
    
            mPlankingsText = new SPTextField(90, 40, GuiHelper.CommaSeparatedValue(gc.ThisTurn.ShipsSunk), mScene.FontKey, 30);
            mPlankingsText.HAlign = SPTextField.SPHAlign.Left;
            mPlankingsText.VAlign = SPTextField.SPVAlign.Top;
            mPlankingsText.X = plankingsLabel.X + plankingsLabel.Width + 32;
            mPlankingsText.Y = plankingsLabel.Y;
            mPlankingsText.Color = Color.Black;
            statsSprite.AddChild(mPlankingsText);
    
                // Days at Sea
            SPTextField daysAtSeaLabel = new SPTextField(176, 40, "Days at Sea", mScene.FontKey, 30);
            daysAtSeaLabel.HAlign = SPTextField.SPHAlign.Right;
            daysAtSeaLabel.VAlign = SPTextField.SPVAlign.Top;
            daysAtSeaLabel.Y = plankingsLabel.Y + 48;
            daysAtSeaLabel.Color = Color.Black;
            statsSprite.AddChild(daysAtSeaLabel);
    
            mDaysAtSeaText = new SPTextField(90, 40, Locale.SanitizedFloat(gc.ThisTurn.DaysAtSea, "F2", mScene.FontKey, 30), mScene.FontKey, 30);
            mDaysAtSeaText.HAlign = SPTextField.SPHAlign.Left;
            mDaysAtSeaText.VAlign = SPTextField.SPVAlign.Top;
            mDaysAtSeaText.X = daysAtSeaLabel.X + daysAtSeaLabel.Width + 32;
            mDaysAtSeaText.Y = daysAtSeaLabel.Y;
            mDaysAtSeaText.Color = Color.Black;
            statsSprite.AddChild(mDaysAtSeaText);
    
            statsSprite.X = -statsSprite.Width / 2;
            statsSprite.Y = -statsSprite.Height / 2;
    
            mStatsSprite = new SPSprite();
            mStatsSprite.X = mScrollSprite.X + mScrollSprite.Width / 2; //mScene.ViewWidth / 2;
            mStatsSprite.Y = ResManager.RESY(274 + statsSprite.Height / 2);
            mStatsSprite.Visible = false;
            mStatsSprite.AddChild(statsSprite);
            mCanvasSprite.AddChild(mStatsSprite);
    
            // Death Sprite
	        mDeathSprite = new SPSprite();
	        mDeathSprite.Touchable = false;
	
	        SPTexture deathTexture = mScene.TextureByName("death");
	        deathTexture = Globals.WholeTextureFromHalfHoriz(deathTexture);
	
	        SPImage deathImage = new SPImage(deathTexture);
	        deathImage.X = -deathImage.Width / 2;
	        deathImage.Y = -deathImage.Height / 2;
            mDeathSprite.AddChild(deathImage);
	        mDeathSprite.X = mScene.ViewWidth / 2;
	        mDeathSprite.Y = mScene.ViewHeight / 2;
            AddChild(mDeathSprite);
            mDeathSprite.ScaleX = mDeathSprite.ScaleY = 0.5f;

            //ResManager.RESM.PopOffset();

            mCanvasSprite.X = -(mScrollSprite.X + mScrollSprite.Width / 2);
            mCanvasSprite.Y = -(mScrollSprite.Y + mScrollSprite.Height / 2);
            mCanvasScaler.ScaleX = mCanvasScaler.ScaleY = mScene.ScaleForUIView(mScrollSprite, 1.15f, 0.65f);
            Y = -(mScene.ViewHeight - mScrollSprite.Height) / 10;
        }

        public void AttachGamerPic(SPDisplayObject gamerPic)
        {
            if (gamerPic != null && mScrollSprite != null)
            {
                gamerPic.X = 40;
                gamerPic.Y = 22;
                mScrollSprite.AddChild(gamerPic);
            }
        }

        public void DetachGamerPic(SPDisplayObject gamerPic)
        {
            if (gamerPic != null && mScrollSprite != null)
                mScrollSprite.RemoveChild(gamerPic);
        }

        protected virtual void AddMenuButtons()
        {
            if (mButtons != null)
                return;

            mRetryButton = new MenuButton(null, mScene.TextureByName("retry-button"));
            mRetryButton.X = ResManager.RESX(328);
            mRetryButton.Y = ResManager.RESY(448);
            mRetryButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnRetryButtonPressed);
            mCanvasSprite.AddChild(mRetryButton);

            mMenuButton = new MenuButton(null, mScene.TextureByName("menu-button"));
            mMenuButton.X = ResManager.RESX(544);
            mMenuButton.Y = ResManager.RESY(448);
            mMenuButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnMenuButtonPressed);
            mCanvasSprite.AddChild(mMenuButton);
    
            mButtons = new Dictionary<string,SPButton>()
            {
                { "Retry", mRetryButton },
                { "Menu", mMenuButton }
            };

            mButtonsProxy = new ButtonsProxy(InputFocus, Globals.kNavHorizontal);
            mButtonsProxy.AddButton(mRetryButton);
            mButtonsProxy.AddButton(mMenuButton);
            mScene.SubscribeToInputUpdates(mButtonsProxy);
        }

        public void EnableMenuButton(bool enable, string key)
        {
            if (key != null && mButtons.ContainsKey(key))
                mButtons[key].Enabled = enable;
        }

        public void SetMenuButtonHidden(bool hidden, string key)
        {
            if (key != null && mButtons.ContainsKey(key))
                mButtons[key].Visible = !hidden;
        }

        protected void PlaySoundWithKey(string key)
        {
            mScene.PlaySound(key);
        }

        public void DisplaySummaryScroll()
        {
            mCanvasSprite.Visible = true;
            mScene.PushFocusState(InputManager.FOCUS_STATE_PF_GAMEOVER);
        }

        public void HideSummaryScroll()
        {
            mCanvasSprite.Visible = false;
        }

        public float DisplayGameOverSequence()
        {
            mScene.Juggler.RemoveTweensWithTarget(mDeathSprite);
            PlaySoundWithKey("Death"); // Sound plays for ~4s
	
	        float scaleDuration = 2.2f;
	
	        SPTween fadeTween = new SPTween(mDeathSprite, 1.0f, SPTransitions.SPEaseIn);
            fadeTween.AnimateProperty("Alpha", 0f);
	        fadeTween.Delay = scaleDuration - scaleDuration / 8f;
            mScene.Juggler.AddObject(fadeTween);
	
	        SPTween scaleTween = new SPTween(mDeathSprite, scaleDuration, SPTransitions.SPEaseInOut);
            scaleTween.AnimateProperty("ScaleX", 2f);
            scaleTween.AnimateProperty("ScaleY", 2f);
            mScene.Juggler.AddObject(scaleTween);
            return (float)(fadeTween.TotalTime + fadeTween.Delay);
        }

        private void DisplayStatsOverTime(float duration, float delay = 0f)
        {
            if (mStatsSprite == null)
                return;

            mScene.Juggler.RemoveTweensWithTarget(mStatsSprite);
            mStatsSprite.Alpha = 0;
            mStatsSprite.Visible = true;

            SPTween tween = new SPTween(mStatsSprite, duration);
            tween.AnimateProperty("Alpha", 1f);
            tween.Delay = delay;
            mScene.Juggler.AddObject(tween);
        }

        private void HideMasteryPanelOverTime(float duration, float delay = 0f)
        {
            if (mXPPanel == null)
                return;

            mScene.Juggler.RemoveTweensWithTarget(mXPPanel);

            SPTween tween = new SPTween(mXPPanel, duration);
            tween.AnimateProperty("Alpha", 0f);
            tween.Delay = delay;
            mScene.Juggler.AddObject(tween);
        }

        public void DisplayMasterySequence()
        {
            if (mXPText == null || mXPPanel == null)
                DisplayStatsOverTime(0.5f);
            else
            {
                mScene.Juggler.RemoveTweensWithTarget(mXPText);

                SPTween fadeInTween = new SPTween(mXPPanel, 0.5f);
                fadeInTween.AnimateProperty("Alpha", 1f);
                mScene.Juggler.AddObject(fadeInTween);

                float xTarget = mXPPanel.X + ((mXPPanel.Width - ((mXPPanel.Stamp != null) ? mXPPanel.Stamp.Width : 0)) - mXPText.TextBounds.Width) / 2;
                SPTween translateTween = new SPTween(mXPText, 1f);
                translateTween.AnimateProperty("X", xTarget);
                translateTween.AnimateProperty("Y", mXPPanel.Y);
                translateTween.Delay = fadeInTween.TotalTime;
                translateTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_STARTED, (SPEventHandler)OnMasteryIntroBegan);
                translateTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnMasteryIntroCompleted);
                mScene.Juggler.AddObject(translateTween);

                SPTween fadeOutTween = new SPTween(mXPText, 0.25f);
                fadeOutTween.AnimateProperty("Alpha", 0f);
                fadeOutTween.Delay = translateTween.Delay + translateTween.TotalTime - fadeOutTween.TotalTime;
                mScene.Juggler.AddObject(fadeOutTween);
            }
        }

        private void OnMasteryIntroBegan(SPEvent ev)
        {
            if (mXPText != null)
                mXPText.Visible = true;
        }

        private void OnMasteryIntroCompleted(SPEvent ev)
        {
            if (mXPText != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mXPText);
                mXPText = null;
            }

            DisplayMasteryXPGaugeSequence();
        }

        private void DisplayMasteryXPGaugeSequence(float delay = 0f)
        {
            if (mXPPanel == null)
                DisplayStatsOverTime(0.5f, delay);
            else
            {
                mXPPanel.PercentComplete = mLevelXPFraction;

                float target;
                int currentMasteryLevel = mScene.MasteryManager.CurrentModel.MasteryLevel;

                if (mMasteryLevel < currentMasteryLevel)
                    target = 1f;
                else
                    target = mScene.MasteryManager.CurrentModel.LevelXPFraction;

                SPTween tween = new SPTween(mXPPanel, Math.Max(1f, 1f + (target - mLevelXPFraction)));
                tween.AnimateProperty("PercentComplete", target);
                tween.Delay = delay;
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnMasteryXPGaugeCompleted);
                mScene.Juggler.AddObject(tween);
            }
        }

        private void OnMasteryXPGaugeCompleted(SPEvent ev)
        {
            if (mXPPanel.PercentComplete == 1f)
                DisplayMasteryLevelUpSequence(1f);
            else
            {
                // We've finished everything - transition to game stats
                HideMasteryPanelOverTime(0.5f, 1f);
                DisplayStatsOverTime(0.5f, 1f);
            }
        }

        private void DisplayMasteryLevelUpSequence(float delay = 0f)
        {
            if (mXPPanel != null && mXPPanel.Stamp != null)
            {
                SPSprite stamp = mXPPanel.Stamp;
                stamp.Alpha = 1f;
                stamp.Visible = false;
                mScene.Juggler.RemoveTweensWithTarget(stamp);
                StampAnimationWithStamp(stamp, 0.1f, delay);
            }
            else
            {
                MasteryLevelUpCompleted();
            }
        }

        private void MasteryLevelUpCompleted()
        {
            int currentMasteryLevel = mScene.MasteryManager.CurrentModel.MasteryLevel;
            ++mMasteryLevel;
            mLevelXPFraction = 0;

            if (mMasteryLevel <= currentMasteryLevel)
            {
                // We have more progress to animate
                HideMasteryStampOverTime(1f, 2f);
                mScene.Juggler.DelayInvocation(this, 3f, delegate
                {
                    if (mScene != null)
                        DisplayMasteryXPGaugeSequence();
                });
            }
            else if (mMasteryLevel > currentMasteryLevel)
            {
                // We've finished everything - transition to game stats
                HideMasteryPanelOverTime(0.5f, 4f);
                DisplayStatsOverTime(0.5f, 4f);
            }
        }

        private void HideMasteryStampOverTime(float duration, float delay)
        {
            if (mXPPanel == null || mXPPanel.Stamp == null)
                return;

            SPSprite stamp = mXPPanel.Stamp;
            mScene.Juggler.RemoveTweensWithTarget(stamp);

            SPTween tween = new SPTween(stamp, duration);
            tween.AnimateProperty("Alpha", 0f);
            tween.Delay = delay;
            mScene.Juggler.AddObject(tween);
        }

        public float StampsDuration
        {
            get
            {
                float duration = 0;

                if (mScoreSprite != null)
                    duration += 1.0f;

                if (mRankSprites != null)
                {
                    foreach (SPSprite sprite in mRankSprites)
                        duration += 0.5f;

                    if (mRankSprites.Count > 0)
                        duration += 0.5f;
                }

                if (mBestSprite != null)
                    duration += 0.5f;

                return duration;
            }
        }

        public void DisplayStamps()
        {    
            float delay = 0;
    
            if (mScoreSprite != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mScoreSprite);
                StampAnimationWithStamp(mScoreSprite, 0.1f, delay);
                delay += 1.0f;
            }

            if (mRankSprites != null)
            {
                foreach (SPSprite sprite in mRankSprites)
                {
                    mScene.Juggler.RemoveTweensWithTarget(sprite);
                    StampAnimationWithStamp(sprite, 0.1f, delay);
                    delay += 0.5f;
                }

                if (mRankSprites.Count > 0)
                    delay += 0.5f;
            }

            if (mBestSprite != null)
            {
                mScene.Juggler.RemoveTweensWithTarget(mBestSprite);
                StampAnimationWithStamp(mBestSprite, 0.1f, delay);
            }
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

                    if (mXPPanel != null && mXPPanel.Stamp != null && target == mXPPanel.Stamp)
                        PlaySoundWithKey("StampLoud");
                    else
                        PlaySoundWithKey("Stamp");
                }
            }
        }

        protected virtual void OnStamped(SPEvent ev)
        {
            ShakeCanvas(mCanvasSprite);

            // Cheer for new best score
            SPTween tween = ev.CurrentTarget as SPTween;

            if (tween != null)
            {
                SPSprite sprite = tween.Target as SPSprite;
                if (sprite != null)
                {
                    if (sprite == mBestSprite)
                        PlaySoundWithKey("CrowdCheer");
                    else if (mXPPanel != null && sprite == mXPPanel.Stamp)
                    {
                        PlaySoundWithKey("CrewCelebrate");
                        MasteryLevelUpCompleted();
                    }
                }
            }
        }

        protected void OnRetryButtonPressed(SPEvent ev)
        {
            PlaySoundWithKey("Button");
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_GAME_SUMMARY_RETRY));
        }

        protected void OnMenuButtonPressed(SPEvent ev)
        {
            PlaySoundWithKey("Button");
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_GAME_SUMMARY_MENU));
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mScene.Juggler.RemoveTweensWithTarget(this);

                        if (mRankSprites != null)
                        {
                            foreach (SPSprite sprite in mRankSprites)
                                mScene.Juggler.RemoveTweensWithTarget(sprite);
                            mRankSprites = null;
                        }

                        if (mBestSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mBestSprite);
                            mBestSprite = null;
                        }

                        if (mScoreSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mScoreSprite);
                            mScoreSprite = null;
                        }

                        if (mStatsSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mStatsSprite);
                            mStatsSprite = null;
                        }

                        if (mDeathSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mDeathSprite);
                            mDeathSprite = null;
                        }

                        if (mXPPanel != null)
                        {
                            if (mXPPanel.Stamp != null)
                                mScene.Juggler.RemoveTweensWithTarget(mXPPanel.Stamp);

                            mScene.Juggler.RemoveTweensWithTarget(mXPPanel);
                            mXPPanel = null;
                        }

                        if (mXPText != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mXPText);
                            mXPText = null;
                        }

                        if (mCanvasSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mCanvasSprite);
                            mCanvasSprite = null;
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

                        mScrollSprite = null;
                        mButtons = null;
                        mScoreText = null;
                        mAccuracyText = null;
                        mPlankingsText = null;
                        mDaysAtSeaText = null;
                    }
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
