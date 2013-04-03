using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Build.Common;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TeamProjectManager.Common.Infrastructure;
using TfsAccessControlEntry = Microsoft.TeamFoundation.Server.AccessControlEntry;
using VCPermissionChange = Microsoft.TeamFoundation.VersionControl.Client.PermissionChange;

namespace TeamProjectManager.Modules.Security
{
    public static class SecurityManager
    {
        #region Static Properties & Constructor

        public static IList<Permission> Permissions { get; private set; }

        static SecurityManager()
        {
            Permissions = new List<Permission>()
            {
                // Team Project
                new Permission(PermissionScope.TeamProject, "Create test runs", PermissionActionIdConstants.PublishTestResults, AuthorizationProjectPermissions.PublishTestResults),
                new Permission(PermissionScope.TeamProject, "Delete team project", PermissionActionIdConstants.Delete, AuthorizationProjectPermissions.Delete),
                new Permission(PermissionScope.TeamProject, "Delete test runs", PermissionActionIdConstants.DeleteTestResults, AuthorizationProjectPermissions.DeleteTestResults),
                new Permission(PermissionScope.TeamProject, "Edit project-level information", PermissionActionIdConstants.GenericWrite, AuthorizationProjectPermissions.GenericWrite),
                new Permission(PermissionScope.TeamProject, "Manage test configurations", PermissionActionIdConstants.ManageTestConfigurations, AuthorizationProjectPermissions.ManageTestConfigurations),
                new Permission(PermissionScope.TeamProject, "Manage test environments", PermissionActionIdConstants.ManageTestEnvironments, AuthorizationProjectPermissions.ManageTestEnvironments),
                new Permission(PermissionScope.TeamProject, "View project-level information", PermissionActionIdConstants.GenericRead, AuthorizationProjectPermissions.GenericRead),
                new Permission(PermissionScope.TeamProject, "View test runs", PermissionActionIdConstants.ViewTestResults, AuthorizationProjectPermissions.ViewTestResults),

                // Team Build
                new Permission(PermissionScope.TeamBuild, "Administer build permissions", PermissionStringConstants.AdministerBuildPermissions, BuildPermissions.AdministerBuildPermissions),
                new Permission(PermissionScope.TeamBuild, "Delete build definition", PermissionStringConstants.DeleteBuildDefinition, BuildPermissions.DeleteBuildDefinition),
                new Permission(PermissionScope.TeamBuild, "Delete builds", PermissionStringConstants.DeleteBuilds, BuildPermissions.DeleteBuilds),
                new Permission(PermissionScope.TeamBuild, "Destroy builds", PermissionStringConstants.DestroyBuilds, BuildPermissions.DestroyBuilds),
                new Permission(PermissionScope.TeamBuild, "Edit build definition", PermissionStringConstants.EditBuildDefinition, BuildPermissions.EditBuildDefinition),
                new Permission(PermissionScope.TeamBuild, "Edit build quality", PermissionStringConstants.EditBuildQuality, BuildPermissions.EditBuildQuality),
                new Permission(PermissionScope.TeamBuild, "Manage build qualities", PermissionStringConstants.ManageBuildQualities, BuildPermissions.ManageBuildQualities),
                new Permission(PermissionScope.TeamBuild, "Manage build queue", PermissionStringConstants.ManageBuildQueue, BuildPermissions.ManageBuildQueue),
                new Permission(PermissionScope.TeamBuild, "Override check-in validation by build", PermissionStringConstants.OverrideBuildCheckInValidation, BuildPermissions.OverrideBuildCheckInValidation),
                new Permission(PermissionScope.TeamBuild, "Queue builds", PermissionStringConstants.QueueBuilds, BuildPermissions.QueueBuilds),
                new Permission(PermissionScope.TeamBuild, "Retain indefinitely", PermissionStringConstants.RetainIndefinitely, BuildPermissions.RetainIndefinitely),
                new Permission(PermissionScope.TeamBuild, "Stop builds", PermissionStringConstants.StopBuilds, BuildPermissions.StopBuilds),
                new Permission(PermissionScope.TeamBuild, "Update build information", PermissionStringConstants.UpdateBuildInformation, BuildPermissions.UpdateBuildInformation),
                new Permission(PermissionScope.TeamBuild, "View build definition", PermissionStringConstants.ViewBuildDefinition, BuildPermissions.ViewBuildDefinition),
                new Permission(PermissionScope.TeamBuild, "View builds", PermissionStringConstants.ViewBuilds, BuildPermissions.ViewBuilds),

                // Work Item Areas
                new Permission(PermissionScope.WorkItemAreas, "Create child nodes", PermissionActionIdConstants.CreateChildren, AuthorizationCssNodePermissions.CreateChildren),
                new Permission(PermissionScope.WorkItemAreas, "Delete this node", PermissionActionIdConstants.Delete, AuthorizationCssNodePermissions.Delete),
                new Permission(PermissionScope.WorkItemAreas, "Edit this node", PermissionActionIdConstants.GenericWrite, AuthorizationCssNodePermissions.GenericWrite),
                new Permission(PermissionScope.WorkItemAreas, "Edit work items in this node", PermissionActionIdConstants.WorkItemWrite, AuthorizationCssNodePermissions.WorkItemWrite),
                new Permission(PermissionScope.WorkItemAreas, "Manage test plans", PermissionActionIdConstants.ManageTestPlans, AuthorizationCssNodePermissions.ManageTestPlans),
                new Permission(PermissionScope.WorkItemAreas, "View permissions for this node", PermissionActionIdConstants.GenericRead, AuthorizationCssNodePermissions.GenericRead),
                new Permission(PermissionScope.WorkItemAreas, "View work items in this node", PermissionActionIdConstants.WorkItemRead, AuthorizationCssNodePermissions.WorkItemRead),

                // Work Item Iterations
                new Permission(PermissionScope.WorkItemIterations, "Create child nodes", PermissionActionIdConstants.CreateChildren, AuthorizationIterationNodePermissions.CreateChildren),
                new Permission(PermissionScope.WorkItemIterations, "Delete this node", PermissionActionIdConstants.Delete, AuthorizationIterationNodePermissions.Delete),
                new Permission(PermissionScope.WorkItemIterations, "Edit this node", PermissionActionIdConstants.GenericWrite, AuthorizationIterationNodePermissions.GenericWrite),
                new Permission(PermissionScope.WorkItemIterations, "View permissions for this node", PermissionActionIdConstants.GenericRead, AuthorizationIterationNodePermissions.GenericRead),

                // Source Control
                new Permission(PermissionScope.SourceControl, "Administer labels", VCPermissionChange.ItemPermissionLabelOther, (int)VersionedItemPermissions.LabelOther),
                new Permission(PermissionScope.SourceControl, "Check in", VCPermissionChange.ItemPermissionCheckin, (int)VersionedItemPermissions.Checkin),
                new Permission(PermissionScope.SourceControl, "Check in other users' changes", VCPermissionChange.ItemPermissionCheckinOther, (int)VersionedItemPermissions.CheckinOther),
                new Permission(PermissionScope.SourceControl, "Check out", VCPermissionChange.ItemPermissionPendChange, (int)VersionedItemPermissions.PendChange),
                new Permission(PermissionScope.SourceControl, "Label", VCPermissionChange.ItemPermissionLabel, (int)VersionedItemPermissions.Label),
                new Permission(PermissionScope.SourceControl, "Lock", VCPermissionChange.ItemPermissionLock, (int)VersionedItemPermissions.Lock),
                new Permission(PermissionScope.SourceControl, "Manage branch", VCPermissionChange.ItemPermissionManageBranch, (int)VersionedItemPermissions.ManageBranch),
                new Permission(PermissionScope.SourceControl, "Manage permissions", VCPermissionChange.ItemPermissionAdminProjectRights, (int)VersionedItemPermissions.AdminProjectRights),
                new Permission(PermissionScope.SourceControl, "Merge", VCPermissionChange.ItemPermissionMerge, (int)VersionedItemPermissions.Merge),
                new Permission(PermissionScope.SourceControl, "Read", VCPermissionChange.ItemPermissionRead, (int)VersionedItemPermissions.Read),
                new Permission(PermissionScope.SourceControl, "Revise other users' changes", VCPermissionChange.ItemPermissionReviseOther, (int)VersionedItemPermissions.ReviseOther),
                new Permission(PermissionScope.SourceControl, "Undo other users' changes", VCPermissionChange.ItemPermissionUndoOther, (int)VersionedItemPermissions.UndoOther),
                new Permission(PermissionScope.SourceControl, "Unlock other users' changes", VCPermissionChange.ItemPermissionUnlockOther, (int)VersionedItemPermissions.UnlockOther),
            };
        }

