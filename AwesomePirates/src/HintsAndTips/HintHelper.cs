using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class HintHelper
    {
        public const uint kHintTextPropTag = 0x10;

        public static HintPackage ThisIsYourShip(SceneController scene, int category, Vector2 target)
        {
            Prop prop = new Prop(category);
    
            // Cutlass pointer
            SPImage pointerImage = new SPImage(scene.TextureByName("pointer"));
            pointerImage.X = -pointerImage.Width / 2;
            pointerImage.Y = -pointerImage.Height / 2;
    
            SPSprite pointerSprite = new SPSprite();
            pointerSprite.X = target.X - pointerImage.Height / 1.5f;
            pointerSprite.Y = target.Y;
            pointerSprite.Rotation = SPMacros.SP_D2R(90);
            pointerSprite.AddChild(pointerImage);
            prop.AddChild(pointerSprite);
    
            SPTween tween = new SPTween(pointerSprite, 0.5f);
            tween.AnimateProperty("X", pointerSprite.X - 20);
            tween.Loop = SPLoopType.Reverse;
    
            // Text
            SPTextField label = new SPTextField(380, 56, "This is your pirate ship", scene.FontKey, 40);
            label.X = -label.Width / 2;
            label.HAlign = SPTextField.SPHAlign.Left;
            label.VAlign = SPTextField.SPVAlign.Top;
            label.Color = SPUtils.ColorFromColor(0xfcc30e);
    
            Prop textProp = new Prop(category);
            textProp.X = (pointerSprite.X - (label.Width - pointerImage.Height / 3)) + label.Width / 2;
            textProp.Y = pointerSprite.Y + pointerImage.Width / 2;
            textProp.AddChild(label);
            prop.AddChild(textProp);
    
            HintPackage package = new HintPackage(prop, tween);
            package.AddFlipProp(textProp);

            return package;
        }

        public static HintPackage ShipDoesntSinkPropWithScene(SceneController scene, int category, Vector2 origin, Vector2 target)
        {
            Prop prop = new Prop(category);
        
            // Navy ship
            SPSprite navyShipSprite = new SPSprite();
            navyShipSprite.X = origin.X;
            navyShipSprite.Y = origin.Y;
            prop.AddChild(navyShipSprite);
    
            SPImage navyShipImage = new SPImage(scene.TextureByName("ship-pf-navy_00"));
            navyShipImage.X = -navyShipImage.Width / 2;
            navyShipImage.Y = -navyShipImage.Height / 2;
            navyShipSprite.AddChild(navyShipImage);
    
            // Cannonballs
            Vector2 vector = target - origin;
            float cannonballAngle = Globals.VectorToAngle2(vector);
            float navyShipAngle = cannonballAngle;
    
            navyShipSprite.Rotation = navyShipAngle;

            SPTexture cannonballTexture = scene.TextureByName("single-shot_00");
    
            for (int i = 0; i < 5; ++i)
            {
                float scale = 1;
        
                switch (i) {
                    case 0:
                    case 4:
                        scale = 0.15625f;
                        break;
                    case 1:
                    case 3:
                        scale = 0.25f;
                        break;
                    case 2:
                        scale = 0.375f;
                        break;
                    default:
                        break;
                }
        
                float length = ((i + 1) * 0.2f) * vector.Length() - vector.Length() * 0.1f;
                Vector2 point = Globals.AngleToVector(cannonballAngle);
                point *= length;
                SPImage cannonballImage = new SPImage(cannonballTexture);
                cannonballImage.ScaleX = cannonballImage.ScaleY = scale;
                cannonballImage.X = navyShipSprite.X + point.X - cannonballImage.Width / 2;
                cannonballImage.Y = navyShipSprite.Y + point.Y - cannonballImage.Height / 2;
                prop.AddChild(cannonballImage);
            }
    
            // Explosion
            SPImage explosionImage = new SPImage(scene.TextureByName("explode_01"));
            explosionImage.X = target.X - explosionImage.Width / 2;
            explosionImage.Y = target.Y - explosionImage.Height / 2;
            prop.AddChild(explosionImage);

            return new HintPackage(prop, null);
        }

        public static HintPackage PointerHintWithScene(SceneController scene, Vector2 target, float radius, string text, bool animated)
        {
            Prop prop = new Hint(0, target);
    
            // Get angle from target to center of the screen (this should have the favourable side-effect of placing the hint away from the edges of the screen)
            Vector2 vector = new Vector2(target.X - scene.ViewWidth / 2, target.Y - scene.ViewHeight / 2);
            float vectorAngle = Globals.VectorToAngle2(vector);
            float pointerAngle = vectorAngle + SPMacros.PI_HALF;
            int dir = (vectorAngle < 0) ? 1 : -1;
    
            if (pointerAngle > SPMacros.PI)
                pointerAngle -= SPMacros.TWO_PI;
            else if (pointerAngle < -SPMacros.PI)
                pointerAngle += SPMacros.TWO_PI;
    
            // Rotate pointer by this angle
            SPImage pointerImage = new SPImage(scene.TextureByName("pointer"));
            pointerImage.X = -pointerImage.Width / 2;
            pointerImage.Y = -pointerImage.Height / 2;
    
            SPSprite pointerSprite = new SPSprite();
            pointerSprite.X = target.X;
            pointerSprite.Y = target.Y;
            pointerSprite.Rotation = pointerAngle;
            pointerSprite.AddChild(pointerImage);
    
            // Give pointer some clearance from its target
            pointerSprite.X -= (radius + pointerImage.Height / 2) * (float)Math.Cos(vectorAngle);
            pointerSprite.Y -= (radius + pointerImage.Height / 2) * (float)Math.Sin(vectorAngle);
    
            // Place textfield at base of cutlass pointer
            int quadrant = 1;
            float absPointerAngle = Math.Abs(vectorAngle);
            Vector2 pointerBase = Vector2.Zero;
    
            // Anti-clockwise positive rotation coordinate system because +y is down the screen in Sparrow
            if (vectorAngle > 0)
                quadrant = (absPointerAngle < SPMacros.PI_HALF) ? 1 : 2;
            else
                quadrant = (absPointerAngle < SPMacros.PI_HALF) ? 4 : 3;
    
            if (quadrant == 2 || quadrant == 4) 
            {
                // Bottom right: Q2,Q4
                pointerBase = pointerImage.LocalToGlobal(new Vector2(pointerImage.Width, pointerImage.Height));
            }
            else
            {
                // Bottom left: Q1,Q3
                pointerBase = pointerImage.LocalToGlobal(new Vector2(0, pointerImage.Height));
            }
    
            float textFieldMultiplier = 0, textFieldFactorX = 0;
    
            switch (quadrant)
            {
                case 1:
                    textFieldMultiplier = -0.9f;
                    break;
                case 2:
                    textFieldMultiplier = -0.7f;
                    textFieldFactorX = 1f;
                    break;
                case 3:
                    textFieldMultiplier = 0;
                    textFieldFactorX = 0.5f;
                    break;
                case 4:
                    textFieldMultiplier = -0.15f;
                    break;
                default:
                    textFieldMultiplier = 0;
                    break;
            }

            textFieldMultiplier *= 1.5f;
    
            // Create with max width
            SPTextField textField = new SPTextField(512, 48, text, scene.FontKey, 36);
            textField.HAlign = SPTextField.SPHAlign.Center;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = SPUtils.ColorFromColor(0xfcc30e);
            textField.X = -textField.Width / 2;
    
            Prop textProp = new Prop(0);
            textProp.X = pointerBase.X + (textFieldFactorX * textField.TextBounds.Width) / 2;
            textProp.Y = pointerBase.Y + textFieldMultiplier * textField.Height;
            textProp.Tag = kHintTextPropTag;
            textProp.AddChild(textField);
    
            // Animate the pointer
            SPTween tween = null;
    
            if (animated)
            {
                tween = new SPTween(pointerSprite, 0.5f);
                tween.AnimateProperty("X", pointerSprite.X - dir * 20 * (float)Math.Cos(vectorAngle));
                tween.AnimateProperty("Y", pointerSprite.Y - dir * 20 * (float)Math.Sin(vectorAngle));
                tween.Loop = SPLoopType.Reverse;
            }
    
            prop.AddChild(pointerSprite);
            prop.AddChild(textProp);

            HintPackage package = new HintPackage(prop, tween);
            package.AddFlipProp(textProp);

            return package;
        }

        public static HintPackage PointerHintWithScene(SceneController scene, Vector2 target, SPDisplayObject movingTarget, float radius, string text, bool animated)
        {
            HintPackage package = HintHelper.PointerHintWithScene(scene, target, radius, text, animated);
            foreach (Prop prop in package.Props)
            {
                if (prop is Hint)
                    (prop as Hint).Target = movingTarget;
            }

            return package;
        }

        public static HintPackage HintWithScene(SceneController scene, Vector2 target, float radius, string text, Color color)
        {
            // Create with max width
            SPTextField textField = new SPTextField(512, 48, text, scene.FontKey, 36);
            textField.HAlign = SPTextField.SPHAlign.Center;
            textField.VAlign = SPTextField.SPVAlign.Top;
            textField.Color = color;
            textField.X = -textField.Width / 2;

            Prop textProp = new Prop(0);
            textProp.X = target.X;
            textProp.Y = target.Y + textField.Height;
            textProp.Tag = kHintTextPropTag;
            textProp.AddChild(textField);

            Prop prop = new Hint(0, target);
            prop.AddChild(textProp);

            HintPackage package = new HintPackage(prop, null);
            package.AddFlipProp(textProp);

            return package;
        }

        public static HintPackage HintWithScene(SceneController scene, Vector2 target, SPDisplayObject movingTarget, float radius, string text, Color color)
        {
            HintPackage package = HintHelper.HintWithScene(scene, target, radius, text, color);
            foreach (Prop prop in package.Props)
            {
                if (prop is Hint)
                    (prop as Hint).Target = movingTarget;
            }

            return package;
        }
    }
}
