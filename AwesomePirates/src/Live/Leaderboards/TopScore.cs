using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using AwesomePirates;

namespace SpynDoctor
{
    static class TopScore
    {
        public static bool IsLogging { get; set; }
        public static ConsoleView View { get; set; }

        public static void Write(string msg, Color? color = null)
        {
#if WINDOWS && DEBUG
            Debug.Write(msg);
#endif
            if (View != null)
                View.Write(msg, color);
        }

        public static void WriteLine(string msg, Color? color = null)
        {
#if DEBUG
            Debug.WriteLine(msg);
#endif
            if (View != null)
                View.WriteLine(msg, color);
        }
    }
}
