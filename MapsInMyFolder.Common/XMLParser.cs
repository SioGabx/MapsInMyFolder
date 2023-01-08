using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace MapsInMyFolder.Commun
{
    public static class XMLParser
    {
        public static string Read(string key_arg)
        {
            string key = key_arg;
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            else
            {
                key = key_arg.Trim();
            }

            XDocument doc = XDocument.Load(GetPathAndCreateIfNotExist());
            XElement KeyElement = doc.Descendants(key).FirstOrDefault<XElement>();
            if (KeyElement != null)
            {
                return KeyElement.Value;
            }
            return null;
        }

        public static string GetPathAndCreateIfNotExist()
        {
            string strSettingsXmlFilePath = Settings.SettingsPath();
            if (!File.Exists(strSettingsXmlFilePath))
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(strSettingsXmlFilePath));
                var sts = new XmlWriterSettings()
                {
                    Indent = true,
                };
                string newPath = Settings.SettingsPath(true);
                XmlWriter xmlWriter = XmlWriter.Create(newPath, sts);
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("Settings");
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
                return newPath;
            }
            return strSettingsXmlFilePath;
        }

        public static void Write(string key_arg, string value_arg = null, string description_arg = null)
        {
            string key = key_arg;
            string value = value_arg;
            string description = description_arg;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            else
            {
                key = key_arg.Trim();
            }
            if (string.IsNullOrEmpty(value_arg))
            {
                value = String.Empty;
            }
            if (string.IsNullOrEmpty(description_arg))
            {
                description = String.Empty;
            }
            string strSettingsXmlFilePath = Settings.SettingsPath();
            XDocument doc;
            try
            {
                doc = XDocument.Load(GetPathAndCreateIfNotExist());
            }
            catch (FileNotFoundException)
            {
                Settings.SettingsPath(true);
                doc = XDocument.Load(GetPathAndCreateIfNotExist());
            }
            XElement KeyElement = doc.Descendants(key).FirstOrDefault<XElement>();
            if (KeyElement != null)
            {
                if (KeyElement.Value != value)
                {
                    KeyElement.SetValue(value);
                }
                else
                {
                    return;
                }
            }
            else
            {
                XElement Settings = doc.Descendants("Settings").FirstOrDefault<XElement>();
                if (Settings != null)
                {
                    XElement xElement = new XElement(key, value);
                    if (!string.IsNullOrEmpty(description))
                    {
                        XComment xRoleComment = new XComment(description);
                        Settings.Add(xRoleComment);
                    }
                    Settings.Add(xElement);
                }
            }
            doc.Save(strSettingsXmlFilePath);
        }
    }
}
