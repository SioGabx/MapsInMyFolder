using MapsInMyFolder.Commun;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder
{

    public static class Message
    {

        public static ContentDialog SetContentDialog(object text, object caption = null, MessageDialogButton messageBoxButton = MessageDialogButton.OK)
        {

            ContentDialog dialog = new ContentDialog
            {
                Title = caption ?? "",
                Content = text,
                CloseButtonText = "",
                PrimaryButtonText = "",
                SecondaryButtonText = "",
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false,
                DefaultButton = ContentDialogButton.Primary,
                Background = Collectif.HexValueToSolidColorBrush("171719")
            };

            Debug.WriteLine("DialogMsg" + text);

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

        public static async void NoReturnBoxAsync(object text, object caption = null)
        {
            try
            {
                await Message.SetContentDialog(text, caption, MessageDialogButton.OK).ShowAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    public enum MessageDialogButton
    {
        OK, OKCancel, YesNo, YesNoCancel, YesCancel
    }
}
