using MapsInMyFolder.VectorTileRenderer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MapsInMyFolder.Commun
{
    public partial class TileGenerator
    {
        public async Task<HttpResponse> GetTilePBF(int LayerID, string urlBase, int TileX, int TileY, int TileZoom, string save_temp_directory, int render_tile_size, int TextSizeMultiplicateur, double OverflowTextCorrectingValue, bool pbfdisableadjacent = false)
        {
            //System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            HttpResponse returnReponse = HttpResponse.HttpResponseError;
            try
            {
                async Task<T> DelayedDummyResultTask<T>(TimeSpan delay)
                {
                    await Task.Delay(delay);
                    return default(T);
                }

                var PBFRenderingTask = PBFRenderingAsync(0, LayerID, urlBase, TileX, TileY, TileZoom, save_temp_directory, render_tile_size, TextSizeMultiplicateur, OverflowTextCorrectingValue, pbfdisableadjacent);
                var PBFRenderingTaskVsDummyDelayedTask = await Task.WhenAny(PBFRenderingTask, DelayedDummyResultTask<HttpResponse>(TimeSpan.FromSeconds(30)));
                if (PBFRenderingTaskVsDummyDelayedTask == PBFRenderingTask)
                {
                    //if PBFRenderingTask take less thant 30 seconds, check if she failed :
                    if (PBFRenderingTaskVsDummyDelayedTask.IsFaulted)
                    {
                        Debug.WriteLine("PBFRenderingTask : Une erreur s'est produite");
                        return returnReponse;
                    }
                    else
                    {
                        //PBFRenderingTask was the first to complete and has not trown any exeption
                        returnReponse = PBFRenderingTaskVsDummyDelayedTask.Result;
                    }
                }
                else
                {
                    //if PBFRenderingTask take more thant 30 seconds
                    DebugMode.WriteLine("PBFRenderingTask have take more than 30 seconds to complete. Task abord");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetImageAsync error" + ex.ToString());
            }

            return returnReponse;
        }

        static readonly object PBF_RenderingAsync_Locker = new object();
        static readonly object PBF_SetProviders_Locker = new object();
        public async Task<HttpResponse> PBFRenderingAsync(int tache, int LayerID, string urlBase, int TileX, int TileY, int zoom, string save_temp_directory, int render_tile_size, int TextSizeMultiplicateur, double OverflowTextCorrectingValue, bool pbfdisableadjacent = false)
        {
            //System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            DebugMode.WriteLine("Function PBFrender LayerId:" + LayerID + " for tache " + tache);
            int settings_max_tiles_cache_days = Settings.tiles_cache_expire_after_x_days;
            if (LayerID <= 0)
            {
                //disable cache
                settings_max_tiles_cache_days = -1;
            }

            string save_temp_directory_rawBPF = save_temp_directory + "rawPBF/";
            bool cache_tile = false;
            try
            {
                bool do_download_this_tile = true;

                string filename = TileX + "_" + TileY + ".pbf";
                if (!string.IsNullOrEmpty(save_temp_directory))
                {
                    if (!Directory.Exists(save_temp_directory_rawBPF))
                    {
                        Directory.CreateDirectory(save_temp_directory_rawBPF);
                    }
                    if (settings_max_tiles_cache_days != -1)
                    {
                        cache_tile = true;
                        do_download_this_tile = Collectif.CheckIfDownloadIsNeededOrCached(save_temp_directory_rawBPF, filename, settings_max_tiles_cache_days);
                    }
                }
                else
                {
                    DebugMode.WriteLine("Save temp dir est vide, le cache est désactivé" + save_temp_directory);
                }

                Stream StreamPBFFile;
                HttpResponse response;

                string save_filename = save_temp_directory_rawBPF + filename;
                if (do_download_this_tile)
                {
                    Uri uri = new Uri(Collectif.GetUrl.FromTileXYZ(urlBase, TileX, TileY, zoom, LayerID, Collectif.GetUrl.InvokeFunction.getTile));
                    DebugMode.WriteLine("Tache n°" + tache + " : Telechargement u1");
                    response = await Collectif.ByteDownloadUri(uri, LayerID, true);
                    if (response?.Buffer is null)
                    {
                        DebugMode.WriteLine("Tache n°" + tache + " : u1 is null");
                        return response;
                    }

                    try
                    {
                        if (!Directory.Exists(save_temp_directory_rawBPF))
                        {
                            Directory.CreateDirectory(save_temp_directory_rawBPF);
                        }
                        if (!File.Exists(save_filename) && cache_tile)
                        {
                            DebugMode.WriteLine("Ecriture de  : " + save_temp_directory_rawBPF + filename);
                            lock (PBF_RenderingAsync_Locker)
                            {
                                File.WriteAllBytes(save_filename, response.Buffer);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Impossible d'ecrire le fichier : " + save_filename + "\n" + ex.Message);
                    }
                    StreamPBFFile = Collectif.ByteArrayToStream(response.Buffer);
                }
                else
                {
                    lock (PBF_RenderingAsync_Locker)
                    {
                        StreamPBFFile = File.OpenRead(save_filename);
                    }

                    response = new HttpResponse(null, new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK));
                }

                VectorTileRenderer.Style style = PBFGetStyle(LayerID);
                if (style is null)
                {
                    Javascript.PrintError("Tile style est null", LayerID);
                    return HttpResponse.HttpResponseError;
                }

                var provider = new VectorTileRenderer.Sources.PbfTileSource(StreamPBFFile);

                var providers = new List<List<VectorTileRenderer.Sources.PbfTileSource>>
                {
                    new List<VectorTileRenderer.Sources.PbfTileSource>() { null, null, null },
                    new List<VectorTileRenderer.Sources.PbfTileSource>() { null, null, null },
                    new List<VectorTileRenderer.Sources.PbfTileSource>() { null, null, null }
                };

                lock (PBF_SetProviders_Locker)
                {
                    providers[1][1] = provider;
                }

                if (!pbfdisableadjacent)
                {
                    Parallel.Invoke(
                   () => SetProviders(0, 0, TileX - 1, TileY - 1),
                   () => SetProviders(0, 1, TileX - 1, TileY),
                   () => SetProviders(0, 2, TileX - 1, TileY + 1),
                   () => SetProviders(1, 0, TileX, TileY - 1),
                   () => SetProviders(1, 2, TileX, TileY + 1),
                   () => SetProviders(2, 0, TileX + 1, TileY - 1),
                   () => SetProviders(2, 1, TileX + 1, TileY),
                   () => SetProviders(2, 2, TileX + 1, TileY + 1)
                    );
                }

                void SetProviders(int ArrayX, int ArrayY, int ComputedTileX, int ComputedTileY)
                {
                    VectorTileRenderer.Sources.PbfTileSource pbfTileSource = GetProviderFromXYZ(LayerID, urlBase, ComputedTileX, ComputedTileY, zoom, cache_tile, save_temp_directory_rawBPF, save_temp_directory, filename, settings_max_tiles_cache_days).Result;
                    lock (PBF_SetProviders_Locker)
                    {
                        try
                        {
                            providers[ArrayX][ArrayY] = pbfTileSource;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Erreur set providers :" + ex.Message);
                        }
                    }
                }

                ICanvas bitmap = new SkiaCanvas();
                MapsInMyFolder.VectorTileRenderer.Renderer.ICanvasCollisions ReturnCanvasAndCollisions;
                MapsInMyFolder.VectorTileRenderer.Renderer.Collisions ListOfEntitiesCollisions = new MapsInMyFolder.VectorTileRenderer.Renderer.Collisions();
                Renderer.ROptions Roptions = null;
                if (!(providers[1][1] is null))
                {
                    ReturnCanvasAndCollisions = await CreateBitmap(bitmap, 1, 1, providers[1][1], ListOfEntitiesCollisions, true, 3).ConfigureAwait(false);

                    providers[1][1] = null;

                    if (!(ReturnCanvasAndCollisions is null))
                    {
                        bitmap = ReturnCanvasAndCollisions.bitmap;
                        Roptions = ReturnCanvasAndCollisions.Roptions;
                        ListOfEntitiesCollisions = ReturnCanvasAndCollisions.ListOfEntitiesCollisions;
                    }
                }
                else
                {
                    Debug.WriteLine("Error : providers[1][1] was null");
                    return HttpResponse.HttpResponseError;
                }
                DebugMode.WriteLine("Colistion number " + ListOfEntitiesCollisions.CollisionEntity.Count);
                if (!pbfdisableadjacent)
                {
                    for (int i = 2; i > -1; i--)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (i == 1 && j == 1) { continue; }
                            DebugMode.WriteLine("Recherche de colisions");
                            try
                            {
                                if (!(providers[j][i] is null))
                                {
                                    ReturnCanvasAndCollisions = await CreateBitmap(bitmap, j, i, providers[j][i], ListOfEntitiesCollisions).ConfigureAwait(false);
                                    providers[j][i] = null;
                                    if (!(ReturnCanvasAndCollisions is null))
                                    {
                                        bitmap = ReturnCanvasAndCollisions.bitmap;
                                        ListOfEntitiesCollisions.TextElementsList = ReturnCanvasAndCollisions.ListOfEntitiesCollisions.TextElementsList;
                                    }
                                    ReturnCanvasAndCollisions = null;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Tache n°" + tache + " : Erreur " + j + "-" + i + " = " + ex.Message + "\n" + ex.ToString());
                            }
                        }
                    }
                }

                bitmap.DrawTextOnCanvas(ListOfEntitiesCollisions, Roptions);
                ListOfEntitiesCollisions.TextElementsList.Clear();
                ListOfEntitiesCollisions = null;
                Roptions = null;
                if (!(StreamPBFFile is null))
                {
                    StreamPBFFile.Flush();
                    StreamPBFFile.Close();
                    StreamPBFFile.Dispose();
                }
                ReturnCanvasAndCollisions = null;
                async Task<MapsInMyFolder.VectorTileRenderer.Renderer.ICanvasCollisions> CreateBitmap(ICanvas bitmapf, int PosX, int PosY, VectorTileRenderer.Sources.PbfTileSource pbfTileSource, MapsInMyFolder.VectorTileRenderer.Renderer.Collisions collisions, bool createCanvas = false, int NbrTileHeightWidth = 1)
                {
                    if (pbfTileSource != null)
                    {
                        Renderer.ROptions options = new Renderer.ROptions
                        {
                            NbrTileHeightWidth = NbrTileHeightWidth,
                            ImgPositionX = PosX,
                            ImgPositionY = PosY,
                            ImgCenterPositionX = 1,
                            ImgCenterPositionY = 1,
                            TileSize = render_tile_size,
                            OverflowTextCorrectingValue = OverflowTextCorrectingValue,
                            TextSizeMultiplicateur = TextSizeMultiplicateur,
                            GenerateCanvas = createCanvas
                        };
                        style.SetSourceProvider(0, pbfTileSource);
                        MapsInMyFolder.VectorTileRenderer.Renderer.ICanvasCollisions bitmapff = await Renderer.Render(style, bitmapf, 0, 0, zoom, 1, options: options, collisions: collisions);
                        return bitmapff;
                    }
                    else
                    {
                        DebugMode.WriteLine("pdfTileSource not define");
                        return new Renderer.ICanvasCollisions(collisions, bitmapf, null);
                    }
                }
                DebugMode.WriteLine("Tache n°" + tache + " : FinishDraw");
                SkiaCanvas skiaCanvas = new SkiaCanvas();
                BitmapSource img = bitmap.FinishDrawing();
                img.Freeze();
                BitmapSource img_cropped;
                if (img.Width == render_tile_size * 3)
                {
                    Int32Rect int32Rect = new Int32Rect(render_tile_size, render_tile_size, render_tile_size, render_tile_size);
                    CroppedBitmap cb = new CroppedBitmap(img, int32Rect);
                    //double scale_transform = (double)256 / (double)render_tile_size;
                    //TransformedBitmap cb_r = new TransformedBitmap(cb, new ScaleTransform(scale_transform, scale_transform));
                    //DebugMode.WriteLine("FINALLLL SIZE TILE W=" + cb_r.Width + " H=" + cb_r.Height + " SCALETRANSFORM=" + scale_transform);
                    cb.Freeze();
                    //cb_r.Freeze();
                    //img_cropped = cb_r;
                    img_cropped = cb;
                }
                else
                {
                    img_cropped = img;
                }
                img_cropped.Freeze();
                img = null;
                DebugMode.WriteLine("Tache n°" + tache + " : Cropping to byte");

                return new HttpResponse(Collectif.GetBytesFromBitmapSource(img_cropped), response.ResponseMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Tache n°" + tache + " : Erreur f GetProviderFromXYZ" + ex.Message + "\n" + ex.ToString());
            }

            DebugMode.WriteLine("Tache n°" + tache + " : end return null");
            return HttpResponse.HttpResponseError;
        }


        public static MapsInMyFolder.VectorTileRenderer.Style PBFGetStyle(int layerID)
        {
            string styleValueOrUrlOrPath;
            Layers layers = Layers.GetLayerById(layerID);
            try
            {
                styleValueOrUrlOrPath = layers?.class_specialsoptions?.PBFJsonStyle;
            }
            catch (Exception ex)
            {
                Javascript.PrintError("Tile style load Layer : " + ex.Message, layerID);
                return null;
            }
            if (string.IsNullOrEmpty(styleValueOrUrlOrPath))
            {
                Javascript.PrintError("Tile style non défini", layerID);
                return null;
            }

            try
            {
                //if this is a url, then download the style and save it
                if (Uri.IsWellFormedUriString(styleValueOrUrlOrPath, UriKind.Absolute) && Collectif.IsUrlValid(styleValueOrUrlOrPath))
                {
                    string path = Path.Combine(Collectif.GetSaveTempDirectory(layers.class_name, layers.class_identifiant), "layerstyle", styleValueOrUrlOrPath.GetHashCode().ToString() + ".json");
                    if (File.Exists(path))
                    {
                        styleValueOrUrlOrPath = path;
                    }
                    else
                    {
                        //if file not exist, then download it ONCE
                        lock (PBF_RenderingAsync_Locker)
                        {
                            //maybe after the lock the file exist now
                            if (File.Exists(path))
                            {
                                styleValueOrUrlOrPath = path;
                            }
                            else
                            {
                                Debug.WriteLine("Load style from url :" + path);
                                #region downloadStyleFromUrl
                                try
                                {
                                    HttpResponse httpResponse = Collectif.ByteDownloadUri(new Uri(styleValueOrUrlOrPath), layerID, true).Result;
                                    if (httpResponse?.ResponseMessage.IsSuccessStatusCode == true)
                                    {
                                        if (httpResponse.Buffer != null)
                                        {
                                            styleValueOrUrlOrPath = Collectif.ByteArrayToString(httpResponse.Buffer);
                                            if (!string.IsNullOrEmpty(styleValueOrUrlOrPath))
                                            {
                                                //save filetodisk
                                                string path_dir = Path.GetDirectoryName(path);
                                                if (!Directory.Exists(path_dir))
                                                {
                                                    Directory.CreateDirectory(path_dir);
                                                }
                                                File.WriteAllText(path, styleValueOrUrlOrPath);
                                            }
                                        }
                                        else
                                        {
                                            Debug.WriteLine("VectorTileRenderer.Style buffer from url is null");
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine("VectorTileRenderer.Style response from url is null");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }
                                #endregion
                            }
                        }
                    }
                }

                return new MapsInMyFolder.VectorTileRenderer.Style(styleValueOrUrlOrPath);
            }
            catch (Exception ex)
            {
                Javascript.PrintError("Tile style : " + ex.Message, layerID);
            }
            return null;
        }

        async Task<VectorTileRenderer.Sources.PbfTileSource> GetProviderFromXYZ(int layerID, string urlBase, int TileX_tp, int TileY_tp, int zoom, bool cache_tile, string save_temp_directory_rawBPF, string save_temp_directory, string filename, int settings_max_tiles_cache_days)
        {
            if (!(TileX_tp < 0 || TileY_tp < 0 || zoom < 0))
            {
                string prov_filename = TileX_tp + "_" + TileY_tp + ".pbf";
                bool do_download_this_tile_provider = true;
                if (cache_tile)
                {
                    do_download_this_tile_provider = Collectif.CheckIfDownloadIsNeededOrCached(save_temp_directory_rawBPF, prov_filename, settings_max_tiles_cache_days);
                }
                string prov_save_filename = save_temp_directory_rawBPF + prov_filename;
                if (do_download_this_tile_provider)
                {
                    HttpResponse tp_response = HttpResponse.HttpResponseError;
                    try
                    {
                        Uri temp_uri = new Uri(Collectif.GetUrl.FromTileXYZ(urlBase, TileX_tp, TileY_tp, zoom, layerID, Collectif.GetUrl.InvokeFunction.getTile));
                        tp_response = await Collectif.ByteDownloadUri(temp_uri, 0, true).ConfigureAwait(false);
                        if (tp_response?.Buffer is null)
                        {
                            DebugMode.WriteLine("Erreur loading g");
                            return null;
                        }

                        if (!Directory.Exists(save_temp_directory_rawBPF))
                        {
                            Directory.CreateDirectory(save_temp_directory_rawBPF);
                        }

                        if (!File.Exists(prov_save_filename) && cache_tile)
                        {
                            lock (PBF_RenderingAsync_Locker) { File.WriteAllBytes(prov_save_filename, tp_response.Buffer); }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Impossible d'ecrire le fichier : " + save_temp_directory + filename + "\n" + ex.Message);
                    }

                    return new VectorTileRenderer.Sources.PbfTileSource(Collectif.ByteArrayToStream(tp_response.Buffer));
                }
                else
                {

                    lock (PBF_RenderingAsync_Locker)
                    {
                        if (!File.Exists(prov_save_filename))
                        {
                            return null;
                        }
                        int tentatives = 0;
                        do if (File.Exists(prov_save_filename))
                            {
                                Stream StreamPBFFile = null;
                                bool success = false;
                                try
                                {
                                    DebugMode.WriteLine("Trying to read file");
                                    StreamPBFFile = File.OpenRead(prov_save_filename);
                                    success = true;
                                    DebugMode.WriteLine("End read file");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("TileGenerator : Failed to load " + prov_save_filename + " - " + tentatives + "/" + Settings.max_retry_download + "\n Reason : " + ex.Message);
                                    tentatives++;
                                    success = false;
                                    System.Threading.Thread.SpinWait(500);
                                }
                                if (success)
                                {
                                    return new VectorTileRenderer.Sources.PbfTileSource(StreamPBFFile);
                                }
                                StreamPBFFile.Dispose();
                                StreamPBFFile.Close();

                            } while (tentatives < Settings.max_retry_download);
                    }
                }
            }
            return null;
        }
    }
}
