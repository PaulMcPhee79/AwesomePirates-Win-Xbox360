using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class BlastCache : CacheManager
    {
        public const uint RESOURCE_KEY_BP_COSTUME = 1;
        public const uint RESOURCE_KEY_BP_BLAST_TWEEN = 2;
        public const uint RESOURCE_KEY_BP_AFTERMATH_TWEEN = 3;

        public override void FillResourcePoolForScene(SceneController scene)
        {
            if (mDictPool != null)
		        return;
	        mDictPool = new Dictionary<string,List<ResourceServer>>(1);
            mDictIndexers = new Dictionary<string, PoolIndexer>(1);
	
	        SPTexture abyssalTexture = scene.TextureByName("abyssal-surge");
	
            float blastAnimationDuration = BlastProp.BlastAnimationDuration;
            float blastTweenAlphaTo = 1;
    
            float aftermathAnimationDuration = BlastProp.AftermathAnimationDuration;
            float aftermathTweenAlphaTo = 0;
    
	        List<string> keys = new List<string>() { "Abyssal" };
            List<SPTexture> textures = new List<SPTexture>() { abyssalTexture };
            List<int> counts = new List<int>() { 15 };

            if (!(keys.Count == textures.Count && keys.Count == counts.Count))
                throw new InvalidOperationException("Invalid BlastCache settings.");
	
	        for (int i = 0; i < keys.Count; ++i)
            {
		        int count = counts[i];
		        List<ResourceServer> poolArray = new List<ResourceServer>(count);
                PoolIndexer poolIndexer = new PoolIndexer(count, "BlastCache");
                poolIndexer.InitIndexes(0, 1);
		        string key = keys[i];
		        SPTexture texture = textures[i];
		
		        for (int j = 0; j < count; ++j)
                {
                    ResourceServer resources = new ResourceServer(0, key);

                    SPImage blastImage = new SPImage(texture);
                    blastImage.X = -blastImage.Width / 2;
                    blastImage.Y = -blastImage.Height / 2;
            
                    SPSprite costume = new SPSprite();
                    costume.AddChild(blastImage);
                    resources.AddDisplayObject(costume, RESOURCE_KEY_BP_COSTUME);

                    SPTween blastTween = new SPTween(costume, blastAnimationDuration);
                    blastTween.AnimateProperty("Alpha", blastTweenAlphaTo);
                    blastTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                    //blastTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                    resources.AddTween(blastTween, RESOURCE_KEY_BP_BLAST_TWEEN);
            
                    SPTween aftermathTween = new SPTween(costume, aftermathAnimationDuration);
                    aftermathTween.AnimateProperty("Alpha", aftermathTweenAlphaTo);
                    aftermathTween.Delay = blastTween.Delay + blastTween.TotalTime;
                    aftermathTween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                    //aftermathTween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                    resources.AddTween(aftermathTween, RESOURCE_KEY_BP_AFTERMATH_TWEEN);
            
                    poolArray.Add(resources);
		        }
		
                mDictPool.Add(key, poolArray);
                mDictIndexers.Add(key, poolIndexer);
	        }
        }
    }
}
