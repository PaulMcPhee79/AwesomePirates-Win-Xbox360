using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    interface IMasteryGui
    {
        // Mastery Trees
        string TitleForTree(uint key);
        string TextureNameForTree(uint key);

        // Mastery Nodes
        string TitleForNode(uint key);
        string TitleForSpecialty(uint key);
        string DescForNode(uint key);

        string TextureNameForNodeHighlight(uint key);
        string TextureNameForNodeGlow(uint key);
        string TextureNameForNodeActiveBg(uint key);
        string TextureNameForNodeInactiveBg(uint key);
        string TextureNameForNodeMaxedBg(uint key);
        string TextureNameForNodeIcon(uint key);
    }
}
