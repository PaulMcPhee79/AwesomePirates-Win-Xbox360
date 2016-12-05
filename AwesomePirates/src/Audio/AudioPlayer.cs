using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using SparrowXNA;

namespace AwesomePirates
{
    sealed class AudioPlayer : IDisposable
    {
        public const float kMaxVolumeKnob = 10f;

        public AudioPlayer(string basePath)
        {
            mSfxVolume = mMusicVolume = 1f;
            mMarkedForDestruction = 0;
            mBasePath = (basePath == null) ? "" : basePath;
            mSfxBuffers = new Dictionary<string, ChannelBuffer>();
            mJuggler = new SPJuggler();
        }

        #region Fields
        private bool mIsDisposed = false;
        private int mMarkedForDestruction;
        private float mSfxVolume;
        private float mMusicVolume;
        private string mBasePath;
        private Dictionary<string, ChannelBuffer> mSfxBuffers;
        private SPJuggler mJuggler;
        #endregion

        #region Properties
        public float SfxVolume
        {
            get { return mSfxVolume; }
            set
            {
                float oldValue = mSfxVolume;
                mSfxVolume = value;

                UpdateSfxBuffers();
            }
        }
        public float MusicVolume
        {
            get { return mMusicVolume; }
            set
            {
                float oldValue = mMusicVolume;
                mMusicVolume = value;

                UpdateMusicBuffers();
            }
        }
        public bool MarkedForDestruction { get { return (mMarkedForDestruction == 2); } }
        public float FadeOutDuration
        {
            get
            {
                float duration = 0f;

                foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
                {
                    if (kvp.Value.EaseOutDuration > duration)
                        duration = kvp.Value.EaseOutDuration;
                }

                return duration;
            }
        }
        #endregion

        #region Methods
        private void UpdateSfxBuffers()
        {
            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
            {
                if (!kvp.Value.IsMusic)
                {
                    if (mSfxVolume != 0)
                    {
                        if (kvp.Value.AlwaysOn)
                            kvp.Value.Resume();
                    }
                    else
                    {
                        if (kvp.Value.AlwaysOn)
                            kvp.Value.Pause();
                    }

                    kvp.Value.MasterVolume = mSfxVolume;
                }
            }
        }

        private void UpdateMusicBuffers()
        {
            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
            {
                if (kvp.Value.IsMusic)
                {
                    if (mMusicVolume != 0)
                    {
                        if (kvp.Value.AlwaysOn)
                            kvp.Value.Resume();
                    }
                    else
                    {
                        if (kvp.Value.AlwaysOn)
                            kvp.Value.Pause();
                        else
                            kvp.Value.Stop();
                    }

                    kvp.Value.MasterVolume = mMusicVolume;
                }
            }
        }

        private bool ChannelMayPlay(ChannelBuffer channel)
        {
            return (channel != null && ((mSfxVolume != 0 && !channel.IsMusic) || (mMusicVolume != 0 && channel.IsMusic)));
        }

        public void LoadAudioSettingsFromPlist(string plistPath)
        {
            Dictionary<string, object> audioSettings = PlistParser.DictionaryFromPlist(plistPath);

            foreach (KeyValuePair<string, object> kvp in audioSettings)
            {
                Dictionary<string, object> dict = kvp.Value as Dictionary<string, object>;
                string filename = dict["Filename"] as string;
                int count = Convert.ToInt32(dict["Count"]);
                bool loop = Convert.ToBoolean(dict["Loop"]);
                bool alwaysOn = (dict.ContainsKey("AlwaysOn")) ? Convert.ToBoolean(dict["AlwaysOn"]) : false;
                bool isMusic = (dict.ContainsKey("IsMusic")) ? Convert.ToBoolean(dict["IsMusic"]) : false;
                bool onDemand = (dict.ContainsKey("OnDemand")) ? Convert.ToBoolean(dict["OnDemand"]) : false;
                float easeOutDuration = Globals.ConvertToSingle(dict["EaseOutDuration"]);
                int settings = 0;

                if (loop)
                    settings |= ChannelBuffer.kChannelLoops;
                if (alwaysOn)
                    settings |= ChannelBuffer.kChannelAlwaysOn;
                if (isMusic)
                    settings |= ChannelBuffer.kChannelIsMusic;
                if (onDemand)
                    settings |= ChannelBuffer.kChannelOnDemand;

#if WINDOWS || XBOX
                // May add higher channel capacity on these systems, but too many overlapping sfx can sound bad (e.g. PowderKegs in a Sea of Lava).
                ChannelBuffer sfxBuffer = new ChannelBuffer(mJuggler, count, mBasePath + filename, easeOutDuration, settings);
#else
                ChannelBuffer sfxBuffer = new ChannelBuffer(mJuggler, count, mBasePath + filename, easeOutDuration, settings);
#endif
                AddChannelBuffer(sfxBuffer, kvp.Key);
            }
        }

