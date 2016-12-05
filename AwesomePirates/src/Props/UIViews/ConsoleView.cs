using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class ConsoleView : Prop, IInteractable
    {
        private struct ConsoleCommand
        {
            internal ConsoleCommand(string msg, Color? color, Action<string, Color?> command)
            {
                mMsg = msg;
                mColor = color;
                mCommand = command;
            }

            private string mMsg;
            private Color? mColor;
            private Action<string, Color?> mCommand;

            internal string Msg { get { return mMsg; } }
            internal Color? Color { get { return mColor; } }
            internal Action<string, Color?> Command { get { return mCommand; } }
        }

        private static string s_newLine = "\n";
        private static string[] s_newLineArray = new string[] { s_newLine };

        public ConsoleView(int category, int capacity, int visibleCells, float width, float height)
            : base(category)
        {
            mAdvanceable = true;
            mCapacity = Math.Max(1, capacity);
            mWidth = width;
            mHeight = height;
            mCellHeight = mHeight / Math.Max(1, visibleCells);
            mCommandQueue = new Queue<ConsoleCommand>(10);
            SetupProp();

            mScene.SubscribeToInputUpdates(this);
        }

        #region Fields
        private readonly object SYNC = new object();
        private bool mIsShowing = false;
        private int mCapacity;
        private int mAnnotationCount = 1;
        private float mCellHeight;
        private float mWidth;
        private float mHeight;

        private PlayerIndex mPlayerIndex = PlayerIndex.One;

        private SPTextField mCurrentLine;
        private TableView mTableView;
        private Queue<ConsoleCommand> mCommandQueue;
        #endregion

        #region Properties
        public virtual uint InputFocus { get { return InputManager.HAS_FOCUS_DEBUG_CONSOLE; } }
        public bool IsShowing { get { return mIsShowing; } set { mIsShowing = value; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mTableView != null)
                return;

            SPQuad quad = new SPQuad(mWidth, mHeight);
            quad.Color = Color.Black;
            AddChild(quad);

            mTableView = new TableView(0, mWidth, mHeight);
            mTableView.InputFocus = InputFocus;
            AddChild(mTableView);

            X = (mScene.ViewWidth - mWidth) / 2;
            Y = -mHeight;
            Visible = false;
        }

        private SPSprite CreateLine(string msg, Color? color = null)
        {
            SPSprite sprite = new SPSprite();
            SPTextField textField = new SPTextField(0.95f * mWidth, mCellHeight, msg, SceneController.LeaderboardFontKey, (int)(0.8f * mCellHeight));
            textField.Color = color.HasValue ? color.Value : Color.White;
            textField.X = 0.025f * mWidth;
            textField.HAlign = SPTextField.SPHAlign.Left;
            textField.VAlign = SPTextField.SPVAlign.Bottom;
            sprite.AddChild(textField);

            return sprite;
        }

        private void AddCell(SPSprite cell)
        {
            if (cell == null)
                return;

            while (mTableView.NumCells >= mCapacity)
                RemoveCellAtIndex(0);
            mTableView.AddCell(cell);
        }

        private void RemoveCellAtIndex(int index)
        {
            mTableView.RemoveCellAtIndex(index);
        }

        private void AnnotateMsg(string msg, Color? color = null)
        {
            if (mCurrentLine == null)
                return;
            ++mAnnotationCount;
            mCurrentLine.Text = msg + " x" + mAnnotationCount.ToString();
        }

        private void WriteMsg(string msg, Color? color = null)
        {
            if (mCurrentLine == null)
            {
                SPSprite sprite = CreateLine(msg, color);
                mCurrentLine = sprite.ChildAtIndex(0) as SPTextField;
                AddCell(sprite);
            }
            else
                mCurrentLine.Text += msg;
        }

        private void WriteLineMsg(string msg, Color? color = null)
        {
            mCurrentLine = null;
            SPSprite sprite = CreateLine(msg, color);
            AddCell(sprite);
        }

        private void WriteP(string msg, Color? color = null)
        {
            if (msg == null)
                return;

            bool atEnd = mTableView.AtEnd;
            mTableView.BeginBatchAdd();

            if (msg.Length == 1 && mCurrentLine != null && mCurrentLine.Text.Length > 0 && msg[0] == mCurrentLine.Text[0])
            {
                AnnotateMsg(msg, color);
            }
            else
            {
                string[] lines = msg.Split(s_newLineArray, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; ++i)
                {
                    if (i == 0 || i == lines.Length - 1)
                        WriteMsg(lines[i], color);
                    else
                        WriteLineMsg(lines[i], color);
                }

                mAnnotationCount = 1;
            }

            mTableView.EndBatchAdd();
            mTableView.UpdateViewport();

            if (atEnd)
                mTableView.ScrollToEnd();
        }

        private void WriteLineP(string msg, Color? color = null)
        {
            if (msg == null)
                return;

            bool atEnd = mTableView.AtEnd;
            mTableView.BeginBatchAdd();

            string[] lines = msg.Split(s_newLineArray, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; ++i)
                WriteLineMsg(lines[i], color);

            mTableView.EndBatchAdd();
            mTableView.UpdateViewport();

            if (atEnd)
                mTableView.ScrollToEnd();
            mAnnotationCount = 1;
        }

        public void Write(string msg, Color? color = null)
        {
            lock (SYNC)
            {
                mCommandQueue.Enqueue(new ConsoleCommand(msg, color, new Action<string,Color?>(WriteP)));
            }
        }

        public void WriteLine(string msg, Color? color = null)
        {
            lock (SYNC)
            {
                mCommandQueue.Enqueue(new ConsoleCommand(msg, color, new Action<string, Color?>(WriteLineP)));
            }
        }

        public void Show()
        {
            if (IsShowing)
                return;

            mScene.Juggler.RemoveTweensWithTarget(this);

            SPTween tween = new SPTween(this, 0.5f * (Math.Abs(Y) / mHeight));
            tween.AnimateProperty("Y", 0);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnShown);
            mScene.Juggler.AddObject(tween);
            Visible = mIsShowing = true;

            mScene.PushFocusState(InputManager.FOCUS_STATE_SYS_DEBUG);

            if (ControlsManager.CM.PrevQueryPlayerIndex.HasValue)
                mPlayerIndex = ControlsManager.CM.PrevQueryPlayerIndex.Value;
            else
                mPlayerIndex = PlayerIndex.One;
        }

        public void Hide()
        {
            if (!IsShowing)
                return;

            mScene.Juggler.RemoveTweensWithTarget(this);

            SPTween tween = new SPTween(this, 0.5f - 0.5f * (Math.Abs(Y) / mHeight));
            tween.AnimateProperty("Y", -mHeight);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnHidden);
            mScene.Juggler.AddObject(tween);
            mIsShowing = false;
        }

        private void OnShown(SPEvent ev)
        {
            mTableView.ScrollToEnd();
            mTableView.UpdateViewport();
        }

        private void OnHidden(SPEvent ev)
        {
            mTableView.UpdateViewport();
            Visible = false;
            mScene.PopFocusState(InputManager.FOCUS_STATE_SYS_DEBUG);
        }

        public void DidGainFocus() { }

        public virtual void WillLoseFocus() { }

        public virtual void Update(GamePadState gpState, KeyboardState kbState)
        {
            ControlsManager cm = ControlsManager.CM;
            if (cm.DidButtonRelease(Buttons.Y, mPlayerIndex))
                Hide();
            
            if (mTableView != null)
            {
                if (cm.DidButtonRelease(Buttons.LeftShoulder, mPlayerIndex))
                    mTableView.Scroll(mHeight);
                else if (cm.DidButtonRelease(Buttons.RightShoulder, mPlayerIndex))
                    mTableView.Scroll(-mHeight);

                mTableView.Update(cm.GamePadStateForPlayer(mPlayerIndex), kbState);
            }
        }

        public override void AdvanceTime(double time)
        {
            lock (SYNC)
            {
                foreach (ConsoleCommand command in mCommandQueue)
                    command.Command(command.Msg, command.Color);
                mCommandQueue.Clear();
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
