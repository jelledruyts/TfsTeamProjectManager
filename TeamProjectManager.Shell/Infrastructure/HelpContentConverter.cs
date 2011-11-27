using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TeamProjectManager.Shell.Infrastructure
{
    public class HelpContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var message = value as string;
            if (message != null)
            {
                // Convert strings to text blocks with word wrapping.
                return new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
            }
            else
            {
                // Any other help content should remain as is.
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}