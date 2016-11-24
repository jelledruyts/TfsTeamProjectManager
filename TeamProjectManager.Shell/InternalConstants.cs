
namespace TeamProjectManager.Shell
{
    internal static partial class InternalConstants
    {
        public const string AssemblyVersion = "2.0.*"; // This can be updated anytime as long as .NET user settings are not used (otherwise the settings directory depends on this version).
        public const string AssemblyInformationalVersion = "2.0.0.0"; // Keep this constant as long as possible to avoid the user's settings getting lost (it is used for the LocalUserAppDataPath where the configuration file is stored).
        public const string DefaultWindowTitle = "TFS Team Project Manager";

        public const string RegionNameLogo = "Logo";
        public const string RegionNameStatusPanel = "StatusPanel";
        public const string RegionNameTeamProjects = "TeamProjects";

        public const string LoggerTraceSwitchName = "TeamProjectManager";
        public const string LoggerAppSettingNameLogFilePath = "LogFilePath";
        public const string LoggerDefaultLogFileName = "TeamProjectManager.log";
    }
}