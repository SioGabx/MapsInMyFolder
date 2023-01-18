using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using MapsInMyFolder.MapControl;
using System.Diagnostics;
using System.Windows.Controls;
using MapsInMyFolder.Commun;
using System.Windows.Threading;
using System.Threading;

namespace MapsInMyFolder
{
    public partial class MainPage : Page
    {
        public void MapLoad()
        {
            UIElement EmptyMapTileLayer = new MapTileLayer();
            mapviewer.MapLayer = EmptyMapTileLayer;
            NO_PIN.Visibility = Settings.visibility_pins;
            SE_PIN.Visibility = Settings.visibility_pins;

            Location NO_PIN_starting_location = new Location(Settings.NO_PIN_starting_location_latitude, Settings.NO_PIN_starting_location_longitude);
            Location SE_PIN_starting_location = new Location(Settings.SE_PIN_starting_location_latitude, Settings.SE_PIN_starting_location_longitude);

            MapPanel.SetLocation(NO_PIN, NO_PIN_starting_location);
            MapPanel.SetLocation(SE_PIN, SE_PIN_starting_location);
            Draw_rectangle_selection_arround_pushpin();

            mapviewer.Center = new Location(NO_PIN_starting_location.Latitude - ((NO_PIN_starting_location.Latitude - SE_PIN_starting_location.Latitude) / 2), NO_PIN_starting_location.Longitude - ((NO_PIN_starting_location.Longitude - SE_PIN_starting_location.Longitude) / 2));
            mapviewer.ZoomLevel = Settings.map_defaut_zoom_level;
            System.Windows.Media.SolidColorBrush brush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(255,
                (byte)Settings.background_layer_color_R,
                (byte)Settings.background_layer_color_G,
                (byte)Settings.background_layer_color_B)
                );
            mapviewer.Background = brush;
        }

