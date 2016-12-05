using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class MenuDetailView : Prop
    {
        public MenuDetailView(int category, uint navMap = Globals.kNavVertical)
            : base(category)
        {
            mAdvanceable = true;
            mButtonsProxy = new ButtonsProxy(0, navMap);
            mMutableLabels = new Dictionary<string, SPTextField>();
            mLabelArrays = new Dictionary<string, List<SPTextField>>();
            mMutableImages = new Dictionary<string, SPImage>();
            mButtons = new Dictionary<string, SPButton>();
            mMutableSprites = new Dictionary<string, SPSprite>();
            mMiscProps = new List<Prop>();
            mFlipProps = new List<Prop>();
            mLoopingTweens = new List<SPTween>();
        }
        
        #region Fields
        protected ButtonsProxy mButtonsProxy;
        protected Dictionary<string, SPTextField> mMutableLabels;
        protected Dictionary<string, List<SPTextField>> mLabelArrays;
        protected Dictionary<string, SPImage> mMutableImages;
        protected Dictionary<string, SPButton> mButtons;
        protected Dictionary<string, SPSprite> mMutableSprites;
        protected List<Prop> mMiscProps;
        protected List<Prop> mFlipProps;
        protected List<SPTween> mLoopingTweens;
        #endregion

        #region Properties
        public bool Repeats
        {
            get { return (mButtonsProxy != null) ? mButtonsProxy.Repeats : false; }
            set
            {
                if (mButtonsProxy != null)
                    mButtonsProxy.Repeats = value;
            }
        }
        public double RepeatDelay
        {
            get { return (mButtonsProxy != null) ? mButtonsProxy.RepeatDelay : 0; }
            set
            {
                if (mButtonsProxy != null)
                    mButtonsProxy.RepeatDelay = value;
            }
        }
        public SPDisplayObject CurrentNav { get { return (mButtonsProxy != null) ? mButtonsProxy.CurrentNav : null; } }
        public Dictionary<string, SPTextField> MutableLabels { get { return mMutableLabels; } }
        public Dictionary<string, List<SPTextField>> LabelArrays { get { return mLabelArrays; } }
        public Dictionary<string, SPImage> MutableImages { get { return mMutableImages; } }
        public Dictionary<string, SPButton> Buttons { get { return mButtons; } }
        public Dictionary<string, SPSprite> MutableSprites { get { return mMutableSprites; } }
        public List<Prop> MiscProps { get { return mMiscProps; } }
        public List<SPTween> LoopingTweens { get { return mLoopingTweens; } }
        #endregion

        #region Methods
        public void ResetNav()
        {
            if (mButtonsProxy != null)
                mButtonsProxy.ResetNav();
        }

        public void SetTextureForKey(string textureName, string key)
        {
            if (mMutableImages.ContainsKey(key))
            {
                SPImage image = mMutableImages[key];
                image.Texture = mScene.TextureByName(textureName);
            }
        }

        public void SetTextForKey(string text, string key)
        {
            if (mMutableLabels.ContainsKey(key))
            {
                SPTextField textField = mMutableLabels[key];
                textField.Text = text;
            }
        }

        public void SetTextIndexForKey(int index, string key)
        {
            if (mLabelArrays.ContainsKey(key))
            {
                List<SPTextField> array = mLabelArrays[key];

                foreach (SPTextField textField in array)
                    textField.Visible = false;

                if (index >= 0 && index < array.Count)
                {
                    SPTextField textField = array[index];
                    textField.Visible = true;
                }
            }
        }

        public void SetRepeatingTextureForKey(string textureName, int repeats, string key)
        {
            if (mMutableSprites.ContainsKey(key))
            {
                SPSprite sprite = mMutableSprites[key];
                SPTexture texture = mScene.TextureByName(textureName);

                if (texture != null)
                {
                    sprite.RemoveAllChildren();

                    for (int i = 0; i < repeats; ++i)
                    {
                        SPImage image = new SPImage(texture);
                        image.X = i * image.Width;
                        sprite.AddChild(image);
                    }
                }
            }
        }

        public void Deconstruct()
        {
            if (mMiscProps != null)
            {
                foreach (Prop prop in mMiscProps)
                    mScene.RemoveProp(prop);
            }

            if (mMiscProps != null)
                mMiscProps.Clear();
            if (mMutableLabels != null)
                mMutableLabels.Clear();
            if (mLabelArrays != null)
                mLabelArrays.Clear();
            if (mMutableImages != null)
                mMutableImages.Clear();
            if (mButtons != null)
                mButtons.Clear();
            if (mMutableSprites != null)
                mMutableSprites.Clear();
            RemoveAllChildren();
        }

        public SPDisplayObject ControlForKey(string key)
        {
            SPDisplayObject control = null;

            if (mMutableLabels.ContainsKey(key))
                control = mMutableLabels[key] as SPDisplayObject;

            if (control == null && mMutableImages.ContainsKey(key))
                control = mMutableImages[key] as SPDisplayObject;

            if (control == null && mButtons.ContainsKey(key))
                control = mButtons[key] as SPDisplayObject;

            if (control == null && mMutableSprites.ContainsKey(key))
                control = mMutableSprites[key] as SPDisplayObject;

            return control;
        }

        public void SetControlForKey(SPDisplayObject control, string key)
        {
            if (key == null)
                return;
            else if (control is Prop)
                mMiscProps.Add(control as Prop);
            else if (control is SPTextField)
                mMutableLabels[key] = control as SPTextField;
            else if (control is SPImage)
                mMutableImages[key] = control as SPImage;
            else if (control is SPButton)
            {
                mButtons[key] = control as SPButton;

                if (mButtonsProxy != null && control is MenuButton)
                    mButtonsProxy.AddButton(control as MenuButton, Microsoft.Xna.Framework.Input.Buttons.A);
            }
            else if (control is SPSprite)
                mMutableSprites[key] = control as SPSprite;
        }

        public void RemoveControlForKey(SPDisplayObject control, string key)
        {
            if (key == null)
                return;
            else if (control is Prop)
                mMiscProps.Remove(control as Prop);
            else if (control is SPTextField)
                mMutableLabels.Remove(key);
            else if (control is SPImage)
                mMutableImages.Remove(key);
            else if (control is SPButton)
            {
                mButtons.Remove(key);

                if (mButtonsProxy != null && control is MenuButton)
                    mButtonsProxy.RemoveButton(control as MenuButton);
            }
            else if (control is SPSprite)
                mMutableSprites.Remove(key);
        }

        public List<SPTextField> LabelArrayForKey(string key)
        {
            List<SPTextField> labelArray = null;

            if (mLabelArrays.ContainsKey(key))
                labelArray = mLabelArrays[key];

            return labelArray;
        }

        public void SetLabelArrayForKey(List<SPTextField> array, string key)
        {
            if (array != null && key != null)
                mLabelArrays[key] = array;
        }

        public void RemoveLabelArrayForKey(string key)
        {
            if (key != null)
                mLabelArrays.Remove(key);
        }

        public void AddMiscProp(Prop prop)
        {
            if (prop != null)
                mMiscProps.Add(prop);
        }

        public void RemoveMiscProp(Prop prop)
        {
            if (prop != null)
                mMiscProps.Remove(prop);
        }

        public void AddFlipProp(Prop prop)
        {
            if (prop != null)
                mFlipProps.Add(prop);
        }

        public void RemoveFlipProp(Prop prop)
        {
            if (prop != null)
                mFlipProps.Remove(prop);
        }

        public void AddLoopingTween(SPTween tween)
        {
            if (tween != null)
                mLoopingTweens.Add(tween);
        }

        public void RemoveLoopingTween(SPTween tween)
        {
            if (tween != null)
                mLoopingTweens.Remove(tween);
        }

        public void EnableButtonForKey(bool enable, string key)
        {
            if (mButtons.ContainsKey(key))
                mButtons[key].Enabled = enable;
        }

        public void SetVisibleForKey(bool value, string key)
        {
            SPDisplayObject control = ControlForKey(key);

            if (control != null)
                control.Visible = value;
        }

        public override void Flip(bool enable)
        {
            float flipScaleX = (enable) ? -1 : 1;
    
            foreach (Prop prop in mFlipProps)
                prop.ScaleX = flipScaleX;
        }

        public virtual void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mButtonsProxy != null)
                mButtonsProxy.Update(gpState, kbState);
        }

        public override void AdvanceTime(double time)
        {
            foreach (Prop prop in mMiscProps)
                prop.AdvanceTime(time);

            if (mButtonsProxy != null)
                mButtonsProxy.AdvanceTime(time);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mLoopingTweens != null)
                        {
                            foreach (SPTween tween in mLoopingTweens)
                            {
                                object target = tween.Target;
                                mScene.Juggler.RemoveTweensWithTarget(target);
                            }

                            mLoopingTweens = null;
                        }

                        mButtonsProxy = null;
                        mMutableLabels = null;
                        mLabelArrays = null;
                        mMutableImages = null;
                        mButtons = null;
                        mMutableSprites = null;
                        mMiscProps = null;
                        mFlipProps = null;
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
