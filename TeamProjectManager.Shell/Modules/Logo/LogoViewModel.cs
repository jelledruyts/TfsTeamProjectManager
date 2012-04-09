using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;
using TeamProjectManager.Shell.Infrastructure;

namespace TeamProjectManager.Shell.Modules.Logo
{
    [Export]
    public class LogoViewModel : ViewModelBase
    {
        #region Properties

        public string HeaderTitle { get; private set; }
        public string HeaderSubtitle { get; private set; }
        public RelayCommand OpenLogFileCommand { get; private set; }
        public RelayCommand OpenHomepageCommand { get; private set; }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string ApplicationVersion
        {
            get { return this.GetValue(ApplicationVersionProperty); }
            set { this.SetValue(ApplicationVersionProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="ApplicationVersion"/> observable property.
        /// </summary>
        public static ObservableProperty<string> ApplicationVersionProperty = new ObservableProperty<string, LogoViewModel>(o => o.ApplicationVersion);

        /// <summary>
        /// Gets or sets the visibility of the new version message.
        /// </summary>
        public Visibility NewVersionVisibility
        {
            get { return this.GetValue(NewVersionVisibilityProperty); }
            set { this.SetValue(NewVersionVisibilityProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="NewVersionVisibility"/> observable property.
        /// </summary>
        public static ObservableProperty<Visibility> NewVersionVisibilityProperty = new ObservableProperty<Visibility, LogoViewModel>(o => o.NewVersionVisibility, Visibility.Hidden);

        /// <summary>
        /// Gets or sets the message to display if there is a new version.
        /// </summary>
        public string NewVersionMessage
        {
            get { return this.GetValue(NewVersionMessageProperty); }
            set { this.SetValue(NewVersionMessageProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="NewVersionMessage"/> observable property.
        /// </summary>
        public static ObservableProperty<string> NewVersionMessageProperty = new ObservableProperty<string, LogoViewModel>(o => o.NewVersionMessage);

        /// <summary>
        /// Gets or sets the URL where a new version can be downloaded.
        /// </summary>
        public string NewVersionUrl
        {
            get { return this.GetValue(NewVersionUrlProperty); }
            set { this.SetValue(NewVersionUrlProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="NewVersionUrl"/> observable property.
        /// </summary>
        public static ObservableProperty<string> NewVersionUrlProperty = new ObservableProperty<string, LogoViewModel>(o => o.NewVersionUrl);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public LogoViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Logo")
        {
            this.HeaderTitle = InternalConstants.DefaultWindowTitle;
            this.HeaderSubtitle = string.Format(CultureInfo.CurrentCulture, "v{0} - {1}", App.ApplicationVersion.ToString(3), InternalConstants.EditionName);
            this.OpenLogFileCommand = new RelayCommand(OpenLogFile, CanOpenLogFile);
            this.OpenHomepageCommand = new RelayCommand(OpenHomepage, CanOpenHomepage);
            CheckForUpdates();
        }

        #endregion

        #region Commands

        private bool CanOpenLogFile(object argument)
        {
            return File.Exists(App.LogFilePath);
        }

        private void OpenLogFile(object argument)
        {
            Process.Start(App.LogFilePath);
        }

        private bool CanOpenHomepage(object argument)
        {
            return Network.IsAvailable();
        }

        private void OpenHomepage(object argument)
        {
            Process.Start(Constants.ApplicationUrl);
        }

        #endregion

        #region Helper Methods

        private void CheckForUpdates()
        {
            if (CodePlexClient.IsOnline())
            {
                // Check for the latest released version on a background thread.
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    Uri downloadUrl;
                    var latestVersion = CodePlexClient.GetLatestReleasedVersion(InternalConstants.CodePlexProjectName, this.Logger, out downloadUrl);
                    if (latestVersion != null && latestVersion > App.ApplicationVersion)
                    {
                        // If the latest released version is newer than the current, return the version and URL.
                        e.Result = new Tuple<Version, Uri>(latestVersion, downloadUrl);
                    }
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        this.Logger.Log("An unexpected exception occurred while checking for updates", e.Error);
                    }
                    else
                    {
                        if (e.Result != null)
                        {
                            var result = (Tuple<Version, Uri>)e.Result;
                            var version = result.Item1;
                            var downloadUrl = result.Item2;
                            this.NewVersionMessage = string.Format(CultureInfo.CurrentCulture, "A new version is available: v{0}!", version.ToString());
                            this.NewVersionUrl = downloadUrl.ToString();
                            this.NewVersionVisibility = Visibility.Visible;
                        }
                    }
                };
                worker.RunWorkerAsync();
            }
        }

        #endregion
    }
}