using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace MapsInMyFolder.Commun
{
    public class LanguageDictionaryWrapper<TKey, TValue>
    {
        private readonly Dictionary<string, string> _dictionary;

        public LanguageDictionaryWrapper(Dictionary<string, string> dictionary)
        {
            _dictionary = dictionary;
        }

        public string NullKeyValue => "NullKey";

        public string this[string key]
        {
            get
            {
                string value = "";
                if (_dictionary is null)
                {
                    return NullKeyValue;
                }
                if (_dictionary.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
                else if (!string.IsNullOrEmpty(key))
                {
                    return $"%{key}%";
                }
                else
                {
                    return NullKeyValue;
                }

            }
            set
            {
                _dictionary[key] = value;
            }
        }
    }

    public static class Languages
    {
        public enum Language
        {
            French,
            English
        }

        private static LanguageDictionaryWrapper<string, string> _current = new LanguageDictionaryWrapper<string, string>(new Dictionary<string, string>());

        public static LanguageDictionaryWrapper<string, string> Current
        {
            get { return _current; }
        }

        static Languages()
        {
            Load(Language.French);
        }

        public static void Load(Language language)
        {
            var languageIniValues = Collectif.ReadResourceString($"Languages/{language}.ini");
            var dictionary = IniFileReader.ReadString(languageIniValues);


            _current = new LanguageDictionaryWrapper<string, string>(dictionary);
        }


        public static string GetWithArguments(string key, params object[] args)
        {
            string RawValue = Current[key];
            
            foreach (var arg in args)
            {
                RawValue = RawValue.ReplaceSingle("%s", arg.ToString());
            }
            return RawValue;
        }
    }
}
