using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using EasyStorage;

namespace AwesomePirates
{
    class FileManagerWin : FileManager
    {
        protected override void LoadLocal(PlayerIndex playerIndex, string containerName, string fileName, bool async, FileAction loadAction)
        {
            LoadGlobal(containerName, fileName, async, loadAction);
        }

        protected override void SaveLocal(PlayerIndex playerIndex, string containerName, string fileName, bool async, FileAction saveAction)
        {
            SaveGlobal(containerName, fileName, async, saveAction);
        }
    }
}
