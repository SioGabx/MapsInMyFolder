﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace MapsInMyFolder.Commun
{
    public class LanguageDictionaryWrapper<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, TValue> _dictionary;

        public LanguageDictionaryWrapper(Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public TValue NullKeyValue => default(TValue);

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (_dictionary is null)
                {
                    return NullKeyValue;
                }
                if (_dictionary.TryGetValue(key, out value) && !EqualityComparer<TValue>.Default.Equals(value, default(TValue)))
                {
                    return value;
                }
                else if (!EqualityComparer<TKey>.Default.Equals(key, default(TKey)))
                {
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
            French,
            English,
            None
        }

        private static LanguageDictionaryWrapper<string, string> _current = new LanguageDictionaryWrapper<string, string>(new Dictionary<string, string>());

        public static LanguageDictionaryWrapper<string, string> Current
        {
            get { return _current; }
        }

        static Languages()
        {
            Load(Language.English);
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
                RawValue = RawValue.ReplaceSingle("{%s}", arg.ToString());
            }
            return RawValue;
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