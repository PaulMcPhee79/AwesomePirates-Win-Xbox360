using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class SKShipDeckContainer : Prop, IInteractable
    {
        private const float kDefaultHeight = 120f;

        public SKShipDeckContainer(int category, GameMode mode)
            : base(category)
        {
            if (mode != GameMode.SKFFA && mode != GameMode.SK2v2)
                throw new ArgumentException("SKShipDeckContainer only supports multiplayer game modes.");
            mAdvanceable = true;
            mAwaitingPlayers = true;
            mMode = mode;
            mCompiled = false;
            mShipDecks = new List<SKShipDeck>(4);
            mShipDecksDict = new Dictionary<SKTeamIndex, SKShipDeck>(4, SKTeamIndexComparer.Instance);
            SetupProp();
        }

        #region Fields
        private bool mCompiled;
        private bool mAwaitingPlayers;
        private GameMode mMode;
        private SPSprite mGraphicsLayer;
        private SPSprite mTextLayer;
        private SPSprite mCanvas;
        private List<SKShipDeck> mShipDecks;
        private Dictionary<SKTeamIndex, SKShipDeck> mShipDecksDict;
        #endregion

        #region Properties
        public uint InputFocus { get { return InputManager.HAS_FOCUS_DECK; } }
        #endregion

        #region Methods
        public bool AwaitingPlayers
        {
            get { return mAwaitingPlayers; }
            set
            {
                mAwaitingPlayers = value;

                if (mShipDecks != null)
                {
                    foreach (SKShipDeck deck in mShipDecks)
                        deck.AwaitingPlayer = deck.AwaitingPlayer && value;
                }
            }
        }
        private Vector2 ShipDeckOrigin(SKTeamIndex teamIndex)
        {
            Vector2 origin = Vector2.Zero;

            switch (mMode)
            {
                case GameMode.SKFFA:
                    {
                        float hudWidth = 4 * 282f - 3 * 56f;
                        origin.X = (mScene.ViewWidth - hudWidth) / 2;
                        origin.X += (int)teamIndex * (282f - 56f);
                        origin.Y = mScene.ViewHeight - 102f;
                    }
                    break;
                case GameMode.SK2v2:
                    {
                        float hudWidth = 2 * 282f - 56f;
                        origin.X = (mScene.ViewWidth - hudWidth) / 2;
                        origin.X += (int)teamIndex * (282f - 56f);
                        origin.Y = mScene.ViewHeight - 102f;
                    }
                    break;
            }

            return origin;
        }

        protected override void SetupProp()
        {
            if (mCanvas != null)
                return;

            mCanvas = new SPSprite();
            AddChild(mCanvas);

            mGraphicsLayer = new SPSprite();
            mCanvas.AddChild(mGraphicsLayer);

            mTextLayer = new SPSprite();
            mCanvas.AddChild(mTextLayer);

            Y = kDefaultHeight;
        }

        public SKShipDeck ShipDeckForIndex(SKTeamIndex index)
        {
            if (mShipDecksDict != null && mShipDecksDict.ContainsKey(index))
                return mShipDecksDict[index];
            else
                return null;
        }

        public void AddShipDeck(SKShipDeck shipDeck)
        {
            if (mCompiled || shipDeck == null || mShipDecks == null || mShipDecksDict == null || mShipDecksDict.ContainsKey(shipDeck.TeamIndex))
                return;

            mShipDecks.Add(shipDeck);
            mShipDecksDict.Add(shipDeck.TeamIndex, shipDeck);
        }

        public void Compile()
        {
            if (mCompiled || mShipDecks == null || mShipDecks.Count == 0)
                return;

            // Position the new shipDeck based on its PlayerIndex.
            foreach (SKShipDeck deck in mShipDecks)
                deck.Origin = ShipDeckOrigin(deck.TeamIndex);

            int numChildren = mShipDecks[0].NumChildren;
            for (int i = 0; i < numChildren; ++i)
            {
                foreach (SKShipDeck deck in mShipDecks)
                {
                    if (deck.NumChildren == 0)
                        continue;

                    SPDisplayObject child = deck.ChildAtIndex(0);

                    if (child == null)
                        continue;

                    // Adjust their positions to match their new parent (i.e. us).
                    child.X += deck.X;
                    child.Y += deck.Y;

                    // Layer the shipDeck's children to optimize drawing.
                    if (child is SPTextField)
                        mTextLayer.AddChild(child);
                    else
                        mGraphicsLayer.AddChild(child);
                }
            }

            mCompiled = true;
        }

        public void EnableCombatControls(bool enable)
        {
            if (mShipDecks == null)
                return;

            foreach (SKShipDeck deck in mShipDecks)
                deck.EnableCombatControls(enable);
        }

        public void EnableCombatControls(bool enable, PlayerIndex playerIndex)
        {
            if (mShipDecks == null)
                return;

            foreach (SKShipDeck deck in mShipDecks)
                deck.EnableCombatControls(enable, playerIndex);
        }

        public void ExtendOverTime(float duration)
        {
            mScene.Juggler.RemoveTweensWithTarget(this);

            SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("Y", 0);
            mScene.Juggler.AddObject(tween);

            Visible = true;
        }

        public void RetractOverTime(float duration)
        {
            mScene.Juggler.RemoveTweensWithTarget(this);

            SPTween tween = new SPTween(this, duration);
            tween.AnimateProperty("Y", kDefaultHeight);
            tween.AddEventListener(SPTween.SP_EVENT_TYPE_TWEEN_COMPLETED, (SPEventHandler)OnRetracted);
            mScene.Juggler.AddObject(tween);
        }

        private void OnRetracted(SPEvent ev)
        {
            Visible = false;
        }

        public void DidGainFocus() { }

        public void WillLoseFocus() { }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            if (mShipDecks == null)
                return;

            ControlsManager cm = ControlsManager.CM;

            bool awaitingPlayers = false;
            foreach (SKShipDeck shipDeck in mShipDecks)
            {
                if (mAwaitingPlayers && shipDeck.AwaitingPlayer)
                    awaitingPlayers = true;

                shipDeck.Update(gpState, kbState);
            }

            if (awaitingPlayers)
            {
                for (PlayerIndex playerIndex = PlayerIndex.One; playerIndex <= PlayerIndex.Four; ++playerIndex)
                {
                    if (mMode == GameMode.SKFFA)
                    {
                        if (cm.DidButtonDepress(Buttons.A, playerIndex))
                            mScene.SKManager.AddToTeam((SKTeamIndex)playerIndex, playerIndex);
                    }
                    else
                    {
                        if (cm.DidButtonDepress(Buttons.B, playerIndex))
                            mScene.SKManager.AddToTeam(SKTeamIndex.Red, playerIndex);
                        else if (cm.DidButtonDepress(Buttons.X, playerIndex))
                            mScene.SKManager.AddToTeam(SKTeamIndex.Blue, playerIndex);
                    }
                }
            }
            else
                AwaitingPlayers = awaitingPlayers;
        }

        public override void AdvanceTime(double time)
        {
            foreach (SKShipDeck deck in mShipDecks)
                deck.AdvanceTime(time);
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mScene.UnsubscribeToInputUpdates(this);
                        mScene.Juggler.RemoveTweensWithTarget(this);

                        if (mShipDecks != null)
                        {
                            foreach (SKShipDeck shipDeck in mShipDecks)
                                shipDeck.Dispose();
                            mShipDecks = null;
                        }

                        mShipDecksDict = null;
                    }
                }
                catch (Exception)
                {
                    // Ignore
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
