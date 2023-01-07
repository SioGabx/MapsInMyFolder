using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using MapsInMyFolder.VectorTileRenderer;
using ColorMine;
using System.IO;
using System.Diagnostics;
using SkiaSharp;
using System.Net.Http;

namespace MapsInMyFolder.Commun
{
    public partial class TileGenerator
    {
        public async Task<HttpResponse> GetTilePBF(int layerID, string urlBase, int TileX, int TileY, int TileZoom, string save_temp_directory, int settings_max_tiles_cache_days, int render_tile_size, int TextSizeMultiplicateur, double OverflowTextCorrectingValue, bool pbfdisableadjacent = false)
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            //Commun.TileGeneratorSettings.Number_tile_converted++;
            //int convert_number = Commun.TileGeneratorSettings.Number_tile_converted;
            const int convert_number = 0;
            //Random rnd = new Random();
            //int num = rnd.Next();
            //int hatchcode = (num + urlBase + TileX + TileY + TileZoom + save_temp_directory + settings_max_tiles_cache_days + Commun.TileGeneratorSettings.Number_tile_converted).GetHashCode();
            //DebugMode.WriteLine("Ajout de " + convert_number + " dans la liste. Url = " + urlBase);
            //Commun.TileGeneratorSettings.Numbers_tiles_converted.Add(hatchcode, convert_number);

            //DebugMode.WriteLine("PBF File start Converting id=" + convert_number);
            HttpResponse returnReponse = new HttpResponse(null, new HttpResponseMessage(System.Net.HttpStatusCode.Gone));
            try
            {
                //returnReponse = await PBF_RenderingAsync(urlBase, TileX, TileY, TileZoom, save_temp_directory, settings_max_tiles_cache_days).ConfigureAwait(false);

                async Task<T> DelayedDummyResultTask<T>(TimeSpan delay)
                {
                    await Task.Delay(delay);
                    return default(T);
                }

                var somethingTask = PBF_RenderingAsync(convert_number, layerID, urlBase, TileX, TileY, TileZoom, save_temp_directory, settings_max_tiles_cache_days, render_tile_size, TextSizeMultiplicateur, OverflowTextCorrectingValue, pbfdisableadjacent);
                var winner = await Task.WhenAny(somethingTask, DelayedDummyResultTask<HttpResponse>(TimeSpan.FromSeconds(30)));
                DebugMode.WriteLine("fin de la tache " + convert_number);
                if (winner == somethingTask)
                {
                    // hooray it worked
                    if (winner.IsFaulted)
                    {
                        Debug.WriteLine("PBF_RenderingAsync : Une erreur s'est produite");
                        return returnReponse;
                    }
                    else
                    {
                        returnReponse = winner.Result;
                    }
                }
                else
                {
                    DebugMode.WriteLine("PBF File delay espiré id=" + convert_number);
                    //Commun.TileGeneratorSettings.Numbers_tiles_converted.Remove(hatchcode);
                    return returnReponse;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetImageAsync error" + ex.ToString());
                return returnReponse;
            }

            DebugMode.WriteLine("PBF File end rendering id=" + convert_number);
            //Commun.TileGeneratorSettings.Numbers_tiles_converted.Remove(hatchcode);

            //string progress = "";
            //foreach (var number in Commun.TileGeneratorSettings.Numbers_tiles_converted.Values)
            //{
            //   progress = progress + ", " + number;
            //}
            //DebugMode.WriteLine("progress : " + progress);
            return returnReponse;
        }

        static readonly object PBF_RenderingAsync_Locker = new object();
        static readonly object PBF_SetProviders_Locker = new object();
        public async Task<HttpResponse> PBF_RenderingAsync(int tache, int layerID, string urlBase, int TileX, int TileY, int zoom, string save_temp_directory, int settings_max_tiles_cache_days, int render_tile_size, int TextSizeMultiplicateur, double OverflowTextCorrectingValue, bool pbfdisableadjacent = false)
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            DebugMode.WriteLine("Function PBFrender LayerId:" + layerID + " for tache " + tache);
            if (layerID <= 0)
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
                    Uri uri = new Uri(Collectif.GetUrl.FromTileXYZ(urlBase, TileX, TileY, zoom, layerID));
                    DebugMode.WriteLine("Tache n°" + tache + " : Telechargement u1");
                    response = await Collectif.ByteDownloadUri(uri, layerID, true);
                    if (response?.Buffer is null)
                    {
                        DebugMode.WriteLine("Tache n°" + tache + " : u1 is null");
                        return null;
                    }

