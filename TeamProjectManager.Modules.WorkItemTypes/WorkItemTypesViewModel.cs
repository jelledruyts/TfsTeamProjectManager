using System.ComponentModel.Composition;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    [Export]
    public class WorkItemTypesViewModel : ObservableObject
    {
        public string HeaderInfo { get { return "Work Item Types"; } }
    }
}
