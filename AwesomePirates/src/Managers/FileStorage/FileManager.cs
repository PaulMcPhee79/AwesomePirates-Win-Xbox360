using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using System.Diagnostics;
using EasyStorage;
using SparrowXNA;

namespace AwesomePirates
{
    class FileManager : SPEventDispatcher
    {
        public const string CUST_EVENT_TYPE_GLOBAL_SAVE_DEVICE_SELECTED = "globalSaveDeviceDidConnectEvent";
        public const string CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_SELECTED = "localSaveDeviceDidConnectEvent";
        public const string CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_CANCELLED = "localSaveDeviceDidCancelEvent";
        public const string CUST_EVENT_TYPE_LOCAL_LOAD_FAILED = "localLoadFailedEvent";
        public const string CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED = "localLoadCompletedEvent";
        public const string CUST_EVENT_TYPE_LOCAL_SAVE_FAILED = "localSaveFailedEvent";
        public const string CUST_EVENT_TYPE_LOCAL_SAVE_COMPLETED = "localSaveCompletedEvent";

        public const string kSharedStorageContainerName = "Awesome Pirates";

        private static FileManager instance = null;

        protected FileManager()
        {
            mSaveDevice = null;
            mLocalDevices = new PlayerSaveDevice[4];
        }

        protected virtual void SetupFileManager()
        {
            GameController gc = GameController.GC;
            //gc.Components.Add(new GamerServicesComponent(gc));
            EasyStorageSettings.SetSupportedLanguages(Language.English);
            SharedSaveDevice sharedSaveDevice = new SharedSaveDevice();
            gc.Components.Add(sharedSaveDevice);
            mSaveDevice = sharedSaveDevice;

            mQueuedSaves = new List<QueuedSave>(5);

            // hook two event handlers to force the user to choose a new device if they cancel the
            // device selector or if they disconnect the storage device after selecting it
            sharedSaveDevice.DeviceSelectorCanceled += (s, e) => e.Response = SaveDeviceEventResponse.Prompt;
            sharedSaveDevice.DeviceDisconnected += (s, e) => e.Response = SaveDeviceEventResponse.Prompt;
            sharedSaveDevice.DeviceSelected += sharedSaveDevice_DeviceSelected;

#if WINDOWS
            sharedSaveDevice.PromptForDevice();
#endif

            // hook completion events
            mSaveDevice.LoadCompleted += OnGlobalLoadCompleted;
            mSaveDevice.SaveCompleted += OnGlobalSaveCompleted;
        }

        // Async callback
        void sharedSaveDevice_DeviceSelected(object sender, EventArgs e)
        {
            DispatchEvent(new SPEvent(CUST_EVENT_TYPE_GLOBAL_SAVE_DEVICE_SELECTED));
        }

