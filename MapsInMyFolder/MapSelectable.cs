using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

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
        private bool IsRightClick;
        private bool IsLeftClick;

        public event EventHandler<MapPolygon> OnLocationUpdated;
        public event EventHandler<MapPolygon> RectangleGotFocus;
        public event EventHandler<MapPolygon> RectangleLostFocus;
        public event EventHandler<MapPolygon> OnRectangleDeleted;

        public bool DisposeElementsOnUnload = true;
        public bool RectangleCanBeDeleted;
        public bool IsSnapToGrid;
        public bool DisableRectangleMoving;
        public int MinimalNumberOfRectangles;

        private HitType MouseHitType = HitType.None;

        private Location savedNoPlacement = new Location();
        private Location savedSePlacement = new Location();

        private Point OriginClickPlacement = new Point();
        private Point OriginNoPlacement = new Point();
        private Point OriginSePlacement = new Point();
        private Point SavedLeftClickPreviousActions;

        private List<MapPolygon> Rectangles = new List<MapPolygon>();
        public MapPolygon ActiveRectangle { get; set; }

        public MapSelectable(MapControl.Map map, Location startingNO = null, Location startingSE = null, Page page = null)
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
            savedNoPlacement = startingNO;
            savedSePlacement = startingSE;
            AddRectangle(startingNO, startingSE);
            AttachAllEvents(map, page);

            OnLocationUpdated?.Invoke(this, ActiveRectangle);
        }

        public void AttachAllEvents(MapControl.Map map, Page page = null)
        {
            map.MouseDown += MapMouseDown;
            map.MouseMove += MapMouseMove;
            map.MouseWheel += MapMouseWheel;
            map.MouseRightButtonUp += MapMouseRightButtonUp;
            map.MouseLeftButtonUp += MapMouseLeftButtonUp;
            map.KeyDown += MapKeyDown;
            if (page != null)
            {
                page.MouseUp += WindowMouseUp;
                page.MouseMove += WindowMouseMove;
                page.Unloaded += PageUnloaded;
            }
            map.Unloaded += MapUnloaded;
        }

        public void DettachAllEvents(MapControl.Map map, Page page = null)
        {
            MapUnloaded(map, null);
            PageUnloaded(page, null);
        }

        private void PageUnloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MapSelectable : Page unloaded");
            if (DisposeElementsOnUnload && sender is System.Windows.Controls.Page page)
            {
                page.MouseUp -= WindowMouseUp;
                page.MouseMove -= WindowMouseMove;
                page.Unloaded -= PageUnloaded;
            }
        }

        private void MapUnloaded(object sender, RoutedEventArgs e)
        {
            if (DisposeElementsOnUnload)
            {
                map.MouseDown -= MapMouseDown;
                map.MouseMove -= MapMouseMove;
                map.MouseWheel -= MapMouseWheel;
                map.MouseRightButtonUp -= MapMouseRightButtonUp;
                map.MouseLeftButtonUp -= MapMouseLeftButtonUp;
                map.KeyDown -= MapKeyDown;
                map.Unloaded -= MapUnloaded;
            }
        }

        private void MapKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteRectangle(ActiveRectangle);
                e.Handled = true;
            }
        }

        private void MapMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LeftClickUp();
        }

        private void MapMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            RightClickUp();
        }

        private void MapMouseWheel(object sender, MouseWheelEventArgs e)
        {
            MainPage._instance.LayerTilePreview_RequestUpdate();
        }

        public MapPolygon AddRectangle(Location NO, Location SE)
        {
            SetRectangleAsActive(CreateRectangle(NO, SE));
            map.Children.Add(ActiveRectangle);
            ActiveRectangle.Focus();
            return ActiveRectangle;
        }

        public MapPolygon AddRectangle(MapPolygon rectangle)
        {
            AttachEventToRectangle(rectangle);
            map.Children.Add(ActiveRectangle);
            Rectangles.Add(rectangle);
            ActiveRectangle.Focus();
            return ActiveRectangle;
        }

        public void SetRectangleAsActive(MapPolygon rectangle)
        {
            if (rectangle == ActiveRectangle || rectangle is null)
            {
                return;
            }
            if (ActiveRectangle != null)
            {
                ApplyRectangleInactiveColor(ActiveRectangle);
            }
            ApplyRectangleActiveColor(rectangle);
            if (map.Children.Contains(rectangle))
            {
                map.Children.Remove(rectangle);
                map.Children.Add(rectangle);
            }
            ActiveRectangle = rectangle;
        }

        private MapPolygon CreateRectangle(Location NO, Location SE)
        {
            Debug.WriteLine("Create Rectangle");
            MapPolygon rectangle = new MapPolygon
            {
                StrokeDashCap = System.Windows.Media.PenLineCap.Square
            };
            ApplyRectangleActiveColor(rectangle);
            SetRectangleLocation(NO, SE, rectangle);
            CleanRectangleLocations(rectangle);
            AttachEventToRectangle(rectangle);
            Rectangles.Add(rectangle);
            return rectangle;
        }

        public MapPolygon AttachEventToRectangle(MapPolygon rectangle)
        {
            rectangle.MouseLeave += RectangleMouseLeave;
            rectangle.MouseMove += UpdateSelectionRectangle;
            rectangle.MouseDown += RectangleMouseDown;
            rectangle.MouseUp += RectangleMouseUp;
            rectangle.PreviewMouseLeftButtonUp += RectanglePreviewMouseLeftButtonUp;
            rectangle.KeyDown += RectangleKeyDown;
            rectangle.Unloaded += DettachEventToRectangle;
            rectangle.Focusable = true;
            return rectangle;
        }

        private void RectangleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteRectangle(ActiveRectangle);
                e.Handled = true;
            }
        }

        private void RectanglePreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MouseHitType != HitType.None) { return; }
            SetRectangleAsActive(sender as MapPolygon);
        }

        private void RectangleMouseLeave(object sender, MouseEventArgs e)
        {
            if (IsLeftClick || IsRightClick) { return; }
            map.Cursor = Cursors.Arrow;
        }

        private void DettachEventToRectangle(object sender, RoutedEventArgs e)
        {
            if (DisposeElementsOnUnload)
            {
                MapPolygon rectangle = sender as MapPolygon;
                rectangle.MouseLeave -= RectangleMouseLeave;
                rectangle.MouseMove -= UpdateSelectionRectangle;
                rectangle.MouseDown -= RectangleMouseDown;
                rectangle.MouseUp -= RectangleMouseUp;
                rectangle.PreviewMouseLeftButtonUp -= RectanglePreviewMouseLeftButtonUp;
                rectangle.KeyDown -= RectangleKeyDown;
                rectangle.Unloaded -= DettachEventToRectangle;
            }
        }

        public bool DeleteRectangle(MapPolygon rectangle)
        {
            if (Rectangles.Count <= MinimalNumberOfRectangles)
            {
                return false;
            }
            if (!RectangleCanBeDeleted)
            {
                return false;
            }
            if (rectangle is null)
            {
                return false;
            }
            if (map.Children.Contains(rectangle))
            {

                map.Children.Remove(rectangle);
            }
            if (Rectangles.Contains(rectangle))
            {
                Rectangles.Remove(rectangle);
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

            OnRectangleDeleted?.Invoke(this, rectangle);
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
                return (savedNoPlacement, savedSePlacement);
            }
        }

        public void SetRectangleLocation(Location NO, Location SE)
        {
            SetRectangleLocation(NO, SE, ActiveRectangle);
        }

        public void SetRectangleLocation(Location NO, Location SE, MapPolygon rectangle = null)
        {
            rectangle ??= ActiveRectangle;

            bool doUpdateNo = true;
            bool doUpdateSe = true;

            var currentLocation = GetRectangleLocation(rectangle);

            if (NO == null)
            {
                doUpdateNo = false;
                NO = currentLocation.NO;
            }

            if (SE == null)
            {
                doUpdateSe = false;
                SE = currentLocation.SE;
            }

            (bool isIllegal, Location result) IllegalLocation(Location targetLocation, Location actualLocation)
            {
                if (Math.Abs(targetLocation.Longitude) == 180)
                {
                    double maxLongitude = 179.99999999;

                    if (targetLocation.Longitude == actualLocation.Longitude)
                    {
                        return (false, targetLocation);
                    }

                    if (MouseHitType == HitType.Body || (Keyboard.Modifiers == ModifierKeys.Shift && (IsLeftClick || IsRightClick)))
                    {
                        return (true, targetLocation);
                    }

                    if (Math.Abs(actualLocation.Longitude) >= maxLongitude)
                    {
                        return (true, targetLocation);
                    }

                    if (targetLocation.Longitude == 180)
                    {
                        targetLocation = new Location(targetLocation.Latitude, maxLongitude);
                    }

                    if (targetLocation.Longitude == -180)
                    {
                        targetLocation = new Location(targetLocation.Latitude, -maxLongitude);
                    }
                }

                return (false, targetLocation);
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
                    NOFix = map.ViewToLocation(new Point(map.LocationToView(currentLocation.NO).X, Target_No_PlacementPoint.Y));
                    SEFix = map.ViewToLocation(new Point(map.LocationToView(currentLocation.SE).X, Target_Se_PlacementPoint.Y));
                    return (NOFix, SEFix);
                }

                return (NO, SE);
            }

            if (doUpdateNo && doUpdateSe && MouseHitType == HitType.Body)
            {
                var noIsIllegal = IllegalLocation(NO, currentLocation.NO);
                var seIsIllegal = IllegalLocation(SE, currentLocation.SE);

                if (noIsIllegal.isIllegal || seIsIllegal.isIllegal)
                {
                    var fixLoc = FixLocation(NO, SE);
                    NO = fixLoc.NOFix;
                    SE = fixLoc.SEFix;
                }
            }

            if (doUpdateNo)
            {
                var noIsIllegal = IllegalLocation(NO, currentLocation.NO);

                if (noIsIllegal.isIllegal)
                {
                    return;
                }
                else
                {
                    NO = noIsIllegal.result;
                }
            }

            if (doUpdateSe)
            {
                var seIsIllegal = IllegalLocation(SE, currentLocation.SE);

                if (seIsIllegal.isIllegal)
                {
                    return;
                }
                else
                {
                    SE = seIsIllegal.result;
                }
            }

            if (IsSnapToGrid)
            {
                var snapLocations = SnapToGrid(NO, SE, rectangle);

                if (doUpdateNo)
                {
                    NO = snapLocations.NO;
                }

                if (doUpdateSe)
                {
                    SE = snapLocations.SE;
                }
            }

            rectangle.Locations = new List<Location>() { NO, new Location(SE.Latitude, NO.Longitude), SE, new Location(NO.Latitude, SE.Longitude) };

            rectangle.Visibility = NO.Latitude == SE.Latitude && NO.Longitude == SE.Longitude ? Visibility.Hidden : Visibility.Visible;

            OnLocationUpdated?.Invoke(this, rectangle);
        }

        private (Location NO, Location SE) SnapToGrid(Location NO, Location SE, MapPolygon rectangle)
        {
            int zoomLevel = 16;
            var noLocationTiles = Collectif.CoordonneesToTile(NO.Latitude, NO.Longitude, zoomLevel);
            var seLocationTiles = Collectif.CoordonneesToTile(SE.Latitude, SE.Longitude, zoomLevel);

            var noCornerLocation = Collectif.TileToCoordonnees(noLocationTiles.X, noLocationTiles.Y, zoomLevel);
            var noCornerLocationPP = Collectif.TileToCoordonnees(noLocationTiles.X + 1, noLocationTiles.Y + 1, zoomLevel);

            if (noCornerLocationPP.Latitude == NO.Latitude)
            {
                noCornerLocation.Latitude = noCornerLocationPP.Latitude;
            }

            if (noCornerLocationPP.Longitude == NO.Longitude)
            {
                noCornerLocation.Longitude = noCornerLocationPP.Longitude;
            }

            Point NOPoint = map.LocationToView(NO);
            Point SEPoint = map.LocationToView(SE);
            int tileXSupplement = 0;
            int tileYSupplement = 0;

            if ((IsLeftClick || IsRightClick) && MouseHitType != HitType.Body)
            {
                if (NOPoint.X < SEPoint.X && NOPoint.Y < SEPoint.Y)
                {
                    if (MouseHitType == HitType.None || MouseHitType == HitType.R || MouseHitType == HitType.LR || MouseHitType == HitType.UR || (MouseHitType == HitType.T && IsRightClick))
                    {
                        tileXSupplement++;
                    }

                    if (MouseHitType == HitType.None || MouseHitType == HitType.B || MouseHitType == HitType.LL || MouseHitType == HitType.UL || MouseHitType == HitType.LR)
                    {
                        tileYSupplement++;
                    }
                }

                if (NOPoint.X > SEPoint.X && NOPoint.Y < SEPoint.Y)
                {
                    tileYSupplement++;
                }

                if (NOPoint.X < SEPoint.X && NOPoint.Y > SEPoint.Y)
                {
                    tileXSupplement++;
                }
            }

            var seCornerLocation = Collectif.TileToCoordonnees(seLocationTiles.X + tileXSupplement, seLocationTiles.Y + tileYSupplement, zoomLevel);
            var seCornerLocationPP = Collectif.TileToCoordonnees(seLocationTiles.X + 1 + tileXSupplement, seLocationTiles.Y + 1 + tileYSupplement, zoomLevel);

            if (seCornerLocationPP.Latitude == SE.Latitude || noCornerLocation.Latitude == seCornerLocation.Latitude)
            {
                seCornerLocation.Latitude = seCornerLocationPP.Latitude;
            }

            if (seCornerLocationPP.Longitude == SE.Longitude || noCornerLocation.Longitude == seCornerLocation.Longitude)
            {
                seCornerLocation.Longitude = seCornerLocationPP.Longitude;
            }

            return (new Location(noCornerLocation.Latitude, noCornerLocation.Longitude), new Location(seCornerLocation.Latitude, seCornerLocation.Longitude));
        }
        public void CleanRectangleLocations()
        {
            CleanRectangleLocations(ActiveRectangle);
        }

        public int DeleteUnusedRectangles()
        {
            int numberOfUnusedRectangleDeleted = 0;
            foreach (MapPolygon rectangle in Rectangles.ToArray())
            {
                var locations = GetRectangleLocation(rectangle);
                if (locations.NO.Latitude == locations.NO.Longitude || locations.SE.Latitude == locations.SE.Longitude || locations.NO.Latitude == locations.SE.Latitude || locations.NO.Longitude == locations.SE.Longitude)
                {
                        numberOfUnusedRectangleDeleted++;
                    DeleteRectangle(rectangle);
                }
            }

            return numberOfUnusedRectangleDeleted;
        }

        private void CleanRectangleLocations(MapPolygon rectangle)
        {
            var actualLocations = GetRectangleLocation(rectangle);
            Location NO = actualLocations.NO;
            Location SE = actualLocations.SE;
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

            SetRectangleLocation(NO, SE, rectangle);
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
                var activeRectangleLocations = GetRectangleLocation(ActiveRectangle);
                OriginNoPlacement = map.LocationToView(activeRectangleLocations.NO);
                OriginSePlacement = map.LocationToView(activeRectangleLocations.SE);
            }
        }

        private void MapMouseDown(object sender, MouseButtonEventArgs e)
        {
            MapMouseDown(sender, e, true);
        }

        private void MapMouseDown(object sender, MouseButtonEventArgs e, bool updateHitType)
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
                if (updateHitType)
                {
                    SaveCurrentMousePosition(e);
                    map.Cursor = Cursors.Cross;
                    SetRectangleLocation(map.ViewToLocation(e.GetPosition(map)), map.ViewToLocation(e.GetPosition(map)));
                }
            }
            else if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                if (sender is MapControl.Map)
                {
                    e.Handled = true;
                }
            }
            else if (!IsLeftClick && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (IsRightClick)
                {
                    return;
                }
                IsLeftClick = true;
                if (updateHitType)
                {
                    SaveCurrentMousePosition(e);
                    MouseHitType = SetHitType(e.GetPosition(map));
                }
                ApplyMapCursor();
            }
        }

        private void MapMouseMove(object sender, MouseEventArgs e)
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

        private Point TransformSelectionIfShift(MapPolygon rectangle, Point targetPoint, int oppositeCorner = 0, string overrideWidthValue = null)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                return targetPoint;
            }

            Point selectionRectanglePoint = map.LocationToView(rectangle.Locations.ElementAt(oppositeCorner));
            double width = Math.Abs(targetPoint.X - selectionRectanglePoint.X);
            double height = Math.Abs(targetPoint.Y - selectionRectanglePoint.Y);

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

            Point returnNewTargetPoint = new Point(selectionRectanglePoint.X, selectionRectanglePoint.Y);
            bool xTargetPointIsSuperiorAsSelectionRectanglePoint = targetPoint.X > selectionRectanglePoint.X;
            bool yTargetPointIsSuperiorAsSelectionRectanglePoint = targetPoint.Y > selectionRectanglePoint.Y;

            if (xTargetPointIsSuperiorAsSelectionRectanglePoint == false && yTargetPointIsSuperiorAsSelectionRectanglePoint == true)
            {
                width *= -1;
                height *= 1;
            }
            else if (xTargetPointIsSuperiorAsSelectionRectanglePoint == true && yTargetPointIsSuperiorAsSelectionRectanglePoint == true)
            {
                width *= 1;
                height *= 1;
            }
            else if (xTargetPointIsSuperiorAsSelectionRectanglePoint == false && yTargetPointIsSuperiorAsSelectionRectanglePoint == false)
            {
                width *= -1;
                height *= -1;
            }
            else if (xTargetPointIsSuperiorAsSelectionRectanglePoint == true && yTargetPointIsSuperiorAsSelectionRectanglePoint == false)
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
            Point mouseLocation = e.GetPosition(map);

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
                SetRectangleLocation(null, map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouseLocation)));
            }
            else if (IsLeftClick && MouseHitType != HitType.None && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                ResizeOrMoveSelectionRectangle(sender, e);
            }
            else
            {
                if (Mouse.LeftButton != MouseButtonState.Pressed && Mouse.RightButton != MouseButtonState.Pressed)
                {
                    MouseHitType = SetHitType(mouseLocation);
                }
                ApplyMapCursor();
            }
        }

        private void UpdateVisualCursor(MouseEventArgs e)
        {
            if (MouseHitType != HitType.UR && MouseHitType != HitType.UL && MouseHitType != HitType.LR && MouseHitType != HitType.LL)
            {
                return;
            }

            Point mousePositionPoint = e.GetPosition(map);
            var rectLocation = GetRectangleLocation(ActiveRectangle);

            Point NOPoint = map.LocationToView(rectLocation.NO);
            Point SEPoint = map.LocationToView(rectLocation.SE);

            if ((Math.Abs(NOPoint.X - SEPoint.X) < 5) || (Math.Abs(NOPoint.Y - SEPoint.Y) < 5))
            {
                return;
            }

            Point rectangleCenterPoint = new Point(Math.Floor((NOPoint.X + SEPoint.X) / 2), Math.Floor((NOPoint.Y + SEPoint.Y) / 2));
            bool mouseIsAtRightFromTheCenter = mousePositionPoint.X >= rectangleCenterPoint.X;
            bool mouseIsAboveCenter = mousePositionPoint.Y >= rectangleCenterPoint.Y;

            HitType visualCursorHitType = HitType.None;
            if (!mouseIsAboveCenter && mouseIsAtRightFromTheCenter)
            {
                visualCursorHitType = HitType.UR;
            }
            else if (!mouseIsAboveCenter && !mouseIsAtRightFromTheCenter)
            {
                visualCursorHitType = HitType.UL;
            }
            else if (mouseIsAboveCenter && mouseIsAtRightFromTheCenter)
            {
                visualCursorHitType = HitType.LR;
            }
            else if (mouseIsAboveCenter && !mouseIsAtRightFromTheCenter)
            {
                visualCursorHitType = HitType.LL;
            }
            ApplyMapCursor(visualCursorHitType);
        }

        private void ResizeOrMoveSelectionRectangle(object sender, MouseEventArgs e)
        {
            Point mouseLocation = ExtandPositionByXUnit(e.GetPosition(map), 4, MouseHitType);
            Location mousePosition;
            Location positionToMakeSquareIfShift;
            var rectanglePosition = GetRectangleLocation(ActiveRectangle);

            switch (MouseHitType)
            {
                case HitType.Body:
                    double displacementX;
                    double displacementY;
                    Point savedNoPlacementPoint;
                    Point savedSePlacementPoint;
                    double xPercentage = (OriginClickPlacement.X - OriginNoPlacement.X) / (OriginSePlacement.X - OriginNoPlacement.X);
                    double yPercentage = (OriginClickPlacement.Y - OriginNoPlacement.Y) / (OriginSePlacement.Y - OriginNoPlacement.Y);

                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        if (OriginClickPlacement == SavedLeftClickPreviousActions)
                        {
                            savedNoPlacement = rectanglePosition.NO;
                            savedSePlacement = rectanglePosition.SE;
                        }
                        savedNoPlacementPoint = map.LocationToView(savedNoPlacement);
                        savedSePlacementPoint = map.LocationToView(savedSePlacement);
                        displacementX = mouseLocation.X - (savedNoPlacementPoint.X + (savedSePlacementPoint.X - savedNoPlacementPoint.X) * xPercentage);
                        displacementY = mouseLocation.Y - (savedNoPlacementPoint.Y + (savedSePlacementPoint.Y - savedNoPlacementPoint.Y) * yPercentage);
                        if (Math.Abs(displacementX) > Math.Abs(displacementY))
                        {
                            displacementY = 0;
                        }
                        else
                        {
                            displacementX = 0;
                        }
                    }
                    else
                    {
                        savedNoPlacement = rectanglePosition.NO;
                        savedSePlacement = rectanglePosition.SE;
                        savedNoPlacementPoint = map.LocationToView(savedNoPlacement);
                        savedSePlacementPoint = map.LocationToView(savedSePlacement);
                        displacementX = mouseLocation.X - (savedNoPlacementPoint.X + (savedSePlacementPoint.X - savedNoPlacementPoint.X) * xPercentage);
                        displacementY = mouseLocation.Y - (savedNoPlacementPoint.Y + (savedSePlacementPoint.Y - savedNoPlacementPoint.Y) * yPercentage);
                    }
                    Location targetNOloc = map.ViewToLocation(new Point(savedNoPlacementPoint.X + displacementX, savedNoPlacementPoint.Y + displacementY));
                    Location targetSEloc = map.ViewToLocation(new Point(savedSePlacementPoint.X + displacementX, savedSePlacementPoint.Y + displacementY));

                    SetRectangleLocation(targetNOloc, targetSEloc);
                    SavedLeftClickPreviousActions = e.GetPosition(map);
                    break;
                case HitType.UL:
                    //top left
                    positionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouseLocation, 2));
                    SetRectangleLocation(positionToMakeSquareIfShift, null);
                    break;
                case HitType.UR:
                    //top right
                    positionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouseLocation, 1));
                    SetRectangleLocation(
                        new Location(positionToMakeSquareIfShift.Latitude, rectanglePosition.NO.Longitude),
                        new Location(rectanglePosition.SE.Latitude, positionToMakeSquareIfShift.Longitude)
                    );
                    break;
                case HitType.LR:
                    //Bottom right
                    positionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouseLocation, 0));
                    SetRectangleLocation(null, positionToMakeSquareIfShift);
                    break;
                case HitType.LL:
                    //Bottom left
                    positionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouseLocation, 3));
                    SetRectangleLocation(
                        new Location(rectanglePosition.NO.Latitude, positionToMakeSquareIfShift.Longitude),
                        new Location(positionToMakeSquareIfShift.Latitude, rectanglePosition.SE.Longitude)
                    );
                    break;
                case HitType.L:
                    //left
                    positionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouseLocation, 2, "width"));
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        mousePosition = new Location(positionToMakeSquareIfShift.Latitude, positionToMakeSquareIfShift.Longitude);
                    }
                    else
                    {
                        mousePosition = new Location(rectanglePosition.NO.Latitude, positionToMakeSquareIfShift.Longitude);
                    }
                    SetRectangleLocation(mousePosition, null);
                    break;
                case HitType.R:
                    //right
                    positionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouseLocation, 0, "width"));
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        mousePosition = new Location(positionToMakeSquareIfShift.Latitude, positionToMakeSquareIfShift.Longitude);
                    }
                    else
                    {
                        mousePosition = new Location(rectanglePosition.SE.Latitude, positionToMakeSquareIfShift.Longitude);
                    }
                    SetRectangleLocation(null, mousePosition);
                    break;
                case HitType.B:
                    //bottom
                    positionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouseLocation, 0, "height"));
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        mousePosition = new Location(positionToMakeSquareIfShift.Latitude, positionToMakeSquareIfShift.Longitude);
                    }
                    else
                    {
                        mousePosition = new Location(positionToMakeSquareIfShift.Latitude, rectanglePosition.SE.Longitude);
                    }
                    SetRectangleLocation(null, mousePosition);
                    break;
                case HitType.T:
                    //top
                    positionToMakeSquareIfShift = map.ViewToLocation(TransformSelectionIfShift(ActiveRectangle, mouseLocation, 2, "height"));
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        mousePosition = new Location(positionToMakeSquareIfShift.Latitude, positionToMakeSquareIfShift.Longitude);
                    }
                    else
                    {
                        mousePosition = new Location(positionToMakeSquareIfShift.Latitude, rectanglePosition.NO.Longitude);
                    }
                    SetRectangleLocation(mousePosition, null);
                    break;
            }
            UpdateVisualCursor(e);
        }
    }
}
