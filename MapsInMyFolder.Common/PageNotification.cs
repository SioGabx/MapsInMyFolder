using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MapsInMyFolder.Commun
{
    public abstract partial class Notification
    {
        public int InsertPosition = 0;
        public static event EventHandler<(string NotificationId, string Destinateur)> UpdateNotification;
        public static List<Notification> ListOfNotificationsOnShow = new List<Notification>();
        public string NotificationId = "Notif" + DateTime.Now.Ticks.ToString();
        public string Destinateur = null;
        public bool DisappearAfterAMoment = false;
        public bool IsPersistant = false;
        protected string Information = "";
        protected string Title = "";
        protected Action OnClickCallback = null;

        public Notification(string Information, string Title, string Destinateur, Action callback = null)
        {
            this.Information = Information;
            this.Title = Title;
            this.OnClickCallback = callback;
            this.Destinateur = Destinateur;
        }

        public virtual void Text(string Information = null, string Title = null)
        {
            if (!string.IsNullOrEmpty(Information))
            {
                this.Information = Information;
            }
            if (!string.IsNullOrEmpty(Title))
            {

                this.Title = Title;
            }
        }

        public void SendUpdate()
        {
            if (ListOfNotificationsOnShow.Contains(this))
            {
                UpdateNotification?.Invoke(this, (NotificationId, Destinateur));
            }
        }

        public bool Register()
        {
            if (ListOfNotificationsOnShow.Contains(this))
            {
                return false;
            }
            ListOfNotificationsOnShow.Add(this);
            SendUpdate();
            if (DisappearAfterAMoment)
            {
                Task.Delay(TimeSpan.FromSeconds(6)).ContinueWith(_ => Remove());
            }
            return true;
        }

        public void Remove()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ListOfNotificationsOnShow.Remove(this);
                UpdateNotification?.Invoke(null, (NotificationId, Destinateur));
            });
        }

        public static Notification GetById(string NotificationId)
        {
            return ListOfNotificationsOnShow.FirstOrDefault(notif => notif.NotificationId == NotificationId);
        }

        public static int Count()
        {
            return ListOfNotificationsOnShow.Count;
        }

        public virtual Grid Get()
        {
            Grid ContentGrid = Elements.ContentGrid(NotificationId, OnClickCallback, Remove);
            Border ContentBorder = Elements.ContentBorder();
            ContentBorder.Child = Elements.ContentTextBlock(Information, Title, OnClickCallback);
            ContentGrid.Children.Add(ContentBorder);
            ContentGrid.Children.Add(Elements.CloseButton(Remove));
            return ContentGrid;
        }
    }

    public abstract partial class Notification
    {
        public static class Elements
        {
            public static Grid ContentGrid(string NotificationId, Action OnClickCallback, Action CloseCallback)
            {
                Grid ContentGrid = new Grid()
                {
                    Background = Collectif.HexValueToSolidColorBrush("#303031"),
                    MaxHeight = 400
                };
                ContentGrid.RowDefinitions.Add(new RowDefinition());
                ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());

                ContentGrid.Name = NotificationId;

                if (OnClickCallback != null)
                {
                    ContentGrid.MouseLeftButtonUp += ContentGrid_MouseLeftButtonUp;

                    void ContentGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs o)
                    {
                        o.Handled = true;
                        CloseCallback();
                        OnClickCallback();
                        ContentGrid.MouseLeftButtonUp -= ContentGrid_MouseLeftButtonUp;
                    }
                }

                System.Windows.Shapes.Rectangle BorderBottom = new System.Windows.Shapes.Rectangle()
                {
                    Fill = Collectif.HexValueToSolidColorBrush("#62626b"),
                    Height = 1,
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                BorderBottom.PreviewMouseUp += (_, e) => e.Handled = true;
                ContentGrid.Children.Add(BorderBottom);
                return ContentGrid;
            }

            public static Border ContentBorder()
            {
                Border ContentBorder = new Border()
                {
                    BorderThickness = new Thickness(3),
                    BorderBrush = Brushes.Transparent,
                    Margin = new Thickness(5, 2, 20, 5),
                };
                ContentBorder.SetValue(Grid.RowProperty, 0);
                return ContentBorder;
            }

            public static TextBlock ContentTextBlockBase(string Content = null)
            {
                TextBlock ContentTextBlock = new TextBlock()
                {
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Foreground = Collectif.HexValueToSolidColorBrush("#FFE2E2E1"),
                    TextAlignment = TextAlignment.Justify
                };
                if (!string.IsNullOrEmpty(Content))
                {
                    ContentTextBlock.Text = Content;
                }
                return ContentTextBlock;
            }

            public static TextBlock ContentTextBlock(string Information, string Title, Action OnClickCallback)
            {
                TextBlock ContentTextBlock = ContentTextBlockBase();

                if (OnClickCallback != null)
                {
                    ContentTextBlock.Cursor = Cursors.Hand;
                }

                ContentTextBlock.Inlines.Add(new System.Windows.Documents.Run()
                {
                    Text = Title
                });
                ContentTextBlock.Inlines.Add(new System.Windows.Documents.Run()
                {
                    Text = "\u00A0:\u00A0"
                });
                ContentTextBlock.Inlines.Add(new System.Windows.Documents.Run()
                {
                    Text = Information,
                    FontWeight = FontWeights.Light,
                });
                return ContentTextBlock;
            }

            public static Button CloseButton(Action CloseCallback)
            {
                Button CloseButton = new Button()
                {
                    Style = (Style)Application.Current.Resources["IconButton"],
                    Height = 26,
                    Width = 25,
                    Foreground = Collectif.HexValueToSolidColorBrush("#FFE2E2E1"),
                    Cursor = Cursors.Hand,
                    ToolTip = Languages.Current["tooltipsCloseNotification"],
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                CloseButton.Content = CloseButtonPath();
                CloseButton.Click += CloseButton_Click;


                void CloseButton_Click(object sender, EventArgs o)
                {
                    CloseCallback();
                    CloseButton.IsEnabled = false;
                    CloseButton.Click -= CloseButton_Click;
                }

                return CloseButton;
            }

            public static System.Windows.Shapes.Path CloseButtonPath()
            {
                return new Path()
                {
                    Margin = new Thickness(0, 4, 0, 0),
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeThickness = 0.8,
                    Data = (Geometry)Application.Current.Resources["CloseButton"],
                    Stroke = Collectif.HexValueToSolidColorBrush("#FFE2E2E1"),
                    Stretch = Stretch.Uniform,
                    Height = 10,
                    Width = 10
                };
            }

            public static StackPanel ContentStackPanel()
            {
                return new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Background = Brushes.Transparent
                };
            }

            public static ModernWpf.Controls.ProgressBar ContentProgressBar(int ProgressValue = 0)
            {
                return new ModernWpf.Controls.ProgressBar()
                {
                    Value = ProgressValue,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Maximum = 1000,
                    Minimum = 0,
                    Margin = new Thickness(0, 5, 5, 0),
                    Foreground = Collectif.HexValueToSolidColorBrush("#44ac34")
                };
            }

        }
    }

    public class NText : Notification
    {
        public NText(string Information, string Title, string Destinateur, Action callback = null) : base(Information, Title, Destinateur, callback)
        {
        }

    }

    public class NProgress : Notification
    {
        double Progress = 0;
        bool CanBeClosed = true;
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
