using System;
using System.Globalization;
using System.Windows.Data;
using Microsoft.TeamFoundation.Build.Client;

namespace TeamProjectManager.Modules.BuildDefinitions
{
    public class ContinuousIntegrationTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var continuousIntegrationType = (ContinuousIntegrationType)value;
            switch (continuousIntegrationType)
            {
                case ContinuousIntegrationType.None:
                    return "Manual";
                case ContinuousIntegrationType.Individual:
                    return "Continuous Integration";
                case ContinuousIntegrationType.Batch:
                    return "Rolling";
                case ContinuousIntegrationType.Gated:
                    return "Gated";
                case ContinuousIntegrationType.Schedule:
                    return "Schedule";
                case ContinuousIntegrationType.ScheduleForced:
                    return "Schedule (Forced)";
                default:
                    return continuousIntegrationType.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
