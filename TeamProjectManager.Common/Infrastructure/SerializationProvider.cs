using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Provides serialization services.
    /// </summary>
    public static class SerializationProvider
    {
        #region Fields

        private static XmlWriterSettings xmlWriterSettings = new XmlWriterSettings { Indent = true, NewLineHandling = NewLineHandling.Entitize };
        private static XmlWriterSettings xmlWriterSettingsForString = new XmlWriterSettings { OmitXmlDeclaration = true };

        #endregion

        #region Methods

        /// <summary>
        /// Reads a serialized object from a file.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="fileName">The file name of the serialized object.</param>
        /// <returns>The deserialized object.</returns>
        public static T Read<T>(string fileName)
        {
            var serializer = GetSerializer(typeof(T), true);
            using (var stream = File.OpenRead(fileName))
            {
                return (T)serializer.ReadObject(stream);
            }
        }

        /// <summary>
        /// Writes a serialized object to a file.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="fileName">The file name of the serialized object.</param>
        public static void Write<T>(T obj, string fileName)
        {
            Write(obj, fileName, false);
        }

        /// <summary>
        /// Writes a serialized object to a file.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="fileName">The file name of the serialized object.</param>
        /// <param name="preserveObjectReferences"><see langword="true"/> to use non-standard XML constructs to preserve object reference data; otherwise, <see langword="false"/>.</param>
        public static void Write<T>(T obj, string fileName, bool preserveObjectReferences)
        {
            var serializer = GetSerializer(obj.GetType(), preserveObjectReferences);
            using (var writer = XmlWriter.Create(fileName, xmlWriterSettings))
            {
                serializer.WriteObject(writer, obj);
            }
        }

        /// <summary>
        /// Creates a deep clone of an object by serializing and deserializing it.
        /// </summary>
        /// <typeparam name="T">The type of the object to clone.</typeparam>
        /// <param name="obj">The object to clone.</param>
        /// <returns>A deep clone of the object.</returns>
        public static T Clone<T>(T obj)
        {
            if (obj == null)
            {
                return default(T);
            }
            var serializer = GetSerializer(obj.GetType(), true);
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                stream.Position = 0;
                return (T)serializer.ReadObject(stream);
            }
        }

        /// <summary>
        /// Writes a serialized object to a string.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>The serialized string.</returns>
        public static string WriteToString<T>(T obj)
        {
            return WriteToString<T>(obj, false);
        }

        /// <summary>
        /// Writes a serialized object to a string.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="preserveObjectReferences"><see langword="true"/> to use non-standard XML constructs to preserve object reference data; otherwise, <see langword="false"/>.</param>
        /// <returns>The serialized string.</returns>
        public static string WriteToString<T>(T obj, bool preserveObjectReferences)
        {
            var serializer = GetSerializer(obj.GetType(), preserveObjectReferences);
            var message = new StringBuilder();
            using (var writer = XmlWriter.Create(message, xmlWriterSettingsForString))
            {
                serializer.WriteObject(writer, obj);
            }
            return message.ToString();
        }

        /// <summary>
        /// Reads a serialized object from a string.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="serializedInstance">The serialized object.</param>
        /// <returns>The deserialized object.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static T ReadFromString<T>(string serializedInstance)
        {
            var serializer = GetSerializer(typeof(T), true);
            using (var stringReader = new StringReader(serializedInstance))
            using (var xmlReader = new XmlTextReader(stringReader))
            {
                return (T)serializer.ReadObject(xmlReader);
            }
        }

        #endregion

        #region Helper Methods

        private static DataContractSerializer GetSerializer(Type type, bool preserveObjectReferences)
        {
            return new DataContractSerializer(type, null, int.MaxValue, false, preserveObjectReferences, null);
        }

        #endregion
    }
}
