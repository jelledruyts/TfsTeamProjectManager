using System.Windows.Shell;

namespace TeamProjectManager.Shell.Infrastructure
{
    internal interface IStatusService
    {
        void SetMainWindowTitleStatus(string status);
        void ClearMainWindowTitleStatus();
        void SetMainWindowProgress(TaskbarItemProgressState state, double progressValue);
        void SetMainWindowProgress(TaskbarItemProgressState state);
    }
}