using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace MapsInMyFolder.Commun
{
    public class LanguageDictionaryWrapper<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, TValue> _dictionary;

        public LanguageDictionaryWrapper(Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public TValue NullKeyValue => default;

        public TValue this[TKey key]
        {
            get
            {
                if (_dictionary is null)
                {
                    return NullKeyValue;
                }
                if (_dictionary.TryGetValue(key, out TValue value) && !EqualityComparer<TValue>.Default.Equals(value, default))
                {
                    return value;
                }
                else if (!EqualityComparer<TKey>.Default.Equals(key, default))
                {
                    Debug.WriteLine($"Languages : Key {key} not found");
                    return (TValue)(object)$"%{key}%";
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

        public string this[TKey key, params object[] args]
        {
            get
            {
                string RawValue = this[key].ToString();

                foreach (var arg in args)
                {
                    RawValue = RawValue.ReplaceSingle("{%s}", arg.ToString());
                }
                return RawValue;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class Languages
    {
        public enum Language
        {
            [UserFriendlyString("%settingsPropertyValuesLanguageUseSystemLanguage%")]
            SystemLanguage,
            [UserFriendlyString("Français")]
            French,
            [UserFriendlyString("English")]
            English,
        }

        private static LanguageDictionaryWrapper<string, string> _current = new LanguageDictionaryWrapper<string, string>(new Dictionary<string, string>());

        public static LanguageDictionaryWrapper<string, string> Current
        {
            get { return _current; }
        }

        static Languages()
        {
            Language SelectedLanguage = Settings.application_languages;
            if (SelectedLanguage == Language.SystemLanguage)
            {
                SelectedLanguage = DetectSystemLanguage();
            }
            Load(SelectedLanguage);
        }

        private static Language DetectSystemLanguage()
        {
            CultureInfo culture = CultureInfo.CurrentUICulture;
            if (Enum.TryParse(culture.Parent.EnglishName, true, out Language SystemLanguage))
            {
                return SystemLanguage;
            }
            else
            {
                return Language.English;
            }
        }

        public static void Load(Language language)
        {
            var languageIniValues = Collectif.ReadResourceString($"Commun/Languages/Ressources/{language}.ini");
            var dictionary = IniFileReader.ReadString(languageIniValues);
            _current = new LanguageDictionaryWrapper<string, string>(dictionary);
        }

        public static string GetWithArguments(string key, params object[] args)
        {
            return Current[key, args];
        }

        public static string ReplaceInString(string texte)
        {
            foreach (KeyValuePair<string, string> keyValuePair in Current)
            {
                texte = texte.Replace($"%{keyValuePair.Key}%", keyValuePair.Value);
            }
            return texte;
        }
    }
}
