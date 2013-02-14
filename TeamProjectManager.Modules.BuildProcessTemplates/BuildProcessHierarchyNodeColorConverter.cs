using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TeamProjectManager.Modules.BuildProcessTemplates
{
    public class BuildProcessHierarchyNodeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var node = (BuildProcessHierarchyNode)value;
            return node.HasBuildDefinitions ? DependencyProperty.UnsetValue : Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}