using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows;
using TeamProjectManager.Common;

[assembly: AssemblyVersion("1.0.*")] // This can be updated anytime as long as .NET user settings are not used (otherwise the settings directory depends on this version).
[assembly: AssemblyInformationalVersion("1.0.0.0")] // Keep this constant as long as possible to avoid the user's settings getting lost (it is used for the LocalUserAppDataPath where the configuration file is stored).
[assembly: Guid("70d06212-76d7-492c-8957-9f0b59d1bb2c")]

[assembly: AssemblyProduct(Constants.ApplicationName)]
[assembly: AssemblyTitle(Constants.ApplicationName)]
[assembly: AssemblyDescription(Constants.ApplicationDescription)]
[assembly: AssemblyCompany(Constants.ApplicationCompany)]
[assembly: AssemblyCopyright(Constants.ApplicationCopyright)]

[assembly: AssemblyConfiguration(Constants.AssemblyConfiguration)]
[assembly: AssemblyTrademark(Constants.AssemblyTrademark)]
[assembly: AssemblyCulture(Constants.AssemblyCulture)]
[assembly: NeutralResourcesLanguage(Constants.AssemblyNeutralResourcesLanguage)]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
    //(used if a resource is not found in the page, 
    // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
    //(used if a resource is not found in the page, 
    // app, or any theme specific resource dictionaries)
)]