using System;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Flags]
    public enum ImportOptions
    {
        None = 0,
        ValidateWorkItemTypeDefinitions = 1,
        ImportWorkItemTypeDefinitions = 2,
        Simulate = 4,
        SaveCopy = 8
    }
}