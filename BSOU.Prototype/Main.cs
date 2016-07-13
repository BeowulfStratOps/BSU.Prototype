using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BSU.Sync;
using System.IO;
using System.Diagnostics;
using System.Threading;
using NLog;

namespace BSU.Prototype
{
    public partial class Main : Form
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        delegate void SetLoadButtonCallback(bool Status);
        delegate void SetSyncButtonCallback(bool Status);
        delegate void SetSyncUrlBoxCallBack(bool Status);
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
        private void btnLoad_Click(object sender, EventArgs e)
        {
            bool loaded = false;
            Task t = Task.Factory.StartNew(() =>
            {
                SetLoadButton(false);
                SetTextBoxes(false);
                statusStrip.Text = "Loading Server (procesing your local mods, might be slow)";
                Uri SyncUri = new Uri(SyncUrlBox.Text);
                Program.LoadedServer = new Server();
                loaded = Program.LoadedServer.LoadFromWeb(SyncUri, new DirectoryInfo(LocalPathBox.Text));

            }).ContinueWith(x =>
            {
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
                    SetLoadButton(true);
                    SetSyncButton(true);
                }
                else
                {
                    MessageBox.Show("Failed to load server file. Check the patha and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetLoadButton(true);
                    SetTextBoxes(true);
                }
            });
            
        }
        private void btnSync_Click(object sender, EventArgs e)
        {
            MessageBox.Show("About to fetch mods! This might take a long time.");
            Stopwatch Sw = new Stopwatch();
            Task t = Task.Factory.StartNew(() =>
            {
                SetSyncButton(false);
                Sw.Start();
                statusStrip.Text = "Fetching changes";
                Program.LoadedServer.FetchChanges(Program.LoadedServer.GetLocalPath(), Remote.GetModFolderHashes(Program.LoadedServer.GetServerFileUri()));

                if (TeamSpeakPlugin.TeamSpeakInstalled())
                {
                    logger.Info("TeamSpeak install detected at {0}", TeamSpeakPlugin.TeamSpeakPath());
                    var TeamSpeakPlugins = TeamSpeakPlugin.GetModFoldersWithPlugins(Program.LoadedServer.GetLoadedMods(), Program.LoadedServer.GetLocalPath().ToString());
                    TeamSpeak.FindAndCopyTeamSpeakPlugin(TeamSpeakPlugins, Program.LoadedServer.GetLocalPath(), new DirectoryInfo(TeamSpeakPlugin.TeamSpeakPath()));
                }

            }).ContinueWith(x =>
            {
                Sw.Stop();
                SetSyncButton(true);
                SetTextBoxes(true);
                statusStrip.Text = string.Format("Changes fetched in {0}", Sw.Elapsed.ToString());
                MessageBox.Show(string.Format("Fetched mods in {0}", Sw.Elapsed.ToString()));

            });
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
