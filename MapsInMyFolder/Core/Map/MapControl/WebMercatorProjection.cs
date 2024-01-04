﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;

using System.Windows;

namespace MapsInMyFolder.MapControl
{
    /// <summary>
    /// Spherical Mercator Projection, EPSG:3857.
    /// See "Map Projections - A Working Manual" (https://pubs.usgs.gov/pp/1395/report.pdf), p.41-44.
    /// </summary>
    public class WebMercatorProjection : MapProjection
    {
        public const string DefaultCrsId = "EPSG:3857";

        public WebMercatorProjection()
        {
            CrsId = DefaultCrsId;
            IsNormalCylindrical = true;
            IsWebMercator = true;
            MaxLatitude = YToLatitude(180d);
        }

        public override Vector GetRelativeScale(Location location)
        {
            var k = 1d / Math.Cos(location.Latitude * Math.PI / 180d); // p.44 (7-3)

            return new Vector(k, k);
        }

        public override Point LocationToMap(Location location)
        {
            return new Point(
                Wgs84MetersPerDegree * location.Longitude,
                Wgs84MetersPerDegree * LatitudeToY(location.Latitude));
        }

        public override Location MapToLocation(Point point)
        {
            return new Location(
                YToLatitude(point.Y / Wgs84MetersPerDegree),
                point.X / Wgs84MetersPerDegree);
        }

        public static double LatitudeToY(double latitude)
        {
            if (latitude <= -90d)
            {
                return double.NegativeInfinity;
            }

            if (latitude >= 90d)
            {
                return double.PositiveInfinity;
            }

            return Math.Log(Math.Tan((latitude + 90d) * Math.PI / 360d)) * 180d / Math.PI;
        }

        public static double YToLatitude(double y)
        {
            return 90d - (Math.Atan(Math.Exp(-y * Math.PI / 180d)) * 360d / Math.PI);
        }
    }
}