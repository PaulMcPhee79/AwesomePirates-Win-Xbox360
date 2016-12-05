using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    struct VibrationDescriptor
    {
        public VibrationDescriptor(float lfIntensity, double lfDuration, float hfIntensity, double hfDuration)
        {
            LowFreqIntensity = lfIntensity;
            LowFreqDuration = lfDuration;
            HighFreqIntensity = hfIntensity;
            HighFreqDuration = hfDuration;
        }

        public void Reset()
        {
            LowFreqIntensity = 0;
            LowFreqDuration = 0;
            HighFreqIntensity = 0;
            HighFreqDuration = 0;
        }

        public float LowFreqIntensity;
        public double LowFreqDuration;

        public float HighFreqIntensity;
        public double HighFreqDuration;
    }

    class ControlsManager : SPEventDispatcher
    {
        public const string CUST_EVENT_TYPE_DEFAULT_CONTROLLER_DISCONNECTED = "defaultControllerDisconnectedEvent";
        public const string CUST_EVENT_TYPE_CONTROLLER_DID_ENGAGE = "controllerDidEngageEvent";
        public const string CUST_EVENT_TYPE_EXIT_BUTTON_PRESSED = "exitButtonPressedEvent";

        private const int kNumControllers = 4;

        private static ControlsManager instance = null;

        private ControlsManager()
        {


        }

        private void SetupControlsManager()
        {
            for (int i = 0; i < kNumControllers; ++i)
            {
                mPlayerIndexes[i] = (PlayerIndex)i;
                mThumbStickDidActivateMap[i] = mThumbStickDidDeactivateMap[i] = mThumbStickActivatedMap[i] = 0;

                mPrevGamePadState[i] = mGamePadState[i] = GamePad.GetState(mPlayerIndexes[i]);
                SetGamePadCapabilitiesAtIndex(i, GamePad.GetCapabilities(mPlayerIndexes[i]));
            }

            for (int i = 0; i < kNumControllers; ++i)
            {
                mPrevGamePadState[i] = mGamePadState[i];
                mGamePadState[i] = GamePad.GetState((PlayerIndex)i);
            }
        }

        #region Fields
        private int mMainPlayerIndex = -1;
        private PlayerIndex? mDefaultPlayerIndex = null;
        private PlayerIndex? mPrevQueryPlayerIndex = null;
        private PlayerIndex[] mPlayerIndexes = new PlayerIndex[kNumControllers];
        private GamePadState[] mGamePadState = new GamePadState[kNumControllers];
        private GamePadState[] mPrevGamePadState = new GamePadState[kNumControllers];
        private bool[] mEngaged = new bool[kNumControllers];
        private uint[] mThumbStickDidActivateMap = new uint[kNumControllers];
        private uint[] mThumbStickActivatedMap = new uint[kNumControllers];
        private uint[] mThumbStickDidDeactivateMap = new uint[kNumControllers];
        private GamePadCapabilities[] mGamePadCapabilities = new GamePadCapabilities[kNumControllers];
        private bool[] mSupportedControllers = new bool[kNumControllers];
        private VibrationDescriptor[] mVibrations = new VibrationDescriptor[kNumControllers];
        private KeyboardState mKeyboardState;
        private KeyboardState mPrevKeyboardState;
        private MouseState mMouseState;
        private MouseState mPrevMouseState;
        #endregion

        #region Properties
        public static ControlsManager CM
        {
            get
            {
                if (instance == null)
                {
                    instance = new ControlsManager();
                    instance.SetupControlsManager();
                }
                return instance;
            }
        }
        public static int NumControllers { get { return kNumControllers; } }
        public int NumConnectedControllers
        {
            get
            {
                if (mGamePadState == null)
                    return 0;

                int num = 0;

                for (int i = 0; i < kNumControllers; ++i)
                {
                    if (mGamePadState[i].IsConnected)
                        ++num;
                }

                return num;
            }
        }
        protected bool HasConnectedController
        {
            get
            {
                bool hasController = false;

                for (int i = 0; i < kNumControllers; ++i)
                    hasController |= mGamePadState[i].IsConnected;

                return hasController;
            }
        }
        public int PlayerIndexMap { get { return (mMainPlayerIndex != -1) ? 1 << mMainPlayerIndex : 0xf; } }
        public PlayerIndex? DefaultPlayerIndex { get { return mDefaultPlayerIndex; } }
        public PlayerIndex? PrevQueryPlayerIndex { get { return mPrevQueryPlayerIndex; } set { mPrevQueryPlayerIndex = value; } }
        public PlayerIndex? MainPlayerIndex
        {
            get { return (mMainPlayerIndex == -1) ? (PlayerIndex?)null : mPlayerIndexes[mMainPlayerIndex]; }
            set
            {
                if (value.HasValue)
                {
                    for (int i = 0; i < mPlayerIndexes.Length; ++i)
                    {
                        if (mPlayerIndexes[i] == value.Value)
                        {
                            mMainPlayerIndex = i;
                            break;
                        }
                    }
                }
                else
                    mMainPlayerIndex = -1;
            }
        }
        public KeyboardState KeyboardState { get { return mKeyboardState; } }
        public KeyboardState PrevKeyboardState { get { return mPrevKeyboardState; } }
        public MouseState MouseState { get { return mMouseState; } }
        public MouseState PrevMouseState { get { return mPrevMouseState; } }
        #endregion

        #region Methods
        protected void SetGamePadCapabilitiesAtIndex(int index, GamePadCapabilities gpCap)
        {
            mSupportedControllers[index] = gpCap.GamePadType == GamePadType.GamePad || gpCap.GamePadType == GamePadType.ArcadeStick;
            mGamePadCapabilities[index] = gpCap;
        }

        protected bool SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor)
        {
            if (mGamePadCapabilities[(int)playerIndex].HasLeftVibrationMotor && mGamePadCapabilities[(int)playerIndex].HasRightVibrationMotor)
                return GamePad.SetVibration(playerIndex, leftMotor, rightMotor);
            else
                return true;
        }

        protected int ResolveIndex(PlayerIndex? index)
        {
            return (index.HasValue) ? (int)index : mMainPlayerIndex;
        }

        protected bool DidDefaultControllerDisconnect()
        {
            if (!mDefaultPlayerIndex.HasValue)
                return false;
            else
            {
                int i = (int)mDefaultPlayerIndex;
                return (!mGamePadState[i].IsConnected && mPrevGamePadState[i].IsConnected);
            }
        }

        protected bool DidDefaultControllerReconnect()
        {
            if (!mDefaultPlayerIndex.HasValue)
                return false;
            else
            {
                int i = (int)mDefaultPlayerIndex;
                return (mGamePadState[i].IsConnected && !mPrevGamePadState[i].IsConnected);
            }
        }

