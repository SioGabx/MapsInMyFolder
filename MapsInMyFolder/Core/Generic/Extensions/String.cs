using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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


    }
}
