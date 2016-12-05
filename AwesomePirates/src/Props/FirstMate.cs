using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class FirstMate : Prop, IInteractable
    {
        public const string CUST_EVENT_TYPE_FIRST_MATE_NEXT_MSG = "firstMateNextMsgEvent";
        public const string CUST_EVENT_TYPE_FIRST_MATE_DECISION = "firstMateDecisionEvent";
        public const string CUST_EVENT_TYPE_FIRST_MATE_RETIRED = "firstMateRetiredEvent";

        public FirstMate(int category, List<string> msgs, string textureName, int dir)
            : base(category)
        {
            mRetiring = false;
		    mDecision = false;
		    mMsgIndex = 0;
		    mUserData = 0;
        
            mTextureName = textureName;
		
            Vector2 spawn = (dir == -1) ? new Vector2(-136f, mScene.ViewHeight - 160f) : new Vector2(mScene.ViewWidth, mScene.ViewHeight - 160f);
            Vector2 dest = (dir == -1) ? new Vector2(spawn.X + 136f, spawn.Y) : new Vector2(spawn.X - 136f, spawn.Y);
        
		    X = spawn.X;
		    Y = spawn.Y;
		    Dest = dest;
		    Despawn = spawn;
		    mMsgs = new List<string>(msgs);
		    mMsgCloud = null;
		    mDir = (dir == 0) ? 1 : dir / Math.Abs(dir);
            SetupProp();

            mScene.SubscribeToInputUpdates(this);
        }

        #region Fields
        private bool mRetiring;
        private bool mDecision;

        private int mDir;
        private int mMsgIndex;
        private int mUserData;

        private string mTextureName;

        private Vector2 mDest;
        private Vector2 mDespawn;

        private List<string> mMsgs;
        private MessageCloud mMsgCloud;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_HELP; } }

        public Vector2 Dest { get { return mDest; } set { mDest = value; } }
        public Vector2 Despawn { get { return mDespawn; } set { mDespawn = value; } }
        public int UserData { get { return mUserData; } set { mUserData = value; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            SPImage image = new SPImage(mScene.TextureByName(mTextureName));
            AddChild(image);
        }

        public void BeginAnnouncements(float delay = 0f)
        {
            if (!mScene.HasInputFocus(InputFocus))
                mScene.PushFocusState(InputManager.FOCUS_STATE_PF_HELP);

            mScene.Juggler.RemoveTweensWithTarget(this);

            float duration = Math.Max(Math.Abs(X - mDest.X) / Width, Math.Abs(Y - mDest.Y) / Height);
            SPTween tween = new SPTween(this, duration / 2f);
            tween.AnimateProperty("X", mDest.X);
            tween.AnimateProperty("Y", mDest.Y);
            tween.Delay = delay;
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnArrivedAtDest);
            mScene.Juggler.AddObject(tween);
        }

        public bool AnnounceNextMessage()
        {
            if (mMsgCloud == null || mMsgIndex >= mMsgs.Count)
                return false;

            mMsgCloud.SetMessageText(mMsgs[mMsgIndex++]);

            if (mMsgIndex == mMsgs.Count)
                mMsgCloud.State = MessageCloud.MsgCloudState.Aye;
            else
                mMsgCloud.State = MessageCloud.MsgCloudState.Next;

            return true;
        }

        public void RetireToCabin()
        {
            if (mRetiring)
                return;

            if (mMsgCloud != null)
            {
                mMsgCloud.DismissOverTime(0.25f);
            }
            else
            {
                mScene.Juggler.RemoveTweensWithTarget(this);

                float duration = Math.Max(Math.Abs(X - mDespawn.X) / Width, Math.Abs(Y - mDespawn.Y) / Height);
                SPTween tween = new SPTween(this, duration / 2f);
                tween.AnimateProperty("X", mDespawn.X);
                tween.AnimateProperty("Y", mDespawn.Y);
                tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnRetiredToCabin);
                mScene.Juggler.AddObject(tween);
                mRetiring = true;
            }
        }

        public void AddMsgs(List<string> msgs)
        {
            mMsgs = new List<string>(msgs);
            mMsgIndex = 0;
            AnnounceNextMessage();
        }

        private void OnArrivedAtDest(SPEvent ev)
        {
            if (mMsgCloud != null)
                return;

            mMsgCloud = new MessageCloud(Category, 0, 0, mDir);
            mMsgCloud.Alpha = 0f;
            mMsgCloud.X = (mDir == 1) ? X - mMsgCloud.Width + 36f : X + Width - 36f;
            mMsgCloud.Y = Y - mMsgCloud.Height / 2;

            mScene.AddProp(mMsgCloud);
            AttachMessageCloudEvents();
            AnnounceNextMessage();

            SPTween tween = new SPTween(mMsgCloud, 0.25f);
            tween.AnimateProperty("Alpha", 1f);
            mScene.Juggler.AddObject(tween);
        }

        private void OnRetiredToCabin(SPEvent ev)
        {
            if (mScene.HasInputFocus(InputFocus))
                mScene.PopFocusState(InputManager.FOCUS_STATE_PF_HELP);

            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_FIRST_MATE_RETIRED));
        }

        public void OnMessageCloudNext(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_FIRST_MATE_NEXT_MSG));
            AnnounceNextMessage();
        }

        public void OnMessageCloudChoice(SPEvent ev)
        {
            mDecision = mMsgCloud.Choice;
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_FIRST_MATE_DECISION));
        }

        public void OnMessageCloudDismissed(SPEvent ev)
        {
            DetachMessageCloudEvents();
            mScene.Juggler.RemoveTweensWithTarget(mMsgCloud);
            mScene.RemoveProp(mMsgCloud);
            mMsgCloud = null;

            RetireToCabin();
        }

        private void AttachMessageCloudEvents()
        {
            mMsgCloud.AddEventListener(MessageCloud.CUST_EVENT_TYPE_MSG_CLOUD_NEXT, (SPEventHandler)OnMessageCloudNext);
            mMsgCloud.AddEventListener(MessageCloud.CUST_EVENT_TYPE_MSG_CLOUD_CHOICE, (SPEventHandler)OnMessageCloudChoice);
            mMsgCloud.AddEventListener(MessageCloud.CUST_EVENT_TYPE_MSG_CLOUD_DISMISSED, (SPEventHandler)OnMessageCloudDismissed);
        }

        private void DetachMessageCloudEvents()
        {
            mMsgCloud.RemoveEventListener(MessageCloud.CUST_EVENT_TYPE_MSG_CLOUD_NEXT, (SPEventHandler)OnMessageCloudNext);
            mMsgCloud.RemoveEventListener(MessageCloud.CUST_EVENT_TYPE_MSG_CLOUD_CHOICE, (SPEventHandler)OnMessageCloudChoice);
            mMsgCloud.RemoveEventListener(MessageCloud.CUST_EVENT_TYPE_MSG_CLOUD_DISMISSED, (SPEventHandler)OnMessageCloudDismissed);
        }

        public virtual void DidGainFocus() { }

        public void WillLoseFocus()
        {
            // Commented out to allow pause menu to show during help sequences.
            //if (!mRetiring)
            //    RetireToCabin();
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mMsgCloud != null)
                mMsgCloud.Update(gpState, kbState);
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

                        if (mMsgCloud != null)
                        {
                            DetachMessageCloudEvents();
                            mScene.Juggler.RemoveTweensWithTarget(mMsgCloud);
                            mScene.RemoveProp(mMsgCloud);
                            mMsgCloud = null;
                        }

                        mMsgs = null;
                        mTextureName = null;
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
