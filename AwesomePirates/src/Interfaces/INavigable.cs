using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    interface INavigable
    {
        uint NavMap { get; set; }
        SPDisplayObject CurrentNav { get; }
        void ResetNav();
        void MovePrevNav();
        void MoveNextNav();
    }
}
