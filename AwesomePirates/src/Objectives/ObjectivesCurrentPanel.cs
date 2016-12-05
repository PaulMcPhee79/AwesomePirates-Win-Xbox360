using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class ObjectivesCurrentPanel : Prop
    {
        public const string CUST_EVENT_TYPE_OBJECTIVES_CURRENT_PANEL_CONTINUED = "objectivesCurrentPanelContinuedEvent";

        private const uint kObjQuotaCompleteColor = 0x007e00;
        private const uint kObjQuotaIncompleteColor = 0xbd3100;
        private const uint kObjQuotaFailedColor = 0x9e1319;
        
        public ObjectivesCurrentPanel(int category)
            : base(category)
        {
            Touchable = true;
            SetupProp();
        }

        #region Fields
        private SPTexture mTickTexture;
        private SPTexture mCrossTexture;

        private SPSprite mMaxRankSprite;
        private SPSprite mGamerPicSprite;
        private SPSprite mScrollSprite;
        private SPSprite mCanvasContent;
        private SPSprite mObjectivesContent;
        private SPSprite mCanvas;
        private SPSprite mCanvasScaler;
        private SPButton mContinueButton;
        private ObjectivesHat mHat;

        private List<SPImage> mIcons;
        private List<SPTextField> mDescriptions;
        private List<SPTextField> mQuotas;
        private List<SPTextField> mFails;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvas != null)
                return;
            mCanvasScaler = new SPSprite();
            mCanvasScaler.X = mScene.ViewWidth / 2 - X;
            mCanvasScaler.Y = mScene.ViewHeight / 2 - Y;
            AddChild(mCanvasScaler);

            mCanvas = new SPSprite();
            mCanvasScaler.AddChild(mCanvas);
    
            mTickTexture = mScene.TextureByName("objectives-tick");
            mCrossTexture = mScene.TextureByName("objectives-cross");
    
            // Decorations
            SPTexture scrollTexture = GuiHelper.CachedScrollTextureByName("scroll-quarter-large", mScene);
            SPImage scrollImage = new SPImage(scrollTexture);
            mScrollSprite = new SPSprite();
            mScrollSprite.AddChild(scrollImage);
            mScrollSprite.ScaleX = 620.0f / scrollImage.Width;
            mScrollSprite.ScaleY = 680.0f / scrollImage.Width;
            mScrollSprite.X = 2 * 90;
            mScrollSprite.Y = 2 * 32;
            mCanvas.AddChild(mScrollSprite);

            // Gamer Picture Sprite
            mGamerPicSprite = new SPSprite();
            mGamerPicSprite.X = mScrollSprite.X;
            mGamerPicSprite.Y = mScrollSprite.Y;
            mGamerPicSprite.ScaleX = mGamerPicSprite.ScaleY = mScrollSprite.ScaleX;
            mCanvas.AddChild(mGamerPicSprite);
    
            // Content
            mObjectivesContent = new SPSprite();
            mCanvas.AddChild(mObjectivesContent);
    
            mCanvasContent = new SPSprite();
            mObjectivesContent.AddChild(mCanvasContent);

            SPImage titleImage = new SPImage(mScene.TextureByName("objectives-text"));
            titleImage.X = 2 * 200;
            titleImage.Y = 2 * 41;
            mCanvasContent.AddChild(titleImage);

            // Icons
            mIcons = new List<SPImage>(ObjectivesRank.kNumObjectivesPerRank);

            for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
            {
                SPImage iconImage = new SPImage(mCrossTexture);
                iconImage.X = 2 * 121;
                iconImage.Y = 2 * (88 + i * 54);
                mCanvasContent.AddChild(iconImage);
                mIcons.Add(iconImage);
            }
    
            // Description TextFields
            mDescriptions = new List<SPTextField>(ObjectivesRank.kNumObjectivesPerRank);

            for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
            {
                SPTextField textField = new SPTextField(386, 64, "", mScene.FontKey, 26);
                textField.X = 2 * 143;
                textField.Y = 2 * (80 + i * 54);
                textField.HAlign = SPTextField.SPHAlign.Left;
                textField.VAlign = SPTextField.SPVAlign.Center;
                textField.Color = Color.Black;
                mCanvasContent.AddChild(textField);
                mDescriptions.Add(textField);
            }
    
            // Quota TextFields
            mQuotas = new List<SPTextField>(ObjectivesRank.kNumObjectivesPerRank);

            for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
            {
                SPTextField textField = new SPTextField(100, 32, "", mScene.FontKey, 26);
                textField.X = 2 * 338;
                textField.Y = 2 * (90 + i * 54);
                textField.HAlign = SPTextField.SPHAlign.Center;
                textField.VAlign = SPTextField.SPVAlign.Center;
                textField.Color = SPUtils.ColorFromColor(kObjQuotaCompleteColor);
                mCanvasContent.AddChild(textField);
                mQuotas.Add(textField);
            }
    
            // Failed TextFields
            mFails = new List<SPTextField>(ObjectivesRank.kNumObjectivesPerRank);

            for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
            {
                SPTextField textField = new SPTextField(100, 28, "Failed", mScene.FontKey, 22);
                textField.X = 2 * 338;
                textField.Y = 2 * (104 + i * 54);
                textField.HAlign = SPTextField.SPHAlign.Center;
                textField.VAlign = SPTextField.SPVAlign.Center;
                textField.Color = SPUtils.ColorFromColor(kObjQuotaFailedColor);
                textField.Visible = false;
                mCanvasContent.AddChild(textField);
                mFails.Add(textField);
            }
    
            // Hat
            mHat = new ObjectivesHat(-1, ObjectivesHat.HatType.Angled, mScene.ObjectivesManager.RankLabel);
            mHat.X = 238;
            mHat.Y = 10 + mHat.Height / 2;
            mObjectivesContent.AddChild(mHat);
    
            // Button
            mContinueButton = new SPButton(mScene.TextureByName("continue-button"));
            mContinueButton.X = 2 * 212;
            mContinueButton.Y = 2 * 213;
            mContinueButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnContinuePressed);
            mCanvasContent.AddChild(mContinueButton);

            mCanvas.X = -(mScrollSprite.X + mScrollSprite.Width / 2);
            mCanvas.Y = -(mScrollSprite.Y + mScrollSprite.Height / 2);
            mCanvasScaler.ScaleX = mCanvasScaler.ScaleY = mScene.ScaleForUIView(mScrollSprite, 1f, 0.7f);
        }

        public void AttachGamerPic(SPDisplayObject gamerPic)
        {
            if (gamerPic != null && mGamerPicSprite != null)
            {
                gamerPic.X = 20; // 40;
                gamerPic.Y = 16; // 22;
                mGamerPicSprite.AddChild(gamerPic);
            }
        }

        public void DetachGamerPic(SPDisplayObject gamerPic)
        {
            if (gamerPic != null && mGamerPicSprite != null)
                mGamerPicSprite.RemoveChild(gamerPic);
        }

        private void SetObjectiveCompletedIcon(bool completed, int index)
        {
            if (index < mIcons.Count)
                mIcons[index].Texture = (completed) ? mTickTexture : mCrossTexture;
        }

        private void SetDescriptionText(string text, int index)
        {
            if (index < mDescriptions.Count)
            {
                SPTextField textField = mDescriptions[index];
        
                if (!textField.Text.Equals(text))
                    textField.Text = text;
            }
        }

        private void SetQuotaText(string text, uint color, int index)
        {
            if (index < mQuotas.Count)
            {
                SPTextField textField = mQuotas[index];
                if (!textField.Text.Equals(text))
                    textField.Text = text;
                textField.Color = SPUtils.ColorFromColor(color);
            }
        }

        private void SetFailed(bool failed, int index)
        {
            if (index < mFails.Count)
                mFails[index].Visible = failed;
        }

        public void EnabledButtons(bool enable)
        {
            mContinueButton.Visible = enable;
        }

        public void PopulateWithObjectivesRank(ObjectivesRank objRank)
        {
            if (objRank == null)
                return;
    
            if (objRank.IsMaxRank)
            {
                if (mMaxRankSprite == null)
                {
                    mMaxRankSprite = MaxRankSprite();
                    mMaxRankSprite.X = mScrollSprite.X + (mScrollSprite.Width - mMaxRankSprite.Width) / 2;
                    mMaxRankSprite.Y = mScrollSprite.Y + mMaxRankSprite.Height / 15;
                    mObjectivesContent.AddChild(mMaxRankSprite);
                }
        
                mCanvasContent.Visible = false;
                mMaxRankSprite.Visible = true;
            }
            else
            {
                if (mMaxRankSprite != null)
                    mMaxRankSprite.Visible = false;
                if (mCanvasContent != null)
                    mCanvasContent.Visible = true;
        
                for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
                {
                    // Icon
                    SetObjectiveCompletedIcon(objRank.IsObjectiveCompletedAtIndex(i), i);
            
                    // Description
                    SetDescriptionText(objRank.ObjectiveTextAtIndex(i), i);
            
                    // Quota
                    int count = objRank.ObjectiveCountAtIndex(i), quota = objRank.ObjectiveQuotaAtIndex(i);
                    uint color = (objRank.IsObjectiveFailedAtIndex(i) ? kObjQuotaFailedColor : ((count >= quota) ? kObjQuotaCompleteColor : kObjQuotaIncompleteColor));
                    SetQuotaText(count.ToString() + "/" + quota, color, i);
            
                    // Failed
                    SetFailed(objRank.IsObjectiveFailedAtIndex(i), i);
                }
            }
    
            if (mHat != null)
                mHat.SetText(mScene.ObjectivesManager.RankLabel);
        }

        public SPSprite MaxRankSprite()
        {
            float spriteWidth = 340, spriteHeight = 332;
    
            SPSprite sprite = new SPSprite();
            SPTextField textField = new SPTextField(spriteWidth, 48, "Congratulations!", mScene.FontKey, 40);
            textField.X = (spriteWidth - textField.Width) / 2;
            textField.Y = 0;
            textField.HAlign = SPTextField.SPHAlign.Center;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = Color.Black;
            sprite.AddChild(textField);

            SPImage image = new SPImage(mScene.TextureByName("objectives-congrats"));
            image.X = (spriteWidth - image.Width) / 2;
            image.Y = 68;
            sprite.AddChild(image);
    
            string text = "You have achieved the^highest rank. Now try^to beat the high score...";
            if (GameController.GC.IsTrialMode)
                text = "Upgrade to the full version^for 24 ranks and a 34x^score multiplier.";

            textField = new SPTextField(spriteWidth, spriteHeight - (1.05f * sprite.Height), text, mScene.FontKey, 27);
            textField.X = (spriteWidth - textField.Width) / 2;
            textField.Y = spriteHeight - textField.Height;
            textField.HAlign = SPTextField.SPHAlign.Center;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = Color.Black;
            sprite.AddChild(textField);
    
            return sprite;
        }

        public void AddToPanel(SPDisplayObject displayObject, float xPercent, float yPercent)
        {
            if (displayObject != null && mScrollSprite != null && mCanvas != null)
            {
                displayObject.X = mScrollSprite.X + mScrollSprite.Width * xPercent;
                displayObject.Y = mScrollSprite.Y + mScrollSprite.Height * yPercent;
                mCanvas.AddChild(displayObject);
            }
        }

        public void RemoveFromPanel(SPDisplayObject displayObject)
        {
            if (displayObject != null && mCanvas != null)
                mCanvas.RemoveChild(displayObject);
        }

        private void OnContinuePressed(SPEvent ev)
        {
            mScene.PlaySound("Button");
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_OBJECTIVES_CURRENT_PANEL_CONTINUED));
        }
        #endregion
    }
}
