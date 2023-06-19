using System;
using System.Windows;
using System.Windows.Controls;

namespace MapsInMyFolder.Commun
{
    public static class MessageContentDialogHelpers
    {
        public static void FocusSenderOnLoad(TextBox textbox)
        {
            textbox.Loaded += MessageContentDialogHelpers.FocusSenderOnLoad;
            textbox.Unloaded += MessageContentDialogHelpers.RemoveFocusSenderOnUnLoad;

        }
        private static void FocusSenderOnLoad(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textbox)
            {
                textbox.Focus();
                //textbox.Loaded -= FocusSenderOnLoad;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(sender));
            }
        }

        private static void RemoveFocusSenderOnUnLoad(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textbox)
            {
                textbox.Loaded -= FocusSenderOnLoad;
                textbox.Unloaded -= RemoveFocusSenderOnUnLoad;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(sender));
            }
        }
    }
}