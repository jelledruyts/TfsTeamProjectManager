using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Timers;
using System.Windows.Shell;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Shell.Infrastructure;

namespace TeamProjectManager.Shell.Modules.StatusPanel
{
    [Export]
    public class StatusPanelViewModel : ViewModelBase
    {
        #region Fields

        private Timer removeTasksTimer;
        private System.Threading.ReaderWriterLockSlim executingTasksLock = new System.Threading.ReaderWriterLockSlim();

        #endregion

        #region Properties

        public ObservableCollection<ApplicationTaskViewModel> ExecutingTasks { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusPanelViewModel"/> class.
        /// </summary>
        [ImportingConstructor]
        public StatusPanelViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Status Panel")
        {
            this.removeTasksTimer = new Timer(1000); // Check every second to remove tasks.
            this.removeTasksTimer.Elapsed += new ElapsedEventHandler(OnRemoveTasksTimerElapsed);
            this.ExecutingTasks = new ObservableCollection<ApplicationTaskViewModel>();
            this.EventAggregator.GetEvent<StatusEvent>().Subscribe(OnStatusEvent, ThreadOption.UIThread);
        }

        #endregion

        #region Message Handlers

        private void OnStatusEvent(StatusEventArgs message)
        {
            var task = message.Task;
            if (task == null)
            {
                task = new ApplicationTask(message.EventType.ToString());
                if (message.Details != null)
                {
                    task.Status = message.Details;
                }
                if (message.Exception != null)
                {
                    task.SetError(message.Exception);
                }
                else if (message.EventType <= TraceEventType.Error)
                {
                    task.SetError();
                }
                else if (message.EventType == TraceEventType.Warning)
                {
                    task.SetWarning();
                }
                task.SetComplete(message.Message);
            }
            this.executingTasksLock.EnterWriteLock();
            try
            {
                this.ExecutingTasks.Insert(0, new ApplicationTaskViewModel(task, this.Logger));
            }
            finally
            {
                this.executingTasksLock.ExitWriteLock();
            }
            this.removeTasksTimer.Enabled = true;
            task.StatusChanged += new EventHandler<EventArgs>(OnTaskStatusChanged);
            UpdateStatus();
        }

        #endregion

        #region Event Handlers

        private void OnRemoveTasksTimerElapsed(object sender, ElapsedEventArgs e)
        {
            ApplicationTaskViewModel[] completedTasks = null;
            this.executingTasksLock.EnterReadLock();
            try
            {
                // Remove all tasks that completed more than 30 seconds ago.
                completedTasks = this.ExecutingTasks.Where(t => !t.DetailsVisible && t.Task.TimeCompleted.HasValue && t.Task.TimeCompleted < DateTimeOffset.Now.Subtract(TimeSpan.FromSeconds(30))).ToArray();
            }
            finally
            {
                this.executingTasksLock.ExitReadLock();
            }

            if (completedTasks != null && completedTasks.Length > 0)
            {
                this.ExecuteUIAction(() =>
                {
                    this.executingTasksLock.EnterWriteLock();
                    try
                    {
                        foreach (var task in completedTasks)
                        {
                            this.ExecutingTasks.Remove(task);
                            task.Task.StatusChanged -= new EventHandler<EventArgs>(OnTaskStatusChanged);
                        }
                        this.removeTasksTimer.Enabled = (this.ExecutingTasks.Count > 0);
                    }
                    finally
                    {
                        this.executingTasksLock.ExitWriteLock();
                    }
                });
            }
        }

        private void OnTaskStatusChanged(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        #endregion

        #region Helper Methods

        private void UpdateStatus()
        {
            this.executingTasksLock.EnterReadLock();
            try
            {
                // A task changed, update the global status.
                var anyTaskStillRunning = !this.ExecutingTasks.All(t => t.Task.IsComplete);
                if (anyTaskStillRunning)
                {
                    // Calculate the total progress.
                    var incompleteTaskCount = this.ExecutingTasks.Where(t => !t.Task.IsComplete).Count();
                    var progressTasks = this.ExecutingTasks.Where(t => t.Task.PercentComplete.HasValue);
                    if (progressTasks.Count() > 0)
                    {
                        // There are tasks with progress, set the total progress value on the task bar.
                        var totalPercentComplete = progressTasks.Sum(t => t.Task.PercentComplete.Value) / progressTasks.Count();
                        var state = (progressTasks.Any(t => t.Task.IsError) ? TaskbarItemProgressState.Error : TaskbarItemProgressState.Normal);
                        SetProgress(state, totalPercentComplete, incompleteTaskCount);
                    }
                    else
                    {
                        // There are tasks but they are not reporting their progress, set an indeterminate state.
                        SetProgress(TaskbarItemProgressState.Indeterminate, null, incompleteTaskCount);
                    }
                }
                else
                {
                    SetProgress(TaskbarItemProgressState.None);
                }
            }
            finally
            {
                this.executingTasksLock.ExitReadLock();
            }
        }

        private void SetProgress(TaskbarItemProgressState state)
        {
            SetProgress(state, null, null);
        }

        private void SetProgress(TaskbarItemProgressState state, double? percentComplete, int? incompleteTaskCount)
        {
            var statusService = ServiceLocator.Current.GetInstance<IStatusService>();
            if (statusService != null)
            {
                this.ExecuteUIActionAsync(() =>
                {
                    if (percentComplete.HasValue && incompleteTaskCount.HasValue)
                    {
                        var taskCountMessage = incompleteTaskCount.Value.ToCountString("task");
                        var progressMessage = string.Format(CultureInfo.CurrentCulture, "Executing {0} ({1} complete)", taskCountMessage, percentComplete.Value.ToPercentageString());
                        statusService.SetMainWindowTitleStatus(progressMessage);
                        statusService.SetMainWindowProgress(state, percentComplete.Value);
                    }
                    else if (incompleteTaskCount.HasValue)
                    {
                        var taskCountMessage = incompleteTaskCount.Value.ToCountString("task");
                        var progressMessage = string.Format(CultureInfo.CurrentCulture, "Executing {0}", taskCountMessage);
                        statusService.SetMainWindowTitleStatus(progressMessage);
                        statusService.SetMainWindowProgress(state);
                    }
                    else
                    {
                        statusService.ClearMainWindowTitleStatus();
                        statusService.SetMainWindowProgress(state);
                    }
                });
            }
        }

        #endregion
    }
}