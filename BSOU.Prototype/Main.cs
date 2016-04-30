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
            Task t = Task.Factory.StartNew(() =>
            {
                SetLoadButton(false);
                SetTextBoxes(false);
                statusStrip.Text = "Loading Server (procesing your local mods, might be slow)";
                Uri SyncUri = new Uri(SyncUrlBox.Text);
                Program.LoadedServer = new Server();
                Program.LoadedServer.LoadFromWeb(SyncUri, new DirectoryInfo(LocalPathBox.Text));

            }).ContinueWith(x =>
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

                    DirectoryInfo LocalPath = Program.LoadedServer.GetLocalPath();
                    var TeamSpeakPlugins = TeamSpeakPlugin.GetModFoldersWithPlugins(Program.LoadedServer.GetLoadedMods(), Program.LoadedServer.GetLocalPath().ToString());
                    foreach (ModFolder m in TeamSpeakPlugins)
                    {
                        DirectoryInfo modPath = new DirectoryInfo(Path.Combine(LocalPath.ToString(), m.ModName.ToString()));
                        DirectoryInfo modPluginFolder = new DirectoryInfo(Path.Combine(modPath.FullName, "plugins"));
                        DirectoryInfo tsPluginFolder = new DirectoryInfo(Path.Combine(TeamSpeakPlugin.TeamSpeakPath(), "plugins"));
                        foreach (var file in modPluginFolder.GetFiles("*", SearchOption.AllDirectories))
                        {
                            string relativePath = file.FullName.Replace(modPluginFolder.ToString() + @"\", string.Empty);
                            FileInfo tsFilePath = new FileInfo(Path.Combine(tsPluginFolder.ToString(), relativePath));
                            if (!tsFilePath.Exists)
                            {
                                // File does not exist in TS plugin folder, just copy it.
                                try
                                {
                                    FileInfo folder = new FileInfo(Path.Combine(tsPluginFolder.ToString(), relativePath));
                                    Directory.CreateDirectory(folder.ToString().Replace(folder.Name, string.Empty));
                                    File.Copy(file.FullName, Path.Combine(tsPluginFolder.ToString(), relativePath));
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    MessageBox.Show(string.Format("Failed to copy {0} to TS plugin folder. You may want to copy the files manually. (Please report this issue)", relativePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    logger.Error("Failed to copy file", ex);
                                }
                            }
                            else
                            {
                                // Check to make sure that the files are different and copy only if they are and **file is not in use**
                                // TODO: Check if its in use, right now it will fail if TS is using the files.
                                if (!FileEquals(file, tsFilePath))
                                {
                                    try
                                    {
                                        File.Copy(file.FullName, Path.Combine(tsPluginFolder.ToString(), relativePath));
                                    }
                                    catch (UnauthorizedAccessException ex)
                                    {
                                        MessageBox.Show(string.Format("Failed to copy {0} to TS plugin folder. You may want to copy the files manually. (Please report this issue)", relativePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        logger.Error("Failed to copy file", ex);
                                    }
                                }
                            }
                        }
                    }
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
        private bool FileEquals(FileInfo A, FileInfo B)
        {
            // Adapted from kb320348
            FileStream fs1, fs2;
            int f1byte, f2byte;
            if (A == B)
            {
                return true;
            }
            fs1 = new FileStream(A.FullName, FileMode.Open);
            fs2 = new FileStream(B.FullName, FileMode.Open);

            if (fs1.Length != fs2.Length)
            {
                fs1.Close();
                fs2.Close();
                return false;
            }

            do
            {
                f1byte = fs1.ReadByte();
                f2byte = fs2.ReadByte();
            }
            while ((f1byte == f2byte) && (f1byte != -1));

            fs1.Close();
            fs2.Close();

            return ((f1byte - f2byte) == 0);



        }

        private void Main_Load(object sender, EventArgs e)
        {
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
