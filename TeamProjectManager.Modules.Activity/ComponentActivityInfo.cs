using System;

namespace TeamProjectManager.Modules.Activity
{
    public class ComponentActivityInfo
    {
        public string ComponentName { get; private set; }
        public DateTime Time { get; private set; }
        public string User { get; private set; }
        public string Description { get; private set; }

        public ComponentActivityInfo(string componentName, DateTime time, string user, string description)
        {
            this.ComponentName = componentName;
            this.Time = time;
            this.User = user;
            this.Description = description;
        }
    }
}