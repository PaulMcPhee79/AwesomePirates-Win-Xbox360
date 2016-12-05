using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;

namespace AwesomePirates
{
    class XACTAudioPlayer : IDisposable
    {
        public const float kXACTMaxVolumeKnob = 10f;

        public XACTAudioPlayer(string engineName, string[] waveBankNames, string[] soundBankNames, string[] categories, string[] cues)
        {
            Debug.Assert(waveBankNames != null && soundBankNames != null && categories != null && cues != null, "Bad arguments to XACTAudioPlayer constructor.");

            try
            {
                mAudioEngine = new AudioEngine(engineName);
                mWaveBanks = new WaveBank[waveBankNames.Length];
                mSoundBanks = new SoundBank[soundBankNames.Length];

                for (int i = 0; i < waveBankNames.Length; ++i)
                    mWaveBanks[i] = new WaveBank(mAudioEngine, waveBankNames[i]);

                for (int i = 0; i < soundBankNames.Length; ++i)
                    mSoundBanks[i] = new SoundBank(mAudioEngine, soundBankNames[i]);

                mCategories = new Dictionary<string, AudioCategory>(categories.Length);
                foreach (string name in categories)
                    mCategories[name] = mAudioEngine.GetCategory(name);

                mCues = new Dictionary<string, List<Cue>>(cues.Length);
                foreach (string name in cues)
                    mCues[name] = new List<Cue>(5);
            }
            catch (InvalidOperationException ioe)
            {
                Debug.WriteLine("1: Failed to create AudioEngine: " + ioe.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine("2: Failed to create AudioEngine: " + e.Message);
            }
        }

        #region Fields
        private bool mIsDisposed = false;
        private AudioEngine mAudioEngine;
        private WaveBank[] mWaveBanks;
        private SoundBank[] mSoundBanks;
        private Dictionary<string, AudioCategory> mCategories;
        private Dictionary<string, List<Cue>> mCues;
        #endregion

        #region Properties

        #endregion

        #region Methods
        public bool IsCuePlaying(string name)
        {
            if (name == null || mCues == null || !mCues.ContainsKey(name))
                return false;

            bool isPlaying = false;
            List<Cue> cues = mCues[name];
            foreach (Cue cue in cues)
            {
                if (cue.IsPlaying)
                {
                    isPlaying = true;
                    break;
                }
            }

            return isPlaying;
        }

        public void SetVolumeForCategory(float volume, string name)
        {
            if (mAudioEngine == null || !mCategories.ContainsKey(name))
                return;
            mCategories[name].SetVolume(MathHelper.Clamp(volume, 0f, 1f));
        }

        public void Play(string name)
        {
            if (mAudioEngine == null || name == null || mSoundBanks == null || mCues == null || !mCues.ContainsKey(name))
                return;

            // Use this opportunity to remove old cues so that we don't exceed the xbox's 300 concurrent cue limit.
            List<Cue> cues = mCues[name];
            int cueCount = cues.Count;

            for (int i = 0; i < cueCount; ++i)
            {
                if (cues[0].IsPaused)
                    continue;
                else if (cues[0].IsStopped)
                {
                    Cue stoppedCue = cues[0];
                    cues.RemoveAt(0);

                    if (!stoppedCue.IsDisposed)
                        stoppedCue.Dispose();
                }
                else
                    break; // Older cues will also not be stopped.
            }

            foreach (SoundBank soundbank in mSoundBanks)
            {
                //try
                //{
                    Cue cue = soundbank.GetCue(name);
                
                    if (cue != null)
                    {
                        if (cue.IsPrepared)
                        {
                            cues.Add(cue);
                            cue.Play();
                        }
                        else
                            cue.Dispose();
                        break;
                    }
                //}
                //catch (Exception)
                //{
                //    continue;
                //}
            }
        }

        public void PauseCategory(string name)
        {
            if (mAudioEngine == null || mCategories == null || !mCategories.ContainsKey(name))
                return;

            mCategories[name].Pause();
        }

        public void Pause()
        {
            if (mAudioEngine == null || mCategories == null)
                return;

            foreach (KeyValuePair<string, AudioCategory> kvp in mCategories)
                kvp.Value.Pause();
        }

        public void ResumeCategory(string name)
        {
            if (mAudioEngine == null || mCategories == null || !mCategories.ContainsKey(name))
                return;

            mCategories[name].Resume();
        }

        public void Resume()
        {
            if (mAudioEngine == null || mCategories == null)
                return;

            foreach (KeyValuePair<string, AudioCategory> kvp in mCategories)
                kvp.Value.Resume();
        }

        public void StopCategory(string name)
        {
            if (mAudioEngine == null || name == null || mCategories == null || !mCategories.ContainsKey(name))
                return;

            AudioCategory category = mCategories[name];
            category.Stop(AudioStopOptions.Immediate);
        }

        public void Stop(string name, AudioStopOptions options = AudioStopOptions.AsAuthored)
        {
            if (mAudioEngine == null || name == null || mCues == null || !mCues.ContainsKey(name))
                return;

            List<Cue> cues = mCues[name];
            foreach (Cue cue in cues)
            {
                if (cue.IsPlaying || cue.IsPaused)
                    cue.Stop(AudioStopOptions.AsAuthored);
            }
        }

        public void AdvanceTime(double time)
        {
            if (mAudioEngine != null)
                mAudioEngine.Update();
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
                try
                {
                    if (disposing)
                    {
                        if (mAudioEngine != null)
                        {
                            mAudioEngine.Dispose();
                            mAudioEngine = null;
                        }

                        if (mWaveBanks != null)
                        {
                            foreach (WaveBank waveBank in mWaveBanks)
                                waveBank.Dispose();
                            mWaveBanks = null;
                        }

                        if (mSoundBanks != null)
                        {
                            foreach (SoundBank soundBank in mSoundBanks)
                                soundBank.Dispose();
                            mSoundBanks = null;
                        }

                        if (mCues != null)
                        {
                            foreach (KeyValuePair<string, List<Cue>> kvp in mCues)
                            {
                                foreach (Cue cue in kvp.Value)
                                    cue.Dispose();
                            }

                            mCues = null;
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

        ~XACTAudioPlayer()
        {
            Dispose(false);
        }
        #endregion
    }
}
