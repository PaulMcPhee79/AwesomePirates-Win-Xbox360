using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using EasyStorage;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class FileManagerXbox : FileManager
    {
        /// <summary>
        /// Defines the hardware thread on which the operations are performed on the Xbox 360.
        /// </summary>
        public static readonly int[] ProcessorAffinity = new int[] { 5 };

        #region Fields
        private Queue<FileOperationState> pendingStates = new Queue<FileOperationState>(20);
        private readonly object isoLock = new object();
        private readonly object pendingOperationCountLock = new object();
        private int pendingOperations;
        private IsolatedStorageFile mIsoFile;
        public event SaveCompletedEventHandler SaveCompleted;
        public event LoadCompletedEventHandler LoadCompleted;
        #endregion

        #region Methods
        protected override void SetupFileManager()
        {
            base.SetupFileManager();
            mIsoFile = IsolatedStorageFile.GetUserStoreForApplication();
            LoadCompleted += OnGlobalLoadCompleted;
            SaveCompleted += OnGlobalSaveCompleted;
        }

        public override bool IsReadyGlobal()
        {
            return true;
        }

        public override bool IsBusyGlobal()
        {
            lock (pendingOperationCountLock)
            {
                return pendingOperations > 0;
            }
        }

        public override bool FileExistsGlobal(string containerName, string fileName)
        {
            bool exists = false;

            try
            {
                lock (isoLock)
                {
                    using (IsolatedStorageFile isoFile = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (isoFile != null)
                            exists = isoFile.FileExists(fileName);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("XBox::FileExistsGlobal: " + e.Message);
                throw (e);
            }

            return exists;
        }

        protected override void LoadGlobal(string containerName, string fileName, bool async, FileAction loadAction)
        {
            // We Ignore containerName and async (we only load sync from IsolatedStorage on XBox).
            Exception error = null;

            try
            {
                lock (isoLock)
                {
                    IsolatedStorageFile isoFile = mIsoFile;
                    //using (IsolatedStorageFile isoFile = IsolatedStorageFile.GetUserStoreForApplication())
                    //{
                        if (isoFile.FileExists(fileName))
                        {
                            using (IsolatedStorageFileStream fs = isoFile.OpenFile(fileName, FileMode.Open))
                            {
                                if (fs != null)
                                {
                                    loadAction(fs);
                                }
                            }
                        }
                    }
                //}
            }
            catch (Exception e)
            {
                error = e;
            }

            if (LoadCompleted != null)
            {
                // construct our event arguments
                FileActionCompletedEventArgs args = new FileActionCompletedEventArgs(error, null);

                // fire our completion event
                LoadCompleted(this, args);
            }
        }

        protected override void SaveGlobal(string containerName, string fileName, bool async, FileAction saveAction)
        {
            if (containerName == null || fileName == null || saveAction == null)
            {
                if (SaveCompleted != null)
                {
                    // construct our event arguments
                    FileActionCompletedEventArgs args = new FileActionCompletedEventArgs(null, null);

                    // fire our completion event
                    SaveCompleted(this, args);
                }

                return;
            }

            if (async)
            {
                // increment our pending operations count
                PendingOperationsIncrement();

                // get a FileOperationState and fill it in
                FileOperationState state = GetFileOperationState();
                state.Filename = fileName;
                state.Action = saveAction;

                // queue up the work item
                //Thread workItem = new Thread(DoSaveAsyncGlobal);
                //workItem.Start(state);
                ThreadPool.QueueUserWorkItem(DoSaveAsyncGlobal, state);
            }
            else
            {
                // perform the save operation
                Exception error = null;
                try
                {
                    lock (isoLock)
                    {
                        using (IsolatedStorageFile isoFile = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            using (IsolatedStorageFileStream fs = isoFile.CreateFile(fileName))
                            {
                                if (fs != null)
                                {
                                    saveAction(fs);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                if (SaveCompleted != null)
                {
                    // construct our event arguments
                    FileActionCompletedEventArgs args = new FileActionCompletedEventArgs(error, null);

                    // fire our completion event
                    SaveCompleted(this, args);
                }
            }
        }

        /// <summary>
        /// Helper that performs our asynchronous saving.
        /// </summary>
        private void DoSaveAsyncGlobal(object asyncState)
        {
            // set our processor affinity
            SetProcessorAffinity();

            FileOperationState state = asyncState as FileOperationState;
            Exception error = null;

            // perform the save operation
            try
            {
                lock (isoLock)
                {
                    using (IsolatedStorageFile isoFile = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (IsolatedStorageFileStream fs = isoFile.CreateFile(state.Filename))
                        {
                            if (fs != null)
                            {
                                state.Action(fs);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            if (SaveCompleted != null)
            {
                // construct our event arguments
                FileActionCompletedEventArgs args = new FileActionCompletedEventArgs(error, null);

                // fire our completion event
                SaveCompleted(this, args);
            }

            // recycle our state object
            ReturnFileOperationState(state);

            // decrement our pending operation count
            PendingOperationsDecrement();
        }

        /// <summary>
        /// Helper to set processor affinity for a thread.
        /// </summary>
        private void SetProcessorAffinity()
        {
#if XBOX
            // Nick Gravelyn does this on Threadpool threads, so we will too.
            Thread.CurrentThread.SetProcessorAffinity(ProcessorAffinity);
#endif
        }

        /// <summary>
        /// Helper to increment the pending operation count.
        /// </summary>
        private void PendingOperationsIncrement()
        {
            lock (pendingOperationCountLock)
                pendingOperations++;
        }

        /// <summary>
        /// Helper to decrement the pending operation count.
        /// </summary>
        private void PendingOperationsDecrement()
        {
            lock (pendingOperationCountLock)
                pendingOperations--;
        }

        /// <summary>
        /// Helper for getting a FileOperationState object.
        /// </summary>
        private FileOperationState GetFileOperationState()
        {
            lock (pendingStates)
            {
                // recycle any states if we have some available
                if (pendingStates.Count > 0)
                {
                    FileOperationState state = pendingStates.Dequeue();
                    state.Reset();
                    return state;
                }

                return new FileOperationState();
            }
        }

        /// <summary>
        /// Helper for returning a FileOperationState to be recycled.
        /// </summary>
        private void ReturnFileOperationState(FileOperationState state)
        {
            lock (pendingStates)
            {
                pendingStates.Enqueue(state);
            }
        }

        public override void Destroy()
        {
            base.Destroy();

            if (mIsoFile != null)
            {
                mIsoFile.Dispose();
                mIsoFile = null;
            }
        }
        #endregion

        /// <summary>
        /// State object used for our operations.
        /// </summary>
        class FileOperationState
        {
            public string Filename;
            public FileAction Action;

            public void Reset()
            {
                Filename = null;
                Action = null;
            }
        }
    }
}
