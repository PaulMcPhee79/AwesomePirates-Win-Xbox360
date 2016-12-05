using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace AwesomePirates
{
    interface IInteractable
    {
        uint InputFocus { get; }
        void DidGainFocus();
        void WillLoseFocus();
        void Update(GamePadState gpState, KeyboardState kbState);
    }
}
