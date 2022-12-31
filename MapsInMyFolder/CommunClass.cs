using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Windows.Threading;
using System.Threading;
using ModernWpf.Controls;
using System.Data.SQLite;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using MapsInMyFolder.Commun;
using System.Reflection;
using System.Windows.Media;

namespace MapsInMyFolder
{
    //Some stuff for ModernWpf TitleBar
    public class PixelsToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new GridLength((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value - (double)151;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //    partial class MainWindow : Window
    //{

    //}

    public static class Message
    {
        public static ContentDialog SetContentDialog(object text, object caption = null, MessageDialogButton messageBoxButton = MessageDialogButton.OK, ContentDialog dialog_arg = null)
        {
            ContentDialog dialog = null;
            if (dialog_arg is null || dialog_arg == null)
            {
                //dialog = MainWindow._instance.Dialogue;
                Application.Current.ExecOnUiThread(() => dialog = MainWindow._instance.Dialogue);
            }
            else
            {
                dialog = dialog_arg;
            }
            Debug.WriteLine("DialogMsg" + text);
            dialog.Content = text;
            dialog.Title = caption ?? "";
            dialog.CloseButtonText = "";
            dialog.SecondaryButtonText = "";
            dialog.PrimaryButtonText = "";
            dialog.IsPrimaryButtonEnabled = false;
            dialog.IsSecondaryButtonEnabled = false;

            switch (messageBoxButton)
            {
                case MessageDialogButton.OK:
                    ShowButtonPrimary("Ok");
                    break;
                case MessageDialogButton.OKCancel:
                    ShowButtonPrimary("Oui");
                    ShowButtonCancel("Annuler");
                    break;
                case MessageDialogButton.YesNo:
                    ShowButtonPrimary("Oui");
                    ShowButtonSecondary("Non");
                    break;
                case MessageDialogButton.YesNoCancel:
                    ShowButtonPrimary("Oui");
                    ShowButtonSecondary("Non");
                    ShowButtonCancel("Annuler");
                    break;
                case MessageDialogButton.YesCancel:
                    ShowButtonPrimary("Oui");
                    ShowButtonCancel("Annuler");
                    break;
            }

            void ShowButtonPrimary(string text)
            {
                dialog.PrimaryButtonText = text;
                dialog.IsPrimaryButtonEnabled = true;
            }
            void ShowButtonSecondary(string text)
            {
                dialog.SecondaryButtonText = text;
                dialog.IsSecondaryButtonEnabled = true;
            }
            void ShowButtonCancel(string text)
            {
                dialog.CloseButtonText = text;
            }
            return dialog;
        }

        public static async void NoReturnBoxAsync(object text, object caption = null, ContentDialog dialog_arg = null)
        {
            try
            {
                await Message.SetContentDialog(text, caption, MessageDialogButton.OK, dialog_arg).ShowAsync().ConfigureAwait(false);
            }
            catch (Exception ex) {
            Debug.WriteLine(ex.Message);
            }
        }
    }

    public enum MessageDialogButton
    {
        OK, OKCancel, YesNo, YesNoCancel, YesCancel
    }
}
