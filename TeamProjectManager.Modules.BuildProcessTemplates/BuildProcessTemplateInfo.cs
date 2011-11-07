using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.TeamFoundation.Build.Client;

namespace TeamProjectManager.Modules.BuildProcessTemplates
{
    public class BuildProcessTemplateInfo
    {
        public IProcessTemplate ProcessTemplate { get; private set; }
        public IList<IBuildDefinition> BuildDefinitions { get; private set; }
        public string BuildDefinitionsDescription { get; private set; }

        public BuildProcessTemplateInfo(IProcessTemplate processTemplate, IList<IBuildDefinition> buildDefinitions)
        {
            this.ProcessTemplate = processTemplate;
            this.BuildDefinitions = buildDefinitions ?? new IBuildDefinition[0];
            var buildDefinitionsDescription = new StringBuilder();
            foreach (var buildDefinition in this.BuildDefinitions)
            {
                if (buildDefinitionsDescription.Length > 0)
                {
                    buildDefinitionsDescription.Append(", ");
                }
                buildDefinitionsDescription.Append(buildDefinition.Name);
            }
            this.BuildDefinitionsDescription = buildDefinitionsDescription.ToString();
        }

        public static bool AreEquivalent(IProcessTemplate x, IProcessTemplate y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            if (x.SupportedReasons != y.SupportedReasons)
            {
                return false;
            }
            if (x.TemplateType != y.TemplateType)
            {
                return false;
            }
            if (!string.Equals(x.TeamProject, y.TeamProject, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.Equals(x.ServerPath, y.ServerPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.Equals(x.Description, y.Description, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.Equals(x.Parameters, y.Parameters, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }
    }
}