using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class KeyCycler
    {
        public KeyCycler(uint[] keys)
        {
            if (keys == null || keys.Length < 2)
                throw new ArgumentException("KeyCycler requires a minimum of 2 keys to cycle.");

            int cdCapacity = keys.Length / 2;
            mAvailableKeys = new uint[keys.Length - cdCapacity];
            mCooldownKeys = new uint[cdCapacity];

            int i = 0;
            for (i = 0; i < mAvailableKeys.Length; ++i)
                mAvailableKeys[i] = keys[i];
            for (int j = 0; j < mCooldownKeys.Length; ++j)
                mCooldownKeys[j] = keys[i+j];

            mCooldownIndex = 0;
        }

        #region Fields
        protected int mCooldownIndex;
        protected uint[] mAvailableKeys;
        protected uint[] mCooldownKeys;
        #endregion

        #region Methods
        public virtual uint NextKey()
        {
            int randIndex = GameController.GC.NextRandom(mAvailableKeys.Length-1);
            uint nextKey = mAvailableKeys[randIndex];
            mAvailableKeys[randIndex] = mCooldownKeys[mCooldownIndex];
            mCooldownKeys[mCooldownIndex] = nextKey;

            if (++mCooldownIndex == mCooldownKeys.Length)
                mCooldownIndex = 0;

            return nextKey;
        }

        public virtual void Randomize()
        {
            int rndCount = mAvailableKeys.Length + mCooldownKeys.Length;
            for (int i = 0; i < rndCount; ++i)
                NextKey();
            mCooldownIndex = 0;
        }
        #endregion
    }
}
