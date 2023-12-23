using MapsInMyFolder.Commun;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Marquer les membres comme étant static", Justification = "Used by CEFSHARP")]
    public class LayerCEFSharpLink
    {
        public void ClearCache(string listOfId = "0")
        {
            long DirectorySize = 0;
            string[] splittedListOfId = listOfId.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in splittedListOfId)
            {
                int id_int = int.Parse(str.Trim());
                DirectorySize += MainPage.ClearCache(id_int);
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
                MainPage.DBLayerFavorite(id_int, isAdding);
            }, null);
        }

        public void LayerVisibility(double id = 0, bool isVisible = true)
        {
            //Debug.WriteLine($"Layer_visibility : id={id} & isVisible={isVisible}");
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.DBLayerVisibility(id_int, (isVisible ? "Visible" : "Hidden"));
            }, null);
        }


        public void LayerMakeEdits(double id = 0, double prefilid = -1)
        {
            int id_int = Convert.ToInt32(id);
            int prefilid_int = Convert.ToInt32(prefilid);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainWindow.Instance.FrameLoad_CustomOrEditLayers(id_int, prefilid_int);
            }, null);
        }

        public void LayerSetAsCurrent(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                //Debug.WriteLine("Layer_set_current " + id);
                MainWindow.Instance.MainPage.SetCurrentLayer(id_int);
            }, null);
        }
        public void LayerShowWarningLegacyVersionNewerThanEdited(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.ShowLayerWarning(id_int);
            }, null);
        }

        public void LayerRequestSearchUpdate()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, (SendOrPostCallback)delegate
            {
                MainWindow.Instance.MainPage.SearchLayerStart(true);
            }, null);
        }

        public string LayerRequestGetSearchString()
        {
            return Application.Current.Dispatcher.Invoke(() => MainWindow.Instance.MainPage.SearchGetText(), DispatcherPriority.Send);
        }

        public void LayerRequestRefreshPanel()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainWindow.Instance.MainPage.ReloadPage();
                MainWindow.Instance.MainPage.SearchLayerStart();
            }, null);
        }

        public string LayerGetTilePreviewUrlFromId(double id = 0)
        {
            if (!Settings.layerpanel_livepreview)
            {
                return "";
            }
            async Task<string> LayerGetTilePreviewUrlFromId_Task(double id)
            {
                int id_int = Convert.ToInt32(id);
                DispatcherOperation op = Application.Current.Dispatcher.BeginInvoke(new Func<string>(() => MainWindow.Instance.MainPage.LayerTilePreview_ReturnUrl(id_int)));
                await op;
                return op.Result.ToString();
            }
            return LayerGetTilePreviewUrlFromId_Task(id).Result;
        }
    }
}
