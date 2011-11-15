using System;
using System.ComponentModel;

namespace TeamProjectManager.Common
{
    public sealed class TeamProjectCollectionInfo : INotifyPropertyChanged
    {
        private TeamFoundationServerInfo teamFoundationServerInfo;
        public TeamFoundationServerInfo TeamFoundationServerInfo
        {
            get
            {
                return this.teamFoundationServerInfo;
            }
            set
            {
                if (this.teamFoundationServerInfo != null)
                {
                    throw new InvalidOperationException("The TeamFoundationServerInfo property can only be set once.");
                }
                this.teamFoundationServerInfo = value;
                OnPropertyChanged("TeamFoundationServerInfo");
            }
        }

        public string Name { get; private set; }
        public Uri Uri { get; private set; }

        public TeamProjectCollectionInfo(string name, Uri uri)
        {
            this.Name = name;
            this.Uri = uri;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}