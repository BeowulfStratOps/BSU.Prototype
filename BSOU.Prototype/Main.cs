using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BSO.Sync;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace BSOU.Prototype
{
    public partial class Main : Form
    {
        delegate void SetLoadButtonCallback(bool Status);
        delegate void SetSyncButtonCallback(bool Status);
        public Main()
        {
            InitializeComponent();
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
                Program.LoadedServer.FetchChanges(Program.LoadedServer.GetLocalPath(), Remote.GetModFolderHashes(Program.LoadedServer.GetServerFileUri()));
            }).ContinueWith(x =>
            {
                Sw.Stop();
                SetSyncButton(true);
                MessageBox.Show(string.Format("Fetched mods in {0}", Sw.Elapsed.ToString()));
            });
        }
    }
}
