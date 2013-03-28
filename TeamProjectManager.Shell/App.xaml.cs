using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
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

            // Create the logger.
            this.logger = new Logger();
            this.logger.Log(string.Format(CultureInfo.CurrentCulture, "Application started (v{0} - {1})", ApplicationVersion.ToString(), InternalConstants.EditionName), TraceEventType.Information);
            App.LogFilePath = this.logger.LogFilePath;

            // Ensure that the current culture is used for all controls (see http://www.west-wind.com/Weblog/posts/796725.aspx).
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            var applicationDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // Unblock any files that were blocked by Windows because they were downloaded.
            try
            {
                logger.Log("Program - Unblocking files in " + applicationDirectory, TraceEventType.Verbose);
                foreach (var fileName in Directory.GetFiles(applicationDirectory))
                {
                    FileSystem.UnblockFile(fileName);
                }
            }
            catch (Exception exc)
            {
                logger.Log("An exception occurred while attempting to unblock any files blocked by Windows.", exc, TraceEventType.Warning);
            }

            // Set up the Windows jump list.
            var jumpList = new JumpList();
            jumpList.JumpItems.Add(new JumpTask { Title = "Open Log File", ApplicationPath = "notepad.exe", Arguments = App.LogFilePath, Description = "Open the log file" });
            jumpList.JumpItems.Add(new JumpTask { Title = "Go To Homepage", ApplicationPath = Constants.ApplicationUrl, Description = "Go to the homepage for " + Constants.ApplicationName });
            jumpList.Apply();
            JumpList.SetJumpList(this, jumpList);

            // Launch the PRISM bootstrapper.
            new Bootstrapper(this.logger).Run();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TfsTeamProjectCollectionCache.ClearCache();
            this.logger.Log("Application exited", TraceEventType.Information);
            base.OnExit(e);
        }
    }
}