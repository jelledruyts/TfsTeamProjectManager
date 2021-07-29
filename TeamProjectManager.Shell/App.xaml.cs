using System;
using System.Net;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Shell;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Shell.Infrastructure;

namespace TeamProjectManager.Shell
{
    public partial class App : Application
    {
        internal static readonly Version ApplicationVersion = Assembly.GetEntryAssembly().GetName().Version;
        internal static string LogFilePath { get; private set; }
        private Logger logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure that the application closes when the main window is closed (even if dialogs are still open or hidden).
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Create the logger.
            this.logger = new Logger();
            this.logger.Log(string.Format(CultureInfo.CurrentCulture, "Application started (v{0})", ApplicationVersion.ToString()), TraceEventType.Information);
            App.LogFilePath = this.logger.LogFilePath;

            // Ensure that the current culture is used for all controls (see http://www.west-wind.com/Weblog/posts/796725.aspx).
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            // Use TLS version(s) specified in config
            ServicePointManager.SecurityProtocol = GetSecurityProtocolTypeFromConfig() ?? ServicePointManager.SecurityProtocol;

            var applicationDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // Unblock any files that were blocked by Windows because they were downloaded.
            try
            {
                this.logger.Log("Program - Unblocking files in " + applicationDirectory, TraceEventType.Verbose);
                foreach (var fileName in Directory.GetFiles(applicationDirectory))
                {
                    FileSystem.UnblockFile(fileName);
                }
            }
            catch (Exception exc)
            {
                this.logger.Log("An exception occurred while attempting to unblock any files blocked by Windows", exc, TraceEventType.Warning);
            }

            // Set up the Windows jump list.
            var jumpList = new JumpList();
            jumpList.JumpItems.Add(new JumpTask { Title = "Open Log Directory", ApplicationPath = Path.GetDirectoryName(App.LogFilePath), Description = "Open the log directory" });
            jumpList.JumpItems.Add(new JumpTask { Title = "Go To Homepage", ApplicationPath = Constants.ApplicationUrl, Description = "Go to the homepage for " + Constants.ApplicationName });
            jumpList.Apply();
            JumpList.SetJumpList(this, jumpList);

            // Launch the PRISM bootstrapper.
            try
            {
                new Bootstrapper(this.logger).Run();
            }
            catch (Exception exc)
            {
                var rtle = exc as ReflectionTypeLoadException;
                this.logger.Log("An exception occurred while starting the application", exc);
                var message = "We're sorry, the application couldn't be started :-(";
                if (rtle != null)
                {
                    var loaderExceptionLogs = new StringBuilder();
                    loaderExceptionLogs.Append("Loader Exceptions:").AppendLine();
                    foreach (var loaderException in rtle.LoaderExceptions)
                    {
                        loaderExceptionLogs.Append(loaderException.ToString()).AppendLine();
                    }
                    this.logger.Log(loaderExceptionLogs.ToString(), TraceEventType.Error);
                }
                message += Environment.NewLine + Environment.NewLine + "If you keep experiencing this issue, please report it and include the contents of the log file (\"{0}\").".FormatCurrent(this.logger.LogFilePath);
                MessageBox.Show(message, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private SecurityProtocolType? GetSecurityProtocolTypeFromConfig()
        {
            string versions = ConfigurationManager.AppSettings["TlsVersions"];
            if (string.IsNullOrWhiteSpace(versions)) return null;

            //OR all the flags together: https://stackoverflow.com/a/20737127
            return versions.Split(null).ToList().Select(v =>
            {
                Enum.TryParse<SecurityProtocolType>(v, out var e);
                return e;
            }).Aggregate((f, e) => (f |= e));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //TfsTeamProjectCollectionCache.ClearCache();
            this.logger.Log("Application exited", TraceEventType.Information);
            base.OnExit(e);
        }
    }
}