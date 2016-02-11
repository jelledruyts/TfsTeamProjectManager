using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public partial class ComparisonResultViewerDialog : Window
    {
        private TeamProjectComparisonResult comparisonResult;

        public ComparisonResultViewerDialog(TeamProjectComparisonResult comparisonResult)
        {
            InitializeComponent();
            this.comparisonResult = comparisonResult;
            this.DataContext = this.comparisonResult;
            this.workItemConfigurationResultsDataGrid.SelectedItem = this.comparisonResult.BestMatch;
            this.diffToolsComboBox.ItemsSource = GetDiffTools();
            this.diffToolsComboBox.SelectedIndex = 0;
            this.Title = string.Format(CultureInfo.CurrentCulture, "Comparison result details for Team Project \"{0}\"", this.comparisonResult.TeamProject);
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
            return this.diffToolsComboBox.SelectedItem != null && this.workItemConfigurationResultsDataGrid.SelectedItem != null && itemComparisonResult != null && itemComparisonResult.Status != ComparisonStatus.ExistsOnlyInSource && itemComparisonResult.Status != ComparisonStatus.ExistsOnlyInTarget;
        }

        private void CompareInDiffTool()
        {
            if (this.workItemConfigurationItemResultsDataGrid.SelectedItem != null)
            {
                var itemComparisonResult = (WorkItemConfigurationItemComparisonResult)this.workItemConfigurationItemResultsDataGrid.SelectedItem;
                if (CanCompareInDiffTool(itemComparisonResult))
                {
                    var diffTool = (DiffTool)this.diffToolsComboBox.SelectedItem;
                    var configurationComparisonResult = (WorkItemConfigurationComparisonResult)this.workItemConfigurationResultsDataGrid.SelectedItem;
                    var sourceFile = Path.GetTempFileName();
                    var targetFile = Path.GetTempFileName();
                    try
                    {
                        itemComparisonResult.NormalizedSourceDefinition.Save(sourceFile);
                        itemComparisonResult.NormalizedTargetDefinition.Save(targetFile);

                        var sourceLabel = string.Format(CultureInfo.CurrentCulture, "Work Item Type '{0}' in Source '{1}'", itemComparisonResult.ItemName, configurationComparisonResult.Source.Name);
                        var targetLabel = string.Format(CultureInfo.CurrentCulture, "Work Item Type '{0}' in Team Project '{1}'", itemComparisonResult.ItemName, this.comparisonResult.TeamProject);

                        diffTool.Launch(sourceFile, targetFile, sourceLabel, targetLabel);
                    }
                    finally
                    {
                        File.Delete(sourceFile);
                        File.Delete(targetFile);
                    }
                }
            }
        }

        private static ICollection<DiffTool> GetDiffTools()
        {
            var diffTools = new List<DiffTool>();

            // See if a custom diff tool was specified in the configuration file.
            var diffToolCommandOverride = ConfigurationManager.AppSettings["DiffToolCommand"];
            var diffToolArgumentsOverride = ConfigurationManager.AppSettings["DiffToolArguments"];
            if (!string.IsNullOrEmpty(diffToolCommandOverride))
            {
                diffTools.Add(new DiffTool("Custom", diffToolCommandOverride, diffToolArgumentsOverride));
            }

            // Visual Studio 2012 or above have a built-in diff tool, call devenv.exe directly.
            TryAddDevenvTool("14.0", "Visual Studio 2015", diffTools);
            TryAddDevenvTool("12.0", "Visual Studio 2013", diffTools);
            TryAddDevenvTool("11.0", "Visual Studio 2012", diffTools);

            // Older versions should have a diffmerge.exe tool in the IDE path.
            TryAddDiffmergeTool("10.0", "Visual Studio 2010", diffTools);
            TryAddDiffmergeTool("9.0", "Visual Studio 2008", diffTools);

            return diffTools;
        }

        private static void TryAddDevenvTool(string visualStudioVersion, string visualStudioName, IList<DiffTool> diffTools)
        {
            using (var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\{0}\Setup\VS".FormatInvariant(visualStudioVersion)))
            {
                if (regKey != null)
                {
                    var devenvPath = (string)regKey.GetValue("EnvironmentPath");
                    if (!string.IsNullOrWhiteSpace(devenvPath) && File.Exists(devenvPath))
                    {
                        diffTools.Add(new DiffTool(visualStudioName, devenvPath, "/diff %1 %2 %6 %7"));
                    }
                }
            }
        }

        private static void TryAddDiffmergeTool(string visualStudioVersion, string visualStudioName, IList<DiffTool> diffTools)
        {
            using (var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\{0}\Setup\VS".FormatInvariant(visualStudioVersion)))
            {
                if (regKey != null)
                {
                    var idePath = (string)regKey.GetValue("EnvironmentDirectory");
                    if (!string.IsNullOrWhiteSpace(idePath))
                    {
                        var diffmergePath = Path.Combine(idePath, "diffmerge.exe");
                        if (File.Exists(diffmergePath))
                        {
                            diffTools.Add(new DiffTool(visualStudioName, diffmergePath, "%1 %2 %6 %7 /ignorespace"));
                        }
                    }
                }
            }
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