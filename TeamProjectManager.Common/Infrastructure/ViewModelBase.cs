using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
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

        /// <summary>
        /// The event aggregator that can be used to publish and subscribe to loosely coupled events.
        /// </summary>
        public IEventAggregator EventAggregator { get; private set; }

        /// <summary>
        /// The logger that can be used to publish log messages to.
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// The information about the current view model.
        /// </summary>
        public ViewModelInfo Info { get; private set; }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Gets the currently selected Team Project Collection.
        /// </summary>
        public TeamProjectCollectionInfo SelectedTeamProjectCollection
        {
            get { return this.GetValue(SelectedTeamProjectCollectionProperty); }
            set { this.SetValue(SelectedTeamProjectCollectionProperty, value); }
        }

        /// <summary>
        /// The definition of the <see cref="SelectedTeamProjectCollection"/> observable property.
        /// </summary>
        public static ObservableProperty<TeamProjectCollectionInfo> SelectedTeamProjectCollectionProperty = new ObservableProperty<TeamProjectCollectionInfo, ViewModelBase>(o => o.SelectedTeamProjectCollection, OnSelectedTeamProjectCollectionChanged);


        /// <summary>
        /// Gets the currently selected external editor.
        /// </summary>
        public string SelectedExternalEditor
        {
            get { return this.GetValue(SelectedExternalEditorProperty); }
            set { this.SetValue(SelectedExternalEditorProperty, value); }
        }

        /// <summary>
        /// The definition of the <see cref="SelectedExternalEditor"/> observable property.
        /// </summary>
        public static ObservableProperty<string> SelectedExternalEditorProperty = new ObservableProperty<string, ViewModelBase>(o => o.SelectedExternalEditor, OnSelectedExternalEditorChanged);

        /// <summary>
        /// Gets the currently selected Team Projects.
        /// </summary>
        public ICollection<TeamProjectInfo> SelectedTeamProjects
        {
            get { return this.GetValue(SelectedTeamProjectsProperty); }
            set { this.SetValue(SelectedTeamProjectsProperty, value); }
        }

        /// <summary>
        /// The definition of the <see cref="SelectedTeamProjects"/> observable property.
        /// </summary>
        public static ObservableProperty<ICollection<TeamProjectInfo>> SelectedTeamProjectsProperty = new ObservableProperty<ICollection<TeamProjectInfo>, ViewModelBase>(o => o.SelectedTeamProjects, OnSelectedTeamProjectsChanged);

        /// <summary>
        /// Gets the visibility of UI elements that should be shown in case the currently selected Team Foundation Server is unsupported.
        /// </summary>
        public Visibility TfsUnsupportedVisibility
        {
            get { return this.GetValue(TfsUnsupportedVisibilityProperty); }
            private set { this.SetValue(TfsUnsupportedVisibilityProperty, value); }
        }

        /// <summary>
        /// The definition of the <see cref="TfsUnsupportedVisibility"/> observable property.
        /// </summary>
        public static ObservableProperty<Visibility> TfsUnsupportedVisibilityProperty = new ObservableProperty<Visibility, ViewModelBase>(o => o.TfsUnsupportedVisibility, Visibility.Hidden);

        /// <summary>
        /// Gets the visibility of UI elements that should be shown in case the currently selected Team Foundation Server is supported.
        /// </summary>
        public Visibility TfsSupportedVisibility
        {
            get { return this.GetValue(TfsSupportedVisibilityProperty); }
            private set { this.SetValue(TfsSupportedVisibilityProperty, value); }
        }

        /// <summary>
        /// The definition of the <see cref="TfsSupportedVisibility"/> observable property.
        /// </summary>
        public static ObservableProperty<Visibility> TfsSupportedVisibilityProperty = new ObservableProperty<Visibility, ViewModelBase>(o => o.TfsSupportedVisibility, Visibility.Visible);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
        /// </summary>
        /// <param name="eventAggregator">The event aggregator that can be used to publish and subscribe to loosely coupled events.</param>
        /// <param name="logger">The logger that can be used to publish log messages to.</param>
        /// <param name="title">The title of the view to be shown in the UI.</param>
        protected ViewModelBase(IEventAggregator eventAggregator, ILogger logger, string title)
            : this(eventAggregator, logger, title, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
        /// </summary>
        /// <param name="eventAggregator">The event aggregator that can be used to publish and subscribe to loosely coupled events.</param>
        /// <param name="logger">The logger that can be used to publish log messages to.</param>
        /// <param name="title">The title of the view to be shown in the UI.</param>
        /// <param name="description">The description associated with the view.</param>
        protected ViewModelBase(IEventAggregator eventAggregator, ILogger logger, string title, string description)
        {
            this.EventAggregator = eventAggregator;
            this.Logger = logger;
            this.Info = new ViewModelInfo(title, description);
            this.EventAggregator.GetEvent<TeamProjectCollectionSelectionChangedEvent>().Subscribe(e => this.SelectedTeamProjectCollection = e.SelectedTeamProjectCollection);
            this.EventAggregator.GetEvent<TeamProjectSelectionChangedEvent>().Subscribe(e => this.SelectedTeamProjects = e.SelectedTeamProjects);
            this.EventAggregator.GetEvent<ExternalEditorSelectionChangedEvent>().Subscribe(e => this.SelectedExternalEditor = e.SelectedExternalEditor);
        }

        #endregion

        #region Event Handlers

        private static void OnSelectedTeamProjectCollectionChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<TeamProjectCollectionInfo> e)
        {
            var viewModel = (ViewModelBase)sender;
            viewModel.OnSelectedTeamProjectCollectionChangedInternal();
        }

        private void OnSelectedTeamProjectCollectionChangedInternal()
        {
            var supported = this.SelectedTeamProjectCollection == null || (this.SelectedTeamProjectCollection.TeamFoundationServer != null && IsTfsSupported(this.SelectedTeamProjectCollection.TeamFoundationServer));
            this.TfsSupportedVisibility = supported ? Visibility.Visible : Visibility.Hidden;
            this.TfsUnsupportedVisibility = supported ? Visibility.Hidden : Visibility.Visible;

            OnSelectedTeamProjectCollectionChanged();
        }

        private static void OnSelectedExternalEditorChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<string> e)
        {
            var viewModel = (ViewModelBase)sender;
            viewModel.OnSelectedExternalEditorChanged();
        }

        private static void OnSelectedTeamProjectsChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<ICollection<TeamProjectInfo>> e)
        {
            var viewModel = (ViewModelBase)sender;
            viewModel.OnSelectedTeamProjectsChanged();
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

        /// <summary>
        /// Called when the selected Team Project Collection changed.
        /// </summary>
        /// <remarks>
        /// Override this to handle any logic that must run when the Team Project Collection changed.
        /// </remarks>
        protected virtual void OnSelectedTeamProjectCollectionChanged()
        {
        }

        /// <summary>
        /// Called when the selected External Editor changed.
        /// </summary>
        /// <remarks>
        /// Override this to handle any logic that must run when the External Editor changed.
        /// </remarks>
        protected virtual void OnSelectedExternalEditorChanged()
        {
        }

        /// <summary>
        /// Called when the selected Team Projects changed.
        /// </summary>
        /// <remarks>
        /// Override this to handle any logic that must run when the Team Projects changed.
        /// </remarks>
        protected virtual void OnSelectedTeamProjectsChanged()
        {
        }

        /// <summary>
        /// Determines if the provided Team Foundation Server is supported by this view model.
        /// </summary>
        /// <param name="server">The Team Foundation Server for which to determine if it is supported.</param>
        /// <returns><see langword="true"/> if the Team Foundation Server is supported; <see langword="false"/> otherwise.</returns>
        protected virtual bool IsTfsSupported(TeamFoundationServerInfo server)
        {
            return true;
        }

        /// <summary>
        /// Returns the number of currently selected Team Projects.
        /// </summary>
        /// <returns></returns>
        protected int GetNumberOfSelectedTeamProjects()
        {
            return (this.SelectedTeamProjectCollection != null && this.SelectedTeamProjects != null ? this.SelectedTeamProjects.Count : 0);
        }

        /// <summary>
        /// Determines if any Team Project has been selected.
        /// </summary>
        /// <returns><see langword="true"/> if any Team Project has been selected; <see langword="false"/> otherwise.</returns>
        protected bool IsAnyTeamProjectSelected()
        {
            return GetNumberOfSelectedTeamProjects() > 0;
        }

        /// <summary>
        /// Gets the Team Project Collection instance for the currently selected Team Project Collection.
        /// </summary>
        /// <returns>The Team Project Collection instance for the currently selected Team Project Collection.</returns>
        protected TfsTeamProjectCollection GetSelectedTfsTeamProjectCollection()
        {
            if (this.SelectedTeamProjectCollection == null)
            {
                return null;
            }
            else
            {
                return GetTfsTeamProjectCollection(this.SelectedTeamProjectCollection.Uri);
            }
        }

        /// <summary>
        /// Gets the Team Project Collection instance for the specified URI.
        /// </summary>
        /// <param name="uri">The URI of the Team Project Collection.</param>
        /// <returns>The Team Project Collection instance for the specified URI.</returns>
        protected TfsTeamProjectCollection GetTfsTeamProjectCollection(Uri uri)
        {
            return TfsTeamProjectCollectionCache.GetTfsTeamProjectCollection(uri);
        }

        #endregion
    }
}