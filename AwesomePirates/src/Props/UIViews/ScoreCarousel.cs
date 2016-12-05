using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using SparrowXNA;

namespace AwesomePirates
{
    class ScoreCarousel : Prop
    {
        private const float kTransitionDuration = 0.5f;
        private const float kTransitionDelay = 5f;

        public ScoreCarousel(int category)
            : base(category)
        {
            mTweener = new FloatTweener(1f, SPTransitions.SPLinear, new Action(OnTransitioned));
            SetupProp();
        }

        #region Fields
        private SPTextField mLocalBestScore;
        private SPSprite mLocalBestSprite;

        private int mIndex = -1;
        private List<SPTextField> mTextFields = new List<SPTextField>(4);
        private List<SPImage> mGamerpics = new List<SPImage>(4);
        private List<SPSprite> mSprites = new List<SPSprite>(4);
        private List<string> mScorers = new List<string>(4);
        private FloatTweener mTweener;
        #endregion

        #region Properties
        private int NextIndex { get { return mScorers != null && mScorers.Count > 0 ? (mIndex + 1) % mScorers.Count : -1; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mLocalBestSprite != null)
                return;

            mLocalBestSprite = new SPSprite();
            AddChild(mLocalBestSprite);

            SPImage hiScoreImage = new SPImage(mScene.TextureByName("sk-trophy-0"));
            hiScoreImage.Y = 8;
            SPSprite hiScore = new SPSprite();
            hiScore.AddChild(hiScoreImage);
            mLocalBestSprite.AddChild(hiScore);

            mLocalBestScore = new SPTextField(256, 48, "", mScene.FontKey, 40);
            mLocalBestScore.X = hiScoreImage.X + hiScoreImage.Width + 4;
            mLocalBestScore.Color = Color.Black;
            mLocalBestScore.HAlign = SPTextField.SPHAlign.Left;
            mLocalBestScore.VAlign = SPTextField.SPVAlign.Top;
            mLocalBestSprite.AddChild(mLocalBestScore);
        }

        private bool ContainsScorer(string scorer)
        {
            foreach (string s in mScorers)
            {
                if (s == scorer)
                    return true;
            }

            return false;
        }

        private int IndexOfScorer(string scorer)
        {
            int i = 0, index = -1;
            foreach (string s in mScorers)
            {
                if (s == scorer)
                {
                    index = i;
                    break;
                }

                ++i;
            }

            return index;
        }

        public void Clear()
        {
            if (mScorers == null)
                return;

            List<string> mScorersCopy = new List<string>(mScorers);
            foreach (string scorer in mScorersCopy)
                RemoveScorer(scorer);
        }

        public void AutoPopulateScorers()
        {
            GameController gc = GameController.GC;
            List<string> profileNames = gc.ProfileManager.ProfileNames;

            if (profileNames != null)
            {
                foreach (string name in profileNames)
                {
                    if (name != GameStats.DefaultAlias)
                        AddScorer(name);
                }
            }
        }

        private void ResetTweener()
        {
            if (mSprites != null && mTweener != null && mIndex >= 0 && mIndex < mSprites.Count)
            {
                if (mSprites.Count > 1)
                    mTweener.Reset(1f, 0f, kTransitionDuration, kTransitionDelay);
                else
                    mTweener.Reset(1f);
            }
        }

        private int LocalScoreForProfile(PlayerProfile profile)
        {
            if (profile == null)
                return 0;

            int statsScore = 0;
            if (!GameController.GC.ProfileManager.IsPlayerUsingGlobalStats(profile.PlayerIndex) && profile.PlayerStats != null)
                statsScore = profile.PlayerStats.HiScore;

            int tableScore = GameController.GC.HiScores.HighestScoreForPlayer(profile.GamerTag).value;
            return Math.Max(statsScore, tableScore);
        }

