using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    sealed class ChannelBuffer : IDisposable
    {
        public const int kChannelPaused = 1 << 0;
        public const int kChannelLoops = 1 << 1;
        public const int kChannelIsMusic = 1 << 2;
        public const int kChannelAlwaysOn = 1 << 3;
        public const int kChannelOnDemand = 1 << 4;

        public ChannelBuffer(SPJuggler juggler, int capacity, string soundName, float easeOutDuration = 0f, int settings = 0)
        {
            mCapacity = Math.Max(1, capacity);
            mEaseOutDuration = easeOutDuration;
            mSettings = settings;
            mMasterVolume = 1f;
            mPlaying = new List<SfxChannel>(capacity);
            mStopping = new List<SfxChannel>(capacity);
            mJuggler = juggler;
            Debug.Assert(mJuggler != null, "ChannelBuffer juggler must not be null.");

            if (soundName != null)
            {
                string noExtension = soundName.Substring(0, soundName.Length - 4); // FIXME: Remove the need for this hack.
                mSound = GameController.GC.Content.Load<SoundEffect>(noExtension);
            }
        }

        #region Fields
        private bool mIsDisposed = false;
        private int mSettings; 
        private int mCapacity;
        private float mEaseOutDuration;
        private float mMasterVolume;
        private SoundEffect mSound;

        private List<SfxChannel> mPlaying;
        private List<SfxChannel> mStopping;
        private SPJuggler mJuggler;
        #endregion

        #region Properties
        public bool Paused { get { return (mSettings & kChannelPaused) == kChannelPaused; } }
        public bool Loops { get { return (mSettings & kChannelLoops) == kChannelLoops; } }
        public bool IsMusic { get { return (mSettings & kChannelIsMusic) == kChannelIsMusic; } }
        public bool AlwaysOn { get { return (mSettings & kChannelAlwaysOn) == kChannelAlwaysOn; } }
        public bool OnDemand { get { return (mSettings & kChannelOnDemand) == kChannelOnDemand; } }
        public int Capacity { get { return mCapacity; } }
        public float EaseOutDuration { get { return mEaseOutDuration; } }
        public float MasterVolume
        {
            get { return mMasterVolume; }
            set
            {
                mMasterVolume = value;

                if (mPlaying != null)
                {
                    foreach (SfxChannel channel in mPlaying)
                        channel.MasterVolume = value;
                }

                if (mStopping != null)
                {
                    foreach (SfxChannel channel in mStopping)
                        channel.MasterVolume = value;
                }
            }
        }
        public int NumActiveChannels { get { return mPlaying.Count + mStopping.Count; } }
        public SPJuggler Juggler { get { return mJuggler; } }
        #endregion

        #region Methods
        private void PlaySoundChannel(SfxChannel channel)
        {
            if (channel == null || channel.Instance.State == SoundState.Playing)
                return;
            channel.Instance.Play();

            if (mStopping.Contains(channel))
                mStopping.Remove(channel);
            if (!mPlaying.Contains(channel))
                mPlaying.Add(channel);
        }

        private void SetVolumeForChannel(float volume, SfxChannel channel, float easeDuration = 0f)
        {
            if (channel == null)
                return;

            mJuggler.RemoveTweensWithTarget(channel);
            easeDuration = Math.Max(0, easeDuration);

            if (!SPMacros.SP_IS_FLOAT_EQUAL(easeDuration, 0f))
            {
                SPTween tween = new SPTween(channel, easeDuration);
                tween.AnimateProperty("VolumeProxy", volume);
                mJuggler.AddObject(tween);
            }
            else
            {
                channel.VolumeProxy = volume;
            }
        }

        private void FadeVolumeForChannel(SfxChannel channel, float easeDuration)
        {
            if (channel == null)
                return;

            mJuggler.RemoveTweensWithTarget(channel);

            SPTween tween = new SPTween(channel, easeDuration);
            tween.AnimateProperty("VolumeProxy", 0f);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnChannelFaded);
            mJuggler.AddObject(tween);
        }

        private void OnChannelFaded(SPEvent ev)
        {
            SPTween tween = ev.CurrentTarget as SPTween;

            if (tween != null)
            {
                SfxChannel channel = tween.Target as SfxChannel;

                if (channel != null && !channel.Instance.IsDisposed)
                    channel.Instance.Stop();
            }
        }

        public void SetVolume(float volume, float easeDuration = 0f)
        {
            foreach (SfxChannel channel in mPlaying)
                SetVolumeForChannel(volume, channel, easeDuration);
        }

        public void Play(float volume = 1f, float easeDuration = 0f)
        {
            SfxChannel sfxChannel = null;

            if (!OnDemand)
            {
                foreach (SfxChannel channel in mPlaying)
                {
                    if (!channel.Instance.IsDisposed && channel.Instance.State == SoundState.Stopped)
                    {
                        sfxChannel = channel;
                        break;
                    }
                }
            }

            if (OnDemand || sfxChannel == null)
            {
                while (NumActiveChannels >= Capacity)
                {
                    if (mStopping.Count > 0)
                    {
                        sfxChannel = mStopping[0];
                        mStopping.RemoveAt(0);
                        mJuggler.RemoveTweensWithTarget(sfxChannel);

                        if (!sfxChannel.Instance.IsDisposed)
                        {
                            sfxChannel.Instance.Stop();
                            sfxChannel.Dispose();
                        }

                        sfxChannel = null;
                    }
                    else if (mPlaying.Count > 0)
                    {
                        sfxChannel = mPlaying[0];
                        mPlaying.RemoveAt(0);
                        mJuggler.RemoveTweensWithTarget(sfxChannel);

                        if (!sfxChannel.Instance.IsDisposed)
                        {
                            sfxChannel.Instance.Stop();
                            sfxChannel.Dispose();
                        }

                        sfxChannel = null;
                    }
                }
            }

            if (sfxChannel == null)
            {
                sfxChannel = new SfxChannel(mSound.CreateInstance(), 0f, mMasterVolume);
                sfxChannel.Instance.IsLooped = Loops;
            }

            sfxChannel.VolumeProxy = 0;
            SetVolumeForChannel(volume, sfxChannel, easeDuration);
            PlaySoundChannel(sfxChannel);
        }

        public void Pause()
        {
            if (Paused)
                return;

            foreach (SfxChannel channel in mPlaying)
            {
                if (channel.Instance.State == SoundState.Playing)
                    channel.Instance.Pause();
            }
            mSettings |= kChannelPaused;
        }

        public void Resume()
        {
            if (!Paused)
                return;

            foreach (SfxChannel channel in mPlaying)
            {
                if (channel.Instance.State == SoundState.Paused)
                    channel.Instance.Resume();
            }
            mSettings &= ~kChannelPaused;
        }

        private void StopChannels(List<SfxChannel> channels)
        {
            if (channels == null)
                return;

            foreach (SfxChannel channel in channels)
            {
                mJuggler.RemoveTweensWithTarget(channel);

                if (channel.Instance.IsDisposed)
                    continue;

                if (channel.Instance.State != SoundState.Stopped)
                    channel.Instance.Stop();
                channel.Dispose();
            }
        }

        public void Stop()
        {
            if (mPlaying == null || mStopping == null)
            {
                mSettings &= ~kChannelPaused;
                return;
            }

            StopChannels(mPlaying);
            mPlaying.Clear();
            //StopChannels(mStopping); // Don't double dispose
            mStopping.Clear();
            mSettings &= ~kChannelPaused;
        }

        public void StopEaseOut()
        {
            if (!SPMacros.SP_IS_FLOAT_EQUAL(mEaseOutDuration, 0f))
                StopWithEaseDuration(mEaseOutDuration);
            else
                Stop();
        }

        public void StopWithEaseDuration(float easeDuration)
        {
            if (mPlaying.Count > 0)
            {
                while (mPlaying.Count > 0)
                {
                    SfxChannel channel = mPlaying[0];

                    if (mPlaying.Contains(channel))
                        mPlaying.Remove(channel);
                    if (!mStopping.Contains(channel))
                        mStopping.Add(channel);

                    FadeVolumeForChannel(channel, easeDuration);
                }
            }
            else
            {
                Stop();
            }

            mSettings &= ~kChannelPaused;
        }

        public void FadeAllSounds()
        {
            StopEaseOut();
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mSound != null)
                        {
                            Stop();
                            //mSound.Dispose(); // ContentManager must dispose of this resource
                            mSound = null;
                        }

                        mPlaying = null;
                        mStopping = null;
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

        ~ChannelBuffer()
        {
            Dispose(false);
        }
        #endregion
    }
}
