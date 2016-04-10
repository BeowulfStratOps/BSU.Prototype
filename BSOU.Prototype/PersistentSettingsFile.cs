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
        public PersistentSettingsFile(string ModPath, string SyncUrl)
        {
            this.ModPath = ModPath;
            this.SyncUrl = SyncUrl;
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
    }
}
