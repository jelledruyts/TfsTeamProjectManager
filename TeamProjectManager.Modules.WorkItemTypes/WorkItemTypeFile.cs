using System.IO;
using System.Xml.Linq;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeFile
    {
        public string Path { get; private set; }
        public string DisplayName { get; private set; }

        public WorkItemTypeFile(string path)
        {
            this.Path = path;
            if (File.Exists(path))
            {
                try
                {
                    var doc = XDocument.Load(path);
                    this.DisplayName = doc.Element(XName.Get("WITD", "http://schemas.microsoft.com/VisualStudio/2008/workitemtracking/typedef")).Element("WORKITEMTYPE").Attribute("name").Value;
                }
                catch
                {
                    // Ignore exceptions from XML parsing.
                }
            }
            if (string.IsNullOrEmpty(this.DisplayName))
            {
                this.DisplayName = new FileInfo(path).Name;
            }
        }

        public WorkItemTypeFile(string path, string displayName)
        {
            this.Path = path;
            this.DisplayName = displayName;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}