using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace AwesomePirates
{
    class InputManager : IDisposable
    {
        // Thumbsticks
        public const float kThumbStickActivationThreshold = 0.75f;
        public const float kThumbStickDeactivationThreshold = 0.35f;

        public enum ThumbStickDir
        {
            TLLeft = 0x1,
            TLRight = 0x2,
            TLDown = 0x4,
            TLUp = 0x8,
            TLMask = 0xF,

            TRLeft = 0x10,
            TRRight = 0x20,
            TRDown = 0x40,
            TRUp = 0x80,
            TRMask = 0xF0
        }

        // Notes:
        // Focus State  : [8 bits category | 24 bits state]
        // Has Focus    : [8 bits category | 24 bits individual settings]

        protected const int NUM_STATE_BITS = 24;
        protected const uint FOCUS_CAT_MASK = 0xff000000;
        protected const uint HAS_FOCUS_MASK = ~FOCUS_CAT_MASK;

        // ***** Focus categories *****
        protected const uint FOCUS_CAT_TITLE = 1 << NUM_STATE_BITS;
        protected const uint FOCUS_CAT_MENU = 1 << (NUM_STATE_BITS + 1);
        protected const uint FOCUS_CAT_PLAYFIELD = 1 << (NUM_STATE_BITS + 2);
        protected const uint FOCUS_CAT_SYSTEM = 1 << (NUM_STATE_BITS + 3);

        // ***** Focus states *****
        public const uint FOCUS_STATE_NONE = 0;

            // FOCUS_CAT_TITLE
            public const uint FOCUS_STATE_TITLE = FOCUS_CAT_TITLE + 1;

            // FOCUS_CAT_MENU
            public const uint FOCUS_STATE_MENU                          = FOCUS_CAT_MENU + 1;
            public const uint FOCUS_STATE_MENU_POTIONS                  = FOCUS_CAT_MENU + 2;
            public const uint FOCUS_STATE_MENU_INFO                     = FOCUS_CAT_MENU + 3;
            public const uint FOCUS_STATE_MENU_INFO_STATS               = FOCUS_CAT_MENU + 4;
            public const uint FOCUS_STATE_MENU_INFO_GAME_CONCEPTS       = FOCUS_CAT_MENU + 5;
            public const uint FOCUS_STATE_MENU_INFO_SPELLS_MUNITIONS    = FOCUS_CAT_MENU + 6;
            public const uint FOCUS_STATE_MENU_CREDITS                  = FOCUS_CAT_MENU + 7;
            public const uint FOCUS_STATE_MENU_OBJECTIVES_LOG           = FOCUS_CAT_MENU + 8;
            public const uint FOCUS_STATE_MENU_ACHIEVEMENTS             = FOCUS_CAT_MENU + 9;
            public const uint FOCUS_STATE_MENU_OPTIONS                  = FOCUS_CAT_MENU + 10;
            public const uint FOCUS_STATE_MENU_QUERY                    = FOCUS_CAT_MENU + 11;
            public const uint FOCUS_STATE_MENU_ALERT                    = FOCUS_CAT_MENU + 12;
            public const uint FOCUS_STATE_MENU_DISPLAY_ADJUST           = FOCUS_CAT_MENU + 13;
            public const uint FOCUS_STATE_MENU_LEADERBOARD              = FOCUS_CAT_MENU + 14;
            public const uint FOCUS_STATE_MENU_MASTERY                  = FOCUS_CAT_MENU + 15;
            public const uint FOCUS_STATE_MENU_MODE_SELECT              = FOCUS_CAT_MENU + 16;

            // FOCUS_CAT_PLAYFIELD
            public const uint FOCUS_STATE_PF_PLAYFIELD                  = FOCUS_CAT_PLAYFIELD + 1;
            public const uint FOCUS_STATE_PF_VOODOO_WHEEL               = FOCUS_CAT_PLAYFIELD + 2;
            public const uint FOCUS_STATE_PF_PAUSE                      = FOCUS_CAT_PLAYFIELD + 3;
            public const uint FOCUS_STATE_PF_OBJECTIVES_RANKUP          = FOCUS_CAT_PLAYFIELD + 4;
            public const uint FOCUS_STATE_PF_GAMEOVER                   = FOCUS_CAT_PLAYFIELD + 5;
            public const uint FOCUS_STATE_PF_SK_GAMEOVER                = FOCUS_CAT_PLAYFIELD + 6;
            public const uint FOCUS_STATE_PF_TUTORIAL                   = FOCUS_CAT_PLAYFIELD + 7;
            public const uint FOCUS_STATE_PF_HELP                       = FOCUS_CAT_PLAYFIELD + 8;

            // FOCUS_CAT_SYSTEM
            public const uint FOCUS_STATE_SYS_EXIT                      = FOCUS_CAT_SYSTEM + 1;
            public const uint FOCUS_STATE_SYS_STORAGE                   = FOCUS_CAT_SYSTEM + 2;
            public const uint FOCUS_STATE_SYS_DEBUG                     = FOCUS_CAT_SYSTEM + 3;


        // ***** Individual bit settings *****

            // FOCUS_CAT_TITLE
            public const uint HAS_FOCUS_TITLE                           = FOCUS_CAT_TITLE + 0x1;

            // FOCUS_CAT_MENU
            public const uint HAS_FOCUS_MENU                            = FOCUS_CAT_MENU + 0x1;
            public const uint HAS_FOCUS_MENU_POTIONS                    = FOCUS_CAT_MENU + 0x2;
            public const uint HAS_FOCUS_MENU_INFO                       = FOCUS_CAT_MENU + 0x4;
            public const uint HAS_FOCUS_MENU_INFO_STATS                 = FOCUS_CAT_MENU + 0x8;
            public const uint HAS_FOCUS_MENU_INFO_GAME_CONCEPTS         = FOCUS_CAT_MENU + 0x10;
            public const uint HAS_FOCUS_MENU_INFO_SPELLS_MUNITIONS      = FOCUS_CAT_MENU + 0x20;
            public const uint HAS_FOCUS_MENU_CREDITS                    = FOCUS_CAT_MENU + 0x40;
            public const uint HAS_FOCUS_MENU_OBJECTIVES_LOG             = FOCUS_CAT_MENU + 0x80;
            public const uint HAS_FOCUS_MENU_ACHIEVEMENTS               = FOCUS_CAT_MENU + 0x100;
            public const uint HAS_FOCUS_MENU_OPTIONS                    = FOCUS_CAT_MENU + 0x200;
            public const uint HAS_FOCUS_MENU_QUERY                      = FOCUS_CAT_MENU + 0x400;
            public const uint HAS_FOCUS_MENU_ALERT                      = FOCUS_CAT_MENU + 0x800;
            public const uint HAS_FOCUS_MENU_DISPLAY_ADJUST             = FOCUS_CAT_MENU + 0x1000;
            public const uint HAS_FOCUS_MENU_LEADERBOARD                = FOCUS_CAT_MENU + 0x2000;
            public const uint HAS_FOCUS_MENU_MASTERY                    = FOCUS_CAT_MENU + 0x4000;
            public const uint HAS_FOCUS_MENU_MODE_SELECT                = FOCUS_CAT_MENU + 0x8000;
            public const uint HAS_FOCUS_MENU_ALL                        = FOCUS_CAT_MENU + HAS_FOCUS_MASK;

            // FOCUS_CAT_PLAYFIELD
            public const uint HAS_FOCUS_DECK                            = FOCUS_CAT_PLAYFIELD + 0x1;
            public const uint HAS_FOCUS_VOODOO_WHEEL                    = FOCUS_CAT_PLAYFIELD + 0x2;
            public const uint HAS_FOCUS_PAUSE_BUTTON                    = FOCUS_CAT_PLAYFIELD + 0x4;
            public const uint HAS_FOCUS_PAUSE_MENU                      = FOCUS_CAT_PLAYFIELD + 0x8;
            public const uint HAS_FOCUS_OBJECTIVES_RANKUP               = FOCUS_CAT_PLAYFIELD + 0x10;
            public const uint HAS_FOCUS_GAMEOVER                        = FOCUS_CAT_PLAYFIELD + 0x20;
            public const uint HAS_FOCUS_SK_GAMEOVER                     = FOCUS_CAT_PLAYFIELD + 0x40;
            public const uint HAS_FOCUS_TUTORIAL                        = FOCUS_CAT_PLAYFIELD + 0x80;
            public const uint HAS_FOCUS_HELP                            = FOCUS_CAT_PLAYFIELD + 0x100;

            // FOCUS_CAT_SYSTEM
            public const uint HAS_FOCUS_EXIT_MENU                       = FOCUS_CAT_SYSTEM + 0x1;
            public const uint HAS_FOCUS_STORAGE_DIALOG                  = FOCUS_CAT_SYSTEM + 0x2;
            public const uint HAS_FOCUS_DEBUG_CONSOLE                   = FOCUS_CAT_SYSTEM + 0x4;
            

        public InputManager()
        {
            mBusyUpdatingClients = false;
            mFocusMap = mModalFocusMap = 0;
            mFocusStack = new List<uint>();
            mModalFocusStack = new List<uint>();
            mClients = new List<IInteractable>(25);
            mSubscribeQueue = new List<IInteractable>(10);
            mUnsubscribeQueue = new List<IInteractable>(10);
            mModalClients = new List<IInteractable>(5);
            mModalSubscribeQueue = new List<IInteractable>(5);
            mModalUnsubscribeQueue = new List<IInteractable>(5);
            PushFocusState(FOCUS_STATE_NONE);
            PushFocusState(FOCUS_STATE_NONE, true);
        }

        #region Fields
        protected bool mIsDisposed = false;
        protected bool mBusyUpdatingClients;
        private uint mFocusMap;
        private List<uint> mFocusStack;
        private List<IInteractable> mClients;
        private List<IInteractable> mSubscribeQueue;
        private List<IInteractable> mUnsubscribeQueue;

        private uint mModalFocusMap;
        private List<uint> mModalFocusStack;
        private List<IInteractable> mModalClients;
        private List<IInteractable> mModalSubscribeQueue;
        private List<IInteractable> mModalUnsubscribeQueue;
        #endregion

        #region Methods
        public void Subscribe(IInteractable client, bool modal = false)
        {
            if (client == null)
                return;

            List<IInteractable> clients = (modal) ? mModalClients : mClients;
            List<IInteractable> subscribeQueue = (modal) ? mModalSubscribeQueue : mSubscribeQueue;
            List<IInteractable> unsubscribeQueue = (modal) ? mModalUnsubscribeQueue : mUnsubscribeQueue;

            if (mBusyUpdatingClients)
            {
                if (!clients.Contains(client))
                {
                    if (!subscribeQueue.Contains(client))
                        subscribeQueue.Add(client);
                }

                unsubscribeQueue.Remove(client);
            }
            else
            {
                if (!clients.Contains(client))
                    clients.Add(client);
            }

            if (HasFocus(client.InputFocus))
                client.DidGainFocus();
            else
                client.WillLoseFocus();
        }

        public void Unsubscibe(IInteractable client, bool modal = false)
        {
            if (client == null)
                return;

            List<IInteractable> clients = (modal) ? mModalClients : mClients;
            List<IInteractable> subscribeQueue = (modal) ? mModalSubscribeQueue : mSubscribeQueue;
            List<IInteractable> unsubscribeQueue = (modal) ? mModalUnsubscribeQueue : mUnsubscribeQueue;

            if (mBusyUpdatingClients)
            {
                if (!unsubscribeQueue.Contains(client))
                    unsubscribeQueue.Add(client);

                subscribeQueue.Remove(client);
            }
            else
                clients.Remove(client);
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            List<IInteractable> clients = (mModalFocusMap != 0) ? mModalClients : mClients;

            mBusyUpdatingClients = true;
            foreach (IInteractable client in clients)
            {
                if (HasFocus(client.InputFocus))
                    client.Update(gpState, kbState);
            }
            mBusyUpdatingClients = false;

            foreach (IInteractable client in mSubscribeQueue)
                mClients.Add(client);
            foreach (IInteractable client in mUnsubscribeQueue)
                mClients.Remove(client);
            mSubscribeQueue.Clear();
            mUnsubscribeQueue.Clear();

            foreach (IInteractable client in mModalSubscribeQueue)
                mModalClients.Add(client);
            foreach (IInteractable client in mModalUnsubscribeQueue)
                mModalClients.Remove(client);
            mModalSubscribeQueue.Clear();
            mModalUnsubscribeQueue.Clear();
        }

        private void UpdateFocusMap(uint focusState, bool modal)
        {
            uint focusMap = 0;

            switch (focusState)
            {
                case FOCUS_STATE_NONE:
                    focusMap = 0;
                    break;
                case FOCUS_STATE_TITLE:
                    focusMap = HAS_FOCUS_TITLE;
                    break;
                case FOCUS_STATE_MENU:
                    focusMap = HAS_FOCUS_MENU;
                    break;
                case FOCUS_STATE_MENU_POTIONS:
                    focusMap = HAS_FOCUS_MENU_POTIONS;
                    break;
                case FOCUS_STATE_MENU_INFO:
                    focusMap = HAS_FOCUS_MENU_INFO;
                    break;
                case FOCUS_STATE_MENU_INFO_STATS:
                    focusMap = HAS_FOCUS_MENU_INFO_STATS;
                    break;
                case FOCUS_STATE_MENU_INFO_GAME_CONCEPTS:
                    focusMap = HAS_FOCUS_MENU_INFO_GAME_CONCEPTS;
                    break;
                case FOCUS_STATE_MENU_INFO_SPELLS_MUNITIONS:
                    focusMap = HAS_FOCUS_MENU_INFO_SPELLS_MUNITIONS;
                    break;
                case FOCUS_STATE_MENU_CREDITS:
                    focusMap = HAS_FOCUS_MENU_CREDITS;
                    break;
                case FOCUS_STATE_MENU_OBJECTIVES_LOG:
                    focusMap = HAS_FOCUS_MENU_OBJECTIVES_LOG;
                    break;
                case FOCUS_STATE_MENU_ACHIEVEMENTS:
                    focusMap = HAS_FOCUS_MENU_ACHIEVEMENTS;
                    break;
                case FOCUS_STATE_MENU_OPTIONS:
                    focusMap = HAS_FOCUS_MENU_OPTIONS;
                    break;
                case FOCUS_STATE_MENU_QUERY:
                    focusMap = HAS_FOCUS_MENU_QUERY;
                    break;
                case FOCUS_STATE_MENU_ALERT:
                    focusMap = HAS_FOCUS_MENU_ALERT;
                    break;
                case FOCUS_STATE_MENU_DISPLAY_ADJUST:
                    focusMap = HAS_FOCUS_MENU_DISPLAY_ADJUST;
                    break;
                case FOCUS_STATE_MENU_LEADERBOARD:
                    focusMap = HAS_FOCUS_MENU_LEADERBOARD;
                    break;
                case FOCUS_STATE_MENU_MASTERY:
                    focusMap = HAS_FOCUS_MENU_MASTERY;
                    break;
                case FOCUS_STATE_MENU_MODE_SELECT:
                    focusMap = HAS_FOCUS_MENU_MODE_SELECT;
                    break;
                case FOCUS_STATE_PF_PLAYFIELD:
                    focusMap = HAS_FOCUS_DECK | HAS_FOCUS_PAUSE_BUTTON;
                    break;
                case FOCUS_STATE_PF_VOODOO_WHEEL:
                    focusMap = HAS_FOCUS_VOODOO_WHEEL | HAS_FOCUS_PAUSE_BUTTON;
                    break;
                case FOCUS_STATE_PF_PAUSE:
                    focusMap = HAS_FOCUS_PAUSE_MENU;
                    break;
                case FOCUS_STATE_PF_OBJECTIVES_RANKUP:
                    focusMap = HAS_FOCUS_OBJECTIVES_RANKUP;
                    break;
                case FOCUS_STATE_PF_GAMEOVER:
                    focusMap = HAS_FOCUS_GAMEOVER;
                    break;
                case FOCUS_STATE_PF_SK_GAMEOVER:
                    focusMap = HAS_FOCUS_SK_GAMEOVER;
                    break;
                case FOCUS_STATE_PF_TUTORIAL:
                    focusMap = HAS_FOCUS_TUTORIAL | HAS_FOCUS_PAUSE_BUTTON;
                    break;
                case FOCUS_STATE_PF_HELP:
                    focusMap = HAS_FOCUS_HELP;
                    break;
                case FOCUS_STATE_SYS_EXIT:
                    focusMap = HAS_FOCUS_EXIT_MENU;
                    break;
                case FOCUS_STATE_SYS_STORAGE:
                    focusMap = HAS_FOCUS_STORAGE_DIALOG;
                    break;
                case FOCUS_STATE_SYS_DEBUG:
                    focusMap = HAS_FOCUS_DEBUG_CONSOLE;
                    break;
            }

            NotifyFocusChange(focusMap, modal);

            if (modal)
                mModalFocusMap = focusMap;
            else
                mFocusMap = focusMap;
        }

        public bool HasFocus(uint focus)
        {
            uint focusMap = mModalFocusMap != 0 ? mModalFocusMap : mFocusMap;
            return (focusMap & FOCUS_CAT_MASK) == (focus & FOCUS_CAT_MASK) && ((focusMap & HAS_FOCUS_MASK) & (focus & HAS_FOCUS_MASK)) != 0; //== (focus & HAS_FOCUS_MASK);
        }

        private bool HasFocus(uint focusMap, uint focus)
        {
            return (focusMap & FOCUS_CAT_MASK) == (focus & FOCUS_CAT_MASK) && ((focusMap & HAS_FOCUS_MASK) & (focus & HAS_FOCUS_MASK)) != 0; //== (focus & HAS_FOCUS_MASK);
        }

        private void NotifyFocusChange(uint focusMap, bool modal)
        {
            bool wasBusy = mBusyUpdatingClients;

            mBusyUpdatingClients = true;
            for (int i = 0; i < 2; ++i)
            {
                // Non-modal clients don't need to know about:
                    // 1. Non-modal focus states if the modal focus map is active.
                    // 2. Modal focus states if they they don't toggle the modal focus map activity (on/off).
                if (i == 0 && ((!modal && mModalFocusMap != 0) || (modal && focusMap != 0 && mModalFocusMap != 0)))
                    continue;

                // Modal clients don't need to know about non-modal focus changes.
                if (i == 1 && !modal)
                    continue;

                uint oldFocusMap = 0, newFocusMap = 0;

                if (i == 0)
                {
                    if (modal && focusMap == 0 && mModalFocusMap != 0)
                    {
                        // Special case: switching from modal back down to non-modal.
                        oldFocusMap = focusMap;
                        newFocusMap = mFocusMap;
                    }
                    else
                    {
                        oldFocusMap = mFocusMap;
                        newFocusMap = focusMap;
                    }
                }
                else
                {
                    oldFocusMap = mModalFocusMap;
                    newFocusMap = focusMap;
                }

                List<IInteractable> clients = (i == 0) ? mClients : mModalClients;
                foreach (IInteractable client in clients)
                {
                    bool hadFocus = HasFocus(oldFocusMap, client.InputFocus);
                    bool hasFocus = HasFocus(newFocusMap, client.InputFocus);

                    if (hadFocus && !hasFocus)
                        client.WillLoseFocus();
                    else if (!hadFocus && hasFocus)
                        client.DidGainFocus();
                }

                clients = (i == 0) ? mSubscribeQueue : mModalSubscribeQueue;
                foreach (IInteractable client in clients)
                {
                    bool hadFocus = HasFocus(oldFocusMap, client.InputFocus);
                    bool hasFocus = HasFocus(newFocusMap, client.InputFocus);

                    if (hadFocus && !hasFocus)
                        client.WillLoseFocus();
                    else if (!hadFocus && hasFocus)
                        client.DidGainFocus();
                }
            }

            if (!wasBusy)
                mBusyUpdatingClients = false;
        }

        public void PushFocusState(uint focusState, bool modal = false)
        {
            List<uint> focusStack = (modal) ? mModalFocusStack : mFocusStack;
            int stackCount = focusStack.Count;

            // Don't allow the same state to double-up on top of the stack. This would only happen when
            // clients are mismanaging states.
            if (stackCount == 0 || focusStack[stackCount - 1] != focusState)
            {
                focusStack.Add(focusState);
                UpdateFocusMap(focusState, modal);
            }
        }

        public void PopFocusState(uint focusState = FOCUS_STATE_NONE, bool modal = false)
        {
            List<uint> focusStack = (modal) ? mModalFocusStack : mFocusStack;
            int stackCount = focusStack.Count;

            if (stackCount > 1) // Don't pop base state
            {
                if (focusState == FOCUS_STATE_NONE || focusStack[stackCount - 1] == focusState)
                {
                    focusStack.RemoveAt(stackCount - 1);
                    UpdateFocusMap(focusStack[stackCount - 2], modal);
                }
            }
        }

        public void PopToFocusState(uint focusState, bool modal = false)
        {
            List<uint> focusStack = (modal) ? mModalFocusStack : mFocusStack;

            while (focusStack.Count > 1 && focusStack[focusStack.Count - 1] != focusState) // Don't pop base state
                PopFocusState(focusStack[focusStack.Count - 1]);

            // If focus state was not on the stack, then push it onto the stack.
            if (focusStack.Count == 1)
                PushFocusState(focusState);
            else
                UpdateFocusMap(focusState, modal);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                if (disposing)
                {
                    if (mClients != null)
                    {
                        mClients.Clear();
                        mClients = null;
                    }

                    if (mSubscribeQueue != null)
                    {
                        mSubscribeQueue.Clear();
                        mSubscribeQueue = null;
                    }

                    if (mUnsubscribeQueue != null)
                    {
                        mUnsubscribeQueue.Clear();
                        mUnsubscribeQueue = null;
                    }

                    if (mModalClients != null)
                    {
                        mModalClients.Clear();
                        mModalClients = null;
                    }

                    if (mModalSubscribeQueue != null)
                    {
                        mModalSubscribeQueue.Clear();
                        mModalSubscribeQueue = null;
                    }

                    if (mModalUnsubscribeQueue != null)
                    {
                        mModalUnsubscribeQueue.Clear();
                        mModalUnsubscribeQueue = null;
                    }

                    mFocusStack = null;
                    mModalFocusStack = null;
                }

                mIsDisposed = true;
            }
        }

        ~InputManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
