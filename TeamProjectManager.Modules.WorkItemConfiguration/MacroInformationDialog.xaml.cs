using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public partial class MacroInformationDialog : Window
    {
        #region Static Instance Handling

        private static MacroInformationDialog instance;

        public static void ShowInstance()
        {
            if (instance == null)
            {
                instance = new MacroInformationDialog();
                instance.Closed += (sender, e) => { instance = null; };
            }
            instance.Show();
            instance.Activate();
        }

        public static RelayCommand ShowInstanceCommand { get; private set; }

        static MacroInformationDialog()
        {
            ShowInstanceCommand = new RelayCommand((arguments) => { ShowInstance(); });
        }

        #endregion

        #region Properties

        public IList<MacroDefinition> Macros { get; private set; }
        public MacroDefinition SelectedMacro { get; set; }
        public RelayCommand CopyToClipboardCommand { get; private set; }

        #endregion

        #region Constructor

        public MacroInformationDialog()
        {
            InitializeComponent();
            this.Macros = WorkItemConfigurationItemImportExport.GetSupportedMacroDefinitions();
            this.SelectedMacro = this.Macros.FirstOrDefault();
            this.CopyToClipboardCommand = new RelayCommand(CopyToClipboard, CanCopyToClipboard);
            this.DataContext = this;
        }

        #endregion

        #region CopyToClipboard Command

        private bool CanCopyToClipboard(object argument)
        {
            return this.SelectedMacro != null;
        }

        private void CopyToClipboard(object argument)
        {
            Clipboard.SetText(this.SelectedMacro.Macro);
        }

        #endregion

    }
}