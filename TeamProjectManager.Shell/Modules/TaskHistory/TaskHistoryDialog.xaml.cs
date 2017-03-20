using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;

namespace TeamProjectManager.Shell.Modules.TaskHistory
{
    [Export(typeof(TaskHistoryDialog))]
    public partial class TaskHistoryDialog : Window
    {
        public TaskHistoryDialog()
        {
            InitializeComponent();
        }

        [Import]
        public TaskHistoryViewModel ViewModel
        {
            get
            {
                return (TaskHistoryViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void statusHistoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).ScrollToEnd();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}