#if false
        protected void FindNewActiveController()
        {
            int i;

            if (mDefaultPlayerIndex.HasValue)
            {
                // Try Default Player Index
                i = (int)mDefaultPlayerIndex;

                if (mGamePadState[i].IsConnected)
                {
                    MainPlayerIndex = (PlayerIndex)i;
                    return;
                }
            }

            // Try SignedInGamer.GameDefaults controller
            /*
            foreach (SignedInGamer gamer in SignedInGamer.SignedInGamers) 
            { 
                if (gamer.PlayerIndex == MainPlayerIndex)
                {
                    gamer.GameDefaults.
                }
            } 
            */

            // Incrementally try controller indexes that are not in use
            foreach (PlayerIndex playerIndex in mPlayerIndexes)
            {
                i = (int)playerIndex;

                if (mGamePadState[i].IsConnected)
                {
                    MainPlayerIndex = playerIndex;
                    break;
                }
            }

            // Give up until new controller connection is detected.
        }
#endif

        public bool HasControllerEngaged(PlayerIndex playerIndex)
        {
            return mEngaged[(int)playerIndex];
        }

        public void ControllerDidEngage(PlayerIndex playerIndex)
        {
            bool wasEngaged = HasControllerEngaged(playerIndex);
            mEngaged[(int)playerIndex] = true;

            if (!wasEngaged)
                DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_CONTROLLER_DID_ENGAGE, playerIndex));
        }

        public void SetDefaultPlayerIndex(PlayerIndex? index)
        {
            mDefaultPlayerIndex = index;
            MainPlayerIndex = index;
        }

        public PlayerIndex IndexForPlayer(int player)
        {
            int index = player - 1;
#if DEBUG
            if (index < 0 || index >= mPlayerIndexes.Length)
                throw new ArgumentException("Player index out of range. Must be between 1-4");
#endif
            return mPlayerIndexes[index];
        }

        public GamePadState GamePadStateForPlayer(PlayerIndex? index = null)
        {
            int resolvedIndex = ResolveIndex(index);

            if (resolvedIndex != -1)
                return mGamePadState[resolvedIndex];
            else
                return new GamePadState();
        }

        public GamePadState PrevGamePadStateForPlayer(PlayerIndex? index = null)
        {
            int resolvedIndex = ResolveIndex(index);

            if (resolvedIndex != -1)
                return mPrevGamePadState[resolvedIndex];
            else
                return new GamePadState();
        }

        public bool IsButtonDown(Buttons button, PlayerIndex? index = null)
        {
            int i = ResolveIndex(index);
            bool isDown = false;

            if (i != -1)
            {
                if (!mSupportedControllers[i])
                    isDown = false;
                else
                {
                    isDown = (mGamePadState[i].IsConnected && mGamePadState[i].IsButtonDown(button));
                    PrevQueryPlayerIndex = (isDown) ? (PlayerIndex)i : (PlayerIndex?)null;
                }
            }
            else
            {
                // Search all controllers
                PrevQueryPlayerIndex = (PlayerIndex?)null;

                for (i = 0; i < kNumControllers; ++i)
                {
                    isDown = (mSupportedControllers[i] && mGamePadState[i].IsConnected && mGamePadState[i].IsButtonDown(button));

                    if (isDown)
                    {
                        PrevQueryPlayerIndex = (PlayerIndex)i;
                        break;
                    }
                }
            }

            return isDown;
        }

        public bool DidButtonDepress(Buttons button, PlayerIndex? index = null)
        {
            int i = ResolveIndex(index);
            bool didDepress = false;

            if (i != -1)
            {
                if (!mSupportedControllers[i])
                    didDepress = false;
                else
                {
                    didDepress = (mGamePadState[i].IsConnected && mGamePadState[i].IsButtonDown(button) && mPrevGamePadState[i].IsButtonUp(button));
                    PrevQueryPlayerIndex = (didDepress) ? (PlayerIndex)i : (PlayerIndex?)null;
                }
            }
            else
            {
                // Search all controllers
                PrevQueryPlayerIndex = (PlayerIndex?)null;

                for (i = 0; i < kNumControllers; ++i)
                {
                    didDepress = (mSupportedControllers[i] && mGamePadState[i].IsConnected && mGamePadState[i].IsButtonDown(button) && mPrevGamePadState[i].IsButtonUp(button));

                    if (didDepress)
                    {
                        PrevQueryPlayerIndex = (PlayerIndex)i;
                        break;
                    }
                }
            }

            return didDepress;
        }

        public bool DidButtonsDepress(Buttons[] buttons, PlayerIndex? index = null)
        {
            if (buttons == null)
                return false;

            int i = ResolveIndex(index);
            bool didDepress = false;

            foreach (Buttons button in buttons)
            {
                didDepress = DidButtonDepress(button, index);

                if (didDepress)
                    break;
            }

            PrevQueryPlayerIndex = (PlayerIndex?)null;

            return didDepress;
        }

        public bool DidButtonRelease(Buttons button, PlayerIndex? index = null)
        {
            int i = ResolveIndex(index);
            bool didRelease = false;

            if (i != -1)
            {
                if (!mSupportedControllers[i])
                    didRelease = false;
                else
                {
                    didRelease = (mGamePadState[i].IsConnected && mGamePadState[i].IsButtonUp(button) && mPrevGamePadState[i].IsButtonDown(button));
                    PrevQueryPlayerIndex = (didRelease) ? (PlayerIndex)i : (PlayerIndex?)null;
                }
            }
            else
            {
                // Search all controllers
                PrevQueryPlayerIndex = (PlayerIndex?)null;

                for (i = 0; i < kNumControllers; ++i)
                {
                    didRelease = (mSupportedControllers[i] && mGamePadState[i].IsConnected && mGamePadState[i].IsButtonUp(button) && mPrevGamePadState[i].IsButtonDown(button));

                    if (didRelease)
                    {
                        PrevQueryPlayerIndex = (PlayerIndex)i;
                        break;
                    }
                }
            }

            return didRelease;
        }

        public bool DidButtonsRelease(Buttons[] buttons, PlayerIndex? index = null)
        {
            if (buttons == null)
                return false;

            int i = ResolveIndex(index);
            bool didRelease = false;

            foreach (Buttons button in buttons)
            {
                didRelease = DidButtonRelease(button, index);

                if (didRelease)
                    break;
            }

            PrevQueryPlayerIndex = (PlayerIndex?)null;

            return didRelease;
        }

        public bool IsThumbstickActivated(InputManager.ThumbStickDir dir, PlayerIndex? index = null)
        {
            int i = ResolveIndex(index);
            bool isActivated = false;

            if (i != -1)
                isActivated = mSupportedControllers[i] && (mThumbStickActivatedMap[i] & (uint)dir) == (uint)dir;
            else
            {
                // Search all controllers
                for (i = 0; i < kNumControllers; ++i)
                {
                    isActivated = mSupportedControllers[i] && (mThumbStickActivatedMap[i] & (uint)dir) == (uint)dir;

                    if (isActivated)
                        break;
                }
            }

            return isActivated;
        }

        public bool DidThumbstickActivate(InputManager.ThumbStickDir dir, PlayerIndex? index = null)
        {
            int i = ResolveIndex(index);
            bool didActivate = false;

            if (i != -1)
            {
                if (!mSupportedControllers[i])
                    didActivate = false;
                else
                {
                    didActivate = (mThumbStickDidActivateMap[i] & (uint)dir) == (uint)dir;
                    PrevQueryPlayerIndex = (didActivate) ? (PlayerIndex)i : (PlayerIndex?)null;
                }
            }
            else
            {
                // Search all controllers
                PrevQueryPlayerIndex = (PlayerIndex?)null;

                for (i = 0; i < kNumControllers; ++i)
                {
                    didActivate = mSupportedControllers[i] && (mThumbStickDidActivateMap[i] & (uint)dir) == (uint)dir;

                    if (didActivate)
                    {
                        PrevQueryPlayerIndex = (PlayerIndex)i;
                        break;
                    }
                }
            }

            return didActivate;
        }

        public bool DidThumbstickDeactivate(InputManager.ThumbStickDir dir, PlayerIndex? index = null)
        {
            int i = ResolveIndex(index);
            bool didDeactivate = false;

            if (i != -1)
            {
                if (!mSupportedControllers[i])
                    didDeactivate = false;
                else
                {
                    didDeactivate = (mThumbStickDidDeactivateMap[i] & (uint)dir) == (uint)dir;
                    PrevQueryPlayerIndex = (didDeactivate) ? (PlayerIndex)i : (PlayerIndex?)null;
                }
            }
            else
            {
                // Search all controllers
                PrevQueryPlayerIndex = (PlayerIndex?)null;

                for (i = 0; i < kNumControllers; ++i)
                {
                    didDeactivate = mSupportedControllers[i] && (mThumbStickDidDeactivateMap[i] & (uint)dir) == (uint)dir;

                    if (didDeactivate)
                    {
                        PrevQueryPlayerIndex = (PlayerIndex)i;
                        break;
                    }
                }
            }

            return didDeactivate;
        }

        private void UpdateThumbstickMaps()
        {
            for (int i = 0; i < kNumControllers; ++i)
            {
                mThumbStickDidActivateMap[i] = mThumbStickDidDeactivateMap[i] = 0;

                if (!mGamePadState[i].IsConnected || !mSupportedControllers[i])
                {
                    mThumbStickActivatedMap[i] = 0;
                    continue;
                }

                if (mGamePadState[i].PacketNumber == mPrevGamePadState[i].PacketNumber)
                    continue;

                bool didActivate = false, didDeactivate = false;
                Vector2 pos = mGamePadState[i].ThumbSticks.Left;
                uint iter = (uint)InputManager.ThumbStickDir.TLLeft;

                while (iter <= (uint)InputManager.ThumbStickDir.TRUp)
                {
                    didActivate = didDeactivate = false;

                    if ((mThumbStickActivatedMap[i] & iter) != iter)
                    {
                        switch ((InputManager.ThumbStickDir)iter)
                        {
                            case InputManager.ThumbStickDir.TLLeft: didActivate = pos.X < -InputManager.kThumbStickActivationThreshold; break;
                            case InputManager.ThumbStickDir.TLRight: didActivate = pos.X > InputManager.kThumbStickActivationThreshold; break;
                            case InputManager.ThumbStickDir.TLDown: didActivate = pos.Y < -InputManager.kThumbStickActivationThreshold; break;
                            case InputManager.ThumbStickDir.TLUp: didActivate = pos.Y > InputManager.kThumbStickActivationThreshold; break;
                            case InputManager.ThumbStickDir.TRLeft: didActivate = pos.X < -InputManager.kThumbStickActivationThreshold; break;
                            case InputManager.ThumbStickDir.TRRight: didActivate = pos.X > InputManager.kThumbStickActivationThreshold; break;
                            case InputManager.ThumbStickDir.TRDown: didActivate = pos.Y < -InputManager.kThumbStickActivationThreshold; break;
                            case InputManager.ThumbStickDir.TRUp: didActivate = pos.Y > InputManager.kThumbStickActivationThreshold; break;
                        }
                    }

                    if ((mThumbStickActivatedMap[i] & iter) == iter)
                    {
                        switch ((InputManager.ThumbStickDir)iter)
                        {
                            case InputManager.ThumbStickDir.TLLeft: didDeactivate = pos.X > -InputManager.kThumbStickDeactivationThreshold; break;
                            case InputManager.ThumbStickDir.TLRight: didDeactivate = pos.X < InputManager.kThumbStickDeactivationThreshold; break;
                            case InputManager.ThumbStickDir.TLDown: didDeactivate = pos.Y > -InputManager.kThumbStickDeactivationThreshold; break;
                            case InputManager.ThumbStickDir.TLUp: didDeactivate = pos.Y < InputManager.kThumbStickDeactivationThreshold; break;
                            case InputManager.ThumbStickDir.TRLeft: didDeactivate = pos.X > -InputManager.kThumbStickDeactivationThreshold; break;
                            case InputManager.ThumbStickDir.TRRight: didDeactivate = pos.X < InputManager.kThumbStickDeactivationThreshold; break;
                            case InputManager.ThumbStickDir.TRDown: didDeactivate = pos.Y > -InputManager.kThumbStickDeactivationThreshold; break;
                            case InputManager.ThumbStickDir.TRUp: didDeactivate = pos.Y < InputManager.kThumbStickDeactivationThreshold; break;
                        }
                    }

                    if (didActivate)
                        mThumbStickDidActivateMap[i] |= iter;
                    if (didDeactivate)
                        mThumbStickDidDeactivateMap[i] |= iter;
                    iter <<= 1;
                }

                mThumbStickActivatedMap[i] |= mThumbStickDidActivateMap[i];
                mThumbStickActivatedMap[i] &= ~mThumbStickDidDeactivateMap[i];
            }
        }

        public void StopAllGamePadVibrations()
        {
            if (mVibrations == null)
                return;

            int vbIter = 0;
            try
            {
                int limit = Math.Min(mVibrations.Length, kNumControllers);
                for (vbIter = 0; vbIter < limit; ++vbIter)
                {
                    if (mVibrations[vbIter].LowFreqDuration <= 0 && mVibrations[vbIter].HighFreqDuration <= 0)
                        continue;
                    if (SetVibration((PlayerIndex)vbIter, 0, 0))
                        mVibrations[vbIter].Reset();
                }
            }
            catch (Exception e)
            {
                // Make sure we don't leave any motors on indefinitely.
                if (mVibrations.Length > vbIter)
                {
                    mVibrations[vbIter].LowFreqDuration = 0.01; // Try again next frame
                    mVibrations[vbIter].HighFreqDuration = 0.01; // Try again next frame
                }

                Debug.WriteLine("ControlsManager::VibrateGamePad failed: " + e.Message);
            }
        }

        public void VibrateGamePad(PlayerIndex playerIndex, VibrationDescriptor descriptor)
        {
            VibrateGamePad(playerIndex, ref descriptor);
        }

        public void VibrateGamePad(PlayerIndex playerIndex, ref VibrationDescriptor descriptor)
        {
            try
            {
                // Currently we just overwrite previous settings for non-zero intensities.
                VibrationDescriptor vd = mVibrations[(int)playerIndex];

                if (descriptor.LowFreqIntensity != 0)
                {
                    vd.LowFreqIntensity = descriptor.LowFreqIntensity;
                    vd.LowFreqDuration = descriptor.LowFreqDuration;
                }

                if (descriptor.HighFreqIntensity != 0)
                {
                    vd.HighFreqIntensity = descriptor.HighFreqIntensity;
                    vd.HighFreqDuration = descriptor.HighFreqDuration;
                }

                if (SetVibration(playerIndex, vd.LowFreqIntensity, vd.HighFreqIntensity))
                    mVibrations[(int)playerIndex] = vd;
            }
            catch (InvalidOperationException e)
            {
                Debug.WriteLine("ControlsManager::VibrateGamePad failed: " + e.Message);
            }
        }

        public void Update(GameTime gameTime)
        {
            for (int i = 0; i < kNumControllers; ++i)
            {
                mPrevGamePadState[i] = mGamePadState[i];
                mGamePadState[i] = GamePad.GetState(mPlayerIndexes[i]);

                if (!mPrevGamePadState[i].IsConnected && mGamePadState[i].IsConnected)
                    SetGamePadCapabilitiesAtIndex(i, GamePad.GetCapabilities(mPlayerIndexes[i]));
            }

            if (DidButtonDepress(Buttons.Back))
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_EXIT_BUTTON_PRESSED));

            UpdateThumbstickMaps();