        private void AddChannelBuffer(ChannelBuffer channelBuffer, string key)
        {
            if (key != null)
                mSfxBuffers[key] = channelBuffer;
        }

        public void PlaySoundWithKey(string key, float volume = 1f, float easeInDuration = 0f)
        {
            if (key == null)
                throw new ArgumentNullException("Attempt to play sound with null key.");

            if (mSfxBuffers.ContainsKey(key))
            {
                ChannelBuffer buffer = mSfxBuffers[key];
                if (ChannelMayPlay(buffer))
                    buffer.Play(volume, easeInDuration);
            }
        }

        public void PlayRandomSoundWithKeyPrefix(string key, int minValue, int maxValue, float volume = 1f)
        {
            int index = GameController.GC.NextRandom(minValue, maxValue);
            PlaySoundWithKey(key + index, volume);
        }

        public void PauseSoundWithKey(string key)
        {
            if (key == null)
                throw new ArgumentNullException("Attempt to pause sound with null key.");

            if (mSfxBuffers.ContainsKey(key))
                mSfxBuffers[key].Pause();
        }

        public void ResumeSoundWithKey(string key)
        {
            if (key == null)
                throw new ArgumentNullException("Attempt to resume sound with null key.");

            if (mSfxBuffers.ContainsKey(key))
            {
                ChannelBuffer buffer = mSfxBuffers[key];
                if (ChannelMayPlay(buffer))
                    buffer.Resume();
            }
        }

        public void StopSoundWithKey(string key, float easeOutDuration = 0f)
        {
            if (key == null)
                throw new ArgumentNullException("Attempt to stop sound with null key.");

            if (mSfxBuffers.ContainsKey(key))
            {
                ChannelBuffer buffer = mSfxBuffers[key];
                if (SPMacros.SP_IS_FLOAT_EQUAL(easeOutDuration, 0f))
                    buffer.Stop();
                else
                    buffer.StopWithEaseDuration(easeOutDuration);
            }
        }

        public void StopEaseOutSoundWithKey(string key)
        {
            if (key == null)
                throw new ArgumentNullException("Attempt to stop sound with null key.");

            if (mSfxBuffers.ContainsKey(key))
                mSfxBuffers[key].StopEaseOut();
        }

        public void SetVolume(float volume, string key, float easeDuration = 0f)
        {
            if (key == null)
                throw new ArgumentNullException("Attempt to change volume of sound with null key.");

            if (mSfxBuffers.ContainsKey(key))
                mSfxBuffers[key].SetVolume(volume, easeDuration);
        }

        public void RemoveSoundWithKey(string key)
        {
            if (key != null)
                mSfxBuffers.Remove(key);
        }

        public void Pause()
        {
            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
                kvp.Value.Pause();
        }

        public void Resume()
        {
            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
            {
                if (ChannelMayPlay(kvp.Value))
                    kvp.Value.Resume();
            }
        }

        public void Stop()
        {
            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
                kvp.Value.Stop();
        }

        public void StopEaseOutSounds()
        {
            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
                kvp.Value.StopEaseOut();
        }

        public void FadeAllSounds()
        {
            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
                kvp.Value.FadeAllSounds();
        }

        public void FadeAndMarkForDestruction()
        {
            if (mMarkedForDestruction != 0)
                return;

            mMarkedForDestruction = 1;

            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
                kvp.Value.FadeAllSounds();
            mJuggler.DelayInvocation(this, FadeOutDuration, MarkForDestruction);
        }

        private void MarkForDestruction()
        {
            if (mMarkedForDestruction == 1)
                mMarkedForDestruction = 2;
        }

        public void RemoveAllSounds()
        {
            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
                kvp.Value.Dispose();

            mSfxBuffers.Clear();
        }

        public void AdvanceTime(double time)
        {
            if (mJuggler != null)
                mJuggler.AdvanceTime(time);
        }

        public void DestroyAudioPlayer()
        {
            Dispose();
        }

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
                        if (mSfxBuffers != null)
                        {
                            foreach (KeyValuePair<string, ChannelBuffer> kvp in mSfxBuffers)
                                kvp.Value.Dispose();
                            mSfxBuffers.Clear();
                            mSfxBuffers = null;
                        }

                        mJuggler.RemoveAllObjects();
                        mJuggler = null;
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

        ~AudioPlayer()
        {
            Dispose(false);
        }
        #endregion
    } 
}
