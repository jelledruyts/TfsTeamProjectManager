using System;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Flags]
    public enum ImportOptions
    {
        None = 0,
        Validate = 1,
        Import = 2,
        SaveCopy = 4
    }
}