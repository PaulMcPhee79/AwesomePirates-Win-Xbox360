using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    static class MasteryGuiHelper
    {
        public static IMasteryGui sHelper = CCMasteryGuiHelper.CCMasteryGui;

        public static void SetHelper(IMasteryGui helper)
        {
            sHelper = helper;
        }

        // Mastery Trees
        public static string TitleForTree(uint key)
        {
            return sHelper.TitleForTree(key);
        }

        public static string TitleForSpecialty(uint key)
        {
            return sHelper.TitleForSpecialty(key);
        }

        public static string TextureNameForTree(uint key)
        {
            return sHelper.TextureNameForTree(key);
        }

        // Mastery Nodes
        public static string TitleForNode(uint key)
        {
            return sHelper.TitleForNode(key);
        }

        public static string DescForNode(uint key)
        {
            return sHelper.DescForNode(key);
        }

        public static string TextureNameForNodeHighlight(uint key)
        {
            return sHelper.TextureNameForNodeHighlight(key);
        }

        public static string TextureNameForNodeGlow(uint key)
        {
            return sHelper.TextureNameForNodeGlow(key);
        }

        public static string TextureNameForNodeActiveBg(uint key)
        {
            return sHelper.TextureNameForNodeActiveBg(key);
        }

        public static string TextureNameForNodeInactiveBg(uint key)
        {
            return sHelper.TextureNameForNodeInactiveBg(key);
        }

        public static string TextureNameForNodeMaxedBg(uint key)
        {
            return sHelper.TextureNameForNodeMaxedBg(key);
        }

        public static string TextureNameForNodeIcon(uint key)
        {
            return sHelper.TextureNameForNodeIcon(key);
        }
    }
}
