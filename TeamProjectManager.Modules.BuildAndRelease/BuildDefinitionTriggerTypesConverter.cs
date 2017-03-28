using Microsoft.TeamFoundation.Build.WebApi;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace TeamProjectManager.Modules.BuildAndRelease
{
    public class BuildDefinitionTriggerTypesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var buildDefinition = value as BuildDefinition;
            if (buildDefinition == null)
            {
                return null;
            }
            var triggerTypes = buildDefinition.Triggers.Select(t => t.TriggerType.ToString()).Distinct().OrderBy(t => t);
            return string.Join(", ", triggerTypes);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}