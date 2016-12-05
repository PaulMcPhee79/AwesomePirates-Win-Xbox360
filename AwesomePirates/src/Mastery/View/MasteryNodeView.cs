using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class MasteryNodeView : Prop
    {
        public MasteryNodeView(int category, uint key)
            : base(category)
        {
            mKey = key;
            mCostume = null;
            SetupProp();
            State = MasteryNode.NodeState.Inactive;
        }

        #region Fields
        private uint mKey;
        private MasteryNode.NodeState mState;
        private SPImage mHighlightImage;
        private SPSprite mGlowSprite;
        private SPImage mBgActiveImage;
        private SPImage mBgInactiveImage;
        private SPImage mBgMaxedImage;
        private SPImage mIconImage;
        private SPSprite mCostume;
        #endregion

        #region Properties
        public uint Key { get { return mKey; } }
        public MasteryNode.NodeState State
        {
            get { return mState; }
            set
            {
                if (value == MasteryNode.NodeState.Inactive)
                {
                    mBgActiveImage.Visible = false;
                    mBgInactiveImage.Visible = true;
                    mBgMaxedImage.Visible = false;
                }
                else if (value == MasteryNode.NodeState.Maxed)
                {
                    mBgActiveImage.Visible = false;
                    mBgInactiveImage.Visible = false;
                    mBgMaxedImage.Visible = true;
                }
                else
                {
                    mBgActiveImage.Visible = true;
                    mBgInactiveImage.Visible = false;
                    mBgMaxedImage.Visible = false;
                }

                mState = value;
            }
        }
        public float IconWidth { get { return (mBgActiveImage != null) ? mBgActiveImage.BoundsInSpace(mCostume.Parent).Width : 1; } }
        public float IconHeight { get { return (mBgActiveImage != null) ? mBgActiveImage.BoundsInSpace(mCostume.Parent).Height : 1; } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mCostume != null)
                return;

            mCostume = new SPSprite();
            AddChild(mCostume);

            SPSprite scaleContainer = new SPSprite();

            // Inactive BG
            SPTexture texture = mScene.TextureByName(MasteryGuiHelper.TextureNameForNodeInactiveBg(mKey));
            mBgInactiveImage = new SPImage(texture);
            scaleContainer.AddChild(mBgInactiveImage);

            // Active BG
            texture = mScene.TextureByName(MasteryGuiHelper.TextureNameForNodeActiveBg(mKey));
            mBgActiveImage = new SPImage(texture);
            scaleContainer.AddChild(mBgActiveImage);

            // Maxed BG
            texture = mScene.TextureByName(MasteryGuiHelper.TextureNameForNodeMaxedBg(mKey));
            mBgMaxedImage = new SPImage(texture);
            scaleContainer.AddChild(mBgMaxedImage);

            // Icon
            texture = mScene.TextureByName(MasteryGuiHelper.TextureNameForNodeIcon(mKey));
            mIconImage = new SPImage(texture);
            mIconImage.X = (mBgActiveImage.Width - mIconImage.Width) / 2;
            mIconImage.Y = (mBgActiveImage.Height - mIconImage.Height) / 2;
            scaleContainer.AddChild(mIconImage);

            // Highlight
            texture = mScene.TextureByName(MasteryGuiHelper.TextureNameForNodeHighlight(mKey));
            mHighlightImage = new SPImage(texture);
            mHighlightImage.X = (mBgActiveImage.Width - mHighlightImage.Width) / 2;
            mHighlightImage.Y = (mBgActiveImage.Height - mHighlightImage.Height) / 2;
            scaleContainer.AddChildAtIndex(mHighlightImage, 0);

            // Glow
            texture = mScene.TextureByName(MasteryGuiHelper.TextureNameForNodeGlow(mKey));
            SPImage glowImage = new SPImage(texture);
            glowImage.X = -glowImage.Width / 2;
            glowImage.Y = -glowImage.Height / 2;

            mGlowSprite = new SPSprite();
            mGlowSprite.X = glowImage.Width / 2 + (mBgActiveImage.Width - glowImage.Width) / 2;
            mGlowSprite.Y = glowImage.Height / 2 + (mBgActiveImage.Height - glowImage.Height) / 2;
            mGlowSprite.ScaleX = mGlowSprite.ScaleY = 1.1f;
            mGlowSprite.AddChild(glowImage);
            scaleContainer.AddChildAtIndex(mGlowSprite, 0);

            scaleContainer.X = -mBgActiveImage.Width / 2;
            scaleContainer.Y = -mBgActiveImage.Height / 2;
            mCostume.AddChild(scaleContainer);

            //mCostume.X = -scaleContainer.X;
            //mCostume.Y = -scaleContainer.Y;
            mCostume.ScaleX = mCostume.ScaleY = 64f / mBgActiveImage.Height; // Based off of a 64x64 blueprint.

            mHighlightImage.Visible = false;
            mGlowSprite.Visible = false;

            PulseGlowSpriteOverTime(0.75f);
        }

        private void PulseGlowSpriteOverTime(float duration)
        {
            if (mGlowSprite == null)
                return;

            mScene.Juggler.RemoveTweensWithTarget(mGlowSprite);

            SPTween tween = new SPTween(mGlowSprite, duration);
            tween.AnimateProperty("Alpha", 0.5f);
            tween.Loop = SPLoopType.Reverse;
            mScene.Juggler.AddObject(tween);
        }

        public void EnableHighlight(bool enable)
        {
            mHighlightImage.Visible = enable;
            mGlowSprite.Visible = enable;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mGlowSprite != null)
                        {
                            mScene.Juggler.RemoveTweensWithTarget(mGlowSprite);
                            mGlowSprite = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // Do nothing
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
