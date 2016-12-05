using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class CCMasteryGuiHelper : IMasteryGui
    {
        private static CCMasteryGuiHelper instance = null;

        private CCMasteryGuiHelper()
        {

        }

        public static CCMasteryGuiHelper CCMasteryGui
        {
            get
            {
                if (instance == null)
                    instance = new CCMasteryGuiHelper();
                return instance;
            }
        }

        // Mastery Trees
        public string TitleForTree(uint key)
        {
            string title = null;
            switch (key)
            {
                case CCMastery.TREE_CANNON_MASTERY: title = "Cannon Mastery"; break;
                case CCMastery.TREE_VOODOO_MASTERY: title = "Voodoo Mastery"; break;
                case CCMastery.TREE_ROGUE_MASTERY: title = "Pirate Mastery"; break;
                default: break;
            }

            return title;
        }

        public string TitleForSpecialty(uint key)
        {
            string title = null;
            switch (key)
            {
                case CCMastery.TREE_CANNON_MASTERY: title = "Cannons"; break;
                case CCMastery.TREE_VOODOO_MASTERY: title = "Voodoo"; break;
                case CCMastery.TREE_ROGUE_MASTERY: title = "Piracy"; break;
                default: break;
            }

            return title;
        }

        public string TextureNameForTree(uint key)
        {
            string textureName = null;
            switch (key)
            {
                case CCMastery.TREE_CANNON_MASTERY: textureName = "cannon-mastery-icon"; break;
                case CCMastery.TREE_VOODOO_MASTERY: textureName = "voodoo-mastery-icon"; break;
                case CCMastery.TREE_ROGUE_MASTERY: textureName = "rogue-mastery-icon"; break;
                default: break;
            }

            return textureName;
        }

        // Mastery Nodes
        public string TitleForNode(uint key)
        {
            string title = null;
            switch (key)
            {
                case CCMastery.CANNON_MORTAL_IMPACT: title = "Mortal Impact"; break;
                case CCMastery.CANNON_TRAIL_OF_DESTRUCTION: title = "Trail of Destruction"; break;
                case CCMastery.CANNON_SCORCHED_HORIZON: title = "Scorched Horizon"; break;
                case CCMastery.CANNON_NOXIOUS_STRAITS: title = "Noxious Straits"; break;
                case CCMastery.CANNON_DEPTH_CHARGE: title = "Depth Charge"; break;
                case CCMastery.CANNON_CAPTAINS_REPRIEVE: title = "Captain's Reprieve"; break;
                case CCMastery.CANNON_CANNONEER: title = "Cannoneer"; break;
                case CCMastery.CANNON_WRECKING_BALL: title = "Wrecking Ball"; break;

                case CCMastery.VOODOO_DAVYS_FURY: title = "Davy's Fury"; break;
                case CCMastery.VOODOO_TWISTED_SISTERS: title = "Twisted Sisters"; break;
                case CCMastery.VOODOO_GHOSTLY_AURA: title = "Ghostly Aura"; break;
                case CCMastery.VOODOO_SPECTER_OF_SALVATION: title = "Specter of Salvation"; break;
                case CCMastery.VOODOO_MOLTEN_ARMY: title = "Molten Army"; break;
                case CCMastery.VOODOO_LEAGUES_OF_ANGUISH: title = "Leagues of Anguish"; break;
                case CCMastery.VOODOO_WITCH_DOCTOR: title = "Witch Doctor"; break;
                case CCMastery.VOODOO_ENCHANTED_REJUVENATION: title = "Enchanted Rejuvenation"; break;

                case CCMastery.ROGUE_ENTANGLEMENT: title = "Entanglement"; break;
                case CCMastery.ROGUE_BLAZE_OF_GLORY: title = "Blaze of Glory"; break;
                case CCMastery.ROGUE_FRIEND_OR_FOE: title = "Friend or Foe"; break;
                case CCMastery.ROGUE_THUNDERMAKER: title = "Thunder Maker"; break;
                case CCMastery.ROGUE_SHARK_BAIT: title = "Shark Bait"; break;
                case CCMastery.ROGUE_THICK_AS_THIEVES: title = "Thick As Thieves"; break;
                case CCMastery.ROGUE_ROYAL_PARDON: title = "Royal Pardon"; break;
                case CCMastery.ROGUE_SCOUNDRELS_WAGER: title = "Scoundrel's Wager"; break;
                default: break;
            }

            return title;
        }

        public string DescForNode(uint key)
        {
            string desc = null;
            switch (key)
            {
                case CCMastery.CANNON_MORTAL_IMPACT: desc = "5% more score from cannon hits."; break;
                case CCMastery.CANNON_TRAIL_OF_DESTRUCTION: desc = "15% more score from ricochets."; break;
                case CCMastery.CANNON_SCORCHED_HORIZON: desc = "Molten Shot shoots 2 extra cannonballs."; break;
                case CCMastery.CANNON_NOXIOUS_STRAITS: desc = "Venom Shot has 10 extra charges."; break;
                case CCMastery.CANNON_DEPTH_CHARGE: desc = "50% more score from Abyssal Surges."; break;
                case CCMastery.CANNON_CAPTAINS_REPRIEVE: desc = "50% chance for a miss not to reduce accuracy bonus."; break;
                case CCMastery.CANNON_CANNONEER: desc = "20% larger ricochet cone."; break;
                case CCMastery.CANNON_WRECKING_BALL: desc = "50% more score from ricochets."; break;

                case CCMastery.VOODOO_DAVYS_FURY: desc = "Grants an extra hand to Hand of Davy."; break;
                case CCMastery.VOODOO_TWISTED_SISTERS: desc = "Tornado Storm spawns an extra tornado."; break;
                case CCMastery.VOODOO_GHOSTLY_AURA: desc = "30% more score when using the Ghost Ship"; break;
                case CCMastery.VOODOO_SPECTER_OF_SALVATION: desc = "Ghost Ship instantly removes up to 4 red crosses on use."; break;
                case CCMastery.VOODOO_MOLTEN_ARMY: desc = "3x score from Sea of Lava and Magma Pools."; break;
                case CCMastery.VOODOO_LEAGUES_OF_ANGUISH: desc = "Whirlpool grants 3x score for overboard sailors."; break;
                case CCMastery.VOODOO_WITCH_DOCTOR: desc = "25% more score from all voodoo spells"; break;
                case CCMastery.VOODOO_ENCHANTED_REJUVENATION: desc = "Ships sunk with voodoo spells reduce red crosses twice as fast as normal."; break;

                case CCMastery.ROGUE_ENTANGLEMENT: desc = "Increases Net size by 30%."; break;
                case CCMastery.ROGUE_BLAZE_OF_GLORY: desc = "3x score from enemies burnt in a Brandy Slick."; break;
                case CCMastery.ROGUE_FRIEND_OR_FOE: desc = "30% more score when flying Navy Colors."; break;
                case CCMastery.ROGUE_THUNDERMAKER: desc = "3x score from Powder Keg sinkings."; break;
                case CCMastery.ROGUE_SHARK_BAIT: desc = "50% more score from shark attacks."; break;
                case CCMastery.ROGUE_THICK_AS_THIEVES: desc = "10% more score from all sources."; break;
                case CCMastery.ROGUE_ROYAL_PARDON: desc = "50% more score from navy ships."; break;
                case CCMastery.ROGUE_SCOUNDRELS_WAGER: desc = "30% more score when you have no red crosses."; break;
                default: break;
            }

            return desc;
        }

        public string TextureNameForNodeHighlight(uint key)
        {
            return "mastery-node-highlight";
        }

        public string TextureNameForNodeGlow(uint key)
        {
            return "mastery-node-glow";
        }

        public string TextureNameForNodeActiveBg(uint key)
        {
            return "mastery-node-active-bg";
        }

        public string TextureNameForNodeInactiveBg(uint key)
        {
            return "mastery-node-inactive-bg";
        }

        public string TextureNameForNodeMaxedBg(uint key)
        {
            return "mastery-node-maxed-bg";
        }

        public string TextureNameForNodeIcon(uint key)
        {
            string textureName = null;
            switch (key)
            {
                case CCMastery.CANNON_MORTAL_IMPACT: textureName = "mortal-impact"; break;
                case CCMastery.CANNON_TRAIL_OF_DESTRUCTION: textureName = "trail-of-destruction"; break;
                case CCMastery.CANNON_SCORCHED_HORIZON: textureName = "scorched-horizon"; break;
                case CCMastery.CANNON_NOXIOUS_STRAITS: textureName = "noxious-straits"; break;
                case CCMastery.CANNON_DEPTH_CHARGE: textureName = "depth-charge"; break;
                case CCMastery.CANNON_CAPTAINS_REPRIEVE: textureName = "captains-reprieve"; break;
                case CCMastery.CANNON_CANNONEER: textureName = "cannoneer"; break;
                case CCMastery.CANNON_WRECKING_BALL: textureName = "wrecking-ball"; break;

                case CCMastery.VOODOO_DAVYS_FURY: textureName = "davys-fury"; break;
                case CCMastery.VOODOO_TWISTED_SISTERS: textureName = "twisted-sisters"; break;
                case CCMastery.VOODOO_GHOSTLY_AURA: textureName = "ghostly-aura"; break;
                case CCMastery.VOODOO_SPECTER_OF_SALVATION: textureName = "specter-of-salvation"; break;
                case CCMastery.VOODOO_MOLTEN_ARMY: textureName = "molten-army"; break;
                case CCMastery.VOODOO_LEAGUES_OF_ANGUISH: textureName = "leagues-of-anguish"; break;
                case CCMastery.VOODOO_WITCH_DOCTOR: textureName = "witch-doctor"; break;
                case CCMastery.VOODOO_ENCHANTED_REJUVENATION: textureName = "enchanted-rejuvenation"; break;

                case CCMastery.ROGUE_ENTANGLEMENT: textureName = "entanglement"; break;
                case CCMastery.ROGUE_BLAZE_OF_GLORY: textureName = "blaze-of-glory"; break;
                case CCMastery.ROGUE_FRIEND_OR_FOE: textureName = "friend-or-foe"; break;
                case CCMastery.ROGUE_THUNDERMAKER: textureName = "thunder-maker"; break;
                case CCMastery.ROGUE_SHARK_BAIT: textureName = "shark-bait"; break;
                case CCMastery.ROGUE_THICK_AS_THIEVES: textureName = "thick-as-thieves"; break;
                case CCMastery.ROGUE_ROYAL_PARDON: textureName = "royal-pardon"; break;
                case CCMastery.ROGUE_SCOUNDRELS_WAGER: textureName = "scoundrels-wager"; break;
                default: break;
            }

            return textureName;
        }
    }
}
