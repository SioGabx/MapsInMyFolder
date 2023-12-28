using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapsInMyFolder.Commun
{
    public class Country
    {
        public string DisplayName { get; set; }
        public string EnglishName { get; set; }
        public static IEnumerable<Country> GetList()
        {
            //TODO : https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.twoletterisolanguagename?view=net-7.0
            //https://yammadev.github.io/flag-icons/
            List<Country> CountryList = new List<Country>();
            foreach (var ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                string CurentCultureCountry = GetCountryFromCultureName(ci.DisplayName);
                string EnglishCultureCountry = GetCountryFromCultureName(ci.EnglishName);
                //Debug.WriteLine(CurentCultureCountry + " / " + EnglishCultureCountry + " / " + ci.Name);
                if (!string.IsNullOrWhiteSpace(CurentCultureCountry) && !string.IsNullOrWhiteSpace(EnglishCultureCountry))
                {
                    CountryList.Add(new Country() { EnglishName = EnglishCultureCountry, DisplayName = CurentCultureCountry });
                }
            }

            CountryList.Add(new Country() { EnglishName = "World", DisplayName = "Monde" });
            CountryList.Add(new Country() { EnglishName = "*", DisplayName = "*" });

            //remove duplicates and order by DisplayName
            return CountryList.GroupBy(x => x.EnglishName).Select(y => y.First()).OrderBy(z => z.DisplayName);
        }

        public static List<Country> GetListFromEnglishName(IEnumerable<string> ListOfEnglishName)
        {
            List<Country> CountryList = new List<Country>();
            foreach (var ci in GetList())
            {
                if (ListOfEnglishName.Contains(ci.EnglishName))
                {
                    CountryList.Add(ci);
                }
            }
            return CountryList;
        }

        public static List<Country> GetListFromEnglishName(string EnglishName)
        {
            return GetListFromEnglishName(new string[] { EnglishName });
        }

        private static string GetCountryFromCultureName(string Name)
        {
            string resultString = Regex.Match(Name, @"(?<=\().+?(?=\))", RegexOptions.RightToLeft).Value;
            if (resultString.Contains(','))
            {
                string[] regionsplit = resultString.Split(',');
                resultString = regionsplit[regionsplit.Length - 1];
            }
            if (resultString.Contains('[') && resultString.Contains(']'))
            {
                Regex regex = new Regex("\\[.*?\\]");
                resultString = regex.Replace(resultString, "");
            }
            resultString = resultString.Trim();
            if (!string.IsNullOrWhiteSpace(resultString) && char.IsLower(resultString[0]))
            {
                resultString = string.Empty;
            }

            return resultString;
        }
    }

    public static class CountryExtensions
    {
        public static bool Contains(this IEnumerable<Country> countries, string EnglishCountryName)
        {
            foreach (Country country in countries)
            {
                if (country.EnglishName == EnglishCountryName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