                    try
                    {
                        if (!Directory.Exists(save_temp_directory_rawBPF))
                        {
                            Directory.CreateDirectory(save_temp_directory_rawBPF);
                        }
                        if (!System.IO.File.Exists(save_filename) && cache_tile)
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
                        StreamPBFFile = System.IO.File.OpenRead(save_filename);
                    }

                    response = new HttpResponse(null, new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK));
                }

                //var style = new MapsInMyFolder.VectorTileRenderer.Style(@"C:\Users\franc\Documents\SharpDevelop Projects\TestPBFtoBitmapConverter\TestPBFtoBitmapConverter\style\accentue.json")
                //{
                //    };

                //string urlorpathorstring = @"https://wxs.ign.fr/an7nvfzojv5wa96dsga5nk8w/static/vectorTiles/styles/PLAN.IGN/gris.json";
                //string stylevalue = Commun.Collectif.ByteArrayToString(Commun.Collectif.ByteDownloadUri(new Uri(urlorpathorstring), layerID).Result.Buffer);
                //C:\Users\franc\Documents\SharpDevelop Projects\TestPBFtoBitmapConverter\TestPBFtoBitmapConverter\style\accentue.json
                string stylevalue;
                Layers layers = Commun.Layers.GetLayerById(layerID);
                try
                {
                    stylevalue = layers?.class_specialsoptions?.PBFJsonStyle;
                }
                catch (Exception ex)
                {
                    Javascript.PrintError("Tile style load Layer : " + ex.Message, layerID);
                    return HttpResponse.HttpResponseError;
                }
                if (string.IsNullOrEmpty(stylevalue))
                {
                    Javascript.PrintError("Tile style non défini", layerID);
                    return HttpResponse.HttpResponseError;
                }
                MapsInMyFolder.VectorTileRenderer.Style style = null;
                try
                {
                    //if this is a url, then download the style and save it
                    if (Uri.IsWellFormedUriString(stylevalue, UriKind.Absolute) && Collectif.IsUrlValid(stylevalue))
                    {
                        string path = Path.Combine(Collectif.GetSaveTempDirectory(layers.class_name, layers.class_identifiant), "layerstyle", stylevalue.GetHashCode().ToString() + ".json");
                        if (System.IO.File.Exists(path))
                        {
                            stylevalue = path;
                        }
                        else
                        {
                            //if file not exist, then download it ONCE
                            lock (PBF_RenderingAsync_Locker)
                            {
                                //maybe after the lock the file exist now
                                if (System.IO.File.Exists(path))
                                {
                                    stylevalue = path;
                                }
                                else
                                {
                                    Debug.WriteLine("Load style from url :" + path);
                                    try
                                    {
                                        HttpResponse httpResponse = Collectif.ByteDownloadUri(new Uri(stylevalue), layerID, true).Result;
                                        if (httpResponse != null && httpResponse.ResponseMessage.IsSuccessStatusCode)
                                        {
                                            if (httpResponse.Buffer != null)
                                            {
                                                stylevalue = Collectif.ByteArrayToString(httpResponse.Buffer);
                                                if (!string.IsNullOrEmpty(stylevalue))
                                                {
                                                    //save filetodisk
                                                    string path_dir = Path.GetDirectoryName(path);
                                                    if (!System.IO.Directory.Exists(path_dir))
                                                    {
                                                        System.IO.Directory.CreateDirectory(path_dir);
                                                    }
                                                    File.WriteAllText(path, stylevalue);
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
                                }
                            }
                        }
                    }

                    style = new MapsInMyFolder.VectorTileRenderer.Style(stylevalue);
                    {
                        //FontDirectory = @"C:\Users\franc\Documents\SharpDevelop Projects\TestPBFtoBitmapConverter\TestPBFtoBitmapConverter\style\font\"
                    }
                }
                catch (Exception ex)
                {
                    Javascript.PrintError("Tile style : " + ex.Message, layerID);
                }
                if (style is null)
                {
                    Javascript.PrintError("Tile style est null", layerID);
                    return HttpResponse.HttpResponseError;
                }

                var provider = new VectorTileRenderer.Sources.PbfTileSource(StreamPBFFile);

                var providers = new List<List<VectorTileRenderer.Sources.PbfTileSource>>
                {
                    new List<VectorTileRenderer.Sources.PbfTileSource>() { null, null, null },
                    new List<VectorTileRenderer.Sources.PbfTileSource>() { null, null, null },
                    new List<VectorTileRenderer.Sources.PbfTileSource>() { null, null, null }
                };

                async Task<VectorTileRenderer.Sources.PbfTileSource> GetProviderFromXYZ(int TileX_tp, int TileY_tp, int indexA, int indexB)
                {
                    DebugMode.WriteLine("Tache n°" + tache + " : GetProvider " + TileX_tp + ", " + TileY_tp);
                    if (!(TileX_tp < 0 || TileY_tp < 0 || zoom < 0))
                    {
                        string prov_filename = TileX_tp + "_" + TileY_tp + ".pbf";
                        bool do_download_this_tile_provider = true;
                        if (cache_tile)
                        {
                            do_download_this_tile_provider = Collectif.CheckIfDownloadIsNeededOrCached(save_temp_directory_rawBPF, prov_filename, settings_max_tiles_cache_days);
                            //do_download_this_tile_provider = Collectif.CheckIfDownloadIsNeededOrCached(save_temp_directory_rawBPF, prov_filename, settings_max_tiles_cache_days);
                        }
                        string prov_save_filename = save_temp_directory_rawBPF + prov_filename;
                        if (do_download_this_tile_provider)
                        {
                            HttpResponse tp_response = HttpResponse.HttpResponseError;
                            try
                            {
                                Uri temp_uri = new Uri(Collectif.GetUrl.FromTileXYZ(urlBase, TileX_tp, TileY_tp, zoom, layerID));
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

                                if (!System.IO.File.Exists(prov_save_filename) && cache_tile)
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
                                if (System.IO.File.Exists(prov_save_filename))
                                {
                                    int tentatives = 0;
                                    bool success = true;
                                    do
                                    {
                                        success = true;
                                        try
                                        {
                                            DebugMode.WriteLine("Trying to read file");
                                            StreamPBFFile = System.IO.File.OpenRead(prov_save_filename);
                                            DebugMode.WriteLine("End read file");
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine("TileGenerator : Failed to load " + prov_save_filename + " - " + tentatives + "/" + Settings.max_retry_download + "\n Reason : " + ex.Message);
                                            tentatives++;
                                            success = false;
                                            System.Threading.Thread.SpinWait(500);
                                        }
                                    } while (!success && tentatives < Settings.max_retry_download);
                                    if (success)
                                    {
                                        return new VectorTileRenderer.Sources.PbfTileSource(StreamPBFFile);
                                    }
                                }
                            }
                        }
                    }
                    return null;
                }

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                lock (PBF_SetProviders_Locker)
                {
                    providers[1][1] = provider;
                }

                //if (!pbfdisableadjacent)
                //{
                //    Parallel.Invoke(
                //   () => providers[0][0] = GetProviderFromXYZ(TileX - 1, TileY - 1, 0, 0).Result,
                //   () => providers[0][1] = GetProviderFromXYZ(TileX - 1, TileY, 0, 1).Result,
                //   () => providers[0][2] = GetProviderFromXYZ(TileX - 1, TileY + 1, 0, 2).Result,
                //   () => providers[1][0] = GetProviderFromXYZ(TileX, TileY - 1, 1, 0).Result,
                //   () => providers[1][2] = GetProviderFromXYZ(TileX, TileY + 1, 1, 2).Result,
                //   () => providers[2][0] = GetProviderFromXYZ(TileX + 1, TileY - 1, 2, 0).Result,
                //   () => providers[2][1] = GetProviderFromXYZ(TileX + 1, TileY, 2, 1).Result,
                //   () => providers[2][2] = GetProviderFromXYZ(TileX + 1, TileY + 1, 2, 2).Result
                //    );
                //} 

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
                    VectorTileRenderer.Sources.PbfTileSource pbfTileSource = GetProviderFromXYZ(ComputedTileX, ComputedTileY, ArrayX, ArrayY).Result;
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

                stopWatch.Stop();
                DebugMode.WriteLine($"Tache n° {tache} : Telechargement de la tuile en {stopWatch.ElapsedMilliseconds} Milliseconds");
                stopWatch.Restart();
                SkiaCanvas canvas = new SkiaCanvas();
                ICanvas bitmap = canvas;
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
                    return null;
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

                stopWatch.Stop();
                //Debug.WriteLine($"Creation des tuiles en {stopWatch.ElapsedMilliseconds} Milliseconds");
                stopWatch.Restart();

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
                stopWatch.Stop();
                // Debug.WriteLine($"Finalizing in {stopWatch.ElapsedMilliseconds} Milliseconds");
                DebugMode.WriteLine("Tache n°" + tache + " : Cropping to byte");

                return new HttpResponse(Collectif.GetBytesFromBitmapSource(img_cropped), response.ResponseMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Tache n°" + tache + " : Erreur f GetProviderFromXYZ" + ex.Message + "\n" + ex.ToString());
            }

            DebugMode.WriteLine("Tache n°" + tache + " : end return null");
            return null;
        }
    }
}
