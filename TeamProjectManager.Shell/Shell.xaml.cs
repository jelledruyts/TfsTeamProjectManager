using System.ComponentModel.Composition;
using System.Windows;

namespace TeamProjectManager.Shell
{
    [Export]
    public partial class Shell : Window
    {
        public Shell()
        {
            InitializeComponent();
        }

        [Import]
        public ShellViewModel ViewModel
        {
            get
            {
                return (ShellViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }
    }
}