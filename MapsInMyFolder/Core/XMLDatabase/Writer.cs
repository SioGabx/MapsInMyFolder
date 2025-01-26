using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Navigation;
using System.Xml;
using System.Xml.Linq;

namespace MapsInMyFolder.Core.XMLDatabase
{
    public static class Writer
    {
        public static void Write(this Database database, string key, string value, string description = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            value ??= string.Empty;
            value = value.XMLEscape();
            var doc = XDocument.Load(database.Path);
            var paths = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
            XElement currentElement = doc.Descendants(Database.BaseContentName).FirstOrDefault() ?? throw new InvalidOperationException($"Root element '{Database.BaseContentName}' not found.");

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
            XElement element = Reader.ReadGetXElement(database, key);
            element?.SetAttributeValue(attribute.Trim(), attribute_value);
            element.Document.Save(database.Path);
        }
    }
}
