using System;
using System.Collections.Specialized;
using System.Linq;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Shell.Modules.TaskHistory
{
    public class ApplicationTaskViewModel : ObservableObject
    {
        #region Properties

        public ApplicationTask Task { get; private set; }

        #endregion

        #region Observable Properties

        public string StatusHistoryDescription
        {
            get { return this.GetValue(StatusHistoryDescriptionProperty); }
            set { this.SetValue(StatusHistoryDescriptionProperty, value); }
        }

        public static ObservableProperty<string> StatusHistoryDescriptionProperty = new ObservableProperty<string, ApplicationTaskViewModel>(o => o.StatusHistoryDescription);

        public string State
        {
            get { return this.GetValue(StateProperty); }
            set { this.SetValue(StateProperty, value); }
        }

        public static readonly ObservableProperty<string> StateProperty = new ObservableProperty<string, ApplicationTaskViewModel>(o => o.State);

        #endregion

        #region Constructors

        public ApplicationTaskViewModel(ApplicationTask task)
        {
            this.Task = task;
            this.Task.ObservablePropertyChanged += Task_ObservablePropertyChanged;
            this.Task.StatusHistory.CollectionChanged += TaskStatusHistory_CollectionChanged;
            if (this.Task.StatusHistory != null)
            {
                var status = string.Empty;
                foreach (string item in this.Task.StatusHistory.ToList())
                {
                    status += Environment.NewLine + item;
                }
                this.StatusHistoryDescription = status.Trim();
            }
            RefreshState();
        }

        #endregion

        #region Event Handlers

        private void Task_ObservablePropertyChanged(object sender, ObservablePropertyChangedEventArgs e)
        {
            RefreshState();
        }

        private void TaskStatusHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // The task status history was updated, append the text to the view model's description.
            var status = this.StatusHistoryDescription;
            foreach (string item in e.NewItems)
            {
                status += Environment.NewLine + item;
            }
            this.StatusHistoryDescription = status.Trim();
        }

        #endregion

        #region Helper Methods

        private void RefreshState()
        {
            this.State = this.Task.IsError ? "Error" : (this.Task.IsWarning ? "Warning" : (this.Task.IsCanceled ? "Canceled" : (this.Task.IsComplete ? "Completed" : "In Progress")));
        }

        #endregion
    }
}