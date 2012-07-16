using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public partial class ComparisonResultViewerDialog : Window
    {
        private TeamProjectComparisonResult comparisonResult;
        private DiffTool diffTool;

        public ComparisonResultViewerDialog(TeamProjectComparisonResult comparisonResult)
        {
            InitializeComponent();
            this.comparisonResult = comparisonResult;
            this.DataContext = this.comparisonResult;
            this.Title = string.Format(CultureInfo.CurrentCulture, "Comparison result details for Team Project \"{0}\"", this.comparisonResult.TeamProject);
            this.diffTool = GetDiffTool();
            // TODO: Replace by dropdown that allows user to choose diff tool.
            this.infoLabel.Text = this.diffTool == null ? "No Diff Tool Detected" : string.Format(CultureInfo.CurrentCulture, "Using {0} Diff Tool", this.diffTool.Name);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void workItemConfigurationResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.workItemConfigurationItemResultsDataGrid.SelectedIndex = 0;
        }

        private void workItemConfigurationItemResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemComparisonResult = (WorkItemConfigurationItemComparisonResult)this.workItemConfigurationItemResultsDataGrid.SelectedItem;
            this.compareInDiffToolButton.IsEnabled = CanCompareInDiffTool(itemComparisonResult);
        }

        private void workItemConfigurationItemResultsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CompareInDiffTool();
        }

        private void compareInDiffToolButton_Click(object sender, RoutedEventArgs e)
        {
            CompareInDiffTool();
        }

        private bool CanCompareInDiffTool(WorkItemConfigurationItemComparisonResult itemComparisonResult)
        {
            return this.diffTool != null && this.workItemConfigurationResultsDataGrid.SelectedItem != null && itemComparisonResult != null && itemComparisonResult.Status != ComparisonStatus.ExistsOnlyInSource && itemComparisonResult.Status != ComparisonStatus.ExistsOnlyInTarget;
        }

        private void CompareInDiffTool()
        {
            if (this.workItemConfigurationItemResultsDataGrid.SelectedItem != null)
            {
                var itemComparisonResult = (WorkItemConfigurationItemComparisonResult)this.workItemConfigurationItemResultsDataGrid.SelectedItem;
                if (CanCompareInDiffTool(itemComparisonResult))
                {
                    var configurationComparisonResult = (WorkItemConfigurationComparisonResult)this.workItemConfigurationResultsDataGrid.SelectedItem;
                    var sourceFile = Path.GetTempFileName();
                    var targetFile = Path.GetTempFileName();
                    try
                    {
                        itemComparisonResult.NormalizedSourceDefinition.Save(sourceFile);
                        itemComparisonResult.NormalizedTargetDefinition.Save(targetFile);

                        var sourceLabel = string.Format(CultureInfo.CurrentCulture, "Work Item Type '{0}' in Source '{1}'", itemComparisonResult.ItemName, configurationComparisonResult.Source.Name);
                        var targetLabel = string.Format(CultureInfo.CurrentCulture, "Work Item Type '{0}' in Team Project '{1}'", itemComparisonResult.ItemName, this.comparisonResult.TeamProject);

                        this.diffTool.Launch(sourceFile, targetFile, sourceLabel, targetLabel);
                    }
                    finally
                    {
                        File.Delete(sourceFile);
                        File.Delete(targetFile);
                    }
                }
            }
        }

        private static DiffTool GetDiffTool()
        {
            var diffToolCommandOverride = ConfigurationManager.AppSettings["DiffToolCommand"];
            var diffToolArgumentsOverride = ConfigurationManager.AppSettings["DiffToolArguments"];
            if (!string.IsNullOrEmpty(diffToolCommandOverride))
            {
                return new DiffTool("Custom", diffToolCommandOverride, diffToolArgumentsOverride);
            }
            else
            {
                return GetVisualStudioDiffTool();
            }
        }

        private static DiffTool GetVisualStudioDiffTool()
        {
            // Visual Studio 2012 has a built-in diff tool, call devenv.exe directly.
            using (var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\11.0\Setup\VS"))
            {
                if (regKey != null)
                {
                    var devenvPath = (string)regKey.GetValue("EnvironmentPath");
                    if (File.Exists(devenvPath))
                    {
                        return new DiffTool("Visual Studio 2012", devenvPath, "/diff %1 %2 %6 %7");
                    }
                }
            }

            // Older versions should have a diffmerge.exe tool in the IDE path.
            string idePath = null;
            string diffToolName = null;
            using (var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\10.0\Setup\VS"))
            {
                if (regKey != null)
                {
                    idePath = (string)regKey.GetValue("EnvironmentDirectory");
                    diffToolName = "Visual Studio 2010";
                }
            }
            if (!Directory.Exists(idePath))
            {
                using (var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\9.0\Setup\VS"))
                {
                    if (regKey != null)
                    {
                        idePath = (string)regKey.GetValue("EnvironmentDirectory");
                        diffToolName = "Visual Studio 2008";
                    }
                }
            }
            if (Directory.Exists(idePath))
            {
                var diffmergePath = Path.Combine(idePath, "diffmerge.exe");
                if (File.Exists(diffmergePath))
                {
                    return new DiffTool(diffToolName, diffmergePath, "%1 %2 %6 %7 /ignorespace");
                }
            }
            return null;
        }

        private class DiffTool
        {
            public string Name { get; private set; }
            public string Command { get; private set; }
            public string Arguments { get; private set; }

            public DiffTool(string name, string command, string arguments)
            {
                this.Name = name;
                this.Command = command;
                this.Arguments = arguments;
            }

            public void Launch(string sourceFileName, string targetFileName, string sourceLabel, string targetLabel)
            {
                var command = Environment.ExpandEnvironmentVariables(this.Command);
                var arguments = this.Arguments;
                arguments = arguments.Replace("%1", string.Concat("\"", sourceFileName, "\""));
                arguments = arguments.Replace("%2", string.Concat("\"", targetFileName, "\""));
                arguments = arguments.Replace("%6", string.Concat("\"", sourceLabel, "\""));
                arguments = arguments.Replace("%7", string.Concat("\"", targetLabel, "\""));
                arguments = Environment.ExpandEnvironmentVariables(arguments);
                var processInfo = new ProcessStartInfo(command, arguments);
                var process = Process.Start(processInfo);
                process.WaitForExit();
            }
        }
    }
}