using System;
using System.Collections.Generic;

namespace MapsInMyFolder.Commun
{
    public static class IniFileReader
    {
        public static Dictionary<string, string> ReadString(string iniString)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

            string[] lines = iniString.Split(new string[] {"\n", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // Ignorer les lignes vides et les commentaires et les sections
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#") || (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]")))
                    continue;

                // Rechercher les clés et les valeurs
                int separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex > 0)
                {
                    string key = trimmedLine.Substring(0, separatorIndex).Trim();
                    string value = trimmedLine.Substring(separatorIndex + 1).Trim();
                    value = Collectif.HTMLEntities(value.Replace("\\n", "\n"), true);
                    keyValuePairs[key] = value;
                }
            }

            return keyValuePairs;
        }
    }
}
