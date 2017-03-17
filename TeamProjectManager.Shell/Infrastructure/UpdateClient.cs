using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Shell.Infrastructure
{
    internal static class UpdateClient
    {
        #region Fields

        private static Regex VersionExpression = new Regex(@"\d{1,5}\.\d{1,5}(\.\d{1,5}(\.\d{1,5})?)?", RegexOptions.IgnoreCase);

        #endregion

        #region IsOnline

        /// <summary>
        /// Gets a value that determines if an update check is possible.
        /// </summary>
        /// <returns><see langword="true"/> if a network connection is available, <see langword="false"/> otherwise.</returns>
        public static bool IsOnline()
        {
            return Network.IsAvailable();
        }

        #endregion

        #region GetLatestReleasedVersion

        public static ApplicationVersion GetLatestReleasedVersion(ILogger logger)
        {
            return GetLatestReleasedVersionFromGitHub("jelledruyts", "TfsTeamProjectManager", logger);
        }

        private static ApplicationVersion GetLatestReleasedVersionFromGitHub(string userName, string projectName, ILogger logger)
        {
            try
            {
                // Get the latest release JSON document.
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Constants.ApplicationName);
                    var latestReleaseJson = client.GetStringAsync(string.Format("https://api.github.com/repos/{0}/{1}/releases/latest", userName, projectName)).Result;
                    var latestRelease = JsonConvert.DeserializeObject<GitHubReleaseInfo>(latestReleaseJson);

                    var versionMatch = VersionExpression.Match(latestRelease.tag_name);
                    if (versionMatch.Success)
                    {
                        Version parsedVersion;
                        if (Version.TryParse(versionMatch.Value, out parsedVersion))
                        {
                            return new ApplicationVersion(parsedVersion, new Uri(latestRelease.html_url));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                if (logger != null)
                {
                    logger.Log("Could not retrieve the latest released version.", exc);
                }
            }
            return null;
        }

        #endregion

        #region Helper Classes

        private class GitHubReleaseInfo
        {
            public string body { get; set; }
            public string name { get; set; }
            public string tag_name { get; set; }
            public string html_url { get; set; }
            public string published_at { get; set; }
        }

        #endregion
    }
}