        #endregion

        #region Apply

        public static void Apply(ApplicationTask task, TfsTeamProjectCollection tfs, string teamProjectName, SecurityGroupChange securityGroup)
        {
            var ss = tfs.GetService<ISecurityService>();
            var buildSecurityNamespace = ss.GetSecurityNamespace(BuildSecurity.BuildNamespaceId);
            var ims = tfs.GetService<IIdentityManagementService>();
            var css = tfs.GetService<ICommonStructureService>();
            var vcs = tfs.GetService<VersionControlServer>();
            var auth = tfs.GetService<IAuthorizationService>();
            var teamProject = css.GetProjectFromName(teamProjectName);
            var teamProjectUrl = teamProject.Uri;
            var groupIdentityName = string.Format(CultureInfo.InvariantCulture, @"[{0}]\{1}", teamProjectName, securityGroup.Name);

            // Create the group if needed.
            var existingGroup = ims.ListApplicationGroups(teamProjectUrl, ReadIdentityOptions.IncludeReadFromSource).FirstOrDefault(g => string.Equals(g.DisplayName, groupIdentityName, StringComparison.OrdinalIgnoreCase));
            var members = new List<TeamFoundationIdentity>();
            IdentityDescriptor groupDescriptor;
            if (existingGroup == null)
            {
                // There is no existing group with the same name, create one.
                task.Status = "Creating new \"{0}\" security group".FormatCurrent(groupIdentityName);
                groupDescriptor = ims.CreateApplicationGroup(teamProjectUrl, securityGroup.Name, securityGroup.Description);
            }
            else
            {
                // We have an existing group with the same name, update the description.
                task.Status = "Updating existing \"{0}\" security group".FormatCurrent(groupIdentityName);
                groupDescriptor = existingGroup.Descriptor;
                var applicationGroupWithMembers = ims.ReadIdentity(groupDescriptor, MembershipQuery.Direct, ReadIdentityOptions.None);
                if (applicationGroupWithMembers.Members != null && applicationGroupWithMembers.Members.Any())
                {
                    members.AddRange(ims.ReadIdentities(applicationGroupWithMembers.Members, MembershipQuery.Direct, ReadIdentityOptions.None).Where(m => m != null));
                }
                if (!string.IsNullOrEmpty(securityGroup.Description))
                {
                    ims.UpdateApplicationGroup(groupDescriptor, GroupProperty.Description, securityGroup.Description);
                }
            }

            // Set permissions.
            if (securityGroup.PermissionChanges.Any(p => p.Action != PermissionChangeAction.None))
            {
                var projectStructures = css.ListStructures(teamProjectUrl);
                var rootArea = projectStructures[0];
                var rootIteration = projectStructures[1];
                ApplyProjectPermission(auth, groupDescriptor, securityGroup.TeamProjectPermissions, PermissionNamespaces.Project + teamProjectUrl);
                ApplyProjectPermission(auth, groupDescriptor, securityGroup.WorkItemAreasPermissions, rootArea.Uri);
                ApplyProjectPermission(auth, groupDescriptor, securityGroup.WorkItemIterationsPermissions, rootIteration.Uri);
                ApplyTeamBuildPermissions(teamProject, groupDescriptor, buildSecurityNamespace, securityGroup.TeamBuildPermissions);
                ApplySourceControlPermissions(teamProject, groupIdentityName, vcs, securityGroup.SourceControlPermissions);
                task.Status = "Applied security permissions";
            }

            // Set members.
            if (securityGroup.RemoveAllUsers || !string.IsNullOrEmpty(securityGroup.UsersToAdd) || !string.IsNullOrEmpty(securityGroup.UsersToRemove))
            {
                ApplyGroupMemberChanges(task, securityGroup, groupDescriptor, ims, members);
                task.Status = "Applied group member changes";
            }
        }

