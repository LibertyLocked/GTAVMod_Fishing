using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAVMod_Fishing
{
    class Globals
    {
        public const string _SCRIPT_VERSION = "0.4.1";

#if DEBUG
        public static bool DebugMode = true;
#else
        public static bool DebugMode = false;
#endif
        public static int DebugIndex = 0;
        public static bool FishAnywhere = false;
    }
}
