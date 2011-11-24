using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.SourceControl
{
    public class CheckinNoteField : ObservableObject
    {
        #region Observable Properties

        public int DisplayOrder
        {
            get { return this.GetValue(DisplayOrderProperty); }
            set { this.SetValue(DisplayOrderProperty, value); }
        }

        public static ObservableProperty<int> DisplayOrderProperty = new ObservableProperty<int, CheckinNoteField>(o => o.DisplayOrder, 1);

        public string Name
        {
            get { return this.GetValue(NameProperty); }
            set { this.SetValue(NameProperty, value); }
        }

        public static ObservableProperty<string> NameProperty = new ObservableProperty<string, CheckinNoteField>(o => o.Name);

        public bool Required
        {
            get { return this.GetValue(RequiredProperty); }
            set { this.SetValue(RequiredProperty, value); }
        }

        public static ObservableProperty<bool> RequiredProperty = new ObservableProperty<bool, CheckinNoteField>(o => o.Required);

        #endregion

        #region Constructors

        public CheckinNoteField()
        {
        }

        public CheckinNoteField(int displayOrder, string name, bool required)
        {
            this.DisplayOrder = displayOrder;
            this.Name = name;
            this.Required = required;
        }

        #endregion
    }
}