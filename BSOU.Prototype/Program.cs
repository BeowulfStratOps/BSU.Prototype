﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BSU.Sync;
using Squirrel;
using System.IO;
using NLog;

namespace BSU.Prototype
{
    static class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        internal static Server LoadedServer;
        internal static bool ServerLoadeded = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Don't update it if we are running with the debugger attached
            // TODO: Maybe change this to detect a command line argument instead 
            if (!SkipUpdateCheck(args))
            {
                using (var mgr = new UpdateManager(
                    $"http://u.beowulfso.com/prototype/{Properties.Settings.Default.UpdateChannel}"))
                {
                    // Note, in most of these scenarios, the app exits after this method
                    // completes!
                    SquirrelAwareApp.HandleEvents(
                        onInitialInstall: v => mgr.CreateShortcutForThisExe(),
                        onAppUpdate: v => mgr.CreateShortcutForThisExe(),
                        onAppUninstall: v => mgr.RemoveShortcutForThisExe());

                    Task.Run(async () =>
                        {
                            var updates = await mgr.CheckForUpdate();
                            if (updates.ReleasesToApply.Any())
                            {
                                var lastVersion = updates.ReleasesToApply.OrderBy(x => x.Version).Last();

                                logger.Info($"Update available, applying version {lastVersion.Version}");

                                await mgr.DownloadReleases(updates.ReleasesToApply);
                                await mgr.ApplyReleases(updates);

                                logger.Info($"Update applied, restarting");
                                UpdateManager.RestartApp();
                            }

                        })
                        .GetAwaiter()
                        .GetResult();

                }
            }
            else
            {
                logger.Info("Skipped update check (either due to argument or debugger attached)");
            }


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }

        private static bool SkipUpdateCheck(string[] args)
        {
            return System.Diagnostics.Debugger.IsAttached || args.Any("noupdate".Contains);
        }
    }
}
