using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using SparrowXNA;

namespace AwesomePirates
{
    // Custom ContentManagers (finer unload granularity): http://blogs.msdn.com/b/shawnhar/archive/2007/03/09/contentmanager-readasset.aspx

    class TextureManager : IDisposable
    {
        public TextureManager(string atlasBasePath, string textureBasePath, ContentManager cm)
        {
            mAtlasBasePath = (atlasBasePath == null) ? "" : atlasBasePath;
            mTextureBasePath = (textureBasePath == null) ? "" : textureBasePath;
            mCm = cm;
        }

        #region Fields
        private bool mIsDisposed = false;
        private string mAtlasBasePath;
        private string mTextureBasePath;
        private ContentManager mCm;
        private Dictionary<string, SPTextureAtlas> mAtlases = new Dictionary<string, SPTextureAtlas>(20);
        private Dictionary<string, SPTexture> mTextureCache = new Dictionary<string, SPTexture>(20);
        private Dictionary<string, List<SPTexture>> mTextureArrayCache = new Dictionary<string, List<SPTexture>>(20);
        private Dictionary<string, Effect> mEffects = new Dictionary<string, Effect>(15);
        #endregion

        #region Methods
        public void CacheTexture(SPTexture texture, string name)
        {
            if (texture == null || name == null)
                throw new ArgumentNullException("Cached texture and name cannot be null.");

            mTextureCache[name] = texture;
        }

        public void CacheTextures(List<SPTexture> textures, string name)
        {
            if (textures == null || name == null)
                throw new ArgumentNullException("Cached textures and name cannot be null.");

            mTextureArrayCache[name] = textures;
        }

        public void AddAtlas(string name, string path = null)
        {
            // Doesn't overwrite existing elements
            if (mAtlases.ContainsKey(name))
                return;

            List<Dictionary<string, object>> subtextures = AtlasParser.AtlasSubtextures(mAtlasBasePath + ((path != null) ? path : name));

            if (subtextures.Count == 0)
                throw new ArgumentException("Attempt to add empty texture atlas.");

            Dictionary<string, object> imagePath = subtextures[0];
            string texturePath = mTextureBasePath + imagePath["imagePath"] as string;

            if (texturePath.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase))
                texturePath = texturePath.Substring(0, texturePath.Length - ".png".Length);

            Texture2D atlasTexture2D = mCm.Load<Texture2D>(texturePath);
            subtextures.RemoveAt(0);
            SPTextureAtlas atlas = new SPTextureAtlas(subtextures, new SPTexture(atlasTexture2D));
            mAtlases.Add(name, atlas);

            /*
            AtlasData atlasData = mCm.Load<AtlasData>(mBasePath + ((path != null) ? path : name));
            Texture2D atlasTexture2D = mCm.Load<Texture2D>(atlasData.ImagePath);
            SPTextureAtlas atlas = new SPTextureAtlas(atlasData, new SPTexture(atlasTexture2D));
            mAtlases.Add(name, atlas);
             * */
        }

        public void RemoveAtlas(string name)
        {
            mAtlases.Remove(name);
        }

        public SPTexture TextureByName(string name, bool cached = true)
        {
            if (name == null)
                throw new ArgumentNullException("Texture name cannot be null.");

            SPTexture texture = null;

            do
            {
                if (cached)
                {
                    if (mTextureCache.ContainsKey(name))
                    {
                        texture = mTextureCache[name];
                        break;
                    }
                }

                foreach (SPTextureAtlas atlas in mAtlases.Values)
                {
                    texture = atlas.TextureByName(name);

                    if (texture != null)
                    {
                        if (cached)
                            mTextureCache[name] = texture;
                        break;
                    }
                }
            } while (false);

            return texture;
        }

        public List<SPTexture> TexturesStartingWith(string name, bool cached = true)
        {
            if (name == null)
                throw new ArgumentNullException("Texture array name prefix cannot be null.");

            List<SPTexture> textures = null;

            do
            {
                if (cached)
                {
                    if (mTextureArrayCache.ContainsKey(name))
                    {
                        textures = mTextureArrayCache[name];
                        break;
                    }
                }

                foreach (SPTextureAtlas atlas in mAtlases.Values)
                {
                    textures = atlas.TexturesStartingWith(name);

                    if (textures.Count != 0)
                    {
                        if (cached)
                            mTextureArrayCache[name] = textures;
                        break;
                    }
                }
            } while (false);

            return textures;
        }

        public void PurgeTextureCache(List<string> names = null)
        {
            if (names == null)
                mTextureCache.Clear();
            else
            {
                foreach (string name in names)
                    mTextureCache.Remove(name);
            }
        }

        public void PurgeTextureArrayCache(List<string> namePrefixes = null)
        {
            if (namePrefixes == null)
                mTextureArrayCache.Clear();
            else
            {
                foreach (string name in namePrefixes)
                    mTextureArrayCache.Remove(name);
            }
        }

        public Effect EffectForKey(string key)
        {
            Effect effect = null;

            if (mEffects != null && key != null)
                mEffects.TryGetValue(key, out effect);

            return effect;
        }

        public void AddEffect(string key, Effect effect)
        {
            if (key != null && effect != null && !mEffects.ContainsKey(key))
                mEffects.Add(key, effect);
        }

        public void RemoveEffect(string key)
        {
            if (key != null && mEffects.ContainsKey(key))
                mEffects.Remove(key);
        }

        public void PurgeEffects()
        {
            mEffects.Clear();
        }

        public void Unload()
        {
            if (mAtlases != null)
            {
                foreach (SPTextureAtlas atlas in mAtlases.Values)
                    atlas.Dispose();
            }

            if (mTextureCache != null)
            {
                foreach (SPTexture texture in mTextureCache.Values)
                    texture.Dispose();
            }

            if (mTextureArrayCache != null)
            {
                foreach (List<SPTexture> textureArray in mTextureArrayCache.Values)
                {
                    foreach (SPTexture texture in textureArray)
                        texture.Dispose();
                }
            }

            if (mCm != null)
                mCm.Unload();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                if (disposing)
                {
                    Unload();
                    mCm = null;
                    mAtlases = null;
                    mTextureCache = null;
                    mTextureArrayCache = null;
                    mAtlasBasePath = null;
                    mTextureBasePath = null;
                }

                mIsDisposed = true;
            }
        }

        ~TextureManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
