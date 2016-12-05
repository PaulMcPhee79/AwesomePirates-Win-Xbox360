using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class HintPackage
    {
        public HintPackage(Prop prop, SPTween loopingTween)
        {
            mProps = new List<Prop>();
            mFlipProps = new List<Prop>();
            mLoopingTweens = new List<SPTween>();
        
            if (prop != null)
                mProps.Add(prop);
            if (loopingTween != null)
                mLoopingTweens.Add(loopingTween);
        }

        #region Fields
        private List<Prop> mProps;
        private List<Prop> mFlipProps;
        private List<SPTween> mLoopingTweens;
        #endregion

        #region Properties
        public List<Prop> Props { get { return mProps; } }
        public List<Prop> FlipProps { get { return mFlipProps; } }
        public List<SPTween> LoopingTweens { get { return mLoopingTweens; } }
        #endregion

        #region Methods
        public void AddProp(Prop prop)
        {
            if (prop != null && mProps != null && !mProps.Contains(prop))
                mProps.Add(prop);
        }

        public void AddFlipProp(Prop prop)
        {
            if (prop != null && mFlipProps != null && !mFlipProps.Contains(prop))
                mFlipProps.Add(prop);
        }

        public void RemoveProp(Prop prop)
        {
            if (prop != null && mProps != null)
                mProps.Remove(prop);
        }

        public void AddLoopingTween(SPTween tween)
        {
            if (tween != null && mLoopingTweens != null && !mLoopingTweens.Contains(tween))
                mLoopingTweens.Add(tween);
        }

        public void RemoveLoopingTween(SPTween tween)
        {
            if (tween != null && mLoopingTweens != null)
                mLoopingTweens.Remove(tween);
        }
        #endregion
    }
}
