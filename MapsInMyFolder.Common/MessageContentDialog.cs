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
        public static async Task<ContentDialogResult> ShowContentDialogAsync(ContentDialog dialog)
        {
            try
            {
                return await dialog.ShowAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erreur affichage ContentDialog : " + ex.ToString());
                return ContentDialogResult.None;
            }
        }

        //public static string InputBoxDialog(object text, object caption = null, MessageDialogButton messageBoxButton = MessageDialogButton.OK, DispatcherFrame frame = null)
        //{
        //    ContentDialog dialog = SetContentDialog(text, caption, messageBoxButton);
        //    //alert("Veuillez indiquer l'adresse URL du panorama à télécharger :","Google Maps")
        //    StackPanel stackPanel = new StackPanel();
        //    TextBlock textBlock = new TextBlock();
        //    if (!string.IsNullOrEmpty(text?.ToString()))
        //    {
        //        textBlock.Text = text.ToString();
        //        textBlock.TextWrapping = TextWrapping.Wrap;
        //    }
        //    TextBox textBox = new TextBox() { };
        //    textBox.Style = (Style)Application.Current.Resources["TextBoxCleanStyle_13"];
        //    textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        //    textBox.Margin = new Thickness(0, 10, 0, 0);
        //    stackPanel.Children.Add(textBlock);
        //    stackPanel.Children.Add(textBox);
        //    dialog.Content = stackPanel;

        //    if (frame != null)
        //    {
        //        dialog.Closed += (_, __) =>
        //        {
        //            frame.Continue = false; // stops the frame
        //        };
        //    }

        //    dialog.ShowAsync();
        //    return textBox.Text;
        //}

        public static (TextBox textBox, ContentDialog dialog) SetInputBoxDialog(object text, object caption = null, MessageDialogButton messageBoxButton = MessageDialogButton.OK)
        {
            ContentDialog dialog = SetContentDialog(text, caption, messageBoxButton);
            StackPanel stackPanel = new StackPanel();
            TextBlock textBlock = new TextBlock();
            if (!string.IsNullOrEmpty(text?.ToString()))
            {
                textBlock.Text = text.ToString();
                textBlock.TextWrapping = TextWrapping.Wrap;
            }
            TextBox textBox = new TextBox
            {
                Style = (Style)Application.Current.Resources["TextBoxCleanStyle_13"],
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 10, 0, 0)
            };
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(textBox);
            dialog.Content = stackPanel;
            return (textBox, dialog);
        }

        public static ContentDialog SetContentDialog(object text, object caption = null, MessageDialogButton messageBoxButton = MessageDialogButton.OK, bool showTextbox = false)
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
 
                 if (showTextbox)
                 {
                    //alert("Veuillez indiquer l'adresse URL du panorama à télécharger :","Google Maps")
                    StackPanel stackPanel = new StackPanel();
                     TextBlock textBlock = new TextBlock();
                     if (!string.IsNullOrEmpty(text?.ToString()))
                     {
                         textBlock.Text = text.ToString();
                         textBlock.TextWrapping = TextWrapping.Wrap;
                     }
                     TextBox textBox = new TextBox
                     {
                         Style = (Style)Application.Current.Resources["TextBoxCleanStyle_13"],
                         HorizontalAlignment = HorizontalAlignment.Stretch,
                         Margin = new Thickness(0, 10, 0, 0)
                     };
                     stackPanel.Children.Add(textBlock);
                     stackPanel.Children.Add(textBox);
                     dialog.Content = stackPanel;
                 }


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
                     case MessageDialogButton.YesNoRetry:
                         ShowButtonPrimary("Oui");
                         ShowButtonSecondary("Non");
                         ShowButtonCancel("Réessayer");
                         break;
                     case MessageDialogButton.YesCancel:
                         ShowButtonPrimary("Oui");
                         ShowButtonCancel("Annuler");
                         break;
                     case MessageDialogButton.YesRetry:
                         ShowButtonPrimary("Oui");
                         ShowButtonCancel("Réessayer");
                         break;
                     case MessageDialogButton.RetryCancel:
                         ShowButtonCancel("Annuler");
                         ShowButtonPrimary("Réessayer");
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
