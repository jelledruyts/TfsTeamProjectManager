using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Events;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// A base class for view models.
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        #region Properties

        private Dispatcher dispatcher;

        /// <summary>
        /// The dispatcher to use when executing actions on the UI thread.
        /// </summary>
        private Dispatcher Dispatcher
        {
            get
            {
                if (dispatcher == null)
                {
                    dispatcher = Application.Current.Dispatcher;
                }
                return dispatcher;
            }
        }

        public IEventAggregator EventAggregator { get; private set; }

        public ILogger Logger { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
        /// </summary>
        protected ViewModelBase(IEventAggregator eventAggregator, ILogger logger)
        {
            this.EventAggregator = eventAggregator;
            this.Logger = logger;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Publishes a status event.
        /// </summary>
        /// <param name="status">The status event arguments.</param>
        protected void PublishStatus(StatusEventArgs status)
        {
            if (this.EventAggregator != null)
            {
                this.EventAggregator.GetEvent<StatusEvent>().Publish(status);
            }
        }

        /// <summary>
        /// Synchronously executes an action on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        protected void ExecuteUIAction(Action action)
        {
            if (this.Dispatcher == null)
            {
                Logger.Log("The UI action cannot be executed because the dispatcher was not initialized.", TraceEventType.Warning);
            }
            else
            {
                // Do not go through the dispatcher unless it's really necessary.
                if (this.Dispatcher.Thread != Thread.CurrentThread)
                {
                    this.Dispatcher.Invoke(new ThreadStart(() => { action(); }));
                }
                else
                {
                    action();
                }
            }
        }

        /// <summary>
        /// Asynchronously executes an action on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        protected void ExecuteUIActionAsync(Action action)
        {
            if (this.Dispatcher == null)
            {
                Logger.Log("The UI action cannot be executed asynchronously because the dispatcher was not initialized.", TraceEventType.Warning);
            }
            else
            {
                // Do not go through the dispatcher unless it's really necessary.
                if (this.Dispatcher.Thread != Thread.CurrentThread)
                {
                    this.Dispatcher.BeginInvoke(new ThreadStart(() => { action(); }));
                }
                else
                {
                    action();
                }
            }
        }

        #endregion
    }
}