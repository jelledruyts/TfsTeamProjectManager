using System;
using System.Collections.Generic;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Common
{
    /// <summary>
    /// Provides information about a Team Project Collection.
    /// </summary>
    public sealed class TeamProjectCollectionInfo : ObservableObject
    {
        #region Properties

        /// <summary>
        /// Gets the name of the Team Project Collection.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the URI of the Team Project Collection.
        /// </summary>
        public Uri Uri { get; private set; }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Gets the Team Foundation Server that this Team Project Collection is part of.
        /// </summary>
        public TeamFoundationServerInfo TeamFoundationServer
        {
            get { return this.GetValue(TeamFoundationServerProperty); }
            internal set { this.SetValue(TeamFoundationServerProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="TeamFoundationServer"/> observable property.
        /// </summary>
        public static ObservableProperty<TeamFoundationServerInfo> TeamFoundationServerProperty = new ObservableProperty<TeamFoundationServerInfo, TeamProjectCollectionInfo>(o => o.TeamFoundationServer);

        /// <summary>
        /// Gets the Team Projects that are part of this Team Project Collection.
        /// </summary>
        public ICollection<TeamProjectInfo> TeamProjects
        {
            get { return this.GetValue(TeamProjectsProperty); }
            internal set { this.SetValue(TeamProjectsProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="TeamProjects"/> observable property.
        /// </summary>
        public static ObservableProperty<ICollection<TeamProjectInfo>> TeamProjectsProperty = new ObservableProperty<ICollection<TeamProjectInfo>, TeamProjectCollectionInfo>(o => o.TeamProjects);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamProjectCollectionInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the Team Project Collection.</param>
        /// <param name="uri">The URI of the Team Project Collection.</param>
        public TeamProjectCollectionInfo(string name, Uri uri)
        {
            this.Name = name;
            this.Uri = uri;
        }

        #endregion
    }
}