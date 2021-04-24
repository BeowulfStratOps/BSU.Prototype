using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BSU.Prototype
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
        /// Gets the saved (if any) sync urls
        /// </summary>
        /// <returns></returns>
        internal static List<string> GetSyncUrls()
        {
            if (!PersistentFileExists())
            {
                return new List<string>();
            }
            PersistentSettingsFile persistentFile = JsonConvert.DeserializeObject<PersistentSettingsFile>(File.ReadAllText(Path.Combine(DataFolder.FullName, "data.json")));
            return persistentFile.SyncUrls;
        }
        /// <summary>
        /// Gets the old style sync URL if any
        /// </summary>
        /// <returns></returns>
        internal static string GetLastSyncUrlOldStyle()
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
                if (persistentFile.ModPath == str)
                {
                    return;
                }
            }
            else
            {
                persistentFile = new PersistentSettingsFile(str, new List<string>());
            }
            persistentFile.ModPath = str;
            File.WriteAllText(Path.Combine(DataFolder.FullName, "data.json"),JsonConvert.SerializeObject(persistentFile));


        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="urls">List of URLs</param>
        internal static void SetSyncUrls(List<string> urls)
        {
            PersistentSettingsFile persistentFile;
            if (PersistentFileExists())
            {
                string json = File.ReadAllText(Path.Combine(DataFolder.FullName, "data.json"));
                persistentFile = JsonConvert.DeserializeObject<PersistentSettingsFile>(json);
            }
            else
            {
                persistentFile = new PersistentSettingsFile(string.Empty, urls);
            }
            persistentFile.SyncUrls = urls;
            File.WriteAllText(Path.Combine(DataFolder.FullName, "data.json"), JsonConvert.SerializeObject(persistentFile));
        }

        internal static int GetUrlsSelectedIndex()
        {
            if (!PersistentFileExists())
            {
                // -1 is a valid default option for the combobox, 0 is not 
                return -1;
            }
            PersistentSettingsFile persistentFile = JsonConvert.DeserializeObject<PersistentSettingsFile>(File.ReadAllText(Path.Combine(DataFolder.FullName, "data.json")));
            return persistentFile.SelectedUrl;
        }

        internal static void SetUrlSelectedIndex(int index)
        {
            PersistentSettingsFile persistentFile;
            if (PersistentFileExists())
            {
                string json = File.ReadAllText(Path.Combine(DataFolder.FullName, "data.json"));
                persistentFile = JsonConvert.DeserializeObject<PersistentSettingsFile>(json);
                if (persistentFile.SelectedUrl == index)
                {
                    return;
                }
            }
            else
            {
                persistentFile = new PersistentSettingsFile(string.Empty, new List<string>());
            }

            persistentFile.SelectedUrl = index;
            File.WriteAllText(Path.Combine(DataFolder.FullName, "data.json"), JsonConvert.SerializeObject(persistentFile));
        }

        private static bool PersistentFileExists()
        {
            
            return File.Exists(Path.Combine(DataFolder.FullName, "data.json"));
        }
    }
}
