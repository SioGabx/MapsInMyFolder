using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class String
    {
        public static string Wrap(this string text, int width)
        {
            if (string.IsNullOrEmpty(text) || width == 0 || width >= text.Length)
            {
                return text;
            }
            var sb = new StringBuilder();
            var sr = new StringReader(text);
            string line;
            var first = true;
            while ((line = sr.ReadLine()) != null)
            {
                var col = 0;
                if (!first)
                {
                    sb.AppendLine();
                    col = 0;
                }
                else
                {
                    first = false;
                }
                char[] wordBreakChars = new char[] { ' ', '_', '\n', '\r', '\v', '\f', '\0' };
                var words = line.Split(wordBreakChars, StringSplitOptions.RemoveEmptyEntries);

                for (var i = 0; i < words.Length; i++)
                {
                    var word = words[i];
                    if (i != 0)
                    {
                        sb.Append(' ');
                        ++col;
                    }
                    if (col + word.Length > width)
                    {
                        sb.AppendLine();
                        col = 0;
                    }
                    sb.Append(word);
                    col += word.Length;
                }
            }
            return sb.ToString();
        }


        public static Dictionary<string, string> GetHTMLEntities() => new Dictionary<string, string>
        {
            { "<", "&lt;" },
            { ">", "&gt;" },
            { "&", "&amp;" },
            { "\"", "&quot;" },
            { "'", "&apos;" },
            { "¢", "&cent;" },
            { "£", "&pound;" },
            { "¥", "&yen;" },
            { "€", "&euro;" },
            { "©", "&copy;" },
            { "®", "&reg;" },
            { "%", "&percnt;" },
            { "»", "&raquo;" },
            { "À", "&Agrave;" },
            { "Ç", "&Ccedil;" },
            { "È", "&Egrave;" },
            { "É", "&Eacute;" },
            { "Ê", "&Ecirc;" },
            { "Ô", "&Ocirc;" },
            { "Ù", "&Ugrave;" },
            { "à", "&agrave;" },
            { "ß", "&szlig;" },
            { "á", "&aacute;" },
            { "â", "&acirc;" },
            { "æ", "&aelig;" },
            { "ç", "&ccedil;" },
            { "è", "&egrave;" },
            { "é", "&eacute;" },
            { "ê", "&ecirc;" },
            { "ë", "&euml;" },
            { "ô", "&ocirc;" },
            { "ù", "&ugrave;" },
            { "ü", "&uuml;" },
            { "∣", "&mid;" }
        };

        public static string EncodeEntities(this string Value)
        {
            foreach (var HTMLEntity in GetHTMLEntities())
            {
                Value = Value.Replace(HTMLEntity.Key, HTMLEntity.Value);
            }
            return Value;
        }
        public static string DecodeEntities(this string Value)
        {
            foreach (var HTMLEntity in GetHTMLEntities())
            {
                Value = Value.Replace(HTMLEntity.Value, HTMLEntity.Key);
            }
            return Value;
        }

        public static bool IsURL(this string Value)
        {
            const string pattern = @"(http|https|ftp|)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?([a-zA-Z0-9\-\?\,\'\/\+&%\$#_]+)";
            Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(Value);
        }

        public static string ToSingleLine(this string Value)
        {
            if (string.IsNullOrWhiteSpace(Value))
            {
                return Value;
            }
            var sb = new StringBuilder(Value.Length);
            foreach (char i in Value)
            {
                if (i != '\n' && i != '\r' && i != '\t')
                {
                    sb.Append(i);
                }
            }
            return sb.ToString();
        }


    }
}
