using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class PotionView : Prop, IInteractable
    {
        private const float kOffsetX = 20f;

        public PotionView(int category)
            : base(category)
        {
            Touchable = true;
            mPotionWasSelected = false;
            mAnimatedPotionSprite = null;
            mSelectButton = null;
            mCostume = null;
            mJuggler = new SPJuggler();
            SetupProp();

            mScene.SubscribeToInputUpdates(this);
        }
        
        #region Fields
        private bool mPotionWasSelected;
        private Dictionary<string, List<SPTextField>> mPotionLabels;

        private SPImage mSelectedPotionTick;
        private SPSprite mAnimatedPotionSprite;
        private SPSprite mSelectedPotionSprite;
        private SPSprite mPotionTips;
        private SpriteCarousel mPotionCarousel;
        private MenuButton mSelectButton;
        private SPSprite mCostume;

        private SPJuggler mJuggler;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_MENU_POTIONS; } }
        public bool PotionWasSelected { get { return mPotionWasSelected; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;
    
            mCostume = new SPSprite();
            AddChild(mCostume);
    
            // Labels
            List<Potion> potions = Potion.NewPotionList;
            List<SPTextField> potionTitles = new List<SPTextField>(potions.Count);
            List<SPTextField> potionRanks = new List<SPTextField>(potions.Count);
            List<SPTextField> potionDescs = new List<SPTextField>(potions.Count);
    
            foreach (Potion potion in potions)
            {
                SPTextField title = new SPTextField(340, 56, "Vial of " + Potion.NameForKey(potion.Key), mScene.FontKey, 38);
                title.X = 130 + kOffsetX;
                title.Y = 314;
                title.Color = Color.Black;
                title.HAlign = SPTextField.SPHAlign.Center;
                title.VAlign = SPTextField.SPVAlign.Top;
                potionTitles.Add(title);
                mCostume.AddChild(title);
        
                SPTextField reqRank = new SPTextField(232, 40, Potion.RequiredRankStringForPotion(potion), mScene.FontKey, 24);
                reqRank.X = 184 + kOffsetX;
                reqRank.Y = 368;
                reqRank.Color = SPUtils.ColorFromColor((Potion.RequiredRankForPotion(potion) > mScene.ObjectivesManager.Rank) ? (uint)0xcf0000 : 0);
                reqRank.HAlign = SPTextField.SPHAlign.Center;
                reqRank.VAlign = SPTextField.SPVAlign.Top;
                potionRanks.Add(reqRank);
                mCostume.AddChild(reqRank);
        
                SPTextField desc = new SPTextField(480, 84, Potion.DescForPotion(potion), mScene.FontKey, 28);
                desc.X = 72 + kOffsetX;
                desc.Y = 410;
                desc.Color = Color.Black;
                desc.HAlign = SPTextField.SPHAlign.Center;
                desc.VAlign = SPTextField.SPVAlign.Top;
                potionDescs.Add(desc);
                mCostume.AddChild(desc);
            }
    
            mPotionLabels = new Dictionary<string,List<SPTextField>>() { {"PotionTitle", potionTitles}, {"PotionRank", potionRanks}, {"PotionDesc", potionDescs} };
    
            // Carousel
            mPotionCarousel = new SpriteCarousel(0, 296 + kOffsetX, 204, 120 + ResManager.RITMFX(140), 48 + ResManager.RITMFY(130));
            mPotionCarousel.Touchable = true;
            mPotionCarousel.AddEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_SPRITE_CAROUSEL_INDEX_CHANGED, (NumericValueChangedEventHandler)OnPotionCarouselIndexChanged);
            mCostume.AddChild(mPotionCarousel);
    
            SPTexture lockedTexture = mScene.TextureByName("locked");
    
            foreach (Potion potion in potions)
            {
                SPSprite potionSprite = GuiHelper.PotionSpriteWithPotion(potion, GuiHelper.GuiHelperSize.Lge, mScene);
                SPImage lockedImage = new SPImage(lockedTexture);
                lockedImage.Scale = new Vector2(0.8f, 0.8f);
                lockedImage.X = -lockedImage.Width / 2;
                lockedImage.Y = potionSprite.Height / 2 - lockedImage.Height;
                lockedImage.Visible = Potion.RequiredRankForPotion(potion) > mScene.ObjectivesManager.Rank;
                potionSprite.AddChild(lockedImage);
                mPotionCarousel.BatchAddSprite(potionSprite);
            }
    
            mPotionCarousel.BatchAddCompleted();
            mPotionCarousel.ScaleX = mPotionCarousel.ScaleY = 1.0f;
    
            // Selected Potion Tick
            mSelectedPotionTick = new SPImage(mScene.TextureByName("good-point"));
            mSelectedPotionTick.X = 456;
            mSelectedPotionTick.Y = 314;
            mSelectedPotionTick.Visible = false;
            mCostume.AddChild(mSelectedPotionTick);
    
            // Tips
            GameController gc = GameController.GC;
            int tipCount = GameSettings.GS.ValueForKey(GameSettings.POTION_TIPS_INTRO);
    
            if (tipCount < 2)
            {
                GameSettings.GS.SetValueForKey(GameSettings.POTION_TIPS_INTRO, tipCount + 1);
        
                mPotionTips = new SPSprite();
                mCostume.AddChild(mPotionTips);
        
                SPImage bubble = new SPImage(mScene.TextureByName("speech-bubble"));
                bubble.Y = bubble.Height;
                bubble.ScaleY = -1;
                mPotionTips.X = 470;
                mPotionTips.Y = 308;
                mPotionTips.Alpha = 0;
                mPotionTips.AddChild(bubble);

                SPTextField text = new SPTextField(0.85f * bubble.Width, 0.8f * bubble.Height, "Potions provide passive benefits. You cannot change potions while at sea.", mScene.FontKey, 26);
                text.X = 40;
                text.Y = 70;
                text.Color = Color.Black;
                text.HAlign = SPTextField.SPHAlign.Center;
                text.VAlign = SPTextField.SPVAlign.Top;
                mPotionTips.AddChild(text);
        
                SPTween fadeInTween = new SPTween(mPotionTips, 1);
                fadeInTween.AnimateProperty("Alpha", 1);
                fadeInTween.Delay = 0.5f;
                mJuggler.AddObject(fadeInTween);
        
                SPTween fadeOutTween = new SPTween(mPotionTips, 1);
                fadeOutTween.AnimateProperty("Alpha", 0);
                fadeOutTween.Delay = fadeInTween.Delay + fadeInTween.TotalTime + 8.0f;
                mJuggler.AddObject(fadeOutTween);
            }

            // Select Button
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            mSelectButton = new MenuButton(null, mScene.TextureByName("select-button"));
            mSelectButton.X = (potionTitles != null && potionTitles.Count > 0) ? potionTitles[0].X + potionTitles[0].Width / 2 - mSelectButton.Width / 2 : ResManager.RESX(100);
            mSelectButton.Y = (potionDescs != null && potionDescs.Count > 0) ? potionDescs[0].Y + potionDescs[0].Height + mSelectButton.Height / 2 : ResManager.RESY(480);
            mSelectButton.Selected = true;
            mSelectButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnPotionSelected);
            mCostume.AddChild(mSelectButton);
            ResManager.RESM.PopOffset();

            UpdateWithIndex(mPotionCarousel.DisplayIndex);
            UpdateSelectedPotionSprite();
        }

        private void UpdateSelectedPotionSprite()
        {
            int i = 0;

            if (mSelectedPotionSprite != null)
            {
                for (i = 0; i < mSelectedPotionSprite.NumChildren; ++i)
                {
                    SPDisplayObject child = mSelectedPotionSprite.ChildAtIndex(i);
                    mJuggler.RemoveTweensWithTarget(child);
                }

                mSelectedPotionSprite.RemoveFromParent();
                mSelectedPotionSprite = null;
            }
    
            mSelectedPotionSprite = new SPSprite();
    
            List<Potion> activePotions = mScene.ActivePotions;
            i = 0;
    
            foreach (Potion potion in activePotions)
            {
                SPSprite potionSprite = GuiHelper.PotionSpriteWithPotion(potion, GuiHelper.GuiHelperSize.Med, mScene);
                potionSprite.X = i * (potionSprite.Width + 16);
                mSelectedPotionSprite.AddChild(potionSprite);
                ++i;
            }

            if (mPotionCarousel != null)
            {
                mSelectedPotionSprite.X = mPotionCarousel.X + 430 - mSelectedPotionSprite.Width / 2;
                mSelectedPotionSprite.Y = mPotionCarousel.Y + 10;
            }
            else
            {
                mSelectedPotionSprite.X = 750 - mSelectedPotionSprite.Width / 2;
                mSelectedPotionSprite.Y = 220;
            }

            mCostume.AddChild(mSelectedPotionSprite);
        }

        private void UpdateSelectedPotionSpriteOverTime(float duration)
        {
            if (mAnimatedPotionSprite != null || mPotionCarousel == null)
                return;
    
            SPDisplayObject leftmostPotionBottle = null;
    
            for (int i = 0; i < mSelectedPotionSprite.NumChildren; ++i)
            {
                SPDisplayObject potionBottle = mSelectedPotionSprite.ChildAtIndex(i);
                mJuggler.RemoveTweensWithTarget(potionBottle);
        
                if (leftmostPotionBottle == null)
                    leftmostPotionBottle = potionBottle;
        
                if (i < (mSelectedPotionSprite.NumChildren-1))
                {
                    SPTween tween = new SPTween(potionBottle, duration);
                    tween.AnimateProperty("X", (i + 1) * (potionBottle.Width + 16));
                    mJuggler.AddObject(tween);
                }
                else
                {
                    SPTween tween = new SPTween(potionBottle, duration);
                    tween.AnimateProperty("X", (i + 1) * (potionBottle.Width + 16));
                    mJuggler.AddObject(tween);
            
                    tween = new SPTween(potionBottle, duration, SPTransitions.SPEaseIn);
                    tween.AnimateProperty("Alpha", 0);
                    mJuggler.AddObject(tween);
                }
            }
    
            List<Potion> activePotions = mScene.ActivePotions;;

            if (activePotions.Count > 0 && leftmostPotionBottle != null)
            {
                Potion potion = activePotions[0];
                mAnimatedPotionSprite = GuiHelper.PotionSpriteWithPotion(potion, GuiHelper.GuiHelperSize.Lge, mScene);
                int index = mPotionCarousel.DisplayIndex;
                Vector2 point = GlobalToLocal(mPotionCarousel.SpritePositionAtIndex(index));
                mAnimatedPotionSprite.X = point.X;
                mAnimatedPotionSprite.Y = point.Y;
                mCostume.AddChild(mAnimatedPotionSprite);
        
                point = new Vector2(leftmostPotionBottle.X, leftmostPotionBottle.Y);
                point = leftmostPotionBottle.LocalToGlobal(point);
                point = GlobalToLocal(point);
        
                SPTween tween = new SPTween(mAnimatedPotionSprite, duration);
                tween.AnimateProperty("X", point.X);
                tween.AnimateProperty("Y", point.Y);
                tween.AnimateProperty("ScaleX", leftmostPotionBottle.Width / Math.Max(32,mAnimatedPotionSprite.Width));
                tween.AnimateProperty("ScaleY", leftmostPotionBottle.Height / Math.Max(48,mAnimatedPotionSprite.Height));
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnUpdatedSelectedPotionSprite);
                mJuggler.AddObject(tween);
            }
        }

        private void OnUpdatedSelectedPotionSprite(SPEvent ev)
        {
            if (mAnimatedPotionSprite != null)
            {
                mAnimatedPotionSprite.RemoveFromParent();
                mAnimatedPotionSprite = null;
            }

            UpdateSelectedPotionSprite();
            mScene.PlaySound("PotionClink");
        }

        public void UpdateWithIndex(int index)
        {
            List<SPTextField> potionTitles = mPotionLabels["PotionTitle"];
            List<SPTextField> potionRanks = mPotionLabels["PotionRank"];
            List<SPTextField> potionDescs = mPotionLabels["PotionDesc"];
            List<uint> potionKeys = Potion.PotionKeys;
    
            foreach (SPTextField label in potionTitles)
                label.Visible = false;
            foreach (SPTextField label in potionRanks)
                label.Visible = false;
            foreach (SPTextField label in potionDescs)
                label.Visible = false;
    
            if (index >= 0 && index < potionKeys.Count)
            {
                uint potionKey = potionKeys[index];
                Potion potion = mScene.PotionForKey(potionKey);
                mSelectedPotionTick.Visible = potion.IsActive;
        
                if (index < potionTitles.Count)
                {
                    SPTextField label = potionTitles[index];
                    float textBoundsWidth = label.TextBounds.Width;
                    mSelectedPotionTick.X = 10 + label.X + textBoundsWidth + (label.Width - textBoundsWidth) / 2;
                    label.Visible = true;
                }
        
                if (index < potionRanks.Count)
                {
                    SPTextField label = potionRanks[index];
                    label.Visible = true;
                }
        
                if (index < potionDescs.Count)
                {
                    SPTextField label = potionDescs[index];
                    label.Visible = true;
                }
            }
        }

        public void SelectCurrentPotion()
        {
            if (mPotionCarousel == null || mAnimatedPotionSprite != null)
                return;
    
            int index = mPotionCarousel.DisplayIndex;
            List<uint> potionKeys = Potion.PotionKeys;
    
            if (index >= 0 && index < potionKeys.Count)
            {
                uint potionKey = potionKeys[index];
                Potion potion = mScene.PotionForKey(potionKey);
        
                if (!potion.IsActive)
                {
                    if (Potion.RequiredRankForPotion(potion) > mScene.ObjectivesManager.Rank)
                    {
                        mScene.PlaySound("Locked");
                    }
                    else
                    {
                        GameController.GC.GameStats.ActivatePotion(true, potionKey);
                        UpdateSelectedPotionSpriteOverTime(0.75f);
                        UpdateWithIndex(index);
                        mPotionWasSelected = true;
                    }
                }
            }
        }

        private void OnPotionCarouselIndexChanged(NumericValueChangedEvent ev)
        {
            UpdateWithIndex(ev.IntValue);
        }

        private void OnPotionSelected(SPEvent ev)
        {
            mScene.PlaySound("Button");
            SelectCurrentPotion();
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            ControlsManager cm = ControlsManager.CM;

            if (mPotionCarousel != null)
                mPotionCarousel.Update(gpState, kbState);
            if (cm.DidButtonDepress(Buttons.A))
                mSelectButton.AutomatedButtonDepress();
            else if (cm.DidButtonRelease(Buttons.A))
                mSelectButton.AutomatedButtonRelease();
        }

        public override void AdvanceTime(double time)
        {
            mPotionCarousel.AdvanceTime(time);
            mJuggler.AdvanceTime(time);
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

                        if (mSelectButton != null)
                        {
                            mSelectButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnPotionSelected);
                            mSelectButton = null;
                        }

                        if (mJuggler != null)
                        {
                            if (mSelectedPotionSprite != null)
                            {
                                for (int i = 0; i < mSelectedPotionSprite.NumChildren; ++i)
                                {
                                    SPDisplayObject child = mSelectedPotionSprite.ChildAtIndex(i);
                                    mJuggler.RemoveTweensWithTarget(child);
                                }
                            }

                            if (mAnimatedPotionSprite != null)
                                mJuggler.RemoveTweensWithTarget(mAnimatedPotionSprite);
                            if (mPotionTips != null)
                                mJuggler.RemoveTweensWithTarget(mPotionTips);

                            mJuggler.RemoveAllObjects();
                            mJuggler = null;
                        }

                        if (mPotionCarousel != null)
                        {
                            mPotionCarousel.RemoveEventListener(NumericValueChangedEvent.CUST_EVENT_TYPE_SPRITE_CAROUSEL_INDEX_CHANGED,
                                (NumericValueChangedEventHandler)OnPotionCarouselIndexChanged);
                            mPotionCarousel = null;
                        }

                        mPotionLabels = null;
                        mSelectedPotionTick = null;
                        mSelectedPotionSprite = null;
                        mAnimatedPotionSprite = null;
                        mPotionTips = null;
                        mCostume = null;
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
