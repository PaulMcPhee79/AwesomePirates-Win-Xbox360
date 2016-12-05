using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class ResourceServer : Prop
    {
        public ResourceServer(int category, string key = null)
            : base (category)
        {
            mKey = key;
            mPoolIndex = -1;
            mPoolIndices = null;
            mTweens = null;
            mMovies = null;
            mDisplayObjects = null;
            mMiscResources = null;
            mClient = new WeakReference(null);
        }

        #region Fields
        private string mKey;
        private int mPoolIndex;
        private int[] mPoolIndices;
        private Dictionary<uint, SPTween> mTweens;
        private Dictionary<uint, SPMovieClip> mMovies;
        private Dictionary<uint, SPDisplayObject> mDisplayObjects;
        private Dictionary<uint, object> mMiscResources;
        private WeakReference mClient; // IResourceClient
        #endregion

        #region Properties
        public int PoolIndex { get { return mPoolIndex; } set { mPoolIndex = value; } }
        public string Key { get { return mKey; } }
        public IResourceClient Client
        {
            get { return mClient.Target as IResourceClient; }
            set
            {
                if (mClient == null)
                    return;
                if (value != null && mClient.Target != null)
                    throw new InvalidOperationException("ResourceServer may only have one client at a time.");
                mClient.Target = value;
            }
        }
        #endregion

        #region Methods
        public void SetPoolIndexCapacity(int capacity)
        {
            if (mPoolIndices != null || capacity <= 1)
                return;

            mPoolIndices = new int[capacity-1];

            for (int i = 0; i < capacity-1; ++i)
                mPoolIndices[i] = -1;
        }

        public int GetPoolIndex(int index)
        {
            if (index == 0)
                return PoolIndex;
            else if (mPoolIndices == null || index <= 0 || index > mPoolIndices.Length)
                return -1;
            else
                return mPoolIndices[index-1];
        }

        public void SetPoolIndex(int index, int value)
        {
            if (index == 0)
                PoolIndex = index;
            else if (mPoolIndices != null && index > 0 && index <= mPoolIndices.Length)
                mPoolIndices[index-1] = value;
        }

        public void ReassignScene(SceneController scene)
        {
            if (scene != mScene)
                mScene = scene;
        }

        public void OnTweenStarted(SPEvent ev)
        {
            SPTween tween = ev.CurrentTarget as SPTween;
            Client.ResourceEventFiredWithKey(tween.Tag, SPTween.SP_EVENT_TYPE_TWEEN_STARTED, tween);
        }

        public void OnTweenCompleted(SPEvent ev)
        {
            SPTween tween = ev.CurrentTarget as SPTween;
            Client.ResourceEventFiredWithKey(tween.Tag, SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, tween);
        }

        public void OnMovieCompleted(SPEvent ev)
        {
            SPMovieClip movie = ev.CurrentTarget as SPMovieClip;
            Client.ResourceEventFiredWithKey(movie.Tag, SPMovieClip.SP_EVENT_TYPE_MOVIE_COMPLETED, movie);
        }

        public void AddTween(SPTween tween, uint key)
        {
            if (mTweens == null)
                mTweens = new Dictionary<uint, SPTween>();
            tween.Tag = key;
            mTweens.Add(key, tween);
        }

        public bool StartTweenForKey(uint key)
        {
            bool started = false;
            SPTween tween;
            mTweens.TryGetValue(key, out tween);
    
            if (tween != null)
            {
                started = true;
                tween.Reset();
                mScene.Juggler.AddObject(tween);
            }
    
            return started;
        }

        public void StopTweenForKey(uint key)
        {
            SPTween tween;
            mTweens.TryGetValue(key, out tween);

            if (tween != null)
                mScene.Juggler.RemoveObject(tween);
        }

        public void AddMovie(SPMovieClip movie, uint key)
        {
            if (mMovies == null)
                mMovies = new Dictionary<uint, SPMovieClip>();
            mMovies.Add(key, movie);
            AddDisplayObject(movie, key);
        }

        public void AddDisplayObject(SPDisplayObject displayObject, uint key)
        {
            if (mDisplayObjects == null)
                mDisplayObjects = new Dictionary<uint, SPDisplayObject>();
            displayObject.Tag = key;
            mDisplayObjects.Add(key, displayObject);
        }

        public SPDisplayObject RemoveDisplayObjectForKey(uint key)
        {
            SPDisplayObject displayObject;

            if (mMovies.ContainsKey(key))
            {
                displayObject = mMovies[key] as SPMovieClip;
                mScene.Juggler.RemoveObject(displayObject as SPMovieClip);
                mMovies.Remove(key);
            }

            mDisplayObjects.TryGetValue(key, out displayObject);

            if (displayObject != null)
            {
                displayObject.RemoveFromParent();
                mDisplayObjects.Remove(key);
            }

            return displayObject;
        }

        public SPDisplayObject DisplayObjectForKey(uint key)
        {
            SPDisplayObject displayObject;
            mDisplayObjects.TryGetValue(key, out displayObject);
            return displayObject;
        }

        public void AddMiscResource(object resource, uint key)
        {
            if (mMiscResources == null)
                mMiscResources = new Dictionary<uint, object>();
            mMiscResources.Add(key, resource);
        }

        public object RemoveMiscResourceForKey(uint key)
        {
            object miscResource;
            mMiscResources.TryGetValue(key, out miscResource);

            if (mMiscResources != null)
                mMiscResources.Remove(key);
            return miscResource;
        }

        public object MiscResourceForKey(uint key)
        {
            object miscResource;
            mMiscResources.TryGetValue(key, out miscResource);
            return miscResource;
        }

        public void Reset()
        {
            if (mMovies != null)
            {
                foreach (KeyValuePair<uint, SPMovieClip> kvp in mMovies)
                {
                    kvp.Value.CurrentFrame = 0;
                    kvp.Value.Pause();
                }
            }

            if (mDisplayObjects != null)
            {
                foreach (KeyValuePair<uint, SPDisplayObject> kvp in mDisplayObjects)
                    kvp.Value.RemoveFromParent();
            }

            Client = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mTweens != null)
                        {
                            foreach (KeyValuePair<uint, SPTween> kvp in mTweens)
                                mScene.Juggler.RemoveObject(kvp.Value);
                            mTweens = null;
                        }

                        if (mMovies != null)
                        {
                            foreach (KeyValuePair<uint, SPMovieClip> kvp in mMovies)
                                mScene.Juggler.RemoveObject(kvp.Value);
                            mMovies = null;
                        }

                        if (mDisplayObjects != null)
                        {
                            foreach (KeyValuePair<uint, SPDisplayObject> kvp in mDisplayObjects)
                                kvp.Value.RemoveFromParent();
                            mDisplayObjects = null;
                        }

                        mPoolIndices = null;
                        mMiscResources = null;
                        mClient = null;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion
    }
}
