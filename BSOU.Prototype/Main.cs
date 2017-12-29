using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BSU.Sync;
using System.IO;
using System.Diagnostics;
using NLog;
using BSU.Sync.FileTypes.BI;

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
                this.statusStrip.Text = $"Fetching changes ({arg.ProgressValue}/{arg.MaximumValue})...";
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
                    progressLabel1.Text = "Downloads (0/???)   ???GB remaining";
                }
                else
                {
                    progress1.Value = arg.BytesTotal == 0 ? 100 : (int)(100 * arg.BytesDonwloaded / arg.BytesTotal);
                    var remaining = (arg.BytesTotal - arg.BytesDonwloaded) / (1024.0 * 1024 * 1024);
                    progressLabel1.Text = $"Downloads ({arg.Files}/{arg.FilesTotal})   {remaining:0.00}GB remaining";
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
                    progressLabel2.Text = "Updates (0/???)   ???GB remaining";
                }
                else
                {
                    progress2.Value = arg.BytesTotal == 0 ? 100 : (int) (100 * arg.BytesDonwloaded / arg.BytesTotal);
                    var remaining = (arg.BytesTotal - arg.BytesDonwloaded) / (1024.0 * 1024 * 1024);
                    progressLabel2.Text = $"Updates ({arg.Files}/{arg.FilesTotal})   {remaining:0.00}GB remaining";
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
                statusStrip.Text = "Loading Server (procesing your local mods, might be slow)";
                SetProgressLabels("Loading Server");
                Uri SyncUri = new Uri(SyncUrlBox.Text);

                server.ProgressUpdateEvent += HandleProgressUpdateEvent;
                server.FetchProgessUpdateEvent += HandleFetchProcessUpdateEvent;

                Program.LoadedServer = server;
                try
                {
                    loaded = Program.LoadedServer.LoadFromWeb(SyncUri, new DirectoryInfo(LocalPathBox.Text));
                }
                catch (IOException)
                {
                    ioerror = true;
                }

            }).ContinueWith(x =>
            {
                server.ProgressUpdateEvent -= HandleProgressUpdateEvent;
                if (loaded)
                {
                    Program.ServerLoadeded = true;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Remote Mods Fetched: ");
                    foreach (ModFolder m in Program.LoadedServer.GetLoadedMods())
                    {
                        sb.AppendFormat("\t {0} \n", m.ModName);
                    }
                    MessageBox.Show(sb.ToString());
                    statusStrip.Text = "Server Loaded";
                    SetProgressLabels("Server Loaded");
                    SetLoadButton(true);
                    SetSyncButton(true);
                }
                else if (!ioerror)
                {
                    MessageBox.Show("Failed to load server file. Check the URL and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetLoadButton(true);
                    SetTextBoxes(true);
                    HandleProgressUpdateEvent(null, new ProgressUpdateEventArguments() { ProgressValue = 0 });
                    statusStrip.Text = "Failed to load due to bad URL";
                    logger.Error($"Failed to load config file {SyncUrlBox.Text}");
                } else if (ioerror)
                {
                    MessageBox.Show("Failed to hash local files. Check they are not in use and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetLoadButton(true);
                    SetTextBoxes(true);
                    HandleProgressUpdateEvent(null, new ProgressUpdateEventArguments() { ProgressValue = 0 });
                    statusStrip.Text = "Failed to load due to IO Error";
                    logger.Error("Failed to load config file due to IO error (File in use?)");
                }
            });
        }
        private void btnLoad_Click(object sender, EventArgs e)
        {
            load();

        }

        private void sync()
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

            MessageBox.Show("About to fetch mods! This might take a long time.");
            Stopwatch Sw = new Stopwatch();
            Task t = Task.Factory.StartNew(() =>
            {
                SetTextBoxes(false);
                SetLoadButton(false);
                SetSyncButton(false);
                Sw.Start();
                statusStrip.Text = "Fetching changes";
                SetProgressLabels("Fetching changes");
                FailedChanges = Program.LoadedServer.FetchChanges(Program.LoadedServer.GetLocalPath(), Remote.GetModFolderHashes(Program.LoadedServer.GetServerFileUri()));

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

                    Bikey.CopyBiKeys(Program.LoadedServer.GetLoadedMods(), Program.LoadedServer.GetLocalPath());

                    // Generate and install Arma3 Launcher preset
                    Local local = ArmALauncher.ReadLocal();
                    local = ArmALauncher.UpdateLocal(Program.LoadedServer.GetLoadedMods(), local, Program.LoadedServer.GetLocalPath().FullName);
                    ArmALauncher.WriteLocal(local);

                    string preset = ArmALauncher.GeneratePreset(Program.LoadedServer.GetLocalPath().FullName,
                        Program.LoadedServer.GetLoadedMods());

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
                statusStrip.Text = string.Format("Changes fetched in {0}", Sw.Elapsed.ToString());
                if (FailedChanges > 0)
                {
                    if (
                        MessageBox.Show(
                            string.Format(
                                "Failed to acquire {0} changes. Your mods are not up to date as a result. \r\nYou must re-sync to be up to date. Ensure you are connected to the internet.",
                                FailedChanges), "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) ==
                        DialogResult.Retry)
                    {
                        load();
                    }
                }
                else
                {
                    MessageBox.Show(string.Format("Fetched mods in {0}", Sw.Elapsed.ToString()));
                }
            });
        }
        private void btnSync_Click(object sender, EventArgs e)
        {
            sync();
        }
        private void Main_Load(object sender, EventArgs e)
        {
            this.Text = String.Format("Beowulf Sync Prototype {0}", Application.ProductVersion);
            SyncUrlBox.Text = PersistentSettings.GetLastSyncUrl();
            LocalPathBox.Text = PersistentSettings.GetLastModFolder();
        }

        private void SyncUrlBox_Leave(object sender, EventArgs e)
        {
            PersistentSettings.SetLastSyncUrl(SyncUrlBox.Text);
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
            fbd.Description = "Select where you wish to install the mods to";
            fbd.ShowDialog();
            LocalPathBox.Text = fbd.SelectedPath;
            PersistentSettings.SetLastModFolder(LocalPathBox.Text);
        }
    }
}
