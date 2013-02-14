using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.BuildProcessTemplates
{
    public class BuildProcessHierarchyNode
    {
        public BuildProcessHierarchyNodeType Type { get; private set; }
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public IList<BuildProcessTemplateInfo> BuildProcessTemplates { get; private set; }
        public IList<BuildProcessHierarchyNode> Children { get; private set; }
        public bool HasBuildProcessTemplates { get; private set; }
        public bool HasBuildDefinitions { get; private set; }
        public bool IsExpanded { get; private set; }

        public BuildProcessHierarchyNode(BuildProcessHierarchyNodeType type, string name, IList<BuildProcessTemplateInfo> buildProcessTemplates, IList<BuildProcessHierarchyNode> children)
        {
            this.Type = type;
            this.Name = name;
            this.BuildProcessTemplates = buildProcessTemplates ?? new BuildProcessTemplateInfo[0];
            this.Children = children ?? new BuildProcessHierarchyNode[0];
            this.HasBuildProcessTemplates = this.BuildProcessTemplates.Any();
            var numBuildDefinitions = 0;
            switch (this.Type)
            {
                case BuildProcessHierarchyNodeType.BuildProcessTemplateServerPath:
                    numBuildDefinitions = this.Children.Sum(c => c.Children.Count);
                    this.DisplayName = string.Format(CultureInfo.CurrentCulture, "{0} (Registered in {1}, used by {2})", this.Name, this.Children.Count.ToCountString("Team Project"), numBuildDefinitions.ToCountString("Build Definition"));
                    break;
                case BuildProcessHierarchyNodeType.TeamProject:
                    numBuildDefinitions = this.Children.Count;
                    this.DisplayName = string.Format(CultureInfo.CurrentCulture, "{0} (Registered as {1} template, used by {2})", this.Name, this.BuildProcessTemplates.Single().ProcessTemplate.TemplateType.ToString().ToLowerInvariant(), numBuildDefinitions.ToCountString("Build Definition"));
                    break;
                default:
                    numBuildDefinitions = 1;
                    this.DisplayName = this.Name;
                    break;
            }
            this.HasBuildDefinitions = numBuildDefinitions > 0;
            this.IsExpanded = this.Type == BuildProcessHierarchyNodeType.BuildProcessTemplateServerPath;
        }
    }
}