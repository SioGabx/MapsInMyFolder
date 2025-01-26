using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MapsInMyFolder.Core.XMLDatabase
{
    public static class Reader
    {
        public static XElement ReadGetXElement(this Database database, string key)
        {
            var doc = XDocument.Load(database.Path);
            var rootElement = doc.Descendants(Database.BaseContentName).FirstOrDefault();

            if (rootElement == null)
                throw new InvalidOperationException($"Root element '{Database.BaseContentName}' not found in the database.");

            var paths = key.Split('/', StringSplitOptions.RemoveEmptyEntries);

            return paths.Aggregate(rootElement, (current, path) => current?.Element(path.Trim()));
        }
        public static string Read(this Database database, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            XElement element = ReadGetXElement(database, key);
            return element?.Value.XMLUnescape();
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
