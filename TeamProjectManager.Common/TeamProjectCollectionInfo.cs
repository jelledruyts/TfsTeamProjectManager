using System;
using System.Collections.Generic;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Common
{
    public sealed class TeamProjectCollectionInfo : ObservableObject
    {
        #region Properties

        public string Name { get; private set; }
        public Uri Uri { get; private set; }

        #endregion

        #region Observable Properties

        public TeamFoundationServerInfo TeamFoundationServer
        {
            get { return this.GetValue(TeamFoundationServerProperty); }
            internal set { this.SetValue(TeamFoundationServerProperty, value); }
        }

        public static ObservableProperty<TeamFoundationServerInfo> TeamFoundationServerProperty = new ObservableProperty<TeamFoundationServerInfo, TeamProjectCollectionInfo>(o => o.TeamFoundationServer);

        public ICollection<TeamProjectInfo> TeamProjects
        {
            get { return this.GetValue(TeamProjectsProperty); }
            set { this.SetValue(TeamProjectsProperty, value); }
        }

        public static ObservableProperty<ICollection<TeamProjectInfo>> TeamProjectsProperty = new ObservableProperty<ICollection<TeamProjectInfo>, TeamProjectCollectionInfo>(o => o.TeamProjects);

        #endregion

        #region Constructors

        public TeamProjectCollectionInfo(string name, Uri uri)
        {
            this.Name = name;
            this.Uri = uri;
        }

        #endregion
    }
}