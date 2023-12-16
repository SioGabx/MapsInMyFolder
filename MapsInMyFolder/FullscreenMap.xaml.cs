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
        public MapSelectable MapSelectable { get; set; }
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

        public static List<SelectionRectangle> Rectangles { get; set; } = new List<SelectionRectangle>();

        public static SelectionRectangle GetSelectionRectangleFromRectangle(MapPolygon searchedRectangle)
        {
            return Rectangles?.FirstOrDefault(rectangle => rectangle.Rectangle == searchedRectangle);
        }

        public SelectionRectangle(MapPolygon rectangle, string name, string minZoom, string maxZoom, string color, string strokeThickness)
        {
            Rectangle = rectangle;
            CreateSelectionElement(name, minZoom, maxZoom, color, strokeThickness);
        }

        public void Focus(bool isFocused = false)
        {
            if (isFocused && (PropertiesDisplayElement.IsFocused || PropertiesDisplayElement.IsKeyboardFocusWithin))
            {
                PropertiesDisplayElement.BorderBrush = Collectif.HexValueToSolidColorBrush("#F18712");
            }
            else if (isFocused)
            {
                PropertiesDisplayElement.BorderBrush = Collectif.HexValueToSolidColorBrush("#F18712");
            }
            else
            {
                PropertiesDisplayElement.BorderBrush = Collectif.HexValueToSolidColorBrush("#6f6f7b");
            }
        }

        public Border CreateSelectionElement(string name, string minZoom, string maxZoom, string color, string strokeThickness)
        {
            Grid getGrid(bool addColumns = true)
            {
                Grid contentGrid = new Grid()
                {
                    Background = Collectif.HexValueToSolidColorBrush("#303031")
                };
                contentGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(17)
                });
                contentGrid.RowDefinitions.Add(new RowDefinition());
                if (addColumns)
                {
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        Width = new GridLength(10)
                    });
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }
                return contentGrid;
            }

            TextBox setSimpleColumnTextBox(Grid grid, string labelText, string textBoxValue, string defaultTextBoxValue)
            {
                Label label = new Label()
                {
                    Content = labelText,
                };
                Grid.SetRow(label, 0);

                TextBox textbox = new TextBox();
                if (string.IsNullOrWhiteSpace(textBoxValue))
                {
                    textbox.Text = defaultTextBoxValue;
                }
                else
                {
                    textbox.Text = textBoxValue;
                }

                Grid.SetRow(textbox, 1);
                grid.Children.Add(label);
                grid.Children.Add(textbox);
                return textbox;
            }

            (TextBox LeftTextBox, TextBox RightTextBox) setDoubleColumnTextBox(Grid grid, string leftLabelText, string rightLabelText)
            {
                grid.Margin = new Thickness(0, 20, 0, 0);
                TextBox GetTextBox(object content, int column)
                {
                    Label label = new Label()
                    {
                        Content = content,
                    };
                    Grid.SetRow(label, 0);
                    Grid.SetColumn(label, column);
                    TextBox textbox = new TextBox();
                    Grid.SetRow(textbox, 1);
                    Grid.SetColumn(textbox, column);
                    grid.Children.Add(label);
                    grid.Children.Add(textbox);
                    return textbox;
                }

                TextBox leftTextbox = GetTextBox(leftLabelText, 0);
                TextBox rightTextbox = GetTextBox(rightLabelText, 2);
                return (leftTextbox, rightTextbox);
            }

            PropertiesDisplayElement = new Border()
            {
                BorderBrush = Collectif.HexValueToSolidColorBrush("#6f6f7b"),
                Background = Collectif.HexValueToSolidColorBrush("#303031"),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Margin = new Thickness(0, 0, 0, 10),
                Focusable = true
            };

            PropertiesDisplayElement.GotFocus += PropertiesDisplayElement_GotFocus;
            PropertiesDisplayElement.IsKeyboardFocusWithinChanged += PropertiesDisplayElement_IsKeyboardFocusWithinChanged;
            PropertiesDisplayElement.Unloaded += PropertiesDisplayElement_Unloaded;

            StackPanel stackPanel = new StackPanel()
            {
                Margin = new Thickness(10, 10, 5, 20),
            };

            Grid zoneNameGrid = getGrid(false);
            NameTextBox = setSimpleColumnTextBox(zoneNameGrid, Languages.Current["editorSelectionsPropertyNameName"], name, Languages.Current["editorSelectionsPropertyDefaultValueName"]);
            stackPanel.Children.Add(zoneNameGrid);

            Grid colorGrid = getGrid(false);
            ColorTextBox = setSimpleColumnTextBox(colorGrid, Languages.Current["editorSelectionsPropertyNameColor"], color, "#000000");
            stackPanel.Children.Add(colorGrid);

            Grid strokeThicknessGrid = getGrid(false);
            StrokeThicknessTextBox = setSimpleColumnTextBox(strokeThicknessGrid, Languages.Current["editorSelectionsPropertyNameStrokeThickness"], strokeThickness, "5");
            stackPanel.Children.Add(strokeThicknessGrid);

            Grid zoomGrid = getGrid();
            var zoomTextBox = setDoubleColumnTextBox(zoomGrid, Languages.Current["editorSelectionsPropertyNameMinZoom"], Languages.Current["editorSelectionsPropertyNameMaxZoom"]);
            stackPanel.Children.Add(zoomGrid);
            MinZoomTextBox = zoomTextBox.LeftTextBox;
            MaxZoomTextBox = zoomTextBox.RightTextBox;
            MinZoomTextBox.Text = getTextIfInfinity(minZoom);
            MaxZoomTextBox.Text = getTextIfInfinity(maxZoom);

            Grid nordOuestGrid = getGrid();
            var nordOuestTextBox = setDoubleColumnTextBox(nordOuestGrid, Languages.Current["editorSelectionsPropertyNameNorthwestLatitude"], Languages.Current["editorSelectionsPropertyNameNorthwestLongitude"]);
            stackPanel.Children.Add(nordOuestGrid);
            NOLatitudeTextBox = nordOuestTextBox.LeftTextBox;
            NOLongitudeTextBox = nordOuestTextBox.RightTextBox;

            Grid sudEstGrid = getGrid();
            var sudEstTextBox = setDoubleColumnTextBox(sudEstGrid, Languages.Current["editorSelectionsPropertyNameSoutheastLatitude"], Languages.Current["editorSelectionsPropertyNameSoutheastLongitude"]);
            stackPanel.Children.Add(sudEstGrid);
            PropertiesDisplayElement.Child = stackPanel;
            SELatitudeTextBox = sudEstTextBox.LeftTextBox;
            SELongitudeTextBox = sudEstTextBox.RightTextBox;

            var locations = MapSelectable.GetRectangleLocationFromRectangle(Rectangle);
            NOLatitudeTextBox.Text = locations.NO.Latitude.ToString();
            NOLongitudeTextBox.Text = locations.NO.Longitude.ToString();
            SELatitudeTextBox.Text = locations.SE.Latitude.ToString();
            SELongitudeTextBox.Text = locations.SE.Longitude.ToString();

            NOLatitudeTextBox.TextChanged += UpdateLocation_NO;
            NOLongitudeTextBox.TextChanged += UpdateLocation_NO;
            SELatitudeTextBox.TextChanged += UpdateLocation_SE;
            SELongitudeTextBox.TextChanged += UpdateLocation_SE;

            MinZoomTextBox.TextChanged += FilterZoomOnTextChanged;
            MaxZoomTextBox.TextChanged += FilterZoomOnTextChanged;
            StrokeThicknessTextBox.TextChanged += FilterStrokeThicknessOnTextChanged;

            return PropertiesDisplayElement;

            string getTextIfInfinity(string texteValue)
            {
                if (string.IsNullOrWhiteSpace(texteValue) || texteValue == "-1" || string.Equals(texteValue, "infinity", StringComparison.InvariantCultureIgnoreCase))
                {
                    return "∞";
                }
                else if (int.TryParse(texteValue, out int _))
                {
                    return texteValue;
                }
                else
                {
                    return "∞";
                }
            }
        }

        public void FilterZoomOnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBoxSender = sender as TextBox;
            Collectif.FilterDigitOnlyWhileWritingInTextBox(textBoxSender, new List<char>() { '-', '∞' }, true, false);
            if (textBoxSender.Text.Trim() == "-1")
            {
                textBoxSender.Text = "∞";
            }
        }

        public void FilterStrokeThicknessOnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBoxSender = sender as TextBox;
            Collectif.FilterDigitOnlyWhileWritingInTextBox(textBoxSender, new List<char>() { '.' }, true, false);
        }

        private void PropertiesDisplayElement_Unloaded(object sender, RoutedEventArgs e)
        {
            PropertiesDisplayElement.GotFocus -= PropertiesDisplayElement_GotFocus;
            PropertiesDisplayElement.IsKeyboardFocusWithinChanged -= PropertiesDisplayElement_IsKeyboardFocusWithinChanged;
            PropertiesDisplayElement.Unloaded -= PropertiesDisplayElement_Unloaded;
            NOLatitudeTextBox.TextChanged -= UpdateLocation_NO;
            NOLongitudeTextBox.TextChanged -= UpdateLocation_NO;
            SELatitudeTextBox.TextChanged -= UpdateLocation_SE;
            SELongitudeTextBox.TextChanged -= UpdateLocation_SE;

            MinZoomTextBox.TextChanged -= FilterZoomOnTextChanged;
            MaxZoomTextBox.TextChanged -= FilterZoomOnTextChanged;
            StrokeThicknessTextBox.TextChanged -= FilterStrokeThicknessOnTextChanged;
        }

        private void PropertiesDisplayElement_GotFocus(object sender, RoutedEventArgs e)
        {
            MapSelectable?.SetRectangleAsActive(Rectangle);
            PropertiesDisplayElementGotFocus?.Invoke(sender, e);
        }

        private void PropertiesDisplayElement_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MapSelectable?.SetRectangleAsActive(Rectangle);
            PropertiesDisplayElementGotFocus?.Invoke(sender, null);
        }

        public void UpdateLocation_NO(object sender, TextChangedEventArgs e)
        {
            TextBox textBoxSender = sender as TextBox;
            Collectif.FilterDigitOnlyWhileWritingInTextBox(textBoxSender, new List<char>() { '.', '-' }, true, false);
            if (string.IsNullOrWhiteSpace(NOLatitudeTextBox.Text) || string.IsNullOrWhiteSpace(NOLongitudeTextBox.Text))
            {
                return;
            }
            if (!double.TryParse(NOLatitudeTextBox.Text, out var latitude)) return;
            if (!double.TryParse(NOLongitudeTextBox.Text, out var longitude)) return;
            int selectStartAt = textBoxSender.SelectionStart;
            if (longitude == 180)
            {
                longitude = 179.999999;
                textBoxSender.Text = longitude.ToString();
                textBoxSender.SelectionStart = selectStartAt;
            }
            else if (longitude == -180)
            {
                longitude = -179.999999;
                textBoxSender.Text = longitude.ToString();
                textBoxSender.SelectionStart = selectStartAt;
            }
            MapSelectable.SetRectangleLocation(new Location(latitude, longitude), null, Rectangle);
        }

        public void UpdateLocation_SE(object sender, TextChangedEventArgs e)
        {
            TextBox textBoxSender = sender as TextBox;
            Collectif.FilterDigitOnlyWhileWritingInTextBox(textBoxSender, new List<char>() { '.', '-' }, true, false);
            if (string.IsNullOrWhiteSpace(SELatitudeTextBox.Text) || string.IsNullOrWhiteSpace(SELongitudeTextBox.Text))
            {
                return;
            }
            if (!double.TryParse(SELatitudeTextBox.Text, out var latitude)) return;
            if (!double.TryParse(SELongitudeTextBox.Text, out var longitude)) return;
            int selectStartAt = textBoxSender.SelectionStart;
            if (longitude == 180)
            {
                longitude = 179.999999;
                textBoxSender.Text = longitude.ToString();
                textBoxSender.SelectionStart = selectStartAt;
            }
            else if (longitude == -180)
            {
                longitude = -179.999999;
                textBoxSender.Text = longitude.ToString();
                textBoxSender.SelectionStart = selectStartAt;
            }

            MapSelectable.SetRectangleLocation(null, new Location(latitude, longitude), Rectangle);
        }
    }

    public partial class FullscreenRectanglesMap : Page
    {
        public MapSelectable mapSelectable;

        public FullscreenRectanglesMap()
        {
            InitializeComponent();
            mapSelectable = new MapSelectable(MapViewer, new Location(0, 0), new Location(0, 0), this) { RectangleCanBeDeleted = true };
            mapSelectable.RectangleGotFocus += MapSelectable_RectangleGotFocus;
            mapSelectable.RectangleLostFocus += MapSelectable_RectangleLostFocus;
            Notification.UpdateNotification += Notification_UpdateNotification;
            mapSelectable.OnLocationUpdated += MapSelectable_OnLocationUpdated;
            mapSelectable.OnRectangleDeleted += MapSelectable_OnRectangleDeleted;
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
            static void SetValue(TextBox textBoxElement, string value, System.Windows.Controls.TextChangedEventHandler action)
            {
                if (textBoxElement.Text != value && !textBoxElement.IsKeyboardFocused)
                {
                    textBoxElement.TextChanged -= action;
                    textBoxElement.Text = value;
                    textBoxElement.TextChanged += action;
                }
            }

            var Locations = mapSelectable.GetRectangleLocation(e);
            SelectionRectangle selectionRectangle = SelectionRectangle.GetSelectionRectangleFromRectangle(e);
            if (selectionRectangle == null)
            {
                return;
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
                if (Collectif.FindChildByName<Grid>(NotificationZone, NotificationInternalArgs.NotificationId) != null)
                {
                    Grid NotificationZoneContentGrid = Collectif.FindChildByName<Grid>(NotificationZone, NotificationInternalArgs.NotificationId);
                    NotificationZone.Children.Remove(NotificationZoneContentGrid);
                    NotificationZone.Children.Insert(Math.Min(sender.InsertPosition, NotificationZone.Children.Count), ContentGrid);
                    NotificationZoneContentGrid.Children.Clear();
                    NotificationZoneContentGrid = null;
                }
                else
                {
                    ContentGrid.Opacity = 0;
                    var doubleAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond));
                    ContentGrid.BeginAnimation(OpacityProperty, doubleAnimation);
                    sender.InsertPosition = NotificationZone.Children.Add(ContentGrid);
                }
            }
            else if (Collectif.FindChildByName<Grid>(NotificationZone, NotificationInternalArgs.NotificationId) != null)
            {
                Grid ContentGrid = Collectif.FindChildByName<Grid>(NotificationZone, NotificationInternalArgs.NotificationId);
                var doubleAnimation = new DoubleAnimation(ContentGrid.ActualHeight, 0, new Duration(TimeSpan.FromSeconds(0.1)));
                void DeleteAfterAnimation(object sender, EventArgs eventArgs)
                {
                    ContentGrid?.Children?.Clear();
                    ContentGrid = null;
                    NotificationZone.Children.Remove(ContentGrid);
                    doubleAnimation.Completed -= DeleteAfterAnimation;
                }
                doubleAnimation.Completed += DeleteAfterAnimation;
                ContentGrid.BeginAnimation(MaxHeightProperty, doubleAnimation);
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

        private static void FocusRectangle(MapPolygon e)
        {
            SelectionRectangle selectionRectangle = SelectionRectangle.GetSelectionRectangleFromRectangle(e);
            selectionRectangle?.Focus(true);
            selectionRectangle?.PropertiesDisplayElement.BringIntoView();
        }

        private void AddRectangle_Click(object sender, RoutedEventArgs e)
        {
            AddNewSelection();
        }

        public void AddNewSelection(MapPolygon NewRectangle = null, string Nom = null, string MinZoom = null, string MaxZoom = null, string Color = null, string StrokeThickness = null)
        {
            DeleteUnusedRectangles();
            NewRectangle ??= mapSelectable.AddRectangle(new Location(0, 0), new Location(0, 0));
            SelectionRectangle selectionRectangle = new SelectionRectangle(NewRectangle, Nom, MinZoom, MaxZoom, Color, StrokeThickness)
            {
                MapSelectable = mapSelectable
            };

            SelectionRectangle.Rectangles.Add(selectionRectangle);
            selectionRectangle.Focus(true);
            RectanglesStackPanel.Children.Add(selectionRectangle.PropertiesDisplayElement);

            MapViewer.Focus();
            SelectionScrollViewer.ScrollToEnd();
        }

        private void DeleteUnusedRectangles()
        {
            int NumberOfElementInside = RectanglesStackPanel.Children.Count;
            int NumberOfUnusedRectangleDeleted = mapSelectable.DeleteUnusedRectangles();
            if (NumberOfUnusedRectangleDeleted > 0 && NumberOfElementInside != RectanglesStackPanel.Children.Count)
            {
                string infoText = NumberOfUnusedRectangleDeleted == 1 ? Languages.Current["editorSelectionsNotificationEmptySelectionDeletedSingle"] : Languages.GetWithArguments("editorSelectionsNotificationEmptySelectionDeletedMultiples", NumberOfElementInside - RectanglesStackPanel.Children.Count);
                Notification InfoUnusedRectangleDeleted = new NText(infoText, "MapsInMyFolder", "FullscreenMap")
                {
                    NotificationId = "InfoUnusedRectangleDeleted",
                    DisappearAfterAMoment = true,
                    IsPersistant = true,
                };
                InfoUnusedRectangleDeleted.Register();
            }
        }

        private void ClosePage_button_Click(object sender, RoutedEventArgs e)
        {
            PageDispose();
            MainWindow.Instance.FrameBack();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            PageDispose();
        }

        private void MapViewer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MapViewer.Focus();
        }
    }
}