#if WINDOWS
            mPrevKeyboardState = mKeyboardState;
            mKeyboardState = Keyboard.GetState();

            mPrevMouseState = mMouseState;
            mMouseState = Mouse.GetState();
#endif
            if (DidDefaultControllerDisconnect())
                DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_DEFAULT_CONTROLLER_DISCONNECTED));
            if (DidDefaultControllerReconnect())
                MainPlayerIndex = mDefaultPlayerIndex;

            int vbIter = 0;
            double time = gameTime.ElapsedGameTime.TotalSeconds;

            try
            {
                for (vbIter = 0; vbIter < kNumControllers; ++vbIter)
                {
                    if (mVibrations[vbIter].LowFreqDuration > 0)
                    {
                        mVibrations[vbIter].LowFreqDuration -= time;
                        if (mVibrations[vbIter].LowFreqDuration <= 0)
                        {
                            mVibrations[vbIter].LowFreqIntensity = 0;
                            if (!SetVibration((PlayerIndex)vbIter, 0, mVibrations[vbIter].HighFreqIntensity))
                                mVibrations[vbIter].LowFreqDuration = 0.01; // Try again next frame
                        }
                    }

                    if (mVibrations[vbIter].HighFreqDuration > 0)
                    {
                        mVibrations[vbIter].HighFreqDuration -= time;
                        if (mVibrations[vbIter].HighFreqDuration <= 0)
                        {
                            mVibrations[vbIter].HighFreqIntensity = 0;
                            if (!SetVibration((PlayerIndex)vbIter, mVibrations[vbIter].LowFreqIntensity, 0))
                                mVibrations[vbIter].HighFreqDuration = 0.01; // Try again next frame
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Make sure we don't leave any motors on indefinitely.
                if (mVibrations.Length > vbIter)
                {
                    mVibrations[vbIter].LowFreqDuration = 0.01; // Try again next frame
                    mVibrations[vbIter].HighFreqDuration = 0.01; // Try again next frame
                }

                Debug.WriteLine("ControlsManager::VibrateGamePad failed: " + e.Message);
            }
        }
        #endregion
    }
}
