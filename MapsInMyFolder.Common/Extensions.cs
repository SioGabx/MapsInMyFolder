using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MapsInMyFolder.Commun
{

    public static class Extensions
    {
        public static bool Contains(this String str, String substring, StringComparison comp)
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

        public static void SetText(this TextBox textbox, string text)
        {
            textbox.GetType().GetProperty("Text").SetValue(textbox, text, null);
        }
        public static void SetSelectedIndex(this ComboBox combobox, int index)
        {
            combobox.GetType().GetProperty("SelectedIndex").SetValue(combobox, index, null);
        }

        public static string RemoveNewLineChar(this string theString)
        {
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


        public static int IndexOf<T>(this IEnumerable<T> enumerable, T element, IEqualityComparer<T> comparer = null)
        {
            int i = 0;
            comparer = comparer ?? EqualityComparer<T>.Default;
            foreach (var currentElement in enumerable)
            {
                if (comparer.Equals(currentElement, element))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }


        public static IEnumerable<string> SelectedValues(this BlackPearl.Controls.CoreLibrary.MultiSelectCombobox MultiSelectCombobox, string DisplayMemberPath = null)
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
                        yield return ele.GetType().GetProperty(DisplayMemberPath).GetValue(ele).ToString().Trim();
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

        public static string Replace(this string Texte, IEnumerable<char> oldCharArray, string newString)
        {
            foreach (char SearchStr in oldCharArray)
            {
                Texte = Texte.Replace(SearchStr.ToString(), newString);
            }
            return Texte;
        }

        public static string ReplaceLoop(this string Texte, string oldString, string newString)
        {
            while (Texte.Contains(oldString))
            {
                Texte = Texte.Replace(oldString, newString);
            }
            return Texte;
        }



    }
}
