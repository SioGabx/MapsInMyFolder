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
                xmlWriter.WriteStartElement(Database.BaseContentName);
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
        }

        public static void CreateFile(this Database database)
        {
            CreateFile(database.Path);
        }
    }
}
