using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class OptionsView : Prop, IInteractable
    {
        public const string CUST_EVENT_TYPE_DISPLAY_ADJUST_REQUEST = "displayAdjustRequestEvent";
        public const string CUST_EVENT_TYPE_SFX_VOLUME_CHANGED = "sfxVolumeChangedEvent";
        public const string CUST_EVENT_TYPE_MUSIC_VOLUME_CHANGED = "musicVolumeChangedEvent";
        public const string CUST_EVENT_TYPE_TUTORIAL_RESET_REQUEST = "tutorialResetRequestEvent";
        public const string CUST_EVENT_TYPE_CREDITS_REQUEST = "creditsRequestEvent";

        private const int kMinIndex = 0;
        private const int kDisplayIndex = 0;
        private const int kSoundIndex = 1;
        private const int kMusicIndex = 2;
        private const int kTutorialIndex = 3;
        private const int kCreditsIndex = 4;
        private const int kMaxIndex = 4;

        private const int kOptionsFontSize = 32;

        public OptionsView(int category)
            : base(category)
        {
            mAdvanceable = true;
            mCostume = null;
            mOptions = null;
            mEvents = OptionsView.EventsList;
            mSelectedIndex = kMinIndex;
            mNav = new UINavigator(Globals.kNavVertical);
            mNav.Repeats = true;
            mNav.RepeatDelay = 0.3f;
            SetupProp();
            UpdateOptions();
            mScene.SubscribeToInputUpdates(this);
        }

        #region Fields
        private SPSprite mCostume;
        private int mSelectedIndex;
        private List<SPTextField> mOptions;
        private List<string> mEvents;
        private UINavigator mNav;

        private NumericValueChangedEvent mSfxVolumeChangedEvent;
        private NumericValueChangedEvent mMusicVolumeChangedEvent;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_MENU_OPTIONS; } }
        public static List<string> EventsList
        {
            get
            {
                return new List<string>()
                {
                    CUST_EVENT_TYPE_DISPLAY_ADJUST_REQUEST,
                    CUST_EVENT_TYPE_SFX_VOLUME_CHANGED,
                    CUST_EVENT_TYPE_MUSIC_VOLUME_CHANGED,
                    CUST_EVENT_TYPE_TUTORIAL_RESET_REQUEST,
                    CUST_EVENT_TYPE_CREDITS_REQUEST
                };
            }
        }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mOptions != null)
                return;

            mCostume = new SPSprite();
            AddChild(mCostume);

            SPTexture ropeVertTexture = mScene.TextureByName("vert-rope");
            SPTexture ropeHorizTexture = mScene.TextureByName("horiz-rope");

            // Center vert rope
            SPImage ropeImage = new SPImage(ropeVertTexture);
            ropeImage.X = 474;
            ropeImage.Y = 132;
            mCostume.AddChild(ropeImage);

            // Left vert rope
            ropeImage = new SPImage(ropeVertTexture);
            ropeImage.X = 180;
            ropeImage.Y = 150;
            ropeImage.Rotation = SPMacros.SP_D2R(-15);
            mCostume.AddChild(ropeImage);

            // Right vert rope
            ropeImage = new SPImage(ropeVertTexture);
            ropeImage.X = 768;
            ropeImage.Y = 146;
            ropeImage.Rotation = SPMacros.SP_D2R(15);
            mCostume.AddChild(ropeImage);

            float xInterval = (80f / (float)Math.Cos(SPMacros.SP_D2R(15))) * (float)Math.Sin(SPMacros.SP_D2R(15));
            List<string> optionTitles = new List<string>() { "Display", "Sound Effects", "Menu Music", "Tutorial", "Credits" };
            List<string> optionText = new List<string>() { "Adjust", "On", "On", "Reset", "View" };
            mOptions = new List<SPTextField>();

            // Loop twice to optimize draw calls by not alternating textures between fonts and ropes.
            int i = 0;
            foreach (string title in optionTitles)
            {
                // Title ShadowTextField
                ShadowTextField shadowTextField = new ShadowTextField(Category, 160, 40, kOptionsFontSize, title, mScene.FontKey);
                shadowTextField.X = 280;
                shadowTextField.Y = 164 + i * 80;
                shadowTextField.FontColor = SPUtils.ColorFromColor(0x797ca9);
                shadowTextField.SetTextAlignment(SPTextField.SPHAlign.Right, SPTextField.SPVAlign.Center);
                mCostume.AddChild(shadowTextField);

                // Option TextField
                SPTextField optionTextField = new SPTextField(140, 40, optionText[i], mScene.FontKey, kOptionsFontSize);
                optionTextField.X = 520;
                optionTextField.Y = shadowTextField.Y;
                optionTextField.HAlign = SPTextField.SPHAlign.Center;
                optionTextField.VAlign = SPTextField.SPVAlign.Center;
                optionTextField.Color = Color.Black;
                mCostume.AddChild(optionTextField);
                mOptions.Add(optionTextField);
                mNav.AddNav(optionTextField);
                ++i;
            }

            i = 0;
            foreach (string title in optionTitles)
            {
                // Horizontal Rope
                ropeImage = new SPImage(ropeHorizTexture);
                ropeImage.X = 177 + i * xInterval;
                ropeImage.Y = 140 + (i + 1) * 80;
                ropeImage.ScaleX = (ropeImage.Width - 2 * i * xInterval) / ropeImage.Width;
                mCostume.AddChild(ropeImage);
                ++i;
            }

            // Sfx volume knobs
            SPImage volumeKnob = new SPImage(mScene.TextureByName("large_dpad_left"));
            volumeKnob.ScaleX = volumeKnob.ScaleY = 64f / volumeKnob.Width;

            SPTextField sfxTextField = mOptions[kSoundIndex];
            volumeKnob.X = sfxTextField.X - 24;
            volumeKnob.Y = sfxTextField.Y + (sfxTextField.Height - volumeKnob.Height) / 2;
            mCostume.AddChild(volumeKnob);

            volumeKnob = new SPImage(mScene.TextureByName("large_dpad_right"));
            volumeKnob.ScaleX = volumeKnob.ScaleY = 64f / volumeKnob.Width;
            volumeKnob.X = sfxTextField.X + volumeKnob.Width + 36;
            volumeKnob.Y = sfxTextField.Y + (sfxTextField.Height - volumeKnob.Height) / 2;
            mCostume.AddChild(volumeKnob);

            // Music volume knobs
            SPTextField musicTextField = mOptions[kMusicIndex];

            volumeKnob = new SPImage(mScene.TextureByName("large_dpad_left"));
            volumeKnob.ScaleX = volumeKnob.ScaleY = 64f / volumeKnob.Width;
            volumeKnob.X = musicTextField.X - 24;
            volumeKnob.Y = musicTextField.Y + (musicTextField.Height - volumeKnob.Height) / 2;
            mCostume.AddChild(volumeKnob);

            volumeKnob = new SPImage(mScene.TextureByName("large_dpad_right"));
            volumeKnob.ScaleX = volumeKnob.ScaleY = 64f / volumeKnob.Width;
            volumeKnob.X = musicTextField.X + volumeKnob.Width + 36;
            volumeKnob.Y = musicTextField.Y + (musicTextField.Height - volumeKnob.Height) / 2;
            mCostume.AddChild(volumeKnob);
        }

        private void UpdateOptions()
        {
            for (int i = kMinIndex; i <= kMaxIndex; ++i)
            {
                SPTextField optionTextField = mOptions[i];
                optionTextField.FontSize = (i == mSelectedIndex) ? kOptionsFontSize + 8 : kOptionsFontSize;
                optionTextField.Color = (i == mSelectedIndex) ? Color.Blue : Color.Black;

                if (i == kSoundIndex)
                    optionTextField.Text = GameSettings.GS.ValueForKey(GameSettings.SFX_VOLUME).ToString();
                else if (i == kMusicIndex)
                    optionTextField.Text = GameSettings.GS.ValueForKey(GameSettings.MUSIC_VOLUME).ToString();
            }
        }

        private void MovePrevOption()
        {
            --mSelectedIndex;

            if (mSelectedIndex < kMinIndex)
                mSelectedIndex = kMaxIndex;
        }

        private void MoveNextOption()
        {
            ++mSelectedIndex;

            if (mSelectedIndex > kMaxIndex)
                mSelectedIndex = kMinIndex;
        }

        private bool MoveOption(int dir)
        {
            if (dir == 1)
                MoveNextOption();
            else if (dir == -1)
                MovePrevOption();
            else
                return false;
            return true;
        }

        private void IncrementSelectedOption(int amount)
        {
            if (mSelectedIndex == kSoundIndex)
            {
                int sfxVolumeCurrent = GameSettings.GS.ValueForKey(GameSettings.SFX_VOLUME);
                int sfxVolumeNew = Math.Max(0, Math.Min(10, sfxVolumeCurrent + amount));

                if (sfxVolumeNew != sfxVolumeCurrent)
                {
                    if (mSfxVolumeChangedEvent == null)
                        mSfxVolumeChangedEvent = new NumericValueChangedEvent(CUST_EVENT_TYPE_SFX_VOLUME_CHANGED, sfxVolumeNew, sfxVolumeCurrent);
                    else
                        mSfxVolumeChangedEvent.UpdateValues(sfxVolumeNew, sfxVolumeCurrent);

                    DispatchEvent(mSfxVolumeChangedEvent as SPEvent);
                    mScene.PlaySound("Button");
                }
            }
            else if (mSelectedIndex == kMusicIndex)
            {
                int musicVolumeCurrent = GameSettings.GS.ValueForKey(GameSettings.MUSIC_VOLUME);
                int musicVolumeNew = Math.Max(0, Math.Min(10, musicVolumeCurrent + amount));

                if (musicVolumeNew != musicVolumeCurrent)
                {
                    if (mMusicVolumeChangedEvent == null)
                        mMusicVolumeChangedEvent = new NumericValueChangedEvent(CUST_EVENT_TYPE_MUSIC_VOLUME_CHANGED, musicVolumeNew, musicVolumeCurrent);
                    else
                        mMusicVolumeChangedEvent.UpdateValues(musicVolumeNew, musicVolumeCurrent);

                    DispatchEvent(mMusicVolumeChangedEvent as SPEvent);
                    mScene.PlaySound("Button");
                }
            }
        }

        private void ActivateSelectedOption()
        {
            if (mSelectedIndex == kSoundIndex || mSelectedIndex == kMusicIndex)
                return;

            DispatchEvent(SPEvent.SPEventWithType(mEvents[mSelectedIndex]));
            mScene.PlaySound("Button");
        }

        private int DidNavigate(int preNav, int postNav, int maxNav)
        {
            int navDir = 0;
            if (preNav != postNav)
            {
                if (preNav == 0 && postNav == maxNav)
                    navDir = -1;
                else if (preNav == maxNav && postNav == 0)
                    navDir = 1;
                else if (preNav < postNav)
                    navDir = 1;
                else
                    navDir = -1;
            }

            return navDir;
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            ControlsManager cm = ControlsManager.CM;
            bool didActivate = false, didNavigate = false;

#if false
            if (cm.DidButtonDepress(Buttons.DPadUp) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLUp))
            {
                MovePrevOption();
                didNavigate = true;
            }
            else if (cm.DidButtonDepress(Buttons.DPadDown) || cm.DidThumbstickActivate(InputManager.ThumbStickDir.TLDown))
            {
                MoveNextOption();
                didNavigate = true;
            }
#else
            int preNav = mNav.NavIndex;
            mNav.Update(gpState, kbState);
            didNavigate = MoveOption(DidNavigate(preNav, mNav.NavIndex, mNav.NavCount - 1));
#endif

            if (cm.DidButtonDepress(Buttons.DPadLeft))
            {
                IncrementSelectedOption(-1);
                didNavigate = true;
            }
            else if (cm.DidButtonDepress(Buttons.DPadRight))
            {
                IncrementSelectedOption(1);
                didNavigate = true;
            }

            if (cm.DidButtonDepress(Buttons.A))
            {
                didActivate = true;
                didNavigate = true;
            }

            if (didActivate)
                ActivateSelectedOption();
            if (didNavigate || didActivate)
                UpdateOptions();
        }

        public override void AdvanceTime(double time)
        {
            if (mNav != null)
            {
                int preNav = mNav.NavIndex;
                mNav.AdvanceTime(time);
                if (MoveOption(DidNavigate(preNav, mNav.NavIndex, mNav.NavCount - 1)))
                    UpdateOptions();
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
                        mScene.UnsubscribeToInputUpdates(this);
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
