using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using System.ComponentModel.Composition;
using TeamProjectManager.Common.Events;

namespace TeamProjectManager.Shell.Modules.TaskHistory
{
    [ModuleExport(typeof(TaskHistoryModule))]
    public class TaskHistoryModule : IModule
    {
        [Import]
        private IEventAggregator EventAggregator { get; set; }

        [Import]
        private TaskHistoryDialog TaskHistoryDialog { get; set; }

        public void Initialize()
        {
            this.EventAggregator.GetEvent<DialogRequestedEvent>().Subscribe(OnDialogRequestedEvent, ThreadOption.UIThread);
        }

        private void OnDialogRequestedEvent(DialogType type)
        {
            if (type == DialogType.TaskHistory)
            {
                this.TaskHistoryDialog.Show();
                this.TaskHistoryDialog.Activate();
            }
        }
    }
}