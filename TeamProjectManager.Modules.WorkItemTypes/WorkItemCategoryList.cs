using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    [XmlRoot(Namespace = "http://schemas.microsoft.com/VisualStudio/2008/workitemtracking/categories", ElementName = "CATEGORIES")]
    public class WorkItemCategoryList
    {
        [XmlElement(ElementName = "CATEGORY", Namespace = "")]
        public ObservableCollection<WorkItemCategory> Categories { get; set; }

        public WorkItemCategoryList()
        {
            this.Categories = new ObservableCollection<WorkItemCategory>();
        }

        public XmlDocument Save()
        {
            var serializer = new XmlSerializer(typeof(WorkItemCategoryList));
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, this);
                stream.Flush();
                stream.Position = 0;
                var xml = new XmlDocument();
                xml.Load(stream);
                return xml;
            }
        }

        public static WorkItemCategoryList Load(XmlDocument xml)
        {
            var serializer = new XmlSerializer(typeof(WorkItemCategoryList));
            using (var stream = new MemoryStream())
            {
                xml.Save(stream);
                stream.Flush();
                stream.Position = 0;
                return (WorkItemCategoryList)serializer.Deserialize(stream);
            }
        }
    }
}