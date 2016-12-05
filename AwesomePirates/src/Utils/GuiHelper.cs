using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class GuiHelper
    {
        public enum GuiHelperSize
        {
            Sml = 0,
            Med,
            Lge
        }

        public static string CommaSeparatedValue(int value)
        {
            return Globals.CommaSeparatedValue(value);
        }

        public static string CommaSeparatedValue(uint value)
        {
            return Globals.CommaSeparatedValue(value);
        }

        public static SPTexture CachedScrollTextureByName(string textureName, SceneController scene)
        {
            if (textureName == null)
                return null;

            string cachedTextureName = textureName + "-cached";
    
            // Re-use rendered texture memory
            SPTexture texture = scene.TextureByName(cachedTextureName);
    
            if (texture == null)
            {
                texture = scene.TextureByName(textureName);
                texture = Globals.WholeTextureFromQuarter(texture);
                scene.CacheTexture(texture, cachedTextureName);
            }
    
            return texture;
        }

        public static SPSprite CountdownSpriteForValue(int value, SceneController scene)
        {
            float offsetX = 0;
            int tens = value / 10, ones = value % 10;
            SPSprite sprite = new SPSprite();

            if (tens != 0)
            {
                SPImage tensImage = new SPImage(scene.TextureByName("fancy-" + tens));
                tensImage.X = offsetX;
                offsetX += tensImage.Width + 2;
                sprite.AddChild(tensImage);
            }

            SPImage onesImage = new SPImage(scene.TextureByName("fancy-" + ones));
            onesImage.X = offsetX;
            sprite.AddChild(onesImage);

            return sprite;
        }

        public static SPSprite ScoreMultiplierSpriteForValue(int value, SceneController scene)
        {
            float offsetX = 0;
            int tens = value / 10, ones = value % 10;
            SPSprite sprite = new SPSprite();
    
            if (tens != 0)
            {
                SPImage tensImage = new SPImage(scene.TextureByName("fancy-" + tens));
                tensImage.X = offsetX;
                offsetX += tensImage.Width + 2;
                sprite.AddChild(tensImage);
            }
    
            SPImage onesImage = new SPImage(scene.TextureByName("fancy-" + ones));
            onesImage.X = offsetX;
            offsetX += onesImage.Width + 2 * 2;
            sprite.AddChild(onesImage);
    
            SPImage xImage = new SPImage(scene.TextureByName("fancy-x"));
            xImage.X = offsetX;
            xImage.Y = sprite.Height - xImage.Height;
            sprite.AddChild(xImage);
    
            return sprite;
        }

        public static SPSprite PotionSpriteWithPotion(Potion potion, GuiHelperSize size, SceneController scene)
        {
            string suffix = "lge";
            GameController gc = GameController.GC;
    
	        switch (size)
            {
                case GuiHelperSize.Sml: suffix = "sml"; break;
                case GuiHelperSize.Med: suffix = "med"; break;
                case GuiHelperSize.Lge:
                default: suffix = "lge"; break;
            }
    
	        SPSprite sprite = new SPSprite();
	        SPImage image = new SPImage(scene.TextureByName("potion-contents-" + suffix));
	        image.X = -image.Width / 2;
	        image.Y = -image.Height / 2;
	        image.Color = potion.Color;
            image.Effecter = new SPEffecter(scene.EffectForKey("Potion"), gc.PotionDraw, gc.NextRandom(150, 300) / 100f);
            sprite.AddChild(image);
	
	        image = new SPImage(scene.TextureByName("potion-vial-" + suffix));
	        image.X = -image.Width / 2;
	        image.Y = -image.Height / 2;
            sprite.AddChild(image);
	        return sprite;
        }

        // Organize potions so that they can be drawn with only a single texture switch.
        public static SPSprite AggregatePotionSprites(List<SPSprite> potionSprites, SPEffecter effecter)
        {
            SPSprite container = new SPSprite();

            if (potionSprites == null)
                return container;

            SPSprite contentSubcontainer = new SPSprite();
            SPSprite vialSubcontainer = new SPSprite();
            container.AddChild(contentSubcontainer);
            container.AddChild(vialSubcontainer);

            contentSubcontainer.Effecter = effecter;

            foreach (SPSprite potionSprite in potionSprites)
            {
                if (potionSprite.NumChildren < 2)
                    continue;

                SPImage contents = potionSprite.ChildAtIndex(0) as SPImage;
                contents.X += potionSprite.X;
                contents.Y += potionSprite.Y;
                contents.Effecter = null;
                contentSubcontainer.AddChild(contents);

                // Also child index 0, because contents is no re-parented.
                SPImage vial = potionSprite.ChildAtIndex(0) as SPImage;
                vial.X += potionSprite.X;
                vial.Y += potionSprite.Y;
                vialSubcontainer.AddChild(vial);
            }

            return container;
        }
    }
}
