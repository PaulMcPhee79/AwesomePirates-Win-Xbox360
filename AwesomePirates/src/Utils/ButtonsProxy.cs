using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class ButtonsProxy : SPEventDispatcher, IInteractable, INavigable
    {
        public const string CUST_EVENT_TYPE_DID_NAVIGATE_BUTTONS = "buttonsProxyDidNavigateEvent";

        public ButtonsProxy(uint inputFocus, uint navMap = Globals.kNavNone, bool dispatchesEvents = false)
        {
            mUINavigator = new UINavigator(navMap);
            mDispatchesEvents = dispatchesEvents;
            mInputFocus = inputFocus;
            mButtons = new Dictionary<MenuButton, Buttons>(5);

            mLocked = false;
            mAddQueue = new Dictionary<MenuButton, Buttons>(5);
            mRemoveQueue = new List<MenuButton>(5);
        }

        #region Fields
        private bool mDispatchesEvents;
        private UINavigator mUINavigator;
        private uint mInputFocus;
        private MenuButton mFocusDeselectedButton;
        private Dictionary<MenuButton, Buttons> mButtons;

        private bool mLocked;
        private Dictionary<MenuButton, Buttons> mAddQueue;
        private List<MenuButton> mRemoveQueue;
        #endregion

        #region Properties
        public bool Repeats
        {
            get { return (mUINavigator != null) ? mUINavigator.Repeats : false; }
            set
            {
                if (mUINavigator != null)
                    mUINavigator.Repeats = value;
            }
        }
        public double RepeatDelay
        {
            get { return (mUINavigator != null) ? mUINavigator.RepeatDelay : 0; }
            set
            {
                if (mUINavigator != null)
                    mUINavigator.RepeatDelay = value;
            }
        }
        public virtual uint NavMap { get { return (mUINavigator != null) ? mUINavigator.NavMap : Globals.kNavNone; } set { if (mUINavigator != null) mUINavigator.NavMap = value; } }
        public SPDisplayObject CurrentNav { get { return (mUINavigator != null) ? mUINavigator.CurrentNav : null; } }
        public int NavIndex { get { return (mUINavigator != null) ? mUINavigator.NavIndex : -1; } }
        public virtual uint InputFocus { get { return mInputFocus; } set { mInputFocus = value; } }
        public MenuButton SelectedButton
        {
            get
            {
                MenuButton selectedButton = null;

                if (mButtons != null)
                {
                    foreach (KeyValuePair<MenuButton, Buttons> kvp in mButtons)
                    {
                        if (kvp.Key.Selected)
                        {
                            selectedButton = kvp.Key;
                            break;
                        }
                    }
                }

                return selectedButton;
            }
        }
        #endregion

        #region Methods
        public void ResetNav()
        {
            if (mUINavigator != null)
                mUINavigator.ResetNav();
        }

        public void MovePrevNav()
        {
            if (mUINavigator != null)
                mUINavigator.MovePrevNav();
        }

        public void MoveNextNav()
        {
            if (mUINavigator != null)
                mUINavigator.MoveNextNav();
        }

        public void AddButton(MenuButton menuButton, Buttons xnaButton = Buttons.A)
        {
            if (menuButton == null)
                return;

            if (!mLocked)
            {
                mButtons[menuButton] = xnaButton;

                if (mUINavigator != null && menuButton.IsNavigable)
                    mUINavigator.AddNav(menuButton);
            }
            else
            {
                if (!mAddQueue.ContainsKey(menuButton))
                {
                    mRemoveQueue.Remove(menuButton);
                    mAddQueue[menuButton] = xnaButton;
                }
            }
        }

        public void RemoveButton(MenuButton menuButton)
        {
            if (menuButton == null)
                return;

            if (!mLocked)
            {
                menuButton.Selected = false;

                if (mButtons != null)
                    mButtons.Remove(menuButton);
                if (mUINavigator != null)
                    mUINavigator.RemoveNav(menuButton);
                if (menuButton == mFocusDeselectedButton)
                    mFocusDeselectedButton = null;
            }
            else
            {
                if (!mRemoveQueue.Contains(menuButton))
                {
                    mAddQueue.Remove(menuButton);
                    mRemoveQueue.Add(menuButton);
                }
            }
        }

        public void Clear()
        {
            if (mButtons != null)
            {
                Dictionary<MenuButton, Buttons> buttons = new Dictionary<MenuButton, Buttons>(mButtons);

                foreach (KeyValuePair<MenuButton, Buttons> kvp in buttons)
                    RemoveButton(kvp.Key);
            }

            mFocusDeselectedButton = null;
        }

        public virtual void DidGainFocus()
        {
            if (mFocusDeselectedButton != null && mFocusDeselectedButton == CurrentNav)
                mFocusDeselectedButton.Selected = true;

            mFocusDeselectedButton = null;
        }

        public virtual void WillLoseFocus()
        {
            MenuButton selectedButton = SelectedButton;
            if (selectedButton != null)
            {
                selectedButton.Selected = false;
                mFocusDeselectedButton = selectedButton;
            }
            else
                mFocusDeselectedButton = null;
        }

        public virtual void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mButtons == null)
                return;

            ControlsManager cm = ControlsManager.CM;

            mLocked = true;
            foreach (KeyValuePair<MenuButton, Buttons> kvp in mButtons)
            {
                if (kvp.Key.Selected)
                {
                    if (cm.DidButtonDepress(kvp.Value))
                        kvp.Key.AutomatedButtonDepress();
                    else if (cm.DidButtonRelease(kvp.Value))
                        kvp.Key.AutomatedButtonRelease();
                }
                else
                {
                    kvp.Key.AutomatedButtonRelease(false);
                }
            }
            mLocked = false;

            if (mAddQueue.Count > 0)
            {
                foreach (KeyValuePair<MenuButton, Buttons> kvp in mAddQueue)
                    AddButton(kvp.Key, kvp.Value);
                mAddQueue.Clear();
            }

            if (mRemoveQueue.Count > 0)
            {
                foreach (MenuButton button in mRemoveQueue)
                    RemoveButton(button);
                mRemoveQueue.Clear();
            }

            if (mUINavigator != null)
            {
                int navIndex = mUINavigator.NavIndex;
                mUINavigator.Update(gpState, kbState);

                if (mDispatchesEvents && navIndex != mUINavigator.NavIndex)
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_DID_NAVIGATE_BUTTONS));
            }
        }

        public virtual void AdvanceTime(double time)
        {
            if (mUINavigator != null)
            {
                int navIndex = mUINavigator.NavIndex;
                mUINavigator.AdvanceTime(time);

                if (mDispatchesEvents && navIndex != mUINavigator.NavIndex)
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_DID_NAVIGATE_BUTTONS));
            }
        }
        #endregion
    }
}
