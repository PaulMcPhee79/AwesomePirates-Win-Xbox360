using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class LootPropCache : CacheManager
    {
        public const uint RESOURCE_KEY_LP_COSTUME = 1;
        public const uint RESOURCE_KEY_LP_WARDROBE = 2;
        public const uint RESOURCE_KEY_LP_ALPHA_TWEEN = 3;
        public const uint RESOURCE_KEY_LP_SCALE_TWEEN = 4;

        public override void FillResourcePoolForScene(SceneController scene)
        {
            if (mDictPool != null)
		        return;
	        mDictPool = new Dictionary<string,List<ResourceServer>>(3);
            mDictIndexers = new Dictionary<string, PoolIndexer>(3);
	
	        SPTexture prisonerTexture = scene.TextureByName("pirate-hat");
	
            float lootAnimationDuration = LootProp.LootAnimationDuration;
            float tweenAlphaTo = 0;
            float tweenScaleTo = 1.25f;
    
	        List<string> keys = new List<string>() { "Prisoner" };
	        List<SPTexture> textures = new List<SPTexture>() { prisonerTexture };
            List<int> counts = new List<int>() { 5 };

            if (!(keys.Count == textures.Count && keys.Count == counts.Count))
                throw new InvalidOperationException("Invalid LootPropCache settings.");
	
	        for (int i = 0; i < keys.Count; ++i)
            {
		        int count = counts[i];
		        List<ResourceServer> poolArray = new List<ResourceServer>(count);
                PoolIndexer poolIndexer = new PoolIndexer(count, "LootPropCache");
                poolIndexer.InitIndexes(0, 1);
		        string key = keys[i];
		        SPTexture texture = textures[i];
		
		        for (int j = 0; j < count; ++j)
                {
                    ResourceServer resources = new ResourceServer(0, key);
            
			        SPImage costume = new SPImage(texture);
			        costume.X = -costume.Width / 2;
			        costume.Y = -costume.Height / 2;
                    resources.AddDisplayObject(costume, RESOURCE_KEY_LP_COSTUME);
            
                    SPSprite wardrobe = new SPSprite();
                    resources.AddDisplayObject(wardrobe, RESOURCE_KEY_LP_WARDROBE);
            
                    SPTween tween = new SPTween(wardrobe, lootAnimationDuration * 0.9f, SPTransitions.SPEaseIn);
                    tween.AnimateProperty("Alpha", tweenAlphaTo);
                    resources.AddTween(tween, RESOURCE_KEY_LP_ALPHA_TWEEN);
            
                    tween = new SPTween(wardrobe, lootAnimationDuration);
                    tween.AnimateProperty("ScaleX", tweenScaleTo);
                    tween.AnimateProperty("ScaleY", tweenScaleTo);
                    tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                    //tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                    resources.AddTween(tween, RESOURCE_KEY_LP_SCALE_TWEEN);
    
                    poolArray.Add(resources);
		        }

                mDictPool.Add(key, poolArray);
                mDictIndexers.Add(key, poolIndexer);
	        }
        }
    }
}
