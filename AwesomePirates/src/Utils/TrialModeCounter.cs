using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices; 

namespace AwesomePirates
{
    class TrialModeCounter : GameComponent
    {
        bool trialMode = true;
        bool trialModeTimeout = false;
        bool showGuide = false;
        Stopwatch stopWatch;

        const int TimeOut = 8;
        Game game;

        public TrialModeCounter(Game game)
            : base(game)
        {
            this.game = game;
        }

        public bool IsTrialMode
        {
            get { return (trialMode) ? Guide.IsTrialMode : false; }
        }

        public override void Initialize()
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Guide.IsTrialMode)
            {
                Enabled = false;
                return;
            }


            if (stopWatch.Elapsed.Minutes >= TimeOut && !trialModeTimeout)
            {
                trialModeTimeout = true;
                showGuide = true;
            }

            if (showGuide && !Guide.IsVisible)
            {
                try
                {
                    Guide.BeginShowMessageBox("Time Expired", "The Trial for this community game has\r\nended. You can restart the demo to play\r\nagain, or unlock the game below.\r\n\r\nWould you like to unlock the full game?",
                        new String[] { "Exit Game", "Unlock Game" }, 0,
                        MessageBoxIcon.Alert,
                        result =>
                        {
                            int? choice = Guide.EndShowMessageBox(result);

                            if (choice.HasValue && choice.Value == 1)
                            {
                                trialMode = false;
                                Enabled = false;
                            }
                            else
                                game.Exit();

                        }, null);

                    showGuide = false;
                }
                catch
                {
                }
            }
        }
    }
}
