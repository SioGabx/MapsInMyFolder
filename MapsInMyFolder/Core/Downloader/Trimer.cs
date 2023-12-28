using MapsInMyFolder.Commun;
using System;

namespace MapsInMyFolder
{
    public class Trimer
    {
        public (int X, int Y) NO_decalage;
        public (int X, int Y) SE_decalage;
        public int width;
        public int height;
        public Trimer((int X, int Y) NO_decalage, (int X, int Y) SE_decalage, int width, int height)
        {
            this.width = width;
            this.height = height;
            this.SE_decalage = SE_decalage;
            this.NO_decalage = NO_decalage;
        }

        public static Trimer GetTrimValue(double NO_Latitude, double NO_Longitude, double SE_Latitude, double SE_Longitude, int zoom, int? tile_width)
        {
            int tile_width_NotNull = tile_width ?? 256;
            (int X, int Y) GetTrimFromLocation(double Latitude, double Longitude)
            {
                var list_of_tile_number_from_given_lat_and_long = Collectif.CoordonneesToTile(Latitude, Longitude, zoom);

                var CoinsHautGaucheLocationFromTile = Collectif.TileToCoordonnees(list_of_tile_number_from_given_lat_and_long.X, list_of_tile_number_from_given_lat_and_long.Y, zoom);
                double longitude_coins_haut_gauche_curent_tileX = CoinsHautGaucheLocationFromTile.Longitude;
                double latitude_coins_haut_gauche_curent_tileY = CoinsHautGaucheLocationFromTile.Latitude;

                var NextCoinsHautGaucheLocationFromTile = Collectif.TileToCoordonnees(list_of_tile_number_from_given_lat_and_long.X + 1, list_of_tile_number_from_given_lat_and_long.Y + 1, zoom);
                double longitude_coins_haut_gauche_next_tileX = NextCoinsHautGaucheLocationFromTile.Longitude;
                double latitude_coins_haut_gauche_next_tileY = NextCoinsHautGaucheLocationFromTile.Latitude;

                double longitude_decalage = Math.Abs(Longitude - longitude_coins_haut_gauche_curent_tileX) * 100 / Math.Abs(longitude_coins_haut_gauche_curent_tileX - longitude_coins_haut_gauche_next_tileX) / 100;
                double latitude_decalage = Math.Abs(Latitude - latitude_coins_haut_gauche_curent_tileY) * 100 / Math.Abs(latitude_coins_haut_gauche_curent_tileY - latitude_coins_haut_gauche_next_tileY) / 100;
                int decalage_x = Math.Abs(Convert.ToInt32(Math.Round(longitude_decalage * tile_width_NotNull, 0)));
                int decalage_y = Math.Abs(Convert.ToInt32(Math.Round(latitude_decalage * tile_width_NotNull, 0)));
                return (decalage_x, decalage_y);
            }

            var NO_decalage = GetTrimFromLocation(NO_Latitude, NO_Longitude);
            var SE_decalage = GetTrimFromLocation(SE_Latitude, SE_Longitude);
            int NbrtilesInCol = Collectif.CoordonneesToTile(SE_Latitude, SE_Longitude, zoom).X - Collectif.CoordonneesToTile(NO_Latitude, NO_Longitude, zoom).X + 1;
            int NbrtilesInRow = Collectif.CoordonneesToTile(SE_Latitude, SE_Longitude, zoom).Y - Collectif.CoordonneesToTile(NO_Latitude, NO_Longitude, zoom).Y + 1;
            int final_image_width = Math.Abs((NbrtilesInCol * tile_width_NotNull) - (NO_decalage.X + (tile_width_NotNull - SE_decalage.X)));
            int final_image_height = Math.Abs((NbrtilesInRow * tile_width_NotNull) - (NO_decalage.Y + (tile_width_NotNull - SE_decalage.Y)));
            if (final_image_width < 10 || final_image_height < 10)
            {
                final_image_width = 10;
                final_image_height = 10;
            }

            return new Trimer(NO_decalage, SE_decalage, final_image_width, final_image_height);
        }
    }
}
