using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AwesomePirates
{
    class SKPup
    {
        public const int PUP_KEY_COUNT = 8;

        public const uint PUP_SEA_OF_LAVA = (1 << 0);
        public const uint PUP_GHOST_SHIP = (1 << 1);
        public const uint PUP_HAND_OF_DAVY = (1 << 2);
        public const uint PUP_TEMPEST = (1 << 3);
        public const uint PUP_NET = (1 << 4);
        public const uint PUP_BRANDY_SLICK = (1 << 5);
        public const uint PUP_POWDER_KEG = (1 << 6);
        public const uint PUP_HEALTH = (1 << 7);

        private const float PUP_WGT_SEA_OF_LAVA = 0.1f;
        private const float PUP_WGT_GHOST_SHIP = 0.1f;
        private const float PUP_WGT_HAND_OF_DAVY = 0.1f;
        private const float PUP_WGT_TEMPEST = 0.1f;
        private const float PUP_WGT_NET = 0.1f;
        private const float PUP_WGT_BRANDY_SLICK = 0.1f;
        private const float PUP_WGT_POWDER_KEG = 0.1f;
        private const float PUP_WGT_HEALTH = 0.2f;

        public static uint[] AllKeys
        {
            get
            {
                return new uint[]
                {
                    PUP_SEA_OF_LAVA,
                    PUP_GHOST_SHIP,
                    PUP_HAND_OF_DAVY,
                    PUP_TEMPEST,
                    PUP_NET,
                    PUP_BRANDY_SLICK,
                    PUP_POWDER_KEG,
                    PUP_HEALTH
                };
            }
        }

        public static float[] AllWeightings
        {
            get
            {
                return new float[]
                {
                    PUP_WGT_SEA_OF_LAVA,
                    PUP_WGT_GHOST_SHIP,
                    PUP_WGT_HAND_OF_DAVY,
                    PUP_WGT_TEMPEST,
                    PUP_WGT_NET,
                    PUP_WGT_BRANDY_SLICK,
                    PUP_WGT_POWDER_KEG,
                    PUP_WGT_HEALTH
                };
            }
        }

        public static uint[] LootKeys
        {
            get
            {
                return new uint[]
                {
                    PUP_SEA_OF_LAVA,
                    PUP_GHOST_SHIP,
                    PUP_HAND_OF_DAVY,
                    PUP_TEMPEST,
                    PUP_NET,
                    PUP_BRANDY_SLICK,
                    PUP_POWDER_KEG,
                    PUP_HEALTH,
                    PUP_HEALTH
                };
            }
        }

        public static float[] LootWeightings
        {
            get
            {
                return new float[]
                {
                    PUP_WGT_SEA_OF_LAVA,
                    PUP_WGT_GHOST_SHIP,
                    PUP_WGT_HAND_OF_DAVY,
                    PUP_WGT_TEMPEST,
                    PUP_WGT_NET,
                    PUP_WGT_BRANDY_SLICK,
                    PUP_WGT_POWDER_KEG,
                    PUP_WGT_HEALTH,
                    PUP_WGT_HEALTH
                };
            }
        }

        public static int AmountForKey(uint key)
        {
            int amount = 0;

            switch (key)
            {
                case PUP_SEA_OF_LAVA: amount = 1; break;
                case PUP_GHOST_SHIP: amount = 1; break;
                case PUP_HAND_OF_DAVY: amount = 2; break;
                case PUP_TEMPEST: amount = 2; break;
                case PUP_NET: amount = 1; break;
                case PUP_BRANDY_SLICK: amount = 1; break;
                case PUP_POWDER_KEG: amount = 12; break;
                case PUP_HEALTH: amount = 20; break;
            }

            return amount;
        }

        public static float DurationForKey(uint key)
        {
            float duration = 0f;

            switch (key)
            {
                case PUP_SEA_OF_LAVA: duration = 6f; break;
                case PUP_GHOST_SHIP: duration = 20f; break;
                case PUP_HAND_OF_DAVY: duration = 20f; break;
                case PUP_TEMPEST: duration = 20f; break;
                case PUP_NET: duration = 25f; break;
                case PUP_BRANDY_SLICK: duration = 30f; break;
                case PUP_POWDER_KEG: duration = 0f; break;
                case PUP_HEALTH: duration = 0f; break;
            }

            return duration;
        }

        public static string TextureNameForKey(uint key)
        {
            string textureName = null;

            switch (key)
            {
                case PUP_SEA_OF_LAVA: textureName = "sk-pup-lava"; break;
                case PUP_GHOST_SHIP: textureName = "sk-pup-ghost"; break;
                case PUP_HAND_OF_DAVY: textureName = "sk-pup-davy"; break;
                case PUP_TEMPEST: textureName = "sk-pup-tempest"; break;
                case PUP_NET: textureName = "sk-pup-net"; break;
                case PUP_BRANDY_SLICK: textureName = "sk-pup-brandy"; break;
                case PUP_POWDER_KEG: textureName = "sk-pup-keg"; break;
                case PUP_HEALTH: textureName = "sk-pup-health"; break;
            }

            return textureName;
        }

        public static string SoundNameForKey(uint key)
        {
            string soundName = null;

            switch (key)
            {
                case PUP_SEA_OF_LAVA: soundName = "AshMolten"; break;
                case PUP_GHOST_SHIP: soundName = null; break;
                case PUP_HAND_OF_DAVY: soundName = "SKPupHod"; break;
                case PUP_TEMPEST: soundName = "SKPupTempest"; break;
                case PUP_NET: soundName = null; break;
                case PUP_BRANDY_SLICK: soundName = null; break;
                case PUP_POWDER_KEG: soundName = "SKPupKeg"; break;
                case PUP_HEALTH: soundName = "CrewCelebrate"; break;
            }

            return soundName;
        }

        public static Vector2 IconOffsetForKey(uint key)
        {
            Vector2 offset = Vector2.Zero;

            switch (key)
            {
                case PUP_SEA_OF_LAVA: offset = new Vector2(0f, -12f); break;
                case PUP_GHOST_SHIP: offset = new Vector2(0f, -7f); break;
                case PUP_HAND_OF_DAVY: offset = new Vector2(2, -6f); break;
                case PUP_TEMPEST: offset = new Vector2(-1f, 3f); break;
                case PUP_NET: offset = new Vector2(1f, -2f); break;
                case PUP_BRANDY_SLICK: offset = new Vector2(1f, 5f); break;
                case PUP_POWDER_KEG: offset = Vector2.Zero; break;
                case PUP_HEALTH: offset = new Vector2(0f, -9f); ; break;
            }

            return offset;
        }
    }
}
