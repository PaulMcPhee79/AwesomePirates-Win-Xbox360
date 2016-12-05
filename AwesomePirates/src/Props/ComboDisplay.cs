using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class ComboDisplay : Prop
    {
        private const float kCannonballWidth = 32.0f;
        private const int kComboMin = 0;
        private const int kComboMax = 3;
        private const float kComboWidth = 3 * 32.0f;
        
        public ComboDisplay()
            : base(-1)
        {
            mAdvanceable = true;
		    mFlyingDutchman = false;
		    mProcActive = false;
		    mRolling = 0;
		    mCannonballClips = null;
		    mProcClips = null;
		    mFlyingDutchmanClips = null;
		    mClipStack = new List<List<SPMovieClip>>();
		    mCurrentClips = null;
		    mComboMultiplier = kComboMin;
            mCannonballs = null;
		    mJuggler = new SPJuggler();
        }

        #region Fields
        private bool mFlyingDutchman;
        private bool mProcActive;
        private int mRolling;
        private int mComboMultiplier;
        private List<SPMovieClip> mCannonballClips;
        private List<SPMovieClip> mProcClips;
        private List<SPMovieClip> mFlyingDutchmanClips;
        private List<List<SPMovieClip>> mClipStack;
        private List<SPMovieClip> mCurrentClips;
        private List<SPSprite> mCannonballs;
        private SPJuggler mJuggler;
        #endregion

        #region Methods
        public void LoadFromDictionary(Dictionary<string, object> dictionary, List<string> keys)
        {
            if (mCannonballs != null)
		        return;
	        Dictionary<string, object> dict = dictionary["Combo"] as Dictionary<string, object>;
	        float y = 2 * Globals.ConvertToSingle(dict["y"]);
	        dict = dictionary["Types"] as Dictionary<string, object>;
	
	        int i = 0;
	
	        string key = keys[i++];
	        dict = dict[key] as Dictionary<string, object>;
	        dict = dict["Textures"] as Dictionary<string, object>;
	        string textureName = dict["comboTexture"] as string;
	        List<SPTexture> textureFrames = mScene.TexturesStartingWith(textureName);
	
	        if (mCannonballClips == null)
            {
		        mCannonballClips = new List<SPMovieClip>(kComboMax);
		        mCannonballs = new List<SPSprite>(kComboMax);
		
		        for (i = 0; i < kComboMax; ++i)
                {
			        SPSprite sprite = new SPSprite();
			        sprite.X = 0.5f * kCannonballWidth + i * kCannonballWidth;
			
			        SPMovieClip clip = new SPMovieClip(textureFrames, 1);
			        clip.X = -clip.Width / 2;
			        clip.Y = -clip.Height / 2;
			
                    mCannonballClips.Add(clip);
                    mCannonballs.Add(sprite);
                    AddChild(sprite);
		        }
	        }
	
	        X = mComboMultiplier * kCannonballWidth - kComboWidth;
	        Y = y;
            PushClips(mCannonballClips);
        }

        private bool IsSrcClipLowerPriority(List<SPMovieClip> src, List<SPMovieClip> other)
        {
            bool result = false;

            if (src == other || other == mCannonballClips)
                result = false;
            else if (src == mFlyingDutchmanClips && other == mProcClips)
                result = true;
            else
                result = false;

            return result;
        }

        private void PushClips(List<SPMovieClip> clips)
        {
            mClipStack.Remove(clips);

	        int index = -1;
	
	        for (int i = 0; i < mClipStack.Count; ++i)
            {
		        List<SPMovieClip> clipIter = mClipStack[i];
		
		        if (IsSrcClipLowerPriority(clips, clipIter))
                {
			        index = i;
			        break;
		        }
	        }
	
	        if (index == -1)
            {
		        index = mClipStack.Count;
                SetCurrentCannonballClips(clips);
	        }

            mClipStack.Insert(index, clips);
        }

        private void PopClips(List<SPMovieClip> clips)
        {
            mClipStack.Remove(clips);

            if (mCurrentClips == clips)
            {
                List<SPMovieClip> nextClips = mClipStack[mClipStack.Count - 1];
                SetCurrentCannonballClips(nextClips);
            }
        }

        public override void AdvanceTime(double time)
        {
            if (mRolling != 0)
            {
		        foreach (SPSprite sprite in mCannonballs)
			        sprite.Rotation += mRolling * 0.1f;
	        }

            mJuggler.AdvanceTime(time);
        }

        private void RollCannonballsTo(int value)
        {
            mScene.Juggler.RemoveTweensWithTarget(this);
	
	        float targetValue = value * kCannonballWidth - kComboWidth;
	        float duration = Math.Min(Math.Abs(targetValue - X) / (2 * kCannonballWidth), 1.0f);

	        SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("X", targetValue);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnCannonballRollingStopped);
            mScene.Juggler.AddObject(tween);
            mRolling = (targetValue < X) ? -1 : 1;
        }

        private void OnCannonballRollingStopped(SPEvent ev)
        {
            mRolling = 0;
        }

        public void SetComboMulitplier(int value)
        {
            if (value != mComboMultiplier && value >= kComboMin && value <= mScene.AchievementManager.ComboMultiplierMax)
            {
                mComboMultiplier = value;
                X = value * kCannonballWidth - kComboWidth;
            }
        }

        public void SetComboMultiplierAnimated(int value)
        {
            if (value != mComboMultiplier && value >= kComboMin && value <= kComboMax)
            {
                RollCannonballsTo(value);
		        mComboMultiplier = value;
	        }
        }

        private void SetCurrentCannonballClips(List<SPMovieClip> clips)
        {
            int index = 0;
	
            mJuggler.RemoveAllObjects();
	
	        foreach (SPSprite sprite in mCannonballs)
            {
                sprite.RemoveAllChildren();
                sprite.AddChild(clips[index++]);
	        }
	
	        foreach (SPMovieClip clip in clips)
                mJuggler.AddObject(clip);
	        mCurrentClips = clips;
        }

        private void SetupClipsWithPrefix(List<SPMovieClip> clips, string texturePrefix)
        {
            if (clips == null)
                throw new ArgumentNullException("Cannot setup ComboDisplay clips with null.");

	        List<SPTexture> textureFrames = mScene.TexturesStartingWith(texturePrefix);
	
	        if (textureFrames != null)
            {
		        for (int i = 0; i < kComboMax; ++i)
                {
			        SPMovieClip clip = new SPMovieClip(textureFrames, 8);
			        clip.X = -clip.Width / 2;
			        clip.Y = -clip.Height / 2;
			        clip.CurrentFrame = Math.Min(clip.NumFrames-1, i);
                    clips.Add(clip);
		        }
	        }
        }

        public void SetupProcWithTexturePrefix(string texturePrefix)
        {
            if (mProcClips == null)
		        mProcClips = new List<SPMovieClip>(kComboMax);
            mProcClips.Clear();
            SetupClipsWithPrefix(mProcClips, texturePrefix);
	
	        if (mProcActive)
            {
		        mProcActive = false;
                ActivateProc();
	        }
        }

        public void ActivateProc()
        {
            if (mProcActive || mProcClips == null)
		        return;
            PushClips(mProcClips);
	        mProcActive = true;
        }

        public void DeactivateProc()
        {
            if (!mProcActive || mProcClips == null)
		        return;
	        mProcActive = false;
            PopClips(mProcClips);
        }

        public void OnComboMultiplierChanged(NumericValueChangedEvent ev)
        {
            SetComboMultiplierAnimated(ev.IntValue);
        }

        public void ActivateFlyingDutchman()
        {
            if (!mFlyingDutchman)
            {
		        if (mFlyingDutchmanClips == null)
                {
			        mFlyingDutchmanClips = new List<SPMovieClip>(kComboMax);
                    SetupClipsWithPrefix(mFlyingDutchmanClips, "dutchman-shot_");
		        }

                PushClips(mFlyingDutchmanClips);
                mFlyingDutchman = true;
	        }
        }

        public void DeactivateFlyingDutchman()
        {
            if (mFlyingDutchman)
            {
		        mFlyingDutchman = false;
                PopClips(mFlyingDutchmanClips);
	        }
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mJuggler != null)
                        {
                            mJuggler.RemoveAllObjects();
                            mJuggler = null;
                        }

                        mCurrentClips = null;
                        mCannonballClips = null;
                        mProcClips = null;
                        mFlyingDutchmanClips = null;
                        mCannonballs = null;
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
