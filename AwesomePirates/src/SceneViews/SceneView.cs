using System;
using SparrowXNA;

namespace AwesomePirates
{
    class SceneView : SPEventDispatcher
    {
        public SceneView()
        {
            mHud = null;
            mAchievementPanel = null;
        }

        #region Fields
        protected Hud mHud;
        protected AchievementPanel mAchievementPanel;
        #endregion

        #region Properties
        public Hud Hud { get { return mHud; } }
        public AchievementPanel AchievementPanel { get { return mAchievementPanel; } }
        #endregion

        #region Methods
        public virtual void SetupView() { }

        public virtual void AttachEventListeners() { }

        public virtual void DetachEventListeners() { }

        public void MoveAchievementPanelToCategory(int category)
        {
            if (mAchievementPanel != null)
                mAchievementPanel.MoveToCategory(category);
        }

        public virtual void Flip(bool enable) { }

        public virtual void AdvanceTime(double time)
        {
            GameController.GC.AchievementManager.AdvanceTime(time);
        }

        public virtual void DestroyView() { }
        #endregion
    }
}
