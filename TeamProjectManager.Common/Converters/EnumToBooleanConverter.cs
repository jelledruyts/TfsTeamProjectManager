using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TeamProjectManager.Common.Converters
{
    /// <summary>
    /// A value converter that returns a value that determines if the current enum value is equal to a specified converter argument.
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
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
            string parameterString = parameter as string;
            if (value == null || parameterString == null)
            {
                return DependencyProperty.UnsetValue;
            }

            if (!Enum.IsDefined(value.GetType(), value))
            {
                return DependencyProperty.UnsetValue;
            }

            var enumType = GetEnumType(value.GetType());
            object parameterValue = Enum.Parse(enumType, parameterString);
            return parameterValue.Equals(value);
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
            string parameterString = parameter as string;
            if (parameterString == null || value.Equals(false))
            {
                return Binding.DoNothing;
            }

            var enumType = GetEnumType(targetType);
            return Enum.Parse(enumType, parameterString);
        }

        /// <summary>
        /// Gets the type of the enum.
        /// </summary>
        /// <param name="targetType">The target type, i.e. an enum type or a nullable enum type.</param>
        /// <returns>The enum type.</returns>
        private static Type GetEnumType(Type targetType)
        {
            var enumType = targetType;
            if (enumType.IsGenericType && enumType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // If it is nullable, then get the underlying type; e.g. if "Nullable<int>" then this will return just "int".
                enumType = enumType.GetGenericArguments()[0];
            }
            return enumType;
        }
    }
}