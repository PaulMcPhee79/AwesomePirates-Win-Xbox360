using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class TitleSubview : MenuDetailView
    {
        public enum GuidePosition
        {
            MidLower = 0,
            MidUpper = 1,
            LeftMid = 2,
            RightMid = 3
        }

        public TitleSubview(int category, uint navMap = Globals.kNavVertical)
            : base(category, navMap)
        {
            mDoesScaleToFill = false;
            mScaleToFillThreshold = 1f;
        }

        #region Fields
        protected bool mDoesScaleToFill;
        protected float mScaleToFillThreshold;
        protected CCPoint mGamerPicPos;
        protected CCPoint mGuidePos;
        protected CCPoint mClosePosition;
        protected MethodInfo mCloseSelector;
        #endregion

        #region Properties
        public bool DoesScaleToFill { get { return mDoesScaleToFill; } set { mDoesScaleToFill = value; } }
        public float ScaleToFillThreshold { get { return mScaleToFillThreshold; } set { mScaleToFillThreshold = value; } }
        public CCPoint GamerPicPos { get { return mGamerPicPos; } set { mGamerPicPos = value; } }
        public CCPoint GuidePos { get { return mGuidePos; } set { mGuidePos = value; } }
        public CCPoint ClosePosition { get { return mClosePosition; } set { mClosePosition = value; } }
        public MethodInfo CloseSelector { get { return mCloseSelector; } set { mCloseSelector = value; } }
        #endregion

        #region Methods
        public virtual void AttachGamerPic(SPDisplayObject gamerPic)
        {
            if (gamerPic != null && GamerPicPos != null)
            {
                gamerPic.X = GamerPicPos.X;
                gamerPic.Y = GamerPicPos.Y;
                AddChild(gamerPic);
            }
        }

        public virtual void DetachGamerPic(SPDisplayObject gamerPic)
        {
            if (gamerPic != null)
                RemoveChild(gamerPic);
        }

        public static CCPoint GuidePositionForScene(GuidePosition gPos, SceneController scene)
        {
            if (scene == null)
                return null;

            CCPoint pos = null;

            switch (gPos)
            {
                case GuidePosition.MidLower:
                    pos = new CCPoint(scene.ViewWidth / 2, scene.ViewHeight - scene.GuidePropDimensions.Y / 2);
                    break;
                case GuidePosition.MidUpper:
                    pos = new CCPoint(scene.ViewWidth / 2, scene.GuidePropDimensions.Y / 2);
                    break;
                case GuidePosition.LeftMid:
                    pos = new CCPoint(scene.GuidePropDimensions.X / 2, scene.ViewHeight / 2);
                    break;
                case GuidePosition.RightMid:
                    pos = new CCPoint(scene.ViewWidth - scene.GuidePropDimensions.X / 2, scene.ViewHeight / 2);
                    break;
            }

            return pos;
        }
        #endregion
    }
}
