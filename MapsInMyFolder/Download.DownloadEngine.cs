using CefSharp;
using MapsInMyFolder.Commun;
using NetVips;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Shell;

namespace MapsInMyFolder
{
    public class DownloadEngine
    {
        public int id;
        public int dbid;
        public int layerid;
        public IEnumerable<HttpStatusCode> AlloweRequestErrors;
        public IEnumerable<TileProperty> urls;
        public CancellationTokenSource cancellationTokenSource;
        public CancellationToken cancellationToken;
        public string format;
        public string finalSaveFormat;
        public int zoom;
        public Status state;
        public int nbrOfTiles;
        public int nbrOfTilesWaitingForDownloading;
        public string urlBase;
        public string identifier;
        public string saveTempDirectory;
        public string saveDirectory;
        public string fileName;
        public string fileTempName;
        public int tileSize;
        public int quality;
        public Dictionary<string, double> location;
        public int resizeWidth;
        public int resizeHeignt;
        public TileLoader tileLoader;
        public string varContext;
        public Enums.Interpretation interpretation;
        public ScaleInfo scaleInfo;

        public int maxDownloadtilesInParralele; //max_download_tiles_in_parralele
        public int waitingBeforeStartAnotherTile; // waiting_before_start_another_tile_download

        public int skippedPanelUpdate;
        public string lastCommand;
        public string lastCommandNotImportant;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:Les paramètres CancellationToken doivent venir en dernier", Justification = "it is safe to suppress a warning from this rule to avoid a breaking change and it more readible")]
        public DownloadEngine(int id,
                                          int dbid,
                                          int layerid,
                                          IEnumerable<TileProperty> urls,
                                          CancellationTokenSource cancellationTokenSource,
                                          CancellationToken cancellationToken,
                                          string format,
                                          string finalSaveFormat,
                                          int zoom,
                                          string saveTempDirectory,
                                          string saveDirectory,
                                          string fileName,
                                          string fileTempName,
                                          Dictionary<string, double> location,
                                          int resizeWidth, int resizeHeignt, TileLoader tileLoaderGenerator,
                                          Enums.Interpretation interpretation,
                                          ScaleInfo scaleInfo, IEnumerable<HttpStatusCode> AlloweRequestErrors,
                                          string varContext,
                                          int nbrOfTiles = 0,
                                          string urlBase = "",
                                          string identifier = "",
                                          Status state = Status.waitfordownloading,
                                          int? tileSize = null,
                                          int nbrOfTilesWaitingForDownloading = 0,
                                          int quality = 100,
                                          int maxDownloadtilesInParralele = 1,
                                          int waitingBeforeStartAnotherTile = 0)
        {
            if (id != 0)
            {
                this.fileTempName = fileTempName;
                this.fileName = fileName;
                this.state = state;
                this.saveTempDirectory = saveTempDirectory;
                this.saveDirectory = saveDirectory;
                this.format = format;
                this.finalSaveFormat = finalSaveFormat;
                this.zoom = zoom;
                this.id = id;
                this.layerid = layerid;
                this.dbid = dbid;
                this.urls = urls;
                this.cancellationTokenSource = cancellationTokenSource;
                this.cancellationToken = cancellationToken;
                this.nbrOfTiles = nbrOfTiles;
                this.nbrOfTilesWaitingForDownloading = nbrOfTilesWaitingForDownloading;
                this.urlBase = urlBase;
                this.identifier = identifier;
                this.location = location;
                this.tileSize = tileSize ?? 256;
                this.quality = quality;
                this.resizeWidth = resizeWidth;
                this.resizeHeignt = resizeHeignt;
                this.tileLoader = tileLoaderGenerator;
                this.interpretation = interpretation;
                this.scaleInfo = scaleInfo;
                this.AlloweRequestErrors = AlloweRequestErrors;
                this.varContext = varContext;
                this.skippedPanelUpdate = 0;
                this.lastCommand = string.Empty;
                this.lastCommandNotImportant = string.Empty;
                if (maxDownloadtilesInParralele <= 0)
                {
                    this.maxDownloadtilesInParralele = Settings.max_download_tiles_in_parralele;
                }
                else
                {
                    this.maxDownloadtilesInParralele = Math.Min(Math.Min(Settings.max_download_tiles_in_parralele, maxDownloadtilesInParralele), 1);
                }

                this.waitingBeforeStartAnotherTile = Math.Max(Settings.waiting_before_start_another_tile_download, waitingBeforeStartAnotherTile);
            }
        }

        public static Dictionary<int, DownloadEngine> DownloadEngineDictionnary { get; set; } = new Dictionary<int, DownloadEngine>();
        public static int CurrentNumberOfDownload { get; set; } = 0;

        public static int Add(DownloadEngine engine, int id)
        {
            Remove(id);
            DownloadEngineDictionnary.Add(id, engine);
            return DownloadEngineDictionnary.Count;
        }

        public static bool Remove(int id)
        {
            if (DownloadEngineDictionnary.ContainsKey(id))
            {
                DownloadEngineDictionnary.Remove(id);
                return true;
            }
            return false;
        }

        public static void Clear()
        {
            DownloadEngineDictionnary.Clear();
        }

        public static int GetId()
        {
            return DownloadEngineDictionnary.Count + 1;
        }

        public static IEnumerable<DownloadEngine> GetEngineList()
        {
            return DownloadEngineDictionnary.Values;
        }

        public static DownloadEngine GetEngineById(int id)
        {
            if (DownloadEngineDictionnary.TryGetValue(id, out DownloadEngine engine))
            {
                return engine;
            }
            return null;
        }
    }
}
