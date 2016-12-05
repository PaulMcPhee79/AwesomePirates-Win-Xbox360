using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class MasteryManager : IDisposable
    {
        public const int kDefaultMasteryModelKey = 99;

        public MasteryManager(IMasteryTemplate template)
        {
            mTemplate = template;
            mModels = new Dictionary<int, MasteryModel>();
            mCurrentModel = null;
        }

        #region Fields
        private bool mIsDisposed = false;
        private MasteryModel mCurrentModel;
        private Dictionary<int, MasteryModel> mModels;
        private IMasteryTemplate mTemplate;
        #endregion

        #region Properties
        public MasteryModel CurrentModel { get { return mCurrentModel; } private set { mCurrentModel = value; } }
        public uint MasteryBitmap { get { return (mCurrentModel != null) ? mCurrentModel.MasteryBitmap : 0; } }
        public IMasteryTemplate Template { get { return mTemplate; } }
        #endregion

        #region Methods
        public void RefreshMasteryBitmap()
        {
            if (CurrentModel != null)
            {
                CurrentModel.RefreshMasteryBitmap();

                if (Template != null)
                    Template.MasteryBitmap = MasteryBitmap;
            }
        }

        public void AddModel(int key, MasteryModel model)
        {
            if (mModels == null)
                mModels = new Dictionary<int, MasteryModel>();
            if (!mModels.ContainsKey(key))
                mModels.Add(key, model);
        }

        public void RemoveModel(int key, bool shouldDispose = true)
        {
            if (mModels != null && mModels.ContainsKey(key))
            {
                MasteryModel model = mModels[key];
                mModels.Remove(key);

                if (CurrentModel == model)
                {
                    CurrentModel = (mModels.ContainsKey(kDefaultMasteryModelKey)) ? mModels[kDefaultMasteryModelKey] : null;
                    RefreshMasteryBitmap();
                }

                if (shouldDispose)
                    model.Dispose();
            }
        }

        public bool ContainsModel(int key)
        {
            return (mModels != null && mModels.ContainsKey(key));
        }

        public void SetCurrentModel(int key)
        {
            if (mModels != null && mModels.ContainsKey(key))
                CurrentModel = mModels[key];
        }

        public float ApplyScoreBonus(float score, ShipActor ship)
        {
            float adjustedScore = score;

            if (mTemplate != null)
                adjustedScore = mTemplate.ApplyScoreBonus(score, ship);
            return adjustedScore;
        }

        public float ApplyScoreBonus(float score, OverboardActor prisoner)
        {
            float adjustedScore = score;

            if (mTemplate != null)
                adjustedScore = mTemplate.ApplyScoreBonus(score, prisoner);
            return adjustedScore;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mModels != null)
                        {
                            foreach (KeyValuePair<int, MasteryModel> kvp in mModels)
                                kvp.Value.Dispose();
                            mModels = null;
                        }

                        mCurrentModel = null;
                        mTemplate = null;
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
                finally
                {
                    mIsDisposed = true;
                }
            }
        }

        ~MasteryManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
