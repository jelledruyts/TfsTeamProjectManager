using System;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Flags]
    public enum ImportOptions
    {
        None = 0,
        Simulate = 1,
        SaveCopy = 2
    }
}