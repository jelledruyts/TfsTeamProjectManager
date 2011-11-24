using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.SourceControl
{
    public class SourceControlSettings : ObservableObject
    {
        #region Properties

        public string TeamProject { get; private set; }

        #endregion

        #region Observable Properties

        public bool EnableMultipleCheckout
        {
            get { return this.GetValue(EnableMultipleCheckoutProperty); }
            set { this.SetValue(EnableMultipleCheckoutProperty, value); }
        }

        public static ObservableProperty<bool> EnableMultipleCheckoutProperty = new ObservableProperty<bool, SourceControlSettings>(o => o.EnableMultipleCheckout, true);

        public bool EnableGetLatestOnCheckout
        {
            get { return this.GetValue(EnableGetLatestOnCheckoutProperty); }
            set { this.SetValue(EnableGetLatestOnCheckoutProperty, value); }
        }

        public static ObservableProperty<bool> EnableGetLatestOnCheckoutProperty = new ObservableProperty<bool, SourceControlSettings>(o => o.EnableGetLatestOnCheckout);

        public ObservableCollection<CheckinNoteField> CheckinNoteFields
        {
            get { return this.GetValue(CheckinNoteFieldsProperty); }
            set { this.SetValue(CheckinNoteFieldsProperty, value); }
        }

        public static ObservableProperty<ObservableCollection<CheckinNoteField>> CheckinNoteFieldsProperty = new ObservableProperty<ObservableCollection<CheckinNoteField>, SourceControlSettings>(o => o.CheckinNoteFields, OnCheckinNoteFieldsChanged);

        public string CheckinNoteFieldsList
        {
            get { return this.GetValue(CheckinNoteFieldsListProperty); }
            private set { this.SetValue(CheckinNoteFieldsListProperty, value); }
        }

        public static ObservableProperty<string> CheckinNoteFieldsListProperty = new ObservableProperty<string, SourceControlSettings>(o => o.CheckinNoteFieldsList);

        #endregion

        #region Property Change Handlers

        private static void OnCheckinNoteFieldsChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<ObservableCollection<CheckinNoteField>> e)
        {
            var value = (SourceControlSettings)sender;
            value.CheckinNoteFieldsList = value.CheckinNoteFields == null ? null : string.Join(", ", value.CheckinNoteFields.OrderBy(c => c.DisplayOrder).Select(c => c.Name + (c.Required ? "*" : string.Empty)));
        }

        #endregion

        #region Constructors

        public SourceControlSettings()
            : this(null, true, false, null)
        {
        }

        public SourceControlSettings(string teamProject, bool enableMultipleCheckout, bool enableGetLatestOnCheckout, IEnumerable<CheckinNoteField> checkinNoteFields)
        {
            this.TeamProject = teamProject;
            this.EnableMultipleCheckout = enableMultipleCheckout;
            this.EnableGetLatestOnCheckout = enableGetLatestOnCheckout;
            this.CheckinNoteFields = new ObservableCollection<CheckinNoteField>((checkinNoteFields ?? new CheckinNoteField[0]).OrderBy(f => f.DisplayOrder));
        }

        #endregion
    }
}