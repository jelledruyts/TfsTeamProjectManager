using System;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    [Flags]
    public enum ImportOptions
    {
        None = 0,
        Validate = 1,
        Import = 2
    }
}