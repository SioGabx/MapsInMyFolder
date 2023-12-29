using MapsInMyFolder.Commun;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Marquer les membres comme étant static", Justification = "Used by CEFSHARP")]
    public class LayerLink
    {
        private UserControls.LayersPanel LayersPanel { get; }
        private MapControl.Map MapViewer { get; }

        public LayerLink(UserControls.LayersPanel LayersPanel, MapControl.Map MapViewer)
        {
            this.LayersPanel = LayersPanel;
            this.MapViewer = MapViewer;
        }

        public void ClearCache(string listOfId = "0")
        {
            long DirectorySize = 0;
            string[] splittedListOfId = listOfId.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in splittedListOfId)
            {
                int id_int = int.Parse(str.Trim());
                DirectorySize += Layers.ClearCache(id_int);
                Debug.WriteLine("Clear_cache layer " + id_int);
            }
            if (DirectorySize >= 0)
            {
                string memoryFreed = Collectif.FormatBytes(DirectorySize);
                string cacheCleanedMessage = "";

                if (splittedListOfId.Length == 1)
                {
                    cacheCleanedMessage = Languages.GetWithArguments("layerMessageCacheCleared", Layers.GetLayerById(int.Parse(splittedListOfId[0])).Name, memoryFreed);
                }
                else
                {
                    cacheCleanedMessage = Languages.GetWithArguments("layerMessageCachesCleared", memoryFreed);
                }
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    Message.NoReturnBoxAsync(cacheCleanedMessage, Languages.Current["dialogTitleOperationSuccess"]);
                }, null);
            }
        }


        public void LayerFavorite(double id = 0, bool isAdding = true)
        {
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                Layers.SetAsFavorite(id_int, isAdding);
            }, null);
        }

        public void LayerVisibility(double id = 0, bool isVisible = true)
        {
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                Layers.SetVisibility(id_int, isVisible ? "Visible" : "Hidden");
            }, null);
        }


        public void LayerMakeEdits(double id, string EditMode)
        {
            int id_int = Convert.ToInt32(id);
            if (!Enum.TryParse(EditMode, out CustomOrEditLayersPage.EditingMode EditingMode))
            {
                EditingMode = CustomOrEditLayersPage.EditingMode.New;
            }
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainWindow.Instance.FrameLoad_CustomOrEditLayers(id_int, EditingMode);
            }, null);
        }

        public void LayerSetAsCurrent(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                LayersPanel.OnSetCurrentLayerEvent(id_int);
            }, null);
        }
        public void LayerShowWarningLegacyVersionNewerThanEdited(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.Instance.ShowLayerWarning(id_int);
            }, null);
        }

        public void LayerRequestSearchUpdate()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, (SendOrPostCallback)delegate
            {
                LayersPanel.LayersSearchBar.SearchLayerStart(true);

            }, null);
        }

        public string LayerRequestGetSearchString()
        {
            return Application.Current.Dispatcher.Invoke(() => LayersPanel.LayersSearchBar.SearchGetText(), DispatcherPriority.Send);
        }

        public void LayerRequestRefreshPanel()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                LayersPanel.Reload();
                LayersPanel.LayersSearchBar.SearchLayerStart();
            }, null);
        }

        public string LayerGetTilePreviewUrlFromId(double id = 0)
        {
            if (!Settings.layerpanel_livepreview)
            {
                return "";
            }
            string LayerGetTilePreviewUrlFromId_Task(double id)
            {
                int id_int = Convert.ToInt32(id);

                if (MapViewer is null)
                {
                    return string.Empty;
                }
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    double TargetZoomLevel = MapViewer.TargetZoomLevel;
                    double Latitude = MapViewer.Center.Latitude;
                    double Longitude = MapViewer.Center.Longitude;
                    return Layers.PreviewGetUrl(id_int, TargetZoomLevel, Latitude, Longitude);
                }, DispatcherPriority.Normal);
            }
            return LayerGetTilePreviewUrlFromId_Task(id);
        }
    }
}
