using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class WorkItemConfiguration
    {
        #region Properties

        public string Name { get; set; }
        public ObservableCollection<WorkItemConfigurationItem> Items { get; private set; }

        public string Description
        {
            get
            {
                return this.Name + (this.Items.Any() ? string.Format(CultureInfo.CurrentCulture, " ({0})", string.Join(", ", this.Items.Select(w => w.Name))) : string.Empty);
            }

        }

        #endregion

        #region Constructors

        public WorkItemConfiguration()
            : this(null, null)
        {
        }

        public WorkItemConfiguration(string name, IEnumerable<WorkItemConfigurationItem> items)
        {
            this.Name = name;
            this.Items = new ObservableCollection<WorkItemConfigurationItem>(items ?? new WorkItemConfigurationItem[0]);
        }

        #endregion

        #region IsValid

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(this.Name) && this.Items.Any();
        }

        #endregion

        #region Static Factory Methods

        public static WorkItemConfiguration FromTeamProject(TfsTeamProjectCollection tfs, Project project)
        {
            // Export work item type definitions.
            var projectWorkItemTypes = new List<WorkItemConfigurationItem>();
            foreach (WorkItemType workItemType in project.WorkItemTypes)
            {
                projectWorkItemTypes.Add(WorkItemConfigurationItem.FromXml(workItemType.Export(false)));
            }

            // Export categories.
            projectWorkItemTypes.Add(WorkItemConfigurationItem.FromXml(project.Categories.Export()));

            return new WorkItemConfiguration(project.Name, projectWorkItemTypes);
        }

        public static WorkItemConfiguration FromProcessTemplate(string processTemplateFileName)
        {
            // Load the process template XML.
            if (!File.Exists(processTemplateFileName))
            {
                throw new FileNotFoundException("The process template file does not exist: " + processTemplateFileName);
            }
            var processTemplate = new XmlDocument();
            processTemplate.Load(processTemplateFileName);
            var baseDir = Path.GetDirectoryName(processTemplateFileName);
            var items = new List<WorkItemConfigurationItem>();
            string processTemplateName = null;
            var processTemplateNameNode = processTemplate.SelectSingleNode("/ProcessTemplate/metadata/name");
            if (processTemplateNameNode != null)
            {
                processTemplateName = processTemplateNameNode.InnerText;
            }

            // Find the work item tracking XML file.
            var workItemFileNameAttribute = processTemplate.SelectSingleNode("/ProcessTemplate/groups/group[@id='WorkItemTracking']/taskList/@filename");
            if (workItemFileNameAttribute != null)
            {
                // Load the work item tracking XML.
                var workItemConfigurationTemplateFileName = Path.Combine(baseDir, workItemFileNameAttribute.InnerText);
                if (!File.Exists(workItemConfigurationTemplateFileName))
                {
                    throw new FileNotFoundException("The work item configuration file defined in the process template file does not exist: " + workItemConfigurationTemplateFileName);
                }
                var workItemConfigurationTemplate = new XmlDocument();
                workItemConfigurationTemplate.Load(workItemConfigurationTemplateFileName);

                // Find all work item type definition XML files.
                foreach (XmlAttribute witFileNameAttribute in workItemConfigurationTemplate.SelectNodes("/tasks/task[@id='WITs']/taskXml/WORKITEMTYPES/WORKITEMTYPE/@fileName"))
                {
                    var witFileName = Path.Combine(baseDir, witFileNameAttribute.InnerText);
                    items.Add(WorkItemTypeDefinition.FromFile(witFileName));
                }

                // Find the categories XML file.
                var categoriesFileNameAttribute = workItemConfigurationTemplate.SelectSingleNode("/tasks/task[@id='Categories']/taskXml/CATEGORIES/@fileName");
                if (categoriesFileNameAttribute != null)
                {
                    var categoriesFileName = Path.Combine(baseDir, categoriesFileNameAttribute.InnerText);
                    items.Add(WorkItemConfigurationItem.FromFile(categoriesFileName));
                }
                else
                {
                    // If the process template doesn't specify any categories (typically because it's an old
                    // process template from before Work Item Categories existed), load an empty list anyway.
                    // This will improve comparisons because a Team Project will always have a Work Item
                    // Categories configuration item (even if it's empty).
                    items.Add(WorkItemConfigurationItem.FromXml("<cat:CATEGORIES xmlns:cat=\"http://schemas.microsoft.com/VisualStudio/2008/workitemtracking/categories\"/>"));
                }
            }

            return new WorkItemConfiguration(processTemplateName, items);
        }

        #endregion
    }
}