        #endregion

        #region Helper Methods

        private static void ApplyProjectPermission(IAuthorizationService auth, IdentityDescriptor groupDescriptor, IEnumerable<PermissionChange> permissionChanges, string objectId)
        {
            if (permissionChanges.Where(p => p.Action != PermissionChangeAction.None).Any())
            {
                foreach (var permissionChange in permissionChanges.Where(p => p.Action != PermissionChangeAction.None))
                {
                    if (permissionChange.Action == PermissionChangeAction.Inherit)
                    {
                        // Remove both the allow and deny setting to make sure inheritance is in effect.
                        auth.RemoveAccessControlEntry(objectId, new TfsAccessControlEntry(permissionChange.Permission.PermissionConstant, groupDescriptor.Identifier, true));
                        auth.RemoveAccessControlEntry(objectId, new TfsAccessControlEntry(permissionChange.Permission.PermissionConstant, groupDescriptor.Identifier, false));
                    }
                    else
                    {
                        var deny = permissionChange.Action == PermissionChangeAction.Deny;
                        auth.AddAccessControlEntry(objectId, new TfsAccessControlEntry(permissionChange.Permission.PermissionConstant, groupDescriptor.Identifier, deny));
                    }
                }
            }
        }

        private static void ApplySourceControlPermissions(ProjectInfo teamProject, string groupIdentity, VersionControlServer vcs, IEnumerable<PermissionChange> sourceControlPermissions)
        {
            if (sourceControlPermissions.Where(p => p.Action != PermissionChangeAction.None).Any())
            {
                var allows = sourceControlPermissions.Where(p => p.Action == PermissionChangeAction.Allow).Select(p => p.Permission.PermissionConstant).ToArray();
                var denies = sourceControlPermissions.Where(p => p.Action == PermissionChangeAction.Deny).Select(p => p.Permission.PermissionConstant).ToArray();
                var inherits = sourceControlPermissions.Where(p => p.Action == PermissionChangeAction.Inherit).Select(p => p.Permission.PermissionConstant).ToArray();
                if (allows.Any() || denies.Any() || inherits.Any())
                {
                    var permissionChange = new VCPermissionChange(vcs.GetTeamProject(teamProject.Name).ServerItem, groupIdentity, allows, denies, inherits);
                    var successfulChanges = vcs.SetPermissions(new SecurityChange[] { permissionChange });
                    if (successfulChanges.Length != 1)
                    {
                        throw new ApplicationException("The Source Control permissions were not applied successfully for Team Project \"{0}\"".FormatCurrent(teamProject.Name));
                    }
                }
            }
        }

