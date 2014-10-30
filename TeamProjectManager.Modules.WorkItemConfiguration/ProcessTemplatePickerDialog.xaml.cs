using Microsoft.TeamFoundation.Server;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public partial class ProcessTemplatePickerDialog : Window
    {
        public IList<TemplateHeader> ProcessTemplates
        {
            get
            {
                return (IList<TemplateHeader>)this.processTemplatesListBox.ItemsSource;
            }
            private set
            {
                this.processTemplatesListBox.ItemsSource = value;
            }
        }

        public TemplateHeader SelectedProcessTemplate
        {
            get { return (TemplateHeader)this.processTemplatesListBox.SelectedItem; }
        }

        public ProcessTemplatePickerDialog(IList<TemplateHeader> processTemplates)
        {
            InitializeComponent();
            this.ProcessTemplates = processTemplates;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void processTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.okButton.IsEnabled = this.SelectedProcessTemplate != null;
        }
    }
}