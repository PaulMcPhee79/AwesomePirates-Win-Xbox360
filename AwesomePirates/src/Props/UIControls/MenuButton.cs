using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class MenuButton : SPButton
    {
        // Method from string: http://stackoverflow.com/questions/540066/calling-a-function-from-a-string-in-c-sharp

        private static Action<MenuButton> s_EffecterCallback = null;

        public MenuButton(Action selector, SPTexture upState, SPTexture downState = null, bool isNavigable = true, bool useCommonEffecter = true)
            : base(upState, downState)
        {
            mActionSelector = selector;
            mSfxKey = null;
            mSfxVolume = 1;
            mIsNavigable = isNavigable;

            mSelected = false;

            if (useCommonEffecter && s_EffecterCallback != null)
                s_EffecterCallback(this);
            else
                mSelectedEffecter = null;

            mVertCount = 0;
            mVerts = null;
            ScaleWhenDown = 0.9f;
        }
        
        #region Fields
        private Action mActionSelector;
        private string mSfxKey;
        private float mSfxVolume;
        private bool mIsNavigable;

        private bool mSelected;
        private SPEffecter mSelectedEffecter;

        private int mVertCount;
        private List<Vector2> mVerts;
        #endregion

        #region Properties
        public static Action<MenuButton> EffecterSetup { set { s_EffecterCallback = value; } }
        public Action ActionSelector { get { return mActionSelector; } }
        public string SfxKey { get { return mSfxKey; } set { mSfxKey = value; } }
        public float SfxVolume { get { return mSfxVolume; } set { mSfxVolume = value; } }
        public bool IsNavigable { get { return mIsNavigable; } }
        public bool Selected
        {
            get { return mSelected; }
            set
            {
                mSelected = value;
                if (mSelectedEffecter != null)
                    Effecter = (mSelected) ? mSelectedEffecter : null;
            }
        }
        public SPEffecter SelectedEffecter
        {
            get { return mSelectedEffecter; }
            set
            {
                if (mSelectedEffecter != null && mSelectedEffecter == Effecter)
                    Effecter = null;
                mSelectedEffecter = value;

                if (mSelected && mSelectedEffecter != null)
                    Effecter = mSelectedEffecter;
            }
        }
        #endregion

        #region Methods
        public void PopulateTouchBoundsWithVerts(List<Vector2> verts)
        {
            if (mVertCount != 0)
            {
		        mVertCount = 0;
		        mVerts.Clear();
	        }
	
	        if (verts == null || verts.Count == 0)
		        return;

            mVerts.AddRange(verts);
        }

        public override void OnTouch(SPTouchEvent touchEvent)
        {
            if (!Enabled)
		        return;
	
	        if (mVertCount != 0 && !IsDown)
            {
		        SPTouch touch = touchEvent.AnyTouch(touchEvent.TouchesWithTarget(this));
                Vector2 pt = touch.LocationInSpace(this);

                if (!SPUtils.PointInPoly(pt, mVerts))
                    return;
	        }

            base.OnTouch(touchEvent);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mSfxKey = null;
                        mVertCount = 0;

                        if (mVerts != null)
                        {
                            mVerts.Clear();
                            mVerts = null;
                        }

                        mSelectedEffecter = null;
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
