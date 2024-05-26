using MapsInMyFolder.Core.Database;
using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Properties;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Layers
{
    public static class Loader
    {
        public static IEnumerable<Layer> LoadFromDatabase(Database.Database database, Tables Table)
        {
            string Query = $"SELECT * FROM {Table} ORDER BY {Settings.Default.LayerOrder}";

            foreach (SQLiteDataReader DataReader in database.ExecuteReaderIEnumerable(Query))
            {
                yield return new Layer()
                {
                    LayerId = DataReader.GetInt("ID"),
                    Name = DataReader.GetString("NAME").ToSingleLine(),
                    Identifier = DataReader.GetString("IDENTIFIER").ToSingleLine(),
                    IsFavorite = DataReader.GetBoolean("FAVORITE"),
                    Description = DataReader.GetString("DESCRIPTION"),
                    Tags = DataReader.GetString("TAGS").ToSingleLine(),
                    Country = DataReader.GetString("COUNTRY").ToSingleLine(),
                    TileUrl = DataReader.GetString("TILE_URL").ToSingleLine(),
                    MinZoom = DataReader.GetInt("MIN_ZOOM"),
                    MaxZoom = DataReader.GetInt("MAX_ZOOM"),
                    TilesFormat = DataReader.GetString("FORMAT").ConvertToEnum<Format>(),
                    SiteName = DataReader.GetString("SITE").ToSingleLine(),
                    SiteUrl = DataReader.GetString("SITE_URL").ToSingleLine(),
                    Style = DataReader.GetString("STYLE"),
                    TileSize = DataReader.GetInt("TILE_SIZE"),
                    Script = DataReader.GetString("SCRIPT"),
                    Visibility = DataReader.GetString("VISIBILITY").ConvertToEnum<Display>(),
                    UserAgent = null,
                };

            }

        }
    }
}
