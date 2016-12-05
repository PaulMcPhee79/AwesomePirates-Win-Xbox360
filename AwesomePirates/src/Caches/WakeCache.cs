using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class WakeCache : CacheManager
    {
        private List<List<SPSprite>> mWakePool = null;
        private PoolIndexer mWakeIndexer = null;

        public override void FillResourcePoolForScene(SceneController scene)
        {
            if (mWakePool != null)
		        return;
            int wakeCount = 50;
            mWakePool = new List<List<SPSprite>>(wakeCount);
            mWakeIndexer = new PoolIndexer(wakeCount, "WakeCache");
            mWakeIndexer.InitIndexes(0, 1);

            SPTexture rippleTexture = scene.TextureByName("wake");
            //Random rand = GameController.GC.Random;
            //List<SPTexture> ripplesTextures = scene.TexturesStartingWith("wake_");
	        float widthCache = rippleTexture.Width, heightCache = rippleTexture.Height;
            //float widthCache = ripplesTextures[0].Width, heightCache = ripplesTextures[0].Height;

            for (int i = 0; i < wakeCount; ++i)
            {
                List<SPSprite> wake = new List<SPSprite>(Wake.kRippleCount);
                mWakePool.Add(wake);

                for (int j = 0; j < Wake.kRippleCount; ++j)
                {
                    SPSprite rippleSprite = new SPSprite();
                    rippleSprite.Visible = false;

                    SPImage rippleImage = new SPImage(rippleTexture);
                    //SPImage rippleImage = new SPImage(ripplesTextures[rand.Next(0, 3)]);
                    rippleImage.X = -widthCache / 2;
                    rippleImage.Y = -heightCache / 2;
                    rippleSprite.AddChild(rippleImage);
                    wake.Add(rippleSprite);
                }
	        }
        }

        public List<SPSprite> CheckoutRipples(int count, out int index)
        {
            if (mWakePool == null || count != Wake.kRippleCount)
            {
                index = -1;
                return null;
            }
            else if ((index = mWakeIndexer.CheckoutNextIndex()) == -1)
                return null;

            List<SPSprite> ripples = mWakePool[index];
            return ripples;
        }

        public void CheckinRipples(List<SPSprite> ripples, int index)
        {
            if (mWakeIndexer != null && ripples != null)
                mWakeIndexer.CheckinIndex(index);
        }

        public override void ReassignResourceServersToScene(SceneController scene)
        {
            // Do nothing - we don't use ResourceServers.
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mWakePool = null;
                        mWakeIndexer = null;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
    }
}
