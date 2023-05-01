using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MapsInMyFolder
{
    public partial class MapSelectable
    {
        private HitType SetHitType(Point point)
        {
            var ActiveRectangleLocations = GetRectangleLocation(ActiveRectangle);
            Point No_Placement = map.LocationToView(ActiveRectangleLocations.NO);
            Point Se_Placement = map.LocationToView(ActiveRectangleLocations.SE);

            double tblr_GAP = Settings.selection_rectangle_resize_tblr_gap;
            double angle_GAP = Settings.selection_rectangle_resize_angle_gap;
            double left = No_Placement.X;
            double top = No_Placement.Y;
            double right = Se_Placement.X;
            double bottom = Se_Placement.Y;
            //Debug.WriteLine($"mouse  : {point.X} / {point.Y}");
            //Debug.WriteLine($"left   : {left}");
            //Debug.WriteLine($"top    : {top}");
            //Debug.WriteLine($"right  : {right}");
            //Debug.WriteLine($"bottom : {bottom}");

            if (point.X < left) return HitType.None;
            if (point.X > right) return HitType.None;
            if (point.Y < top) return HitType.None;
            if (point.Y > bottom) return HitType.None;

            if (DisableRectangleMoving)
            {
                return HitType.NotAllowed;
            }
            double width = Se_Placement.X - No_Placement.X;
            double height = Se_Placement.Y - No_Placement.Y;
            if ((width < (3 * tblr_GAP)) || (height < (3 * tblr_GAP)))
            {
                if (map.ZoomLevel < 18)
                {
                    if (IsSnapToGrid)
                    {
                        return HitType.NotAllowed;
                    }
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
            if (IsSnapToGrid)
            {
                return HitType.None;
            }
            return HitType.Body;
        }
        private void ApplyMapCursor(HitType hitType = HitType.Null)
        {
            Cursor desired_cursor = Cursors.Arrow;
            if (hitType == HitType.Null)
            {
                if (IsLeftClick) { return; }
                if (IsRightClick) { return; }
                hitType = MouseHitType;
            }
            switch (hitType)
            {
                case HitType.None:
                    desired_cursor = Cursors.Arrow;
                    break;
                case HitType.NotAllowed:
                    //desired_cursor = Cursors.No;
                    desired_cursor = Cursors.Arrow;
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
            if (map.Cursor != desired_cursor)
            {
                map.Cursor = desired_cursor;
            }
        }

        private static Point ExtandPositionByXUnit(Point positionToExtand, int XUnit, HitType MouseHitType)
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
    }
    public partial class MapSelectable
    {
        private enum HitType
        {
            None, Body, UL, UR, LR, LL, L, R, T, B, Null, NotAllowed
        };

        private MapControl.Map map;

        private bool IsRightClick = false;
        private bool IsLeftClick = false;

        public event EventHandler<MapPolygon> OnLocationUpdated;
        public event EventHandler<MapPolygon> RectangleGotFocus;
        public event EventHandler<MapPolygon> RectangleLostFocus;
        public event EventHandler<MapPolygon> OnRectangleDeleted;

        public bool RectangleCanBeDeleted = false;
        public bool IsSnapToGrid = false;
        public bool DisableRectangleMoving = false;
        public int MinimalNumberOfRectangles = 0;

        private HitType MouseHitType = HitType.None;

        private Location SavedNo_Placement = new Location();
        private Location SavedSe_Placement = new Location();

        private Point OriginClickPlacement = new Point();
        private Point OriginNoPlacement = new Point();
        private Point OriginSePlacement = new Point();
        private Point SavedLeftClickPreviousActions;

        private List<MapPolygon> Rectangles = new List<MapPolygon>();

        private MapPolygon _ActiveRectangle;
        public MapPolygon ActiveRectangle
        {
            get
            {
                return _ActiveRectangle;
            }
            set
            {
                _ActiveRectangle = value;
                //RectangleGotFocus?.Invoke(this, value);
            }
        }


        public MapSelectable(MapControl.Map map, Location StartingNO = null, Location StartingSE = null, Window window = null, System.Windows.Controls.Page page = null)
        {
            this.map = map;
            map.Focusable = true;
            System.Windows.Controls.UIElementCollection uIElementCollection = map.Children;
            try
            {
                foreach (UIElement element in uIElementCollection)
                {
                    if (element.GetType() == typeof(MapPolygon))
                    {
                        map.Children.Remove(element);
                    }
                }
            }
            catch (Exception) { }
            SavedNo_Placement = StartingNO;
            SavedSe_Placement = StartingSE;
            AddRectangle(StartingNO, StartingSE);

            map.MouseDown += (o, e) => MapMouseDown(o, e);
            map.MouseMove += MapMouseMove;
            map.MouseWheel += (o, e) => MainPage._instance.LayerTilePreview_RequestUpdate();
            map.MouseRightButtonUp += (o, e) => RightClickUp();
            map.MouseLeftButtonUp += (o, e) => LeftClickUp();//MouseLeftButtonUp
            map.KeyDown += (o, e) =>
            {
                if (e.Key == Key.Delete)
                {
                    DeleteRectangle(ActiveRectangle);
                    e.Handled = true;
                }

            };

            if (window != null)
            {
                window.MouseUp += WindowMouseUp;
                window.MouseMove += WindowMouseMove;
            }
            if (page != null)
            {
                page.MouseUp += WindowMouseUp;
                page.MouseMove += WindowMouseMove;
            }
            OnLocationUpdated?.Invoke(this, ActiveRectangle);
        }

        public MapPolygon AddRectangle(Location NO, Location SE)
        {
            SetRectangleAsActive(CreateRectangle(NO, SE));
            map.Children.Add(ActiveRectangle);
            ActiveRectangle.Focus();
            return ActiveRectangle;
        }

        public MapPolygon AddRectangle(MapPolygon Rectangle)
        {
            AttachEventToRectangle(Rectangle);
            map.Children.Add(ActiveRectangle);
            Rectangles.Add(Rectangle);
            ActiveRectangle.Focus();
            return ActiveRectangle;
        }

        public void SetRectangleAsActive(MapPolygon Rectangle)
        {
            if (Rectangle == ActiveRectangle || Rectangle is null)
            {
                return;
            }
            if (ActiveRectangle != null)
            {
                ApplyRectangleInactiveColor(ActiveRectangle);
            }
            ApplyRectangleActiveColor(Rectangle);
            if (map.Children.Contains(Rectangle))
            {
                map.Children.Remove(Rectangle);
                map.Children.Add(Rectangle);
            }
            ActiveRectangle = Rectangle;
        }

        private MapPolygon CreateRectangle(Location NO, Location SE)
        {
            Debug.WriteLine("Create Rectangle");
            MapPolygon Rectangle = new MapPolygon
            {
                StrokeDashCap = System.Windows.Media.PenLineCap.Square
            };
            ApplyRectangleActiveColor(Rectangle);
            SetRectangleLocation(NO, SE, Rectangle);
            CleanRectangleLocations(Rectangle);
            AttachEventToRectangle(Rectangle);
            Rectangles.Add(Rectangle);
            return Rectangle;
        }

        public MapPolygon AttachEventToRectangle(MapPolygon Rectangle)
        {
            Rectangle.MouseLeave += (o, e) =>
            {
                if (IsLeftClick || IsRightClick) { return; }
                map.Cursor = Cursors.Arrow;
            };
            Rectangle.Focusable = true;

            Rectangle.MouseMove += (o, e) =>
            {
                UpdateSelectionRectangle(o, e);
            };
            Rectangle.MouseDown += RectangleMouseDown;
            Rectangle.MouseUp += RectangleMouseUp;
            Rectangle.PreviewMouseLeftButtonUp += (o, e) =>
            {
                if (MouseHitType != HitType.None) { return; }
                SetRectangleAsActive(Rectangle);
            };
            Rectangle.KeyDown += (o, e) =>
            {
                if (e.Key == Key.Delete)
                {
                    DeleteRectangle(ActiveRectangle);
                    e.Handled = true;
                }

            };
            return Rectangle;
        }

        public bool DeleteRectangle(MapPolygon Rectangle)
        {
            if (Rectangles.Count <= MinimalNumberOfRectangles)
            {
                return false;
            }
            if (!RectangleCanBeDeleted)
            {
                return false;
            }
            if (Rectangle is null)
            {
                return false;
            }
            if (map.Children.Contains(Rectangle))
            {

                map.Children.Remove(Rectangle);
            }
            if (Rectangles.Contains(Rectangle))
            {
                Rectangles.Remove(Rectangle);
            }
            if (Rectangles.Count > 0)
            {
                MapPolygon LastAddedPolygon = Rectangles.Last();
                SetRectangleAsActive(LastAddedPolygon);
                LastAddedPolygon.Focus();
            }
            else
            {
                MapPolygon newPolygon = AddRectangle(new Location(0, 0), new Location(0, 0));
                SetRectangleAsActive(newPolygon);
                newPolygon.Focus();
            }

            OnRectangleDeleted?.Invoke(this, Rectangle);
            return true;
        }

        private void ApplyRectangleActiveColor(MapPolygon Rectangle)
        {
            RectangleGotFocus?.Invoke(this, Rectangle);
            Rectangle.Fill = Collectif.HexValueToSolidColorBrush("#4CF18712");
            Rectangle.Stroke = Collectif.HexValueToSolidColorBrush("#F18712");
            Rectangle.StrokeThickness = 3;
        }
        private void ApplyRectangleInactiveColor(MapPolygon Rectangle)
        {
            RectangleLostFocus?.Invoke(this, Rectangle);
            Rectangle.Fill = Collectif.HexValueToSolidColorBrush("#4C000000");
            Rectangle.Stroke = Collectif.HexValueToSolidColorBrush("#707070");
            Rectangle.StrokeThickness = 3;
        }

        private void WindowMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.MiddleButton != MouseButtonState.Pressed)
            {
                MiddleClickUp(sender, e);
                RightClickUp();
                LeftClickUp();
                UpdateSelectionRectangle(sender, e);
            }
        }

        private void WindowMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                return;
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                UpdateSelectionRectangle(sender, e);
            }
            else
            {
                IsRightClick = false;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelectionRectangle(sender, e);
            }
            else
            {
                IsLeftClick = false;
            }
        }

        private void RectangleMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                IsLeftClick = false;
                IsRightClick = false;
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed && Mouse.RightButton == MouseButtonState.Released)
            {
                e.Handled = true;
                IsLeftClick = true;
                SaveCurrentMousePosition(e);
                if (!(sender as UIElement).IsFocused)
                {
                    (sender as UIElement).Focus();
                }
            }
        }

        private void RectangleMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                e.Handled = true;
                return;
            }

            if (Mouse.LeftButton != MouseButtonState.Pressed && Mouse.RightButton != MouseButtonState.Pressed)
            {
                Point mouse_location = e.GetPosition(map);
                MouseHitType = SetHitType(mouse_location);
                ApplyMapCursor();
            }
        }

        private void RightClickUp()
        {
            if (IsRightClick && Mouse.RightButton == MouseButtonState.Released)
            {

                IsRightClick = false;
                CleanRectangleLocations(ActiveRectangle);
            }
        }

        private void LeftClickUp()
        {
            if (IsLeftClick && Mouse.LeftButton == MouseButtonState.Released)
            {
                IsLeftClick = false;
                CleanRectangleLocations(ActiveRectangle);
            }

            MainPage._instance.LayerTilePreview_RequestUpdate();
        }

        private void MiddleClickUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton != MouseButtonState.Pressed)
            {
                if (sender.GetType() == map.GetType())
                {
                    e.Handled = true;
                }
                MapMouseDown(sender, e, false);
            }
        }

        public (Location NO, Location SE) GetRectangleLocation()
        {
            return GetRectangleLocation(ActiveRectangle);
        }

        public static (Location NO, Location SE) GetRectangleLocationFromRectangle(MapPolygon Rectangle)
        {
            Location[] locations = Rectangle?.Locations?.ToArray();
            if (locations?.Length >= 4)
            {
                return (locations[0], locations[2]);
            }
            else
            {
                return (new Location(0, 0), new Location(0, 0));
            }
        }

        public (Location NO, Location SE) GetRectangleLocation(MapPolygon Rectangle)
        {
            Location[] locations = Rectangle?.Locations?.ToArray();
            if (locations?.Length >= 4)
            {
                return (locations[0], locations[2]);
            }
            else
            {
                return (SavedNo_Placement, SavedSe_Placement);
            }
        }

        public void SetRectangleLocation(Location NO, Location SE)
        {
            SetRectangleLocation(NO, SE, ActiveRectangle);
        }

        public void SetRectangleLocation(Location NO, Location SE, MapPolygon Rectangle = null)
        {
            if (Rectangle == null)
            {
                Rectangle = ActiveRectangle;
            }
            bool DoUpdateNo = true;
            bool DoUpdateSe = true;

            var CurentLocation = GetRectangleLocation(Rectangle);
            if (NO == null || SE == null)
            {

                if (NO == null)
                {
                    DoUpdateNo = false;
                    NO = CurentLocation.NO;
                }
                if (SE == null)
                {
                    DoUpdateSe = false;
                    SE = CurentLocation.SE;
                }
            }
            (bool isIllegal, Location result) IllegalLocation(Location TargetLocation, Location ActualLocation)
            {
                if (Math.Abs(TargetLocation.Longitude) == 180)
                {
                    double MaxLongitude = 179.99999999;
                    if (TargetLocation.Longitude == ActualLocation.Longitude)
                    {
                        return (false, TargetLocation);
                    }
                    if (MouseHitType == HitType.Body || (Keyboard.Modifiers == ModifierKeys.Shift && (IsLeftClick || IsRightClick)))
                    {
                        return (true, TargetLocation);
                    }

                    if (Math.Abs(ActualLocation.Longitude) >= MaxLongitude)
                    {
                        return (true, TargetLocation);
                    }

                    if (TargetLocation.Longitude == 180)
                    {
                        TargetLocation = new Location(TargetLocation.Latitude, MaxLongitude);
                    }
                    if (TargetLocation.Longitude == -180)
                    {
                        TargetLocation = new Location(TargetLocation.Latitude, -MaxLongitude);
                    }
                }
                return (false, TargetLocation);
            }

            (Location NOFix, Location SEFix) FixLocation(Location NO, Location SE)
            {
                if (Math.Abs(NO.Longitude) >= 180 || Math.Abs(SE.Longitude) >= 180)
                {
                    Point MAX_No_PlacementPoint = map.LocationToView(new Location(0, -180));
                    Point Target_No_PlacementPoint = map.LocationToView(NO);
                    Point Target_Se_PlacementPoint = map.LocationToView(SE);
                    Location NOFix;
                    Location SEFix;
                    NOFix = map.ViewToLocation(new Point(map.LocationToView(CurentLocation.NO).X, Target_No_PlacementPoint.Y));
                    SEFix = map.ViewToLocation(new Point(map.LocationToView(CurentLocation.SE).X, Target_Se_PlacementPoint.Y));
                    return (NOFix, SEFix);
                }
                return (NO, SE);
            }

            if (DoUpdateNo && DoUpdateSe && MouseHitType == HitType.Body)
            {
                var NoIsIllegal = IllegalLocation(NO, CurentLocation.NO);
                var SeIsIllegal = IllegalLocation(SE, CurentLocation.SE);
                if (NoIsIllegal.isIllegal || SeIsIllegal.isIllegal)
                {
                    var FixLoc = FixLocation(NO, SE);
                    NO = FixLoc.NOFix;
                    SE = FixLoc.SEFix;
                }
            }

            if (DoUpdateNo)
            {
                var NoIsIllegal = IllegalLocation(NO, CurentLocation.NO);
                if (NoIsIllegal.isIllegal)
                {
                    return;
                }
                else
                {
                    NO = NoIsIllegal.result;
                }
            }

            if (DoUpdateSe)
            {
                var SeIsIllegal = IllegalLocation(SE, CurentLocation.SE);
                if (SeIsIllegal.isIllegal)
                {
                    return;
                }
                else
                {
                    SE = SeIsIllegal.result;
                }
            }

            if (IsSnapToGrid)
            {
                var SnapLocations = SnapToGrid(NO, SE, Rectangle);
                if (DoUpdateNo)
                {
                    NO = SnapLocations.NO;
                }
                if (DoUpdateSe)
                {
                    SE = SnapLocations.SE;
                }
            }

            Rectangle.Locations = new List<Location>() { NO, new Location(SE.Latitude, NO.Longitude), SE, new Location(NO.Latitude, SE.Longitude) };

            if (NO.Latitude == SE.Latitude && NO.Longitude == SE.Longitude)
            {
                Rectangle.Visibility = Visibility.Hidden;
            }
            else if (Rectangle.Visibility == Visibility.Hidden)
            {
                Rectangle.Visibility = Visibility.Visible;
            }
            OnLocationUpdated?.Invoke(this, Rectangle);
        }

        private (Location NO, Location SE) SnapToGrid(Location NO, Location SE, MapPolygon Rectangle)
        {
            int ZoomLevel = 16;//((int)map.ZoomLevel) + 2;
            var NoLocationTiles = Collectif.CoordonneesToTile(NO.Latitude, NO.Longitude, ZoomLevel);
            var SeLocationTiles = Collectif.CoordonneesToTile(SE.Latitude, SE.Longitude, ZoomLevel);

            var NoCornerLocation = Collectif.TileToCoordonnees(NoLocationTiles.X, NoLocationTiles.Y, ZoomLevel);
            var NoCornerLocationPP = Collectif.TileToCoordonnees(NoLocationTiles.X + 1, NoLocationTiles.Y + 1, ZoomLevel);

            if (NoCornerLocationPP.Latitude == NO.Latitude)
            {
                NoCornerLocation.Latitude = NoCornerLocationPP.Latitude;
            }

            if (NoCornerLocationPP.Longitude == NO.Longitude)
            {
                NoCornerLocation.Longitude = NoCornerLocationPP.Longitude;
            }


            Point NOPoint = map.LocationToView(NO);
            Point SEPoint = map.LocationToView(SE);
            int TileXSupplement = 0;
            int TileYSupplement = 0;

            if ((IsLeftClick || IsRightClick) && MouseHitType != HitType.Body)
            {
                if (NOPoint.X < SEPoint.X && NOPoint.Y < SEPoint.Y)
                {
                    Debug.WriteLine("a - " + MouseHitType);
                    if (MouseHitType == HitType.None || MouseHitType == HitType.R || MouseHitType == HitType.LR || MouseHitType == HitType.UR || (MouseHitType == HitType.T && IsRightClick))
                    {
                        TileXSupplement++;
                    }
                    if (MouseHitType == HitType.None || MouseHitType == HitType.B || MouseHitType == HitType.LL || MouseHitType == HitType.UL || MouseHitType == HitType.LR)
                    {
                        TileYSupplement++;
                    }
                }
                if (NOPoint.X > SEPoint.X && NOPoint.Y < SEPoint.Y)
                {
                    Debug.WriteLine("b");
                    TileYSupplement++;
                }
                if (NOPoint.X < SEPoint.X && NOPoint.Y > SEPoint.Y)
                {
                    Debug.WriteLine("c");
                    TileXSupplement++;
                }

            }

            var SeCornerLocation = Collectif.TileToCoordonnees(SeLocationTiles.X + TileXSupplement, SeLocationTiles.Y + TileYSupplement, ZoomLevel);
            var SeCornerLocationPP = Collectif.TileToCoordonnees(SeLocationTiles.X + 1 + TileXSupplement, SeLocationTiles.Y + 1 + TileYSupplement, ZoomLevel);
            if (SeCornerLocationPP.Latitude == SE.Latitude || NoCornerLocation.Latitude == SeCornerLocation.Latitude)
            {
                SeCornerLocation.Latitude = SeCornerLocationPP.Latitude;
            }

            if (SeCornerLocationPP.Longitude == SE.Longitude || NoCornerLocation.Longitude == SeCornerLocation.Longitude)
            {
                SeCornerLocation.Longitude = SeCornerLocationPP.Longitude;
            }

            return (new Location(NoCornerLocation.Latitude, NoCornerLocation.Longitude), new Location(SeCornerLocation.Latitude, SeCornerLocation.Longitude));
        }

        public void CleanRectangleLocations()
        {
            CleanRectangleLocations(ActiveRectangle);
        }

        public int DeleteUnusedRectangles()
        {
            MapPolygon[] TempList = Rectangles.ToArray();
            int NumberOfUnusedRectangleDeleted = 0;
            foreach (MapPolygon Rectangle in TempList)
            {
                var Locations = GetRectangleLocation(Rectangle);
                if (Locations.NO.Latitude == 0 && Locations.NO.Longitude == 0 && Locations.SE.Latitude == 0 && Locations.SE.Longitude == 0)
                {
                    NumberOfUnusedRectangleDeleted++;
                    DeleteRectangle(Rectangle);
                }
            }
            return NumberOfUnusedRectangleDeleted;
        }

        private void CleanRectangleLocations(MapPolygon Rectangle)
        {
            var ActualLocations = GetRectangleLocation(Rectangle);
            Location NO = ActualLocations.NO;
            Location SE = ActualLocations.SE;
            Location NO_Temp = NO;
            Location SE_Temp = SE;
            if (NO.Latitude < SE.Latitude)
            {
                NO = new Location(SE_Temp.Latitude, NO_Temp.Longitude);
                SE = new Location(NO_Temp.Latitude, SE_Temp.Longitude);
            }
            if (NO.Longitude > SE.Longitude)
            {
                NO = new Location(NO.Latitude, SE_Temp.Longitude);
                SE = new Location(SE.Latitude, NO_Temp.Longitude);
            }
            SetRectangleLocation(NO, SE, Rectangle);
        }


        private void SetHandCursor()
        {
            map.Cursor = new Cursor(Collectif.ReadResourceStream("cursors/closedhand.cur"));
        }

        private void SaveCurrentMousePosition(MouseEventArgs e)
        {
            if (Mouse.MiddleButton != MouseButtonState.Pressed)
            {
                OriginClickPlacement = e.GetPosition(map);
                SavedLeftClickPreviousActions = e.GetPosition(map);
                var ActiveRectangleLocations = GetRectangleLocation(ActiveRectangle);
                OriginNoPlacement = map.LocationToView(ActiveRectangleLocations.NO);
                OriginSePlacement = map.LocationToView(ActiveRectangleLocations.SE);
            }
        }


        private void MapMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e, bool UpdateHitType = true)
        {
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                IsRightClick = false;
                IsLeftClick = false;
                SetHandCursor();
            }
            else if (IsRightClick == false && Mouse.RightButton == MouseButtonState.Pressed)
            {
                IsRightClick = true;
                if (UpdateHitType)
                {
                    SaveCurrentMousePosition(e);
                    map.Cursor = Cursors.Cross;
                    SetRectangleLocation(map.ViewToLocation(e.GetPosition(map)), map.ViewToLocation(e.GetPosition(map)));
                }
            }
            else if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                if (sender.GetType() == map.GetType())
                {
                    e.Handled = true;
                }
            }
            else if (IsLeftClick == false && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (IsRightClick) { return; }
                IsLeftClick = true;
                if (UpdateHitType)
                {
                    SaveCurrentMousePosition(e);
                    MouseHitType = SetHitType(e.GetPosition(map));
                }
                ApplyMapCursor();
            }
        }

        private void MapMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            e.Handled = true;
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                return;
            }

            if (e.RightButton == MouseButtonState.Pressed)
            {
                UpdateSelectionRectangle(sender, e);
            }
            else
            {
                IsRightClick = false;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelectionRectangle(sender, e);
            }
            else
            {
                IsLeftClick = false;
            }
        }

        private Point TransformSelectionIfShift(MapPolygon Rectangle, Point targetPoint, int OpositeCorner = 0, string overrideWidthValue = null)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                return targetPoint;
            }

            Point SelectionRectanglePoint = map.LocationToView(Rectangle.Locations.ElementAt(OpositeCorner));
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
            if (XtargetPointIsSuperiorAsSelectionRectanglePoint == false && YtargetPointIsSuperiorAsSelectionRectanglePoint == true)
            {
                width *= -1;
                height *= 1;
            }


            //BAS DROITE
            if (XtargetPointIsSuperiorAsSelectionRectanglePoint == true && YtargetPointIsSuperiorAsSelectionRectanglePoint == true)
            {
                width *= 1;
                height *= 1;
            }

            //HAUT GAUCHE
            if (XtargetPointIsSuperiorAsSelectionRectanglePoint == false && YtargetPointIsSuperiorAsSelectionRectanglePoint == false)
            {
                width *= -1;
                height *= -1;
            }

            //HAUT DROITE
            if (XtargetPointIsSuperiorAsSelectionRectanglePoint == true && YtargetPointIsSuperiorAsSelectionRectanglePoint == false)
            {
                width *= 1;
                height *= -1;
            }

            returnNewTargetPoint.X += width;
            returnNewTargetPoint.Y += height;
            return returnNewTargetPoint;
        }


        private void UpdateSelectionRectangle(object sender, MouseEventArgs e)
        {
            Point mouse_location = e.GetPosition(map);

            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                return;
            }

            if (IsRightClick)
            {
                if (map.Cursor != Cursors.Cross)
                {
                    map.Cursor = Cursors.Cross;
                }
                SetRectangleLocation(null, map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, e.GetPosition(map))));
            }
            else if (IsLeftClick && MouseHitType != HitType.None && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                ResizeOrMoveSelectionRectange(sender, e);
            }
            else
            {
                if (Mouse.LeftButton != MouseButtonState.Pressed && Mouse.RightButton != MouseButtonState.Pressed)
                {
                    MouseHitType = SetHitType(mouse_location);
                }
                ApplyMapCursor();
            }
        }

        private void UpdateVisualCursor(MouseEventArgs e)
        {
            if (MouseHitType != HitType.UR && MouseHitType != HitType.UL && MouseHitType != HitType.LR && MouseHitType != HitType.LL) { return; }
            Point MousePositionPoint = e.GetPosition(map);
            var rectLocation = GetRectangleLocation(ActiveRectangle);

            Point NOPoint = map.LocationToView(rectLocation.NO);
            Point SEPoint = map.LocationToView(rectLocation.SE);

            if ((Math.Abs(NOPoint.X - SEPoint.X) < 5) || (Math.Abs(NOPoint.Y - SEPoint.Y) < 5))
            {
                return;
            }

            Point RectangleCenterPoint = new Point(Math.Floor((NOPoint.X + SEPoint.X) / 2), Math.Floor((NOPoint.Y + SEPoint.Y) / 2));
            bool MouseIsAtRightFromTheCenter = MousePositionPoint.X >= RectangleCenterPoint.X;
            bool MouseIsAboveCenter = MousePositionPoint.Y >= RectangleCenterPoint.Y;

            HitType VisualCursorHitType = HitType.None;
            if (!MouseIsAboveCenter && MouseIsAtRightFromTheCenter)
            {
                VisualCursorHitType = HitType.UR;
            }
            else if (!MouseIsAboveCenter && !MouseIsAtRightFromTheCenter)
            {
                VisualCursorHitType = HitType.UL;
            }
            else if (MouseIsAboveCenter && MouseIsAtRightFromTheCenter)
            {
                VisualCursorHitType = HitType.LR;
            }
            else if (MouseIsAboveCenter && !MouseIsAtRightFromTheCenter)
            {
                VisualCursorHitType = HitType.LL;
            }
            ApplyMapCursor(VisualCursorHitType);
        }

        private void ResizeOrMoveSelectionRectange(object sender, MouseEventArgs e)
        {
            Point mouse_location = ExtandPositionByXUnit(e.GetPosition(map), 4, MouseHitType);
            Location PositionSouris;
            Location PositionToMakeSquareIfShift;
            var rectanglePosition = GetRectangleLocation(ActiveRectangle);
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
                            SavedNo_Placement = rectanglePosition.NO;
                            SavedSe_Placement = rectanglePosition.SE;
                        }
                        SavedNo_PlacementPoint = map.LocationToView(SavedNo_Placement);
                        SavedSe_PlacementPoint = map.LocationToView(SavedSe_Placement);
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
                        SavedNo_Placement = rectanglePosition.NO;
                        SavedSe_Placement = rectanglePosition.SE;
                        SavedNo_PlacementPoint = map.LocationToView(SavedNo_Placement);
                        SavedSe_PlacementPoint = map.LocationToView(SavedSe_Placement);
                        deplacement_X = mouse_location.X - (SavedNo_PlacementPoint.X + (SavedSe_PlacementPoint.X - SavedNo_PlacementPoint.X) * XPourcent);
                        deplacement_Y = mouse_location.Y - (SavedNo_PlacementPoint.Y + (SavedSe_PlacementPoint.Y - SavedNo_PlacementPoint.Y) * YPourcent);
                    }
                    Location TargetNOloc = map.ViewToLocation(new Point(SavedNo_PlacementPoint.X + deplacement_X, SavedNo_PlacementPoint.Y + deplacement_Y));
                    Location TargetSEloc = map.ViewToLocation(new Point(SavedSe_PlacementPoint.X + deplacement_X, SavedSe_PlacementPoint.Y + deplacement_Y));

                    SetRectangleLocation(TargetNOloc, TargetSEloc);
                    SavedLeftClickPreviousActions = e.GetPosition(map);
                    break;
                case HitType.UL:
                    //top left
                    PositionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouse_location, 2));
                    SetRectangleLocation(PositionToMakeSquareIfShift, null);
                    break;
                case HitType.UR:
                    //top right
                    PositionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouse_location, 1));
                    SetRectangleLocation(
                        new Location(PositionToMakeSquareIfShift.Latitude, rectanglePosition.NO.Longitude),
                        new Location(rectanglePosition.SE.Latitude, PositionToMakeSquareIfShift.Longitude)
                        );
                    break;
                case HitType.LR:
                    //Bottom right
                    PositionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouse_location, 0));
                    SetRectangleLocation(null, PositionToMakeSquareIfShift);
                    break;
                case HitType.LL:
                    //Bottom left
                    PositionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouse_location, 3));
                    SetRectangleLocation(
                       new Location(rectanglePosition.NO.Latitude, PositionToMakeSquareIfShift.Longitude),
                       new Location(PositionToMakeSquareIfShift.Latitude, rectanglePosition.SE.Longitude)
                       );
                    break;
                case HitType.L:
                    //left
                    PositionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouse_location, 2, "width"));
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, PositionToMakeSquareIfShift.Longitude);
                    }
                    else
                    {
                        PositionSouris = new Location(rectanglePosition.NO.Latitude, PositionToMakeSquareIfShift.Longitude);
                    }
                    SetRectangleLocation(PositionSouris, null);
                    break;
                case HitType.R:
                    //right
                    PositionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouse_location, 0, "width"));
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, PositionToMakeSquareIfShift.Longitude);
                    }
                    else
                    {
                        PositionSouris = new Location(rectanglePosition.SE.Latitude, PositionToMakeSquareIfShift.Longitude);
                    }
                    SetRectangleLocation(null, PositionSouris);
                    break;
                case HitType.B:
                    //bottom
                    PositionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouse_location, 0, "height"));
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, PositionToMakeSquareIfShift.Longitude);
                    }
                    else
                    {
                        PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, rectanglePosition.SE.Longitude);
                    }
                    SetRectangleLocation(null, PositionSouris);
                    break;
                case HitType.T:
                    //top
                    PositionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouse_location, 2, "height"));
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, PositionToMakeSquareIfShift.Longitude);
                    }
                    else
                    {
                        PositionSouris = new Location(PositionToMakeSquareIfShift.Latitude, rectanglePosition.NO.Longitude);
                    }
                    SetRectangleLocation(PositionSouris, null);
                    break;
            }
            UpdateVisualCursor(e);
        }

    }


}
