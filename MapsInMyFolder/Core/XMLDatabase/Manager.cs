using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Navigation;
using System.Xml;
using System.Xml.Linq;

namespace MapsInMyFolder.Core.XMLDatabase
{
    public static class Manager
    {
        const string DatabaseContentName = "Content";
        private static XElement ReadGetXElement(this Database database, string key)
        {
            var doc = XDocument.Load(database.Path);
            var rootElement = doc.Descendants(DatabaseContentName).FirstOrDefault();

            if (rootElement == null)
                throw new InvalidOperationException($"Root element '{DatabaseContentName}' not found in the database.");

            var paths = key.Split('/', StringSplitOptions.RemoveEmptyEntries);

            return paths.Aggregate(rootElement, (current, path) => current?.Element(path.Trim()));
        }
        private static void CreateFile(string filepath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            var settings = new XmlWriterSettings()
            {
                Indent = true,
            };
            using (var xmlWriter = XmlWriter.Create(filepath, settings))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement(DatabaseContentName);
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
        }

        public static void CreateFile(this Database database)
        {
            CreateFile(database.Path);
        }

        public static void Write(this Database database, string key, string value, string description = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            value ??= string.Empty;

            var doc = XDocument.Load(database.Path);
            var paths = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
            XElement currentElement = doc.Descendants(DatabaseContentName).FirstOrDefault() ?? throw new InvalidOperationException($"Root element '{DatabaseContentName}' not found.");

            foreach (var (pathSegment, isLastSegment) in paths.Select((p, i) => (p.Trim(), i == paths.Length - 1)))
            {
                var childElement = currentElement.Element(pathSegment);

                if (childElement == null)
                {
                    var newElement = new XElement(pathSegment, isLastSegment ? value : string.Empty);
                    if (isLastSegment && !string.IsNullOrWhiteSpace(description))
                    {
                        currentElement.Add(new XComment(description));
                    }
                    currentElement.Add(newElement);
                    currentElement = newElement;
                }
                else
                {
                    currentElement = childElement;
                    if (isLastSegment && childElement.Value != value)
                    {
                        childElement.SetValue(value);
                    }
                }
            }
            doc.Save(database.Path);
        }



        public static string Read(this Database database, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            XElement element = ReadGetXElement(database, key);
            return element?.Value;
        }



        public static void WriteAttribute(this Database database, string key, string attribute, string attribute_value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(attribute))
            {
                return;
            }
            if (string.IsNullOrEmpty(attribute_value))
            {
                attribute_value = string.Empty;
            }
            XElement element = ReadGetXElement(database, key);
            element?.SetAttributeValue(attribute.Trim(), attribute_value);
            element.Document.Save(database.Path);
        }

        public static string ReadAttribute(this Database database, string key, string attribute)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(attribute))
            {
                return null;
            }

            XElement element = ReadGetXElement(database, key);
            return element?.Attribute(attribute.Trim())?.Value;
        }














    }
}
