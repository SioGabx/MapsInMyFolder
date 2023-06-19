using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;

namespace MapsInMyFolder.Commun
{
    public class TileLocationCalculator
    {
        public static double[] TransformLocationFromWGS84(string TargetWkt, double Latitude, double Longitude)
        {
            string WGS84Wkt = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";
            return TransformLocation(WGS84Wkt, TargetWkt, Latitude, Longitude);
        }

        public static double[] TransformLocation(string OriginWkt, string TargetWkt, double ProjX, double ProjY)
        {
            //Debug.WriteLine(ProjectedCoordinateSystem.WebMercator.WKT);
            try
            {
                double[] LatLongLocationToConvert = new double[] { ProjX, ProjY };
                var CoordinateSystemFactory = new CoordinateSystemFactory();
                var OriginCoordinateSystem = CoordinateSystemFactory.CreateFromWkt(OriginWkt);
                var TargetCoordinateSystem = CoordinateSystemFactory.CreateFromWkt(TargetWkt);

                var CoordinateTransformation = new CoordinateTransformationFactory();
                var ICoordinateTransformation = CoordinateTransformation.CreateFromCoordinateSystems(OriginCoordinateSystem, TargetCoordinateSystem);
                return ICoordinateTransformation.MathTransform.Transform(LatLongLocationToConvert);
            }
            catch (Exception ex)
            {
                Javascript.Functions.PrintError("TransformLocation : " + ex.ToString());
                return null;
            }
        }


    }
}
