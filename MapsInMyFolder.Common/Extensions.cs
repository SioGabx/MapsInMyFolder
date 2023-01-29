using System;
using System.Collections.Generic;
using System.Windows;
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


        public static void ScrollToElement(this System.Windows.Controls.ScrollViewer scrollViewer, System.Windows.UIElement uIElement)
        {
            GeneralTransform groupBoxTransform = uIElement.TransformToAncestor(scrollViewer);
            Rect rectangle = groupBoxTransform.TransformBounds(new Rect(new Point(0, 0), uIElement.RenderSize));
            scrollViewer.ScrollToVerticalOffset(rectangle.Top + scrollViewer.VerticalOffset);
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

    }

}
