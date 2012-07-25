using System;
using System.Globalization;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Shell.Infrastructure
{
    internal static class CodePlexClient
    {
        #region Fields

        private static Regex VersionExpression = new Regex(@"\d{1,5}\.\d{1,5}(\.\d{1,5}(\.\d{1,5})?)?", RegexOptions.IgnoreCase);

        #endregion

        #region IsOnline

        /// <summary>
        /// Gets a value that determines if CodePlex is available.
        /// </summary>
        /// <returns><see langword="true"/> if a network connection is available, <see langword="false"/> otherwise.</returns>
        public static bool IsOnline()
        {
            return Network.IsAvailable();
        }

        #endregion

        #region GetLatestReleasedVersion

        public static ApplicationVersion GetLatestReleasedVersion(string projectName, string editionName, ILogger logger)
        {
            try
            {
                // Load the latest releases RSS feed.
                var releaseRssFeedUrl = string.Format(CultureInfo.InvariantCulture, "http://{0}.codeplex.com/Project/ProjectRss.aspx?ProjectRSSFeed=codeplex%3a%2f%2frelease%2f{0}", projectName);
                using (var reader = XmlReader.Create(releaseRssFeedUrl))
                {
                    var feed = SyndicationFeed.Load(reader);
                    Version highestVersion = null;
                    Uri downloadUrl = null;
                    foreach (var item in feed.Items)
                    {
                        // For each release item in the feed, check if the title contains the edition name (if given) and one or more version numbers.
                        var title = item.Title.Text;
                        if (!string.IsNullOrEmpty(title) && (string.IsNullOrEmpty(editionName) || title.IndexOf(editionName, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            var versionMatches = VersionExpression.Matches(title);
                            foreach (Match versionMatch in versionMatches)
                            {
                                if (versionMatch.Success)
                                {
                                    Version parsedVersion;
                                    if (Version.TryParse(versionMatch.Value, out parsedVersion))
                                    {
                                        // Find the highest version number that actually has at least 3 parts: major.minor.build.
                                        // This makes sure to exclude versions that are planned but not yet released (so the build number isn't known yet).
                                        if (parsedVersion.Build >= 0)
                                        {
                                            if (highestVersion == null || parsedVersion > highestVersion)
                                            {
                                                highestVersion = parsedVersion;
                                                if (item.Links.Count > 0)
                                                {
                                                    // The link points to the download web page.
                                                    downloadUrl = item.Links[0].Uri;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (highestVersion != null)
                    {
                        return new ApplicationVersion(highestVersion, downloadUrl);
                    }
                }
            }
            catch (Exception exc)
            {
                if (logger != null)
                {
                    logger.Log("Could not retrieve the latest released version from CodePlex.", exc);
                }
            }
            return null;
        }

        #endregion
    }
}