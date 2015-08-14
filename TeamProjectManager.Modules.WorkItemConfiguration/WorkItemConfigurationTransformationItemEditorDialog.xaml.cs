using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public partial class WorkItemConfigurationTransformationItemEditorDialog : Window
    {
        #region Properties

        public WorkItemConfigurationTransformationItem Transformation
        {
            get
            {
                return (WorkItemConfigurationTransformationItem)this.DataContext;
            }
            private set
            {
                this.DataContext = value;
            }
        }

        #endregion

        #region Constructors

        public WorkItemConfigurationTransformationItemEditorDialog()
            : this(new WorkItemConfigurationTransformationItem(), true)
        {
        }

        public WorkItemConfigurationTransformationItemEditorDialog(WorkItemConfigurationTransformationItem transformation)
            : this(transformation, false)
        {
        }

        private WorkItemConfigurationTransformationItemEditorDialog(WorkItemConfigurationTransformationItem transformation, bool canCancel)
        {
            InitializeComponent();
            this.Transformation = transformation;
            this.cancelButton.IsEnabled = canCancel;
            this.workItemConfigurationItemTypesComboBox.ItemsSource = Enum.GetValues(typeof(WorkItemConfigurationItemType)).Cast<WorkItemConfigurationItemType>().Select(t => new { Key = t, Value = WorkItemConfigurationItem.GetDisplayName(t) });
        }

        #endregion

        #region Event Handlers

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void loadTransformationXmlButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the transformation file to load.";
            dialog.Filter = "All Files (*.*)|*.*|XDT Files (*.xdt)|*.xdt|XSLT Files (*.xslt)|*.xslt";
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                this.Transformation.TransformationXml = File.ReadAllText(dialog.FileName);
                if (string.IsNullOrEmpty(this.Transformation.Description))
                {
                    this.Transformation.Description = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
                var extension = Path.GetExtension(dialog.FileName);
                if (this.Transformation.TransformationXml.IndexOf("http://www.w3.org/1999/XSL/Transform", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    this.Transformation.TransformationType = TransformationType.Xslt;
                }
                else
                {
                    this.Transformation.TransformationType = TransformationType.Xdt;
                }
            }
        }

        #endregion
    }
}