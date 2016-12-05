using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class SaveNoticeView : Prop, ISPAnimatable
    {
        public SaveNoticeView(int category)
            : base(category)
        {
            mAdvanceable = false;
            mAnimKey = SPJuggler.NextAnimKey();
            SetupProp();
        }

        #region Fields
        private uint mAnimKey;
        private FloatTweener mTweener;
        #endregion

        #region Properties
        public float TweenedValue { get { return mTweener.TweenedValue; } }
        public Action TweenComplete { get { return mTweener.TweenComplete; } set { mTweener.TweenComplete = value; } }

        public bool IsComplete { get { return false; } }
        public uint AnimKey { get { return mAnimKey; } }
        public object Target { get { return this; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            Alpha = 0f;
            mTweener = new FloatTweener(Alpha, SPTransitions.SPLinear);
            AddChild(new SPImage(mScene.TextureByName("saving-prompt")));
        }

        public void Reset(float from, float to, double duration, double delay = 0)
        {
            mTweener.Reset(from, to, duration, delay); 
        }

        public override void AdvanceTime(double time)
        {
            mTweener.AdvanceTime(time);
            if (!mTweener.Delaying && Alpha != mTweener.TweenedValue)
                Alpha = mTweener.TweenedValue;
        }
        #endregion
    }
}
