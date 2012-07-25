using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.Security
{
    [Export]
    public class SecurityViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetSecurityGroupsCommand { get; private set; }
        public RelayCommand DeleteSelectedSecurityGroupsCommand { get; private set; }
        public RelayCommand AddSecurityGroupCommand { get; private set; }

        #endregion

        #region Observable Properties

        public MembershipQuery MembershipMode
        {
            get { return this.GetValue(MembershipModeProperty); }
            set { this.SetValue(MembershipModeProperty, value); }
        }

        public static ObservableProperty<MembershipQuery> MembershipModeProperty = new ObservableProperty<MembershipQuery, SecurityViewModel>(o => o.MembershipMode);

        public ICollection<SecurityGroupInfo> SecurityGroups
        {
            get { return this.GetValue(SecurityGroupsProperty); }
            set { this.SetValue(SecurityGroupsProperty, value); }
        }

        public static ObservableProperty<ICollection<SecurityGroupInfo>> SecurityGroupsProperty = new ObservableProperty<ICollection<SecurityGroupInfo>, SecurityViewModel>(o => o.SecurityGroups);

        public ICollection<SecurityGroupInfo> SelectedSecurityGroups
        {
            get { return this.GetValue(SelectedSecurityGroupsProperty); }
            set { this.SetValue(SelectedSecurityGroupsProperty, value); }
        }

        public static ObservableProperty<ICollection<SecurityGroupInfo>> SelectedSecurityGroupsProperty = new ObservableProperty<ICollection<SecurityGroupInfo>, SecurityViewModel>(o => o.SelectedSecurityGroups);

        public string NewSecurityGroupName
        {
            get { return this.GetValue(NewSecurityGroupNameProperty); }
            set { this.SetValue(NewSecurityGroupNameProperty, value); }
        }

        public static ObservableProperty<string> NewSecurityGroupNameProperty = new ObservableProperty<string, SecurityViewModel>(o => o.NewSecurityGroupName);

        public string NewSecurityGroupDescription
        {
            get { return this.GetValue(NewSecurityGroupDescriptionProperty); }
            set { this.SetValue(NewSecurityGroupDescriptionProperty, value); }
        }

        public static ObservableProperty<string> NewSecurityGroupDescriptionProperty = new ObservableProperty<string, SecurityViewModel>(o => o.NewSecurityGroupDescription);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public SecurityViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Security", "Allows you to manage the security for Team Projects.")
        {
            this.GetSecurityGroupsCommand = new RelayCommand(GetSecurityGroups, CanGetSecurityGroups);
            this.DeleteSelectedSecurityGroupsCommand = new RelayCommand(DeleteSelectedSecurityGroups, CanDeleteSelectedSecurityGroups);
            this.AddSecurityGroupCommand = new RelayCommand(AddSecurityGroup, CanAddSecurityGroup);
        }

        #endregion

        #region Commands

        private bool CanGetSecurityGroups(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetSecurityGroups(object argument)
        {
            var teamProjects = this.SelectedTeamProjects.ToList();
            var membershipMode = this.MembershipMode;
            var task = new ApplicationTask("Retrieving security groups", teamProjects.Count);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var securityService = tfs.GetService<IIdentityManagementService>();

                var step = 0;
                var securityGroups = new List<SecurityGroupInfo>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        foreach (var applicationGroup in securityService.ListApplicationGroups(teamProject.Uri.ToString(), ReadIdentityOptions.None).Where(g => g.IsActive).OrderBy(g => g.DisplayName))
                        {
                            var members = new List<string>();
                            if (membershipMode != MembershipQuery.None)
                            {
                                var applicationGroupWithMembers = securityService.ReadIdentity(applicationGroup.Descriptor, membershipMode, ReadIdentityOptions.None);
                                if (applicationGroupWithMembers.Members != null && applicationGroupWithMembers.Members.Any())
                                {
                                    members.AddRange(securityService.ReadIdentities(applicationGroupWithMembers.Members, membershipMode, ReadIdentityOptions.None).Where(m => m != null).Select(m => m.DisplayName));
                                }
                            }
                            object descriptionObject = null;
                            string description = null;
                            if (applicationGroup.TryGetProperty("Description", out descriptionObject))
                            {
                                description = descriptionObject.ToString();
                            }
                            var securityGroup = new SecurityGroupInfo(teamProject, applicationGroup.Descriptor.Identifier, applicationGroup.DisplayName, description, members);
                            securityGroups.Add(securityGroup);
                        }
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProject.Name), exc);
                    }
                }

                e.Result = securityGroups;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving security groups", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.SecurityGroups = (ICollection<SecurityGroupInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.SecurityGroups.Count().ToCountString("security group"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanDeleteSelectedSecurityGroups(object argument)
        {
            return this.SelectedSecurityGroups != null && this.SelectedSecurityGroups.Any();
        }

        private void DeleteSelectedSecurityGroups(object argument)
        {
            var result = MessageBox.Show("This will delete the selected security groups. Are you sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var securityGroupsToDelete = this.SelectedSecurityGroups.ToList();
                var task = new ApplicationTask("Deleting security groups", securityGroupsToDelete.Count);
                PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    var tfs = GetSelectedTfsTeamProjectCollection();
                    var securityService = tfs.GetService<IIdentityManagementService>();

                    var step = 0;
                    var count = 0;
                    foreach (var securityGroup in securityGroupsToDelete)
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Deleting security group \"{0}\" in Team Project \"{1}\"", securityGroup.Name, securityGroup.TeamProject.Name));
                        try
                        {
                            securityService.DeleteApplicationGroup(IdentityHelper.CreateTeamFoundationDescriptor(securityGroup.Sid));
                            count++;
                        }
                        catch (Exception exc)
                        {
                            task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting security group \"{0}\" in Team Project \"{1}\"", securityGroup.Name, securityGroup.TeamProject.Name), exc);
                        }
                    }

                    e.Result = count;
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        Logger.Log("An unexpected exception occurred while deleting security groups", e.Error);
                        task.SetError(e.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        var count = (int)e.Result;
                        task.SetComplete("Deleted " + count.ToCountString("security group"));
                    }

                    // Refresh the list.
                    GetSecurityGroups(null);
                };
                worker.RunWorkerAsync();
            }
        }

        private bool CanAddSecurityGroup(object argument)
        {
            return IsAnyTeamProjectSelected() && !string.IsNullOrEmpty(this.NewSecurityGroupName);
        }

        private void AddSecurityGroup(object argument)
        {
            var result = MessageBox.Show("This will add a new security group to all selected Team Projects. Are you sure you want to continue?", "Confirm Add", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var teamProjects = this.SelectedTeamProjects.ToList();
                var groupName = this.NewSecurityGroupName;
                var groupDescription = this.NewSecurityGroupDescription;
                var task = new ApplicationTask("Adding security groups", teamProjects.Count);
                PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    var tfs = GetSelectedTfsTeamProjectCollection();
                    var securityService = tfs.GetService<IIdentityManagementService>();

                    var step = 0;
                    var count = 0;
                    foreach (var teamProject in teamProjects)
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Adding security group \"{0}\" to Team Project \"{1}\"", groupName, teamProject.Name));
                        try
                        {
                            securityService.CreateApplicationGroup(teamProject.Uri.ToString(), groupName, groupDescription);
                            count++;
                        }
                        catch (Exception exc)
                        {
                            task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while adding security group \"{0}\" to Team Project \"{1}\"", groupName, teamProject.Name), exc);
                        }
                    }

                    e.Result = count;
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        Logger.Log("An unexpected exception occurred while deleting security groups", e.Error);
                        task.SetError(e.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        var count = (int)e.Result;
                        task.SetComplete("Added " + count.ToCountString("security group"));
                    }
                };
                worker.RunWorkerAsync();
            }
        }

        #endregion
    }
}