using ModernWpf.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MapsInMyFolder.Commun
{

    public static class Message
    {
        public static (TextBox textBox, ContentDialog dialog) SetInputBoxDialog(object text, object caption = null, MessageDialogButton messageBoxButton = MessageDialogButton.OK)
        {

            if (string.IsNullOrEmpty(text?.ToString()))
            {
                text = "";
            }
            
            if (string.IsNullOrEmpty(caption?.ToString()))
            {
                caption = "MapsInMyFolder";
            }

            TextBlock textBlock = new TextBlock
            {
                Text = text.ToString(),
                TextWrapping = TextWrapping.Wrap
            };

            TextBox textBox = new TextBox
            {
                Style = (Style)Application.Current.Resources["TextBoxCleanStyleDefault"],
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 10, 0, 0),
                        Height = 25,
            };
            MessageContentDialogHelpers.FocusSenderOnLoad(textBox);
            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(textBox);

            ContentDialog dialog = SetContentDialog(stackPanel, caption, messageBoxButton);
            return (textBox, dialog);
        }

        public static ContentDialog SetContentDialog(object text, object caption = null, MessageDialogButton messageBoxButton = MessageDialogButton.OK)
        {
            return Application.Current.Dispatcher.Invoke(delegate
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
                     Background = Collectif.HexValueToSolidColorBrush("#171719")
                 };

                 string dialogButtonOK = Languages.Current["dialogButtonOK"];
                 string dialogButtonCancel = Languages.Current["dialogButtonCancel"];
                 string dialogButtonYes = Languages.Current["dialogButtonYes"];
                 string dialogButtonNo = Languages.Current["dialogButtonNo"];
                 string dialogButtonRetry = Languages.Current["dialogButtonRetry"];

                 switch (messageBoxButton)
                 {
                     case MessageDialogButton.OK:
                         ShowButtonPrimary(dialogButtonOK);
                         break;
                     case MessageDialogButton.OKCancel:
                         ShowButtonPrimary(dialogButtonOK);
                         ShowButtonCancel(dialogButtonCancel);
                         break;
                     case MessageDialogButton.YesNo:
                         ShowButtonPrimary(dialogButtonYes);
                         ShowButtonSecondary(dialogButtonNo);
                         break;
                     case MessageDialogButton.YesNoCancel:
                         ShowButtonPrimary(dialogButtonYes);
                         ShowButtonSecondary(dialogButtonNo);
                         ShowButtonCancel(dialogButtonCancel);
                         break;
                     case MessageDialogButton.YesNoRetry:
                         ShowButtonPrimary(dialogButtonYes);
                         ShowButtonSecondary(dialogButtonNo);
                         ShowButtonCancel(dialogButtonRetry);
                         break;
                     case MessageDialogButton.YesCancel:
                         ShowButtonPrimary(dialogButtonYes);
                         ShowButtonCancel(dialogButtonCancel);
                         break;
                     case MessageDialogButton.YesRetry:
                         ShowButtonPrimary(dialogButtonYes);
                         ShowButtonCancel(dialogButtonRetry);
                         break;
                     case MessageDialogButton.RetryCancel:
                         ShowButtonCancel(dialogButtonCancel);
                         ShowButtonPrimary(dialogButtonRetry);
                         break;
                 }

                 void ShowButtonPrimary(string text_args)
                 {
                     dialog.PrimaryButtonText = text_args;
                     dialog.IsPrimaryButtonEnabled = true;
                 }
                 void ShowButtonSecondary(string text_args)
                 {
                     dialog.SecondaryButtonText = text_args;
                     dialog.IsSecondaryButtonEnabled = true;
                 }
                 void ShowButtonCancel(string text_args)
                 {
                     dialog.CloseButtonText = text_args;
                 }
                 return dialog;
             });
        }

        public static async void NoReturnBoxAsync(object text, object caption = null)
        {
            try
            {
                await SetContentDialog(text, caption, MessageDialogButton.OK).ShowAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    public enum MessageDialogButton
    {
        OK, OKCancel, YesNo, YesNoCancel, YesCancel, YesNoRetry, YesRetry, RetryCancel
    }
}
