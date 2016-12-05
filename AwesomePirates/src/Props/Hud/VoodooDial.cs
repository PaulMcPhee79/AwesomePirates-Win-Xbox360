using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class VoodooDial : Prop
    {
        public const string CUST_EVENT_TYPE_VOODOO_DIAL_PRESSED = "voodooDialPressedEvent";

        public VoodooDial(int category, uint key)
            : base(category)
        {
            mKey = key;
            mButton = null;
            SetupProp();
        }

        #region Fields
        private uint mKey;
        private MenuButton mButton;
        #endregion

        #region Properties
        public uint Key { get { return mKey; } }
        public MenuButton Button { get { return mButton; } }
        public bool Enabled { get { return mButton.Enabled; } set { if (mButton != null) mButton.Enabled = (mKey != 0 && value); } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mButton != null)
		        return;

            if (mKey == 0)
            {
                Enabled = false;
                return;
            }

            SPTexture iconTexture = mScene.TextureByName(Idol.IconTextureNameForKey(mKey));
	
	        // Button
            mButton = new MenuButton(null, iconTexture);
	        mButton.AlphaWhenDisabled = 0.3f;
	        mButton.ScaleWhenDown = 1.4f;
            mButton.AddActionEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, new Action<SPEvent>(OnButtonPressed));
            AddChild(mButton);

            if (mButton.SelectedEffecter != null)
                mButton.SelectedEffecter.Factor = 1.75f;
        }

        public void Highlight(bool enable)
        {
            if (mButton != null)
                mButton.Selected = enable;
        }

        private void OnButtonPressed(SPEvent ev)
        {
            DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_VOODOO_DIAL_PRESSED));
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mButton != null)
                        {
                            mButton.RemoveEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED);
                            mButton = null;
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
