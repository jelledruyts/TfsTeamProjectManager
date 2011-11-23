using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class MatchingPartsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var matchingParts = (ICollection<WorkItemTypeDefinitionPart>)value;
            var result = new List<object>();
            foreach (WorkItemTypeDefinitionPart part in Enum.GetValues(typeof(WorkItemTypeDefinitionPart)))
            {
                result.Add(new { IsSelected = matchingParts.Contains(part), Name = part.ToString() });
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}