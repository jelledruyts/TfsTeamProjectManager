using System;
using System.Globalization;
using System.Windows.Data;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class WorkItemConfigurationItemTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = (WorkItemConfigurationItemType)value;
            switch (type)
            {
                case WorkItemConfigurationItemType.WorkItemType:
                    return WorkItemConfigurationItem.WorkItemTypeDefinitionName;
                case WorkItemConfigurationItemType.Categories:
                    return WorkItemConfigurationItem.CategoriesName;
                default:
                    return type.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}