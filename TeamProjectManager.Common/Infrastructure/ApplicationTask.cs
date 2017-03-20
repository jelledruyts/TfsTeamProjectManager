using System;
using System.Collections.ObjectModel;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    ///  A task executing within an application that can track status, progress and errors.
    /// </summary>
    public class ApplicationTask : ObservableObject
    {
        #region Properties

        /// <summary>
        /// Gets the name of the task.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the time the task started.
        /// </summary>
        public DateTimeOffset TimeStarted { get; private set; }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Gets or sets the status of the task.
        /// </summary>
        public string Status
        {
            get { return this.GetValue(StatusProperty); }
            set { this.SetValue(StatusProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="Status"/> observable property.
        /// </summary>
        public static ObservableProperty<string> StatusProperty = new ObservableProperty<string, ApplicationTask>(o => o.Status, OnStatusChanged);

        /// <summary>
        /// Gets or sets the percentage this task has completed.
        /// </summary>
        public double? PercentComplete
        {
            get { return this.GetValue(PercentCompleteProperty); }
            private set { this.SetValue(PercentCompleteProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="PercentComplete"/> observable property.
        /// </summary>
        public static ObservableProperty<double?> PercentCompleteProperty = new ObservableProperty<double?, ApplicationTask>(o => o.PercentComplete);

        /// <summary>
        /// Gets or sets a value that determines if this task has completed.
        /// </summary>
        public bool IsComplete
        {
            get { return this.GetValue(IsCompleteProperty); }
            private set { this.SetValue(IsCompleteProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="IsComplete"/> observable property.
        /// </summary>
        public static ObservableProperty<bool> IsCompleteProperty = new ObservableProperty<bool, ApplicationTask>(o => o.IsComplete);

        /// <summary>
        /// Gets or sets the time the task completed.
        /// </summary>
        public DateTimeOffset? TimeCompleted
        {
            get { return this.GetValue(TimeCompletedProperty); }
            private set { this.SetValue(TimeCompletedProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="TimeCompleted"/> observable property.
        /// </summary>
        public static ObservableProperty<DateTimeOffset?> TimeCompletedProperty = new ObservableProperty<DateTimeOffset?, ApplicationTask>(o => o.TimeCompleted);

        /// <summary>
        /// Gets or sets the history of statuses for this task.
        /// </summary>
        public ObservableCollection<string> StatusHistory
        {
            get { return this.GetValue(StatusHistoryProperty); }
            private set { this.SetValue(StatusHistoryProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="StatusHistory"/> observable property.
        /// </summary>
        public static ObservableProperty<ObservableCollection<string>> StatusHistoryProperty = new ObservableProperty<ObservableCollection<string>, ApplicationTask>(o => o.StatusHistory);

        /// <summary>
        /// Gets a value that determines if this task is in a warning state.
        /// </summary>
        public bool IsWarning
        {
            get { return this.GetValue(IsWarningProperty); }
            private set { this.SetValue(IsWarningProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="IsWarning"/> observable property.
        /// </summary>
        public static ObservableProperty<bool> IsWarningProperty = new ObservableProperty<bool, ApplicationTask>(o => o.IsWarning);

        /// <summary>
        /// Gets a value that determines if this task is in an error state.
        /// </summary>
        public bool IsError
        {
            get { return this.GetValue(IsErrorProperty); }
            private set { this.SetValue(IsErrorProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="IsError"/> observable property.
        /// </summary>
        public static ObservableProperty<bool> IsErrorProperty = new ObservableProperty<bool, ApplicationTask>(o => o.IsError);

        /// <summary>
        /// Gets the total number of steps to be completed in this task.
        /// </summary>
        public int? TotalSteps
        {
            get { return this.GetValue(TotalStepsProperty); }
            private set { this.SetValue(TotalStepsProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="TotalSteps"/> observable property.
        /// </summary>
        public static ObservableProperty<int?> TotalStepsProperty = new ObservableProperty<int?, ApplicationTask>(o => o.TotalSteps);

        /// <summary>
        /// Gets the current step.
        /// </summary>
        public int? CurrentStep
        {
            get { return this.GetValue(CurrentStepProperty); }
            private set { this.SetValue(CurrentStepProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="CurrentStep"/> observable property.
        /// </summary>
        public static ObservableProperty<int?> CurrentStepProperty = new ObservableProperty<int?, ApplicationTask>(o => o.CurrentStep);

        /// <summary>
        /// Gets a value that determines if this task can be canceled.
        /// </summary>
        public bool CanCancel
        {
            get { return this.GetValue(CanCancelProperty); }
            private set { this.SetValue(CanCancelProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="CanCancel"/> observable property.
        /// </summary>
        public static ObservableProperty<bool> CanCancelProperty = new ObservableProperty<bool, ApplicationTask>(o => o.CanCancel);

        /// <summary>
        /// Gets a value that determines if this task was requested to be canceled.
        /// </summary>
        public bool IsCanceled
        {
            get { return this.GetValue(IsCanceledProperty); }
            private set { this.SetValue(IsCanceledProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="IsCanceled"/> observable property.
        /// </summary>
        public static ObservableProperty<bool> IsCanceledProperty = new ObservableProperty<bool, ApplicationTask>(o => o.IsCanceled);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationTask"/> class.
        /// </summary>
        /// <param name="name">The name of this task.</param>
        public ApplicationTask(string name)
            : this(name, null, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationTask"/> class.
        /// </summary>
        /// <param name="name">The name of this task.</param>
        /// <param name="totalSteps">The total number of steps to be completed in this task.</param>
        public ApplicationTask(string name, int? totalSteps)
            : this(name, totalSteps, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationTask"/> class.
        /// </summary>
        /// <param name="name">The name of this task.</param>
        /// <param name="canCancel">A value that determines if this task can be canceled.</param>
        public ApplicationTask(string name, bool canCancel)
            : this(name, null, canCancel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationTask"/> class.
        /// </summary>
        /// <param name="name">The name of this task.</param>
        /// <param name="totalSteps">The total number of steps to be completed in this task.</param>
        /// <param name="canCancel">A value that determines if this task can be canceled.</param>
        public ApplicationTask(string name, int? totalSteps, bool canCancel)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("The name must not be empty.");
            }
            this.Name = name;
            this.TimeStarted = DateTimeOffset.Now;
            this.StatusHistory = new ObservableCollection<string>();
            this.TotalSteps = totalSteps;
            this.CanCancel = canCancel;
        }

        #endregion

        #region Set Progress

        /// <summary>
        /// Sets the progress of this task.
        /// </summary>
        /// <param name="step">The current step this task has completed.</param>
        /// <param name="status">The current status of the task.</param>
        public void SetProgress(int step, string status)
        {
            if (!this.TotalSteps.HasValue)
            {
                throw new InvalidOperationException("The total number of steps must be known when setting progress with a step number.");
            }
            this.CurrentStep = step;
            SetProgress((double)this.CurrentStep.Value / (double)this.TotalSteps.Value, status);
        }

        /// <summary>
        /// Sets the incremental progress of the current step.
        /// </summary>
        /// <param name="stepPercentComplete">The percentage the current step has completed.</param>
        public void SetProgressForCurrentStep(double stepPercentComplete)
        {
            SetProgressForCurrentStep(stepPercentComplete, null);
        }

        /// <summary>
        /// Sets the incremental progress of the current step.
        /// </summary>
        /// <param name="stepPercentComplete">The percentage the current step has completed.</param>
        /// <param name="status">The current status of the task.</param>
        public void SetProgressForCurrentStep(double stepPercentComplete, string status)
        {
            if (!this.TotalSteps.HasValue)
            {
                throw new InvalidOperationException("The total number of steps must be known when setting progress for the current step.");
            }
            var taskProgress = (double)this.CurrentStep.Value / (double)this.TotalSteps.Value;
            var stepProgress = stepPercentComplete / this.TotalSteps.Value;
            SetProgress(taskProgress + stepProgress, status);
        }

        /// <summary>
        /// Sets the progress of this task.
        /// </summary>
        /// <param name="percentComplete">The percentage this task has completed.</param>
        /// <param name="status">The current status of the task.</param>
        public void SetProgress(double percentComplete, string status)
        {
            this.PercentComplete = percentComplete;
            if (!string.IsNullOrEmpty(status))
            {
                this.Status = status;
            }
            else
            {
                // The status property itself didn't change but the general task status did.
                this.OnStatusChanged(EventArgs.Empty);
            }
        }

        #endregion

        #region Set Complete

        /// <summary>
        /// Marks this task complete.
        /// </summary>
        public void SetComplete()
        {
            SetComplete(null);
        }

        /// <summary>
        /// Marks this task complete.
        /// </summary>
        /// <param name="completedMessage">The completed message.</param>
        public void SetComplete(string completedMessage)
        {
            this.PercentComplete = null;
            this.IsComplete = true;
            this.TimeCompleted = DateTimeOffset.Now;
            if (!string.IsNullOrEmpty(completedMessage))
            {
                this.Status = completedMessage;
            }
            else
            {
                // The status property itself didn't change but the general task status did.
                this.OnStatusChanged(EventArgs.Empty);
            }
        }

        #endregion

        #region Set Warning

        /// <summary>
        /// Sets this task in a warning state.
        /// </summary>
        public void SetWarning()
        {
            SetWarning(null, null);
        }

        /// <summary>
        /// Sets this task in a warning state.
        /// </summary>
        /// <param name="warningMessage">The warning message.</param>
        public void SetWarning(string warningMessage)
        {
            SetWarning(warningMessage, null);
        }

        /// <summary>
        /// Sets this task in a warning state.
        /// </summary>
        /// <param name="exception">The exception that caused the warning.</param>
        public void SetWarning(Exception exception)
        {
            SetWarning(null, exception);
        }

        /// <summary>
        /// Sets this task in a warning state.
        /// </summary>
        /// <param name="warningMessage">The warning message.</param>
        /// <param name="exception">The exception that caused the warning.</param>
        public void SetWarning(string warningMessage, Exception exception)
        {
            this.IsWarning = true;
            SetStatus(warningMessage, exception);
        }

        #endregion

        #region Set Error

        /// <summary>
        /// Marks this task as in error.
        /// </summary>
        public void SetError()
        {
            SetError(null, null);
        }

        /// <summary>
        /// Marks this task as in error.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public void SetError(string errorMessage)
        {
            SetError(errorMessage, null);
        }

        /// <summary>
        /// Marks this task as in error.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        public void SetError(Exception exception)
        {
            SetError(null, exception);
        }

        /// <summary>
        /// Marks this task as in error.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that caused the error.</param>
        public void SetError(string errorMessage, Exception exception)
        {
            this.IsWarning = false;
            this.IsError = true;
            SetStatus(errorMessage, exception);
        }

        #endregion

        #region Request Cancel

        /// <summary>
        /// Requests that this task is canceled.
        /// </summary>
        public void RequestCancel()
        {
            if (!this.CanCancel)
            {
                throw new InvalidOperationException("The task does not support cancellation.");
            }
            this.IsCanceled = true;
        }

        #endregion

        #region Helper Methods

        private void SetStatus(string message, Exception exception)
        {
            // Determine the status.
            string status = null;
            if (!string.IsNullOrEmpty(message))
            {
                status = message;
                if (exception != null)
                {
                    status += ": " + exception.Message;
                }
            }
            else if (exception != null)
            {
                status = exception.Message;
            }

            // Set the status.
            if (!string.IsNullOrEmpty(status))
            {
                this.Status = status;
            }
            else
            {
                // The status property itself didn't change but the general task status did.
                this.OnStatusChanged(EventArgs.Empty);
            }

            // Add the exception details directly to the history.
            if (exception != null)
            {
                this.StatusHistory.Add(exception.ToString());
            }
        }

        #endregion

        #region StatusChanged Event

        /// <summary>
        /// Called when the status property changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ObservablePropertyChangedEventArgs{TProperty}"/> instance containing the event data.</param>
        private static void OnStatusChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<string> e)
        {
            // Add a history entry automatically.
            var task = (ApplicationTask)sender;
            if (!string.IsNullOrEmpty(e.NewValue))
            {
                task.StatusHistory.Add(e.NewValue);
            }
            task.OnStatusChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the task's status has changed.
        /// </summary>
        public event EventHandler<EventArgs> StatusChanged;

        /// <summary>
        /// Raises the <see cref="E:StatusChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void OnStatusChanged(EventArgs e)
        {
            if (this.StatusChanged != null)
            {
                this.StatusChanged(this, e);
            }
        }

        #endregion
    }
}