using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MapsInMyFolder.Commun
{
    public static class Extensions
    {

        public static string GetCellValue(this DataGridCellInfo cell)
        {
            string RawValue = cell.Item?.GetType()?.GetProperty(cell.Column?.SortMemberPath)?.GetValue(cell.Item, null)?.ToString();
            return RawValue;
        }
        public static object GetPropertyValue(this object T, string PropName)
        {
            return T.GetType().GetProperty(PropName)?.GetValue(T, null);
        }

        public static bool Contains(this string str, string substring, StringComparison comp)
        {
            if (substring == null)
            {
                throw new ArgumentNullException(nameof(substring), "substring cannot be null.");
            }
            else if (!Enum.IsDefined(typeof(StringComparison), comp))
            {
                throw new ArgumentException("comp is not a member of StringComparison", nameof(comp));
            }
            return str.IndexOf(substring, comp) >= 0;
        }

        public static string UcFirst(this string theString)
        {
            if (string.IsNullOrEmpty(theString))
            {
                return string.Empty;
            }
            char[] theChars = theString.ToCharArray();
            theChars[0] = char.ToUpper(theChars[0]);
            return new string(theChars);
        }

        public static string TrimStart(this string target, string trimString)
        {
            if (string.IsNullOrEmpty(trimString)) return target;

            string result = target;
            while (result.StartsWith(trimString))
            {
                result = result.Substring(trimString.Length);
            }

            return result;
        }

        public static string TrimEnd(this string target, string trimString)
        {
            if (string.IsNullOrEmpty(trimString)) return target;

            string result = target;
            while (result.EndsWith(trimString))
            {
                result = result.Substring(0, result.Length - trimString.Length);
            }

            return result;
        }

        public static bool IsJson(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;

            source = source.Trim();

            if ((source.StartsWith("{") && source.EndsWith("}")) || // Object
                (source.StartsWith("[") && source.EndsWith("]")))   // Array
            {
                try
                {
                    Newtonsoft.Json.Linq.JToken.Parse(source);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public static void SetText(this TextBox textbox, string text, TextChangedEventHandler textChangedEvent)
        {
            textbox.TextChanged -= textChangedEvent;
            textbox.Text = text;
            textbox.TextChanged += textChangedEvent;
        }
        public static void SetSelectedIndex(this ComboBox combobox, int index)
        {
            combobox.GetType().GetProperty("SelectedIndex").SetValue(combobox, index, null);
        }

        public static void AddChangeIfExist(this HttpRequestHeaders requestHeaders, string name, string value)
        {
            if (requestHeaders == null)
            {
                return;
            }
            if (requestHeaders.Contains(name))
            {
                requestHeaders.Remove(name);
            }
            requestHeaders.Add(name, value);
        }

        public static string RemoveNewLineChar(this string theString)
        {
            if (string.IsNullOrWhiteSpace(theString))
            {
                return theString;
            }
            var sb = new System.Text.StringBuilder(theString.Length);
            foreach (char i in theString)
            {
                if (i != '\n' && i != '\r' && i != '\t')
                {
                    sb.Append(i);
                }
            }
            return sb.ToString();
        }

        public static double ScrollToElementVerticalOffset(this System.Windows.Controls.ScrollViewer scrollViewer, System.Windows.UIElement uIElement)
        {
            GeneralTransform groupBoxTransform = uIElement.TransformToAncestor(scrollViewer);
            Rect rectangle = groupBoxTransform.TransformBounds(new Rect(new Point(0, 0), uIElement.RenderSize));
            return rectangle.Top + scrollViewer.VerticalOffset;
        }
        public static void ScrollToElement(this System.Windows.Controls.ScrollViewer scrollViewer, System.Windows.UIElement uIElement)
        {

            scrollViewer.ScrollToVerticalOffset(scrollViewer.ScrollToElementVerticalOffset(uIElement));
            uIElement.Focus();
        }

        public static string GetName(this UIElement obj)
        {
            // First see if it is a FrameworkElement
            if (obj is FrameworkElement element)
            {
                return element.Name;
            }
            // If not, try reflection to get the value of a Name property.
            try
            {
                return (string)obj.GetType().GetProperty("Name").GetValue(obj, null);
            }
            catch
            {
                // Last of all, try reflection to get the value of a Name field.
                try
                {
                    return (string)obj.GetType().GetField("Name").GetValue(obj);
                }
                catch
                {
                    return null;
                }
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> enumerable, T element, IEqualityComparer<T> Comparer = null)
        {
            int i = 0;
            Comparer ??= EqualityComparer<T>.Default;
            foreach (var currentElement in enumerable)
            {
                if (Comparer.Equals(currentElement, element))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        public static IEnumerable<string> SelectedValuesAsString(this BlackPearl.Controls.CoreLibrary.MultiSelectCombobox MultiSelectCombobox, string DisplayMemberPath = null)
        {
            if (string.IsNullOrEmpty(DisplayMemberPath))
            {
                DisplayMemberPath = MultiSelectCombobox.DisplayMemberPath;
            }
            if (MultiSelectCombobox?.SelectedItems != null)
            {
                foreach (object ele in MultiSelectCombobox.SelectedItems)
                {
                    if (ele.GetType() == typeof(string))
                    {
                        yield return ele.ToString();
                    }
                    else
                    {
                        yield return ele.GetType().GetProperty(DisplayMemberPath)?.GetValue(ele)?.ToString()?.Trim();
                    }
                }
            }
        }

        public static IEnumerable<int> SelectedValuesAsInt(this BlackPearl.Controls.CoreLibrary.MultiSelectCombobox MultiSelectCombobox, string DisplayMemberPath = null)
        {
            if (string.IsNullOrEmpty(DisplayMemberPath))
            {
                DisplayMemberPath = MultiSelectCombobox.DisplayMemberPath;
            }
            if (MultiSelectCombobox?.SelectedItems != null)
            {
                foreach (object ele in MultiSelectCombobox.SelectedItems)
                {
                    if (ele.GetType() == typeof(string))
                    {
                        yield return (int)ele;
                    }
                    else
                    {
                        yield return (int)(ele.GetType().GetProperty(DisplayMemberPath).GetValue(ele));
                    }
                }
            }
        }

        public static bool ContainsOneOrMore(this IEnumerable<string> BaseArray, IEnumerable<string> SearchArray)
        {
            foreach (string SearchStr in SearchArray)
            {
                foreach (string BaseStr in BaseArray)
                {
                    if (BaseStr.Trim() == SearchStr.Trim())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool Contains(this string[] array, string value)
        {
            if (array == null)
            {
                return false;
            }
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Contains(this IEnumerable<HttpStatusCode> httpStatusCodes, HttpStatusCode statusCode)
        {
            return httpStatusCodes.Any(c => c == statusCode);
        }


        public static string Replace(this string Texte, IEnumerable<char> oldCharArray, string newString)
        {
            foreach (char SearchStr in oldCharArray)
            {
                Texte = Texte.Replace(SearchStr.ToString(), newString);
            }
            return Texte;
        }

        public static string ReplaceSingle(this string chaine, string oldValue, string newValue)
        {
            int index = chaine.IndexOf(oldValue);
            if (index != -1)
            {
                chaine = chaine.Remove(index, oldValue.Length).Insert(index, newValue);
            }
            return chaine;
        }

        public static string ReplaceLoop(this string Texte, string oldString, string newString)
        {
            while (Texte.Contains(oldString))
            {
                Texte = Texte.Replace(oldString, newString);
            }
            return Texte;
        }

        public static void DisposeItems<T>(this IEnumerable<T> collection) where T : IDisposable
        {
            foreach (T item in collection)
            {
                item.Dispose();
            }
        }
    }

    //https://stackoverflow.com/a/26575203/9947331 and https://stackoverflow.com/a/27013997/9947331
    //Usage : [UserFriendlyString( "I am before the enum element" ) ]
    public class UserFriendlyStringAttribute : Attribute
    {
        public string UserFriendlyString;
        public UserFriendlyStringAttribute(string value)
        {
            UserFriendlyString = value;
        }
    }

    public static class EnumExtender
    {
        public static string GetUserFriendlyString(this Enum enumeration)
        {
            var memberInfo = enumeration.GetType().GetMember(enumeration.ToString());
            if (memberInfo.Length <= 0) return enumeration.ToString();

            var attributes = memberInfo[0].GetCustomAttributes(typeof(UserFriendlyStringAttribute), false);
            return attributes.Length > 0 ? ((UserFriendlyStringAttribute)attributes[0]).UserFriendlyString : enumeration.ToString();
        }
    }


}
