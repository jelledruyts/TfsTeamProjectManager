using System;

namespace TeamProjectManager.Modules.Activity
{
    [Flags]
    public enum ActivityComponentTypes
    {
        None = 0,
        SourceControl = 1,
        WorkItemTracking = 2,
        TeamBuild = 4,
        All = SourceControl | WorkItemTracking | TeamBuild
    }
}