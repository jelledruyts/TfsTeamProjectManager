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

namespace TeamProjectManager.Modules.WorkItemConfiguration.Compare
{
    public partial class ComparisonResultViewerDialog : Window
    {
        private const string VsWhereLocation = @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe";
        private const string VsWhereArgs = @"-format json -nologo -utf8";

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

                    itemComparisonResult.NormalizedSourceDefinition.Save(sourceFile);
                    itemComparisonResult.NormalizedTargetDefinition.Save(targetFile);

                    var sourceLabel = string.Format(CultureInfo.CurrentCulture, "Work Item Type '{0}' in Source '{1}'", itemComparisonResult.ItemName, configurationComparisonResult.Source.Name);
                    var targetLabel = string.Format(CultureInfo.CurrentCulture, "Work Item Type '{0}' in Team Project '{1}'", itemComparisonResult.ItemName, this.comparisonResult.TeamProject);

                    diffTool.Launch(sourceFile, targetFile, sourceLabel, targetLabel);
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

            var vswhere = Environment.ExpandEnvironmentVariables(VsWhereLocation);
            if (File.Exists(vswhere))
            {
                var psi = new ProcessStartInfo(vswhere, VsWhereArgs)
                {
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                var vswhereProcess = new Process()
                {
                    StartInfo = psi
                };
                vswhereProcess.Start();
                string vswhereOutput = vswhereProcess.StandardOutput.ReadToEnd();
                vswhereProcess.WaitForExit();

                if (vswhereProcess.ExitCode == 0)
                {
                    var vsinstances = Newtonsoft.Json.JsonConvert.DeserializeObject<VsWhere.VsInstance[]>(vswhereOutput);
                    
                    foreach (var vs in vsinstances)
                    {
                        diffTools.Add(new DiffTool(vs.displayName, vs.productPath, "/diff %1 %2 %6 %7"));
                    }
                }
            }
            
            // Visual Studio 2012 and above have a built-in diff tool, call devenv.exe directly.
            TryAddDevenvToolFromRegistry("14.0", "Visual Studio 2015", diffTools);
            TryAddDevenvToolFromRegistry("12.0", "Visual Studio 2013", diffTools);
            TryAddDevenvToolFromRegistry("11.0", "Visual Studio 2012", diffTools);

            // Older versions should have a diffmerge.exe tool in the IDE path.
            TryAddDiffmergeToolFromRegistry("10.0", "Visual Studio 2010", diffTools);
            TryAddDiffmergeToolFromRegistry("9.0", "Visual Studio 2008", diffTools);

            return diffTools;
        }

        private static void TryAddDevenvToolFromRegistry(string visualStudioVersion, string visualStudioName, IList<DiffTool> diffTools)
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

        private static void TryAddDiffmergeToolFromRegistry(string visualStudioVersion, string visualStudioName, IList<DiffTool> diffTools)
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
                if (process != null)
                {
                    // Delete the temp files when the diff tool has exited.
                    process.Exited += (object sender, EventArgs e) =>
                    {
                        File.Delete(sourceFileName);
                        File.Delete(targetFileName);
                    };
                }
            }
        }
    }
}