        public void AddScorer(string gamertag)
        {
            if (gamertag == null || mScorers == null || ContainsScorer(gamertag))
                return;

            GameController gc = GameController.GC;
            PlayerProfile profile = gc.ProfileManager.ProfileForTag(gamertag);

            if (profile == null || profile.GamerPicture == null)
                return;

            mScorers.Add(gamertag);

            SPSprite sprite = new SPSprite();
            sprite.Alpha = mScorers.Count == 1 ? 1f : 0f;
            mSprites.Add(sprite);
            AddChild(sprite);

            SPImage image = new SPImage(profile.GamerPicture);
            image.Scale = new Vector2(0.85f, 0.85f);
            mGamerpics.Add(image);
            sprite.AddChild(image);

            SPTextField textField = new SPTextField(256, 48, profile.PlayerStats == null ? "" : GuiHelper.CommaSeparatedValue(LocalScoreForProfile(profile)), mScene.FontKey, 40);
            textField.X = image.X + image.Width + 8;
            textField.Color = Color.Black;
            textField.HAlign = SPTextField.SPHAlign.Left;
            textField.VAlign = SPTextField.SPVAlign.Top;
            mTextFields.Add(textField);
            sprite.AddChild(textField);

            if (mScorers.Count == 1)
            {
                mIndex = 0;
                sprite.Alpha = 1f;
                ResetTweener();
            }
            else if (mScorers.Count == 2)
                ResetTweener();

            mLocalBestSprite.Visible = mScorers.Count == 0;
        }

        public void RemoveScorer(string gamertag)
        {
            if (gamertag == null || mScorers == null || !ContainsScorer(gamertag))
                return;

            int index = IndexOfScorer(gamertag);
            mScorers.RemoveAt(index);

            if (index < 0 && index >= mSprites.Count)
                return;

            mTextFields.RemoveAt(index);
            mGamerpics.RemoveAt(index);
            SPSprite sprite = mSprites[index];
            mSprites.RemoveAt(index);
            sprite.RemoveFromParent();
            sprite.Dispose();
            sprite = null;

            if (mScorers.Count == 0)
                mIndex = -1;
            else if (mScorers.Count == 1)
            {
                mIndex = 0;
                ResetTweener();
                mSprites[mIndex].Alpha = 1f;
            }

            mLocalBestSprite.Visible = mScorers.Count == 0;
        }

        public void UpdateScores()
        {
            if (mScorers == null)
                return;

            GameController gc = GameController.GC;
            for (int i = 0; i < mScorers.Count; ++i)
            {
                string scorer = mScorers[i];
                PlayerProfile profile = gc.ProfileManager.ProfileForTag(scorer);
                if (profile == null || profile.PlayerStats == null || i >= mTextFields.Count)
                    continue;

                mTextFields[i].Text = GuiHelper.CommaSeparatedValue(LocalScoreForProfile(profile));
            }

            if (mLocalBestScore != null)
                mLocalBestScore.Text = GuiHelper.CommaSeparatedValue(mScene.AchievementManager.HiScores.BestScore.value);
        }

        public override void AdvanceTime(double time)
        {
            if (mIndex == -1 || mSprites == null || mSprites.Count < 2)
                return;

            if (mIndex >= mSprites.Count)
                mIndex = 0;

            mTweener.AdvanceTime(time);
            if (!mTweener.Delaying && mSprites[mIndex].Alpha != mTweener.TweenedValue)
            {
                mSprites[mIndex].Alpha = mTweener.TweenedValue;
                mSprites[NextIndex].Alpha = 1f - mSprites[mIndex].Alpha;
            }
        }

        private void OnTransitioned()
        {
            if (mSprites == null || mSprites.Count == 0 || mIndex < 0 || mIndex >= mSprites.Count)
                return;

            mSprites[mIndex].Alpha = mTweener.TweenedValue;
            mSprites[NextIndex].Alpha = 1f - mSprites[mIndex].Alpha;
            mIndex = NextIndex;
            ResetTweener();
        }

        public void OnGamerPicsRefreshed(SPEvent ev)
        {
            if (mScorers == null || mGamerpics == null)
                return;

            GameController gc = GameController.GC;
            for (int i = 0; i < mScorers.Count; ++i)
            {
                string scorer = mScorers[i];
                PlayerProfile profile = gc.ProfileManager.ProfileForTag(scorer);
                if (profile == null || profile.GamerPicture == null || i >= mGamerpics.Count)
                    continue;

                mGamerpics[i].Texture = profile.GamerPicture;
            }
        }
        #endregion
    }
}
