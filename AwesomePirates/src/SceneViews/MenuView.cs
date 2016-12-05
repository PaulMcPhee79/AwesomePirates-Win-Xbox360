using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class MenuView : Prop, IInteractable
    {
        public const string CUST_EVENT_TYPE_MENU_VIEW_DID_TRANSITION_IN = "menuViewDidTransitionInEvent";
        public const string CUST_EVENT_TYPE_MENU_VIEW_DID_TRANSITION_OUT = "menuViewDidTransitionOutEvent";
        public const string CUST_EVENT_TYPE_MENU_VIEW_START_TO_PLAY = "menuViewStartToPlayEvent";

        private static float s_CloseSubviewScale = 0.8f;

        public MenuView(int category, MenuController controller)
            : base(category)
        {
            mController = controller;
            SetupProp();
            mScene.SubscribeToInputUpdates(this);
        }
        
        #region Fields
        private Vector2 mLogoInPos;
        private Vector2 mShadyInPos;

        // View
        private MenuButton mCloseSubviewButton;
        private MenuButton mBuyButton;
        private MenuButton mInviteButton;
        private TitleScreen mTitleScreen;
        private TitleSubview mMenuSubview;
        private ObjectivesLog mObjectivesLog;
        private MasteryLog mMasteryLog;
        private SPSprite mCanvas;
        private int mSubviewContainerIndex;
        private List<SPSprite> mSubviewContainers;
        private ViewParser mViewParser;

        // Saving Progress
        private SaveNoticeView mSavingView;

        // Hi Scores Carousel
        private ScoreCarousel mHiScoreCarousel;

        // Mode Select
        private ModeSelectView mModeSelectView;

        // Achievements
        private TableView mAchievementsView;

        // Stats
        private TableView mStatsView;

        // Leaderboard
        private LeaderboardView mLeaderboardView;

        // Options
        private OptionsView mOptionsView;

        // Display Adjustment
        private SafeAreaView mSafeAreaView;

        // Potions
        private PotionView mPotionView;

        // Exit Prompt
        private Prop mExitPrompt;

        // Model
        private Dictionary<string, TitleSubview> mSubviews;
        private List<TitleSubview> mSubviewStack;
        private MenuController mController;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_MENU_ALL; } }
        public int SubviewStackHeight { get { return mSubviewStack != null ? mSubviewStack.Count : 0; } }
        public TitleSubview CurrentSubview
        {
            get
            {
                TitleSubview current = null;
	
	            if (mSubviewStack.Count > 0)
		            current = mSubviewStack[mSubviewStack.Count-1];
	            return current;
            }
        }
        public bool PotionWasSelected { get { return mPotionView != null && mPotionView.PotionWasSelected; } }
        public bool IsModeSelectViewShowing { get { return (mModeSelectView != null && mModeSelectView.State != ModeSelectView.ModeSelectState.None); } }
        #endregion

        #region Methods
        //public void TempDraw(SPDisplayObject displayObject, GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        //{
        //    mScene.CustomDrawer.HighlightDraw(displayObject, gameTime, support, parentTransform);
        //}

        protected override void SetupProp()
        {
            if (mViewParser != null)
                return;
    
            mCanvas = new SPSprite();
            mCanvas.X = mScene.ViewWidth / 2;
            mCanvas.Y = mScene.ViewHeight / 2;
            AddChild(mCanvas);

            mSubviewContainerIndex = 0;
            mSubviewContainers = new List<SPSprite>();

        // 1. Load plist subviews
            mViewParser = new ViewParser(mScene, mController, (SPEventHandler)mController.OnButtonTriggered, "data/plists/Title.plist");
            mViewParser.Category = Category;
            mViewParser.FontKey = mScene.FontKey;
	
	        mSubviews = mViewParser.ParseTitleSubviewsByViewName("Subviews");
            mSubviewStack = new List<TitleSubview>();
    
        // 2. Create menu subview
            mMenuSubview = new TitleSubview(Category);
            mMenuSubview.Repeats = true;
            mSubviews.Add("Menu", mMenuSubview);
    
            // Shady
        //[RESM pushItemOffsetWithAlignment:RALowerCenter];
            SPImage shadyImage = new SPImage(mScene.TextureByName("shady"));
            SPSprite shady = new SPSprite();
            shady.Touchable = false;
    
            if (ResManager.RESM.IsCustRes)
            {
                shady.X = (mScene.ViewWidth - 1024) / 4 - 76;
                shady.Y = (mScene.ViewHeight - 768) + 312;
            }
            else
            {
                shady.X = -90;
                shady.Y = 206;
            }
    
            shady.AddChild(shadyImage);
            mMenuSubview.AddChild(shady);
            mMenuSubview.SetControlForKey(shady, "Shady");
        //[RESM popOffset];

            // Logo
        //[RESM pushItemOffsetWithAlignment:RACenter];
            SPImage logoImage = new SPImage(mScene.TextureByName("logo"));
    
            SPSprite logo = new SPSprite();
            logo.Touchable = false;
    
            if (ResManager.RESM.IsCustRes)
            {
                logo.X = (mScene.ViewWidth - 1024) / 2 + 140;
                logo.Y = (mScene.ViewHeight - 768) / 2 + 112;
            }
            else
            {
                logo.X = 80;
                logo.Y = 164;
                logo.ScaleX = logo.ScaleY = 230.0f / 256.0f;
            }

            logo.AddChild(logoImage);

            SPSprite logoPromptSprite = new SPSprite();
            SPImage startImage = new SPImage(mScene.TextureByName("large_start"));
            logoPromptSprite.AddChild(startImage);

            SPTextField logoText = new SPTextField(160, 40, "TO  PLAY", mScene.FontKey, 32);
            logoText.X = startImage.X + 1.2f * startImage.Width;
            logoText.Y = startImage.Y + 0.25f * startImage.Height;
            logoText.Color = SPUtils.ColorFromColor(0xfcc30e);
            logoText.HAlign = SPTextField.SPHAlign.Left;
            logoText.VAlign = SPTextField.SPVAlign.Top;
            logoPromptSprite.AddChild(logoText);

            logoPromptSprite.X = logoImage.X + (logoImage.Width - logoPromptSprite.Width) / 2;
            logoPromptSprite.Y = logoImage.Y + logoImage.Height + 64;
            logo.AddChild(logoPromptSprite);
            mMenuSubview.SetControlForKey(logoPromptSprite, "LogoPrompt");

            SPTween logoPromptTween = new SPTween(logoPromptSprite, 1.0f);
            logoPromptTween.AnimateProperty("Alpha", 0);
            logoPromptTween.Loop = SPLoopType.Reverse;
            mScene.Juggler.AddObject(logoPromptTween);
            mMenuSubview.AddLoopingTween(logoPromptTween);

            mMenuSubview.AddChild(logo);
            mMenuSubview.SetControlForKey(logo, "Logo");
        //[RESM popOffset];
    
            // Side Scroll
            SPImage sideScrollImage = new SPImage(mScene.TextureByName("menu-side-scroll"));
            sideScrollImage.ScaleX = 1.05f;
            sideScrollImage.Touchable = false;
            SPSprite sideScroll = new SPSprite();
            sideScroll.X = mScene.ViewWidth - (sideScrollImage.Width - 1);
            sideScroll.Y = 0;
            sideScroll.AddChild(sideScrollImage);
            mMenuSubview.AddChild(sideScroll);
            mMenuSubview.SetControlForKey(sideScroll, "SideScroll");
    
            // Hi Score
            mHiScoreCarousel = new ScoreCarousel((int)PFCat.HUD);
            mHiScoreCarousel.AutoPopulateScorers();
            mHiScoreCarousel.X = sideScrollImage.Width - 400;
            mHiScoreCarousel.Y = 8;
            sideScroll.AddChild(mHiScoreCarousel);
    
            // Buttons
            MenuButton objectivesButton = new MenuButton(new Action(mController.Objectives), mScene.TextureByName("objectives-button"));
            objectivesButton.SfxKey = "Button";
            objectivesButton.X = sideScrollImage.Width - 290;
            objectivesButton.Y = 106;
            objectivesButton.ScaleX = objectivesButton.ScaleY = 1.1f;
            objectivesButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            mMenuSubview.SetControlForKey(objectivesButton, "ObjectivesButton");
            sideScroll.AddChild(objectivesButton);

            MenuButton masteryButton = new MenuButton(new Action(mController.Mastery), mScene.TextureByName("mastery-button"));
            masteryButton.SfxKey = "Button";
            masteryButton.X = sideScrollImage.Width - 282;
            masteryButton.Y = 190;
            masteryButton.ScaleX = masteryButton.ScaleY = 1.1f;
            masteryButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            mMenuSubview.SetControlForKey(masteryButton, "MasteryButton");
            sideScroll.AddChild(masteryButton);

            MenuButton achievementsButton = new MenuButton(new Action(mController.Achievements), mScene.TextureByName("acts-of-piracy-button"));
            achievementsButton.SfxKey = "Button";
            achievementsButton.X = sideScrollImage.Width - 290;
            achievementsButton.Y = 274;
            achievementsButton.ScaleX = achievementsButton.ScaleY = 1.1f;
            achievementsButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            mMenuSubview.SetControlForKey(achievementsButton, "AchievementsButton");
            sideScroll.AddChild(achievementsButton);

            MenuButton lbButton = new MenuButton(new Action(mController.Leaderboard), mScene.TextureByName("hall-of-infamy-button"));
            lbButton.SfxKey = "Button";
            lbButton.X = sideScrollImage.Width - 284;
            lbButton.Y = 352;
            lbButton.ScaleX = lbButton.ScaleY = 1.1f;
            lbButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            mMenuSubview.SetControlForKey(lbButton, "LeaderboardButton");
            sideScroll.AddChild(lbButton);

            MenuButton infoButton = new MenuButton(new Action(mController.Info), mScene.TextureByName("info-button"));
            infoButton.SfxKey = "Button";
            infoButton.X = sideScrollImage.Width - 286;
            infoButton.Y = 432;
            infoButton.ScaleX = infoButton.ScaleY = 1.1f;
            infoButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            mMenuSubview.SetControlForKey(infoButton, "InfoButton");
            sideScroll.AddChild(infoButton);
        
            MenuButton optionsButton = new MenuButton(new Action(mController.Options), mScene.TextureByName("options-button"));
            optionsButton.SfxKey = "Button";
            optionsButton.X = sideScrollImage.Width - 344;
            optionsButton.Y = 512;
            optionsButton.ScaleX = optionsButton.ScaleY = 1.1f;
            optionsButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            mMenuSubview.SetControlForKey(optionsButton, "OptionsButton");
            sideScroll.AddChild(optionsButton);

            mBuyButton = new MenuButton(new Action(mController.BuyNow), mScene.TextureByName("buy-now"));
            mBuyButton.SfxKey = "Button";
            mBuyButton.X = sideScrollImage.Width - 244;
            mBuyButton.Y = 608;
            mBuyButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            mMenuSubview.SetControlForKey(mBuyButton, "BuyButton");
            sideScroll.AddChild(mBuyButton);

            mInviteButton = new MenuButton(new Action(mController.InviteFriends), mScene.TextureByName("invite-button"));
            mInviteButton.SfxKey = "Button";
            if (mInviteButton.SelectedEffecter != null)
                mInviteButton.SelectedEffecter.Factor = 1.2f;
            mInviteButton.X = sideScrollImage.Width - 284;
            mInviteButton.Y = 620;
            mInviteButton.ScaleX = mInviteButton.ScaleY = 1.2f;
            mInviteButton.Visible = !mBuyButton.Visible;
            mInviteButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            mMenuSubview.SetControlForKey(mInviteButton, "InviteButton");
            sideScroll.AddChild(mInviteButton);
            
            ResManager.RESM.PushOffset(new ResOffset(0, 0, -14, 128));
            MenuButton potionsButton = new MenuButton(new Action(mController.Potions), mScene.TextureByName("potions-button"));
            potionsButton.SfxKey = "Button";
            potionsButton.X = 0;
            potionsButton.Y = ResManager.RESY(360) - 0.2f * potionsButton.Height;
            potionsButton.ScaleX = potionsButton.ScaleY = 1.2f;
            potionsButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            mMenuSubview.SetControlForKey(potionsButton, "PotionsButton");
            sideScroll.AddChild(potionsButton);
            ResManager.RESM.PopOffset();
    
        // 3. Create Mode Select Subview
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            TitleSubview modeSelectSubview = new TitleSubview(Category);
            modeSelectSubview.CloseSelector = mController.GetType().GetMethod("CloseModeSelect");
            modeSelectSubview.ClosePosition = new CCPoint(ResManager.RESX(688), ResManager.RESY(140));
            modeSelectSubview.GuidePos = TitleSubview.GuidePositionForScene(TitleSubview.GuidePosition.MidLower, mScene);
            modeSelectSubview.DoesScaleToFill = false;
            mSubviews.Add("ModeSelect", modeSelectSubview);
            ResManager.RESM.PopOffset();

            mModeSelectView = new ModeSelectView(Category);
            mModeSelectView.X = mScene.ViewWidth / 2;
            mModeSelectView.Y = mScene.ViewHeight / 2;
            mModeSelectView.AddEventListener(ModeSelectView.CUST_EVENT_TYPE_CAREER_MODE_SELECTED, (SPEventHandler)mController.SinglePlayerGameRequested);
            mModeSelectView.AddEventListener(ModeSelectView.CUST_EVENT_TYPE_FFA_MODE_SELECTED, (SPEventHandler)mController.FFAGameRequested);
            mModeSelectView.AddEventListener(ModeSelectView.CUST_EVENT_TYPE_2V2_MODE_SELECTED, (SPEventHandler)mController.TwoVTwoGameRequested);
            modeSelectSubview.AddChild(mModeSelectView);
            modeSelectSubview.AddMiscProp(mModeSelectView);

        // 4. Create Objectives Log
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            TitleSubview objectivesSubview = new TitleSubview(Category);
            objectivesSubview.CloseSelector = mController.GetType().GetMethod("CloseObjectives");
            objectivesSubview.ClosePosition = new CCPoint(ResManager.RESX(784), ResManager.RESY(96));
            objectivesSubview.GuidePos = TitleSubview.GuidePositionForScene(TitleSubview.GuidePosition.MidLower, mScene);
            objectivesSubview.GamerPicPos = new CCPoint(ResManager.RESX(80), ResManager.RESY(94));
            objectivesSubview.ScaleToFillThreshold = 1f;
            objectivesSubview.DoesScaleToFill = true;
            mSubviews.Add("Objectives", objectivesSubview);
            ResManager.RESM.PopOffset();
    
            mObjectivesLog = new ObjectivesLog(Category, mScene.ObjectivesManager.Rank);
            objectivesSubview.AddChild(mObjectivesLog);
            objectivesSubview.AddMiscProp(mObjectivesLog);

        // 5. Create Mastery Subview
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            TitleSubview masterySubview = new TitleSubview(Category);
            masterySubview.CloseSelector = mController.GetType().GetMethod("CloseMastery");
            masterySubview.ClosePosition = new CCPoint(ResManager.RESX(784), ResManager.RESY(96));
            masterySubview.GuidePos = TitleSubview.GuidePositionForScene(TitleSubview.GuidePosition.MidLower, mScene);
            masterySubview.GamerPicPos = new CCPoint(ResManager.RESX(80), ResManager.RESY(94));
            masterySubview.ScaleToFillThreshold = 1f;
            masterySubview.DoesScaleToFill = true;
            mSubviews.Add("Mastery", masterySubview);
            ResManager.RESM.PopOffset();

        // 6. Dummy Exit/Buy Subview
            TitleSubview exitSubview = new TitleSubview(Category);
            mSubviews.Add("Exit", exitSubview);

        // 7. Close Subview Button
            s_CloseSubviewScale = ResManager.RITMFXY(1f);
            s_CloseSubviewScale = (float)Math.Pow(s_CloseSubviewScale, 2) * 0.65f;

            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            SPTexture closeButtonTexture = mScene.TextureByName("large_face_b"); //mScene.TextureByName("menu-close");
	        mCloseSubviewButton = new MenuButton(new Action(mController.CloseSubview), closeButtonTexture, closeButtonTexture, false, false);
            mCloseSubviewButton.Tag = MenuController.kIgnorePlayerSwitchButtonTag;
	        mCloseSubviewButton.X = ResManager.RESX(750);
	        mCloseSubviewButton.Y = ResManager.RESY(72);
	        mCloseSubviewButton.SfxKey = "Button";
            mCloseSubviewButton.ScaleX = s_CloseSubviewScale;
            mCloseSubviewButton.ScaleY = s_CloseSubviewScale;
	        mCloseSubviewButton.ScaleWhenDown = 0.85f;
            mCloseSubviewButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnButtonTriggered);
            ResManager.RESM.PopOffset();

        // 8. Info subview
            TitleSubview infoSubview = SubviewForKey("Info");
            infoSubview.Repeats = true;
            infoSubview.RepeatDelay = 0.3;

            SPButton statsLogButton = DisplayObjectFromSubview("Info", "statsLogInfo") as SPButton;
            SPTextField statsLogLabel = DisplayObjectFromSubview("Info", "statsLogLabel") as SPTextField;
    
            if (statsLogButton != null && statsLogLabel != null)
                statsLogButton.AddContent(statsLogLabel);
            statsLogLabel.Touchable = true;
    
            SPButton gameConceptsButton = DisplayObjectFromSubview("Info", "gameConceptsInfo") as SPButton;
            SPTextField gameConceptsLabel = DisplayObjectFromSubview("Info", "gameConceptsLabel") as SPTextField;
    
            if (gameConceptsButton != null && gameConceptsLabel != null)
                gameConceptsButton.AddContent(gameConceptsLabel);
            gameConceptsLabel.Touchable = true;
    
            SPButton spellsMunitionsButton = DisplayObjectFromSubview("Info", "spellsMunitionsInfo") as SPButton;
            SPTextField spellsMunitionsLabel = DisplayObjectFromSubview("Info", "spellsMunitionsLabel") as SPTextField;
    
            if (spellsMunitionsButton != null && spellsMunitionsLabel != null)
                spellsMunitionsButton.AddContent(spellsMunitionsLabel);
            spellsMunitionsLabel.Touchable = true;

        // 9. Saving Progress prompt
            mSavingView = new SaveNoticeView(mScene.HelpCategory);
            mSavingView.X = 40;
            mSavingView.Y = 72;
            mSavingView.TweenComplete = new Action(OnSavingProgressPromptCompleted);

            // Scale content to match viewport
            float adjustedScale = 1f;

            if (!SPMacros.SP_IS_FLOAT_EQUAL(sideScrollImage.Height, mScene.ViewHeight))
                adjustedScale = mScene.ViewHeight / sideScrollImage.Height;
            
            // Adjust sideScroll scale
            sideScroll.Scale = new Vector2(adjustedScale, adjustedScale);
            sideScroll.X += (1f - adjustedScale) * sideScrollImage.Width;

            /*
            // Adjust logo scale
            if (adjustedScale < 0.9f || adjustedScale > 1.01f)
            {
                logo.Scale = new Vector2(adjustedScale, adjustedScale);
                logo.X += (1f - adjustedScale) * logo.Width;
                logo.Y += (1f - adjustedScale) * logo.Height;

                // Adjust shady scale
                shady.Scale = new Vector2(adjustedScale, adjustedScale);
                shady.X += (1f - adjustedScale) * shady.Width;
                shady.Y += (1f - adjustedScale) * shady.Height;
            }
            */

            /*
            // Adjust logo scale 2 (for resolutions with aspect ratio < 1.33)
            float logoWidth = logo.Width;
            float logoWidthLimit = mScene.ViewWidth - sideScroll.Width;

            if (logoWidthLimit < logoWidth)
            {
                if (adjustedScale < 0.9f || adjustedScale > 1.01f)
                {
                    // Undo previous adjustments
                    logo.X -= (1f - adjustedScale) * logo.Width;
                    logo.Y -= (1f - adjustedScale) * logo.Height;
                    shady.X -= (1f - adjustedScale) * shady.Width;
                    shady.Y -= (1f - adjustedScale) * shady.Height;
                }

                float shadyOffset = logo.X - shady.X;

                adjustedScale = 0.9f * logoWidthLimit / logoWidth;
                logo.ScaleX = logo.ScaleY = adjustedScale;
                logo.X = ((logoWidthLimit + (adjustedScale * 1.25f * potionsButton.Width)) - logo.Width) / 2;
                logo.Y += (1f - adjustedScale) * logo.Height;

                // Adjust shady scale
                shady.ScaleX = shady.ScaleY = adjustedScale;
                shady.X = logo.X - shadyOffset * adjustedScale;
                shady.Y += (1f - adjustedScale) * shady.Height;
            }
            */

            mLogoInPos = logo.Origin;
            mShadyInPos = shady.Origin;

            CreateExitPrompt();

            if (GameController.GC.IsLogging)
            {
                ConsoleView consoleView = new ConsoleView((int)PFCat.GUIDE, 200, 15, mScene.ViewWidth, mScene.ViewHeight / 2);
                mScene.AddProp(consoleView);
                SpynDoctor.TopScore.View = consoleView;
            }
        }

        public void AttachEventListeners()
        {

        }

        public void DetachEventListeners()
        {

        }

        private void CreateExitPrompt()
        {
            if (mExitPrompt != null)
                return;

            SPImage exitImage = new SPImage(mScene.TextureByName("exit"));
            exitImage.X = -exitImage.Width / 2;

            SPSprite flipSprite = new SPSprite();
            flipSprite.AddChild(exitImage);

            mExitPrompt = new Prop((int)PFCat.SEA);
            mExitPrompt.AddChild(flipSprite);
            mExitPrompt.X = exitImage.Width / 2;
            mExitPrompt.ScaleX = mExitPrompt.ScaleY = 1.1f;
            mExitPrompt.Alpha = 0.85f;
            mScene.AddProp(mExitPrompt);
        }

        public void EnableExitPrompt(bool enable)
        {
            if (mExitPrompt != null)
                mExitPrompt.Visible = enable;
            mScene.IsExitViewAvailable = enable;
        }

        private void TransitionOverTime(float duration, bool inward)
        {
            SPSprite shady = mMenuSubview.ControlForKey("Shady") as SPSprite;
            SPSprite logo = mMenuSubview.ControlForKey("Logo") as SPSprite;
            SPSprite sideScroll = mMenuSubview.ControlForKey("SideScroll") as SPSprite;
    
            mScene.SpecialJuggler.RemoveTweensWithTarget(shady);
            mScene.SpecialJuggler.RemoveTweensWithTarget(logo);
            mScene.SpecialJuggler.RemoveTweensWithTarget(sideScroll);
    
            float maxDuration = -1;
            SPTween eventTween = null;
    
            // Shady
            float shadyOriginX = (inward) ? -shady.Width : mShadyInPos.X;
            float shadyTargetX = (inward) ? mShadyInPos.X : -shady.Width;
            float shadyMaxDist = shadyTargetX - shadyOriginX;
            float shadyActualDist = shadyTargetX - shady.X;
            float shadyDuration = duration * (shadyActualDist / shadyMaxDist);
    
            SPTween shadyTween = new SPTween(shady, shadyDuration);
            shadyTween.AnimateProperty("X", shadyTargetX);
            mScene.SpecialJuggler.AddObject(shadyTween);
    
            maxDuration = shadyDuration;
            eventTween = shadyTween;
    
            // Logo
            float logoOriginY = (inward) ? -logo.Height : mLogoInPos.Y;
            float logoTargetY = (inward) ? mLogoInPos.Y : -logo.Height;
            float logoMaxDist = logoTargetY - logoOriginY;
            float logoActualDist = logoTargetY - logo.Y;
            float logoDuration = duration * (logoActualDist / logoMaxDist);
    
            SPTween logoTween = new SPTween(logo, logoDuration);
            logoTween.AnimateProperty("Y", logoTargetY);
            mScene.SpecialJuggler.AddObject(logoTween);
    
            if (logoDuration > maxDuration)
            {
                maxDuration = logoDuration;
                eventTween = logoTween;
            }
    
            // Side Scroll
            float sideScrollOriginX = (inward) ? mScene.ViewWidth : mScene.ViewWidth - (sideScroll.Width-1);
            float sideScrollTargetX = (inward) ? mScene.ViewWidth - (sideScroll.Width-1) : mScene.ViewWidth;
            float sideScrollMaxDist = sideScrollOriginX - sideScrollTargetX;
            float sideScrollActualDist = sideScroll.X - sideScrollTargetX;
            float sideScrollDuration = duration * (sideScrollActualDist / sideScrollMaxDist);
    
            SPTween sideScrollTween = new SPTween(sideScroll, sideScrollDuration);
            sideScrollTween.AnimateProperty("X", sideScrollTargetX);
            mScene.SpecialJuggler.AddObject(sideScrollTween);
    
            if (sideScrollDuration > maxDuration)
            {
                maxDuration = sideScrollDuration;
                eventTween = sideScrollTween;
            }
            
            eventTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, inward ? (SPEventHandler)OnTransitionedIn : (SPEventHandler)OnTransitionedOut);
        }

        public void OnGamerPicsRefreshed(SPEvent ev)
        {
            if (mLeaderboardView != null)
                mLeaderboardView.OnGamerPicsRefreshed(ev);
            if (mHiScoreCarousel != null)
                mHiScoreCarousel.OnGamerPicsRefreshed(ev);
        }

        public void SplashScreenDidHide()
        {
            if (mTitleScreen != null)
                mTitleScreen.IsSplashShowing = false;
        }

        public void TransitionInOverTime(float duration)
        {
            TransitionOverTime(duration, true);
        }

        public void TransitionOutOverTime(float duration)
        {
            TransitionOverTime(duration, false);
        }

        private void OnTransitionedIn(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MENU_VIEW_DID_TRANSITION_IN));
        }

        private void OnTransitionedOut(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MENU_VIEW_DID_TRANSITION_OUT));
        }

        public override void Flip(bool enable)
        {
            if (mExitPrompt != null && mExitPrompt.NumChildren > 0)
            {
                if (enable)
                {
                    mExitPrompt.X = mScene.ViewWidth - mExitPrompt.Width / 2;
                    mExitPrompt.ChildAtIndex(0).ScaleX = -1;
                }
                else
                    mExitPrompt.X = mExitPrompt.Width / 2;
            }
        }

        public void DisplaySavingProgressPrompt()
        {
            if (mScene == null || mSavingView == null)
                return;

            mSavingView.Alpha = 0.95f;
            mSavingView.Reset(mSavingView.Alpha, 1f, 0.5);
            mScene.RemoveProp(mSavingView, false);
            mScene.AddProp(mSavingView);
            mScene.SpecialJuggler.RemoveObject(mSavingView);
            mScene.SpecialJuggler.AddObject(mSavingView);
        }

        public void HideSavingProgressPrompt(double delay = 0)
        {
            if (mSavingView == null)
                return;

            mSavingView.Reset(mSavingView.Alpha, 0f, 0.5, delay);
        }

        private void OnSavingProgressPromptCompleted()
        {
            if (mSavingView == null)
                return;

            if (mSavingView.TweenedValue == 1f)
                HideSavingProgressPrompt(1.5);
            else
            {
                mScene.RemoveProp(mSavingView, false);
                mScene.SpecialJuggler.RemoveObject(mSavingView);
            }
        }

        public void PlayerLoggedIn(PlayerIndex playerIndex)
        {
            if (mHiScoreCarousel != null)
            {
                mHiScoreCarousel.Clear();
                mHiScoreCarousel.AutoPopulateScorers();
            }
        }

        public void PlayerLoggedOut(PlayerIndex playerIndex)
        {
            if (mHiScoreCarousel != null)
            {
                mHiScoreCarousel.Clear();
                mHiScoreCarousel.AutoPopulateScorers();
            }
        }

        public void UpdateHiScoreText()
        {
            if (mHiScoreCarousel != null)
                mHiScoreCarousel.UpdateScores();
        }

        public void UpdateObjectivesLog()
        {
            mObjectivesLog.Rank = mScene.ObjectivesManager.Rank;
            mObjectivesLog.SyncWithObjectives();
        }

        public void ActivateCurrentSubview(bool reset = false)
        {
            if (CurrentSubview != null)
            {
                if (reset)
                    CurrentSubview.ResetNav();

                if (CurrentSubview.CurrentNav != null && CurrentSubview.CurrentNav is MenuButton)
                    (CurrentSubview.CurrentNav as MenuButton).Selected = true;
            }
        }

        public void DeactivateCurrentSubview()
        {
            if (CurrentSubview != null)
            {
                if (CurrentSubview.CurrentNav != null)
                {
                    if (CurrentSubview.CurrentNav is MenuButton)
                        (CurrentSubview.CurrentNav as MenuButton).Selected = false;
                    if (CurrentSubview.CurrentNav is SPButton)
                        (CurrentSubview.CurrentNav as SPButton).AutomatedButtonRelease(false);
                }
            }
        }

        public TitleSubview SubviewForKey(string key)
        {
            return mSubviews[key];
        }

        private SPDisplayObject DisplayObjectFromSubview(string key, string objectName)
        {
            TitleSubview subview = SubviewForKey(key);
            SPDisplayObject displayObject = subview.ControlForKey(objectName);
            return displayObject;
        }

        public void PushSubviewForKey(string key)
        {
            TitleSubview subview = mSubviews[key];
            PushSubview(subview);
        }

        private void PushSubview(TitleSubview subview, bool playSounds = true)
        {
            if (subview != null && !mSubviewStack.Contains(subview))
            {
                DeactivateCurrentSubview();

		        if (mSubviewStack.Count > 0)
                {
			        TitleSubview top = mSubviewStack[mSubviewStack.Count-1];
			        top.Touchable = false;
                    top.RemoveChild(mCloseSubviewButton);
		        }
		
		        subview.Visible = true;
		        subview.Touchable = true;
		
		        if (subview.CloseSelector != null)
                {
			        CCPoint closePosition = subview.ClosePosition;

			        if (closePosition != null)
                    {
                        mCloseSubviewButton.X = closePosition.X - 15f; //ResManager.RESX(closePosition.X);
                        mCloseSubviewButton.Y = closePosition.Y - 8f; //ResManager.RESY(closePosition.Y);
                        subview.AddChild(mCloseSubviewButton);
			        }
		        }

                mSubviewStack.Add(subview);

                if (playSounds && mSubviewStack.Count > 1)
                    PlayPushSubviewSound();

                while (mSubviewContainerIndex >= mSubviewContainers.Count)
                    mSubviewContainers.Add(new SPSprite());

                SPSprite container = mSubviewContainers[mSubviewContainerIndex++];
                container.ScaleX = container.ScaleY = ScaleForSubview(subview);
                mCloseSubviewButton.ScaleX = mCloseSubviewButton.ScaleY = s_CloseSubviewScale / container.ScaleX;
                subview.X = -mCanvas.X;
                subview.Y = -mCanvas.Y;
                container.AddChild(subview);
                mCanvas.AddChild(container);
	        }

            ActivateCurrentSubview(true);

            if (mController.State == MenuController.MenuState.In)
            {
                if (mSubviewStack.Count > 1)
                    EnableExitPrompt(false);

                if (mSubviewStack.Count == 2)
                {
                    mScene.ReassignDefaultController();

                    if (subview != null)
                    {
                        ControlsManager cm = ControlsManager.CM;

                        if (cm.NumConnectedControllers > 1)
                        {
                            CCPoint guidePos = subview.GuidePos;

                            if (guidePos != null)
                                mScene.ShowGuideProp(cm.PlayerIndexMap, guidePos.X, guidePos.Y, 2f);
                        }
                    }
                }

                subview.AttachGamerPic(mScene.GamerPic);
            }
        }

        private float ScaleForSubview(TitleSubview subview)
        {
            float scale = 1f;

            if (subview != null && subview.DoesScaleToFill)
            {
                float sX = subview.Width / mScene.ViewWidth, sY = subview.Height / mScene.ViewHeight;
                if ((sX > subview.ScaleToFillThreshold || sY > subview.ScaleToFillThreshold) || (sX < subview.ScaleToFillThreshold && sY < subview.ScaleToFillThreshold))
                    scale = 0.95f * subview.ScaleToFillThreshold / Math.Max(sX, sY);
            }

            return scale;
        }

        public void PopSubview(bool playSounds = true)
        {
            if (mSubviewStack.Count > 0)
            {
                DeactivateCurrentSubview();

		        TitleSubview subview = mSubviewStack[mSubviewStack.Count-1];
		        subview.Visible = false;
		        subview.Touchable = false;
                subview.RemoveChild(mCloseSubviewButton);
                subview.DetachGamerPic(mScene.GamerPic);
                //mCanvas.RemoveChild(subview);
                mCanvas.RemoveChildAtIndex(mCanvas.NumChildren - 1);
                mScene.Juggler.RemoveTweensWithTarget(subview);
                mSubviewStack.RemoveAt(mSubviewStack.Count-1);
                --mSubviewContainerIndex;
		
		        if (mSubviewStack.Count > 0)
                {
			        TitleSubview top = mSubviewStack[mSubviewStack.Count-1];
			        top.Touchable = true;
			
			        if (top.CloseSelector != null)
                    {
                        CCPoint closePosition = top.ClosePosition;
                
                        ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);		
                        if (closePosition != null)
                        {
                            mCloseSubviewButton.X = closePosition.X - 15f; //ResManager.RESX(closePosition.X);
                            mCloseSubviewButton.Y = closePosition.Y - 8f; //ResManager.RESY(closePosition.Y);
                        }

                        ResManager.RESM.PopOffset();

                        if (mSubviewContainerIndex > 0)
                        {
                            SPSprite container = mSubviewContainers[mSubviewContainerIndex - 1];
                            mCloseSubviewButton.ScaleX = mCloseSubviewButton.ScaleY = s_CloseSubviewScale / container.ScaleX;
                        }
                        top.AddChild(mCloseSubviewButton);
                    }

                    top.AttachGamerPic(mScene.GamerPic);
		        }

                if (playSounds)
                    PlayPopSubviewSound();
                ActivateCurrentSubview();

                if (mSubviewStack.Count == 1 && mController.State == MenuController.MenuState.In)
                {
                    ControlsManager.CM.SetDefaultPlayerIndex(null);
                    mScene.HideGuideProp();
                    EnableExitPrompt(true);
                }
	        }
        }

        public void PopAllSubviews()
        {
            if (mSubviewStack == null)
                return;

            int repeats = 0, stackCount = mSubviewStack.Count;
            while (stackCount > 1)
            {
                if (mCloseSubviewButton != null)
                {
                    mCloseSubviewButton.AutomatedButtonDepress();
                    mCloseSubviewButton.AutomatedButtonRelease();
                }
                else
                    PopSubview();
                
                // Prevent infinite loop.
                if (stackCount == mSubviewStack.Count)
                {
                    if (repeats >= 2) // Allow for up to 3-deep sub-subviews.
                        break;
                    else
                    {
                        ++repeats;
                        continue;
                    }
                }
                repeats = 0;
                stackCount = mSubviewStack.Count;
            }

            //ControlsManager.CM.SetDefaultPlayerIndex(null);
        }

        public void DestroySubviewForKey(string key)
        {
            if (key == null)
                return;
            TitleSubview subview = null;

            try
            {
                subview = mSubviews[key];

                if (mSubviewStack.Contains(subview))
                    Debug.WriteLine("MenuView: Attempt to destroy a subview while it is still on the stack.");
                mSubviews.Remove(key);
                subview.Dispose();
            }
            catch (Exception) { }
        }

        public override void AdvanceTime(double time)
        {
            if (mController.State == MenuController.MenuState.In)
            {
                TitleSubview currentSubview = CurrentSubview;
                if (currentSubview != null)
                    currentSubview.AdvanceTime(time);
                if (mPotionView != null)
                    mPotionView.AdvanceTime(time);
                if (mOptionsView != null)
                    mOptionsView.AdvanceTime(time);
                if (mHiScoreCarousel != null)
                    mHiScoreCarousel.AdvanceTime(time);
            }

            if (mBuyButton != null)
            {
                bool isTrialMode = GameController.GC.IsTrialMode;
                if (mBuyButton.Visible && mMenuSubview != null && mBuyButton == mMenuSubview.CurrentNav && !isTrialMode)
                {
                    mMenuSubview.ResetNav();
                    if (mMenuSubview != CurrentSubview && mMenuSubview.CurrentNav is MenuButton)
                        (mMenuSubview.CurrentNav as MenuButton).Selected = false;
                }

                mBuyButton.Visible = isTrialMode;
                if (mInviteButton != null)
                    mInviteButton.Visible = !mBuyButton.Visible;
            }
            if (mLeaderboardView != null)
            {
                if (mLeaderboardView.IsTrialMode)
                {
                    if (!GameController.GC.IsTrialMode)
                        mLeaderboardView.IsTrialMode = false;
                }
            }
        }

        public void SetAlertTitle(string title, string text)
        {
            TitleSubview subview = mSubviews["Alert"];
            subview.SetTextForKey(title, "alertTitle");
            subview.SetTextForKey(text, "alertDesc");
        }

        public void SetQueryTitle(string title, string text)
        {
            TitleSubview subview = mSubviews["Query"];
            subview.SetTextForKey(title, "queryTitle");
            subview.SetTextForKey(text, "queryDesc");
        }

        private void PlayPushSubviewSound()
        {
            mScene.PlaySound("PageTurn");
        }

        private void PlayPopSubviewSound()
        {
            mScene.PlaySound("PageTurn");
        }

        public void HidePotionsButton(bool hide)
        {
            MenuButton potionsButton = mMenuSubview.ControlForKey("PotionsButton") as MenuButton;
            potionsButton.Visible = !hide;
        }

        public void ShowTitle()
        {
            if (mTitleScreen != null)
                return;

            mTitleScreen = new TitleScreen((int)PFCat.HUD);

            if (mScene is PlayfieldController)
                mTitleScreen.AddEventListener(TitleScreen.CUST_EVENT_TYPE_TITLE_SCREEN_BEGIN, (SPEventHandler)(mScene as PlayfieldController).OnBeginPressed);
            PushSubview(mTitleScreen, false);
        }

        public void HideTitle()
        {
            if (mTitleScreen != null)
            {
                PopSubview(false);
                if (mScene is PlayfieldController)
                    mTitleScreen.RemoveEventListener(TitleScreen.CUST_EVENT_TYPE_TITLE_SCREEN_BEGIN, (SPEventHandler)(mScene as PlayfieldController).OnBeginPressed);
                mTitleScreen.Dispose();
                mTitleScreen = null;
            }
        }

        public void ShowModeSelect()
        {
            if (mModeSelectView != null)
                mModeSelectView.ShowMenu();
        }

        public bool CloseModeSelect()
        {
            return (mModeSelectView != null && mModeSelectView.CloseMenu());
        }

        public void PopulateOptionsView()
        {
            if (mOptionsView != null || mSubviews == null || !mSubviews.ContainsKey("Options"))
                return;

            TitleSubview subview = mSubviews["Options"];
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            mOptionsView = new OptionsView(0);
            mOptionsView.X = ResManager.RESX(0);
            mOptionsView.Y = ResManager.RESY(0);
            ResManager.RESM.PopOffset();
            subview.AddChild(mOptionsView);

            // Hook events
            List<string> eventsList = OptionsView.EventsList;

            foreach (string eventType in eventsList)
                mOptionsView.AddEventListener(eventType, (SPEventHandler)mController.OnOptionMenuSelection);
        }

        public void UnpopulateOptionsView()
        {
            if (mOptionsView != null)
            {
                // Unhook to events
                List<string> eventsList = OptionsView.EventsList;

                foreach (string eventType in eventsList)
                    mOptionsView.RemoveEventListener(eventType, (SPEventHandler)mController.OnOptionMenuSelection);

                mOptionsView.RemoveFromParent();
                mOptionsView.Dispose();
                mOptionsView = null;
            }
        }

        public void PopulateDisplayAdjustmentView()
        {
            if (mSafeAreaView != null || mSubviews == null || !mSubviews.ContainsKey("Display"))
                return;
            TitleSubview subview = mSubviews["Display"];
            List<TitleSubview> poppedSubviews = new List<TitleSubview>();

            // Don't want Safe Area to Render scroll-backed subviews and thus block the view.
            while (CurrentSubview != mMenuSubview)
            {
                poppedSubviews.Insert(0, CurrentSubview);
                PopSubview(false);
            }

            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            mSafeAreaView = new SafeAreaView(0);
            mSafeAreaView.X = ResManager.RESX(0);
            mSafeAreaView.Y = ResManager.RESY(0);
            mSafeAreaView.AddEventListener(SafeAreaView.CUST_EVENT_TYPE_DISPLAY_ADJUSTMENT_COMPLETED, (SPEventHandler)mController.OnDisplayAdjustmentCompleted);
            mSafeAreaView.AddEventListener(SafeAreaView.CUST_EVENT_TYPE_DISPLAY_ADJUSTED_UP, (SPEventHandler)mController.OnDisplayAdjustedUp);
            mSafeAreaView.AddEventListener(SafeAreaView.CUST_EVENT_TYPE_DISPLAY_ADJUSTED_DOWN, (SPEventHandler)mController.OnDisplayAdjustedDown);
            ResManager.RESM.PopOffset();
            subview.AddChild(mSafeAreaView);

            // Restore subview stack
            foreach (TitleSubview poppedSubview in poppedSubviews)
                PushSubview(poppedSubview, false);
        }

        public void UnpopulateDisplayAdjustmentView()
        {
            if (mSafeAreaView != null && mController != null)
            {
                mSafeAreaView.RemoveEventListener(SafeAreaView.CUST_EVENT_TYPE_DISPLAY_ADJUSTMENT_COMPLETED, (SPEventHandler)mController.OnDisplayAdjustmentCompleted);
                mSafeAreaView.RemoveEventListener(SafeAreaView.CUST_EVENT_TYPE_DISPLAY_ADJUSTED_UP, (SPEventHandler)mController.OnDisplayAdjustedUp);
                mSafeAreaView.RemoveEventListener(SafeAreaView.CUST_EVENT_TYPE_DISPLAY_ADJUSTED_DOWN, (SPEventHandler)mController.OnDisplayAdjustedDown);
                mSafeAreaView.RemoveFromParent();
                mSafeAreaView.Dispose();
                mSafeAreaView = null;
            }
        }

        public void PopulatePotionView()
        {
            if (mPotionView != null || mSubviews == null || !mSubviews.ContainsKey("Potions"))
                return;
            TitleSubview subview = mSubviews["Potions"];
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            mPotionView = new PotionView(0);
            mPotionView.X = ResManager.RESX(0);
            mPotionView.Y = ResManager.RESY(0);
            ResManager.RESM.PopOffset();
            subview.AddChild(mPotionView);
        }

        public void UnpopulatePotionView()
        {
            if (mPotionView != null)
            {
                mPotionView.RemoveFromParent();
                mPotionView.Dispose();
                mPotionView = null;
            }
        }

        public void SelectCurrentPotion()
        {
            if (mPotionView != null)
                mPotionView.SelectCurrentPotion();
        }

        public void PopulateMasteryLog()
        {
            if (mScene.MasteryManager.CurrentModel == null || mMasteryLog != null || mSubviews == null || !mSubviews.ContainsKey("Mastery"))
                return;

            TitleSubview subview = mSubviews["Mastery"];
            mMasteryLog = new MasteryLog(Category, mScene.MasteryManager.CurrentModel);
            subview.AddMiscProp(mMasteryLog);
            subview.AddChild(mMasteryLog);
            mScene.SubscribeToInputUpdates(mMasteryLog);
        }

        public void UnpopulateMasteryLog()
        {
            if (mMasteryLog != null)
            {
                TitleSubview subview = mSubviews["Mastery"];
                subview.RemoveMiscProp(mMasteryLog);
                mScene.UnsubscribeToInputUpdates(mMasteryLog);
                mMasteryLog.RemoveFromParent();
                mMasteryLog.Dispose();
                mMasteryLog = null;
            }
        }

        public bool SendCloseToMasteryLog()
        {
            bool shouldClose = false;

            if (mMasteryLog != null)
            {
                if (mMasteryLog.HasCurrentPage)
                    mMasteryLog.TurnToMenu();
                else
                    shouldClose = true;
            }

            return shouldClose;
        }

        public void PopulateAchievementsView()
        {
            if (mAchievementsView != null || mSubviews == null || !mSubviews.ContainsKey("Achievements"))
                return;

            TitleSubview subview = mSubviews["Achievements"];
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);

            float width = 560, height = 128 * 3;
            mAchievementsView = new TableView(0, width, height);
            mAchievementsView.BeginBatchAdd();

            for (int i = 0; i <= AchievementManager.ACHIEVEMENT_COUNT; ++i)
            {
                SPDisplayObject cell = mScene.AchievementManager.AchievementCellForIndex(i, mScene);

                if (cell != null)
                {
                    mAchievementsView.AddCell(cell);

                    if (cell is SPDisplayObjectContainer)
                    {
                        SPDisplayObject speedboatDisplayObject = (cell as SPDisplayObjectContainer).ChildForTag(AchievementManager.k88_MPH_BUTTON_TAG);

                        if (speedboatDisplayObject != null && speedboatDisplayObject is MenuButton)
                        {
                            MenuButton speedboatButton = speedboatDisplayObject as MenuButton;
                            mAchievementsView.AddButton(speedboatButton, Buttons.A);
                            speedboatButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)mController.OnSpeedboatLaunchRequested);
                        }
                    }
                }
            }

            mAchievementsView.EndBatchAdd();
            mAchievementsView.X = ResManager.RESX(200);
            mAchievementsView.Y = ResManager.RESY(158);
            ResManager.RESM.PopOffset();

            SPDisplayObject achScroll = subview.ControlForKey("Scroll");

            if (achScroll != null)
                subview.AddChildAtIndex(mAchievementsView, subview.ChildIndex(achScroll) + 1);
            else
                subview.AddChild(mAchievementsView);

            mAchievementsView.UpdateViewport();
            mAchievementsView.InputFocus = InputManager.HAS_FOCUS_MENU_ACHIEVEMENTS;
            mScene.SubscribeToInputUpdates(mAchievementsView);
        }

        public void UnpopulateAchievementsView()
        {
            if (mAchievementsView != null)
            {
                mScene.UnsubscribeToInputUpdates(mAchievementsView);
                mAchievementsView.RemoveFromParent();
                mAchievementsView.Dispose();
                mAchievementsView = null;
            }
        }

        public void PopulateStatsView()
        {
            if (mStatsView != null || mSubviews == null || !mSubviews.ContainsKey("StatsLog"))
                return;

            TitleSubview subview = mSubviews["StatsLog"];
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            float width = 560, height = 64 * 6;
            mStatsView = new TableView(0, width, height);
            mStatsView.BeginBatchAdd();

            for (int i = 0; i < GameStats.NumProfileStats; ++i)
                mStatsView.AddCell(mScene.AchievementManager.StatsCellForIndex(i, mScene));

            mStatsView.EndBatchAdd();
            mStatsView.X = ResManager.RESX(200);
            mStatsView.Y = ResManager.RESY(158);
            ResManager.RESM.PopOffset();

            SPDisplayObject statsScroll = subview.ControlForKey("Scroll");
            if (statsScroll != null)
                subview.AddChildAtIndex(mStatsView, subview.ChildIndex(statsScroll) + 1);
            else
                subview.AddChild(mStatsView);

            mStatsView.UpdateViewport();
            mStatsView.InputFocus = InputManager.HAS_FOCUS_MENU_INFO_STATS;
            mScene.SubscribeToInputUpdates(mStatsView);
        }

        public void UnpopulateStatsView()
        {
            if (mStatsView != null)
            {
                mScene.UnsubscribeToInputUpdates(mStatsView);
                mStatsView.RemoveFromParent();
                mStatsView.Dispose();
                mStatsView = null;
            }
        }

        public int MaxIndexLocalLeaderboard()
        {
            return 100;
        }

        public int MaxIndexFriendsLeaderboard()
        {
            return 100;
        }

        public int MaxIndexGlobalLeaderboard()
        {
            return GameController.GC.LiveLeaderboard != null ? GameController.GC.LiveLeaderboard.NumTopScores : 0;
        }

        public HiScoreTable TableContentsLocalLeaderboard(int pageIndex, int numScores)
        {
            return mScene.AchievementManager.HiScores;
        }

        public HiScoreTable TableContentsFriendsLeaderboard(int pageIndex, int numScores)
        {
            return GameController.GC.LiveLeaderboard != null 
                ? GameController.GC.LiveLeaderboard.TopFriendsScores(numScores, SignedInGamer.SignedInGamers[GameController.GC.ProfileManager.MainPlayerIndex])
                : new HiScoreTable(mScene.FontKey, numScores);
        }

        public HiScoreTable TableContentsGlobalLeaderboard(int pageIndex, int numScores)
        {
            return GameController.GC.LiveLeaderboard != null ? GameController.GC.LiveLeaderboard.TopScores(pageIndex, numScores) : new HiScoreTable(mScene.FontKey, numScores);
        }

        public void PopulateLeaderboardView()
        {
            if (mLeaderboardView != null || mSubviews == null || !mSubviews.ContainsKey("Leaderboard"))
                return;

            // Highlight local scorers
            string defaultAlias = GameStats.DefaultAlias;
            List<string> localNames = GameController.GC.ProfileManager.OnlineProfileNames;
            Dictionary<string, string> localScorers = new Dictionary<string, string>(4);
            if (localNames != null && localNames.Count > 0)
            {
                foreach (string name in localNames)
                {
                    if (name != defaultAlias)
                        localScorers[name] = name;
                }
            }

            mLeaderboardView = new LeaderboardView(0, ControlsManager.CM.MainPlayerIndex, 560, 64 * 5, 100);

            TitleSubview subview = mSubviews["Leaderboard"];
            SPDisplayObject hiScoreScroll = subview.ControlForKey("Scroll");
            if (hiScoreScroll != null)
                subview.AddChildAtIndex(mLeaderboardView, subview.ChildIndex(hiScoreScroll) + 1);
            else
                subview.AddChild(mLeaderboardView);

            mLeaderboardView.AddScope("Local", "lb-local", false, false, MaxIndexLocalLeaderboard, TableContentsLocalLeaderboard, null);
            mLeaderboardView.AddScope("Friends", "lb-friends", true, true, MaxIndexFriendsLeaderboard, TableContentsFriendsLeaderboard, localScorers);
            mLeaderboardView.AddScope("Global", "lb-global", true, false, MaxIndexGlobalLeaderboard, TableContentsGlobalLeaderboard, localScorers);
            mLeaderboardView.IsTrialMode = GameController.GC.IsTrialMode;
        }

        public void UnpopulateLeaderboardView()
        {
            if (mLeaderboardView != null)
            {
                mLeaderboardView.RemoveFromParent();
                mLeaderboardView.Dispose();
                mLeaderboardView = null;
            }
        }

        public void DidGainFocus()
        {
            ActivateCurrentSubview();
        }

        public void WillLoseFocus()
        {
            DeactivateCurrentSubview();
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            ControlsManager cm = ControlsManager.CM;

            if (GameController.GC.IsLogging && cm.DidButtonRelease(Buttons.Y) && SpynDoctor.TopScore.View != null)
                SpynDoctor.TopScore.View.Show();

            if (mScene.HasInputFocus(InputManager.HAS_FOCUS_MENU) && cm.DidButtonRelease(Buttons.Start))
            {
                DeactivateCurrentSubview();
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MENU_VIEW_START_TO_PLAY));
            }
            else if (CurrentSubview != null)
            {
                CurrentSubview.Update(gpState, kbState);

                if (mCloseSubviewButton != null && CurrentSubview != mMenuSubview)
                {
                    if (cm.DidButtonDepress(Buttons.B))
                        mCloseSubviewButton.AutomatedButtonDepress();
                    else if (cm.DidButtonRelease(Buttons.B))
                        mCloseSubviewButton.AutomatedButtonRelease();
                }
            }
        }

        private BookletSubview LoadBookletSubviewForKey(string key)
        {
	        if (mViewParser == null)
            {
		        mViewParser = new ViewParser(mScene, mController, (SPEventHandler)mController.OnButtonTriggered, "data/plists/Title.plist");
		        mViewParser.Category = Category;
		        mViewParser.FontKey = mScene.FontKey;
	        }
	
            BookletSubview subview = null;
	        Dictionary<string, object> dict = mViewParser.ViewData;

            try
            {
                if (!dict.ContainsKey(key))
                    return null;
                dict = dict[key] as Dictionary<string, object>;

                List<object> pages = dict["Pages"] as List<object>;

                subview = new BookletSubview(Category, key);
	            subview.Cover = mViewParser.ParseTitleSubviewByName("Cover", key);
                subview.DoesScaleToFill = subview.Cover.DoesScaleToFill;
                subview.ScaleToFillThreshold = subview.Cover.ScaleToFillThreshold;
                subview.ClosePosition = subview.Cover.ClosePosition;
	            subview.CurrentPage = mViewParser.ParseSubviewByName("Pages", key, 0);
	            subview.NumPages = pages.Count;
                subview.RefreshPageNo();
                subview.AddEventListener(BookletSubview.CUST_EVENT_TYPE_BOOKLET_PAGE_TURNED, (SPEventHandler)OnBookletPageTurned);
                mSubviews[key] = subview;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to load booklet subview for key: " + key + ". " + e.Message);
            }

            return subview;
        }

        public BookletSubview BookletSubviewForKey(string key)
        {
            BookletSubview subview = null;

            if (mSubviews.ContainsKey(key))
                subview = mSubviews[key] as BookletSubview;

	        if (subview == null)
            {
		        subview = LoadBookletSubviewForKey(key);

                if (subview != null)
                    mSubviews[key] = subview;
	        }

	        return subview;
        }

        private void OnBookletPageTurned(SPEvent ev)
        {
            BookletSubview subview = ev.CurrentTarget as BookletSubview;
            MenuDetailView page = mViewParser.ParseSubviewByName("Pages", subview.BookKey, subview.PageIndex);
            subview.CurrentPage = page;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mScene.UnsubscribeToInputUpdates(this);

                        if (mSubviews != null)
                        {
                            if (mSubviews.ContainsKey("Credits"))
                            {
                                BookletSubview subview = mSubviews["Credits"] as BookletSubview;
                                subview.RemoveEventListener(BookletSubview.CUST_EVENT_TYPE_BOOKLET_PAGE_TURNED, (SPEventHandler)OnBookletPageTurned);
                            }
                        }

                        if (mTitleScreen != null)
                        {
                            if (mScene is PlayfieldController)
                                mTitleScreen.RemoveEventListener(TitleScreen.CUST_EVENT_TYPE_TITLE_SCREEN_BEGIN, (SPEventHandler)(mScene as PlayfieldController).OnBeginPressed);
                            mTitleScreen.Dispose();
                            mTitleScreen = null;
                        }

                        if (mSavingView != null)
                        {
                            mSavingView.TweenComplete = null;
                            mScene.SpecialJuggler.RemoveObject(mSavingView);
                            mScene.RemoveProp(mSavingView);
                            mSavingView = null;
                        }

                        mCloseSubviewButton = null;
                        mMenuSubview = null;
                        mObjectivesLog = null;
                        mCanvas = null;
                        mViewParser = null;

                        UnpopulatePotionView();
                        UnpopulateAchievementsView();
                        UnpopulateDisplayAdjustmentView();
                        UnpopulateMasteryLog();
                        UnpopulateOptionsView();
                        UnpopulateStatsView();
                        UnpopulateLeaderboardView();

                        mSubviews = null;
                        mSubviewStack = null;
                        mController = null;
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
