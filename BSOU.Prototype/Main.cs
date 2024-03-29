﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BSU.Sync;
using System.IO;
using System.Diagnostics;
using System.Linq;
using NLog;
using BSU.Sync.FileTypes.BI;
using NLog.Targets;

namespace BSU.Prototype
{
    public partial class Main : Form
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        delegate void SetLoadButtonCallback(bool Status);
        delegate void SetSyncButtonCallback(bool Status);
        delegate void SetSyncUrlBoxCallBack(bool Status);
        delegate void SetProgressLabelsCallback(string text);

        public int ProgessValue { get; set; }

        private int FailedChanges = 0;

        public Main()
        {
            InitializeComponent();
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            logger.Info(string.Format("Version: {0}", Application.ProductVersion));
        }

        private void SetTextBoxes(bool Status)
        {
            if (this.SyncUrlBox.InvokeRequired)
            {
                SetSyncUrlBoxCallBack d = new SetSyncUrlBoxCallBack(SetTextBoxes);
                this.Invoke(d, new object[] { Status });
            }
            else
            {
                SyncUrlBox.Enabled = Status;
            }
            if (this.LocalPathBox.InvokeRequired)
            {
                SetSyncUrlBoxCallBack d = new SetSyncUrlBoxCallBack(SetTextBoxes);
                this.Invoke(d, new object[] { Status });
            }
            else
            {
                LocalPathBox.Enabled = Status;
            }
            if (this.btnDirectorySelect.InvokeRequired)
            {
                SetSyncUrlBoxCallBack d = new SetSyncUrlBoxCallBack(SetTextBoxes);
                this.Invoke(d, new object[] { Status });
            }
            else
            {
                btnDirectorySelect.Enabled = Status;
            }
        }
        private void SetLoadButton(bool Status)
        {
            if (this.btnLoad.InvokeRequired)
            {
                SetLoadButtonCallback d = new SetLoadButtonCallback(SetLoadButton);
                this.Invoke(d, new object[] { Status });
            }
            else
            {
                btnLoad.Enabled = Status;
            }
        }
        private void SetSyncButton(bool Status)
        {
            if (this.btnSync.InvokeRequired)
            {
                SetSyncButtonCallback d = new SetSyncButtonCallback(SetSyncButton);
                this.Invoke(d, new object[] { Status });
            }
            else
            {
                btnSync.Enabled = Status;
            }
        }

        private void SetProgressLabels(string text)
        {
            if (progressLabel1.InvokeRequired || progressLabel2.InvokeRequired)
            {
                var d = new SetProgressLabelsCallback(SetProgressLabels);
                Invoke(d, text);
            }
            else
            {
                progressLabel1.Text = text;
                progressLabel2.Text = text;
            }
        }

        private string GetSyncUrl()
        {
            var syncUrl = string.Empty;
            if (SyncUrlBox.InvokeRequired)
            {
                SyncUrlBox.Invoke(new MethodInvoker(delegate { syncUrl = SyncUrlBox.Text; }));

            }
            else
            {
                syncUrl = SyncUrlBox.Text;
            }

            return syncUrl;
        }

        private string GetLocalPath()
        {
            var localPath = string.Empty;
            if (LocalPathBox.InvokeRequired)
            {
                LocalPathBox.Invoke(new MethodInvoker(delegate { localPath = LocalPathBox.Text; }));
            }
            else
            {
                localPath = LocalPathBox.Text;
            }

            return localPath;
        }

        private void HandleProgressUpdateEvent(object sender, ProgressUpdateEventArguments arg)
        {

            if (this.progress1.InvokeRequired)
            {
                Server.ProgressUpdateEventHandler d = new Server.ProgressUpdateEventHandler(HandleProgressUpdateEvent);
                this.Invoke(d, new object[] { sender, arg });
            }
            else
            {
                progress1.Value = arg.ProgressValue;
                progress2.Value = arg.ProgressValue;
            }
        }

