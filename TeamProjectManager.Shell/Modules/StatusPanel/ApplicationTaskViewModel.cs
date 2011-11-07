using System;
using System.Collections.Specialized;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Shell.Modules.StatusPanel
{
    public class ApplicationTaskViewModel : ObservableObject
    {
        #region Properties

        /// <summary>
        /// Gets the task.
        /// </summary>
        public ApplicationTask Task { get; private set; }

        /// <summary>
        /// Gets the command that shows the details.
        /// </summary>
        public RelayCommand ShowDetailsCommand { get; private set; }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Gets or sets the description of the task's status history.
        /// </summary>
        public string StatusHistoryDescription
        {
            get { return this.GetValue(StatusHistoryDescriptionProperty); }
            set { this.SetValue(StatusHistoryDescriptionProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="StatusHistoryDescription"/> observable property.
        /// </summary>
        public static ObservableProperty<string> StatusHistoryDescriptionProperty = new ObservableProperty<string, ApplicationTaskViewModel>(o => o.StatusHistoryDescription);

        /// <summary>
        /// Gets or sets a value that determines if there are task details available.
        /// </summary>
        public bool DetailsAvailable
        {
            get { return this.GetValue(DetailsAvailableProperty); }
            set { this.SetValue(DetailsAvailableProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="DetailsAvailable"/> observable property.
        /// </summary>
        public static ObservableProperty<bool> DetailsAvailableProperty = new ObservableProperty<bool, ApplicationTaskViewModel>(o => o.DetailsAvailable, false);

        /// <summary>
        /// Gets or sets a value that determines if the task details are visible.
        /// </summary>
        public bool DetailsVisible
        {
            get { return this.GetValue(DetailsVisibleProperty); }
            set { this.SetValue(DetailsVisibleProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="DetailsVisible"/> observable property.
        /// </summary>
        public static ObservableProperty<bool> DetailsVisibleProperty = new ObservableProperty<bool, ApplicationTaskViewModel>(o => o.DetailsVisible);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationTaskViewModel"/> class.
        /// </summary>
        /// <param name="task">The task.</param>
        public ApplicationTaskViewModel(ApplicationTask task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }
            this.Task = task;
            task.StatusHistory.CollectionChanged += new NotifyCollectionChangedEventHandler(TaskStatusHistory_CollectionChanged);
            this.ShowDetailsCommand = new RelayCommand((o) => { this.DetailsVisible = true; }, (o) => { return this.DetailsAvailable; });
            this.DetailsAvailable = (this.Task.StatusHistory.Count > 0);
            if (this.Task.StatusHistory != null)
            {
                var status = string.Empty;
                foreach (string item in this.Task.StatusHistory)
                {
                    status += Environment.NewLine + item;
                }
                this.StatusHistoryDescription = status.Trim();
            }
        }

        #endregion

        #region Event Handlers

        private void TaskStatusHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // The task status history was updated, append the text to the view model's description.
            var status = this.StatusHistoryDescription;
            foreach (string item in e.NewItems)
            {
                status += Environment.NewLine + item;
            }
            this.StatusHistoryDescription = status.Trim();
            this.DetailsAvailable = (Task.StatusHistory.Count > 0);
        }

        #endregion
    }
}