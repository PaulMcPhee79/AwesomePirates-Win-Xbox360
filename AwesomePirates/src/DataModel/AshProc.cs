using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class AshProc : SPEventDispatcher
    {
        public AshProc()
        {
            mSpecialProcEventKey = null;
		    mTexturePrefix = "single-shot_";
		    mSoundName = null;
        }

        #region Fields

        private uint mProc;
        private uint mChargesRemaining;
        private uint mTotalCharges;
        private uint mRequirementCount;
        private uint mRequirementCeiling;
        private uint mAddition;
        private uint mRicochetAddition;
        private uint mMultiplier;
        private float mRicochetMultiplier;
        private bool mDeactivatesOnMiss;
        private float mChanceToProc;
        private float mSpecialChanceToProc;
        private string mSpecialProcEventKey;
        private string mTexturePrefix;
        private string mSoundName;

        #endregion

        #region Properties

        public uint Proc { get { return mProc; } set { mProc = value; } }
        public uint ChargesRemaining { get { return mChargesRemaining; } set { mChargesRemaining = value; } }
        public uint TotalCharges { get { return mTotalCharges; } set { mTotalCharges = value; } }
        public uint RequirementCount { get { return mRequirementCount; } set { mRequirementCount = value; } }
        public uint RequirementCeiling { get { return mRequirementCeiling; } set { mRequirementCeiling = value; } }
        public uint Addition { get { return mAddition; } set { mAddition = value; } }
        public uint RicochetAddition { get { return mRicochetAddition; } set { mRicochetAddition = value; } }
        public uint Multiplier { get { return mMultiplier; } set { mMultiplier = value; } }
        public float RicochetMultiplier { get { return mRicochetMultiplier; } set { mRicochetMultiplier = value; } }
        public bool DeactivatesOnMiss { get { return mDeactivatesOnMiss; } set { mDeactivatesOnMiss = value; } }
        public float ChanceToProc { get { return mChanceToProc; } set { mChanceToProc = value; } }
        public float SpecialChanceToProc { get { return mSpecialChanceToProc; } set { mSpecialChanceToProc = value; } }
        public string SpecialProcEventKey { get { return mSpecialProcEventKey; } set { mSpecialProcEventKey = value; } }
        public string TexturePrefix { get { return mTexturePrefix; } set { mTexturePrefix = value; } }
        public string SoundName { get { return mSoundName; } set { mSoundName = value; } }
        public bool IsActive { get { return (mChargesRemaining > 0); } }

        #endregion

        #region Methods

        public void Deactivate()
        {
            mChargesRemaining = 0;
            mRequirementCount = 0;
        }

        public bool ChanceProc()
        {
            if (mChargesRemaining > 0)
                return true;
            int randInt = GameController.GC.NextRandom(0, 1000);
            float chance = randInt / 1000.0f;

            if (chance < mChanceToProc)
                mChargesRemaining = mTotalCharges;
            return (mChargesRemaining > 0);
        }

        public uint ConsumeCharge()
        {
            if (mChargesRemaining > 0)
                --mChargesRemaining;
            return mChargesRemaining;
        }

        #endregion
    }
}