        public static void MapViewerSetSelection(Dictionary<string, double> locations, bool ZoomToNewLocation = true)
        {
            //    Javascript JavascriptLocationInstance = Javascript.JavascriptInstance;
            //    JavascriptLocationInstance.LocationChanged += (o, e) => MapViewerSetSelection(e.Location);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    Location ActualNO_PINLocation = _instance.NO_PIN.Location;
                    Location ActualSE_PINLocation = _instance.SE_PIN.Location;
                    if (ActualNO_PINLocation.Latitude == locations["NO_Latitude"] ||
                        ActualNO_PINLocation.Longitude == locations["NO_Longitude"] ||
                        ActualSE_PINLocation.Latitude == locations["SE_Latitude"] ||
                        ActualSE_PINLocation.Longitude == locations["SE_Longitude"])
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MapViewerSetSelection 1" + ex.Message);
                    return;
                }
                try
                {
                    Location SE_Loc = new Location(locations["SE_Latitude"], locations["SE_Longitude"]);
                    Location NO_Loc = new Location(locations["NO_Latitude"], locations["NO_Longitude"]);
                    _instance.NO_PIN.Location = NO_Loc;
                    _instance.SE_PIN.Location = SE_Loc;
                    _instance.Draw_rectangle_selection_arround_pushpin();
                    if (ZoomToNewLocation)
                    {
                        Point NO_Point_Bounds = _instance.mapviewer.LocationToView(NO_Loc);
                        Point SE_Point_Bounds = _instance.mapviewer.LocationToView(SE_Loc);
                        Location NO_Location_Bounds = _instance.mapviewer.ViewToLocation(new Point(NO_Point_Bounds.X - Settings.maps_margin_ZoomToBounds, NO_Point_Bounds.Y - Settings.maps_margin_ZoomToBounds));
                        Location SE_Location_Bounds = _instance.mapviewer.ViewToLocation(new Point(SE_Point_Bounds.X + Settings.maps_margin_ZoomToBounds, SE_Point_Bounds.Y + Settings.maps_margin_ZoomToBounds));
                        _instance.mapviewer.ZoomToBounds(new BoundingBox(NO_Location_Bounds.Latitude, NO_Location_Bounds.Longitude, SE_Location_Bounds.Latitude, SE_Location_Bounds.Longitude));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MapViewerSetSelection 2 " + ex.Message);
                }
            }, null);
        }

        private void Mapviewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            RightClickUp(e.GetPosition(mapviewer));
        }

        public void RightClickUp(Point p)
        {
            if (Settings.disable_selection_rectangle_moving)
            {
                return;
            }

            if (is_right_mouse_down)
            {
                Point NewLocation = p;
                double No_X = mapviewer.LocationToView(NO_PIN.Location).X;
                double No_Y = mapviewer.LocationToView(NO_PIN.Location).Y;
                double Se_X = mapviewer.LocationToView(SE_PIN.Location).X;
                double Se_Y = mapviewer.LocationToView(SE_PIN.Location).Y;
                if ((Se_X - No_X) <= 2 && (Se_X - No_X) >= -2)
                {
                    if ((Se_X - No_X) < 0)
                    {
                        NewLocation.X -= 2;
                    }
                    else
                    {
                        NewLocation.X += 2;
                    }
                }
                if ((Se_Y - No_Y) <= 2 && (Se_Y - No_Y) >= -2)
                {
                    if ((Se_Y - No_Y) < 0)
                    {
                        NewLocation.Y -= 2;
                    }
                    else
                    {
                        NewLocation.Y += 2;
                    }
                }
                SE_PIN.Location = mapviewer.ViewToLocation(NewLocation);
                Draw_rectangle_selection_arround_pushpin();
                Pushpin_stop_mooving();
                is_right_mouse_down = false;
                //Selection_Rectangle.IsHitTestVisible = true;
            }
        }

        void StartRightClick(MouseButtonEventArgs e)
        {
            if (Settings.disable_selection_rectangle_moving)
            {
                return;
            }
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                return;
            }
            is_right_mouse_down = true;
            mapviewer.Cursor = Cursors.Cross;
            SE_PIN.Location = mapviewer.ViewToLocation(e.GetPosition(mapviewer));
            NO_PIN.Location = mapviewer.ViewToLocation(e.GetPosition(mapviewer));
            StartDownloadButton.Focus();
        }

        private void Mapviewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (is_left_mouse_down) { e.Handled = true; return; }
            StartRightClick(e);
            //Selection_Rectangle.IsHitTestVisible = false;
        }

        private void Mapviewer_MouseLeave(object sender, MouseEventArgs e)
        {
            //RightClickUp(e.GetPosition(mapviewer));
        }

        private void MapGrid_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void NO_PIN_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
        }

        private void NO_PIN_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void NO_PIN_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Pushpin_stop_mooving();
            UpdateSelectionRectangle(e);
        }

        private void SE_PIN_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Pushpin_stop_mooving();
            UpdateSelectionRectangle(e);
        }

        private void SE_PIN_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void SE_PIN_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void Mapviewer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            e.Handled = true;
            if (Settings.disable_selection_rectangle_moving)
            {
                return;
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                UpdateSelectionRectangle(e);
            }
            else
            {
                is_right_mouse_down = false;
                //Selection_Rectangle.IsHitTestVisible = true;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelectionRectangle(e);
            }
            else
            {
                is_left_mouse_down = false;
            }
        }

        private void Mapviewer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (is_left_mouse_down)
            {
                is_left_mouse_down = false;
            }
            if (is_right_mouse_down)
            {
                is_right_mouse_down = false;
                //Selection_Rectangle.IsHitTestVisible = true;
            }
            Pushpin_stop_mooving();
            UpdateSelectionRectangle(e);
            LayerTilePreview_RequestUpdate();
        }
        private void Mapviewer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (is_right_mouse_down)
                {
                    e.Handled = true; return;
                }
                else
                {
                    Cursor grab = new Cursor(Collectif.ReadResourceStream("cursors/closedhand.cur"));
                    mapviewer.Cursor = grab;
                    if (!Settings.disable_selection_rectangle_moving)
                    {
                        is_left_mouse_down = true;
                        saved_left_click_mouse_position = e.GetPosition(mapviewer);
                    }
                }
            }

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                e.Handled = true;
                Cursor grab = new Cursor(Collectif.ReadResourceStream("cursors/closedhand.cur"));
                mapviewer.Cursor = grab;
            }

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (is_left_mouse_down)
                {
                    e.Handled = true; return;
                }
                else
                {
                    if (!Settings.disable_selection_rectangle_moving)
                    {
                        if (e.MiddleButton == MouseButtonState.Released)
                        {
                            e.Handled = true;
                            is_right_mouse_down = true;
                            //Selection_Rectangle.IsHitTestVisible = false;
                            saved_left_click_mouse_position = e.GetPosition(mapviewer);
                        }
                    }
                }
            }
        }
        public List<double> Map_location_to_mouse_location(System.Windows.Input.MouseEventArgs mouse_position)
        {
            System.Windows.Point LocationXY = new System.Windows.Point(mouse_position.GetPosition(mapviewer).X, mouse_position.GetPosition(mapviewer).Y);
            Location pinLocation = mapviewer.ViewToLocation(LocationXY);
            return new List<double>() { pinLocation.Latitude, pinLocation.Longitude };
        }

        private void Map_panel_open_location_panel_Click(object sender, RoutedEventArgs e)
        {
            Message.NoReturnBoxAsync("Cette fonctionnalité fait l'objet d'une prochaine mise à jour, elle n'as pas encore été ajoutée à cette version !", "Erreur");
        }

        public void Pushpin_stop_mooving()
        {
            void CheckPositionOutOfBound(Pushpin pushpin)
            {
                if (pushpin.Location.Latitude > 85)
                {
                    pushpin.Location.Latitude = 85;
                }

                if (pushpin.Location.Latitude < -85)
                {
                    pushpin.Location.Latitude = -85;
                }

                if (pushpin.Location.Longitude > 180)
                {
                    pushpin.Location.Longitude = 180;
                }

                if (pushpin.Location.Longitude < -180)
                {
                    pushpin.Location.Longitude = -180;
                }
            }

           CheckPositionOutOfBound(NO_PIN);
            CheckPositionOutOfBound(SE_PIN);

            Location NO_PIN_Temp_Loc = NO_PIN.Location;
            Location SE_PIN_Temp_Loc = SE_PIN.Location;


            if (NO_PIN.Location.Latitude < SE_PIN.Location.Latitude)
            {
                NO_PIN.Location = new Location(SE_PIN_Temp_Loc.Latitude, NO_PIN.Location.Longitude);
                SE_PIN.Location = new Location(NO_PIN_Temp_Loc.Latitude, SE_PIN.Location.Longitude);
            }

            if (NO_PIN.Location.Longitude > SE_PIN.Location.Longitude)
            {
                NO_PIN.Location = new Location(NO_PIN.Location.Latitude, SE_PIN_Temp_Loc.Longitude);
                SE_PIN.Location = new Location(SE_PIN.Location.Latitude, NO_PIN_Temp_Loc.Longitude);
            }
            Draw_rectangle_selection_arround_pushpin();
        }

        int selection_map_rectange_y_decalage = 45; //Decalage des pins (read only)
        public void Draw_rectangle_selection_arround_pushpin()
        {
            Selection_Rectangle.Locations = new List<Location>() { NO_PIN.Location, new Location(SE_PIN.Location.Latitude, NO_PIN.Location.Longitude), SE_PIN.Location, new Location(NO_PIN.Location.Latitude, SE_PIN.Location.Longitude) };

            if (Settings.visibility_pins == Visibility.Visible)
            {
                NO_PIN.Content = "Latitude = " + Math.Round(NO_PIN.Location.Latitude, 6).ToString() + "\nLongitude = " + Math.Round(NO_PIN.Location.Longitude, 6).ToString();
                SE_PIN.Content = "Latitude = " + Math.Round(SE_PIN.Location.Latitude, 6).ToString() + "\nLongitude = " + Math.Round(SE_PIN.Location.Longitude, 6).ToString();
                selection_map_rectange_y_decalage = 65;
            }
            else
            {
                NO_PIN.Content = "Coin_superieur";
                SE_PIN.Content = "Coin_inferieur";
                selection_map_rectange_y_decalage = 45;
            }

            Curent.Selection.SE_Latitude = SE_PIN.Location.Latitude;
            Curent.Selection.SE_Longitude = SE_PIN.Location.Longitude;
            Curent.Selection.NO_Latitude = NO_PIN.Location.Latitude;
            Curent.Selection.NO_Longitude = NO_PIN.Location.Longitude;
        }

        public Boolean is_left_mouse_down;
        public Boolean is_right_mouse_down;
        Point saved_left_click_mouse_position;
        Point saved_no_pin_location;
        Point saved_se_pin_location;

        public void UpdateSelectionRectangle(MouseEventArgs e)
        {
            //todo : add square selection on shift key
            //todo : corect position on wheel move
            e.Handled = true;
            Point mouse_location = e.GetPosition(mapviewer);
            if (is_left_mouse_down && MouseHitType != HitType.None && e.LeftButton == MouseButtonState.Pressed)
            {
                Location PositionSouris;// = new Location(mouse_map_location[0], mouse_map_location[1]);
                Location get_fixed_location(string NOorSE)
                {
                    List<double> mouse_map_location = Map_location_to_mouse_location(e);
                    double X_correctif;
                    double Y_correctif;
                    if (NOorSE == "NO")
                    {
                        X_correctif = saved_left_click_mouse_position.X - saved_no_pin_location.X;
                        Y_correctif = saved_left_click_mouse_position.Y - saved_no_pin_location.Y;
                        X_correctif = Math.Abs(X_correctif) * -1;
                        Y_correctif = Math.Abs(Y_correctif) * -1;
                    }
                    else
                    {
                        X_correctif = saved_left_click_mouse_position.X - saved_se_pin_location.X;
                        Y_correctif = saved_left_click_mouse_position.Y - saved_se_pin_location.Y;
                        X_correctif = Math.Abs(X_correctif);
                        Y_correctif = Math.Abs(Y_correctif);
                    }

                    if (!(Y_correctif > 1 || Y_correctif < 0))
                    {
                        Y_correctif = 0;
                    }
                    if (!(X_correctif > 1 || X_correctif < 0))
                    {
                        X_correctif = 0;
                    }

                    Location temp_location_from_mouse = new Location(mouse_map_location[0], mouse_map_location[1]);
                    double location_x_fixed = mapviewer.LocationToView(temp_location_from_mouse).X + X_correctif;
                    double location_y_fixed = mapviewer.LocationToView(temp_location_from_mouse).Y + Y_correctif;
                    Location final_edited_location = mapviewer.ViewToLocation(new Point(location_x_fixed, location_y_fixed));
                    return new Location(final_edited_location.Latitude, final_edited_location.Longitude);
                }

                switch (MouseHitType)
                {
                    case HitType.Body:
                        double deplacement_X = mouse_location.X - saved_left_click_mouse_position.X;
                        double deplacement_Y = mouse_location.Y - saved_left_click_mouse_position.Y;
                        Point No_Placement = mapviewer.LocationToView(NO_PIN.Location);
                        Point Se_Placement = mapviewer.LocationToView(SE_PIN.Location);
                        NO_PIN.Location = mapviewer.ViewToLocation(new Point(No_Placement.X + deplacement_X, No_Placement.Y + deplacement_Y));
                        SE_PIN.Location = mapviewer.ViewToLocation(new Point(Se_Placement.X + deplacement_X, Se_Placement.Y + deplacement_Y));
                        saved_left_click_mouse_position = e.GetPosition(mapviewer);
                        break;
                    case HitType.UL:

                        PositionSouris = new Location(get_fixed_location("NO").Latitude, get_fixed_location("NO").Longitude);
                        MapPanel.SetLocation(NO_PIN, PositionSouris);
                        break;
                    case HitType.UR:
                        PositionSouris = new Location(SE_PIN.Location.Latitude, get_fixed_location("SE").Longitude);
                        MapPanel.SetLocation(SE_PIN, PositionSouris);
                        PositionSouris = new Location(get_fixed_location("NO").Latitude, NO_PIN.Location.Longitude);
                        MapPanel.SetLocation(NO_PIN, PositionSouris);
                        break;
                    case HitType.LR:
                        PositionSouris = new Location(get_fixed_location("SE").Latitude, get_fixed_location("SE").Longitude);
                        MapPanel.SetLocation(SE_PIN, PositionSouris);
                        break;
                    case HitType.LL:
                        PositionSouris = new Location(NO_PIN.Location.Latitude, get_fixed_location("NO").Longitude);
                        MapPanel.SetLocation(NO_PIN, PositionSouris);
                        PositionSouris = new Location(get_fixed_location("SE").Latitude, SE_PIN.Location.Longitude);
                        MapPanel.SetLocation(SE_PIN, PositionSouris);
                        break;
                    case HitType.L:
                        PositionSouris = new Location(NO_PIN.Location.Latitude, get_fixed_location("NO").Longitude);
                        MapPanel.SetLocation(NO_PIN, PositionSouris);
                        break;
                    case HitType.R:
                        PositionSouris = new Location(SE_PIN.Location.Latitude, get_fixed_location("SE").Longitude);
                        MapPanel.SetLocation(SE_PIN, PositionSouris);
                        break;
                    case HitType.B:
                        PositionSouris = new Location(get_fixed_location("SE").Latitude, SE_PIN.Location.Longitude);
                        MapPanel.SetLocation(SE_PIN, PositionSouris);
                        break;
                    case HitType.T:
                        PositionSouris = new Location(get_fixed_location("NO").Latitude, NO_PIN.Location.Longitude);
                        MapPanel.SetLocation(NO_PIN, PositionSouris);
                        break;
                }
                Draw_rectangle_selection_arround_pushpin();
            }
            else if (is_right_mouse_down)
            {
                //Selection_Rectangle.IsHitTestVisible = false;
                if (mapviewer.Cursor != Cursors.Cross)
                {
                    Debug.WriteLine("set cursor");
                    mapviewer.Cursor = Cursors.Cross;
                }
                SE_PIN.Location = mapviewer.ViewToLocation(e.GetPosition(mapviewer));
                Draw_rectangle_selection_arround_pushpin();
            }
            else
            {
                MouseHitType = SetHitType(mouse_location);
                SetMouseCursor();
            }
        }

        private enum HitType
        {
            None, Body, UL, UR, LR, LL, L, R, T, B, No
        };

        HitType MouseHitType = HitType.None;
        private HitType SetHitType(Point point)
        {
            double tblr_GAP = Settings.selection_rectangle_resize_tblr_gap;
            double angle_GAP = Settings.selection_rectangle_resize_angle_gap;
            double left = NO_PIN.TranslatePoint(new Point(0, 0), mapviewer).X;
            double top = NO_PIN.TranslatePoint(new Point(0, 0), mapviewer).Y + selection_map_rectange_y_decalage + 5;
            double right = SE_PIN.TranslatePoint(new Point(0, 0), mapviewer).X;
            double bottom = SE_PIN.TranslatePoint(new Point(0, 0), mapviewer).Y + selection_map_rectange_y_decalage + 5;

            if (point.X < left) return HitType.None;
            if (point.X > right) return HitType.None;
            if (point.Y < top) return HitType.None;
            if (point.Y > bottom) return HitType.None;

            if (Settings.disable_selection_rectangle_moving)
            {
                return HitType.No;
            }
            Point No_Placement = mapviewer.LocationToView(NO_PIN.Location);
            Point Se_Placement = mapviewer.LocationToView(SE_PIN.Location);
            double width = Se_Placement.X - No_Placement.X;
            double height = Se_Placement.Y - No_Placement.Y;
            if ((width < (3 * tblr_GAP)) || (height < (3 * tblr_GAP)))
            {
                if (mapviewer.ZoomLevel < 18)
                {
                    return HitType.Body;
                }
            }

            if (point.X - left < angle_GAP)
            {
                if (point.Y - top < angle_GAP) return HitType.UL;
                if (bottom - point.Y < angle_GAP) return HitType.LL;
                if (point.X - left < tblr_GAP) return HitType.L;
            }
            if (right - point.X < angle_GAP)
            {
                if (point.Y - top < angle_GAP) return HitType.UR;
                if (bottom - point.Y < angle_GAP) return HitType.LR;
                if (right - point.X < tblr_GAP) return HitType.R;
            }
            if (point.Y - top < tblr_GAP) return HitType.T;
            if (bottom - point.Y < tblr_GAP) return HitType.B;
            return HitType.Body;
        }

        private void SetMouseCursor()
        {
            Cursor desired_cursor = Cursors.Arrow;

            switch (MouseHitType)
            {
                case HitType.None:
                    desired_cursor = Cursors.Arrow;
                    break;
                case HitType.No:
                    desired_cursor = Cursors.No;
                    break;
                case HitType.Body:
                    desired_cursor = Cursors.SizeAll;
                    break;
                case HitType.UL:
                case HitType.LR:
                    desired_cursor = Cursors.SizeNWSE;
                    break;
                case HitType.LL:
                case HitType.UR:
                    desired_cursor = Cursors.SizeNESW;
                    break;
                case HitType.T:
                case HitType.B:
                    desired_cursor = Cursors.SizeNS;
                    break;
                case HitType.L:
                case HitType.R:
                    desired_cursor = Cursors.SizeWE;
                    break;
            }

            if (mapviewer.Cursor != desired_cursor)
            {
                if (is_left_mouse_down) { return; }
                if (is_right_mouse_down) { return; }
                mapviewer.Cursor = desired_cursor;
            }
        }
        private void Selection_Rectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            if (is_left_mouse_down) { return; }
            if (is_right_mouse_down) { return; }
            mapviewer.Cursor = Cursors.Arrow;
        }
        private void Selection_Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            UpdateSelectionRectangle(e);
        }

        private void Selection_Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!Settings.disable_selection_rectangle_moving)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (is_right_mouse_down) { e.Handled = true; return; }
                    e.Handled = true;
                    is_left_mouse_down = true;
                    saved_left_click_mouse_position = e.GetPosition(mapviewer);
                    saved_no_pin_location = mapviewer.LocationToView(NO_PIN.Location);
                    saved_se_pin_location = mapviewer.LocationToView(SE_PIN.Location);
                }

                if (e.RightButton == MouseButtonState.Pressed)
                {
                    if (is_left_mouse_down)
                    {
                        e.Handled = true; return;
                    }
                    else
                    {
                        e.Handled = true;
                        StartRightClick(e);
                        saved_left_click_mouse_position = e.GetPosition(mapviewer);
                        //Debug.WriteLine(saved_left_click_mouse_position.X + " " + saved_left_click_mouse_position.Y);
                    }
                }
            }
        }

        private void Selection_Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (is_left_mouse_down)
            {
                is_left_mouse_down = false;
                RightClickUp(e.GetPosition(mapviewer));
            }
        }

        private void Mapviewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            LayerTilePreview_RequestUpdate();
        }
    }

    public partial class MainWindow : Window
    {
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            //RightClickUp(e.GetPosition(mapviewer));
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MainPage.RightClickUp(e.GetPosition(MainPage.mapviewer));
            MainPage.UpdateSelectionRectangle(e);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            //e.Handled = true;

            if (e.RightButton == MouseButtonState.Pressed)
            {
                MainPage.UpdateSelectionRectangle(e);
            }
            else
            {
                if (e.MiddleButton == MouseButtonState.Released)
                {
                    MainPage.is_right_mouse_down = false;
                }

                //Selection_Rectangle.IsHitTestVisible = true;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                MainPage.UpdateSelectionRectangle(e);
            }
            else
            {
                MainPage.is_left_mouse_down = false;
            }
        }
    }



}