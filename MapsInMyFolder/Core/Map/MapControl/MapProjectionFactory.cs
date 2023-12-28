// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

namespace MapsInMyFolder.MapControl
{
    public class MapProjectionFactory
    {
        public virtual MapProjection GetProjection(string crsId)
        {
            MapProjection projection = null;

            switch (crsId)
            {
                case WorldMercatorProjection.DefaultCrsId:
                    projection = new WorldMercatorProjection();
                    break;

                case WebMercatorProjection.DefaultCrsId:
                    projection = new WebMercatorProjection();
                    break;


                default:
                    break;
            }

            return projection;
        }
    }
}
