using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    public enum GameMode
    {
        Career = 0,
        SKFFA,
        SK2v2
    }

    class GameModeManager : SPEventDispatcher
    {
        public GameModeManager(GameMode mode = GameMode.Career)
        {
            mMode = mode;
        }

        #region Fields
        private GameMode mMode;
        #endregion

        #region Properties
        public GameMode Mode { get { return mMode; } set { mMode = value; } }
        #endregion

        #region Methods

        #endregion
    }
}
