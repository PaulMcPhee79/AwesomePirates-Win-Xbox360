using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    class VoodooWheel : Prop, IInteractable
    {
        public const string CUST_EVENT_TYPE_VOODOO_MENU_CLOSING = "voodooMenuClosingEvent";

        public enum VoodooWheelState
        {
            Hidden = 0,
            Showing,
            Shown,
            Hiding
        }

        private const float kShowTime = 0.5f;
        private const float kHideTime = 0.5f;
        private const float kButtonSize = 96.0f;
        private const float kCooldownAlpha = 0.5f;
        
        public VoodooWheel(int category, List<Idol> trinkets, List<Idol> gadgets)
            : base(category)
        {
            mTrinketSettings = trinkets;
		    mGadgetSettings = gadgets;
		    mMaxWidth = 0;
		    mMaxHeight = 0;
            mSelectButtons = new Buttons[] { Buttons.A, Buttons.X, Buttons.LeftStick, Buttons.RightStick };
		    mCancelButton = null;
            mCanvas = null;
		    mActivePulse = null;
            mSelectedDial = null;
		    mGadgets = new Dictionary<uint,VoodooDial>(4);
		    mTrinkets = new Dictionary<uint,VoodooDial>(5);
		    mVoodooDialDictionary = new Dictionary<uint,VoodooDial>(9);
		    mVoodooDialArray = new List<VoodooDial>(9);

            SetState(VoodooWheelState.Hidden);
            SetupProp();

            mScene.SubscribeToInputUpdates(this);
        }
        
        #region Fields
        private VoodooWheelState mState;
        private float mMaxWidth;
        private float mMaxHeight;

        private Buttons[] mSelectButtons;

        private MenuButton mCancelButton;
        private SPSprite mCanvas;

        private List<Idol> mTrinketSettings;
        private List<Idol> mGadgetSettings;

        private VoodooDial mActivePulse;
        private VoodooDial mSelectedDial;
        private Dictionary<uint, VoodooDial> mGadgets;
        private Dictionary<uint, VoodooDial> mTrinkets;
        private List<VoodooDial> mVoodooDialArray;
        private Dictionary<uint, VoodooDial> mVoodooDialDictionary;

        private SPTween mShowTween;
        private SPTween mHideTween;

        private Tooltip mTooltip;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_VOODOO_WHEEL; } }
        public VoodooWheelState State { get { return mState; } }
        public List<Idol> TrinketSettings { get { return mTrinketSettings; } }
        public List<Idol> GadgetSettings { get { return mGadgetSettings; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            mTooltip = new Tooltip((int)PFCat.DIALOGS, 0.7f, 1f);

            if (mScene.ViewWidth / mScene.ViewHeight < 1.5f)
            {
                float tipAspectRatio = mTooltip.Width / mTooltip.Height;
                mTooltip.ScaleX = 330f / mTooltip.Width;
                mTooltip.ScaleY = mTooltip.ScaleX / tipAspectRatio;
                mTooltip.Y = mScene.ViewHeight - (84 + mTooltip.Height / 2);
            }
            else
            {
                mTooltip.Y = mScene.ViewHeight - (72 + mTooltip.Height / 2);
            }

            mScene.AddProp(mTooltip);

            foreach (Idol gadget in mGadgetSettings)
            {
                mTooltip.AddTip(gadget.Key, Idol.NameForIdol(gadget), Idol.DescForIdol(gadget));
                AddGadgetWithKey(gadget.Key);
            }

            foreach (Idol trinket in mTrinketSettings)
            {
                mTooltip.AddTip(trinket.Key, Idol.NameForIdol(trinket), Idol.DescForIdol(trinket));
                AddTrinketWithKey(trinket.Key);
            }
	
            // Canvas
            mCanvas = new SPSprite();
            AddChild(mCanvas);
    
	        // Cancel
	        mCancelButton = new MenuButton(null, mScene.TextureByName("voodoo-cancel-icon"));
	        mCancelButton.ScaleWhenDown = 1.4f;
	        mCancelButton.X = -mCancelButton.Width / 2;
	        mCancelButton.Y = -mCancelButton.Height / 2;
            mCancelButton.AddActionEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, new Action<SPEvent>(OnButtonPressed));
            mCanvas.AddChild(mCancelButton);

            if (mCancelButton.SelectedEffecter != null)
                mCancelButton.SelectedEffecter.Factor = 1.75f;
	
	        mMaxWidth = mCancelButton.Height;
	        mMaxHeight = mCancelButton.Width;
	
	        // Position elements
	        if (mVoodooDialArray.Count > 0)
            {
		        Vector2 point = new Vector2(0.0f, -1.6f * kButtonSize);
		        float angularSpacing = SPMacros.TWO_PI / mVoodooDialArray.Count;
                Globals.RotatePointThroughAngle(ref point, -1.5f * angularSpacing);
		
		        foreach (VoodooDial dial in mVoodooDialArray)
                {
			        dial.X = point.X - dial.Width / 2;
			        dial.Y = point.Y - dial.Height / 2;
                    mCanvas.AddChild(dial);
                    Globals.RotatePointThroughAngle(ref point, angularSpacing);
			        mMaxWidth = Math.Max(2 * (Math.Abs(point.X) + dial.Width / 2), mMaxWidth);
			        mMaxHeight = Math.Max(2 * (Math.Abs(point.Y) + dial.Height / 2), mMaxHeight);
                    dial.AddActionEventListener(VoodooDial.CUST_EVENT_TYPE_VOODOO_DIAL_PRESSED, new Action<SPEvent>(OnButtonPressed));
		        }
	        }

            // Tweens
            Reset();

            mShowTween = new SPTween(this, (1.0f - Math.Abs(ScaleX)) * kShowTime);
            mShowTween.AnimateProperty("ScaleX", 1);
            mShowTween.AnimateProperty("ScaleY", 1);
            mShowTween.AnimateProperty("Rotation", Rotation + SPMacros.TWO_PI);
            mShowTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnWheelShown));

            ScaleX = ScaleY = 1;
            Rotation = Rotation + SPMacros.TWO_PI;

            mHideTween = new SPTween(this, Math.Abs(ScaleX) * kHideTime);
            mHideTween.AnimateProperty("ScaleX", 0);
            mHideTween.AnimateProperty("ScaleY", 0);
            mHideTween.AnimateProperty("Rotation", Rotation - SPMacros.TWO_PI);
            mHideTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnWheelHidden));
        }

        private void SetState(VoodooWheelState state)
        {
            switch (state)
            {
		        case VoodooWheelState.Hidden:
			        Visible = false;
			        break;
		        case VoodooWheelState.Showing:
                    Reset();
			        Visible = true;
			        break;
		        case VoodooWheelState.Shown:
			        Touchable = true;
			        break;
		        case VoodooWheelState.Hiding:
			        Touchable = false;
			        break;
	        }

	        mState = state;
        }

        public override void Flip(bool enable)
        {
            mCanvas.ScaleX = (enable) ? -1 : 1;
        }

        private void AddGadgetWithKey(uint key)
        {
            if (key == 0)
                return;
	        VoodooDial dial = new VoodooDial(Category, key);
            dial.Tag = key;
            mGadgets.Add(key, dial);
            mVoodooDialDictionary.Add(key, dial);
            mVoodooDialArray.Add(dial);
        }

        private void AddTrinketWithKey(uint key)
        {
            if (key == 0)
                return;
            VoodooDial dial = new VoodooDial(Category, key);
            dial.Tag = key;
            mTrinkets.Add(key, dial);
            mVoodooDialDictionary.Add(key, dial);
            mVoodooDialArray.Add(dial);
        }

        public VoodooDial DialForKey(uint key)
        {
            if (mVoodooDialDictionary == null)
                return null;

            VoodooDial dial;
            mVoodooDialDictionary.TryGetValue(key, out dial);
            return dial;
        }

        public void ShowAt(float x, float y)
        {
            if (mState == VoodooWheelState.Shown || mState == VoodooWheelState.Showing)
		        return;
            mScene.HudJuggler.RemoveTweensWithTarget(this);
            SetState(VoodooWheelState.Showing);
	        X = Math.Min(x, mScene.ViewWidth - 0.5f * mMaxWidth);
	        Y = Math.Max(Math.Min(y, mScene.ViewHeight - (110 + 0.5f * mMaxHeight)), 0.5f * mMaxHeight); // 110 is approx height of deck railing + deck idol

            mShowTween.Reset();
            mScene.HudJuggler.AddObject(mShowTween);

            if (mTooltip != null)
                mTooltip.X = (mScene.Flipped ? mScene.ViewWidth - x : x) + 206 + mTooltip.Width / 2;
        }

        public void Hide()
        {
            if (mState == VoodooWheelState.Hidden || mState == VoodooWheelState.Hiding)
		        return;
            mScene.HudJuggler.RemoveTweensWithTarget(this);
            SetState(VoodooWheelState.Hiding);

            mHideTween.Reset();
            mScene.HudJuggler.AddObject(mHideTween);

            if (mTooltip != null)
                mTooltip.HideTip();
        }

        private void Reset()
        {
            ScaleX = 0;
            ScaleY = 0;
            Rotation = 0;
            ResetDialHighlights();
            HighlightCancelButton(true);
        }

        private void ResetDialHighlights()
        {
            foreach (VoodooDial dial in mVoodooDialArray)
                dial.Highlight(false);
        }

        private void HighlightCancelButton(bool enable)
        {
            if (mCancelButton != null)
                mCancelButton.Selected = enable;
        }

        private void ApplyDialDecorations(VoodooDial dial)
        {
            if (mSelectedDial == dial)
                return;

            if (mSelectedDial != null)
            {
                mSelectedDial.Highlight(false);

                if (mSelectedDial.Button != null)
                    mSelectedDial.Button.AutomatedButtonRelease(false);
            }

            mSelectedDial = dial;

            if (dial != null)
            {
                dial.Highlight(true);
                mCanvas.AddChild(dial); // Bubble to top

                HighlightCancelButton(false);
                mCancelButton.AutomatedButtonRelease(false);
            }
            else
                HighlightCancelButton(true);
        }

        public void DidGainFocus() { }

        public void WillLoseFocus()
        {
            if (mSelectedDial != null && mSelectedDial.Button != null)
                mSelectedDial.Button.AutomatedButtonRelease(false);
            else if (mCancelButton != null)
                mCancelButton.AutomatedButtonRelease(false);
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mState != VoodooWheelState.Shown)
                return;

            ControlsManager cm = ControlsManager.CM;
            SPButton selectedButton = mCancelButton;
            VoodooDial selectedDial = DialForThumbstickVector(gpState.ThumbSticks.Left);

            ApplyDialDecorations(selectedDial);

            if (selectedDial != null)
            {
                if (mTooltip != null)
                    mTooltip.DisplayTip(selectedDial.Key);

                if (selectedDial.Button != null)
                    selectedButton = selectedDial.Button;
            }
            else if (mTooltip != null)
                mTooltip.HideTip();

            foreach (Buttons button in mSelectButtons)
            {
                if (cm.DidButtonDepress(button))
                    selectedButton.AutomatedButtonDepress();
                else if (cm.DidButtonRelease(button))
                    selectedButton.AutomatedButtonRelease();
            }
        }

        private VoodooDial DialForThumbstickVector(Vector2 pos)
        {
            if (SPMacros.SP_IS_FLOAT_EQUAL(pos.X, 0) && SPMacros.SP_IS_FLOAT_EQUAL(pos.Y, 0))
                return null;

            VoodooDial selectedDial = null;
            float posAngle = Globals.VectorToAngle(new Vector2(pos.X, -pos.Y));
            float angularSpacing = SPMacros.TWO_PI / mVoodooDialArray.Count;

            if (posAngle > SPMacros.TWO_PI)
                posAngle -= SPMacros.TWO_PI;

            Vector2 point = new Vector2(0, -1);
            Globals.RotatePointThroughAngle(ref point, -1.5f * angularSpacing);

            foreach (VoodooDial dial in mVoodooDialArray)
            {
                float pointAngle = Globals.VectorToAngle(point);

                if (Math.Abs(pointAngle - posAngle) < angularSpacing / 2)
                {
                    selectedDial = dial;
                    break;
                }
                Globals.RotatePointThroughAngle(ref point, angularSpacing);
            }

            return selectedDial;
        }

        public void EnableItem(bool enable, uint key)
        {
            VoodooDial dial;
            mVoodooDialDictionary.TryGetValue(key, out dial);

            if (dial != null)
                dial.Enabled = enable;
        }

        public void EnableAllItems(bool enable)
        {
            foreach (VoodooDial dial in mVoodooDialArray)
		        dial.Enabled = enable;
        }

        public void HookDialButtons()
        {
            if (mVoodooDialArray == null)
                return;

            UnhookDialButtons();

            foreach (VoodooDial dial in mVoodooDialArray)
                dial.AddActionEventListener(VoodooDial.CUST_EVENT_TYPE_VOODOO_DIAL_PRESSED, new Action<SPEvent>(OnButtonPressed));
        }

        public void UnhookDialButtons()
        {
            if (mVoodooDialArray == null)
                return;

            foreach (VoodooDial dial in mVoodooDialArray)
                dial.RemoveEventListener(VoodooDial.CUST_EVENT_TYPE_VOODOO_DIAL_PRESSED);
        }

        private void OnWheelShown(SPEvent ev)
        {
            SetState(VoodooWheelState.Shown);
        }

        private void OnWheelHidden(SPEvent ev)
        {
            SetState(VoodooWheelState.Hidden);
        }

        private void OnButtonPressed(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_VOODOO_MENU_CLOSING));
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mScene.UnsubscribeToInputUpdates(this);
                        mScene.HudJuggler.RemoveTweensWithTarget(this);

                        if (mVoodooDialArray != null)
                        {
                            foreach (VoodooDial dial in mVoodooDialArray)
                                dial.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED);
                            mVoodooDialArray = null;
                        }

                        if (mCancelButton != null)
                        {
                            mCancelButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED);
                            mCancelButton = null;
                        }

                        if (mShowTween != null)
                        {
                            mShowTween.RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mShowTween = null;
                        }

                        if (mHideTween != null)
                        {
                            mHideTween.RemoveEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED);
                            mHideTween = null;
                        }

                        if (mTooltip != null)
                        {
                            mScene.RemoveProp(mTooltip);
                            mTooltip = null;
                        }

                        mActivePulse = null;
                        mGadgets = null;
                        mTrinkets = null;
                        mVoodooDialArray = null;
                        mVoodooDialDictionary = null;
                        mTrinketSettings = null;
                        mGadgetSettings = null;
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
