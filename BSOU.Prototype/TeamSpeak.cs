using BSU.Sync;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BSU.Prototype
{
    /// <summary>
    /// Handles moving the TeamSpeak plugins around if needed. 
    /// </summary>
    static class TeamSpeak
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Finds and copies TS plugin from mod folders to the TeamSpeak location
        /// </summary>
        /// <param name="LocalPath"></param>
        /// <param name="ModFolders"></param>
        /// <param name="TeamSpeakLocation"></param>
        /// <returns></returns>
        internal static bool FindAndCopyTeamSpeakPlugin(List<ModFolder> ModFolders, DirectoryInfo LocalPath, DirectoryInfo TeamSpeakLocation)
        {
            foreach (ModFolder m in ModFolders)
            {
                DirectoryInfo modPath = new DirectoryInfo(Path.Combine(LocalPath.ToString(), m.ModName.ToString()));
                List<DirectoryInfo> modPluginFolders = new List<DirectoryInfo>();
                modPluginFolders.Add(new DirectoryInfo(Path.Combine(modPath.FullName, "plugins")));
                modPluginFolders.Add(new DirectoryInfo(Path.Combine(modPath.FullName, "plugin")));
                // ^ Support for both TFAR and ACRE 
                foreach (DirectoryInfo modPluginFolder in modPluginFolders)
                {
                    DirectoryInfo tsPluginFolder = new DirectoryInfo(Path.Combine(TeamSpeakPlugin.TeamSpeakPath(), "plugins"));
                    if (modPluginFolder.Exists)
                    {
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
                                    return false;
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
                                        File.Copy(file.FullName, Path.Combine(tsPluginFolder.ToString(), relativePath),true);
                                    }
                                    catch (UnauthorizedAccessException ex)
                                    {
                                        MessageBox.Show(string.Format("Failed to copy {0} to TS plugin folder. You may want to copy the files manually. (Please report this issue)", relativePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        logger.Error("Failed to copy file", ex);
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private static bool FileEquals(FileInfo A, FileInfo B)
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
    }
}
