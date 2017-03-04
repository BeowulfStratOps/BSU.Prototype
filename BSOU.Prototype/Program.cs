using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BSU.Sync;
using Squirrel;

namespace BSU.Prototype
{
    static class Program
    {
        internal static Server LoadedServer;
        internal static bool ServerLoadeded = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Task.Run(async () =>
            {
                using (var mgr = new UpdateManager($"http://u.beowulfso.com/updates/{Properties.Settings.Default.UpdateChannel}"))
                {
                    await mgr.UpdateApp();
                }
            }).GetAwaiter().GetResult();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}
