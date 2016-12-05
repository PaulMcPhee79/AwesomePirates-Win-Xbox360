using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class StorageDialog : Prop
    {
        public enum StorageDialogType
        {
            Load,
            Save
        }

        private enum StorageDialogMode
        {
            Idle = 0,
            InProgress,
            Failed,
            SelectStorage,
            Cancelled,
            Completed
        }

        public const string CUST_EVENT_TYPE_STORAGE_DIALOG_COMPLETED = "saveProgressCompletedEvent";
        public const string CUST_EVENT_TYPE_STORAGE_DIALOG_CANCELLED = "saveProgressCancelledEvent";

        private const string kLoadInProgressText = "Loading progress...";
        private const string kLoadFailedTitle = "Load Failed";
        private const string kLoadFailedText = "Do you wish to select a new storage device and try again?";
        private const string kSaveInProgressText = "Saving progress...";
        private const string kSaveFailedTitle = "Save Failed";
        private const string kSaveFailedText = "Do you wish to select a new storage device and try again?";
        private const string kStorageSelectionText = "Awaiting storage device selection...";

        public StorageDialog(int category, StorageDialogType dialogType, PlayerIndex playerIndex)
            : base(category)
        {
            mAdvanceable = true;
            mType = dialogType;
            mPlayerIndex = playerIndex;
            mInProgressTimer = 0;
            mButtons = new MenuButton[2];
            mButtonsProxy = new ButtonsProxy(InputFocus, Globals.kNavHorizontal);
            mMode = StorageDialogMode.Idle;
            SetupProp();

            FileManager fm = FileManager.FM;
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_SELECTED, (PlayerIndexEventHandler)OnLocalSaveDeviceSelected);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_CANCELLED, (PlayerIndexEventHandler)OnLocalSaveDeviceCancelled);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, (PlayerIndexEventHandler)OnLocalLoadCompleted);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, (PlayerIndexEventHandler)OnLocalLoadFailed);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_COMPLETED, (PlayerIndexEventHandler)OnLocalSaveCompleted);
            fm.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_FAILED, (PlayerIndexEventHandler)OnLocalSaveFailed);

            GameController gc = GameController.GC;
            gc.ProfileManager.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, (PlayerIndexEventHandler)OnLocalLoadCompleted);
            gc.ProfileManager.AddEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, (PlayerIndexEventHandler)OnLocalLoadFailed);
            gc.BypassingFileManagerEvents = true;

            Mode = StorageDialogMode.Failed;
        }

        #region Fields
        private readonly object s_lock = new object();
        private volatile bool mModeIsQueued = false;
        private StorageDialogMode mQueuedMode = StorageDialogMode.Idle;

        private StorageDialogType mType;
        private StorageDialogMode mMode;
        private PlayerIndex mPlayerIndex;
        private int mLoadCountdown = 0;
        private double mInProgressTimer;

        private SPSprite mScrollSprite;
        private SPSprite mCanvasSprite;
        private SPSprite mCanvasScaler;

        private SPTextField mInProgressText;
        private SPTextField mFailedText;
        private SPTextField mFailedTitle;

        private SPSprite mInProgressWheel;
        private SPSprite mInProgressSprite;
        private SPSprite mFailedButtonsSprite;
        private SPSprite mFailedSprite;

        private MenuButton[] mButtons;
        private ButtonsProxy mButtonsProxy;

        private SPImage mGamerPic;
        private GuideProp mGuideProp;
        #endregion

        #region Properties
        private uint InputFocus { get { return InputManager.HAS_FOCUS_STORAGE_DIALOG; } }
        private StorageDialogMode Mode
        {
            get { return mMode; }
            set
            {
                StorageDialogMode prevMode = mMode;
                mMode = value;
                ModeDidChange(prevMode, value);
            }
        }
        public StorageDialogType DialogType { get { return mType; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCanvasScaler != null)
                return;

            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);

            mCanvasScaler = new SPSprite();
            mCanvasScaler.X = mScene.ViewWidth / 2 - X;
            mCanvasScaler.Y = mScene.ViewHeight / 2 - Y;
            AddChild(mCanvasScaler);

            mCanvasSprite = new SPSprite();
            mCanvasScaler.AddChild(mCanvasSprite);

            // Background scroll
            SPTexture scrollTexture = GuiHelper.CachedScrollTextureByName("scroll-quarter-small", mScene);
            SPImage scrollImage = new SPImage(scrollTexture);
            mScrollSprite = new SPSprite();
            mScrollSprite.AddChild(scrollImage);

            mScrollSprite.ScaleX = mScrollSprite.ScaleY = 520.0f / mScrollSprite.Width;
            mCanvasSprite.AddChild(mScrollSprite);

            // InProgress
            mInProgressSprite = new SPSprite();
            mInProgressSprite.X = ResManager.RESX(0);
            mInProgressSprite.Y = ResManager.RESY(50);
            mCanvasSprite.AddChild(mInProgressSprite);

                // Progress Text
            mInProgressText = new SPTextField(400, 48, kSaveInProgressText, mScene.FontKey, 32);
            mInProgressText.X = 282;
            mInProgressText.Y = 166;
            mInProgressText.HAlign = SPTextField.SPHAlign.Center;
            mInProgressText.VAlign = SPTextField.SPVAlign.Top;
            mInProgressText.Color = Color.Black;
            mInProgressSprite.AddChild(mInProgressText);

                // Progress Wheel
            SPImage wheelImage = new SPImage(mScene.TextureByName("progress-wheel"));
            wheelImage.X = -wheelImage.Width / 2;
            wheelImage.Y = -wheelImage.Height / 2;

            mInProgressWheel = new SPSprite();
            mInProgressWheel.X = 480;
            mInProgressWheel.Y = 292;
            mInProgressWheel.AddChild(wheelImage);
            mInProgressSprite.AddChild(mInProgressWheel);

            // Failed
            mFailedSprite = new SPSprite();
            mFailedSprite.X = ResManager.RESX(0);
            mFailedSprite.Y = ResManager.RESY(50);
            mCanvasSprite.AddChild(mFailedSprite);

                // Failed Title
            mFailedTitle = new SPTextField(300, 56, kSaveFailedTitle, mScene.FontKey, 44);
            mFailedTitle.X = 332;
            mFailedTitle.Y = 130;
            mFailedTitle.HAlign = SPTextField.SPHAlign.Center;
            mFailedTitle.VAlign = SPTextField.SPVAlign.Top;
            mFailedTitle.Color = SPUtils.ColorFromColor(0xba0000);
            mFailedSprite.AddChild(mFailedTitle);

                // Failed Text
            mFailedText = new SPTextField(428, 96, kSaveFailedText, mScene.FontKey, 32);
            mFailedText.X = 266;
            mFailedText.Y = 212;
            mFailedText.HAlign = SPTextField.SPHAlign.Center;
            mFailedText.VAlign = SPTextField.SPVAlign.Top;
            mFailedText.Color = Color.Black;
            mFailedSprite.AddChild(mFailedText);

                // Failed Buttons
            mFailedButtonsSprite = new SPSprite();
            mFailedSprite.AddChild(mFailedButtonsSprite);

            MenuButton yesButton = new MenuButton(null, mScene.TextureByName("yes-button"));
            yesButton.X = 342;
            yesButton.Y = 336;
            yesButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnYesPressed);
            mFailedButtonsSprite.AddChild(yesButton);

            MenuButton noButton = new MenuButton(null, mScene.TextureByName("no-button"));
            noButton.X = 506;
            noButton.Y = 336;
            noButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnNoPressed);
            mFailedButtonsSprite.AddChild(noButton);

            mButtons[0] = yesButton;
            mButtons[1] = noButton;
            mButtonsProxy.AddButton(yesButton);
            mButtonsProxy.AddButton(noButton);
            mScene.SubscribeToInputUpdates(mButtonsProxy, true);

            // Focus Guide
            mGuideProp = new GuideProp(Category);
            mGuideProp.X = ResManager.RESX(480);
            mGuideProp.Y = ResManager.RESY(508);
            mGuideProp.PlayerIndexMap = (1 << (int)mPlayerIndex);
            mCanvasSprite.AddChild(mGuideProp);

            mScrollSprite.X = (mScene.ViewWidth - mScrollSprite.Width) / 2;
            mScrollSprite.Y = (mScene.ViewHeight - mScrollSprite.Height) / 2;
            mCanvasSprite.X = -mCanvasScaler.X;
            mCanvasSprite.Y = -mCanvasScaler.Y;
            mCanvasScaler.ScaleX = mCanvasScaler.ScaleY = mScene.ScaleForUIView(mScrollSprite, 1f, 0.6f);

            // Gamer Picture
            SPTexture gamerPicTexture = GameController.GC.ProfileManager.PureGamerPictureForPlayer(mPlayerIndex);
            if (gamerPicTexture != null)
            {
                mGamerPic = new SPImage(gamerPicTexture);
                mGamerPic.X = ResManager.RESX(240);
                mGamerPic.Y = ResManager.RESY(148);
                mCanvasSprite.AddChild(mGamerPic);
            }

            ResManager.RESM.PopOffset();
        }

        public void OnGamerPicsRefreshed(SPEvent ev)
        {
            if (mGamerPic == null)
                return;

            SPTexture texture = GameController.GC.ProfileManager.PureGamerPictureForPlayer(mPlayerIndex);
            if (texture != null)
                mGamerPic.Texture = texture;
            else
            {
                mGamerPic.RemoveFromParent();
                mGamerPic.Dispose();
                mGamerPic = null;
            }
        }

        private void ModeDidChange(StorageDialogMode prevMode, StorageDialogMode mode)
        {
            if (prevMode == mode)
                return;

            switch (prevMode)
            {
                case StorageDialogMode.Idle:
                    mInProgressSprite.Visible = false;
                    mFailedSprite.Visible = false;
                    mFailedTitle.Visible = false;
                    mFailedButtonsSprite.Visible = false;
                    mFailedSprite.Visible = false;
                    break;
                case StorageDialogMode.InProgress:
                    mInProgressSprite.Visible = false;
                    break;
                case StorageDialogMode.Failed:
                    mFailedSprite.Visible = false;
                    mFailedTitle.Visible = false;
                    mFailedButtonsSprite.Visible = false;
                    break;
                case StorageDialogMode.SelectStorage:
                    mFailedSprite.Visible = false;
                    break;
                case StorageDialogMode.Cancelled:
                    break;
                case StorageDialogMode.Completed:
                    break;
            }

            switch (mode)
            {
                case StorageDialogMode.Idle:
                    break;
                case StorageDialogMode.InProgress:
                    mInProgressSprite.Visible = true;

                    if (mType == StorageDialogType.Load)
                    {
                        mInProgressText.Text = kLoadInProgressText;
                        mLoadCountdown = 2; // Will load after a frame draw runs to update the display (typically a synchronous load).
                    }
                    else
                    {
                        mInProgressText.Text = kSaveInProgressText;
                        FileManager.FM.ConfirmRepeatedQueuedSaveRequest(true);
                    }
                    break;
                case StorageDialogMode.Failed:
                    mFailedSprite.Visible = true;
                    mFailedTitle.Visible = true;
                    mFailedButtonsSprite.Visible = true;
                    mButtonsProxy.ResetNav();

                    FileManager.FM.DestroyPlayerSaveDevice(mPlayerIndex);
                    FileManager.FM.AddSaveDeviceForPlayer(mPlayerIndex);

                    if (mType == StorageDialogType.Load)
                    {
                        mFailedTitle.Text = kLoadFailedTitle;
                        mFailedText.Text = kLoadFailedText;
                    }
                    else
                    {
                        mFailedTitle.Text = kSaveFailedTitle;
                        mFailedText.Text = kSaveFailedText;
                        FileManager.FM.RequestRepeatedQueuedSave();
                    }
                    break;
                case StorageDialogMode.SelectStorage:
                    mFailedSprite.Visible = true;
                    mFailedText.Text = kStorageSelectionText;
                    FileManager.FM.PromptForDeviceLocal(mPlayerIndex);
                    break;
                case StorageDialogMode.Cancelled:
                    mInProgressSprite.Visible = false;
                    mFailedSprite.Visible = false;
                    DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_STORAGE_DIALOG_CANCELLED, mPlayerIndex));
                    break;
                case StorageDialogMode.Completed:
                    mInProgressSprite.Visible = false;
                    mFailedSprite.Visible = false;
                    DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_STORAGE_DIALOG_COMPLETED, mPlayerIndex));
                    break;
            }
        }

        // Async callback
        private void OnLocalSaveDeviceSelected(PlayerIndexEvent ev)
        {
            if (ev == null || ev.PlayerIndex != mPlayerIndex)
                return;

            lock (s_lock)
            {
                mQueuedMode = StorageDialogMode.InProgress;
                mModeIsQueued = true;
            }
        }

        // Async callback
        private void OnLocalSaveDeviceCancelled(PlayerIndexEvent ev)
        {
            if (ev == null || ev.PlayerIndex != mPlayerIndex)
                return;

            lock (s_lock)
            {
                mQueuedMode = StorageDialogMode.Cancelled;
                mModeIsQueued = true;
            }
        }

        // Potentially an Async callback
        private void OnLocalLoadCompleted(PlayerIndexEvent ev)
        {
            if (ev == null || ev.PlayerIndex != mPlayerIndex)
                return;

            lock (s_lock)
            {
                mQueuedMode = StorageDialogMode.Completed;
                mModeIsQueued = true;
            }
        }

        // Potentially an Async callback
        private void OnLocalLoadFailed(PlayerIndexEvent ev)
        {
            if (ev == null || ev.PlayerIndex != mPlayerIndex)
                return;

            lock (s_lock)
            {
                mQueuedMode = StorageDialogMode.Failed;
                mModeIsQueued = true;
            }
        }

        // Async callback
        private void OnLocalSaveCompleted(PlayerIndexEvent ev)
        {
            if (ev == null || ev.PlayerIndex != mPlayerIndex)
                return;

            lock (s_lock)
            {
                mQueuedMode = StorageDialogMode.Completed;
                mModeIsQueued = true;
            }
        }

        // Async callback
        private void OnLocalSaveFailed(PlayerIndexEvent ev)
        {
            if (ev == null || ev.PlayerIndex != mPlayerIndex)
                return;

            lock (s_lock)
            {
                mQueuedMode = StorageDialogMode.Failed;
                mModeIsQueued = true;
            }
        }

        private void OnYesPressed(SPEvent ev)
        {
            if (mMode == StorageDialogMode.Failed)
                Mode = StorageDialogMode.SelectStorage;
        }

        private void OnNoPressed(SPEvent ev)
        {
            if (Mode == StorageDialogMode.Failed)
                Mode = StorageDialogMode.Cancelled;
        }

        public void BeginSave()
        {
            if (Mode == StorageDialogMode.Idle || Mode == StorageDialogMode.SelectStorage)
                Mode = StorageDialogMode.InProgress;
        }

        public override void AdvanceTime(double time)
        {
            mInProgressTimer -= time;
            if (mInProgressTimer <= 0)
            {
                mInProgressTimer = 0.1;
                mInProgressWheel.Rotation += SPMacros.SP_D2R(45);
            }

            if (mModeIsQueued)
            {
                lock (s_lock)
                {
                    if (mModeIsQueued)
                    {
                        if (mQueuedMode == StorageDialogMode.InProgress)
                        {
                            if (Mode == StorageDialogMode.SelectStorage)
                                BeginSave();
                        }
                        else if (mQueuedMode == StorageDialogMode.Cancelled)
                        {
                            if (Mode == StorageDialogMode.SelectStorage)
                                Mode = StorageDialogMode.Cancelled;
                        }
                        else if (mQueuedMode == StorageDialogMode.Completed)
                        {
                            if (Mode == StorageDialogMode.InProgress)
                                Mode = StorageDialogMode.Completed;
                        }
                        else if (mQueuedMode == StorageDialogMode.Failed)
                        {
                            if (Mode == StorageDialogMode.InProgress)
                                Mode = StorageDialogMode.Failed;
                        }

                        mQueuedMode = StorageDialogMode.Idle;
                        mModeIsQueued = false;
                    }
                }
            }
            else if (mLoadCountdown > 0)
            {
                --mLoadCountdown;

                if (mLoadCountdown == 0)
                    GameController.GC.ProfileManager.LoadProgress(mPlayerIndex);
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
                        if (mButtonsProxy != null)
                        {
                            mScene.UnsubscribeToInputUpdates(mButtonsProxy, true);
                            mButtonsProxy = null;
                        }

                        FileManager fm = FileManager.FM;
                        fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_SELECTED, (PlayerIndexEventHandler)OnLocalSaveDeviceSelected);
                        fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_CANCELLED, (PlayerIndexEventHandler)OnLocalSaveDeviceCancelled);
                        fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, (PlayerIndexEventHandler)OnLocalLoadCompleted);
                        fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, (PlayerIndexEventHandler)OnLocalLoadFailed);
                        fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_COMPLETED, (PlayerIndexEventHandler)OnLocalSaveCompleted);
                        fm.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_SAVE_FAILED, (PlayerIndexEventHandler)OnLocalSaveFailed);

                        GameController gc = GameController.GC;
                        gc.ProfileManager.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, (PlayerIndexEventHandler)OnLocalLoadCompleted);
                        gc.ProfileManager.RemoveEventListener(FileManager.CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, (PlayerIndexEventHandler)OnLocalLoadFailed);
                        gc.BypassingFileManagerEvents = false;
                    }
                }
                catch (Exception)
                {
                    // Ignore
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