        private static void ApplyTeamBuildPermissions(ProjectInfo teamProject, IdentityDescriptor groupDescriptor, SecurityNamespace buildSecurityNamespace, IEnumerable<PermissionChange> teamBuildPermissions)
        {
            if (teamBuildPermissions.Where(p => p.Action != PermissionChangeAction.None).Any())
            {
                var allows = teamBuildPermissions.Where(p => p.Action == PermissionChangeAction.Allow).Aggregate(0, (sum, p) => sum += p.Permission.PermissionId);
                var denies = teamBuildPermissions.Where(p => p.Action == PermissionChangeAction.Deny).Aggregate(0, (sum, p) => sum += p.Permission.PermissionId);
                var inherits = teamBuildPermissions.Where(p => p.Action == PermissionChangeAction.Inherit).Aggregate(0, (sum, p) => sum += p.Permission.PermissionId);
                if (allows > 0 || denies > 0 || inherits > 0)
                {
                    // See http://blogs.msdn.com/b/jpricket/archive/2010/07/19/tfs-2010-build-security-api.aspx
                    var teamProjectToken = LinkingUtilities.DecodeUri(teamProject.Uri).ToolSpecificId;
                    if (inherits > 0)
                    {
                        buildSecurityNamespace.RemovePermissions(teamProjectToken, groupDescriptor, inherits);
                    }
                    if (allows > 0 || denies > 0)
                    {
                        buildSecurityNamespace.SetPermissions(teamProjectToken, groupDescriptor, allows, denies, true);
                    }
                }
            }
        }

