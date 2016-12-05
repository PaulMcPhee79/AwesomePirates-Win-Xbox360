using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    sealed class UINavigator : INavigable
    {
        public UINavigator(uint navMap = Globals.kNavNone)
        {
            mRepeats = false;
            mRepeatDir = 0;
            mRepeatCounter = 0;
            mRepeatDelay = 0.25;
            mNavIndex = 0;
            mNavMap = navMap;
            mNavigables = new List<SPDisplayObject>();
        }

        #region Fields
        private bool mRepeats;
        private int mRepeatDir;
        private double mRepeatCounter;
        private double mRepeatDelay;

        private int mNavIndex;
        private uint mNavMap;
        private List<SPDisplayObject> mNavigables;
        #endregion

        #region Properties
        public bool Repeats { get { return mRepeats; } set { mRepeats = value; } }
        private int RepeatDir
        {
            get { return mRepeatDir; }
            set
            {
                if (mRepeatDir != value)
                {
                    mRepeatDir = value;
                    mRepeatCounter = 2 * mRepeatDelay;
                }
            }
        }
        public double RepeatDelay { get { return mRepeatDelay; } set { mRepeatDelay = Math.Max(0.1, value); } }
        public uint NavMap { get { return mNavMap; } set { mNavMap = value; } }
        public int NavIndex { get { return mNavIndex; } }
        public int NavCount { get { return mNavigables != null ? mNavigables.Count : 0; } }
        public SPDisplayObject CurrentNav { get { return (mNavigables != null && mNavIndex < mNavigables.Count) ? mNavigables[mNavIndex] : null; } }
        #endregion

        #region Methods
        private void ActivateNav(SPDisplayObject nav)
        {
            if (nav != null && nav is MenuButton)
                (nav as MenuButton).Selected = true;
        }

        private void DeactivateNav(SPDisplayObject nav)
        {
            if (nav != null && nav is MenuButton)
                (nav as MenuButton).Selected = false;
        }

        public void AddNav(SPDisplayObject nav)
        {
            if (mNavigables != null && nav != null && !mNavigables.Contains(nav))
            {
                mNavigables.Add(nav);

                if (mNavigables.Count == 1)
                    ActivateNav(nav);
            }
        }

        public void RemoveNav(SPDisplayObject nav)
        {
            if (mNavigables != null && nav != null)
            {
                if (nav == CurrentNav)
                {
                    DeactivateNav(nav);
                    mNavigables.Remove(nav);
                    MovePrevNav();
                }
                else
                {
                    mNavigables.Remove(nav);
                }
            }
        }

        public void ResetNav()
        {
            DeactivateNav(CurrentNav);
            mNavIndex = 0;
            ActivateNav(CurrentNav);
        }

        // Skips invisible navs
        public void ActivateNextActiveNav(int dir)
        {
            if (mNavigables != null && CurrentNav != null && mNavigables.Count > 0 && (dir == 1 || dir == -1))
            {
                int startIndex = mNavigables.IndexOf(CurrentNav);
                int i = startIndex + dir;
                while (i != startIndex) 
                {
                    if (dir == 1)
                        MoveNextNav();
                    else
                        MovePrevNav();

                    if (i < 0)
                    {
                        i = mNavigables.Count - 1;
                        continue;
                    }
                    else if (i >= mNavigables.Count)
                    {
                        i = 0;
                        continue;
                    }

                    SPDisplayObject nav = mNavigables[i];
                    if (nav.Visible)
                        break;

                    i += dir;
                }
            }
        }

        public void MovePrevNav()
        {
            if (mNavigables != null && mNavigables.Count > 0)
            {
                DeactivateNav(CurrentNav);
                --mNavIndex;

                if (mNavIndex < 0)
                    mNavIndex = mNavigables.Count - 1;

                ActivateNav(CurrentNav);
            }
        }

        public void MoveNextNav()
        {
            if (mNavigables != null && mNavigables.Count > 0)
            {
                DeactivateNav(CurrentNav);
                ++mNavIndex;

                if (mNavIndex >= mNavigables.Count)
                    mNavIndex = 0;

                ActivateNav(CurrentNav);
            }
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            ControlsManager cm = ControlsManager.CM;
            SPDisplayObject prevNav = CurrentNav;
            int didNavigate = 0, didRepeat = 0;

            if ((mNavMap & Globals.kNavVertical) == Globals.kNavVertical)
            {
                if (cm.DidButtonDepress(Buttons.DPadUp) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLUp))
                {
                    MovePrevNav();
                    didNavigate = -1;
                }
                else if (cm.DidButtonDepress(Buttons.DPadDown) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLDown))
                {
                    MoveNextNav();
                    didNavigate = 1;
                }

                if (Repeats && didNavigate == 0)
                {
                    if (cm.IsButtonDown(Buttons.DPadUp) || cm.IsThumbstickActivated(InputManager.ThumbStickDir.TLUp))
                    {
                        RepeatDir = didRepeat = -1;
                    }
                    else if (cm.IsButtonDown(Buttons.DPadDown) || cm.IsThumbstickActivated(InputManager.ThumbStickDir.TLDown))
                    {
                        RepeatDir = didRepeat = 1;
                    }
                }
            }

            if (didNavigate == 0 && (mNavMap & Globals.kNavHorizontal) == Globals.kNavHorizontal)
            {
                if (cm.DidButtonDepress(Buttons.DPadLeft) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLLeft))
                {
                    MovePrevNav();
                    didNavigate = -1;
                }
                else if (cm.DidButtonDepress(Buttons.DPadRight) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLRight))
                {
                    MoveNextNav();
                    didNavigate = 1;
                }

                if (Repeats && didRepeat == 0 && didNavigate == 0)
                {
                    if (cm.IsButtonDown(Buttons.DPadLeft) || cm.IsThumbstickActivated(InputManager.ThumbStickDir.TLLeft))
                    {
                        RepeatDir = didRepeat = -1;
                    }
                    else if (cm.IsButtonDown(Buttons.DPadRight) || cm.IsThumbstickActivated(InputManager.ThumbStickDir.TLRight))
                    {
                        RepeatDir = didRepeat = 1;
                    }
                }
            }

            if (didRepeat == 0)
                RepeatDir = 0;

            // Skip invisible navs
            if (didNavigate != 0 && CurrentNav != null && !CurrentNav.Visible)
                ActivateNextActiveNav(didNavigate);
        }

        public void AdvanceTime(double time)
        {
            if (mRepeats && RepeatDir != 0)
            {
                mRepeatCounter -= time;

                if (mRepeatCounter <= 0)
                {
                    mRepeatCounter = mRepeatDelay;

                    if (RepeatDir == -1)
                        MovePrevNav();
                    else
                        MoveNextNav();

                    if (CurrentNav != null && !CurrentNav.Visible)
                        ActivateNextActiveNav(RepeatDir);
                }
            }
        }
        #endregion
    }
}
