using System.Runtime.Serialization;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [DataContract(Namespace = "http://schemas.teamprojectmanager.codeplex.com/workitemconfigurationtransform/2013/04")]
    public class WorkItemConfigurationTransformationItem : ObservableObject
    {
        #region Observable Properties

        [DataMember]
        public string Description
        {
            get { return this.GetValue(DescriptionProperty); }
            set { this.SetValue(DescriptionProperty, value); }
        }

        public static readonly ObservableProperty<string> DescriptionProperty = new ObservableProperty<string, WorkItemConfigurationTransformationItem>(o => o.Description);

        [DataMember]
        public WorkItemConfigurationItemType WorkItemConfigurationItemType
        {
            get { return this.GetValue(WorkItemConfigurationItemTypeProperty); }
            set { this.SetValue(WorkItemConfigurationItemTypeProperty, value); }
        }

        public static readonly ObservableProperty<WorkItemConfigurationItemType> WorkItemConfigurationItemTypeProperty = new ObservableProperty<WorkItemConfigurationItemType, WorkItemConfigurationTransformationItem>(o => o.WorkItemConfigurationItemType);

        [DataMember]
        public string WorkItemTypeNames
        {
            get { return this.GetValue(WorkItemTypeNamesProperty); }
            set { this.SetValue(WorkItemTypeNamesProperty, value); }
        }

        public static readonly ObservableProperty<string> WorkItemTypeNamesProperty = new ObservableProperty<string, WorkItemConfigurationTransformationItem>(o => o.WorkItemTypeNames);

        [DataMember]
        public TransformationType TransformationType
        {
            get { return this.GetValue(TransformationTypeProperty); }
            set { this.SetValue(TransformationTypeProperty, value); }
        }

        public static readonly ObservableProperty<TransformationType> TransformationTypeProperty = new ObservableProperty<TransformationType, WorkItemConfigurationTransformationItem>(o => o.TransformationType);

        [DataMember]
        public string TransformationXml
        {
            get { return this.GetValue(TransformationXmlProperty); }
            set { this.SetValue(TransformationXmlProperty, value); }
        }

        public static readonly ObservableProperty<string> TransformationXmlProperty = new ObservableProperty<string, WorkItemConfigurationTransformationItem>(o => o.TransformationXml);

        [IgnoreDataMember]
        public string DisplayName
        {
            get { return this.GetValue(DisplayNameProperty); }
            private set { this.SetValue(DisplayNameProperty, value); }
        }

        public static readonly ObservableProperty<string> DisplayNameProperty = new ObservableProperty<string, WorkItemConfigurationTransformationItem>(o => o.DisplayName);

        [IgnoreDataMember]
        public bool IsValid
        {
            get { return this.GetValue(IsValidProperty); }
            private set { this.SetValue(IsValidProperty, value); }
        }

        public static readonly ObservableProperty<bool> IsValidProperty = new ObservableProperty<bool, WorkItemConfigurationTransformationItem>(o => o.IsValid);

        #endregion

        #region Constructors

        public WorkItemConfigurationTransformationItem()
        {
        }

        #endregion

        #region Overrides

        protected override void OnObservablePropertyChanged(ObservablePropertyChangedEventArgs e)
        {
            base.OnObservablePropertyChanged(e);
            if (!string.IsNullOrEmpty(this.Description))
            {
                this.DisplayName = this.Description;
            }
            else
            {
                this.DisplayName = "{0} transformation of {1}".FormatCurrent(this.TransformationType.ToString().ToUpper(), WorkItemConfigurationItem.GetDisplayName(this.WorkItemConfigurationItemType, this.WorkItemTypeNames));
            }
            this.IsValid = !string.IsNullOrWhiteSpace(this.TransformationXml);
        }

        #endregion
    }
}