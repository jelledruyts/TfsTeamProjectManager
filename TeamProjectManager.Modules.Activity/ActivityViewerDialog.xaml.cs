using System.Globalization;
using System.Windows;

namespace TeamProjectManager.Modules.Activity
{
    public partial class ActivityViewerDialog : Window
    {
        public TeamProjectActivityInfo Activity { get; private set; }

        public ActivityViewerDialog(TeamProjectActivityInfo activity)
        {
            InitializeComponent();
            this.Activity = activity;
            this.Title = string.Format(CultureInfo.CurrentCulture, "Activity details for Team Project \"{0}\"", activity.TeamProject);
            this.DataContext = this.Activity;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}