        private void HandleFetchProcessUpdateEvent(object sender, ProgressUpdateEventArguments arg)
        {
            if (this.progress1.InvokeRequired)
            {
                Server.FetchProgressUpdateEventHandler d = new Server.FetchProgressUpdateEventHandler(HandleFetchProcessUpdateEvent);
                this.Invoke(d, new object[] { sender, arg });
            }
            else
            {
                this.statusStrip.Text = string.Format(Strings.FetchingChanges, arg.ProgressValue, arg.MaximumValue);
            }
        }

        private void HandleDownloadProgressEvent(object sender, DownloadProgressEventArgs arg)
        {

            if (progress1.InvokeRequired || progressLabel1.InvokeRequired)
            {
                var d = new Server.DownloadProgressEventHandler(HandleDownloadProgressEvent);
                Invoke(d, sender, arg);
            }
            else
            {
                if (arg.BytesTotal == -1)
                {
                    progress1.Value = 0;
                    progressLabel1.Text = Strings.DownloadsRemianingFallback;
                }
                else
                {
                    progress1.Value = arg.BytesTotal == 0 ? 100 : (int)(100 * arg.BytesDonwloaded / arg.BytesTotal);
                    var remaining = (arg.BytesTotal - arg.BytesDonwloaded) / (1024.0 * 1024 * 1024);
                    progressLabel1.Text = string.Format(Strings.DownloadsRemaining, arg.Files, arg.FilesTotal, remaining);
                }
            }
        }

        private void HandleUpdateProgressEvent(object sender, DownloadProgressEventArgs arg)
        {

            if (progress2.InvokeRequired || progressLabel2.InvokeRequired)
            {
                var d = new Server.UpdateProgressEventHandler(HandleUpdateProgressEvent);
                Invoke(d, sender, arg);
            }
            else
            {
                if (arg.BytesTotal == -1)
                {
                    progress2.Value = 0;
                    progressLabel2.Text = Strings.UpdatesRemainingFallback;
                }
                else
                {
                    progress2.Value = arg.BytesTotal == 0 ? 100 : (int) (100 * arg.BytesDonwloaded / arg.BytesTotal);
                    var remaining = (arg.BytesTotal - arg.BytesDonwloaded) / (1024.0 * 1024 * 1024);
                    progressLabel2.Text = string.Format(Strings.UpdatesRemaining, arg.Files, arg.FilesTotal, remaining);
                }
            }
        }


