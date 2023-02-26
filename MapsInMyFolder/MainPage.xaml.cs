using MapsInMyFolder.Commun;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Les champs non constants ne doivent pas être visibles", Justification = "for access everywhere")]
        public static MainPage _instance;
        public MainPage()
        {
            _instance = this;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isInitialised)
            {
                Preload();
                Init();
            }
        }

        bool isInitialised = false;
        public void Preload()
        {
            ReloadPage();
            MapLoad();
            DrawRectangleCelectionArroundPushpin();
            UpdatePushpinPositionAndDrawRectangle();
        }
        void Init()
        {
            Init_download_panel();
            Init_layer_panel();
            isInitialised = true;

            Notification.UpdateNotification += (Notification, NotificationId) => UpdateNotification((Notification)Notification, NotificationId);
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
        }

        private void Download_panel_close_button_Click(object sender, RoutedEventArgs e)
        {
            Download_panel_close();
        }

        private void Layer_searchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (layer_searchbar.Text == "Rechercher un calque, un site...")
            {
                layer_searchbar.Text = "";
            }
        }

        private void Layer_searchbar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    last_search = "";
                    SearchLayerStart();
                    layer_browser.Focus();
                }
                catch { }
            }
            if (e.Key == Key.Escape)
            {
                layer_browser.Focus();
            }
        }

        private void Layer_searchbar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(layer_searchbar.Text))
            {
                layer_searchbar.Text = "Rechercher un calque, un site...";
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            }
            else
            {
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }
        }

        private void Layer_searchbar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SearchLayerStart();
        }

        //public Grid CreateNotification(string Title, string Information, Action callback = null, string NotificationInternalName = null)
        //{
        //<Grid Background="#303031" MaxHeight="400">
        //    <Border BorderThickness="3" BorderBrush="Transparent" Margin="5,2,25,5" Grid.Column="0">
        //        <TextBlock TextWrapping="WrapWithOverflow" Foreground="#FFE2E2E1"  TextAlignment="Justify">
        //                    <Span>MapsInMyFolder</Span>
        //                    <Span>&#160;:&#160;</Span>
        //                    <!--<Span FontWeight="Light">Une nouvelle version est disponible. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.</Span>-->
        //                    <Span FontWeight="Light">Une nouvelle version est disponible. </Span>
        //        </TextBlock>
        //    </Border>
        //    <Button  Style="{DynamicResource IconButton}" HorizontalAlignment="Right" Height="10" Width="10" Foreground="#FFE2E2E1" Cursor="Hand" ToolTip="Fermer cette notification" Margin="5,10,5,0" VerticalAlignment="Top">
        //        <Path StrokeStartLineCap="round" StrokeEndLineCap="round" StrokeLineJoin="round" StrokeThickness="0.8" Data="{StaticResource CloseButton}" Stroke="{Binding Foreground, RelativeSource={RelativeSource Mode=FindAncestor, AncestorType=Button}}" Stretch="Uniform" Height="10" Width="10"/>
        //    </Button>
        //    <Rectangle Fill="#62626b" Height="1" VerticalAlignment="Bottom"></Rectangle>
        //</Grid>


        //    Grid ContentGrid = new Grid()
        //    {
        //        Background = Collectif.HexValueToSolidColorBrush("#303031"),
        //        MaxHeight = 400
        //    };
        //    ContentGrid.RowDefinitions.Add(new RowDefinition());
        //    ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
        //    if (!string.IsNullOrEmpty(NotificationInternalName))
        //    {
        //        ContentGrid.Name = NotificationInternalName;
        //    }


        //    Border ContentBorder = new Border()
        //    {
        //        BorderThickness = new Thickness(3),
        //        BorderBrush = System.Windows.Media.Brushes.Transparent,
        //        Margin = new Thickness(5, 2, 20, 5),
        //    };
        //    ContentBorder.SetValue(Grid.RowProperty, 0);

        //    TextBlock ContentTextBlock = new TextBlock()
        //    {
        //        TextWrapping = TextWrapping.WrapWithOverflow,
        //        Foreground = Collectif.HexValueToSolidColorBrush("#FFE2E2E1"),
        //        TextAlignment = TextAlignment.Justify
        //    };

        //    if (callback != null)
        //    {
        //        ContentTextBlock.Cursor = Cursors.Hand;
        //        ContentGrid.MouseLeftButtonUp += (_, _) =>
        //        {
        //            CloseNotification(ContentGrid, null);
        //            callback();
        //        };
        //    }

        //    ContentTextBlock.Inlines.Add(new System.Windows.Documents.Run()
        //    {
        //        Text = Title
        //    });
        //    ContentTextBlock.Inlines.Add(new System.Windows.Documents.Run()
        //    {
        //        Text = "\u00A0:\u00A0"
        //    });
        //    ContentTextBlock.Inlines.Add(new System.Windows.Documents.Run()
        //    {
        //        Text = Information,
        //        FontWeight = FontWeights.Light,
        //    });
        //    ContentBorder.Child = ContentTextBlock;

        //    Button CloseButton = new Button()
        //    {
        //        Style = (Style)Application.Current.Resources["IconButton"],
        //        Height = 26,
        //        Width = 25,
        //        Foreground = Collectif.HexValueToSolidColorBrush("#FFE2E2E1"),
        //        Cursor = Cursors.Hand,
        //        ToolTip = "Fermer cette notification",
        //        Margin = new Thickness(0, 0, 0, 0),
        //        VerticalAlignment = VerticalAlignment.Top,
        //        HorizontalAlignment = HorizontalAlignment.Right
        //    };

        //    CloseButton.Content = new System.Windows.Shapes.Path()
        //    {
        //        Margin = new Thickness(0, 4, 0, 0),
        //        StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,
        //        StrokeEndLineCap = System.Windows.Media.PenLineCap.Round,
        //        StrokeLineJoin = System.Windows.Media.PenLineJoin.Round,
        //        StrokeThickness = 0.8,
        //        Data = (System.Windows.Media.Geometry)Application.Current.Resources["CloseButton"],
        //        Stroke = Collectif.HexValueToSolidColorBrush("#FFE2E2E1"),
        //        Stretch = System.Windows.Media.Stretch.Uniform,
        //        Height = 10,
        //        Width = 10
        //    };
        //    CloseButton.Click += (_, e) =>
        //    {
        //        CloseNotification(ContentGrid, e);
        //        CloseButton.IsEnabled = false;
        //    };

        //    System.Windows.Shapes.Rectangle BorderBottom = new System.Windows.Shapes.Rectangle()
        //    {
        //        Fill = Collectif.HexValueToSolidColorBrush("#62626b"),
        //        Height = 1,
        //        VerticalAlignment = VerticalAlignment.Bottom
        //    };
        //    BorderBottom.PreviewMouseUp += (_, e) => e.Handled = true;

        //    ContentGrid.Children.Add(ContentBorder);
        //    ContentGrid.Children.Add(CloseButton);
        //    ContentGrid.Children.Add(BorderBottom);

        //    ContentGrid.Opacity = 0;
        //    var doubleAnimation = new DoubleAnimation(0, 1, Settings.animations_duration);
        //    ContentGrid.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);

        //    if (Collectif.FindChild<Grid>(NotificationZone, NotificationInternalName) != null)
        //    {
        //        NotificationZone.Children.Remove(Collectif.FindChild<Grid>(NotificationZone, NotificationInternalName));
        //    }
        //    NotificationZone.Children.Add(ContentGrid);

        //    return ContentGrid;
        //}

        //public void CloseNotification(object sender, RoutedEventArgs e)
        //{
        //    if (e is not null)
        //    {
        //        e.Handled = true;
        //    }
        //    Grid ContentGrid = (Grid)sender;
        //    var doubleAnimation = new DoubleAnimation(ContentGrid.ActualHeight, 0, new Duration(TimeSpan.FromSeconds(0.1)));
        //    ContentGrid.BeginAnimation(Grid.MaxHeightProperty, doubleAnimation);
        //}

        public void UpdateNotification(Notification sender, string NotificationInternalId)
        {
            if (sender is not null)
            {
                Grid ContentGrid = sender.Get();
                if (Collectif.FindChild<Grid>(NotificationZone, NotificationInternalId) != null)
                {
                   Grid NotificationZoneContentGrid = Collectif.FindChild<Grid>(NotificationZone, NotificationInternalId);
                    NotificationZone.Children.Remove(NotificationZoneContentGrid);
                    NotificationZone.Children.Insert(Math.Min(sender.InsertPosition, NotificationZone.Children.Count), ContentGrid);
                    NotificationZoneContentGrid.Children.Clear();
                    NotificationZoneContentGrid = null;
                }
                else
                {
                    ContentGrid.Opacity = 0;
                    var doubleAnimation = new DoubleAnimation(0, 1, Settings.animations_duration);
                    ContentGrid.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
                    sender.InsertPosition = NotificationZone.Children.Add(ContentGrid);
                }
            }
            else
            {

                if (Collectif.FindChild<Grid>(NotificationZone, NotificationInternalId) != null)
                {
                    Grid ContentGrid = Collectif.FindChild<Grid>(NotificationZone, NotificationInternalId);
                    var doubleAnimation = new DoubleAnimation(ContentGrid.ActualHeight, 0, new Duration(TimeSpan.FromSeconds(0.1)));
                    doubleAnimation.Completed += (sender, e) =>
                    {
                        ContentGrid?.Children?.Clear();
                        ContentGrid = null;
                        NotificationZone.Children.Remove(ContentGrid);
                    };
                    ContentGrid.BeginAnimation(Grid.MaxHeightProperty, doubleAnimation);
                }


            }
        }

        //public void CloseNotification(object sender, RoutedEventArgs e)
        //{
        //    if (e is not null)
        //    {
        //        e.Handled = true;
        //    }
        //    Grid ContentGrid = (Grid)sender;
        //    var doubleAnimation = new DoubleAnimation(ContentGrid.ActualHeight, 0, new Duration(TimeSpan.FromSeconds(0.1)));
        //    ContentGrid.BeginAnimation(Grid.MaxHeightProperty, doubleAnimation);
        //}


    }
}
