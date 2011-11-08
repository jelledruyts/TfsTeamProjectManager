using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Shell;
using TeamProjectManager.Common;
using TeamProjectManager.Shell.Infrastructure;

namespace TeamProjectManager.Shell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static readonly Version ApplicationVersion = Assembly.GetEntryAssembly().GetName().Version;
        private Logger logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.logger = new Logger();
            this.logger.Log(string.Format(CultureInfo.CurrentCulture, "Application started (v{0})", ApplicationVersion.ToString()), TraceEventType.Information);

            var jumpList = new JumpList();
            jumpList.JumpItems.Add(new JumpTask { Title = "Open Log File", ApplicationPath = "notepad.exe", Arguments = this.logger.LogFilePath, Description = "Open the log file" });
            jumpList.JumpItems.Add(new JumpTask { Title = Constants.ApplicationName + " Homepage", ApplicationPath = Constants.ApplicationUrl, Description = "Go to the homepage for " + Constants.ApplicationName });
            jumpList.Apply();
            JumpList.SetJumpList(this, jumpList);

            new Bootstrapper(this.logger).Run();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            this.logger.Log("Application exited", TraceEventType.Information);
            base.OnExit(e);
        }
    }
}