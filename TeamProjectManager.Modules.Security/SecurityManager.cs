using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Build.Common;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.Security
{
    public static class SecurityManager
    {
        #region Static Properties & Constructor

        private static IList<PermissionGroupFactory> permissionGroupFactories;

        static SecurityManager()
        {
            permissionGroupFactories = new[]
            {
                new PermissionGroupFactory(PermissionScope.TeamProject, FrameworkSecurity.TeamProjectNamespaceId, "Team Project", new [] { "ADMINISTER_BUILD", "START_BUILD", "EDIT_BUILD_STATUS", "UPDATE_BUILD" }, (tpc, tfsVersion, teamProjectName, teamProjectUri) => PermissionNamespaces.Project + teamProjectUri),
                new PermissionGroupFactory(PermissionScope.Tagging, FrameworkSecurity.TaggingNamespaceId, "Tagging", (tpc, tfsVersion, teamProjectName, teamProjectUri) => "/" + LinkingUtilities.DecodeUri(teamProjectUri).ToolSpecificId),
                new PermissionGroupFactory(PermissionScope.WorkItemAreas, AuthorizationSecurityConstants.CommonStructureNodeSecurityGuid, "Areas", (tpc, tfsVersion, teamProjectName, teamProjectUri) =>
                {
                    var css = tpc.GetService<ICommonStructureService>();
                    var projectStructures = css.ListStructures(teamProjectUri);
                    var rootArea = projectStructures.SingleOrDefault(n => n.StructureType == StructureType.ProjectModelHierarchy);
                    if (rootArea == null)
                    {
                        throw new InvalidOperationException("The root area could not be retrieved");
                    }
                    return rootArea.Uri;
                }),
                new PermissionGroupFactory(PermissionScope.WorkItemIterations, AuthorizationSecurityConstants.IterationNodeSecurityGuid, "Iterations", (tpc, tfsVersion, teamProjectName, teamProjectUri) =>
                {
                    var css = tpc.GetService<ICommonStructureService>();
                    var projectStructures = css.ListStructures(teamProjectUri);
                    var rootIteration = projectStructures.SingleOrDefault(n => n.StructureType == StructureType.ProjectLifecycle);
                    if (rootIteration == null)
                    {
                        throw new InvalidOperationException("The root iteration could not be retrieved");
                    }
                    return rootIteration.Uri;
                }),
                new PermissionGroupFactory(PermissionScope.WorkItemQueryFolders, QueryItemSecurityConstants.NamespaceGuid, "Queries", new [] { "FullControl" }, (tpc, tfsVersion, teamProjectName, teamProjectUri) =>
                {
                    var store = tpc.GetService<WorkItemStore>();
                    var project = store.Projects[teamProjectName];
                    foreach (var item in project.QueryHierarchy)
                    {
                        if (!item.IsPersonal && item is QueryFolder)
                        {
                            var teamProjectGuid = LinkingUtilities.DecodeUri(teamProjectUri).ToolSpecificId;
                            return "$/{0}/{1}".FormatInvariant(teamProjectGuid, item.Id.ToString());
                        }
                    }
                    throw new InvalidOperationException("The shared queries folder could not be retrieved");
                }),
                new PermissionGroupFactory(PermissionScope.TeamBuild, BuildSecurity.BuildNamespaceId, "Team Build", (tpc, tfsVersion, teamProjectName, teamProjectUri) => LinkingUtilities.DecodeUri(teamProjectUri).ToolSpecificId),
                new PermissionGroupFactory(PermissionScope.SourceControl, SecurityConstants.RepositorySecurityNamespaceGuid, "TFVC", (tpc, tfsVersion, teamProjectName, teamProjectUri) =>
                {
                    var vcs = tpc.GetService<VersionControlServer>();
                    var teamProject = vcs.TryGetTeamProject(teamProjectName);
                    return teamProject == null ? null: vcs.GetTeamProject(teamProjectName).ServerItem;
                }),
                new PermissionGroupFactory(PermissionScope.GitRepositories, GitConstants.GitSecurityNamespaceId, "Git", (tpc, tfsVersion, teamProjectName, teamProjectUri) =>
                {
                    // For pre-TFS 2015 this is "repositories/<guid>", otherwise it's "repoV2/<guid>".
                    var prefix = tfsVersion >= TfsMajorVersion.V14 ? "repoV2" : "repositories";
                    var teamProjectGuid = LinkingUtilities.DecodeUri(teamProjectUri).ToolSpecificId;
                    return "{0}/{1}".FormatInvariant(prefix, teamProjectGuid);
                }),
            };
        }

        #endregion

        #region GetPermissionGroups

        public static IList<PermissionGroup> GetPermissionGroups(TfsTeamProjectCollection tfs)
        {
            var groups = new List<PermissionGroup>();
            var securityNamespaces = tfs.GetService<ISecurityService>().GetSecurityNamespaces();
            foreach (var factory in permissionGroupFactories)
            {
                foreach (var securityNamespace in securityNamespaces)
                {
                    if (factory.AppliesTo(securityNamespace.Description.NamespaceId))
                    {
                        groups.Add(factory.GetPermissionGroup(securityNamespace));
                    }
                }
            }
            return groups;
        }

        #endregion

        #region ExportPermissions

        public static void ExportPermissions(ILogger logger, ApplicationTask task, TfsTeamProjectCollection tfs, TfsMajorVersion tfsVersion, IList<SecurityGroupPermissionExportRequest> exportRequests)
        {
            if (exportRequests.Any())
            {
                var step = 0;
                var securityNamespaces = tfs.GetService<ISecurityService>().GetSecurityNamespaces();
                var ims = tfs.GetService<IIdentityManagementService>();
                foreach (var exportRequest in exportRequests)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Exporting \"{0}\" permissions from Team Project \"{1}\"", exportRequest.SecurityGroup.Name, exportRequest.SecurityGroup.TeamProject.Name));
                    try
                    {
                        var identity = ims.ReadIdentity(IdentitySearchFactor.Identifier, exportRequest.SecurityGroup.Sid, MembershipQuery.None, ReadIdentityOptions.None);
                        if (identity == null)
                        {
                            var message = "The security group \"{0}\" could not be retrieved.".FormatCurrent(exportRequest.SecurityGroup.FullName);
                            logger.Log(message, TraceEventType.Warning);
                            task.SetWarning(message);
                        }
                        else
                        {
                            var permissions = new List<PermissionChangePersistenceData>();
                            foreach (var securityNamespace in securityNamespaces)
                            {
                                foreach (var factory in permissionGroupFactories)
                                {
                                    if (factory.AppliesTo(securityNamespace.Description.NamespaceId))
                                    {
                                        var token = factory.GetObjectToken(tfs, tfsVersion, exportRequest.SecurityGroup.TeamProject.Name, exportRequest.SecurityGroup.TeamProject.Uri.ToString());
                                        if (token != null)
                                        {
                                            var permissionGroup = factory.GetPermissionGroup(securityNamespace);
                                            var acl = securityNamespace.QueryAccessControlList(token, new[] { identity.Descriptor }, false);
                                            foreach (var ace in acl.AccessControlEntries)
                                            {
                                                foreach (var permission in permissionGroup.Permissions)
                                                {
                                                    var action = PermissionChangeAction.Inherit;
                                                    if ((permission.PermissionBit & ace.Allow) == permission.PermissionBit)
                                                    {
                                                        action = PermissionChangeAction.Allow;
                                                    }
                                                    else if ((permission.PermissionBit & ace.Deny) == permission.PermissionBit)
                                                    {
                                                        action = PermissionChangeAction.Deny;
                                                    }
                                                    permissions.Add(new PermissionChangePersistenceData(permission.Scope, permission.PermissionConstant, action));
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            Directory.CreateDirectory(Path.GetDirectoryName(exportRequest.FileName));
                            PermissionChangePersistenceData.Save(exportRequest.FileName, permissions);
                        }
                    }
                    catch (Exception exc)
                    {
                        var message = string.Format(CultureInfo.CurrentCulture, "An error occurred while exporting \"{0}\" permissions from Team Project \"{1}\"", exportRequest.SecurityGroup.Name, exportRequest.SecurityGroup.TeamProject.Name);
                        logger.Log(message, exc);
                        task.SetError(message, exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
            }
        }

        #endregion

        #region Apply

        public static void Apply(ApplicationTask task, TfsTeamProjectCollection tfs, TfsMajorVersion tfsVersion, string teamProjectName, SecurityGroupChange securityGroup)
        {
            var ims = tfs.GetService<IIdentityManagementService>();
            var css = tfs.GetService<ICommonStructureService>();
            var teamProject = css.GetProjectFromName(teamProjectName);
            var groupIdentityName = string.Format(CultureInfo.InvariantCulture, @"[{0}]\{1}", teamProjectName, securityGroup.Name);

            // Create the group if needed.
            var existingGroup = ims.ListApplicationGroups(teamProject.Uri, ReadIdentityOptions.IncludeReadFromSource).FirstOrDefault(g => string.Equals(g.DisplayName, groupIdentityName, StringComparison.OrdinalIgnoreCase));
            var members = new List<TeamFoundationIdentity>();
            IdentityDescriptor groupDescriptor;
            if (existingGroup == null)
            {
                // There is no existing group with the same name, create one.
                task.Status = "Creating new \"{0}\" security group".FormatCurrent(groupIdentityName);
                groupDescriptor = ims.CreateApplicationGroup(teamProject.Uri, securityGroup.Name, securityGroup.Description);
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
            if (securityGroup.PermissionGroupChanges.SelectMany(g => g.PermissionChanges).Any(p => p.Action != PermissionChangeAction.None))
            {
                var securityNamespaces = tfs.GetService<ISecurityService>().GetSecurityNamespaces();
                foreach (var groupChange in securityGroup.PermissionGroupChanges.Where(g => g.PermissionChanges.Any(c => c.Action != PermissionChangeAction.None)))
                {
                    foreach (var securityNamespace in securityNamespaces)
                    {
                        var factory = permissionGroupFactories.FirstOrDefault(f => f.AppliesTo(securityNamespace.Description.NamespaceId, groupChange.PermissionGroup.Scope));
                        if (factory != null)
                        {
                            var token = factory.GetObjectToken(tfs, tfsVersion, teamProject.Name, teamProject.Uri);
                            if (token != null)
                            {
                                ApplySecurityNamespacePermissions(token, groupDescriptor, securityNamespace, groupChange.PermissionChanges);
                            }
                        }
                    }
                }
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

        private static void ApplySecurityNamespacePermissions(string token, IdentityDescriptor identity, SecurityNamespace securityNamespace, IEnumerable<PermissionChange> permissions)
        {
            if (permissions.Where(p => p.Action != PermissionChangeAction.None).Any())
            {
                var allows = permissions.Where(p => p.Action == PermissionChangeAction.Allow).Aggregate(0, (sum, p) => sum += p.Permission.PermissionBit);
                var denies = permissions.Where(p => p.Action == PermissionChangeAction.Deny).Aggregate(0, (sum, p) => sum += p.Permission.PermissionBit);
                var inherits = permissions.Where(p => p.Action == PermissionChangeAction.Inherit).Aggregate(0, (sum, p) => sum += p.Permission.PermissionBit);
                if (allows > 0 || denies > 0 || inherits > 0)
                {
                    if (securityNamespace == null)
                    {
                        throw new InvalidOperationException("Permissions are being modified but the security namespace is not available in the current Team Project Collection.");
                    }
                    if (inherits > 0)
                    {
                        securityNamespace.RemovePermissions(token, identity, inherits);
                    }
                    if (allows > 0 || denies > 0)
                    {
                        securityNamespace.SetPermissions(token, identity, allows, denies, true);
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

        #region PermissionGroupFactory Class

        private class PermissionGroupFactory
        {
            private Guid securityNamespaceId;
            private PermissionScope scope;
            private string displayName;
            private IEnumerable<string> excludedActions;
            private Func<TfsTeamProjectCollection, TfsMajorVersion, string, string, string> objectTokenFactory;

            public PermissionGroupFactory(PermissionScope scope, Guid securityNamespaceId, string displayName, Func<TfsTeamProjectCollection, TfsMajorVersion, string, string, string> objectTokenFactory)
                : this(scope, securityNamespaceId, displayName, null, objectTokenFactory)
            {
            }

            public PermissionGroupFactory(PermissionScope scope, Guid securityNamespaceId, string displayName, IEnumerable<string> excludedActions, Func<TfsTeamProjectCollection, TfsMajorVersion, string, string, string> objectTokenFactory)
            {
                this.scope = scope;
                this.securityNamespaceId = securityNamespaceId;
                this.displayName = displayName;
                this.excludedActions = excludedActions ?? Enumerable.Empty<string>();
                this.objectTokenFactory = objectTokenFactory;
            }

            public bool AppliesTo(Guid securityNamespaceId)
            {
                return this.securityNamespaceId == securityNamespaceId;
            }

            public bool AppliesTo(Guid securityNamespaceId, PermissionScope scope)
            {
                return this.AppliesTo(securityNamespaceId) && this.scope == scope;
            }

            public PermissionGroup GetPermissionGroup(SecurityNamespace securityNamespace)
            {
                var permissions = new List<Permission>();
                foreach (var action in securityNamespace.Description.Actions.OrderBy(a => a.DisplayName))
                {
                    if (!this.excludedActions.Any(p => p == action.Name))
                    {
                        permissions.Add(new Permission(this.scope, action.DisplayName, action.Name, action.Bit));
                    }
                }
                return new PermissionGroup(this.scope, this.displayName, permissions);
            }

            public string GetObjectToken(TfsTeamProjectCollection tpc, TfsMajorVersion tfsVersion, string teamProjectName, string teamProjectUri)
            {
                return this.objectTokenFactory(tpc, tfsVersion, teamProjectName, teamProjectUri);
            }
        }

        #endregion
    }
}