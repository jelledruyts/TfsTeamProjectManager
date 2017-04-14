using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace TeamProjectManager.Common.Converters
{
    /// <summary>
    /// A value converter that returns a value that determines if the current enum value is equal to a specified converter argument.
    /// </summary>
    public class JoinedListConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = value as IEnumerable;
            if (list == null)
            {
                return value;
            }
            else
            {
                var joinedList = new StringBuilder();
                foreach (var item in list)
                {
                    if (item != null)
                    {
                        if (joinedList.Length > 0)
                        {
                            joinedList.Append(", ");
                        }
                        joinedList.Append(item.ToString());
                    }
                }
                return joinedList.ToString();
            }
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}