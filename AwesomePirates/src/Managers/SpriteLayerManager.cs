using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class SpriteLayerManager
    {
        public SpriteLayerManager(SPDisplayObjectContainer baseLayer, uint layerCount)
        {
            mBase = baseLayer;
            uint count = Math.Max(1, layerCount);

            for (uint i = 0; i < count; ++i)
                mBase.AddChild(new SPSprite());
        }

        #region Fields
        protected SPDisplayObjectContainer mBase;
        #endregion

        #region Methods
        public void AddChild(SPDisplayObject child, int category)
        {
            if (category < mBase.NumChildren)
                (mBase.ChildAtIndex(category) as SPSprite).AddChild(child);
        }

        public void RemoveChild(SPDisplayObject child, int category)
        {
            if (category < mBase.NumChildren)
                (mBase.ChildAtIndex(category) as SPSprite).RemoveChild(child);
        }

        public void SetTouchableLayers(List<int> layers)
        {
            int numChildren = mBase.NumChildren;

            for (int i = 0; i < numChildren; ++i)
                mBase.ChildAtIndex(i).Touchable = false;

            foreach (int layerIndex in layers)
            {
                if (layerIndex < numChildren)
                    mBase.ChildAtIndex(layerIndex).Touchable = true;
            }
        }

        public SPDisplayObject ChildAtCategory(int category)
        {
            SPDisplayObject child = null;

            if (category < mBase.NumChildren)
                child = mBase.ChildAtIndex(category);

            return child;
        }

        public void ClearAllLayers()
        {
            for (int i = 0; i < mBase.NumChildren; ++i)
            {
                SPSprite layer = mBase.ChildAtIndex(i) as SPSprite;
                layer.RemoveAllChildren();
            }
        }

        public void ClearAll()
        {
            mBase.RemoveAllChildren();
        }

        public void FlipChild(bool enable, int category, float width)
        {
            if (category < mBase.NumChildren)
            {
                SPDisplayObject child = mBase.ChildAtIndex(category);

                if (enable)
                {
                    child.ScaleX = -1f;
                    child.X = width;
                }
                else
                {
                    child.ScaleX = 1;
                    child.X = 0;
                }
            }
        }
        #endregion
    }
}
