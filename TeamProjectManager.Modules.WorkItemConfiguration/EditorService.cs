using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class EditorService
    {
        private const string VsCodeName = "code";

        public IDictionary<WorkItemConfigurationItemExport, string> OriginalItemsWithPaths { get; private set; }

        public EditorService(IList<WorkItemConfigurationItemExport> items)
        {
            var tempPath = Path.GetTempPath();

            var originalItems = items ?? new WorkItemConfigurationItemExport[0];
            this.OriginalItemsWithPaths = new Dictionary<WorkItemConfigurationItemExport, string>();

            foreach (var originalItem in originalItems)
            {
                var path = Path.Combine(tempPath, originalItem.Item.Name + ".xml");
                OriginalItemsWithPaths.Add(originalItem, path);

                originalItem.Item.XmlDefinition.Save(path);
            }
        }

        public async Task<ProcessAsyncHelper.ProcessResult> StartEditor(string externalEditor)
        {
            var arguments = string.Empty;
            if (externalEditor.ToLower().Contains(VsCodeName))
            {
                arguments = "-n -w";
            }

            var process = await ProcessAsyncHelper.ExecuteShellCommand(externalEditor,
                "\"" + string.Join("\" \"", this.OriginalItemsWithPaths.Select(pair => pair.Value)) + "\" " + arguments,
                true);

            if (process.Completed && process.ExitCode == 0)
            {
                foreach (var originalWithPath in this.OriginalItemsWithPaths)
                {
                    var originalItem = originalWithPath.Key;
                    var path = originalWithPath.Value;

                    try
                    {
                        originalItem.Item.XmlDefinition.Load(path);
                    }
                    catch (XmlException e)
                    {
                        Console.WriteLine(e);
                        process.ExitCode = -1;
                        break;
                    }
                }
            }

            return process;
        }
    }
}
