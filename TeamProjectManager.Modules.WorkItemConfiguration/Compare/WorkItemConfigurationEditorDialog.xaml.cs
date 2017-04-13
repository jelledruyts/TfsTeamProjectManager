using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.UI;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Compare
{
    public partial class WorkItemConfigurationEditorDialog : Window
    {
        public WorkItemConfiguration Configuration
        {
            get
            {
                return (WorkItemConfiguration)this.DataContext;
            }
            private set
            {
                this.DataContext = value;
                UpdateUI();
            }
        }

        public WorkItemConfigurationEditorDialog()
            : this(new WorkItemConfiguration(), true)
        {
        }

        public WorkItemConfigurationEditorDialog(WorkItemConfiguration configuration)
            : this(configuration, false)
        {
        }

        private WorkItemConfigurationEditorDialog(WorkItemConfiguration configuration, bool canCancel)
        {
            InitializeComponent();
            this.Configuration = configuration;
            this.cancelButton.IsEnabled = canCancel;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            this.nameTextBox.Focus();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void nameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateUI();
        }

        private void addItemsFromFilesHyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog();
                dialog.Title = "Please select the work item configuration files (*.xml) to compare with.";
                dialog.Filter = "XML Files (*.xml)|*.xml";
                dialog.Multiselect = true;
                var result = dialog.ShowDialog(Application.Current.MainWindow);
                if (result == true)
                {
                    foreach (var file in dialog.FileNames)
                    {
                        try
                        {
                            this.Configuration.Items.Add(WorkItemConfigurationItem.FromFile(file));
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show("An error occurred while loading the work item configuration file: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    UpdateUI();
                }
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        private void importFromTeamProjectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false))
                {
                    var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                    if (result == System.Windows.Forms.DialogResult.OK && dialog.SelectedProjects != null && dialog.SelectedProjects.Length > 0)
                    {
                        try
                        {
                            this.IsEnabled = false;

                            var teamProjectCollection = dialog.SelectedTeamProjectCollection;
                            var teamProject = dialog.SelectedProjects.First();
                            var tfs = TfsTeamProjectCollectionCache.GetTfsTeamProjectCollection(teamProjectCollection.Uri);
                            var store = tfs.GetService<WorkItemStore>();
                            var project = store.Projects[teamProject.Name];

                            this.Configuration = WorkItemConfiguration.FromTeamProject(tfs, project);
                        }
                        finally
                        {
                            this.IsEnabled = true;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        private void importFromProcessTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog();
                dialog.Title = "Please select the Process Template file that defines the configuration files to compare with.";
                dialog.Filter = "Process Template XML Files (ProcessTemplate.xml)|ProcessTemplate.xml";
                var result = dialog.ShowDialog(Application.Current.MainWindow);
                if (result == true)
                {
                    try
                    {
                        this.Configuration = WorkItemConfiguration.FromProcessTemplate(dialog.FileName);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("An error occurred while loading the process template: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        private void importFromRegisteredProcessTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.NoProject, false))
                {
                    var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        var projectCollection = dialog.SelectedTeamProjectCollection;
                        var processTemplateService = projectCollection.GetService<IProcessTemplates>();
                        var registeredTemplates = processTemplateService.TemplateHeaders().OrderBy(t => t.Rank).ToArray();

                        var processTemplatePicker = new ItemsPickerDialog();
                        processTemplatePicker.Title = "Please select a Process Template";
                        processTemplatePicker.SelectionMode = SelectionMode.Single;
                        processTemplatePicker.AvailableItems = registeredTemplates;
                        processTemplatePicker.ItemDisplayMemberPath = nameof(TemplateHeader.Name);
                        processTemplatePicker.Owner = this;
                        if (processTemplatePicker.ShowDialog() == true)
                        {
                            var selectedProcessTemplate = (TemplateHeader)processTemplatePicker.SelectedItem;
                            if (selectedProcessTemplate != null)
                            {
                                var downloadedTemplateZipFileName = processTemplateService.GetTemplateData(selectedProcessTemplate.TemplateId);
                                var unzipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                                try
                                {
                                    ZipFile.ExtractToDirectory(downloadedTemplateZipFileName, unzipPath);
                                    var processTemplateXmlFile = Path.Combine(unzipPath, "ProcessTemplate.xml");
                                    if (!File.Exists(processTemplateXmlFile))
                                    {
                                        MessageBox.Show("The selected Process Template did not contain a \"ProcessTemplate.xml\" file in its root directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                    else
                                    {
                                        this.Configuration = WorkItemConfiguration.FromProcessTemplate(processTemplateXmlFile);
                                    }
                                }
                                finally
                                {
                                    File.Delete(downloadedTemplateZipFileName);
                                    Directory.Delete(unzipPath, true);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        private void removeHyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in this.itemsDataGrid.SelectedItems.Cast<WorkItemConfigurationItem>().ToList())
                {
                    this.Configuration.Items.Remove(item);
                }
                UpdateUI();
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        private void itemsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            this.removeHyperlink.IsEnabled = this.itemsDataGrid.SelectedItems.Count > 0;
        }

        private void ShowException(Exception exc)
        {
            MessageBox.Show("An error occurred: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}