        // Async callback
        void playerSaveDevice_DeviceSelected(object sender, EventArgs e)
        {
            if (sender != null && sender is PlayerSaveDevice)
                DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_SELECTED, (sender as PlayerSaveDevice).Player));
        }

        // Async callback
        void playerSaveDevice_DeviceCancelled(object sender, EventArgs e)
        {
            if (e != null && e is SaveDevicePromptEventArgs && sender != null && sender is PlayerSaveDevice)
            {
                SaveDevicePromptEventArgs args = e as SaveDevicePromptEventArgs;

                if (!args.ShowDeviceSelector)
                    DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_LOCAL_SAVE_DEVICE_CANCELLED, (sender as PlayerSaveDevice).Player));
            }
        }

        public static FileManager FM
        {
            get
            {
                if (instance == null)
                {
#if XBOX
                    instance = new FileManagerXbox();
#else
                    instance = new FileManagerWin();
#endif
                    instance.SetupFileManager();
                }
                return instance;
            }
        }

        #region Fields
        protected SharedSaveDevice mSaveDevice;
        protected PlayerSaveDevice[] mLocalDevices;

        protected readonly object s_lock = new object();
        protected volatile bool mQueuedSaveReady = false;
        protected volatile bool mQueuedSaveRepeatRequested = false;
        protected bool mFreshSavesQueued = false;
        protected PlayerIndex mCurrentLoader;
        protected QueuedSave mCurrentQueueSave;
        protected List<QueuedSave> mQueuedSaves;
        #endregion

        #region Methods
        public bool HasFreshSavesQueued { get { return mFreshSavesQueued; } }
        public virtual bool HasDeviceLocal(PlayerIndex playerIndex)
        {
            if (mLocalDevices != null)
                return mLocalDevices[(int)playerIndex] != null;
            else
                return false;
        }

        public virtual bool IsReadyLocal(PlayerIndex playerIndex)
        {
            PlayerSaveDevice device = mLocalDevices[(int)playerIndex];

            if (device != null)
                return device.IsReady;
            else
                return false;
        }

        public virtual bool IsBusyLocal(PlayerIndex playerIndex)
        {
            PlayerSaveDevice device = mLocalDevices[(int)playerIndex];

            if (device != null)
                return device.IsBusy;
            else
                return false;
        }

        public virtual bool IsReadyGlobal()
        {
            return (mSaveDevice != null && mSaveDevice.IsReady);
        }

        public virtual bool IsBusyGlobal()
        {
            return (mSaveDevice != null && mSaveDevice.IsBusy);
        }

        public bool AddSaveDeviceForPlayer(PlayerIndex playerIndex)
        {
            PlayerSaveDevice device = CreatePlayerSaveDevice(playerIndex);
            mLocalDevices[(int)playerIndex] = device;
            return device != null;
        }

        protected PlayerSaveDevice CreatePlayerSaveDevice(PlayerIndex playerIndex)
        {
            PlayerSaveDevice device = null;

            if (SignedInGamer.SignedInGamers[playerIndex] != null)
            {
                device = mLocalDevices[(int)playerIndex];

                if (device == null)
                {
                    try
                    {
                        device = new PlayerSaveDevice(playerIndex);
                        device.DeviceSelectorCanceled += (s, e) => e.Response = SaveDeviceEventResponse.Prompt;
                        device.DeviceDisconnected += (s, e) => e.Response = SaveDeviceEventResponse.Prompt;
                        device.DeviceSelected += playerSaveDevice_DeviceSelected;

                        // hook completion events
                        device.LoadCompleted += OnLocalLoadCompleted;
                        device.SaveCompleted += OnLocalSaveCompleted;
                        device.DeviceReselectPromptClosed += playerSaveDevice_DeviceCancelled;
                        GameController.GC.Components.Add(device);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("FileManager::CreatePlayerSaveDevice failed: " + e.Message);

                        if (device != null)
                        {
                            try
                            {
                                //device.DestroyStorageContainer();
                                GameController.GC.Components.Remove(device);
                            }
                            catch (Exception) { }
                            finally { device = null; }
                        }
                    }
                }
            }

            return device;
        }

        public void DestroyPlayerSaveDevice(PlayerIndex playerIndex)
        {
            if (mLocalDevices != null)
            {
                PlayerSaveDevice device = mLocalDevices[(int)playerIndex];
                if (device != null)
                {
                    //device.DestroyStorageContainer();
                    GameController.GC.Components.Remove(device);
                    mLocalDevices[(int)playerIndex] = null;
                }
            }
        }

        public virtual void PromptForDeviceLocal(PlayerIndex playerIndex)
        {
            PlayerSaveDevice device = mLocalDevices[(int)playerIndex];

            if (device == null)
            {
                device = CreatePlayerSaveDevice(playerIndex);
                mLocalDevices[(int)playerIndex] = device;
            }

            if (device != null)
                device.PromptForDevice();
        }

        public virtual void PromptForDeviceGlobal()
        {
            if (mSaveDevice != null)
                mSaveDevice.PromptForDevice();
        }

        public virtual bool FileExistsLocal(PlayerIndex playerIndex, string containerName, string fileName)
        {
            bool exists = false;

            try
            {
                PlayerSaveDevice device = mLocalDevices[(int)playerIndex];
                exists = (device != null && device.FileExists(containerName, fileName));
            }
            catch (Exception e)
            {
                Debug.WriteLine("FileExistsLocal :" + e.Message);
                throw (e);
            }

            return exists;
        }

        public virtual bool FileExistsGlobal(string containerName, string fileName)
        {
            bool exists = false;

            try
            {
                exists = (mSaveDevice != null && mSaveDevice.FileExists(containerName, fileName));
            }
            catch (Exception e)
            {
                Debug.WriteLine("FileExistsGlobal: " + e.Message);
                throw (e);
            }

            return exists;
        }

        public void LoadLocal(PlayerIndex playerIndex, string containerName, string fileName, FileAction loadAction)
        {
            LoadLocal(playerIndex, containerName, fileName, false, loadAction);
        }

        public void LoadAsyncLocal(PlayerIndex playerIndex, string containerName, string fileName, FileAction loadAction)
        {
            LoadLocal(playerIndex, containerName, fileName, true, loadAction);
        }

        protected virtual void LoadLocal(PlayerIndex playerIndex, string containerName, string fileName, bool async, FileAction loadAction)
        {
            PlayerSaveDevice device = mLocalDevices[(int)playerIndex];
            if (device != null)
            {
                mCurrentLoader = playerIndex;

                if (async)
                    device.LoadAsync(containerName, fileName, loadAction);
                else
                    device.Load(containerName, fileName, loadAction);
            }
        }

        public void LoadGlobal(string containerName, string fileName, FileAction loadAction)
        {
            LoadGlobal(containerName, fileName, false, loadAction);
        }

        public void LoadAsyncGlobal(string containerName, string fileName, FileAction loadAction)
        {
            LoadGlobal(containerName, fileName, true, loadAction);
        }

        protected virtual void LoadGlobal(string containerName, string fileName, bool async, FileAction loadAction)
        {
            try
            {
                if (async)
                    mSaveDevice.LoadAsync(containerName, fileName, loadAction);
                else
                    mSaveDevice.Load(containerName, fileName, loadAction);
            }
            catch (Exception e)
            {
                Debug.WriteLine("LoadGlobal: " + e.Message);
                throw (e);
            }
        }

        protected virtual void OnGlobalLoadCompleted(object sender, FileActionCompletedEventArgs args)
        {
            if (args.Error == null)
                Debug.WriteLine("Global Load completed.");
            else
                Debug.WriteLine(args.Error.Message);
        }

        protected virtual void OnLocalLoadCompleted(object sender, FileActionCompletedEventArgs args)
        {
            if (args.Error == null)
            {
                Debug.WriteLine("Local Load completed.");
                DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_LOCAL_LOAD_COMPLETED, mCurrentLoader));
            }
            else
            {
                Debug.WriteLine(args.Error.Message);
                DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_LOCAL_LOAD_FAILED, mCurrentLoader));
            }
        }

        public void SaveLocal(PlayerIndex playerIndex, string containerName, string fileName, FileAction saveAction)
        {
            SaveLocal(playerIndex, containerName, fileName, false, saveAction);
        }

        public void SaveAsyncLocal(PlayerIndex playerIndex, string containerName, string fileName, FileAction saveAction)
        {
            SaveLocal(playerIndex, containerName, fileName, true, saveAction);
        }

        protected virtual void SaveLocal(PlayerIndex playerIndex, string containerName, string fileName, bool async, FileAction saveAction)
        {
            PlayerSaveDevice device = mLocalDevices[(int)playerIndex];
            if (device != null)
            {
                if (async)
                    device.SaveAsync(containerName, fileName, saveAction);
                else
                    device.Save(containerName, fileName, saveAction);
            }
        }

        public void QueueLocalSaveAsync(PlayerIndex playerIndex, string containerName, string fileName, FileAction saveAction)
        {
            SignedInGamer sigGamer = SignedInGamer.SignedInGamers[playerIndex];
            if (sigGamer == null || sigGamer.IsGuest)
                return;

            mFreshSavesQueued = true;
            lock (s_lock)
            {
                mQueuedSaves.Add(new QueuedSave(playerIndex, QueuedSave.QueuedSaveType.Local, sigGamer.Gamertag, containerName, fileName, saveAction));

                // If count is > 1, then a save is in progress - let it reset the ready state on completion.
                if (!mQueuedSaveReady && mQueuedSaves.Count == 1)
                    mQueuedSaveReady = true;
            }
        }

        public void SaveGlobal(string containerName, string fileName, FileAction saveAction)
        {
            SaveGlobal(containerName, fileName, false, saveAction);
        }

        public void SaveAsyncGlobal(string containerName, string fileName, FileAction saveAction)
        {
            SaveGlobal(containerName, fileName, true, saveAction);
        }

        public void QueueGlobalSaveAsync(string containerName, string fileName, FileAction saveAction)
        {
            mFreshSavesQueued = true;
            lock (s_lock)
            {
                mQueuedSaves.Add(new QueuedSave(PlayerIndex.One, QueuedSave.QueuedSaveType.Global, null, containerName, fileName, saveAction));

                // If count is > 1, then a save is in progress - let it reset the ready state on completion.
                if (!mQueuedSaveReady && mQueuedSaves.Count == 1)
                    mQueuedSaveReady = true;
            }
        }

        protected virtual void SaveGlobal(string containerName, string fileName, bool async, FileAction saveAction)
        {
            if (async)
                mSaveDevice.SaveAsync(containerName, fileName, saveAction);
            else
                mSaveDevice.Save(containerName, fileName, saveAction);
        }

        protected virtual void OnGlobalSaveCompleted(object sender, FileActionCompletedEventArgs args)
        {
            lock (s_lock)
            {
                if (mQueuedSaves.Count > 0)
                    mQueuedSaves.RemoveAt(0);
                mQueuedSaveReady = mQueuedSaves != null && mQueuedSaves.Count > 0;
            }

            if (args.Error == null)
                Debug.WriteLine("Global Save completed.");
            else
                Debug.WriteLine(args.Error.Message);
        }

        // Must call ConfirmRepeatedQueuedSaveRequest after making a successful repeat request.
        public virtual bool RequestRepeatedQueuedSave()
        {
            bool didSucceed = false;

            lock (s_lock)
            {
                if (mCurrentQueueSave != null && mQueuedSaves != null)
                {
                    mQueuedSaves.Insert(0, mCurrentQueueSave);
                    mCurrentQueueSave = null;
                    mQueuedSaveRepeatRequested = didSucceed = true;
                    mQueuedSaveReady = false; // Must wait for confirmation from client.
                }
            }

            return didSucceed;
        }

        public virtual void ConfirmRepeatedQueuedSaveRequest(bool confirm)
        {
            lock (s_lock)
            {
                if (!mQueuedSaveRepeatRequested)
                    return;
                mQueuedSaveRepeatRequested = false;

                if (confirm)
                {
                    mQueuedSaveReady = true; // Received confirmation from client.
                }
                else
                {
                    if (mQueuedSaves != null)
                    {
                        if (mQueuedSaves.Count > 0)
                        {
                            QueuedSave qSave = mQueuedSaves[0];
                            qSave.QSaveAction = null;
                            mQueuedSaves.RemoveAt(0);
                        }

                        mQueuedSaveReady = mQueuedSaves.Count > 0; // Received confirmation from client.
                    }
                }
            }
        }

        protected virtual void OnLocalSaveCompleted(object sender, FileActionCompletedEventArgs args)
        {
            PlayerIndex? playerIndex = null;

            lock (s_lock)
            {
                if (mCurrentQueueSave != null)
                    playerIndex = mCurrentQueueSave.QPlayerIndex;
                if (mQueuedSaves.Count > 0)
                    mQueuedSaves.RemoveAt(0);

                mQueuedSaveReady = mQueuedSaves != null && mQueuedSaves.Count > 0;
            }

            if (args.Error == null)
            {
                Debug.WriteLine("Local Save completed successfully.");

                if (playerIndex.HasValue)
                    DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_LOCAL_SAVE_COMPLETED, playerIndex.Value));
            }
            else
            {
                Debug.WriteLine("Local Save failed: " + args.Error.Message);

                if (playerIndex.HasValue)
                    DispatchEvent(new PlayerIndexEvent(CUST_EVENT_TYPE_LOCAL_SAVE_FAILED, playerIndex.Value));
            }
        }

        protected void ProcessQueuedSaves()
        {
            if (mQueuedSaveReady)
            {
                lock (s_lock)
                {
                    if (mQueuedSaves.Count > 0)
                    {
                        mCurrentQueueSave = mQueuedSaves[0];
                        //mQueuedSaves.RemoveAt(0);

                        if (mCurrentQueueSave.QType == QueuedSave.QueuedSaveType.Local)
                        {
                            SignedInGamer sigGamer = SignedInGamer.SignedInGamers[mCurrentQueueSave.QPlayerIndex];
                            if (sigGamer == null || mCurrentQueueSave.QGamerTag == null || !mCurrentQueueSave.QGamerTag.Equals(sigGamer.Gamertag))
                            {
                                mQueuedSaves.RemoveAt(0);
                                mCurrentQueueSave = null;
                            }
                            else
                            {
                                mQueuedSaveReady = false;
                                SaveAsyncLocal(mCurrentQueueSave.QPlayerIndex, mCurrentQueueSave.QContainerName, mCurrentQueueSave.QFileName, mCurrentQueueSave.QSaveAction);
                            }
                        }
                        else if (mCurrentQueueSave.QType == QueuedSave.QueuedSaveType.Global)
                        {
                            mQueuedSaveReady = false;
                            SaveAsyncGlobal(mCurrentQueueSave.QContainerName, mCurrentQueueSave.QFileName, mCurrentQueueSave.QSaveAction);
                        }
                    }
                    else
                        mQueuedSaveReady = false;
                }
            }
        }

        public void Update()
        {
            // Allow GUI to refresh before calling potentially blocking operations (storage container opening).
            if (mFreshSavesQueued)
            {
                mFreshSavesQueued = false;
                return;
            }

            ProcessQueuedSaves();
        }

        public static void MaskUnmaskBuffer(int mask, byte[] buffer, int len, int offset = 0)
        {
            int masker = mask;
            for (int i = offset; i < len; ++i)
            {
                buffer[i] = (byte)((int)buffer[i] ^ masker);
                masker += i & 7;
                masker ^= mask;
                mask = ~(mask + masker);
            }
        }

        protected class QueuedSave
        {
            public enum QueuedSaveType { Local = 0, Global }

            public QueuedSave(PlayerIndex playerIndex, QueuedSaveType type, string gamerTag, string containerName, string fileName, FileAction saveAction)
            {
                QPlayerIndex = playerIndex;
                QType = type;
                QGamerTag = gamerTag;
                QContainerName = containerName;
                QFileName = fileName;
                QSaveAction = saveAction;
            }

            public QueuedSaveType QType { get; private set; }
            public PlayerIndex QPlayerIndex { get; private set; }
            public string QGamerTag { get; private set; }
            public string QContainerName { get; private set; }
            public string QFileName { get; private set; }
            public FileAction QSaveAction { get; set; }
        }

        public virtual void Destroy()
        {
            if (mSaveDevice != null)
            {
                //mSaveDevice.DestroyStorageContainer();
                mSaveDevice = null;
            }

            if (mLocalDevices != null)
            {
                //foreach (PlayerSaveDevice device in mLocalDevices)
                //{
                //    if (device != null)
                //        device.DestroyStorageContainer();
                //}
                mLocalDevices = null;
            }
        }
        #endregion
    }
}
