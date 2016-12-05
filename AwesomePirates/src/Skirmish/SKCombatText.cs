
//#define SKCOMBAT_TEXT_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class SKCombatText : CombatText
    {
        public SKCombatText(int category, int bufferSize)
            : base(category, bufferSize)
        {
            mCTColors = new Color[]
            {
                WhiteCombatTextColor,
                YellowCombatTextColor,
                RedCombatTextColor,
                SKHelper.ColorForTeamIndex(SKTeamIndex.Red),
                SKHelper.ColorForTeamIndex(SKTeamIndex.Blue),
                SKHelper.ColorForTeamIndex(SKTeamIndex.Green),
                SKHelper.ColorForTeamIndex(SKTeamIndex.Yellow)
            };
        }

        #region Properties
        protected override string CacheKey { get { return SceneController.RESOURCE_CACHE_SKCOMBAT_TEXT; } }
        protected override int RicochetBufferSize { get { return 5; } }
        protected override int CombatCacheNodeSize { get { return 20; } }
        protected override int MaxChars { get { return 6; } }
        #endregion

        #region Methods
        public override void ResetCombatSpriteCache()
        {
            GameController gc = GameController.GC;
            HideAllText();
            gc.CacheResourceForKey(null, SceneController.RESOURCE_CACHE_COMBAT_TEXT);
            //DestroySpriteCache(mCombatSpriteCache);
            mCombatSpriteCache = null;
            mCombatCountCache = null;

            // Prep cache
            int i = 0;
            int[] shipScores = new int[6]; // shipKeys.Length +1 for overboard score.
            string[] shipKeys = new string[] { "MerchantCaravel", "MerchantGalleon", "MerchantFrigate", "Escort", "SilverTrain" };

            Dictionary<string, object> shipDetails = ShipFactory.Factory.AllNpcShipDetails;

            foreach (string shipKey in shipKeys)
            {
                Dictionary<string, object> shipDict = shipDetails[shipKey] as Dictionary<string, object>;
                shipScores[i++] = Convert.ToInt32(shipDict["infamyBonus"]);
            }

            // Add overboard score bonus
            shipScores[i] = Globals.OVERBOARD_SCORE_BONUS;

            int scoreMultiplier = SKManager.kSKScoreMultiplier;
            Dictionary<int, List<SPSprite>> keyCache = new Dictionary<int, List<SPSprite>>(shipScores.Length);
            Dictionary<int, int> countCache = new Dictionary<int, int>(shipScores.Length);

            float scoreBuilder;
            int score, cacheSize;
            int ricochetPotionBonus = Potion.RicochetBonusForPotion(mScene.PotionForKey(Potion.POTION_RICOCHET));

            // Cannon Hits + Shark Attacks + Munitions + Voodoo
            for (i = 0; i < shipScores.Length; ++i)
            {
                // 2v1 means 2x score for the lone player.
                for (int skMultiplier = 1; skMultiplier <= 2; ++skMultiplier)
                {
                    // Only crits; no normal hits in multiplayer modes.
                    float crit = Globals.CRIT_FACTOR;
                    cacheSize = CombatCacheNodeSize;
                    scoreBuilder = crit * shipScores[i];
                    scoreBuilder *= skMultiplier;
                    scoreBuilder *= (float)scoreMultiplier;

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

            mCacheFull = false;
            mCombatSpriteCache = keyCache;
            mCombatCountCache = countCache;
            gc.CacheResourceForKey(mCombatSpriteCache, SceneController.RESOURCE_CACHE_COMBAT_TEXT);

            cacheSize = 0;
            foreach (KeyValuePair<int, int> kvp in mCombatCountCache)
                cacheSize += kvp.Value;
#if SKCOMBAT_TEXT_DEBUG
            System.Diagnostics.Debug.WriteLine("SKCombatText Cache Size: {0}", cacheSize);
#endif
        }
        #endregion
    }
}
