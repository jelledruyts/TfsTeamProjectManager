using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Xml;

namespace TeamProjectManager.Common.Converters
{
    /// <summary>
    /// A value converter that converts between <see cref="XmlDocument"/> and <see cref="string"/>.
    /// </summary>
    public class XmlDocumentConverter : IValueConverter
    {
        private static readonly XmlWriterSettings writerSettings = new XmlWriterSettings { Indent = true };

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
            var document = value as XmlDocument;
            if (document == null)
            {
                return null;
            }
            var formattedString = new StringBuilder();
            using (var writer = XmlTextWriter.Create(formattedString, writerSettings))
            {
                document.WriteTo(writer);
                writer.Flush();
            }
            return formattedString.ToString();
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            var document = new XmlDocument();
            try
            {
                document.LoadXml(value.ToString());
            }
            catch (XmlException)
            {
            }
            return document;
        }
    }
}