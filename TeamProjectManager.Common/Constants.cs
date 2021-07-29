
using System;
using System.Reflection;

namespace TeamProjectManager.Common
{
    /// <summary>
    /// Provides application-wide constants.
    /// </summary>
    public static class Constants
    {
        //    private static readonly Lazy<(string ProductName, string Copyright, string FileVersion, string InformationVersion)> _assemblyInformation =
        //new Lazy<(string ProductName, string Copyright, string FileVersion, string InformationVersion)>(() =>
        //{
        //            //var assembly = Assembly.GetCallingAssembly();
        //            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        //    var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        //    var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        //    var productAttribute = assembly.GetCustomAttribute<AssemblyProductAttribute>();
        //    var copyrightAttribute = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();

        //    var informationalVersion = assemblyVersionAttribute is null
        //                                           ? assembly.GetName().Version.ToString()
        //                                           : assemblyVersionAttribute.InformationalVersion;
        //    return (productAttribute?.Product, copyrightAttribute?.Copyright, fileVersionAttribute?.Version, informationalVersion);
        //});

        private static Lazy<Assembly> _assembly = new Lazy<Assembly>(() => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
        private static Lazy<string> _productName = new Lazy<string>(() => _assembly.Value.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? string.Empty);
        private static Lazy<string> _copyright = new Lazy<string>(() => _assembly.Value.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty);
        private static Lazy<string> _fileVersion = new Lazy<string>(() => _assembly.Value.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? string.Empty);
        //private static Lazy<string> _assemblyVersion = new Lazy<string>(() => _assembly.Value.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? string.Empty);

        /// <summary>
        /// The name of the application.
        /// </summary>
        public static string ApplicationName => _productName.Value;

        /// <summary>
        /// The URL of the homepage of the application.
        /// </summary>
        public const string ApplicationUrl = "https://github.com/jelledruyts/TfsTeamProjectManager";

        /// <summary>
        /// The description of the application.
        /// </summary>
        //public const string ApplicationDescription = "Automates various tasks across Team Projects in Team Foundation Server.";

        /// <summary>
        /// The company of the application.
        /// </summary>
            //public const string ApplicationCompany = "Jelle Druyts";

        /// <summary>
        /// The copyright statement of the application.
        /// </summary>
        //public const string ApplicationCopyright = "Copyright © " + ApplicationCompany;

        /// <summary>
        /// The configuration of the assemblies that make up the application.
        /// </summary>
        //public const string AssemblyConfiguration = "";

        /// <summary>
        /// The trademark of the assemblies that make up the application.
        /// </summary>
        //public const string AssemblyTrademark = "";

        /// <summary>
        /// The culture of the assemblies that make up the application.
        /// </summary>
        //public const string AssemblyCulture = "";

        /// <summary>
        /// The neutral resources language of the assemblies that make up the application.
        /// </summary>
        //public const string AssemblyNeutralResourcesLanguage = "en-US";
    }
}