        private static void ApplyGroupMemberChanges(ApplicationTask task, SecurityGroupChange securityGroup, IdentityDescriptor groupDescriptor, IIdentityManagementService ims, IList<TeamFoundationIdentity> existingMembers)
        {
            var existingMemberAccountNames = existingMembers.Select(m => GetAccountName(m));

            // Remove requested members.
            if (securityGroup.RemoveAllUsers)
            {
                foreach (var member in existingMembers)
                {
                    ims.RemoveMemberFromApplicationGroup(groupDescriptor, member.Descriptor);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(securityGroup.UsersToRemove))
                {
                    foreach (var userToRemove in securityGroup.UsersToRemove.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim()))
                    {
                        if (existingMemberAccountNames.Any(m => string.Equals(m, userToRemove, StringComparison.OrdinalIgnoreCase)))
                        {
                            PerformUserAction(task, ims, userToRemove, identityToRemove => ims.RemoveMemberFromApplicationGroup(groupDescriptor, identityToRemove.Descriptor));
                        }
                    }
                }
            }

            // Add requested members.
            if (!string.IsNullOrEmpty(securityGroup.UsersToAdd))
            {
                foreach (var userToAdd in securityGroup.UsersToAdd.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim()))
                {
                    if (!existingMemberAccountNames.Any(m => string.Equals(m, userToAdd, StringComparison.OrdinalIgnoreCase)))
                    {
                        PerformUserAction(task, ims, userToAdd, identityToAdd => ims.AddMemberToApplicationGroup(groupDescriptor, identityToAdd.Descriptor));
                    }
                }
            }
        }

        private static string GetAccountName(TeamFoundationIdentity identity)
        {
            var scopeName = identity.GetAttribute("ScopeName", null);
            var account = identity.GetAttribute("Account", null);
            if (!string.IsNullOrEmpty(scopeName) && !string.IsNullOrEmpty(account))
            {
                // If the identity has a scope name, it's a TFS group and the domain name is a classification URI (vstfs:///Classification/...).
                // In that case, the account name should be "[ScopeName]\Group" (e.g. "[MyTeamProject]\Contributors").
                return "[{0}]\\{1}".FormatInvariant(scopeName, account);
            }
            else
            {
                return identity.UniqueName;
            }
        }

        private static void PerformUserAction(ApplicationTask task, IIdentityManagementService ims, string userName, Action<TeamFoundationIdentity> action)
        {
            // Look up the user by account name.
            var userIdentity = ims.ReadIdentity(IdentitySearchFactor.AccountName, userName, MembershipQuery.None, ReadIdentityOptions.IncludeReadFromSource);
            if (userIdentity == null)
            {
                task.SetWarning("The requested user was not found: \"{0}\". Please make sure to use the account name, not the display name (which can be ambiguous).".FormatCurrent(userName));
            }
            else
            {
                action(userIdentity);
            }
        }

        #endregion
    }
}