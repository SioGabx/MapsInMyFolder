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
using System.Linq;
using System.Threading.Tasks;

namespace MapsInMyFolder
{
    public partial class MainPage : Page
    {
        public void MapLoad()
        {
            mapviewer.MapLayer = new MapTileLayer();
            NO_PIN.Visibility = Settings.visibility_pins;
            SE_PIN.Visibility = Settings.visibility_pins;

            Location NO_PIN_starting_location = new Location(Settings.NO_PIN_starting_location_latitude, Settings.NO_PIN_starting_location_longitude);
            Location SE_PIN_starting_location = new Location(Settings.SE_PIN_starting_location_latitude, Settings.SE_PIN_starting_location_longitude);

            MapPanel.SetLocation(NO_PIN, NO_PIN_starting_location);
            MapPanel.SetLocation(SE_PIN, SE_PIN_starting_location);
            DrawRectangleCelectionArroundPushpin();

            mapviewer.Center = new Location(NO_PIN_starting_location.Latitude - ((NO_PIN_starting_location.Latitude - SE_PIN_starting_location.Latitude) / 2), NO_PIN_starting_location.Longitude - ((NO_PIN_starting_location.Longitude - SE_PIN_starting_location.Longitude) / 2));
            mapviewer.ZoomLevel = Settings.map_defaut_zoom_level;
            mapviewer.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(255,
                (byte)Settings.background_layer_color_R,
                (byte)Settings.background_layer_color_G,
                (byte)Settings.background_layer_color_B)
            );
        }

