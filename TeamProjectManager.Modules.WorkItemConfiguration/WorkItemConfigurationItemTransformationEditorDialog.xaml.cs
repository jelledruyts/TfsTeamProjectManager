using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public partial class WorkItemConfigurationItemTransformationEditorDialog : Window, INotifyPropertyChanged
    {
        #region Properties

        public IList<WorkItemConfigurationItemExport> Items { get; private set; }
        public WorkItemConfigurationItemExport SelectedItem { get; set; }
        public TransformationType TransformationType { get; set; }
        public ImportOptions Options { get; private set; }
        public bool Simulate { get; set; }
        public bool SaveCopy { get; set; }

        #endregion

        #region Constructors

        public WorkItemConfigurationItemTransformationEditorDialog(IList<WorkItemConfigurationItemExport> items, string itemType)
        {
            InitializeComponent();
            this.Items = items ?? new WorkItemConfigurationItemExport[0];
            this.SelectedItem = this.Items.FirstOrDefault();
            this.Title = "Transforming " + this.Items.Count.ToCountString(itemType);
            this.DataContext = this;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Event Handlers

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyTransformation())
            {
                this.Options = ImportOptions.None;
                if (this.Simulate)
                {
                    this.Options |= ImportOptions.Simulate;
                }
                if (this.SaveCopy)
                {
                    this.Options |= ImportOptions.SaveCopy;
                }
                this.DialogResult = true;
                this.Close();
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void inputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PreviewTransformation();
        }

        private void transformationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PreviewTransformation();
        }

        private void transformationType_Changed(object sender, RoutedEventArgs e)
        {
            PreviewTransformation();
        }

        private void loadTransformation_Click(object sender, RoutedEventArgs e)
        {
            LoadTransformation();
        }

        #endregion

        #region Helper Methods

        private void LoadTransformation()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the transformation file to load.";
            dialog.Filter = "All Files (*.*)|*.*|XDT Files (*.xdt)|*.xdt|XSLT Files (*.xslt)|*.xslt";
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                this.transformationTextBox.Text = File.ReadAllText(dialog.FileName);
                var extension = Path.GetExtension(dialog.FileName);
                if (this.transformationTextBox.Text.IndexOf("http://www.w3.org/1999/XSL/Transform", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    this.TransformationType = TransformationType.Xslt;
                }
                else
                {
                    this.TransformationType = TransformationType.Xdt;
                }
                OnPropertyChanged("TransformationType");
            }
        }

        private void PreviewTransformation()
        {
            try
            {
                this.outputTextBox.Foreground = (Brush)TextBox.ForegroundProperty.DefaultMetadata.DefaultValue;
                var input = this.inputTextBox.Text;
                var transformation = this.transformationTextBox.Text;
                var output = string.Empty;
                if (!string.IsNullOrWhiteSpace(input) && !string.IsNullOrWhiteSpace(transformation))
                {
                    // Perform the transformation.
                    output = WorkItemConfigurationTransformer.Transform(this.TransformationType, input, transformation);

                    // Also replace macros to complete the preview.
                    output = WorkItemConfigurationItemImportExport.ReplaceTeamProjectMacros(output, this.SelectedItem.TeamProject);
                }
                this.outputTextBox.Text = output;
            }
            catch (Exception exc)
            {
                this.outputTextBox.Foreground = Brushes.Red;
                this.outputTextBox.Text = exc.ToString();
            }
        }


        private bool ApplyTransformation()
        {
            var transformXml = this.transformationTextBox.Text;
            try
            {
                this.outputTextBox.Foreground = (Brush)TextBox.ForegroundProperty.DefaultMetadata.DefaultValue;
                this.outputTextBox.Text = string.Empty;

                // First try to apply the transformation to all items.
                var transformations = new Dictionary<WorkItemConfigurationItemExport, XmlDocument>();
                foreach (var item in this.Items)
                {
                    this.outputTextBox.Text += "Applying transformation to {0} ({1})...".FormatCurrent(item.Item.DisplayName, item.TeamProject.Name);
                    this.outputTextBox.ScrollToEnd();
                    var result = WorkItemConfigurationTransformer.Transform(this.TransformationType, item.Item.XmlDefinition, transformXml);
                    transformations.Add(item, result);
                }

                // Only commit if everything succeeded.
                foreach (var item in this.Items)
                {
                    item.Item.XmlDefinition = transformations[item];
                }

                return true;
            }
            catch (Exception exc)
            {
                // Don't close but show the exception.
                this.outputTextBox.Foreground = Brushes.Red;
                this.outputTextBox.Text += Environment.NewLine + exc.ToString();
                this.outputTextBox.ScrollToEnd();
                return false;
            }
        }

        #endregion
    }
}