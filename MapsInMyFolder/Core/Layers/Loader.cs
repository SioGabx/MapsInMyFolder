using MapsInMyFolder.Core.XMLDatabase;
using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Properties;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Windows.Media;

namespace MapsInMyFolder.Core.Layers
{
    public static class Loader
    {
        public static IEnumerable<Layer> LoadFromDatabase(List<Database > database)
        {
           return new List<Layer>() { Layer.Default };
            //foreach (SQLiteDataReader DataReader in database.ExecuteReaderIEnumerable(Query))
            //{
            //    yield return new Layer()
            //    {
            //        LayerId = DataReader.GetInt("ID"),
            //        Name = DataReader.GetString("NAME").ToSingleLine(),
            //        Identifier = DataReader.GetString("IDENTIFIER").ToSingleLine(),
            //        IsFavorite = DataReader.GetBoolean("FAVORITE"),
            //        Description = DataReader.GetString("DESCRIPTION"),
            //        Tags = DataReader.GetString("TAGS").ToSingleLine(),
            //        Country = DataReader.GetString("COUNTRY").ToSingleLine(),
            //        TileUrl = DataReader.GetString("TILE_URL").ToSingleLine(),
            //        MinZoom = DataReader.GetInt("MIN_ZOOM"),
            //        MaxZoom = DataReader.GetInt("MAX_ZOOM"),
            //        TilesFormat = DataReader.GetString("FORMAT").ConvertToEnum<Format>(),
            //        SiteName = DataReader.GetString("SITE").ToSingleLine(),
            //        SiteUrl = DataReader.GetString("SITE_URL").ToSingleLine(),
            //        Style = DataReader.GetString("STYLE"),
            //        TileSize = DataReader.GetInt("TILE_SIZE"),
            //        Script = DataReader.GetString("SCRIPT"),
            //        Visibility = DataReader.GetString("VISIBILITY").ConvertToEnum<Display>(),
            //        BackColor = Color.FromRgb(230, 230, 230),
            //        UserAgent = null,
            //    };

            //}

        }
    }
}
