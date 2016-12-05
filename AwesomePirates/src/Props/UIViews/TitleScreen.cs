using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class TitleScreen : TitleSubview, IInteractable
    {
        public const string CUST_EVENT_TYPE_TITLE_SCREEN_BEGIN = "titleScreenBeginEvent";

        public TitleScreen(int category)
            : base(category)
        {
            mSplashShowing = true;
            SetupProp();
            mScene.SubscribeToInputUpdates(this);
        }

        #region Fields
        private bool mSplashShowing;
        private SPImage mBeginPrompt;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_TITLE; } }
        public bool IsSplashShowing { get { return mSplashShowing; } set { mSplashShowing = value; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            base.SetupProp();

            SPImage bgImage = new SPImage(mScene.TextureByName("title"));

            // Scale and position the image based on the screen resolution, but maintain image's aspect ratio.
            float screenWidth = mScene.ViewWidth, screenHeight = mScene.ViewHeight; // ResManager.RESM.Width, screenHeight = ResManager.RESM.Height;
            float bgWidth = bgImage.Width, bgHeight = bgImage.Height;
            float scaleTo = Math.Max(screenWidth / bgWidth, screenHeight / bgHeight);

            bgImage.Scale = new Vector2(scaleTo, scaleTo);
            bgImage.X = -(bgImage.Width - screenWidth) / 2;
            bgImage.Y = -(bgImage.Height - screenHeight) / 2;
            AddChild(bgImage);

            // Title sprite
            SPSprite sprite = new SPSprite();
            SPImage logoImage = new SPImage(mScene.TextureByName("logo"));
            sprite.AddChild(logoImage);

            mBeginPrompt = new SPImage(mScene.TextureByName("begin-prompt"));
            mBeginPrompt.X = 16 + logoImage.X + (logoImage.Width - mBeginPrompt.Width) / 2;
            mBeginPrompt.Y = logoImage.Y + logoImage.Height + 64;
            sprite.AddChild(mBeginPrompt);

            SPTween promptTween = new SPTween(mBeginPrompt, 0.75f);
            promptTween.AnimateProperty("Alpha", 0);
            promptTween.Loop = SPLoopType.Reverse;
            mScene.Juggler.AddObject(promptTween);

            sprite.X = (screenWidth - sprite.Width) / 2;
            sprite.Y = (screenHeight - sprite.Height) / 2;
            AddChild(sprite);

            /*
            float hudScale = ((screenHeight > bgHeight) ? bgHeight / screenHeight : screenHeight / bgHeight) * scaleTo;

            // Make sure logo sprite fits on all screens
            if (hudScale * sprite.Height > screenHeight)
                hudScale = screenHeight * 0.75f;
            sprite.Pivot = new Vector2(sprite.Width / 2, sprite.Height / 2);
            sprite.X = sprite.PivotX + (screenWidth - sprite.Width) / 2;
            //sprite.Y = sprite.PivotY * hudScale + 100 * (screenHeight / sprite.Height);
            sprite.Y = sprite.PivotY + (screenHeight - sprite.Height) / 2;
            sprite.Scale = new Vector2(hudScale, hudScale);
            AddChild(sprite);
            */
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public override void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mSplashShowing)
                return;

            ControlsManager cm = ControlsManager.CM;
            for (PlayerIndex index = PlayerIndex.One; index <= PlayerIndex.Four; ++index)
            {
                if (cm.DidButtonRelease(Microsoft.Xna.Framework.Input.Buttons.A, index))
                {
                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_TITLE_SCREEN_BEGIN));
                    break;
                }
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

                        if (mBeginPrompt != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mBeginPrompt);
                            mBeginPrompt = null;
                        }
                    }
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
