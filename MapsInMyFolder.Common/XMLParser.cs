using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace MapsInMyFolder.Commun
{
    public static class XMLParser
    {
        public static XMLSettingsParser Settings = new XMLSettingsParser();
        public static XMLCacheParser Cache = new XMLCacheParser();
    }

    public abstract class XMLParserBase
    {
        public abstract string Type { get; }
        public string Read(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            XDocument doc = XDocument.Load(GetPathAndCreateIfNotExist());
            XElement KeyElement = doc.Descendants(key.Trim()).FirstOrDefault<XElement>();

            return KeyElement?.Value;
        }

        public virtual string GetPathAndCreateIfNotExist()
        {
            string FilePath = System.IO.Path.Combine(Settings.working_folder, Type + ".xml");
            if (!File.Exists(FilePath))
            {
                CreateFile(FilePath);
            }
            return FilePath;
        }

        public void CreateFile(string FilePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            var sts = new XmlWriterSettings()
            {
                Indent = true,
            };
            XmlWriter xmlWriter = XmlWriter.Create(FilePath, sts);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement(Type);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        public void Write(string key, string value, string description = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            if (string.IsNullOrEmpty(value))
            {
                value = String.Empty;
            }
            string strSettingsXmlFilePath = GetPathAndCreateIfNotExist();
            XDocument doc = XDocument.Load(strSettingsXmlFilePath);
            XElement KeyElement = doc.Descendants(key.Trim()).FirstOrDefault<XElement>();
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
                XElement XMLFileDocument = doc.Descendants(Type).FirstOrDefault<XElement>();
                if (XMLFileDocument != null)
                {
                    XElement xElement = new XElement(key, value);
                    if (!string.IsNullOrEmpty(description))
                    {
                        XComment xRoleComment = new XComment(description);
                        XMLFileDocument.Add(xRoleComment);
                    }
                    XMLFileDocument.Add(xElement);
                }
            }
            doc.Save(strSettingsXmlFilePath);
        }

        public void WriteAttribute(string key, string attribute, string attribute_value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(attribute))
            {
                return;
            }
            if (string.IsNullOrEmpty(attribute_value))
            {
                attribute_value = String.Empty;
            }

            string strSettingsXmlFilePath = GetPathAndCreateIfNotExist();
            XDocument doc = XDocument.Load(strSettingsXmlFilePath);
            XElement KeyElement = doc.Descendants(key.Trim()).FirstOrDefault<XElement>();
            if (KeyElement != null)
            {
                KeyElement.SetAttributeValue(attribute.Trim(), attribute_value);
            }
            doc.Save(strSettingsXmlFilePath);
        }

        public string ReadAttribute(string key, string attribute)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(attribute))
            {
                return null;
            }

            XDocument doc = XDocument.Load(GetPathAndCreateIfNotExist());
            XElement KeyElement = doc.Descendants(key.Trim()).FirstOrDefault<XElement>();
            return KeyElement?.Attribute(attribute.Trim())?.Value;
        }

    }

    public class XMLSettingsParser : XMLParserBase
    {
        public override string Type => "Settings";
        public override string GetPathAndCreateIfNotExist()
        {
            string strSettingsXmlFilePath = Settings.SettingsPath();
            if (!File.Exists(strSettingsXmlFilePath))
            {
                string newPath = Settings.SettingsPath(true);
                CreateFile(newPath);
                return newPath;
            }
            return strSettingsXmlFilePath;
        }
    }

    public class XMLCacheParser : XMLParserBase
    {
        public override string Type => "Cache";
    }


}
