using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TeamProjectManager.Modules.Security
{
    public class PermissionChange : INotifyPropertyChanged
    {
        #region Properties

        public Permission Permission { get; private set; }
        public PermissionChangeAction action { get; set; }
        public PermissionChangeAction Action { get { return this.action; } set { if (this.action != value) { this.action = value; OnPropertyChanged(); } } }

        #endregion

        #region Constructors

        public PermissionChange(Permission permission)
        {
            if (permission == null)
            {
                throw new ArgumentNullException("permission");
            }
            this.Permission = permission;
            this.Action = PermissionChangeAction.None;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}