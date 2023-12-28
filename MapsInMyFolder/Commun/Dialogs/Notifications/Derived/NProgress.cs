using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace MapsInMyFolder.Commun
{
    public class NProgress : Notification
    {
        double Progress = 0;
        readonly bool CanBeClosed = true;
        public NProgress(string Information, string Title, string Destinateur, Action callback = null, double Progress = 0, bool CanBeClosed = true) : base(Information, Title, Destinateur, callback)
        {
            this.Progress = Progress;
            this.CanBeClosed = CanBeClosed;
        }

        public override Grid Get()
        {
            Grid ContentGrid = Elements.ContentGrid(NotificationId, OnClickCallback, Remove);
            Border ContentBorder = Elements.ContentBorder();
            StackPanel ContentStackPanel = Elements.ContentStackPanel();
            ContentStackPanel.Children.Add(Elements.ContentTextBlock(Information, Title, OnClickCallback));
            ContentStackPanel.Children.Add(Elements.ContentProgressBar(Convert.ToInt32(Math.Ceiling(Progress * 10))));
            ContentBorder.Child = ContentStackPanel;
            ContentGrid.Children.Add(ContentBorder);
            if (CanBeClosed)
            {
                Button CloseButton = Elements.CloseButton(Remove);
                CloseButton.VerticalAlignment = VerticalAlignment.Center;
                Path CloseButtonPath = Elements.CloseButtonPath();
                CloseButtonPath.Margin = new Thickness(0, 0, 0, 0);
                CloseButton.Content = CloseButtonPath;
                ContentGrid.Children.Add(CloseButton);
            }
            return ContentGrid;
        }

        public void SetProgress(double Progress)
        {
            this.Progress = Progress;
            if (Progress >= 100)
            {
                Remove();
            }
            else
            {
                SendUpdate();
            }
        }
    }
}
