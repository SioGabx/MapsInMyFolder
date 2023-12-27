using CefSharp;
using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;

namespace MapsInMyFolder
{
    public partial class MainPage : System.Windows.Controls.Page
    {
        public void RefreshMap()
        {
            SetCurrentLayer(Layers.Current.Id);
        }
        public void RequestReloadPage()
        {
            LayerPanel.ReloadPage();
        }

        public async void ShowLayerWarning(int id)
        {
            int EditedDB_VERSION;
            string EditedDB_SCRIPT;
            string EditedDB_TILE_URL;

            int LastDB_VERSION;
            string LastDB_SCRIPT;
            string LastDB_TILE_URL;


            var DatabaseEditedLayerExecutable = Database.ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'EDITEDLAYERS' WHERE ID = {id}");
            using (DatabaseEditedLayerExecutable.conn)
            {
                using (SQLiteDataReader editedlayers_sqlite_datareader = DatabaseEditedLayerExecutable.Reader)
                {
                    if (!editedlayers_sqlite_datareader.Read())
                    {
                        return;
                    }

                    EditedDB_VERSION = editedlayers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                    EditedDB_SCRIPT = editedlayers_sqlite_datareader.GetStringFromOrdinal("SCRIPT");
                    EditedDB_TILE_URL = editedlayers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");
                }
            }

            var DatabaseLayerExecutable = Database.ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'LAYERS' WHERE ID = {id}");
            using (DatabaseLayerExecutable.conn)
            {
                using (SQLiteDataReader layers_sqlite_datareader = DatabaseLayerExecutable.Reader)
                {
                    layers_sqlite_datareader.Read();
                    LastDB_VERSION = layers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                    LastDB_SCRIPT = layers_sqlite_datareader.GetStringFromOrdinal("SCRIPT");
                    if (string.IsNullOrEmpty(LastDB_SCRIPT))
                    {
                        LastDB_SCRIPT = "";
                    }
                    LastDB_TILE_URL = layers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");
                }
            }

            if (EditedDB_VERSION != LastDB_VERSION)
            {
                bool HasActionToTake = false;
                StackPanel AskMsg = new StackPanel();
                string RemoveSQL = "";

                if (EditedDB_SCRIPT != LastDB_SCRIPT && !string.IsNullOrWhiteSpace(EditedDB_SCRIPT))
                {
                    HasActionToTake = true;
                    TextBlock textBlock = new TextBlock
                    {
                        Text = Languages.Current["layerMessageErrorUpdateScriptChanged"],
                        TextWrapping = TextWrapping.Wrap
                    };
                    AskMsg.Children.Add(textBlock);
                    AskMsg.Children.Add(Collectif.FormatDiffGetScrollViewer(EditedDB_SCRIPT, LastDB_SCRIPT));
                    RemoveSQL += $"'SCRIPT'=NULL";
                }

                if (EditedDB_TILE_URL != LastDB_TILE_URL && !string.IsNullOrWhiteSpace(EditedDB_TILE_URL))
                {
                    HasActionToTake = true;
                    TextBlock textBlock = new TextBlock
                    {
                        Text = Languages.Current["layerMessageErrorUpdateTileURLChanged"],
                        TextWrapping = TextWrapping.Wrap
                    };
                    AskMsg.Children.Add(textBlock);
                    AskMsg.Children.Add(Collectif.FormatDiffGetScrollViewer(EditedDB_TILE_URL, LastDB_TILE_URL));
                    RemoveSQL += $"'TILE_URL'=NULL";
                }

                TextBlock textBlockAsk = new TextBlock
                {
                    Text = Languages.Current["layerMessageErrorUpdateAskFix"],
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeight.FromOpenTypeWeight(600)
                };
                AskMsg.Children.Add(textBlockAsk);
                ContentDialogResult result = ContentDialogResult.Secondary;
                if (HasActionToTake)
                {
                    ContentDialog dialog = Message.SetContentDialog(AskMsg, "MapsInMyFolder", MessageDialogButton.YesNoCancel);

                    result = await dialog.ShowAsync();
                }
                if (result == ContentDialogResult.Primary)
                {
                    Database.ExecuteNonQuerySQLCommand($"UPDATE 'main'.'EDITEDLAYERS' SET 'VERSION'='{LastDB_VERSION}',{RemoveSQL} WHERE ID = {id};");
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    Database.ExecuteNonQuerySQLCommand($"UPDATE 'main'.'EDITEDLAYERS' SET 'VERSION'='{LastDB_VERSION}' WHERE ID = {id};");
                }
                else
                {
                    return;
                }

                RequestReloadPage();
                SetCurrentLayer(Layers.Current.Id);
            }
        }

        public void SetBBOXPreviewRequestUpdate()
        {
            var bbox = mapviewer.ViewRectToBoundingBox(new Rect(0, 0, mapviewer.ActualWidth, mapviewer.ActualHeight));
            Commun.Map.CurentView.NO_Latitude = bbox.North;
            Commun.Map.CurentView.NO_Longitude = bbox.West;
            Commun.Map.CurentView.SE_Latitude = bbox.South;
            Commun.Map.CurentView.SE_Longitude = bbox.East;

            LayerPanel.PreviewRequestUpdate();
            return;
        }
    }
}