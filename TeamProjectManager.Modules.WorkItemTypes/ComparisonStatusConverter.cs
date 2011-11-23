using System;
using System.Globalization;
using System.Windows.Data;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class ComparisonStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (ComparisonStatus)value;
            switch (status)
            {
                case ComparisonStatus.AreEqual:
                    return "Exact match";
                case ComparisonStatus.AreDifferent:
                    return "Different";
                case ComparisonStatus.ExistsOnlyInSource:
                    return "Exists only in the source";
                case ComparisonStatus.ExistsOnlyInTarget:
                    return "Does not exist in the source";
                default:
                    return status.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}