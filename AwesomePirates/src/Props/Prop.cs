using System;
using SparrowXNA;

namespace AwesomePirates
{
    class Prop : SPSprite
    {
        private static SceneController s_Scene = null;

        public Prop(int category)
        {
            mScene = s_Scene;
            mCategory = category;
            mAdvanceable = false;
            mSlowable = true;
            mRemoveMe = false;
            mTurnID = GameController.GC.ThisTurn.TurnID;

            if (mScene != null)
                Touchable = mScene.TouchableDefault;
        }

        public Prop(PFCat category) : this((int)category) { }

        #region Fields
        private bool mShouldDispose = true;
        protected int mCategory;
        protected uint mTurnID;
        protected bool mAdvanceable;
        protected bool mSlowable;
        protected bool mRemoveMe;
        protected SceneController mScene;
        #endregion

        #region Properties
        public bool ShouldDispose { get { return mShouldDispose; } set { mShouldDispose = value; } }
        public int Category { get { return mCategory; } set { mCategory = value; } }
        public bool Advanceable { get { return mAdvanceable; } }
        public bool Slowable { get { return mSlowable; } }
        public bool MarkedForRemoval { get { return mRemoveMe; } }
        public uint TurnID { get { return mTurnID; } }
        #endregion

        #region Methods
        protected virtual void SetupProp() { }

        public virtual void Flip(bool enable) { }

        public virtual void MoveToCategory(int category)
        {
            Prop temp = this;
            mScene.RemoveProp(this, false);
            Category = category;
            mScene.AddProp(this);
        }

        public virtual void AdvanceTime(double time) { }

        public virtual void CheckoutPooledResources() { }

        public virtual void CheckinPooledResources() { }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mScene = null;
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

        public static SceneController PropsScene
        {
            get
            {
                return s_Scene;
            }
            
            set
            {
                if (value == null)
                    throw new InvalidOperationException("Use RelinquishPropScene instead of setting it to null directly.");
                s_Scene = value;
            }
        }

        public static void RelinquishPropScene(SceneController scene)
        {
            if (scene == s_Scene)
                s_Scene = null;
        }
        #endregion
    }
}
