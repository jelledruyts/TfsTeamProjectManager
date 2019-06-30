using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Compare.VsWhere
{
    internal class VsInstance
    {
        public string instanceId { get; set; }
        public DateTime installDate { get; set; }
        public string installationName { get; set; }
        public string installationPath { get; set; }
        public string installationVersion { get; set; }
        public string productId { get; set; }
        public string productPath { get; set; }
        public long state { get; set; }
        public bool isComplete { get; set; }
        public bool isLaunchable { get; set; }
        public bool isPrerelease { get; set; }
        public bool isRebootRequired { get; set; }
        public string displayName { get; set; }
        public string description { get; set; }
        public string channelId { get; set; }
        public string channelUri { get; set; }
        public string enginePath { get; set; }
        public string releaseNotes { get; set; }
        public string thirdPartyNotices { get; set; }
        public DateTime updateDate { get; set; }
        public Catalog catalog { get; set; }
        public Properties properties { get; set; }
    }

}
