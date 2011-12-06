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
        public TeamProjectComparisonResult Comparison { get; private set; }
        private string diffMergeFileName;

        public ComparisonResultViewerDialog(TeamProjectComparisonResult comparison)
        {
            InitializeComponent();
            this.Comparison = comparison;
            this.DataContext = this.Comparison;
            this.Title = string.Format(CultureInfo.CurrentCulture, "Comparison result details for Team Project \"{0}\"", this.Comparison.TeamProject);
            this.diffMergeFileName = GetDiffMergeFileName();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void sourceResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.workItemTypeResultsDataGrid.SelectedIndex = 0;
        }

        private void workItemTypeResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var workItemTypeComparison = (WorkItemTypeComparisonResult)this.workItemTypeResultsDataGrid.SelectedItem;
            this.compareInDiffToolButton.IsEnabled = CanCompareInDiffTool(workItemTypeComparison);
        }

        private bool CanCompareInDiffTool(WorkItemTypeComparisonResult workItemTypeComparison)
        {
            return this.diffMergeFileName != null && this.sourceResultsDataGrid.SelectedItem != null && workItemTypeComparison != null && (workItemTypeComparison.Status == ComparisonStatus.AreDifferent || workItemTypeComparison.Status == ComparisonStatus.AreEqual);
        }

        private void workItemTypeResultsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CompareInDiffTool();
        }

        private void compareInDiffToolButton_Click(object sender, RoutedEventArgs e)
        {
            CompareInDiffTool();
        }

        private void CompareInDiffTool()
        {
            if (this.workItemTypeResultsDataGrid.SelectedItem != null)
            {
                var workItemTypeComparison = (WorkItemTypeComparisonResult)this.workItemTypeResultsDataGrid.SelectedItem;
                if (CanCompareInDiffTool(workItemTypeComparison))
                {
                    var sourceResult = (ComparisonSourceComparisonResult)this.sourceResultsDataGrid.SelectedItem;
                    var sourceFile = Path.GetTempFileName();
                    var targetFile = Path.GetTempFileName();
                    try
                    {
                        workItemTypeComparison.NormalizedSourceDefinition.OwnerDocument.Save(sourceFile);
                        workItemTypeComparison.NormalizedTargetDefinition.OwnerDocument.Save(targetFile);

                        var sourceLabel = string.Format(CultureInfo.CurrentCulture, "Work Item Type '{0}' in Source '{1}'", workItemTypeComparison.WorkItemTypeName, sourceResult.Source.Name);
                        var targetLabel = string.Format(CultureInfo.CurrentCulture, "Work Item Type '{0}' in Team Project '{1}'", workItemTypeComparison.WorkItemTypeName, this.Comparison.TeamProject);

                        var processInfo = new ProcessStartInfo(this.diffMergeFileName, string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\" \"{2}\" \"{3}\" /ignorespace", sourceFile, targetFile, sourceLabel, targetLabel));
                        var process = Process.Start(processInfo);
                        process.WaitForExit();
                    }
                    finally
                    {
                        File.Delete(sourceFile);
                        File.Delete(targetFile);
                    }
                }
            }
        }

        private static string GetDiffMergeFileName()
        {
            var idePath = GetVisualStudioIdePath();
            if (idePath != null)
            {
                var diffMergeFileName = Path.Combine(idePath, "diffmerge.exe");
                if (File.Exists(diffMergeFileName))
                {
                    return diffMergeFileName;
                }
            }
            return null;
        }

        private static string GetVisualStudioIdePath()
        {
            using (var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\10.0\Setup\VS"))
            {
                if (regKey != null)
                {
                    return (string)regKey.GetValue("EnvironmentDirectory");
                }
            }
            using (var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\9.0\Setup\VS"))
            {
                if (regKey != null)
                {
                    return (string)regKey.GetValue("EnvironmentDirectory");
                }
            }
            return null;
        }
    }
}