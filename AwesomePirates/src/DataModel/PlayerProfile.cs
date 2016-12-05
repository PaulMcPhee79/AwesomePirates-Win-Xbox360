using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class PlayerProfile
    {
        public PlayerProfile(GameStats playerStats, PlayerIndex playerIndex)
        {
            mHasPreviouslyChosenSaveDevice = mChoseNotToSave = mProgressLoaded = false;
            mGamerTag = playerStats.Alias;
            mPlayerStats = playerStats;
            mPlayerIndex = playerIndex;
            mPresenceValue = 0;
            mPresenceMode = GamerPresenceMode.None;
        }

        #region Fields
        private bool mHasPreviouslyChosenSaveDevice;
        private bool mChoseNotToSave;
        private bool mProgressLoaded;
        private int mPresenceValue;
        private string mGamerTag;
        private GamerPresenceMode mPresenceMode;
        private PlayerIndex mPlayerIndex;
        private GameStats mPlayerStats;

        private Texture2D mGamerTexture;
        private SPTexture mGamerPicture;
        #endregion

        #region Properties
        public bool HasPreviouslyChosenSaveDevice { get { return mHasPreviouslyChosenSaveDevice; } set { mHasPreviouslyChosenSaveDevice = value; } }
        public bool ChoseNotToSave { get { return mChoseNotToSave; } set { mChoseNotToSave = value; } }
        public bool ProgressLoaded { get { return mProgressLoaded; } set { mProgressLoaded = value; } }
        public int PresenceValue
        {
            get { return mPresenceValue; }
            set
            {
                if (value == mPresenceValue)
                    return;
                mPresenceValue = value;

                SignedInGamer sigGamer = SigGamer;
                if (sigGamer != null)
                    sigGamer.Presence.PresenceValue = value;
            }
        }
        public GamerPresenceMode PresenceMode
        {
            get { return mPresenceMode; }
            set
            {
                if (value == mPresenceMode)
                    return;
                mPresenceMode = value;

                SignedInGamer sigGamer = SigGamer;
                if (sigGamer != null)
                    sigGamer.Presence.PresenceMode = value;
            }
        }
        public string GamerTag { get { return mGamerTag != null ? mGamerTag : mPlayerStats.Alias; } set { mGamerTag = value; } }
        public PlayerIndex PlayerIndex { get { return mPlayerIndex; } }
        public GameStats PlayerStats { get { return mPlayerStats; } }
        public SignedInGamer SigGamer { get { return SignedInGamer.SignedInGamers[mPlayerIndex]; } }
        public SPTexture GamerPicture { get { return mGamerPicture; } }
        #endregion

        #region Methods
        public void Reset(GameStats playerStats)
        {
            mHasPreviouslyChosenSaveDevice = mChoseNotToSave = mProgressLoaded = false;
            mPlayerStats = playerStats;
        }

        public void PrepareForGamerPictureRefresh(SPTexture defaultPic)
        {
            mGamerPicture = defaultPic;
        }

        public void RefreshGamerPicture(SPTexture defaultPic)
        {
            mGamerPicture = null;

            if (mGamerTexture != null)
            {
                mGamerTexture.Dispose();
                mGamerTexture = null;
            }

            SignedInGamer sigGamer = SigGamer;
            if (sigGamer != null)
            {
                try
                {
                    mGamerTexture = Texture2D.FromStream(GameController.GC.GraphicsDevice, sigGamer.GetProfile().GetGamerPicture());
                    if (mGamerTexture != null)
                        mGamerPicture = new SPTexture(mGamerTexture);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("PlayerProfile::RefreshGamerPicture: " + e.Message);
                }
            }

            if (mGamerPicture == null)
                mGamerPicture = defaultPic;
        }
        #endregion
    }
}
