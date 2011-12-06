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
            var boolFormat = parameter != null && string.Equals("bool", parameter.ToString(), StringComparison.OrdinalIgnoreCase);
            if (boolFormat)
            {
                return status == ComparisonStatus.AreEqual;
            }
            else
            {
                var shortFormat = parameter != null && string.Equals("short", parameter.ToString(), StringComparison.OrdinalIgnoreCase);
                switch (status)
                {
                    case ComparisonStatus.AreEqual:
                        return shortFormat ? "==" : "Exact match";
                    case ComparisonStatus.AreDifferent:
                        return shortFormat ? "<>" : "Different";
                    case ComparisonStatus.ExistsOnlyInSource:
                        return shortFormat ? "<-" : "Exists only in the source";
                    case ComparisonStatus.ExistsOnlyInTarget:
                        return shortFormat ? "->" : "Does not exist in the source";
                    default:
                        return status.ToString();
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}