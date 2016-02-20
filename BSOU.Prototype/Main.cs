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

namespace BSOU.Prototype
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            Uri SyncUri = new Uri(SyncUrlBox.Text);
            Program.LoadedServer = new Server();
            Program.LoadedServer.LoadFromWeb(SyncUri, new DirectoryInfo(LocalPathBox.Text));
            Program.ServerLoadeded = true;
            btnSync.Enabled = true;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Remote Mods Fetched: ");
            foreach (ModFolder m in Program.LoadedServer.GetLoadedMods())
            {
                sb.AppendFormat("\t {0} \n", m.ModName);
            }
            MessageBox.Show(sb.ToString());
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            MessageBox.Show("About to fetch mods! This may cause the program to appear to be frozen for a *long* time. This is WIP");
            Stopwatch Sw = new Stopwatch();
            Sw.Start();
            Program.LoadedServer.FetchChanges(Program.LoadedServer.GetLocalPath(), Remote.GetModFolderHashes(Program.LoadedServer.GetServerFileUri()));
            Sw.Stop();
            MessageBox.Show(string.Format("Fetched mods in {0}",Sw.Elapsed.ToString()));
        }
    }
}
