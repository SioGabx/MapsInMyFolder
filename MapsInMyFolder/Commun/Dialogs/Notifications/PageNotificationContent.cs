using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MapsInMyFolder.Commun
{
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
                    ContentGrid.MouseLeftButtonDown += ContentGrid_MouseLeftButtonDown;

                    void ContentGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs o)
                    {
                        o.Handled = true;
                        CloseCallback();
                        OnClickCallback();
                        ContentGrid.MouseLeftButtonDown -= ContentGrid_MouseLeftButtonDown;

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
                Button CloseButton = new Button
                {
                    Style = (Style)Application.Current.Resources["IconButton"],
                    Height = 26,
                    Width = 25,
                    Foreground = Collectif.HexValueToSolidColorBrush("#FFE2E2E1"),
                    Cursor = Cursors.Hand,
                    ToolTip = Languages.Current["tooltipsCloseNotification"],
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Content = CloseButtonPath()
                };
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
}
