using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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

        private void sourceResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.workItemTypeResultsListBox.SelectedIndex = 0;
        }

        private void workItemTypeResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var workItemTypeComparison = (WorkItemTypeComparisonResult)this.workItemTypeResultsListBox.SelectedItem;
            this.compareInDiffToolButton.IsEnabled = this.diffMergeFileName != null && workItemTypeComparison != null && (workItemTypeComparison.Status == ComparisonStatus.AreDifferent || workItemTypeComparison.Status == ComparisonStatus.AreEqual);
        }

        private void compareInDiffToolButton_Click(object sender, RoutedEventArgs e)
        {
            var workItemTypeComparison = (WorkItemTypeComparisonResult)this.workItemTypeResultsListBox.SelectedItem;
            var sourceFile = Path.GetTempFileName();
            var targetFile = Path.GetTempFileName();
            try
            {
                workItemTypeComparison.SourceWorkItemType.XmlDefinition.Save(sourceFile);
                workItemTypeComparison.TargetWorkItemType.XmlDefinition.Save(targetFile);

                // TODO: Allow customization of diffmerge tool and use parameters documented at
                // http://blogs.msdn.com/b/jmanning/archive/2006/02/20/diff-merge-configuration-in-team-foundation-common-command-and-argument-values.aspx
                var processInfo = new ProcessStartInfo(this.diffMergeFileName, string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\"", sourceFile, targetFile));
                var process = Process.Start(processInfo);
                process.WaitForExit();
            }
            finally
            {
                File.Delete(sourceFile);
                File.Delete(targetFile);
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