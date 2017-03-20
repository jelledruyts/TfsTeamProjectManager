using Microsoft.Practices.Prism.Events;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Shell.Modules.TaskHistory
{
    [Export]
    public class TaskHistoryViewModel : ViewModelBase
    {
        #region Properties

        public ObservableCollection<ApplicationTaskViewModel> Tasks { get; private set; }

        #endregion

        #region Observable Properties

        public ApplicationTaskViewModel SelectedTask
        {
            get { return this.GetValue(SelectedTaskProperty); }
            set { this.SetValue(SelectedTaskProperty, value); }
        }

        public static readonly ObservableProperty<ApplicationTaskViewModel> SelectedTaskProperty = new ObservableProperty<ApplicationTaskViewModel, TaskHistoryViewModel>(o => o.SelectedTask);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public TaskHistoryViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Task History")
        {
            this.Tasks = new ObservableCollection<ApplicationTaskViewModel>();
            this.EventAggregator.GetEvent<StatusEvent>().Subscribe(OnStatusEvent, ThreadOption.UIThread);
        }

        #endregion

        #region Event Handlers

        private void OnStatusEvent(StatusEventArgs message)
        {
            if (message.Task != null)
            {
                this.Tasks.Insert(0, new ApplicationTaskViewModel(message.Task));
                if (this.SelectedTask == null)
                {
                    this.SelectedTask = this.Tasks[0];
                }
            }
        }

        #endregion
    }
}