        private void load()
        {
            Server server = new Server();
            bool loaded = false;
            bool ioerror = false;
            Task t = Task.Factory.StartNew(() =>
            {
                SetLoadButton(false);
                SetSyncButton(false);
                SetTextBoxes(false);
                statusStrip.Text = Strings.LoadingServerStatus;
                SetProgressLabels(Strings.LoadingServer);
                Uri SyncUri = new Uri(GetSyncUrl());

                server.ProgressUpdateEvent += HandleProgressUpdateEvent;
                server.FetchProgessUpdateEvent += HandleFetchProcessUpdateEvent;

                Program.LoadedServer = server;
                try
                {
                    loaded = Program.LoadedServer.LoadFromWeb(SyncUri, new DirectoryInfo(GetLocalPath()));
                }
                catch (IOException e)
                {
                    ioerror = true;
                    logger.Error(e);
                }

            }).ContinueWith(x =>
            {
                server.ProgressUpdateEvent -= HandleProgressUpdateEvent;
                if (loaded)
                {
                    Program.ServerLoadeded = true;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(Strings.RemoteModsFetched);
                    foreach (ModFolder m in Program.LoadedServer.GetLoadedMods())
                    {
                        sb.AppendFormat("\t {0} \n", m.ModName);
                    }
                    MessageBox.Show(sb.ToString());
                    statusStrip.Text = Strings.ServerLoaded;
                    SetProgressLabels(Strings.ServerLoaded);
                    SetLoadButton(true);
                    SetSyncButton(true);
                }
                else if (!ioerror)
                {
                    MessageBox.Show(Strings.FailedToLoadServerFile, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetLoadButton(true);
                    SetTextBoxes(true);
                    HandleProgressUpdateEvent(null, new ProgressUpdateEventArguments() { ProgressValue = 0 });
                    statusStrip.Text = Strings.FailedToLoadDueToBadUrl;
                    logger.Error($"Failed to load config file {SyncUrlBox.Text}");
                } else if (ioerror)
                {
                    MessageBox.Show(Strings.FailedToHashLocalFiles, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetLoadButton(true);
                    SetTextBoxes(true);
                    HandleProgressUpdateEvent(null, new ProgressUpdateEventArguments() { ProgressValue = 0 });
                    statusStrip.Text = Strings.FailedToLoadDueToIOError;
                    logger.Error("Failed to load config file due to IO error (File in use?)");
                }
            });
        }
        private void btnLoad_Click(object sender, EventArgs e)
        {
            load();

        }

        private void sync(bool saveChangeList)
        {
            HandleDownloadProgressEvent(null, new DownloadProgressEventArgs
            {
                BytesTotal = -1
            });
            HandleUpdateProgressEvent(null, new DownloadProgressEventArgs
            {
                BytesTotal = -1
            });

            Program.LoadedServer.DownloadProgressEvent += HandleDownloadProgressEvent;
            Program.LoadedServer.UpdateProgressEvent += HandleUpdateProgressEvent;

            MessageBox.Show(Strings.AboutToFetchMods);
            Stopwatch Sw = new Stopwatch();
            Task t = Task.Factory.StartNew(() =>
            {
                SetTextBoxes(false);
                SetLoadButton(false);
                SetSyncButton(false);
                Sw.Start();
                statusStrip.Text = Strings.FetchingChangesStatus;
                SetProgressLabels(Strings.FetchingChangesStatus);
                FailedChanges = Program.LoadedServer.FetchChanges(Program.LoadedServer.GetLocalPath(), Remote.GetModFolderHashes(Program.LoadedServer.GetServerFileUri()),saveChangeList);

                /*
                No longer needed due to TS 3.1 
                if (TeamSpeakPlugin.TeamSpeakInstalled())
                {
                    logger.Info("TeamSpeak install detected at {0}", TeamSpeakPlugin.TeamSpeakPath());
                    var TeamSpeakPlugins = TeamSpeakPlugin.GetModFoldersWithPlugins(Program.LoadedServer.GetLoadedMods(), Program.LoadedServer.GetLocalPath().ToString());
                    TeamSpeak.FindAndCopyTeamSpeakPlugin(TeamSpeakPlugins, Program.LoadedServer.GetLocalPath(), new DirectoryInfo(TeamSpeakPlugin.TeamSpeakPath()));
                }
                */
                
                // Do the same with user configs
                if (ArmA.IsInstalled())
                {
                    // Just in case ArmA isn't installed (why someone is installing mods is another issue..), to prevent any errors
                    //var UserConfigs = UserConfig.GetModFoldersWithUserConfigs(Program.LoadedServer.GetLoadedMods(), Program.LoadedServer.GetLocalPath().ToString());
                    UserConfig.CopyUserConfigs(Program.LoadedServer.GetLoadedMods(), Program.LoadedServer.GetLocalPath());


                    try
                    {
                        Bikey.CopyBiKeys(Program.LoadedServer.GetLoadedMods(), Program.LoadedServer.GetLocalPath());
                    }
                    catch (IOException ex)
                    {
                        // Just log, we don't care
                        logger.Warn(ex);
                    }

                    // Generate and install Arma3 Launcher preset
                    Local local = ArmALauncher.ReadLocal();
                    local = ArmALauncher.UpdateLocal(Program.LoadedServer.GetLoadedMods(), local, Program.LoadedServer.GetLocalPath().FullName);
                    ArmALauncher.WriteLocal(local);

                    string preset = ArmALauncher.GeneratePreset(Program.LoadedServer.GetLocalPath().FullName,
                        Program.LoadedServer.GetLoadedMods(), Program.LoadedServer.Dlcs);

                    ArmALauncher.WritePreset(preset, Program.LoadedServer.GetServerFile().ServerName);
                }

            }).ContinueWith(x =>
            {

                Program.LoadedServer.DownloadProgressEvent -= HandleDownloadProgressEvent;
                Program.LoadedServer.UpdateProgressEvent -= HandleUpdateProgressEvent;
                Sw.Stop();
                SetSyncButton(true);
                SetTextBoxes(true);
                SetLoadButton(true);
                statusStrip.Text = string.Format(Strings.ChangesFetchedIn, Sw.Elapsed.ToString());
                if (FailedChanges > 0)
                {
                    if (
                        MessageBox.Show(
                            string.Format(
                                Strings.FailedToAcquire,
                                FailedChanges), Strings.Error, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) ==
                        DialogResult.Retry)
                    {
                        load();
                    }
                }
                else
                {
                    MessageBox.Show(string.Format(Strings.FetchedModsIn, Sw.Elapsed.ToString()));
                }
            });
        }
        private void btnSync_Click(object sender, EventArgs e)
        {
            var saveChangeList = Control.ModifierKeys == Keys.Control;
            sync(saveChangeList);
        }
        private void Main_Load(object sender, EventArgs e)
        {
            this.Text = String.Format(Strings.ProgramTitle, Application.ProductVersion);
            var persistentSyncUrls = PersistentSettings.GetSyncUrls();
            if (persistentSyncUrls != null)
            {
                foreach (var item in persistentSyncUrls)
                {
                    SyncUrlBox.Items.Add(item);
                }

                SyncUrlBox.SelectedIndex = PersistentSettings.GetUrlsSelectedIndex();
            }
            else
            {
                // If its null, we might have an old style one 
                var oldStyleUrl = PersistentSettings.GetLastSyncUrlOldStyle();
                if (oldStyleUrl != null)
                {
                    SyncUrlBox.Items.Add(oldStyleUrl);
                    SyncUrlBox.SelectedIndex = 0;
                }
            }
            LocalPathBox.Text = PersistentSettings.GetLastModFolder();

            var msg = MessageBox.Show(
                "This prototype version of BSU is deprecated. Please use the shiny new BSU from https://beowulfstratops.github.io/BSU/  .\n\nOpen website now?",
                "Use new BSU!", MessageBoxButtons.YesNo);
            if (msg == DialogResult.Yes)
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://beowulfstratops.github.io/BSU/",
                    UseShellExecute = true
                });
        }

