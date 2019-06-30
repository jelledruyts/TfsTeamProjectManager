using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Compare.VsWhere
{
    internal class Catalog
    {
        public string buildBranch { get; set; }
        public string buildVersion { get; set; }
        public string id { get; set; }
        public string localBuild { get; set; }
        public string manifestName { get; set; }
        public string manifestType { get; set; }
        public string productDisplayVersion { get; set; }
        public string productLine { get; set; }
        public string productLineVersion { get; set; }
        public string productMilestone { get; set; }
        public string productMilestoneIsPreRelease { get; set; }
        public string productName { get; set; }
        public string productPatchVersion { get; set; }
        public string productPreReleaseMilestoneSuffix { get; set; }
        public string productSemanticVersion { get; set; }
        public string requiredEngineVersion { get; set; }
    }

}
