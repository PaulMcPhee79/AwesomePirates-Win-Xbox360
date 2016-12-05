using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class ObjectivesView : Prop
    {
        public enum ViewType
        {
            View = 0,
            Completed,
            Current
        }

        public const string CUST_EVENT_TYPE_OBJECTIVES_CURRENT_PANEL_DISMISSED = "objectivesCurrentPanelDismissedEvent";
        public const string CUST_EVENT_TYPE_OBJECTIVES_RANKUP_PANEL_DISMISSED = "objectivesRankupPanelDismissedEvent";

        public ObjectivesView(int category)
            : base(category)
        {
            Touchable = true;
            mAdvanceable = true;
            mTouchBarrierEnabled = false;
            mRankupPanel = null;
            mCompletedQueue = new List<ObjectivesDescription>();
            SetupProp();
        }

        #region Fields
        private bool mTouchBarrierEnabled;
        private List<ObjectivesDescription> mCompletedQueue;
        // Subviews
        private ObjectivesCompletedPanel mCompletedPanel;
        private ObjectivesCurrentPanel mCurrentPanel;
        private ObjectivesRankupPanel mRankupPanel;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            // Objectives Completed Panel
            mCompletedPanel = new ObjectivesCompletedPanel(mScene.ObjectivesCategoryForViewType(ViewType.Completed));
            mCompletedPanel.X = ResManager.RESM.ResX(0);
            mScene.AddProp(mCompletedPanel);
    
            // Objectives Current Panel
            mCurrentPanel = new ObjectivesCurrentPanel(mScene.ObjectivesCategoryForViewType(ViewType.Current));
            //mCurrentPanel.X = ResManager.RESX(0); mCurrentPanel.Y = ResManager.RESY(0);
            mCurrentPanel.Y = -(mScene.ViewHeight - mCurrentPanel.Height) / 6;
            mCurrentPanel.Visible = false;
            mCurrentPanel.AddEventListener(ObjectivesCurrentPanel.CUST_EVENT_TYPE_OBJECTIVES_CURRENT_PANEL_CONTINUED, (SPEventHandler)OnCurrentPanelDismissed);
            mScene.AddProp(mCurrentPanel);
            ResManager.RESM.PopOffset();
        }

        public void EnableTouchBarrier(bool enable)
        {
            mTouchBarrierEnabled = enable;
        }

        public override void Flip(bool enable)
        {
            if (mCompletedPanel != null)
                mCompletedPanel.Flip(enable);
        }

        public SPSprite MaxRankSprite()
        {
            return mCurrentPanel.MaxRankSprite();
        }

        public void PrepareForNewGame()
        {
            PurgeCompletedQueue();
            HideCurrentPanel();
            HideRankupPanel();
        }

        // Current Panel
        public void PopulateWithObjectivesRank(ObjectivesRank objRank)
        {
            mCurrentPanel.PopulateWithObjectivesRank(objRank);
        }

        public void ShowCurrentPanel()
        {
            mCurrentPanel.AttachGamerPic(mScene.GamerPic);
            mCurrentPanel.Visible = true;
        }

        public void HideCurrentPanel()
        {
            mCurrentPanel.Visible = false;
            mCurrentPanel.DetachGamerPic(mScene.GamerPic);
        }

        public void EnableCurrentPanelButtons(bool enable)
        {
            mCurrentPanel.EnabledButtons(enable);
        }

        public void AddToCurrentPanel(SPDisplayObject displayObject, float xPercent, float yPercent)
        {
            if (displayObject != null && mCurrentPanel != null)
                mCurrentPanel.AddToPanel(displayObject, xPercent, yPercent);
        }

        public void RemoveFromCurrentPanel(SPDisplayObject displayObject)
        {
            if (displayObject != null && mCurrentPanel != null)
                mCurrentPanel.RemoveFromPanel(displayObject);
        }

        private void OnCurrentPanelDismissed(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_OBJECTIVES_CURRENT_PANEL_DISMISSED));
        }

        // Completed Panel
        public void FillCompletedCacheWithRank(ObjectivesRank objRank)
        {
            mCompletedPanel.FillCacheWithRank(objRank);
        }

        public void EnqueueCompletedObjectivesDescription(ObjectivesDescription objDesc)
        {
            if (objDesc != null)
                mCompletedQueue.Add(objDesc);
        }

        private void PurgeCompletedQueue()
        {
            mCompletedQueue.Clear();
            mCompletedPanel.Hide();
        }

        private void PumpCompletedQueue()
        {
            if (mCompletedQueue.Count > 0 && !mCompletedPanel.IsBusy)
            {
                ObjectivesDescription objDesc = mCompletedQueue[0];
                mCompletedPanel.SetText(objDesc.Description);
                mCompletedPanel.DisplayForDuration(5f);
                mCompletedQueue.RemoveAt(0);
            }
        }

        // Rankup Panel
        public void ShowRankupPanelWithRank(uint rank)
        {
            HideRankupPanel();

            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            mRankupPanel = new ObjectivesRankupPanel(Category, rank);
            //mRankupPanel.X = ResManager.RESX(0); mRankupPanel.Y = ResManager.RESY(0);
            mRankupPanel.Y = -(mScene.ViewHeight - mRankupPanel.ScrollHeight) / 10;
            mRankupPanel.AddEventListener(ObjectivesRankupPanel.CUST_EVENT_TYPE_OBJECTIVES_RANKUP_PANEL_CONTINUED, (SPEventHandler)OnRankupPanelDismissed);
            mRankupPanel.EnableTouchBarrier(mTouchBarrierEnabled);
            mRankupPanel.AttachGamerPic(mScene.GamerPic);
            mScene.PushFocusState(InputManager.FOCUS_STATE_PF_OBJECTIVES_RANKUP);
            AddChild(mRankupPanel);
            ResManager.RESM.PopOffset();
        }

        public void HideRankupPanel()
        {
            if (mRankupPanel != null)
            {
                mScene.PopFocusState(InputManager.FOCUS_STATE_PF_OBJECTIVES_RANKUP);
                mRankupPanel.RemoveEventListener(ObjectivesRankupPanel.CUST_EVENT_TYPE_OBJECTIVES_RANKUP_PANEL_CONTINUED, (SPEventHandler)OnRankupPanelDismissed);
                mRankupPanel.DetachGamerPic(mScene.GamerPic);
                RemoveChild(mRankupPanel);
                mRankupPanel.Dispose();
                mRankupPanel = null;
            }
        }

        private void OnRankupPanelDismissed(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_OBJECTIVES_RANKUP_PANEL_DISMISSED));
        }

        public override void AdvanceTime(double time)
        {
            PumpCompletedQueue();
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mCompletedPanel != null)
                        {
                            mScene.RemoveProp(mCompletedPanel);
                            mCompletedPanel.Dispose();
                            mCompletedPanel = null;
                        }

                        if (mCurrentPanel != null)
                        {
                            mScene.RemoveProp(mCurrentPanel);
                            mCurrentPanel.Dispose();
                            mCurrentPanel = null;
                        }

                        if (mRankupPanel != null)
                        {
                            mRankupPanel.RemoveFromParent();
                            mRankupPanel.Dispose();
                            mRankupPanel = null;
                        }

                        mCompletedQueue = null;
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
