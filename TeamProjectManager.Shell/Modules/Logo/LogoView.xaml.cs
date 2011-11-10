using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace TeamProjectManager.Shell.Modules.Logo
{
    [Export]
    public partial class LogoView : UserControl
    {
        public LogoView()
        {
            InitializeComponent();
        }

        [Import]
        public LogoViewModel ViewModel
        {
            get
            {
                return (LogoViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void downloadNewVersionHyperLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri != null)
            {
                Process.Start(e.Uri.ToString());
            }
        }
    }
}