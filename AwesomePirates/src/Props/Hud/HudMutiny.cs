using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class HudMutiny : Prop
    {
        public HudMutiny(int category, int maxMutinyLevel)
            : base(category)
        {
            mReductionDisplayEnabled = true;
            mMutinyLevelMax = Math.Max(1, maxMutinyLevel);
            mNextReduction = 0;
            mMutinyLevel = 0;
            mFillRatio = 1.0f;
            mEmptyCrosses = null;
            mFullCrosses = null;
            mCrossSprites = null;
            mEmptyTexture = mScene.TextureByName("mutiny-empty");
            mFullTexture = mScene.TextureByName("mutiny-full");
            SetupProp();
        }
        
        #region Fields
        private bool mReductionDisplayEnabled;
        private int mNextReduction;

        private int mMutinyLevel;
        private int mMutinyLevelMax;

        private float mFillRatio;

        private List<SPImage> mEmptyCrosses;
        private List<SXGauge> mFullCrosses;
        private List<SPSprite> mCrossSprites;

        private List<SPTween> mExpandTweens;
        private List<SPTween> mShrinkTweens;

        private SPTexture mEmptyTexture;
        private SPTexture mFullTexture;
        #endregion

        #region Properties
        public int MutinyLevel
        {
            get { return mMutinyLevel; }
            set
            {
                int oldLevel = mMutinyLevel;
                mMutinyLevel = Math.Min(mMutinyLevelMax, Math.Max(0, value));
    
                UpdateDisplay();
    
                // Bulge red crosses
                for (int i = oldLevel; i < mMutinyLevel; ++i)
                    BulgeCrossAtIndex(((IsReduced) ? i-1 : i));
                // Bulge blue crosses
                for (int i = oldLevel; i > mMutinyLevel; --i)
                    BulgeCrossAtIndex(i-1);
            }
        }
        public int NextReduction { get { return mNextReduction; } set { mNextReduction = value; } }
        public bool ReductionDisplayEnabled { get { return mReductionDisplayEnabled; } set { mReductionDisplayEnabled = value; } }
        public float FillRatio
        {
            get { return mFillRatio; }
            set
            {
                mFillRatio = value;

                if (mMutinyLevel != 0)
                    UpdateFillRatioDisplay();
                if (mMutinyLevel == mMutinyLevelMax && !IsReduced)
                    BulgeCrossAtIndex(mMutinyLevel-1);
            }
        }
        public bool IsReduced { get { return !SPMacros.SP_IS_FLOAT_EQUAL(1.0f, mFillRatio); } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mEmptyCrosses != null || mFullCrosses != null || mCrossSprites != null)
                return;
            List<SPImage> emptyCrosses = new List<SPImage>(mMutinyLevelMax);
            List<SXGauge> fullCrosses = new List<SXGauge>(mMutinyLevelMax);
            List<SPSprite> sprites = new List<SPSprite>(mMutinyLevelMax);
    
            float nextX = 0, nextScale = 0;
    
            for (int i = 0; i < mMutinyLevelMax; ++i) {
                nextScale = (mEmptyTexture.Width - 2 * (mMutinyLevelMax - (i + 1))) / mEmptyTexture.Width;
        
                SPSprite sprite = new SPSprite();
                AddChild(sprite);
        
                SPImage emptyCross = new SPImage(mEmptyTexture);
                emptyCross.X = -emptyCross.Width / 2;
                sprite.AddChild(emptyCross);
        
                SXGauge fullCross = new SXGauge(mFullTexture, SXGauge.SXGaugeOrientation.Vertical);
                fullCross.Ratio = 1;
                fullCross.X = -fullCross.Width / 2;
                fullCross.Visible = false;
                sprite.AddChild(fullCross);
        
                sprite.ScaleX = sprite.ScaleY = nextScale;
                sprite.X = nextX;
        
                emptyCrosses.Add(emptyCross);
                fullCrosses.Add(fullCross);
                sprites.Add(sprite);
        
                nextX += sprite.Width + 2;
            }
    
    
            mEmptyCrosses = emptyCrosses;
            mFullCrosses = fullCrosses;
            mCrossSprites = sprites;
    
            // Cache bulge tweens
            int crossIndex = 0;
            List<SPTween> expandTweens = new List<SPTween>(mCrossSprites.Count);
            List<SPTween> shrinkTweens = new List<SPTween>(mCrossSprites.Count);
    
            foreach (SPSprite cross in mCrossSprites)
            {
                float targetScale = (mEmptyTexture.Width - 2 * (mMutinyLevelMax - (crossIndex + 1))) / mEmptyTexture.Width;
                SPTween expandTween = new SPTween(cross, 0.2f, SPTransitions.SPEaseOut);
                expandTween.AnimateProperty("ScaleX", 2 * targetScale);
                expandTween.AnimateProperty("ScaleY", 2 * targetScale);
                expandTweens.Add(expandTween);
        
                SPTween shrinkTween = new SPTween(cross, 0.2f, SPTransitions.SPEaseIn);
                shrinkTween.AnimateProperty("ScaleX", targetScale);
                shrinkTween.AnimateProperty("ScaleY", targetScale);
                shrinkTween.Delay = expandTween.TotalTime;
                shrinkTweens.Add(shrinkTween);
        
                ++crossIndex;
            }
    
            mExpandTweens = expandTweens;
            mShrinkTweens = shrinkTweens;
        }

        private void UpdateDisplay()
        {
            int i = 0;
    
            foreach (SPImage emptyCross in mEmptyCrosses)
            {
                emptyCross.Visible = (i >= (mMutinyLevel-1));
                ++i;
            }
    
            i = 0;
    
            foreach (SXGauge fullCross in mFullCrosses)
            {
                fullCross.Ratio = 1.0f;
                fullCross.Visible = (i < mMutinyLevel);
                ++i;
            }

            UpdateFillRatioDisplay();
        }

        private void UpdateFillRatioDisplay()
        {
            if (mMutinyLevel > 0 && mMutinyLevel <= mFullCrosses.Count)
            {
                SXGauge fullCross = mFullCrosses[mMutinyLevel-1];
                fullCross.Ratio = mFillRatio;
            }
        }

        private void BulgeCrossAtIndex(int index)
        {
            if (index < 0 || index >= mCrossSprites.Count)
                return;
            SPSprite cross = mCrossSprites[index];
            mScene.HudJuggler.RemoveTweensWithTarget(cross);
    
            // Maximize cross' z-order
            AddChild(cross);
    
            SPTween expandTween = mExpandTweens[index];
            expandTween.Reset();
            mScene.HudJuggler.AddObject(expandTween);
    
            SPTween shrinkTween = mShrinkTweens[index];
            shrinkTween.Reset();
            mScene.HudJuggler.AddObject(shrinkTween);
        }
        #endregion
    }
}
