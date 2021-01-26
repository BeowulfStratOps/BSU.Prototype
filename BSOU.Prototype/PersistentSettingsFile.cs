using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSU.Prototype
{
    [JsonObject("PersistentSettings")]
    public class PersistentSettingsFile
    {
        public PersistentSettingsFile(string ModPath, List<string> syncUrls)
        {
            this.ModPath = ModPath;
            this.SyncUrls = syncUrls;
        }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty]
        public string ModPath { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty]
        public string SyncUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty]
        public List<string> SyncUrls { get; set; }
        [JsonProperty]
        public int SelectedUrl { get; set; }
    }
}
