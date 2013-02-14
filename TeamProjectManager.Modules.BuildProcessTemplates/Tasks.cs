using Microsoft.TeamFoundation.Build.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.BuildProcessTemplates
{
    internal static class Tasks
    {
        public static IList<BuildProcessTemplateInfo> GetBuildProcessTemplates(ApplicationTask task, IBuildServer buildServer, IEnumerable<string> teamProjectNames)
        {
            var processTemplates = new List<BuildProcessTemplateInfo>();
            var step = 0;

            foreach (var teamProjectName in teamProjectNames)
            {
                task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                try
                {
                    var teamProjectBuildDefinitions = buildServer.QueryBuildDefinitions(teamProjectName, QueryOptions.Process);

                    foreach (var processTemplate in buildServer.QueryProcessTemplates(teamProjectName))
                    {
                        var processTemplateBuildDefinitions = new List<IBuildDefinition>();
                        foreach (var teamProjectBuildDefinition in teamProjectBuildDefinitions)
                        {
                            if (teamProjectBuildDefinition.Process != null && BuildProcessTemplateInfo.AreEquivalent(teamProjectBuildDefinition.Process, processTemplate))
                            {
                                processTemplateBuildDefinitions.Add(teamProjectBuildDefinition);
                            }
                        }
                        processTemplates.Add(new BuildProcessTemplateInfo(processTemplate, processTemplateBuildDefinitions));
                    }
                }
                catch (Exception exc)
                {
                    task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProjectName), exc);
                }
            }
            return processTemplates;
        }

        public static void RegisterBuildProcessTemplate(ApplicationTask task, IBuildServer buildServer, IEnumerable<string> teamProjectNames, string templateServerPath, ProcessTemplateType templateType, bool registerIfTemplateDoesNotExist, bool unregisterAllOtherTemplates, bool unregisterAllOtherTemplatesIncludesUpgradeTemplate, bool simulate)
        {
            var step = 0;
            foreach (var teamProjectName in teamProjectNames)
            {
                try
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));

                    var allTemplates = buildServer.QueryProcessTemplates(teamProjectName);
                    var matchingTemplates = allTemplates.Where(t => t.ServerPath.Equals(templateServerPath, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (unregisterAllOtherTemplates)
                    {
                        var templatesToUnregister = allTemplates.Except(matchingTemplates);
                        if (!unregisterAllOtherTemplatesIncludesUpgradeTemplate)
                        {
                            templatesToUnregister = templatesToUnregister.Where(t => t.TemplateType != ProcessTemplateType.Upgrade);
                        }
                        foreach (var templateToUnregister in templatesToUnregister)
                        {
                            task.Status = string.Format(CultureInfo.CurrentCulture, "Unregistering existing build process template \"{0}\"", templateToUnregister.ServerPath);
                            var buildDefinitions = buildServer.QueryBuildDefinitions(teamProjectName, QueryOptions.Process);
                            foreach (var buildDefinition in buildDefinitions)
                            {
                                if (buildDefinition.Process != null && BuildProcessTemplateInfo.AreEquivalent(buildDefinition.Process, templateToUnregister))
                                {
                                    task.SetWarning(string.Format(CultureInfo.CurrentCulture, "WARNING - The build \"{0}\" uses the build process template \"{1}\" that is being unregistered", buildDefinition.Name, templateToUnregister.ServerPath));
                                }
                            }
                            if (!simulate)
                            {
                                templateToUnregister.Delete();
                            }
                        }
                    }
                    if (!(unregisterAllOtherTemplates && unregisterAllOtherTemplatesIncludesUpgradeTemplate))
                    {
                        if (templateType == ProcessTemplateType.Default || templateType == ProcessTemplateType.Upgrade)
                        {
                            // There can be only one upgrade or default template for a team project.
                            // Make sure there isn't already a template with that type.
                            foreach (var template in allTemplates.Except(matchingTemplates).Where(t => t.TemplateType == templateType))
                            {
                                task.Status = string.Format(CultureInfo.CurrentCulture, "Changing type of existing build process template \"{0}\" from \"{1}\" to \"{2}\"", template.ServerPath, template.TemplateType.ToString(), ProcessTemplateType.Custom.ToString());
                                if (!simulate)
                                {
                                    template.TemplateType = ProcessTemplateType.Custom;
                                    template.Save();
                                }
                            }
                        }
                    }

                    if (registerIfTemplateDoesNotExist && !matchingTemplates.Any())
                    {
                        task.Status = string.Format(CultureInfo.CurrentCulture, "Registering new build process template \"{0}\" as type \"{1}\"", templateServerPath, templateType.ToString());
                        if (!simulate)
                        {
                            var template = buildServer.CreateProcessTemplate(teamProjectName, templateServerPath);
                            template.TemplateType = templateType;
                            template.Save();
                        }
                    }
                    else
                    {
                        foreach (var template in matchingTemplates.Where(t => t.TemplateType != templateType))
                        {
                            task.Status = string.Format(CultureInfo.CurrentCulture, "Changing type of existing build process template \"{0}\" from \"{1}\" to \"{2}\"", template.ServerPath, template.TemplateType.ToString(), templateType.ToString());
                            if (!simulate)
                            {
                                template.TemplateType = templateType;
                                template.Save();
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    task.SetError(string.Format(CultureInfo.CurrentCulture, "An error occurred while registering the build process template \"{0}\" for Team Project \"{1}\"", templateServerPath, teamProjectName), exc);
                }
            }
        }

        public static void UnregisterBuildProcessTemplates(ApplicationTask task, IEnumerable<IProcessTemplate> buildProcessTemplates)
        {
            var step = 0;
            foreach (var buildProcessTemplate in buildProcessTemplates)
            {
                try
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Unregistering build process template \"{0}\"", buildProcessTemplate.ServerPath));
                    buildProcessTemplate.Delete();
                }
                catch (Exception exc)
                {
                    task.SetError(string.Format(CultureInfo.CurrentCulture, "An error occurred while unregistering the build process template \"{0}\" for Team Project \"{1}\"", buildProcessTemplate.ServerPath, buildProcessTemplate.TeamProject), exc);
                }
            }
        }
    }
}