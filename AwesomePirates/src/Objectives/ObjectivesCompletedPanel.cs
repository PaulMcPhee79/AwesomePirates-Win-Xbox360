using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class ObjectivesCompletedPanel : Prop
    {
        public const string CUST_EVENT_TYPE_OBJECTIVES_COMPLETED_PANEL_HIDDEN = "objectivesCompletedPanelHiddenEvent";

        public ObjectivesCompletedPanel(int category)
            : base(category)
        {
            mBusy = false;
            mCanvas = null;
            mFlipCanvas = null;
            mCachedTextFields = null;
            SetupProp();
        }

        #region Fields
        private bool mBusy;
        private SPSprite mCanvas;
        private SPSprite mFlipCanvas;
        private SPTextField mTextField;
        private List<SPTextField> mCachedTextFields;
        #endregion

        #region Properties
        public bool IsBusy { get { return mBusy; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvas != null)
                return;
    
            mFlipCanvas = new SPSprite();
            AddChild(mFlipCanvas);
    
            mCanvas = new SPSprite();
            mFlipCanvas.AddChild(mCanvas);
    
            SPImage scrollImage = new SPImage(mScene.TextureByName("objectives-panel"));
            mCanvas.AddChild(scrollImage);
    
            SPImage iconImage = new SPImage(mScene.TextureByName("objectives-tick"));
            iconImage.X = 2 * 16;
            iconImage.Y = 2 * 23;
            mCanvas.AddChild(iconImage);

            mTextField = new SPTextField(370, 64, "", mScene.FontKey, 24);
            mTextField.X =  80;
            mTextField.Y = 30;
            mTextField.HAlign = SPTextField.SPHAlign.Left;
            mTextField.VAlign = SPTextField.SPVAlign.Center;
            mTextField.Color = new Color(0, 0, 0);
            mCanvas.AddChild(mTextField);
    
            mCanvas.X = -mCanvas.Width / 2;
            mCanvas.Y = -mCanvas.Height;
            mFlipCanvas.X = 2 * 120 + mCanvas.Width / 2;
    
            Visible = false;
        }

        public void FillCacheWithRank(ObjectivesRank objRank)
        {
            HideOverTime(0);
    
            if (mCachedTextFields != null)
            {
                foreach (SPTextField textField in mCachedTextFields)
                    mCanvas.RemoveChild(textField);
                mCachedTextFields.Clear();
            }
    
            if (objRank == null) // nil parameter empties cache
                return;
    
            if (mCachedTextFields == null)
                mCachedTextFields = new List<SPTextField>(ObjectivesRank.kNumObjectivesPerRank);
    
            for (int i = 0; i < ObjectivesRank.kNumObjectivesPerRank; ++i)
            {
                string text = objRank.ObjectiveTextAtIndex(i);
        
                if (text == null)
                    text = "";

                SPTextField textField = new SPTextField(370, 64, text, mScene.FontKey, 24);
                textField.X = 80;
                textField.Y = 30;
                textField.HAlign = SPTextField.SPHAlign.Left;
                textField.VAlign = SPTextField.SPVAlign.Center;
                textField.Color = new Color(0, 0, 0);
                textField.Visible = false;
                mCachedTextFields.Add(textField);
                mCanvas.AddChild(textField);
            }
        }

        public void SetText(string text)
        {
            SPTextField selectedTextField = null;
    
            // Try to get it from the cache
            foreach (SPTextField textField in mCachedTextFields)
            {
                textField.Visible = false;
        
                if (textField.Text.Equals(text))
                    selectedTextField = textField;
            }
    
            if (selectedTextField != null)
            {
                // Found in cache
                selectedTextField.Visible = true;
                mTextField.Visible = false;
            }
            else
            {
                // Not in cache - do long redraw
                mTextField.Text = text;
                mTextField.Visible = true;
            }
        }

        public override void Flip(bool enable)
        {
            mFlipCanvas.ScaleX = (enable) ? -1 : 1;
        }

        public void DisplayForDuration(float duration)
        {
            mScene.HudJuggler.RemoveTweensWithTarget(mCanvas);
    
            mCanvas.Y = -mCanvas.Height;
            Visible = mBusy = true;
    
            SPTween displayTween = new SPTween(mCanvas, 0.35f);
            displayTween.AnimateProperty("Y", 2 * 25);
            displayTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnDisplayed);
            mScene.HudJuggler.AddObject(displayTween);
    
            SPTween hideTween = new SPTween(mCanvas, 0.35f);
            hideTween.AnimateProperty("Y", -mCanvas.Height);
            hideTween.Delay = Math.Max(0.5f, duration);
            hideTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnHidden);
            mScene.HudJuggler.AddObject(hideTween);
        }

        private void OnDisplayed(SPEvent ev)
        {
            mScene.PlaySound("CrewCelebrate");
        }

        public void Hide()
        {
            Visible = mBusy = false;
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_OBJECTIVES_COMPLETED_PANEL_HIDDEN));
        }

        public void HideOverTime(float duration)
        {
            mScene.HudJuggler.RemoveTweensWithTarget(mCanvas);
            Visible = mBusy = true;
    
            SPTween hideTween = new SPTween(mCanvas, duration);
            hideTween.AnimateProperty("Y", -mCanvas.Height);
            hideTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnHidden);
            mScene.HudJuggler.AddObject(hideTween);
        }

        private void OnHidden(SPEvent ev)
        {
            Visible = mBusy = false;
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_OBJECTIVES_COMPLETED_PANEL_HIDDEN));
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mCanvas != null)
                        {
                            mScene.HudJuggler.RemoveTweensWithTarget(mCanvas);
                            mCanvas = null;
                        }
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
