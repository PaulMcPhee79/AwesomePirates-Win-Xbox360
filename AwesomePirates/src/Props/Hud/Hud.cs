using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class Hud : Prop
    {
        private const float kHudBaseWidth = 640f;
#if IOS_SCREENS
        private const float kHudCellY = 2.0f;
#else
        private const float kHudCellY = 6.0f;
#endif
        private const float kInfamyCellLabelWidth = 128.0f;

        public Hud(int category, float x, float y, Color textColor, Color? outlineColor = null)
            : base(category)
        {
		    mListenersAttached = false;
            mHudCells = null;
            mHudMutiny = null;
            mHudContainer = null;
            mOriginX = x;
		    X = x;
		    Y = y;
		    mColor = textColor;
            mOutlineColor = outlineColor;
            SetupProp();
        }
        
        #region Fields
        private bool mListenersAttached;
        private Color mColor;
        private Color? mOutlineColor;
        private float mOriginX;
        private HudCell mInfamyCell;
        private HudCell mAiCell;
        private HudCell mMiscCell;
        private List<HudCell> mHudCells;
        private HudMutiny mHudMutiny;
        private SPSprite mHudContainer;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mHudContainer != null)
                return;

            // hudscale == 1 -> 640 pixel width
            float hudScale = Math.Min(1.2f, (ResManager.RESM.Width / ResManager.kResStandardWidth) - 0.15f);
            mHudContainer = new SPSprite();
            mHudContainer.ScaleX = mHudContainer.ScaleY = hudScale;
            AddChild(mHudContainer);

            /// + 75 + 50 * hudScale
            //mInfamyCell = new HudCell(-1, 1.5f * kInfamyCellLabelWidth, kHudCellY, 40, 10, 2, OutlineTextField.OutlineDrawMode.Pass8);
#if IOS_SCREENS
            mInfamyCell = new HudCell(-1, 2.1f * kInfamyCellLabelWidth, kHudCellY, 36, 10);
            mInfamyCell.SetupWithLabel("Score", 0.9f * kInfamyCellLabelWidth, mColor, mOutlineColor);
#else
            mInfamyCell = new HudCell(-1, 1.5f * kInfamyCellLabelWidth, kHudCellY, 40, 10);
            mInfamyCell.SetupWithLabel("Score", kInfamyCellLabelWidth, mColor, mOutlineColor);
#endif

            mHudCells = new List<HudCell>() { mInfamyCell };

            foreach (HudCell hudCell in mHudCells)
                mHudContainer.AddChild(hudCell);

            if (mScene is PlayfieldController)
            {
                mHudMutiny = new HudMutiny(Category, 6);
#if IOS_SCREENS
                mHudMutiny.X = (mInfamyCell.X + mInfamyCell.Width - kInfamyCellLabelWidth) + 20;
                mHudMutiny.Y = 4;
#else
                mHudMutiny.X = (mInfamyCell.X + mInfamyCell.Width - kInfamyCellLabelWidth) + 32;
                mHudMutiny.Y = 10;
#endif
                mHudMutiny.ScaleX = mHudMutiny.ScaleY = 0.9f;
                mHudContainer.AddChild(mHudMutiny);
            }

            mHudContainer.X = Math.Abs(hudScale * kHudBaseWidth - mHudContainer.Width) / 3;
            
#if false
            float hudScale = Math.Max(0.9f, ResManager.RESM.HudScale);
            mInfamyCell = new HudCell(-1, 240 - (1.1f - hudScale) * 40, kHudCellY, (int)(36 * Math.Max(1f, hudScale)), 10);
            mInfamyCell.SetupWithLabel("Score", kInfamyCellLabelWidth * hudScale, mColor);

            mHudCells = new List<HudCell>() { mInfamyCell };

            foreach (HudCell hudCell in mHudCells)
                AddChild(hudCell);

            if (mScene is PlayfieldController)
            {
                mHudMutiny = new HudMutiny(Category, 6);
                mHudMutiny.X = mInfamyCell.X + mInfamyCell.Width - kInfamyCellLabelWidth + 32 * hudScale; mHudMutiny.Y = 10;
                mHudMutiny.ScaleX = mHudMutiny.ScaleY = 0.9f * hudScale;
                AddChild(mHudMutiny);
            }
#endif
        }

        public void AttachEventListeners()
        {
            if (mListenersAttached)
                return;

            GameController gc = GameController.GC;

            gc.ThisTurn.AddEventListener(ThisTurn.CUST_EVENT_TYPE_INFAMY_VALUE_CHANGED, (NumericValueChangedEventHandler)OnInfamyChanged);
            gc.ThisTurn.AddEventListener(ThisTurn.CUST_EVENT_TYPE_MUTINY_VALUE_CHANGED, (NumericRatioChangedEventHandler)OnMutinyChanged);
            gc.ThisTurn.AddEventListener(ThisTurn.CUST_EVENT_TYPE_MUTINY_COUNTDOWN_CHANGED, (NumericRatioChangedEventHandler)OnMutinyCountdownChanged);

            mListenersAttached = true;
        }

        public void DetachEventListeners()
        {
            if (!mListenersAttached)
                return;

            try
            {
                GameController gc = GameController.GC;
                ThisTurn thisTurn = gc.ThisTurn;

                if (thisTurn != null)
                {
                    thisTurn.RemoveEventListener(ThisTurn.CUST_EVENT_TYPE_INFAMY_VALUE_CHANGED, (NumericValueChangedEventHandler)OnInfamyChanged);
                    thisTurn.RemoveEventListener(ThisTurn.CUST_EVENT_TYPE_MUTINY_VALUE_CHANGED, (NumericRatioChangedEventHandler)OnMutinyChanged);
                    thisTurn.RemoveEventListener(ThisTurn.CUST_EVENT_TYPE_MUTINY_COUNTDOWN_CHANGED, (NumericRatioChangedEventHandler)OnMutinyCountdownChanged);
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            mListenersAttached = false;
        }

        public void SetInfamyValue(int value)
        {
            mInfamyCell.Value = value;
        }

        public override void Flip(bool enable)
        {
            if (enable)
            {
                ScaleX = -1;
                X = mOriginX + mScene.ViewWidth + ResManager.RITMFX(172);
            }
            else
            {
                ScaleX = 1;
                X = mOriginX;
            }
        }

        private void OnInfamyChanged(NumericValueChangedEvent ev)
        {
            if (mInfamyCell != null)
            {
                int infamy = ev.IntValue;
                mInfamyCell.Value = infamy;
            }
        }

        private void OnMutinyChanged(NumericRatioChangedEvent ev)
        {
            if (mHudMutiny != null)
            {
                int value = (int)ev.Value;
                mHudMutiny.MutinyLevel = value;
            }
        }

        private void OnMutinyCountdownChanged(NumericRatioChangedEvent ev)
        {
            if (mHudMutiny != null)
            {
                float ratio = ev.Ratio;
                mHudMutiny.FillRatio = ratio;
            }
        }

        public void EnableScoredMode(bool enable)
        {
            mInfamyCell.Visible = enable;
            mHudMutiny.Visible = enable;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        DetachEventListeners();

                        if (mHudCells != null)
                        {
                            foreach (HudCell hudCell in mHudCells)
                                hudCell.Dispose();
                            mHudCells = null;
                        }

                        mInfamyCell = null;
                        mAiCell = null;
                        mMiscCell = null;
                        mHudMutiny = null;
                        mHudContainer = null;
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
