using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MapsInMyFolder
{
    public class SelectionRectangle
    {
        public MapSelectable mapSelectable;
        public MapPolygon Rectangle;
        public Border PropertiesDisplayElement;
        public TextBox NameTextBox;
        public TextBox StrokeThicknessTextBox;
        public TextBox ColorTextBox;
        public TextBox MinZoomTextBox;
        public TextBox MaxZoomTextBox;
        public TextBox NOLatitudeTextBox;
        public TextBox NOLongitudeTextBox;
        public TextBox SELatitudeTextBox;
        public TextBox SELongitudeTextBox;

        public event EventHandler<RoutedEventArgs> PropertiesDisplayElementGotFocus;
        //public event EventHandler<RoutedEventArgs> PropertiesDisplayElementLostFocus;

        public static List<SelectionRectangle> Rectangles = new List<SelectionRectangle>();

        public static SelectionRectangle GetSelectionRectangleFromRectangle(MapPolygon SearchedRectangle)
        {
            if (Rectangles.Count == 0)
            {
                return null;
            }
            return Rectangles?.Where(Rectangle => Rectangle.Rectangle == SearchedRectangle).FirstOrDefault();
        }


        public SelectionRectangle(MapPolygon Rectangle, string Nom, string MinZoom, string MaxZoom, string Color, string StrokeThickness)
        {
            this.Rectangle = Rectangle;
            CreateSelectionElement(Nom, MinZoom, MaxZoom, Color, StrokeThickness);
        }

        public void Focus(bool IsFocused = false)
        {
            if (IsFocused && (PropertiesDisplayElement.IsFocused || PropertiesDisplayElement.IsKeyboardFocusWithin))
            {
                PropertiesDisplayElement.BorderBrush = Collectif.HexValueToSolidColorBrush("#F18712");
            }
            else if (IsFocused)
            {
                PropertiesDisplayElement.BorderBrush = Collectif.HexValueToSolidColorBrush("#F18712");
                //PropertiesDisplayElement.Focus();
            }
            else
            {
                PropertiesDisplayElement.BorderBrush = Collectif.HexValueToSolidColorBrush("#6f6f7b");
            }
        }

        public Border CreateSelectionElement(string Nom, string MinZoom, string MaxZoom, string Color, string StrokeThickness)
        {
            Grid getGrid(bool AddCollums = true)
            {
                Grid ContentGrid = new Grid()
                {
                    Background = Collectif.HexValueToSolidColorBrush("#303031")
                };
                ContentGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(17)
                });
                ContentGrid.RowDefinitions.Add(new RowDefinition());
                if (AddCollums)
                {
                    ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    ContentGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        Width = new GridLength(10)
                    });
                    ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }
                return ContentGrid;
            }
            
            TextBox setSimpleColumTextBox(Grid grid, string LabelText, string TextBoxValue, string DefaultTextBoxValue)
            {
                Label label = new Label()
                {
                    Content = LabelText,
                };
                Grid.SetRow(label, 0);

                TextBox textbox = new TextBox();
                if (string.IsNullOrWhiteSpace(TextBoxValue))
                {
                    textbox.Text = DefaultTextBoxValue; 
                }
                else
                {
                    textbox.Text = TextBoxValue;
                }

                Grid.SetRow(textbox, 1);
                grid.Children.Add(label);
                grid.Children.Add(textbox);
                return textbox;
            }
            
            (TextBox LeftTextBox, TextBox RightTextBox) setDoubleColumnTextBox(Grid Grid, string LeftLabelText, string RightLabelText)
            {
                Grid.Margin = new Thickness(0, 20, 0, 0);
                Label Leftlabel = new Label()
                {
                    Content = LeftLabelText,
                };
                Grid.SetRow(Leftlabel, 0);
                Grid.SetColumn(Leftlabel, 0);
                TextBox LeftTextbox = new TextBox();
                Grid.SetRow(LeftTextbox, 1);
                Grid.SetColumn(LeftTextbox, 0);
                Grid.Children.Add(Leftlabel);
                Grid.Children.Add(LeftTextbox);
                Label Rightlabel = new Label()
                {
                    Content = RightLabelText,
                };
                Grid.SetRow(Rightlabel, 0);
                Grid.SetColumn(Rightlabel, 2);
                TextBox Rightextbox = new TextBox();
                Grid.SetRow(Rightextbox, 1);
                Grid.SetColumn(Rightextbox, 2);
                Grid.Children.Add(Rightlabel);
                Grid.Children.Add(Rightextbox);
                return (LeftTextbox, Rightextbox);
            }

            PropertiesDisplayElement = new Border()
            {
                BorderBrush = Collectif.HexValueToSolidColorBrush("#6f6f7b"),
                Background = Collectif.HexValueToSolidColorBrush("#303031"),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Margin = new Thickness(0, 0, 0, 10),
                Focusable = true
            };

            PropertiesDisplayElement.GotFocus += (o, e) =>
            {
                mapSelectable?.SetRectangleAsActive(Rectangle);
                PropertiesDisplayElementGotFocus?.Invoke(o, e);
            };
            PropertiesDisplayElement.IsKeyboardFocusWithinChanged += (o, e) =>
            {
                mapSelectable?.SetRectangleAsActive(Rectangle);
                PropertiesDisplayElementGotFocus?.Invoke(o, null);
            };

            StackPanel stackPanel = new StackPanel()
            {
                Margin = new Thickness(10, 10, 5, 20),
            };

            Grid ZoneNameGrid = getGrid(false);
            NameTextBox = setSimpleColumTextBox(ZoneNameGrid,"Nom :", Nom, "Selection sans nom");
            stackPanel.Children.Add(ZoneNameGrid);

            Grid ColorGrid = getGrid(false);
            ColorTextBox = setSimpleColumTextBox(ColorGrid, "Couleur (hex) :", Color, "#000000");
            stackPanel.Children.Add(ColorGrid);

            Grid StrokeThicknessGrid = getGrid(false);
            StrokeThicknessTextBox = setSimpleColumTextBox(StrokeThicknessGrid, "Epaisseur de ligne :", StrokeThickness, "1");
            stackPanel.Children.Add(StrokeThicknessGrid);

            Grid ZoomGrid = getGrid();
            var ZoomTextBox = setDoubleColumnTextBox(ZoomGrid, "Min Zoom :", "Max Zoom :");
            stackPanel.Children.Add(ZoomGrid);
            MinZoomTextBox = ZoomTextBox.LeftTextBox;
            MaxZoomTextBox = ZoomTextBox.RightTextBox;
            if (string.IsNullOrWhiteSpace(MinZoom))
            {
                MinZoomTextBox.Text = "∞";
            }
            else
            {
                MinZoomTextBox.Text = MinZoom;
            }

            if (string.IsNullOrWhiteSpace(MaxZoom))
            {
                MaxZoomTextBox.Text = "∞";
            }
            else
            {
                MaxZoomTextBox.Text = MaxZoom;
            }

            Grid NordOuestGrid = getGrid();
            var NordOuestTextBox = setDoubleColumnTextBox(NordOuestGrid, "Nord-Ouest Latitude :", "Nord-Ouest Longitude :");
            stackPanel.Children.Add(NordOuestGrid);
            NOLatitudeTextBox = NordOuestTextBox.LeftTextBox;
            NOLongitudeTextBox = NordOuestTextBox.RightTextBox;

            Grid SudEstGrid = getGrid();
            var SudEstTextBox = setDoubleColumnTextBox(SudEstGrid, "Sud-Est Latitude :", "Sud-Est Longitude :");
            stackPanel.Children.Add(SudEstGrid);
            PropertiesDisplayElement.Child = stackPanel;
            SELatitudeTextBox = SudEstTextBox.LeftTextBox;
            SELongitudeTextBox = SudEstTextBox.RightTextBox;

            var Locations = MapSelectable.GetRectangleLocationFromRectangle(Rectangle);
            NOLatitudeTextBox.Text = Locations.NO.Latitude.ToString();
            NOLongitudeTextBox.Text = Locations.NO.Longitude.ToString();
            SELatitudeTextBox.Text = Locations.SE.Latitude.ToString();
            SELongitudeTextBox.Text = Locations.SE.Longitude.ToString();

            NOLatitudeTextBox.TextChanged += UpdateLocation_NO;
            NOLongitudeTextBox.TextChanged += UpdateLocation_NO;
            SELatitudeTextBox.TextChanged += UpdateLocation_SE;
            SELongitudeTextBox.TextChanged += UpdateLocation_SE;
            return PropertiesDisplayElement;
        }

        public void UpdateLocation_NO(object o, TextChangedEventArgs e)
        {
            TextBox TextBoxSender = o as TextBox;
            Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBoxSender, new List<char>() { '.', '-' }, true, false);
            if (string.IsNullOrWhiteSpace(NOLatitudeTextBox.Text) || string.IsNullOrWhiteSpace(NOLongitudeTextBox.Text))
            {
                return;
            }
            if (!double.TryParse(NOLatitudeTextBox.Text, out var latitude)) return;
            if (!double.TryParse(NOLongitudeTextBox.Text, out var longitude)) return;
            int SelectStartAt = TextBoxSender.SelectionStart;
            if (longitude == 180)
            {
                longitude = 179.999999;
                TextBoxSender.Text = longitude.ToString();
                TextBoxSender.SelectionStart = SelectStartAt;
            }
            else if (longitude == -180)
            {
                longitude = -179.999999;
                TextBoxSender.Text = longitude.ToString(); TextBoxSender.SelectionStart = SelectStartAt;
            }
            mapSelectable.SetRectangleLocation(new Location(latitude, longitude), null, Rectangle);
        }

        public void UpdateLocation_SE(object o, TextChangedEventArgs e)
        {
            TextBox TextBoxSender = o as TextBox;
            Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBoxSender, new List<char>() { '.', '-' }, true, false);
            if (string.IsNullOrWhiteSpace(SELatitudeTextBox.Text) || string.IsNullOrWhiteSpace(SELongitudeTextBox.Text))
            {
                return;
            }
            if (!double.TryParse(SELatitudeTextBox.Text, out var latitude)) return;
            if (!double.TryParse(SELongitudeTextBox.Text, out var longitude)) return; int SelectStartAt = TextBoxSender.SelectionStart;
            if (longitude == 180)
            {
                longitude = 179.999999;
                TextBoxSender.Text = longitude.ToString(); TextBoxSender.SelectionStart = SelectStartAt;
            }
            else if (longitude == -180)
            {
                longitude = -179.999999;
                TextBoxSender.Text = longitude.ToString(); TextBoxSender.SelectionStart = SelectStartAt;
            }

            mapSelectable.SetRectangleLocation(null, new Location(latitude, longitude), Rectangle);

        }

    }


    public partial class FullscreenMap : Page
    {
        public static MapSelectable mapSelectable;
        public FullscreenMap()
        {
            InitializeComponent();
            mapSelectable = new MapSelectable(MapViewer, new Location(0, 0), new Location(0, 0), null, this) { RectangleCanBeDeleted = true };
            mapSelectable.RectangleGotFocus += MapSelectable_RectangleGotFocus;
            mapSelectable.RectangleLostFocus += MapSelectable_RectangleLostFocus;
            Notification.UpdateNotification += Notification_UpdateNotification;
        }

        private void Notification_UpdateNotification(object sender, (string NotificationId, string Destinateur) e)
        {
            UpdateNotification((Notification)sender, e);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (SelectionRectangle.Rectangles.Count == 0)
            {
                AddNewSelection(null);
            }
            else
            {
                foreach (var rectangle in SelectionRectangle.Rectangles)
                {
                    FocusRectangle(rectangle.Rectangle);
                    rectangle.PropertiesDisplayElement.Focus();
                }

            }

            mapSelectable.OnLocationUpdated += MapSelectable_OnLocationUpdated;
            mapSelectable.OnRectangleDeleted += MapSelectable_OnRectangleDeleted;
        }

        private void PageDispose()
        {
            mapSelectable.RectangleGotFocus -= MapSelectable_RectangleGotFocus;
            mapSelectable.RectangleLostFocus -= MapSelectable_RectangleLostFocus;
            Notification.UpdateNotification -= Notification_UpdateNotification;
            mapSelectable.OnLocationUpdated -= MapSelectable_OnLocationUpdated;
            mapSelectable.OnRectangleDeleted -= MapSelectable_OnRectangleDeleted;
        }


        private void MapSelectable_OnRectangleDeleted(object sender, MapPolygon e)
        {
            SelectionRectangle selectionRectangle = SelectionRectangle.GetSelectionRectangleFromRectangle(e);
            if (selectionRectangle == null)
            {
                return;
            }

            RectanglesStackPanel.Children.Remove(selectionRectangle.PropertiesDisplayElement);
            SelectionRectangle.Rectangles.Remove(selectionRectangle);
        }

        private void MapSelectable_OnLocationUpdated(object sender, MapPolygon e)
        {
            void SetValue(TextBox textbElement, string value, System.Windows.Controls.TextChangedEventHandler action)
            {
                if (textbElement.Text != value && !textbElement.IsKeyboardFocused)
                {
                    textbElement.TextChanged -= action;
                    textbElement.Text = value;
                    textbElement.TextChanged += action;
                }

            }

            var Locations = mapSelectable.GetRectangleLocation(e);
            SelectionRectangle selectionRectangle = SelectionRectangle.GetSelectionRectangleFromRectangle(e);
            if (selectionRectangle == null)
            {
                if (Locations.NO.Latitude != 0 && Locations.NO.Latitude != 0 && Locations.SE.Longitude != 0 && Locations.SE.Longitude != 0)
                {
                    MessageBox.Show("Added");
                    AddNewSelection(e);
                    selectionRectangle = SelectionRectangle.GetSelectionRectangleFromRectangle(e);
                }
                else
                {
                    return;
                }
            }

            SetValue(selectionRectangle.NOLatitudeTextBox, Math.Round(Locations.NO.Latitude, 6).ToString(), selectionRectangle.UpdateLocation_NO);
            SetValue(selectionRectangle.NOLongitudeTextBox, Math.Round(Locations.NO.Longitude, 6).ToString(), selectionRectangle.UpdateLocation_NO);
            SetValue(selectionRectangle.SELatitudeTextBox, Math.Round(Locations.SE.Latitude, 6).ToString(), selectionRectangle.UpdateLocation_SE);
            SetValue(selectionRectangle.SELongitudeTextBox, Math.Round(Locations.SE.Longitude, 6).ToString(), selectionRectangle.UpdateLocation_SE);
        }

        public void UpdateNotification(Notification sender, (string NotificationId, string Destinateur) NotificationInternalArgs)
        {
            if (sender is not null)
            {
                if (NotificationInternalArgs.Destinateur != "FullscreenMap")
                {
                    return;
                }
                Grid ContentGrid = sender.Get();
                if (Collectif.FindChild<Grid>(NotificationZone, NotificationInternalArgs.NotificationId) != null)
                {
                    Grid NotificationZoneContentGrid = Collectif.FindChild<Grid>(NotificationZone, NotificationInternalArgs.NotificationId);
                    NotificationZone.Children.Remove(NotificationZoneContentGrid);
                    NotificationZone.Children.Insert(Math.Min(sender.InsertPosition, NotificationZone.Children.Count), ContentGrid);
                    NotificationZoneContentGrid.Children.Clear();
                    NotificationZoneContentGrid = null;
                }
                else
                {
                    ContentGrid.Opacity = 0;
                    var doubleAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond));
                    ContentGrid.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
                    sender.InsertPosition = NotificationZone.Children.Add(ContentGrid);
                }
            }
            else if (Collectif.FindChild<Grid>(NotificationZone, NotificationInternalArgs.NotificationId) != null)
            {
                Grid ContentGrid = Collectif.FindChild<Grid>(NotificationZone, NotificationInternalArgs.NotificationId);
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

        private void MapSelectable_RectangleLostFocus(object sender, MapPolygon e)
        {
            SelectionRectangle.GetSelectionRectangleFromRectangle(e)?.Focus(false);
        }

        private void MapSelectable_RectangleGotFocus(object sender, MapPolygon e)
        {
            FocusRectangle(e);
        }


        public void FocusRectangle(MapPolygon e)
        {
            SelectionRectangle selectionRectangle = SelectionRectangle.GetSelectionRectangleFromRectangle(e);
            if (selectionRectangle == null)
            {
                return;
            }
            selectionRectangle?.Focus(true);
            selectionRectangle?.PropertiesDisplayElement.BringIntoView();
        }


        public void SetLayer()
        {
            Layers Layer = Layers.GetLayerById(Settings.layer_startup_id);
            MapViewer.MapLayer = new MapTileLayer
            {
                TileSource = new TileSource { UriFormat = "https://tile.openstreetmap.org/{z}/{x}/{y}.png", LayerID = Layer.class_id },
                SourceName = Layer.class_identifiant,
                MaxZoomLevel = Layer.class_max_zoom ?? 0,
                MinZoomLevel = Layer.class_min_zoom ?? 0,
                Description = ""
            };
        }

        private void AddRectangle_Click(object sender, RoutedEventArgs e)
        {
            AddNewSelection();
        }

        public void AddNewSelection(MapPolygon NewRectangle = null, string Nom = null, string MinZoom = null, string MaxZoom = null, string Color = null, string StrokeThickness = null)
        {
            int NumberOfElementInside = RectanglesStackPanel.Children.Count;
            int NumberOfUnusedRectangleDeleted = mapSelectable.DeleteUnusedRectangles();
            if (NumberOfUnusedRectangleDeleted > 0 && NumberOfElementInside != RectanglesStackPanel.Children.Count)
            {
                string infoText = "";
                if (NumberOfUnusedRectangleDeleted == 1)
                {
                    infoText += "Une sélection vide a été supprimée";
                }
                else
                {
                    infoText += $"{NumberOfElementInside - RectanglesStackPanel.Children.Count} sélections vides ont été supprimées";
                }
                Notification InfoUnusedRectangleDeleted = new NText(infoText, "MapsInMyFolder", "FullscreenMap")
                {
                    NotificationId = "InfoUnusedRectangleDeleted",
                    DisappearAfterAMoment = true,
                    IsPersistant = true,
                };
                InfoUnusedRectangleDeleted.Register();
            }

            if (NewRectangle == null)
            {
                NewRectangle = mapSelectable.AddRectangle(new Location(0, 0), new Location(0, 0));
            }
            SelectionRectangle selectionRectangle = new SelectionRectangle(NewRectangle, Nom, MinZoom, MaxZoom, Color, StrokeThickness)
            {
                mapSelectable = mapSelectable
            };
            SelectionRectangle.Rectangles.Add(selectionRectangle);
            selectionRectangle.Focus(true);
            RectanglesStackPanel.Children.Add(selectionRectangle.PropertiesDisplayElement);

           
            MapViewer.Focus();
            SelectionScrollViewer.ScrollToEnd();
        }

        private void ClosePage_button_Click(object sender, RoutedEventArgs e)
        {
            PageDispose();
            MainWindow._instance.FrameBack();
        }
    }
}
