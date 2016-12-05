//#define COMBAT_TEXT_DEBUG

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class CombatText : Prop
    {
        public enum CTColorType
        {
            NonCrit = 0,
            CannonCrit,
            SharkCrit,
            RedTeam,
            BlueTeam,
            GreenTeam,
            YellowTeam
        }

        protected const int kOffsetMax = 64;
        protected const int kOffsetMin = 32;

        public CombatText(int category, int bufferSize)
            : base(category)
        {
            mCacheFull = false;
		    mBufferSize = Math.Max(1,bufferSize);
		    mAnimationIndex = 0;
		    mColor = YellowCombatTextColor;
            mCTColors = new Color[] { WhiteCombatTextColor, YellowCombatTextColor, RedCombatTextColor };
		    mBusy = new List<SPSprite>(mBufferSize);
		    mIdle = new List<SPSprite>(mBufferSize);
		    mCombatSpriteCache = null;
            mCombatCountCache = null;
		
		    // Init crit details
            double critFrameDuration = 0.0525;
		    Vector2 prevOffset = Vector2.Zero;
		    Vector2 offset = Vector2.Zero;
            List<CTAnimation> array = new List<CTAnimation>(mBufferSize);
            GameController gc = GameController.GC;
            SPTween tween;
		
		    // Position offset animations
		    for (int i = 0; i < mBufferSize; ++i)
            {
			    float scale = 2.0f, scaleDelta = -0.35f, scaleInc = -0.35f, scaleDeltaReverse = 1.0f;

                if (gc.NextRandom(0, 1) == 1)
                {
				    scale /= 2;
				    scaleDelta *= -1;
				    scaleInc *= -1;
			    }
			
			    CTAnimation animation = new CTAnimation();
                animation.ScaleX = animation.ScaleY = scale;
			
                // Crits
			    for (int j = 0; j < 4; ++j)
                {
                    offset.X = gc.NextRandom(kOffsetMin, kOffsetMax);
                    offset.Y = gc.NextRandom(kOffsetMin, kOffsetMax);

                    if (gc.NextRandom(0, 1) == 1)
					    offset.X *= -1;
                    if (gc.NextRandom(0, 1) == 1)
					    offset.Y *= -1;
				
				    // Make sure we're not too close to the previous offset (else the animation looks weak)
				    if (Vector2.DistanceSquared(prevOffset, offset) < (0.75f * kOffsetMax))
                    {
					    // Flip to diagonally adjacent quadrant
					    offset.X *= -1;
					    offset.Y *= -1;
				    }
                
                    tween = new SPTween(animation.AnimatedSprite, critFrameDuration);
                    tween.AnimateProperty("X", offset.X);
                    tween.AnimateProperty("Y", offset.Y);
                    tween.AnimateProperty("ScaleX", scale + scaleDelta);
                    tween.AnimateProperty("ScaleY", scale + scaleDelta);
                    tween.Delay = animation.CritDelay;
                    animation.AddTween(tween, true, 0);
                
				    prevOffset.X = offset.X;
				    prevOffset.Y = offset.Y;
				
				    if (Math.Abs(scaleDelta) < scaleDeltaReverse)
					    scaleDelta += scaleInc;
				    else
					    scaleDelta = 1.5f - scale;
			    }
            
                tween = new SPTween(animation.AnimatedSprite, 1);
                tween.AnimateProperty("Alpha", 0);
                tween.Delay = animation.CritDelay;
                tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnCombatTweenCompleted));
                //tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnCombatTweenCompleted);
                animation.AddTween(tween, true, 0);
            
                // Non-crits
                //tween = new SPTween(animation.AnimatedSprite, 1);
                //tween.AnimateProperty("Y", -24);
                //animation.AddTween(tween, false, -1);

                //tween = new SPTween(animation.AnimatedSprite, 0.4);
                //tween.AnimateProperty("Alpha", 0);
                //tween.Delay = 0.5;
                //tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnCombatTweenCompleted));
                ////tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnCombatTweenCompleted);
                //animation.AddTween(tween, false, -1);
            
                //tween = new SPTween(animation.AnimatedSprite, 1);
                //tween.AnimateProperty("Y", 24);
                //animation.AddTween(tween, false, 1);

                //tween = new SPTween(animation.AnimatedSprite, 0.4);
                //tween.AnimateProperty("Alpha", 0);
                //tween.Delay = 0.5;
                //tween.AddActionEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, new Action<SPEvent>(OnCombatTweenCompleted));
                ////tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnCombatTweenCompleted);
                //animation.AddTween(tween, false, 1);

                array.Add(animation);
		    }
		
		    mCritAnimations = array;
            SetupProp();
        }

        #region Fields
        protected bool mCacheFull;

        protected int mAnimationIndex;
        protected Color mColor;
        protected Color[] mCTColors;
        protected int mBufferSize;
        protected List<SPSprite> mBusy;
        protected List<SPSprite> mIdle;

        private List<CTAnimation> mCritAnimations;
        protected Dictionary<int, List<SPSprite>> mCombatSpriteCache;
        protected Dictionary<int, int> mCombatCountCache;
        #endregion

        #region Properties
        public Color TextColor
        {
            get { return mColor; }
            set
            {
                mColor = value;
	
	            foreach (SPSprite sprite in mIdle)
                {
		            SPTextField textfield = sprite.ChildAtIndex(0) as SPTextField;
		            textfield.Color = value;
	            }

                foreach (SPSprite sprite in mBusy)
                {
		            SPTextField textfield = sprite.ChildAtIndex(0) as SPTextField;
		            textfield.Color = value;
	            }
            }
        }
        protected virtual string CacheKey { get { return SceneController.RESOURCE_CACHE_COMBAT_TEXT; } }
        protected virtual int RicochetBufferSize { get { return 5; } }
        protected virtual int CombatCacheNodeSize { get { return 10; } }
        protected virtual int MaxChars { get { return 6; } }
        public static Color RedCombatTextColor { get { return new Color(0xa0, 0x00, 0x00); } }
        public static Color YellowCombatTextColor { get { return new Color(0xfc, 0xff, 0x1b); } } // Video color: 0xffdd1b;
        public static Color WhiteCombatTextColor { get { return new Color(0xff, 0xff, 0xff); } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            // Crit/penalty text
	        for (int i = 0; i < mBufferSize; ++i)
            {
		        SPSprite sprite = CreateCombatTextSpriteWithValue(0);
        
                if (sprite != null)
                    mIdle.Add(sprite);
	        }
    
            ResetCombatSpriteCache();
            mScene.AddProp(this);
        }

        public override void Flip(bool enable)
        {
            float sceneWidth = mScene.ViewWidth;
    
            foreach (CTAnimation animation in mCritAnimations)
                animation.ContainerSprite.X = sceneWidth - animation.ContainerSprite.X;
        }

        private void DestroySpriteCache(Dictionary<int, List<SPSprite>> cache)
        {
            if (cache == null)
                return;

            foreach (KeyValuePair<int, List<SPSprite>> kvp in cache)
            {
                foreach (SPSprite sprite in kvp.Value)
                {
                    if (sprite.NumChildren > 0)
                        sprite.ChildAtIndex(0).Dispose();
                }
            }
        }

        public virtual void ResetCombatSpriteCache()
        {
            GameController gc = GameController.GC;
            HideAllText();
            gc.CacheResourceForKey(null, CacheKey);
            //DestroySpriteCache(mCombatSpriteCache);
            mCombatSpriteCache = null;
            mCombatCountCache = null;
    
            // Prep cache
            int i = 0;
            int[] shipScores = new int[7]; // shipKeys.Length +1 for overboard score
            string[] shipKeys = new string[] { "MerchantCaravel", "MerchantGalleon", "MerchantFrigate", "Pirate", "Navy", "SilverTrain" };

	        Dictionary<string, object> shipDetails = ShipFactory.Factory.AllNpcShipDetails;

            foreach (string shipKey in shipKeys)
            {
                Dictionary<string, object> shipDict = shipDetails[shipKey] as Dictionary<string, object>;
                shipScores[i++] = Convert.ToInt32(shipDict["infamyBonus"]);
            }
    
            // Add overboard score bonus
            shipScores[i] = Globals.OVERBOARD_SCORE_BONUS;
    
            int scoreMultiplier = gc.PlayerDetails.ScoreMultiplier;
            uint masteryBitmap = mScene.MasteryManager.MasteryBitmap;
            Dictionary<int, List<SPSprite>> keyCache = new Dictionary<int, List<SPSprite>>(shipScores.Length);
            Dictionary<int, int> countCache = new Dictionary<int, int>(shipScores.Length);

            Potion potion = mScene.PotionForKey(Potion.POTION_NOTORIETY);
            float notorietyFactor = Potion.NotorietyFactorForPotion(potion);

            float scoreBuilder;
            int score, cacheSize, cycle;
            int ricochetPotionBonus = Potion.RicochetBonusForPotion(mScene.PotionForKey(Potion.POTION_RICOCHET));
            int numCycles = ((masteryBitmap & (CCMastery.ROGUE_SCOUNDRELS_WAGER | CCMastery.VOODOO_GHOSTLY_AURA | CCMastery.ROGUE_FRIEND_OR_FOE)) != 0) ? 2 : 1;
            float ricochetFactor = (((masteryBitmap & CCMastery.CANNON_TRAIL_OF_DESTRUCTION) != 0) ? 1.15f : 1f) *
                (((masteryBitmap & CCMastery.CANNON_WRECKING_BALL) != 0) ? 1.5f : 1f);
            float pureSharkFactor = notorietyFactor *
                (((masteryBitmap & CCMastery.ROGUE_SHARK_BAIT) != 0) ? 1.5f : 1f) *
                (((masteryBitmap & CCMastery.ROGUE_THICK_AS_THIEVES) != 0) ? 1.1f : 1f) *
                Potion.BloodlustFactorForPotion(mScene.PotionForKey(Potion.POTION_BLOODLUST));
            float navyFactor = ((masteryBitmap & CCMastery.ROGUE_ROYAL_PARDON) != 0) ? 1.5f : 1f;
            float pureBaseFactor = notorietyFactor;

            if ((masteryBitmap & CCMastery.CANNON_MORTAL_IMPACT) != 0)
                pureBaseFactor *= 1.05f;
            if ((masteryBitmap & CCMastery.ROGUE_THICK_AS_THIEVES) != 0)
                pureBaseFactor *= 1.1f;

            float baseFactor = pureBaseFactor;
            float sharkFactor = pureSharkFactor;
            // Cannon Hits + Shark Attacks
            for (cycle = 0; cycle < numCycles; ++cycle)
            {
                if (cycle == 1)
                {
                    // R_ScoundrelsWager, R_FriendOrFoe, V_GhostlyAura
                    baseFactor = pureBaseFactor * 1.3f;
                    sharkFactor = pureSharkFactor * 1.3f;
                }

                for (i = 0; i < shipScores.Length; ++i)
                {
                    for (int crit = 1; crit <= 2; ++crit)
                    {
                        float critBonus = crit == 1 ? 1f : Globals.CRIT_FACTOR;
                        cacheSize = (i < (shipScores.Length - 1)) ? CombatCacheNodeSize / 2 : CombatCacheNodeSize;

                        if (i == 4) // NavyShip
                        {
                            scoreBuilder = baseFactor * navyFactor * (critBonus * scoreMultiplier * shipScores[i]);

                            scoreBuilder = critBonus * shipScores[i];
                            scoreBuilder *= baseFactor;
                            scoreBuilder *= navyFactor;
                            scoreBuilder *= (float)scoreMultiplier;
                        }
                        else if (i == shipScores.Length - 1) // Shark
                        {
                            scoreBuilder = critBonus * shipScores[i];
                            scoreBuilder *= sharkFactor;
                            scoreBuilder *= (float)scoreMultiplier;
                        }
                        else
                        {
                            scoreBuilder = critBonus * shipScores[i];
                            scoreBuilder *= baseFactor;
                            scoreBuilder *= (float)scoreMultiplier;
                        }

                        score = (int)scoreBuilder;
                        keyCache[score] = new List<SPSprite>(cacheSize);

                        if (countCache.ContainsKey(score))
                        {
                            int prevCacheSize = countCache[score];

                            if (prevCacheSize < cacheSize)
                                countCache[score] = cacheSize;
                        }
                        else
                        {
                            countCache[score] = cacheSize;
                        }

                        // Cache ricochet scores due to potion
                        if (i < shipScores.Length - 1)
                        {
                            for (int j = 1; j < 6; ++j)
                            {
                                cacheSize = (j < 3) ? 7 - 2 * j : 2; // 5,3,2,2,2

                                if (i == 4) // NavyShip
                                {
                                    scoreBuilder = (shipScores[i] + j * ricochetPotionBonus) * critBonus;
                                    scoreBuilder *= baseFactor;
                                    scoreBuilder *= ricochetFactor;
                                    scoreBuilder *= navyFactor;
                                    scoreBuilder *= (float)scoreMultiplier;
                                }
                                else
                                {
                                    scoreBuilder = (shipScores[i] + j * ricochetPotionBonus) * critBonus;
                                    scoreBuilder *= baseFactor;
                                    scoreBuilder *= ricochetFactor;
                                    scoreBuilder *= (float)scoreMultiplier;
                                }

                                score = (int)scoreBuilder;
                                keyCache[score] = new List<SPSprite>(cacheSize);

                                if (countCache.ContainsKey(score))
                                {
                                    int prevCacheSize = countCache[score];

                                    if (prevCacheSize < cacheSize)
                                        countCache[score] = cacheSize;
                                }
                            }
                        }
                    }
                }
            }

            // Munitions
            int munitionCycle, numMunitionCycles = ((masteryBitmap & (CCMastery.ROGUE_BLAZE_OF_GLORY | CCMastery.ROGUE_THUNDERMAKER)) != 0) ? 2 : 1;
            navyFactor = ((masteryBitmap & CCMastery.ROGUE_ROYAL_PARDON) != 0) ? 1.5f : 1f;
            pureBaseFactor = notorietyFactor;

            if ((masteryBitmap & CCMastery.ROGUE_THICK_AS_THIEVES) != 0)
                pureBaseFactor *= 1.1f;

            baseFactor = pureBaseFactor;

            for (munitionCycle = 0; munitionCycle < numMunitionCycles; ++munitionCycle)
            {
                if (munitionCycle == 1)
                {
                    // R_BlazeOfGlory, R_ThunderMaker
                    baseFactor = pureBaseFactor * 3f;
                }

                for (cycle = 0; cycle < numCycles; ++cycle)
                {
                    if (cycle == 1)
                    {
                        // R_ScoundrelsWager, V_GhostlyAura
                        baseFactor *= 1.3f;
                        sharkFactor *= 1.3f;
                    }

                    for (i = 0; i < shipScores.Length - 1; ++i) // No Sharks
                    {
                        for (int crit = 1; crit <= 2; ++crit)
                        {
                            cacheSize = CombatCacheNodeSize / 2;

                            if (i == 4) // NavyShip
                            {
                                scoreBuilder = crit * shipScores[i];
                                scoreBuilder *= baseFactor;
                                scoreBuilder *= navyFactor;
                                scoreBuilder *= (float)scoreMultiplier;
                            }
                            else
                            {
                                scoreBuilder = crit * shipScores[i];
                                scoreBuilder *= baseFactor;
                                scoreBuilder *= (float)scoreMultiplier;
                            }

                            score = (int)scoreBuilder;
                            keyCache[score] = new List<SPSprite>(cacheSize);

                            if (countCache.ContainsKey(score))
                            {
                                int prevCacheSize = countCache[score];

                                if (prevCacheSize < cacheSize)
                                    countCache[score] = cacheSize;
                            }
                            else
                            {
                                countCache[score] = cacheSize;
                            }
                        }
                    }
                }
            }

            // Voodoo
            int voodooCycle, numVoodooCycles = ((masteryBitmap & CCMastery.VOODOO_MOLTEN_ARMY) != 0) ? 2 : 1;
            navyFactor = ((masteryBitmap & CCMastery.ROGUE_ROYAL_PARDON) != 0) ? 1.5f : 1f;
            pureBaseFactor = notorietyFactor;

            if ((masteryBitmap & CCMastery.VOODOO_WITCH_DOCTOR) != 0)
                pureBaseFactor *= 1.25f;
            if ((masteryBitmap & CCMastery.ROGUE_THICK_AS_THIEVES) != 0)
                pureBaseFactor *= 1.1f;

            baseFactor = pureBaseFactor;

            for (voodooCycle = 0; voodooCycle < numVoodooCycles; ++voodooCycle)
            {
                if (voodooCycle == 1)
                {
                    // V_MoltenArmy
                    baseFactor = pureBaseFactor * 3f;
                }

                for (cycle = 0; cycle < numCycles; ++cycle)
                {
                    if (cycle == 1)
                    {
                        // R_ScoundrelsWager, V_GhostlyAura
                        baseFactor *= 1.3f;
                        sharkFactor *= 1.3f;
                    }

                    for (i = 0; i < shipScores.Length - 1; ++i) // No Sharks
                    {
                        for (int crit = 1; crit <= 2; ++crit)
                        {
                            cacheSize = CombatCacheNodeSize / 2;

                            if (i == 4) // NavyShip
                            {
                                scoreBuilder = crit * shipScores[i];
                                scoreBuilder *= baseFactor;
                                scoreBuilder *= navyFactor;
                                scoreBuilder *= (float)scoreMultiplier;
                            }
                            else
                            {
                                scoreBuilder = crit * shipScores[i];
                                scoreBuilder *= baseFactor;
                                scoreBuilder *= (float)scoreMultiplier;
                            }

                            score = (int)scoreBuilder;
                            keyCache[score] = new List<SPSprite>(cacheSize);

                            if (countCache.ContainsKey(score))
                            {
                                int prevCacheSize = countCache[score];

                                if (prevCacheSize < cacheSize)
                                    countCache[score] = cacheSize;
                            }
                            else
                            {
                                countCache[score] = cacheSize;
                            }
                        }
                    }
                }
            }

            mCacheFull = false;
            mCombatSpriteCache = keyCache;
            mCombatCountCache = countCache;
            gc.CacheResourceForKey(mCombatSpriteCache, CacheKey);

            cacheSize = 0;
            foreach (KeyValuePair<int, int> kvp in mCombatCountCache)
                cacheSize += kvp.Value;
#if COMBAT_TEXT_DEBUG
            System.Diagnostics.Debug.WriteLine("CombatText Cache Size: {0}", cacheSize);
#endif
        }

        public void FillCombatSpriteCache()
        {
            if (mCacheFull)
                return;
    
            foreach (KeyValuePair<int, List<SPSprite>> kvp in mCombatSpriteCache)
            {
                mCacheFull = true;

                int cacheSize = CombatCacheNodeSize;
                if (mCombatCountCache.ContainsKey(kvp.Key))
                    cacheSize = mCombatCountCache[kvp.Key];

                List<SPSprite> subCache = kvp.Value;
        
                if (subCache.Count >= cacheSize)
                    continue;
        
                mCacheFull = false;
        
                // Increment cache size
                SPSprite sprite = CreateCombatTextSpriteWithValue(kvp.Key);
        
                if (sprite != null)
                {
                    subCache.Add(sprite);
                    break;
                }
            }
        }

        private SPSprite CreateCombatTextSpriteWithValue(int value)
        {
            SPSprite sprite = new SPSprite();
            sprite.Touchable = false;
    
            float width = 42f * MaxChars;
            float height = 64f;
#if IOS_SCREENS
            int fontSize = 42;
#else
            int fontSize = 56;
#endif
    
            SPTextField textField = new SPTextField(width, height, value.ToString(), mScene.FontKey, fontSize);
            textField.Touchable = false;
            textField.HAlign = SPTextField.SPHAlign.Center;
            textField.VAlign = SPTextField.SPVAlign.Center;
            textField.X = -textField.Width / 2;
            textField.Y = -textField.Height / 2;
            textField.Color = WhiteCombatTextColor;
            sprite.AddChild(textField);
            return sprite;
        }

        public void PrepareForNewGame()
        {
            foreach (CTAnimation animation in mCritAnimations)
                animation.CleanupWithJuggler(mScene.HudJuggler);
            mScene.RemoveProp(this, false);
            mScene.AddProp(this);
        }

        private SPSprite CachedCombatSpriteForKey(int key)
        {
            SPSprite sprite = null;

            if (mCombatSpriteCache.ContainsKey(key))
            {
                List<SPSprite> array = mCombatSpriteCache[key];

                if (array != null && array.Count > 0)
                {
                    sprite = array[array.Count - 1];
                    array.RemoveAt(array.Count - 1);
                }
            }
    
            return sprite;
        }

        private bool RecacheCombatSprite(SPSprite sprite)
        {
            if (sprite == null || sprite.NumChildren == 0)
                return false;
    
            bool recached = false;
            SPTextField textField = sprite.ChildAtIndex(0) as SPTextField;
            int key = Convert.ToInt32(textField.Text);

            if (mCombatSpriteCache.ContainsKey(key))
            {
                List<SPSprite> array = mCombatSpriteCache[key];
                if (array.Count < CombatCacheNodeSize)
                {
                    array.Add(sprite);
                    recached = true;
                }
            }
    
            return recached;
        }

        private CTAnimation NextAnimation()
        {
            if (++mAnimationIndex >= mCritAnimations.Count)
		        mAnimationIndex = 0;
	        return mCritAnimations[mAnimationIndex];
        }

        public void DisplayCombatText(int value, float x, float y, bool crit, bool twoBy, CTColorType colorType)
        {
            bool cacheHit = false;
#if IOS_SCREENS
	        float scaleFactor = (twoBy) ? 1.2f : 0.9f;
#else
            float scaleFactor = (twoBy) ? 1.3f : 1.0f;
#endif
            SPSprite combatSprite = CachedCombatSpriteForKey(value);
    
            if (combatSprite == null)
            {
                if (mIdle.Count == 0)
                    return;
                combatSprite = mIdle[mIdle.Count-1];
#if COMBAT_TEXT_DEBUG
                System.Diagnostics.Debug.WriteLine("XXXXX   UNCACHED {0} XXXXX", value);
#endif
            }
            else
            {
                cacheHit = true;
#if COMBAT_TEXT_DEBUG
                System.Diagnostics.Debug.WriteLine("$$$$$$   CACHED   $$$$$$$");
#endif
            }
    
            if (mScene.Flipped)
                x = mScene.ViewWidth - x;
    
            mBusy.Add(combatSprite);
    
            if (!cacheHit)
                mIdle.Remove(combatSprite);
            combatSprite.X = combatSprite.Y = 0;
    
            if (combatSprite.NumChildren == 0)
                return;
    
            SPTextField textfield = combatSprite.ChildAtIndex(0) as SPTextField;
            textfield.Color = (crit) ? mCTColors[(int)colorType] : mCTColors[(int)CTColorType.NonCrit];
    
            CTAnimation animation = NextAnimation();
            animation.Reset();
            animation.ScaleFactor = scaleFactor;
    
            if (!cacheHit)
                textfield.Text = value.ToString();
    
            if (!crit)
            {
                animation.ScaleFactor *= 0.75f;
                animation.ContainerSprite.X = Math.Min(Math.Max(0.5f * combatSprite.Width, x), mScene.ViewWidth - 0.5f * combatSprite.Width);
        
                float yOffset = 10.0f + combatSprite.Height;
		        y -= yOffset;
		
		        if (y < combatSprite.Height)
                {
			        // Too close to top of screen - move down and tween downwards
			        y += 2 * yOffset;
                    animation.ContainerSprite.Y = Math.Min(y, mScene.ViewHeight - 70.0f - 0.5f * combatSprite.Height);
                    animation.AnimateAsNonCritDown(combatSprite, mScene.HudJuggler);
		        }
                else
                {
                    // Normal situtation - place above ship and tween upwards
                    animation.ContainerSprite.Y = Math.Min(y, mScene.ViewHeight - 70.0f - 0.5f * combatSprite.Height);
                    animation.AnimateAsNonCritUp(combatSprite, mScene.HudJuggler);
		        }
            }
            else 
            {
                float yOffset = 10.0f + combatSprite.Height * scaleFactor, xOffset = combatSprite.Width * scaleFactor;
		        animation.ContainerSprite.Y = Math.Min(Math.Max(kOffsetMax + yOffset, y - yOffset), mScene.ViewHeight - (kOffsetMax + yOffset));
                animation.ContainerSprite.X = Math.Min(Math.Max(0.5f * xOffset + kOffsetMax, x), mScene.ViewWidth - (0.5f * xOffset + kOffsetMax));
                animation.AnimateAsCrit(combatSprite, mScene.HudJuggler);
            }
    
            AddChild(animation.ContainerSprite);
        }

        private void OnCombatTweenCompleted(SPEvent ev)
        {
            SPSprite combatSprite = null;
	        SPTween tween = ev.CurrentTarget as SPTween;
    
            // Retrieve animated sprite
	        SPSprite animatedSprite = tween.Target as SPSprite;
    
            // Retrieve combat sprite
            if (animatedSprite.NumChildren > 0)
                combatSprite = animatedSprite.ChildAtIndex(0) as SPSprite;
    
            // Retrieve container sprite
            SPSprite containerSprite = animatedSprite.Parent as SPSprite;
            containerSprite.RemoveFromParent();
    
            if (combatSprite != null)
            {
                if (!RecacheCombatSprite(combatSprite))
                    mIdle.Add(combatSprite);
#if COMBAT_TEXT_DEBUG
                else
                    System.Diagnostics.Debug.WriteLine("!!!!!!!  RECACHED  !!!!!!!!");
#endif
                combatSprite.RemoveFromParent();
                mBusy.Remove(combatSprite);
            }
        }

        public void HideAllText()
        {
            foreach (SPSprite combatSprite in mBusy)
            {
                SPSprite animatedSprite = combatSprite.Parent as SPSprite;
        
                if (animatedSprite != null)
                {
                    mScene.HudJuggler.RemoveTweensWithTarget(animatedSprite);
        
                    SPSprite containerSprite = animatedSprite.Parent as SPSprite;
                    containerSprite.RemoveFromParent();

                    combatSprite.RemoveFromParent();
                }
        
                if (!RecacheCombatSprite(combatSprite))
                    mIdle.Add(combatSprite);
	        }

            RemoveAllChildren();
            mBusy.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mCritAnimations != null)
                        {
                            foreach (CTAnimation animation in mCritAnimations)
                                animation.CleanupWithJuggler(mScene.HudJuggler);
                            mCritAnimations = null;
                        }

                        mScene.RemoveProp(this, false);
                        GameController.GC.CacheResourceForKey(null, CacheKey);

                        //DestroySpriteCache(mCombatSpriteCache);
                        mCombatSpriteCache = null;
                        mCombatCountCache = null;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion



        private class CTAnimation
        {
            public CTAnimation()
            {
                mScaleX = mScaleY = 1f;
		        mScaleFactor = 1f;
        
                mAnimatedSprite = new SPSprite();
		        mContainerSprite = new SPSprite();
                mContainerSprite.AddChild(mAnimatedSprite);
        
		        mNonCritUpTweens = new List<SPTween>();
                mNonCritDownTweens = new List<SPTween>();
                mCritTweens = new List<SPTween>();
            }

            #region Fields
            private float mScaleX;
            private float mScaleY;
            private float mScaleFactor;

            private SPSprite mContainerSprite;
            private SPSprite mAnimatedSprite;
            private List<SPTween> mNonCritUpTweens;
            private List<SPTween> mNonCritDownTweens;
            private List<SPTween> mCritTweens;
            #endregion

            #region Properties
            public float ScaleX { get { return mScaleX; } set { mScaleX = value; } }
            public float ScaleY { get { return mScaleY; } set { mScaleY = value; } }
            public float ScaleFactor
            {
                get { return mScaleFactor; }
                set
                {
                    if (SPMacros.SP_IS_FLOAT_EQUAL(value, 0))
                        return;
                    mContainerSprite.ScaleX /= mScaleFactor;
                    mContainerSprite.ScaleY /= mScaleFactor;
                    mScaleFactor = value;
                    mContainerSprite.ScaleX *= value;
                    mContainerSprite.ScaleY *= value;
                }
            }

            public SPSprite ContainerSprite { get { return mContainerSprite; } }
            public SPSprite AnimatedSprite { get { return mAnimatedSprite; } }
            public double NonCritUpDelay
            {
                get
                {
                    double delay = 0;
    
                    foreach (SPTween tween in mNonCritUpTweens)
                        delay += tween.TotalTime;
    
                    return delay;
                }
            }
            public double NonCritDownDelay
            {
                get
                {
                    double delay = 0;
    
                    foreach (SPTween tween in mNonCritDownTweens)
                        delay += tween.TotalTime;
    
                    return delay;
                }
            }
            public double CritDelay
            {
                get
                {
                    double delay = 0;
    
                    foreach (SPTween tween in mCritTweens)
                        delay += tween.TotalTime;
    
                    return delay;
                }
            }
            #endregion

            #region Methods
            public void AddTween(SPTween tween, bool crit, int dir)
            {
                if (crit)
                    mCritTweens.Add(tween);
                else if (dir == -1)
                    mNonCritUpTweens.Add(tween);
                else if (dir == 1)
                    mNonCritDownTweens.Add(tween);
            }

            public void RemoveTween(SPTween tween)
            {
                if (tween == null)
                    return;
                mNonCritUpTweens.Remove(tween);
                mNonCritDownTweens.Remove(tween);
                mCritTweens.Remove(tween);
            }

            public void Reset()
            {
                foreach (SPTween tween in mNonCritUpTweens)
                    tween.Reset();
                foreach (SPTween tween in mNonCritDownTweens)
                    tween.Reset();
                foreach (SPTween tween in mCritTweens)
                    tween.Reset();
                ScaleFactor = 1f;
                mAnimatedSprite.X = mAnimatedSprite.Y = 0;
                mAnimatedSprite.Alpha = 1f;
                mAnimatedSprite.RemoveAllChildren();
            }

            public void AnimateAsNonCritUp(SPDisplayObject displayObject, SPJuggler juggler)
            {
                if (mNonCritUpTweens.Count == 0)
                    return;
    
                mAnimatedSprite.ScaleX = mAnimatedSprite.ScaleY = 1f;
                mAnimatedSprite.AddChild(displayObject);
    
                foreach (SPTween tween in mNonCritUpTweens)
                    juggler.AddObject(tween);
            }

            public void AnimateAsNonCritDown(SPDisplayObject displayObject, SPJuggler juggler)
            {
                if (mNonCritDownTweens.Count == 0)
                    return;
    
                mAnimatedSprite.ScaleX = mAnimatedSprite.ScaleY = 1f;
                mAnimatedSprite.AddChild(displayObject);
    
                foreach (SPTween tween in mNonCritDownTweens)
                    juggler.AddObject(tween);
            }

            public void AnimateAsCrit(SPDisplayObject displayObject, SPJuggler juggler)
            {
                if (mCritTweens.Count == 0)
                    return;
    
                mAnimatedSprite.ScaleX = mScaleX;
                mAnimatedSprite.ScaleY = mScaleY;
                mAnimatedSprite.AddChild(displayObject);
    
                foreach (SPTween tween in mCritTweens)
                    juggler.AddObject(tween);
            }

            public void CleanupWithJuggler(SPJuggler juggler)
            {
                if (juggler != null)
                    juggler.RemoveTweensWithTarget(mAnimatedSprite);
            }
            #endregion
        }
    }
}
