using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class WeightedKeyCycler : KeyCycler
    {
        private const float kDefaultWeighting = 0.1f;

        public WeightedKeyCycler(uint[] keys, float[] weightings)
            : base(keys)
        {
            mWeightings = new Dictionary<uint, float>(keys.Length);
            mRandBrackets = new int[mAvailableKeys.Length];

            for (int i = 0; i < keys.Length; ++i)
            {
                if (weightings.Length > i)
                    mWeightings[keys[i]] = weightings[i];
                else
                    mWeightings[keys[i]] = kDefaultWeighting;
            }
        }

        #region Fields
        protected int[] mRandBrackets;
        protected Dictionary<uint, float> mWeightings;
        #endregion

        #region Methods
        public override uint NextKey()
        {
            int i, randMax = 0;
            for (i = 0; i < mAvailableKeys.Length; ++i)
            {
                randMax += (int)(1000 * mWeightings[mAvailableKeys[i]]);
                mRandBrackets[i] = randMax;
            }

            int randValue = GameController.GC.NextRandom(randMax), limit = mRandBrackets.Length;
            for (i = 0; i < limit; ++i)
            {
                if (randValue < mRandBrackets[i] || i == (limit - 1))
                    break;
            }

            uint nextKey = mAvailableKeys[i];
            mAvailableKeys[i] = mCooldownKeys[mCooldownIndex];
            mCooldownKeys[mCooldownIndex] = nextKey;

            if (++mCooldownIndex == mCooldownKeys.Length)
                mCooldownIndex = 0;

            return nextKey;
        }
        #endregion
    }
}
