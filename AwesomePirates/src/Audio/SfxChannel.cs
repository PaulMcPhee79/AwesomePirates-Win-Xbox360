using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;

namespace AwesomePirates
{
    class SfxChannel : IDisposable
    {
        public SfxChannel(SoundEffectInstance instance, float volume = 1f, float masterVolume = 1f)
        {
            Debug.Assert(instance != null, "SfxChannel requires a non-null SoundEffectInstance.");
            mInstance = instance;
            mMasterVolume = masterVolume;
            VolumeProxy = volume;
        }

        protected bool mIsDisposed = false;
        private float mVolumeProxy;
        private float mMasterVolume;
        private SoundEffectInstance mInstance;

        public float VolumeProxy
        {
            get { return mVolumeProxy; }
            set
            {
                mVolumeProxy = Math.Max(0f, Math.Min(1f, value));
                mInstance.Volume = mVolumeProxy * mMasterVolume;
            }
        }
        public float MasterVolume
        {
            get { return mMasterVolume; }
            set
            {
                mMasterVolume = Math.Max(0f, Math.Min(1f, value));
                VolumeProxy = VolumeProxy;
            }
        }
        public SoundEffectInstance Instance { get { return mInstance; } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mInstance != null)
                        {
                            mInstance.Dispose();
                            mInstance = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
                finally
                {
                    mIsDisposed = true;
                }
            }
        }

        ~SfxChannel()
        {
            Dispose(false);
        }
    }
}
