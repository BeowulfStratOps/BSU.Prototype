using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BSOU.Prototype
{
    internal static class PersistentSettings
    {
        private static DirectoryInfo DataFolder = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeowulfSync"));
        static PersistentSettings()
        {
            Directory.CreateDirectory(DataFolder.FullName);
        }
        /// <summary>
        /// Gets the last saved (if any) mod folder
        /// </summary>
        /// <returns></returns>
        internal static string GetLastModFolder()
        {
            if (!PersistentFileExists())
            {
                return string.Empty;
            }
            PersistentSettingsFile persistentFile = JsonConvert.DeserializeObject<PersistentSettingsFile>(File.ReadAllText(Path.Combine(DataFolder.FullName, "data.json")));
            return persistentFile.ModPath;
        }
        /// <summary>
        /// Gets the last saved (if any) sync url
        /// </summary>
        /// <returns></returns>
        internal static string GetLastSyncUrl()
        {
            if (!PersistentFileExists())
            {
                return string.Empty;
            }
            PersistentSettingsFile persistentFile = JsonConvert.DeserializeObject<PersistentSettingsFile>(File.ReadAllText(Path.Combine(DataFolder.FullName, "data.json")));
            return persistentFile.SyncUrl;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        internal static void SetLastModFolder(string str)
        {
            PersistentSettingsFile persistentFile;
            if (PersistentFileExists())
            {
                string json = File.ReadAllText(Path.Combine(DataFolder.FullName, "data.json"));
                persistentFile = JsonConvert.DeserializeObject<PersistentSettingsFile>(json);
            }
            else
            {
                persistentFile = new PersistentSettingsFile(str, string.Empty);
            }
            if (persistentFile.ModPath == str)
            {
                return;
            }
            persistentFile.ModPath = str;
            File.WriteAllText(Path.Combine(DataFolder.FullName, "data.json"),JsonConvert.SerializeObject(persistentFile));


        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        internal static void SetLastSyncUrl(string str)
        {
            PersistentSettingsFile persistentFile;
            if (PersistentFileExists())
            {
                string json = File.ReadAllText(Path.Combine(DataFolder.FullName, "data.json"));
                persistentFile = JsonConvert.DeserializeObject<PersistentSettingsFile>(json);
            }
            else
            {
                persistentFile = new PersistentSettingsFile(string.Empty, str);
            }
            if (persistentFile.SyncUrl == str)
            {
                return;
            }
            persistentFile.SyncUrl = str;
            File.WriteAllText(Path.Combine(DataFolder.FullName, "data.json"), JsonConvert.SerializeObject(persistentFile));
        }
        private static bool PersistentFileExists()
        {
            
            return File.Exists(Path.Combine(DataFolder.FullName, "data.json"));
        }
    }
}
