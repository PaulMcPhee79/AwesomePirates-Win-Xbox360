using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class SharkCache : CacheManager
    {
        public const uint RESOURCE_KEY_SHARK_SWIM = 1;
        public const uint RESOURCE_KEY_SHARK_ATTACK = 2;

        public const uint RESOURCE_KEY_SHARK_RIPPLES = 1;
        public const uint RESOURCE_KEY_SHARK_RIPPLES_TWEEN = 2;

        public const uint RESOURCE_KEY_SHARK_PERSON = 1;
        public const uint RESOURCE_KEY_SHARK_BLOOD = 2;
        public const uint RESOURCE_KEY_SHARK_PERSON_TWEEN = 3;
        public const uint RESOURCE_KEY_SHARK_BLOOD_TWEEN = 4;

        public override void FillResourcePoolForScene(SceneController scene)
        {
            if (mDictPool != null)
		        return;
	        mDictPool = new Dictionary<string,List<ResourceServer>>(3);
            mDictIndexers = new Dictionary<string, PoolIndexer>(3);
	
	        // Shark
	        List<SPTexture> swimFrames = scene.TexturesStartingWith("shark_");
	        List<SPTexture> attackFrames = scene.TexturesStartingWith("shark-attack_");
	
	        string key = "Shark";
            int poolCount = 15;
            List<ResourceServer> poolArray = new List<ResourceServer>(poolCount);
            mDictPool.Add(key, poolArray);

            PoolIndexer poolIndexer = new PoolIndexer(poolCount, "SharkCache");
            poolIndexer.InitIndexes(0, 1);
            mDictIndexers.Add(key, poolIndexer);

            for (int i = 0; i < poolCount; ++i)
            {
                ResourceServer resources = new ResourceServer(0, key);
		        SPMovieClip swimClip = new SPMovieClip(swimFrames, Shark.SwimFps);
		        swimClip.Loop = true;
		        swimClip.X = -swimClip.Width / 2;
		        swimClip.Y = -swimClip.Height / 2;
                resources.AddMovie(swimClip, RESOURCE_KEY_SHARK_SWIM);
		
		        SPMovieClip attackClip = new SPMovieClip(attackFrames, Shark.AttackFps);
		        attackClip.Loop = false;
		        attackClip.X = -attackClip.Width / 2;
		        attackClip.Y = -attackClip.Height / 2;
                attackClip.AddActionEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, new Action<SPEvent>(resources.OnMovieCompleted));
                //attackClip.AddEventListener(SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, (SPEventHandler)resources.OnMovieCompleted);
                resources.AddMovie(attackClip, RESOURCE_KEY_SHARK_ATTACK);

                poolArray.Add(resources);
	        }
	
	        // Shark Water
	        SPTexture waterRingTexture = scene.TextureByName("shark-white-water");
	
	        key = "SharkWater";
            poolCount = 15;
            poolArray = new List<ResourceServer>(poolCount);
            mDictPool.Add(key, poolArray);

            poolIndexer = new PoolIndexer(poolCount, "SharkWaterCache");
            poolIndexer.InitIndexes(0, 1);
            mDictIndexers.Add(key, poolIndexer);
	
	        int numRipples = SharkWater.NumRipples;
            float waterRingDuration = SharkWater.WaterRingDuration;

            for (int i = 0; i < poolCount; ++i)
            {
                ResourceServer resources = new ResourceServer(0, key);
		        List<SPSprite> ripples = new List<SPSprite>(numRipples);
		        float delay = 0f;
        
		        for (int j = 0; j < numRipples; ++j)
                {
			        SPSprite sprite = new SPSprite();
			        SPImage image = new SPImage(waterRingTexture);
			        image.X = -image.Width / 2;
			        image.Y = -image.Height / 2;
			        sprite.ScaleX = 0.01f;
			        sprite.ScaleY = 0.01f;
                    sprite.AddChild(image);
                    ripples.Add(sprite);
            
                    SPTween tween = new SPTween(sprite, waterRingDuration);
                    tween.AnimateProperty("Alpha", 0f);
                    tween.AnimateProperty("ScaleX", 1f);
                    tween.AnimateProperty("ScaleY", 1f);
                    tween.Delay = delay;
            
                    if (j == numRipples-1)
                        tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                        //tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                    resources.AddTween(tween, RESOURCE_KEY_SHARK_RIPPLES_TWEEN + (uint)j);
                    delay += 0.5f;
		        }

                resources.AddMiscResource(ripples, RESOURCE_KEY_SHARK_RIPPLES);
		        poolArray.Add(resources);
	        }

	
	        // Person Overboard
	        List<SPTexture> overboardFrames = scene.TexturesStartingWith("overboard_");
	        SPTexture bloodTexture = scene.TextureByName("blood");
	
	        key = "Overboard";
            poolCount = 35;
            poolArray = new List<ResourceServer>(poolCount);
            mDictPool.Add(key, poolArray);

            poolIndexer = new PoolIndexer(poolCount, "OverboardCache");
            poolIndexer.InitIndexes(0, 1);
            mDictIndexers.Add(key, poolIndexer);

            for (int i = 0; i < poolCount; ++i)
            {
                ResourceServer resources = new ResourceServer(0, key);
        
                // Person
		        SPMovieClip personClip = new SPMovieClip(overboardFrames, OverboardActor.Fps);
		        personClip.X = -personClip.Width / 2;
		        personClip.Y = -personClip.Height / 2;
		        personClip.Loop = true;
                resources.AddMovie(personClip, RESOURCE_KEY_SHARK_PERSON);
		
                SPTween tween = new SPTween(personClip, 0.5);
                tween.AnimateProperty("Alpha", 0f);
                tween.AnimateProperty("ScaleX", 0.7f);
                tween.AnimateProperty("ScaleY", 0.7f);
                tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                //tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                resources.AddTween(tween, RESOURCE_KEY_SHARK_PERSON_TWEEN);
        
                // Blood
		        SPImage bloodImage = new SPImage(bloodTexture);
		        bloodImage.X = -bloodImage.Width / 2;
		        bloodImage.Y = -bloodImage.Height / 2;
                //bloodImage.Effecter = new SPEffecter(scene.EffectForKey("Refraction"), GameController.GC.BloodDraw);
		
		        SPSprite bloodSprite = new SPSprite();
                bloodSprite.AddChild(bloodImage);
                resources.AddDisplayObject(bloodSprite, RESOURCE_KEY_SHARK_BLOOD);
        
                tween = new SPTween(bloodSprite, 5f);
                tween.AnimateProperty("Alpha", 0f);
                tween.AnimateProperty("ScaleX", 2f);
                tween.AnimateProperty("ScaleY", 2f);
                tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(resources.OnTweenCompleted));
                //tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)resources.OnTweenCompleted);
                resources.AddTween(tween, RESOURCE_KEY_SHARK_BLOOD_TWEEN);

		        poolArray.Add(resources);
	        }
	
	        // Cache plank textures in TextureManager
	        Dictionary<string, object> prisoners = ShipFactory.Factory.AllPrisoners;
	
            foreach (KeyValuePair<string, object> kvp in prisoners)
            {
		        Dictionary<string, object> prisonerDetails = kvp.Value as Dictionary<string, object>;
		        string textureName = prisonerDetails["textureName"] as string;
                scene.TextureByName(textureName);
	        }
        }
    }
}