        private void SyncUrlBox_Leave(object sender, EventArgs e)
        {
            if (!SyncUrlBox.Items.Contains(SyncUrlBox.Text))
            {
                SyncUrlBox.Items.Add(SyncUrlBox.Text);
                SyncUrlBox.SelectedIndex = SyncUrlBox.FindStringExact(SyncUrlBox.Text);
            }

            var values = this.SyncUrlBox.Items.OfType<string>().ToList();
            PersistentSettings.SetSyncUrls(values);
            PersistentSettings.SetUrlSelectedIndex(SyncUrlBox.SelectedIndex);
        }

        private void LocalPathBox_Leave(object sender, EventArgs e)
        {
            PersistentSettings.SetLastModFolder(LocalPathBox.Text);
        }

        private void btnDirectorySelect_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (LocalPathBox.Text != "")
            {
                fbd.SelectedPath = LocalPathBox.Text;
            }
            fbd.Description = Strings.SelectModInstallLocation;
            fbd.ShowDialog();
            LocalPathBox.Text = fbd.SelectedPath;
            PersistentSettings.SetLastModFolder(LocalPathBox.Text);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData != (Keys.L | Keys.Alt)) return base.ProcessCmdKey(ref msg, keyData);

            var fileTarget = (FileTarget) LogManager.Configuration.FindTargetByName("f");
            var logEventInfo = new LogEventInfo {TimeStamp = DateTime.Now};
            var folder = Path.GetDirectoryName(fileTarget.FileName.Render(logEventInfo));

            Process.Start(new ProcessStartInfo
            {
                FileName = folder ?? Application.StartupPath,
                UseShellExecute = true,
                Verb = "Open"
            });
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
