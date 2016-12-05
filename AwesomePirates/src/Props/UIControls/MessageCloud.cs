using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class MessageCloud : Prop
    {
        public enum MsgCloudState
        {
            None = 0,
            Next,
            Aye,
            Closing
        }

        public const string CUST_EVENT_TYPE_MSG_CLOUD_NEXT = "msgCloudNextEvent";
        public const string CUST_EVENT_TYPE_MSG_CLOUD_CHOICE = "msgCloudChoiceEvent";
        public const string CUST_EVENT_TYPE_MSG_CLOUD_DISMISSED = "msgCloudDismissedEvent";

        private const float kCloudScale = 1.2f;
        private const float kMessageWidth = 300f * kCloudScale;
        private const float kMessageHeight = 136f;
        private const int kMessageFontSize = 24;

        public MessageCloud(int category, float x, float y, int dir)
            : base(category)
        {
		    mState = MsgCloudState.None;
		    mChoice = false;
		    X = x;
		    Y = y;
		    mDir = dir;
            mCloudImage = null;
		    mText = null;
		    mAyeButton = null;
            SetupProp();
        }

        #region Fields
        private MsgCloudState mState;
        private int mDir;
        private bool mChoice;
        private SPImage mCloudImage;
        private SPTextField mText;
        private MenuButton mAyeButton;
        #endregion

        #region Properties
        public MsgCloudState State
        {
            get { return mState; }
            set
            {
                switch (value)
                {
                    case MsgCloudState.Next:
                    case MsgCloudState.Aye:
                        {
                            if (mAyeButton == null)
                            {
                                mAyeButton = new MenuButton(null, mScene.TextureByName("msg-aye"));
                                mAyeButton.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, (SPEventHandler)OnCloudButtonTriggered);
                            }

                            mAyeButton.X = (mCloudImage.Width - mAyeButton.Width) / 2;
                            mAyeButton.Y = 0.925f * mCloudImage.Height - mAyeButton.Height;
                            mAyeButton.Selected = true;
                            AddChild(mAyeButton);
                        }
                        break;
                    case MsgCloudState.None:
                    case MsgCloudState.Closing:
                        if (mAyeButton != null)
                        {
                            mAyeButton.Selected = false;
                            RemoveChild(mAyeButton);
                        }
                        break;
                }

                mState = value;
            }
        }
        public bool Choice { get { return mChoice; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCloudImage != null)
                return;

            mCloudImage = new SPImage(mScene.TextureByName("speech-bubble"));
            mCloudImage.ScaleX = kCloudScale;
            AddChild(mCloudImage);

            mText = new SPTextField(kMessageWidth, kMessageHeight, "", mScene.FontKey, kMessageFontSize);
            mText.HAlign = SPTextField.SPHAlign.Center;
            mText.VAlign = SPTextField.SPVAlign.Center;
            mText.X = 64f;
            mText.Y = 40f;
            mText.Color = Color.Black;
            AddChild(mText);

            if (mDir == -1)
            {
                mCloudImage.ScaleX = -kCloudScale;
                mCloudImage.X += mCloudImage.Width;
            }
        }

        public void SetMessageText(string text)
        {
            if (mText != null)
                mText.Text = text;
        }

        public void DismissOverTime(float duration)
        {
            if (mState == MsgCloudState.Closing)
                return;
            State = MsgCloudState.Closing;

            SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("Alpha", 0);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnDismissedComplete);
            mScene.Juggler.AddObject(tween);
        }

        public void DismissInstantly()
        {
            mScene.Juggler.RemoveTweensWithTarget(this);
            mScene.RemoveProp(this);
        }

        private void OnCloudButtonTriggered(SPEvent ev)
        {
            mScene.PlaySound("Button");

            switch (mState)
            {
                case MsgCloudState.Next:
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MSG_CLOUD_NEXT));
                    break;
                case MsgCloudState.Aye:
                    mChoice = true;
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MSG_CLOUD_CHOICE));
                    break;
                case MsgCloudState.None:
                case MsgCloudState.Closing:
                    break;
            }
        }

        private void OnDismissedComplete(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_MSG_CLOUD_DISMISSED));
            mScene.RemoveProp(this);
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mAyeButton != null && (mState == MsgCloudState.Aye || mState == MsgCloudState.Next))
            {
                ControlsManager cm = ControlsManager.CM;

                if (cm.DidButtonDepress(Buttons.A))
                    mAyeButton.AutomatedButtonDepress();
                else if (cm.DidButtonRelease(Buttons.A))
                    mAyeButton.AutomatedButtonRelease();
            }
        }
        #endregion
    }
}
