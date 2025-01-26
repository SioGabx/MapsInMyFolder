using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MapsInMyFolder.Core.XMLDatabase
{

    public static class Sanitizing
    {
        private static readonly (char Character, string Replacement)[] InvalidXMLCharacters = {
            ('<', "&lt;"),
            ('>', "&gt;"),
            ('"', "&quot;"),
            ('\'', "&apos;"),
            ('&', "&amp;")
        };

        public static string XMLEscape(this string text)
        {
            foreach (var (character, replacement) in InvalidXMLCharacters)
            {
                text = text.Replace(character.ToString(), replacement);
            }
            return text;
        }

        public static string XMLUnescape(this string text)
        {
            foreach (var (character, replacement) in InvalidXMLCharacters)
            {
                text = text.Replace(replacement, character.ToString());
            }
            return text;
        }
    }
}
