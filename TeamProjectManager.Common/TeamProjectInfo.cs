using System;

namespace TeamProjectManager.Common
{
    /// <summary>
    /// Provides information about a Team Project.
    /// </summary>
    public sealed class TeamProjectInfo
    {
        /// <summary>
        /// Gets the Team Project Collection that this Team Project is part of.
        /// </summary>
        public TeamProjectCollectionInfo TeamProjectCollection { get; private set; }

        /// <summary>
        /// Gets the name of this Team Project.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the URI of this Team Project.
        /// </summary>
        public Uri Uri { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamProjectInfo"/> class.
        /// </summary>
        /// <param name="teamProjectCollection">The Team Project Collection that this Team Project is part of.</param>
        /// <param name="name">The name of this Team Project.</param>
        /// <param name="uri">The URI of this Team Project.</param>
        public TeamProjectInfo(TeamProjectCollectionInfo teamProjectCollection, string name, Uri uri)
        {
            this.TeamProjectCollection = teamProjectCollection;
            this.Name = name;
            this.Uri = uri;
        }
    }
}