        public Point MakeSquareIfShift(Point targetPoint, int OpositeCorner = 0, string overrideWidthValue = null)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                return targetPoint;
            }

            Point SelectionRectanglePoint = mapviewer.LocationToView(Selection_Rectangle.Locations.ElementAt(OpositeCorner));
            double width = Math.Abs(targetPoint.X - SelectionRectanglePoint.X);
            double height = Math.Abs(targetPoint.Y - SelectionRectanglePoint.Y);
            if (string.IsNullOrEmpty(overrideWidthValue))
            {
                if (Math.Abs(width) < Math.Abs(height))
                {
                    height = width;
                }
                else
                {
                    width = height;
                }
            }
            else if (overrideWidthValue == "width")
            {
                height = width;
            }
            else if (overrideWidthValue == "height")
            {
                width = height;
            }
            Point returnNewTargetPoint = new Point(SelectionRectanglePoint.X, SelectionRectanglePoint.Y);
            bool XtargetPointIsSuperiorAsSelectionRectanglePoint = targetPoint.X > SelectionRectanglePoint.X;
            bool YtargetPointIsSuperiorAsSelectionRectanglePoint = targetPoint.Y > SelectionRectanglePoint.Y;
            //BAS GAUCHE
            //targetPoint.X > SelectionRectanglePoint.X = False
            //targetPoint.Y > SelectionRectanglePoint.Y = True
            if (XtargetPointIsSuperiorAsSelectionRectanglePoint == false && YtargetPointIsSuperiorAsSelectionRectanglePoint == true)
            {
                width *= -1;
                height *= 1;
            }


            //BAS DROITE
            //targetPoint.X > SelectionRectanglePoint.X = True
            //targetPoint.Y > SelectionRectanglePoint.Y = True
            if (XtargetPointIsSuperiorAsSelectionRectanglePoint == true && YtargetPointIsSuperiorAsSelectionRectanglePoint == true)
            {
                width *= 1;
                height *= 1;
            }

            //HAUT GAUCHE
            //targetPoint.X > SelectionRectanglePoint.X = False
            //targetPoint.Y > SelectionRectanglePoint.Y = False
            if (XtargetPointIsSuperiorAsSelectionRectanglePoint == false && YtargetPointIsSuperiorAsSelectionRectanglePoint == false)
            {
                width *= -1;
                height *= -1;
            }

            //HAUT DROITE
            //targetPoint.X > SelectionRectanglePoint.X = True
            //targetPoint.Y > SelectionRectanglePoint.Y = False
            if (XtargetPointIsSuperiorAsSelectionRectanglePoint == true && YtargetPointIsSuperiorAsSelectionRectanglePoint == false)
            {
                width *= 1;
                height *= -1;
            }

            returnNewTargetPoint.X += width;
            returnNewTargetPoint.Y += height;
            return returnNewTargetPoint;
        }

        static Point ExtandPositionByXUnit(Point positionToExtand, int XUnit, HitType MouseHitType)
        {
            Point returnPoint = positionToExtand;
            switch (MouseHitType)
            {
                case HitType.Body:
                    return returnPoint;
                case HitType.UL:
                    //top left
                    returnPoint.X -= XUnit;
                    returnPoint.Y -= XUnit;
                    return returnPoint;
                case HitType.UR:
                    //top right
                    returnPoint.X += XUnit;
                    returnPoint.Y -= XUnit;
                    return returnPoint;
                case HitType.LR:
                    //Bottom right
                    returnPoint.X += XUnit;
                    returnPoint.Y += XUnit;
                    return returnPoint;
                case HitType.LL:
                    //Bottom left
                    returnPoint.X -= XUnit;
                    returnPoint.Y += XUnit;
                    return returnPoint;
                case HitType.L:
                    //left
                    returnPoint.X -= XUnit;
                    returnPoint.Y += 0;
                    return returnPoint;
                case HitType.R:
                    //right
                    returnPoint.X += XUnit;
                    returnPoint.Y += 0;
                    return returnPoint;
                case HitType.B:
                    //bottom
                    returnPoint.X += 0;
                    returnPoint.Y += XUnit;
                    return returnPoint;
                case HitType.T:
                    //top
                    returnPoint.X += 0;
                    returnPoint.Y -= XUnit;
                    return returnPoint;
            }
            return returnPoint;
        }

        public static void MapViewerSetSelection(Dictionary<string, double> locations, bool ZoomToNewLocation = true)
        {
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
                    _instance.DrawRectangleCelectionArroundPushpin();
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
            RightClickUp(MakeSquareIfShift(e.GetPosition(mapviewer)));
        }

        public void MiddleClickUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton != MouseButtonState.Pressed)
            {
                e.Handled = true;
                is_middle_mouse_down = false;
                MapviewerMouseDownEvent(sender, e, false);
            }
        }

        public void RightClickUp(Point p)
        {
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                return;
            }
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
                DrawRectangleCelectionArroundPushpin();
                UpdatePushpinPositionAndDrawRectangle();
                is_right_mouse_down = false;
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

        private void Mapviewer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            e.Handled = true;
            if (Settings.disable_selection_rectangle_moving)
            {
                return;
            }
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
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
            UpdatePushpinPositionAndDrawRectangle();
            UpdateSelectionRectangle(e);
            LayerTilePreview_RequestUpdate();
        }
        private void Mapviewer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            MapviewerMouseDownEvent(sender, e, true);
        }

        public void MapviewerMouseDownEvent(object sender, System.Windows.Input.MouseButtonEventArgs e, bool UpdateHitType = true)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                is_middle_mouse_down = true;
                is_left_mouse_down = false;
                is_right_mouse_down = false;
                mapviewer.Cursor = new Cursor(Collectif.ReadResourceStream("cursors/closedhand.cur"));
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (is_right_mouse_down)
                {
                    return;
                }
                if (!Settings.disable_selection_rectangle_moving && Mouse.MiddleButton != MouseButtonState.Pressed)
                {
                    is_left_mouse_down = true;
                   
                  
                    if (UpdateHitType)
                    {
                        DefineSavedPoint(e);
                        MouseHitType = SetHitType(e.GetPosition(mapviewer));

                    }
                    SetMouseCursor();
                }
                else
                {
                    mapviewer.Cursor = new Cursor(Collectif.ReadResourceStream("cursors/closedhand.cur"));
                }
            }

            if (e.RightButton == MouseButtonState.Pressed && !Settings.disable_selection_rectangle_moving)
            {
                is_right_mouse_down = true;
                if (UpdateHitType)
                {
                    DefineSavedPoint(e);StartRightClick(e);
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

        public void UpdatePushpinPositionAndDrawRectangle()
        {
            void CheckPositionOutOfBound(Pushpin pushpin, Pushpin pushpin_oppose)
            {
                if (pushpin.Location.Latitude > 85)
                {
                    pushpin_oppose.Location.Latitude -= pushpin.Location.Latitude - 85;
                    pushpin.Location.Latitude = 85;
                }

                if (pushpin.Location.Latitude < -85)
                {
                    pushpin.Location.Latitude = -85;
                }

                if (pushpin.Location.Longitude > 180 && !(pushpin_oppose.Location.Longitude > 180))
                {
                    pushpin.Location.Longitude = 180;
                }

                if (pushpin.Location.Longitude < -180 && !(pushpin_oppose.Location.Longitude < -180))
                {
                    pushpin.Location.Longitude = -180;
                }
            }

            CheckPositionOutOfBound(NO_PIN, SE_PIN);
            CheckPositionOutOfBound(SE_PIN, NO_PIN);

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
            DrawRectangleCelectionArroundPushpin();
        }

        int selection_map_rectange_y_decalage = 45; //Decalage des pins (read only)
        public void DrawRectangleCelectionArroundPushpin()
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

            Commun.Map.CurentSelection.SE_Latitude = SE_PIN.Location.Latitude;
            Commun.Map.CurentSelection.SE_Longitude = SE_PIN.Location.Longitude;
            Commun.Map.CurentSelection.NO_Latitude = NO_PIN.Location.Latitude;
            Commun.Map.CurentSelection.NO_Longitude = NO_PIN.Location.Longitude;
        }

        public bool is_left_mouse_down;
        public bool is_right_mouse_down;
        public bool is_middle_mouse_down;

        Location SavedNo_Placement = new Location();
        Location SavedSe_Placement = new Location();

        Point OriginClickPlacement = new Point();
        Point OriginNoPlacement = new Point();
        Point OriginSePlacement = new Point();
        Point SavedLeftClickPreviousActions;
        public void DefineSavedPoint(MouseEventArgs e)
        {
            if (Mouse.MiddleButton != MouseButtonState.Pressed) { 
            OriginClickPlacement = e.GetPosition(mapviewer);
            //origin_saved_left_click_mouse_position = e.GetPosition(mapviewer);
            SavedLeftClickPreviousActions = e.GetPosition(mapviewer);
            OriginNoPlacement = mapviewer.LocationToView(NO_PIN.Location);
            OriginSePlacement = mapviewer.LocationToView(SE_PIN.Location);
            }
        }
        public void UpdateSelectionRectangle(MouseEventArgs e)
        {
            //e.Handled = true;
            Point mouse_location = e.GetPosition(mapviewer);
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                return;
            }
            if (is_left_mouse_down && MouseHitType != HitType.None && e.LeftButton == MouseButtonState.Pressed)
            {
                mouse_location = ExtandPositionByXUnit(e.GetPosition(mapviewer), 4, MouseHitType);
                Location PositionSouris;
                Location PositionToMakeSquareIfShift;
                switch (MouseHitType)
                {
                    case HitType.Body:
                        double deplacement_X;
                        double deplacement_Y;
                        Point SavedNo_PlacementPoint;
                        Point SavedSe_PlacementPoint;
                        double XPourcent = (OriginClickPlacement.X - OriginNoPlacement.X) / (OriginSePlacement.X - OriginNoPlacement.X);
                        double YPourcent = (OriginClickPlacement.Y - OriginNoPlacement.Y) / (OriginSePlacement.Y - OriginNoPlacement.Y);
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            if (OriginClickPlacement == SavedLeftClickPreviousActions)
                            {
                                SavedNo_Placement = NO_PIN.Location;
                                SavedSe_Placement = SE_PIN.Location;
                            }
                            SavedNo_PlacementPoint = mapviewer.LocationToView(SavedNo_Placement);
                            SavedSe_PlacementPoint = mapviewer.LocationToView(SavedSe_Placement);
                            deplacement_X = mouse_location.X - (SavedNo_PlacementPoint.X + (SavedSe_PlacementPoint.X - SavedNo_PlacementPoint.X) * XPourcent);
                            deplacement_Y = mouse_location.Y - (SavedNo_PlacementPoint.Y + (SavedSe_PlacementPoint.Y - SavedNo_PlacementPoint.Y) * YPourcent);
                            if (Math.Abs(deplacement_X) > Math.Abs(deplacement_Y))
                            {
                                deplacement_Y = 0;
                            }
                            else
                            {
                                deplacement_X = 0;
                            }
                        }
                        else
                        {
                            SavedNo_Placement = NO_PIN.Location;
                            SavedSe_Placement = SE_PIN.Location;
                            SavedNo_PlacementPoint = mapviewer.LocationToView(SavedNo_Placement);
                            SavedSe_PlacementPoint = mapviewer.LocationToView(SavedSe_Placement);
                            deplacement_X = mouse_location.X - (SavedNo_PlacementPoint.X + (SavedSe_PlacementPoint.X - SavedNo_PlacementPoint.X) * XPourcent);
                            deplacement_Y = mouse_location.Y - (SavedNo_PlacementPoint.Y + (SavedSe_PlacementPoint.Y - SavedNo_PlacementPoint.Y) * YPourcent);
                        }

                        NO_PIN.Location = mapviewer.ViewToLocation(new Point(SavedNo_PlacementPoint.X + deplacement_X, SavedNo_PlacementPoint.Y + deplacement_Y));
                        SE_PIN.Location = mapviewer.ViewToLocation(new Point(SavedSe_PlacementPoint.X + deplacement_X, SavedSe_PlacementPoint.Y + deplacement_Y));
                        SavedLeftClickPreviousActions = e.GetPosition(mapviewer);
                        break;
                    case HitType.UL:
                        //top left
                        //PositionSouris = new Location(get_fixed_location("NO").Latitude, get_fixed_location("NO").Longitude);
                        PositionToMakeSquareIfShift = mapviewer.ViewToLocation(MakeSquareIfShift(mouse_location, 2));
                        MapPanel.SetLocation(NO_PIN, PositionToMakeSquareIfShift);
                        break;
                    case HitType.UR:
                        //top right
                        PositionToMakeSquareIfShift = mapviewer.ViewToLocation(MakeSquareIfShift(mouse_location, 1));
                        PositionSouris = new Location(SE_PIN.Location.Latitude, PositionToMakeSquareIfShift.Longitude);
                        MapPanel.SetLocation(SE_PIN, PositionSouris);
                        PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, NO_PIN.Location.Longitude);
                        MapPanel.SetLocation(NO_PIN, PositionSouris);
                        break;
                    case HitType.LR:
                        //Bottom right
                        PositionToMakeSquareIfShift = mapviewer.ViewToLocation(MakeSquareIfShift(mouse_location, 0));
                        MapPanel.SetLocation(SE_PIN, PositionToMakeSquareIfShift);
                        break;
                    case HitType.LL:
                        //Bottom left
                        PositionToMakeSquareIfShift = mapviewer.ViewToLocation(MakeSquareIfShift(mouse_location, 3));
                        PositionSouris = new Location(NO_PIN.Location.Latitude, PositionToMakeSquareIfShift.Longitude);
                        MapPanel.SetLocation(NO_PIN, PositionSouris);
                        PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, SE_PIN.Location.Longitude);
                        MapPanel.SetLocation(SE_PIN, PositionSouris);
                        break;
                    case HitType.L:
                        //left
                        PositionToMakeSquareIfShift = mapviewer.ViewToLocation(MakeSquareIfShift(mouse_location, 2, "width"));
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, PositionToMakeSquareIfShift.Longitude);
                        }
                        else
                        {
                            PositionSouris = new Location(NO_PIN.Location.Latitude, PositionToMakeSquareIfShift.Longitude);
                        }
                        MapPanel.SetLocation(NO_PIN, PositionSouris);
                        break;
                    case HitType.R:
                        //right
                        PositionToMakeSquareIfShift = mapviewer.ViewToLocation(MakeSquareIfShift(mouse_location, 0, "width"));
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, PositionToMakeSquareIfShift.Longitude);
                        }
                        else
                        {
                            PositionSouris = new Location(SE_PIN.Location.Latitude, PositionToMakeSquareIfShift.Longitude);
                        }
                        MapPanel.SetLocation(SE_PIN, PositionSouris);
                        break;
                    case HitType.B:
                        //bottom
                        PositionToMakeSquareIfShift = mapviewer.ViewToLocation(MakeSquareIfShift(mouse_location, 0, "height"));
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, PositionToMakeSquareIfShift.Longitude);
                        }
                        else
                        {
                            PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, SE_PIN.Location.Longitude);
                        }
                        MapPanel.SetLocation(SE_PIN, PositionSouris);
                        break;
                    case HitType.T:
                        //top
                        PositionToMakeSquareIfShift = mapviewer.ViewToLocation(MakeSquareIfShift(mouse_location, 2, "height"));
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, PositionToMakeSquareIfShift.Longitude);
                        }
                        else
                        {
                            PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, NO_PIN.Location.Longitude);
                        }
                        MapPanel.SetLocation(NO_PIN, PositionSouris);
                        break;
                }
                DrawRectangleCelectionArroundPushpin();
            }
            else if (is_right_mouse_down)
            {
                if (mapviewer.Cursor != Cursors.Cross)
                {
                    mapviewer.Cursor = Cursors.Cross;
                }

                SE_PIN.Location = mapviewer.ViewToLocation(MakeSquareIfShift(e.GetPosition(mapviewer)));
                DrawRectangleCelectionArroundPushpin();
            }
            else
            {
                if (Mouse.LeftButton != MouseButtonState.Pressed && Mouse.RightButton != MouseButtonState.Pressed)
                {
                    MouseHitType = SetHitType(mouse_location);
                }
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
            if (is_left_mouse_down || is_right_mouse_down) { return; }
            mapviewer.Cursor = Cursors.Arrow;
        }
        private void Selection_Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            UpdateSelectionRectangle(e);
        }

        private void Selection_Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                is_left_mouse_down = false;
                is_right_mouse_down = false;
                return;
            }
            if (!Settings.disable_selection_rectangle_moving)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (is_right_mouse_down) { e.Handled = true; return; }
                    e.Handled = true;
                    is_left_mouse_down = true;
                   
                        DefineSavedPoint(e);
                }

                if (e.RightButton == MouseButtonState.Pressed)
                {
                    e.Handled = true;
                    if (is_left_mouse_down)
                    {
                        return;
                    }
                }
            }
        }

        private void Selection_Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.MiddleButton == MouseButtonState.Pressed) { e.Handled = true; return; }

            if (is_left_mouse_down && Mouse.LeftButton == MouseButtonState.Released)
            {
                is_left_mouse_down = false;
                RightClickUp(e.GetPosition(mapviewer));
            }
            else if (Mouse.LeftButton != MouseButtonState.Pressed && Mouse.RightButton != MouseButtonState.Pressed)
            {
                Point mouse_location = e.GetPosition(mapviewer);
                MouseHitType = SetHitType(mouse_location);
                SetMouseCursor();
            }
        }

        private void Mapviewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            LayerTilePreview_RequestUpdate();
        }
    }

    public partial class MainWindow : Window
    {
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.MiddleButton != MouseButtonState.Pressed)
            {
                MainPage.MiddleClickUp(sender, e);
                MainPage.RightClickUp(e.GetPosition(MainPage.mapviewer));
                MainPage.UpdateSelectionRectangle(e);
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            //e.Handled = true;
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                return;
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                MainPage.UpdateSelectionRectangle(e);
            }
            else
            {
                MainPage.is_right_mouse_down = false;
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