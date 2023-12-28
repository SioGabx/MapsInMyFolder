using MapsInMyFolder.Commun;
using NetVips;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    public partial class Downloader
    {
        static Image AddGraphicalScale(NetVips.Image image, ScaleInfo scaleInfo)
        {
            if (!scaleInfo.doDrawScale)
            {
                return image;
            }

            int pixelLength = (int)Math.Round(scaleInfo.drawScalePixelLength);
            double[] backgroundColor = { 255d, 255d, 255d, 200d };
            double[] lineFirstPart = { 0d, 0d, 0d };
            double[] lineSecondPart = { 128d, 128d, 128d };
            int height = 20;
            int margin = 5;
            string font = "Segoe UI";
            int fontDPI = 80;
            int lineHeight = 2;

            using var lText = Image.Text("0", font, 50, null, Enums.Align.Centre, true, fontDPI, 0, null, true);
            using var rText = Image.Text($"{scaleInfo.drawScaleEchelle}m", font, 50, null, Enums.Align.Centre, true, fontDPI, 0, null, true);
            int width = pixelLength + lText.Width + rText.Width + margin * 4;

            using var scaleBackground = Image.Black(width, height, 4).NewFromImage(backgroundColor);
            using var scaleBackgroundSrgb = scaleBackground.Copy(interpretation: Enums.Interpretation.Srgb);
            using var scaleBackgroundSrgbWithLText = scaleBackgroundSrgb.Composite2(lText, Enums.BlendMode.Over, margin, (int)Math.Round((double)height / 2 - (double)lText.Height / 2));
            using var scaleBackgroundSrgbWithRText = scaleBackgroundSrgbWithLText.Composite2(rText, Enums.BlendMode.Over, scaleBackgroundSrgb.Width - rText.Width - margin, (int)Math.Round((double)height / 2 - (double)rText.Height / 2));
            using var lineBase = Image.Black(pixelLength, lineHeight);
            using var line = lineBase.NewFromImage(lineFirstPart);
            using var lineBase2 = Image.Black((int)Math.Round((double)pixelLength / 2), lineHeight);
            using var line2 = lineBase2.NewFromImage(lineSecondPart);

            using var finalLine = line.Insert(line2, line.Width - (int)Math.Round((double)pixelLength / 2), 0, false);
            using var scaleBackgroundWidthAllElements = scaleBackgroundSrgbWithRText.Composite2(finalLine, Enums.BlendMode.Atop, 2 * margin + lText.Width, (int)Math.Round((double)height / 2 - (double)finalLine.Height / 2));

            return image.Composite(scaleBackgroundWidthAllElements, Enums.BlendMode.Over, margin, image.Height - (height + margin), Enums.Interpretation.Srgb, false);
        }
        private async static Task Assemblage(int id)
        {
            UpdateDownloadPanel(id, $"{Languages.Current["downloadStateAssembly"]}  1/2", "0", true, Status.assemblage);
            DownloadEngine currentEngine = DownloadEngine.GetEngineById(id);
            string format = currentEngine.format;
            string saveDirectory = currentEngine.saveDirectory;
            string saveTempFilename = currentEngine.fileTempName;
            string saveFilename = currentEngine.fileName;
            int tileSize = currentEngine.tileSize;

            var rognageInfo = Trimer.GetTrimValue(
                currentEngine.location["NO_Latitude"],
                currentEngine.location["NO_Longitude"],
                currentEngine.location["SE_Latitude"],
                currentEngine.location["SE_Longitude"],
                currentEngine.zoom,
                tileSize);

            await Task.Run(() =>
            {
                Cache.MaxMem = 0;
                Cache.Trace = false;
                if (currentEngine.state == Status.error)
                {
                    return;
                }

                using var image = EngineTilesToSingleImage(currentEngine);
                if (image == null)
                {
                    return;
                }
                UpdateDownloadPanel(id, Languages.Current["downloadStateCropping"], "0", true, Status.rognage);
                Cache.MaxFiles = 0;

                using var imageRognerBase = Image.Black(rognageInfo.width, rognageInfo.height);
                using var imageRogner = imageRognerBase.Insert(image, -rognageInfo.NO_decalage.X, -rognageInfo.NO_decalage.Y);
                using var imageRedime = ResizeImage(currentEngine, imageRogner, rognageInfo.width, rognageInfo.height);
                using var imageWithScale = AddGraphicalScale(imageRedime, currentEngine.scaleInfo);
                if (currentEngine.state == Status.error)
                {
                    return;
                }
                SaveImage(currentEngine, imageWithScale);
                UpdateDownloadPanel(id, Languages.Current["downloadStateFreeingResourcesProgress"], "100", true, Status.cleanup);

                Debug.WriteLine(
                    "NetVips.Cache.Size" + " : " + Cache.Size + "\n" +
                    "NetVips.Cache.Max" + " : " + Cache.Max + "\n" +
                    "NetVips.Cache.MaxMem" + " : " + Cache.MaxMem + "\n" +
                    "NetVips.Cache.MaxFiles" + " : " + Cache.MaxFiles + "\n" +
                    "NetVips.Stats.Mem" + " : " + Stats.Mem + "\n" +
                    "NetVips.Stats.Files" + " : " + Stats.Files + "\n");

                GC.Collect(9999, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            });

            UpdateDownloadPanel(id, Languages.Current["downloadStateFinalization"], "100", true, Status.cleanup);
            DownloadFinish(id);
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                CheckifMultipleDownloadInProgress();
                Taskbar.ProgressValue = 0;
            }, null);
        }

        static private Image ResizeImage(DownloadEngine currentEngine, NetVips.Image imageRogner, double width, double height)
        {
            try
            {
                if (currentEngine.resizeWidth != -1 && currentEngine.resizeHeignt != -1)
                {
                    UpdateDownloadPanel(currentEngine.id, Languages.Current["downloadStateResizing"], "0", true, Status.rognage);
                    double hrink = currentEngine.resizeHeignt / height;
                    double Vrink = currentEngine.resizeWidth / width;

                    if (currentEngine.resizeHeignt == Math.Round(height * Vrink) || currentEngine.resizeWidth == Math.Round(width * hrink))
                    {
                        //Uniform resizing
                        return imageRogner.Resize(hrink);
                    }
                    else
                    {
                        //Deform resizing
                        return imageRogner.ThumbnailImage(currentEngine.resizeWidth, currentEngine.resizeHeignt, size: Enums.Size.Force);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateDownloadPanel(currentEngine.id, Languages.Current["downloadStateResizingError"], "", true, Status.error, ex.Message);
            }
            return imageRogner;
        }

        private static Image EngineTilesToSingleImage(DownloadEngine curent_engine)
        {
            var layers = Layers.GetLayerById(curent_engine.layerid);
            string format = curent_engine.format;
            string save_temp_directory = curent_engine.saveTempDirectory;
            string save_directory = curent_engine.saveDirectory;
            string save_temp_filename = curent_engine.fileTempName;
            string save_filename = curent_engine.fileName;
            int tile_size = curent_engine.tileSize;

            var NO_tile = Collectif.CoordonneesToTile(curent_engine.location["NO_Latitude"], curent_engine.location["NO_Longitude"], curent_engine.zoom);
            var SE_tile = Collectif.CoordonneesToTile(curent_engine.location["SE_Latitude"], curent_engine.location["SE_Longitude"], curent_engine.zoom);
            int NO_x = NO_tile.X;
            int NO_y = NO_tile.Y;
            int SE_x = SE_tile.X;
            int SE_y = SE_tile.Y;
            int decalage_x = SE_x - NO_x;
            int decalage_y = SE_y - NO_y;

            Cache.Max = 0;
            Cache.MaxFiles = 0;
            Cache.MaxMem = 0;
            List<Image> verticalArray = new List<Image>();

            for (int decalage_boucle_for_y = 0; decalage_boucle_for_y <= decalage_y; decalage_boucle_for_y++)
            {
                int tuile_x = NO_x;
                int tuile_y = NO_y + decalage_boucle_for_y;
                string filename = save_temp_directory + tuile_x + "_" + tuile_y + "." + format;
                Image tempsimage = Image.Black(1, 1);
                List<Image> horizontalArray = new List<Image>();

                for (int decalage_boucle_for_x = 0; decalage_boucle_for_x <= decalage_x; decalage_boucle_for_x++)
                {
                    tuile_x = NO_x + decalage_boucle_for_x;
                    filename = save_temp_directory + tuile_x + "_" + tuile_y + "." + format;
                    FileInfo filinfo = new FileInfo(filename);

                    if (filinfo.Exists && filinfo.Length != 0)
                    {
                        try
                        {
                            tempsimage = Image.NewFromFile(filename);

                            if (tempsimage.Width != tile_size)
                            {
                                double shrinkvalue = tile_size / tempsimage.Width;
                                tempsimage = tempsimage.Resize(shrinkvalue);
                            }

                            if (tempsimage.Bands < 3)
                            {
                                tempsimage = tempsimage.Colourspace(Enums.Interpretation.Srgb);
                            }

                            if (tempsimage.Bands == 3)
                            {
                                tempsimage = tempsimage.Bandjoin(255);
                            }

                            if (Settings.is_in_debug_mode)
                            {
                                var text = Image.Text(tuile_x + " / " + tuile_y, dpi: 150);
                                tempsimage = tempsimage.Composite2(text, Enums.BlendMode.Atop, 0, 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Erreur NetVips : " + ex.ToString());
                            UpdateDownloadPanel(curent_engine.id, $"{Languages.Current["downloadStateAssemblyError"]} P1", "", true, state: Status.error, ex.Message);
                            return null;
                        }
                    }
                    else
                    {
                        //Image not exist, generating empty tile :
                        try
                        {
                            string hexColor = layers?.SpecialsOptions?.BackgroundColor;
                            if (string.IsNullOrWhiteSpace(hexColor))
                            {
                                hexColor = "#000000";
                            }
                            var rgbColor = Collectif.HexValueToSolidColorBrush(hexColor);
                            int alpha = 0;
                            if (curent_engine.finalSaveFormat == "jpeg")
                            {
                                alpha = 255;
                            }
                            tempsimage = Image.Black(tile_size, tile_size) + new double[] { rgbColor.Color.R, rgbColor.Color.G, rgbColor.Color.B, alpha };
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Erreur NetVips : " + ex.ToString());
                            UpdateDownloadPanel(curent_engine.id, $"{Languages.Current["downloadStateAssemblyError"]} P2", "", true, state: Status.error, ex.Message);
                            return null;
                        }
                    }

                    horizontalArray.Add(tempsimage);
                }

                Image tempArrayJoinImage;

                try
                {
                    tempArrayJoinImage = Image.Arrayjoin(horizontalArray.ToArray(), background: new double[] { 255, 255, 255, 255 });

                    horizontalArray.DisposeItems();
                    horizontalArray.Clear();
                }
                catch (Exception ex)
                {
                    UpdateDownloadPanel(curent_engine.id, $"{Languages.Current["downloadStateAssemblyError"]} P3", "", true, Status.error, ex.Message);
                    Debug.WriteLine(ex.Message);
                    return null;
                }

                if (curent_engine.interpretation != Enums.Interpretation.Srgb)
                {
                    try
                    {
                        tempArrayJoinImage = tempArrayJoinImage.Colourspace(curent_engine.interpretation);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error while changing color interpretation to " + curent_engine.interpretation.ToString() + "\n" + ex.Message);
                    }
                }

                verticalArray.Add(tempArrayJoinImage);
                double progress_value = 0;
                double operation_pourcentage_denominateur = decalage_y * decalage_boucle_for_y;

                if (operation_pourcentage_denominateur != 0)
                {
                    progress_value = 100 / decalage_y * decalage_boucle_for_y;
                }
            }

            UpdateDownloadPanel(curent_engine.id, $"{Languages.Current["downloadStateAssembly"]}  2/2", "0", true, Status.assemblage);
            Task.Factory.StartNew(() => Thread.Sleep(300));

            Image image = Image.Black((decalage_x * tile_size) + 1, 1);

            try
            {
                double max_res = 0;

                for (int i = 0; i < verticalArray.Count; i++)
                {
                    if (verticalArray[i].Xres > max_res)
                    {
                        max_res = verticalArray[i].Xres;
                    }
                }

                for (int i = 0; i < verticalArray.Count; i++)
                {
                    if (verticalArray[i].Xres != max_res)
                    {
                        verticalArray[i] = Image.Black(tile_size, tile_size);
                    }
                }
                Image[] ImagesVerticalArray = verticalArray.ToArray();
                image = Image.Arrayjoin(verticalArray.ToArray(), across: 1);

                ImagesVerticalArray.DisposeItems();
                verticalArray.DisposeItems();
                verticalArray.Clear();
            }
            catch (Exception ex)
            {
                UpdateDownloadPanel(curent_engine.id, $"{Languages.Current["downloadStateAssemblyError"]} P4", "", true, Status.error, ex.Message);
                Debug.WriteLine(ex.Message);
            }

            return image;
        }

        public static VOption GetSaveVOption(string final_saveformat, int quality, int? tile_size)
        {
            if (quality <= 0)
            {
                quality = 1;
            }

            VOption saving_options;
            if (final_saveformat == "png")
            {
                saving_options = new VOption {
                    { "compression", quality },
                    { "interlace", true },
                    { "strip", true },
                };
            }
            else if (final_saveformat == "jpeg")
            {
                saving_options = new VOption {
                    { "Q", quality },
                    { "interlace", true },
                    { "optimize_coding", true },
                    { "strip", true },
                };
            }
            else if (final_saveformat == "tiff")
            {
                saving_options = new VOption {
                    { "Q", quality },
                    { "tileWidth", tile_size },
                    { "tileHeight", tile_size },
                    { "compression", "jpeg" },
                    { "interlace", true },
                    { "tile", true },
                    { "pyramid", true },
                    { "bigtif", true }
                };
            }
            else
            {
                saving_options = new VOption();
            }
            return saving_options;
        }

        private static void SaveImage(DownloadEngine currentEngine, NetVips.Image imageRogner)
        {
            int tileSize = currentEngine.tileSize;
            string saveTempDirectory = currentEngine.saveTempDirectory;
            string saveDirectory = currentEngine.saveDirectory;
            string saveTempFilename = currentEngine.fileTempName;
            string saveFilename = currentEngine.fileName;

            UpdateDownloadPanel(currentEngine.id, Languages.Current["downloadStateSaving"], "0", true, Status.enregistrement);
            Thread.Sleep(500);
            var progress = new Progress<int>(percent => UpdateDownloadPanel(currentEngine.id, "", Convert.ToString(percent)));
            imageRogner.SetProgress(progress);
            string imageTempsAssemblagePath = Path.Combine(saveTempDirectory, saveTempFilename);

            if (Directory.Exists(saveTempDirectory))
            {
                if (File.Exists(imageTempsAssemblagePath))
                {
                    File.Delete(imageTempsAssemblagePath);
                }
            }
            else
            {
                Directory.CreateDirectory(saveTempDirectory);
            }

            try
            {
                imageRogner.WriteToFile(imageTempsAssemblagePath, Downloader.GetSaveVOption(currentEngine.finalSaveFormat, currentEngine.quality, tileSize));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                UpdateDownloadPanel(currentEngine.id, Languages.Current["downloadStateSavingError"], "", true, Status.error, ex.Message);
            }
            if (File.Exists(imageTempsAssemblagePath))
            {
                UpdateDownloadPanel(currentEngine.id, Languages.Current["downloadStateMoving"], "", true, Status.progress);
                string targetFilePath = Path.Combine(saveDirectory, saveFilename);
                if (Directory.Exists(saveDirectory))
                {
                    if (File.Exists(targetFilePath))
                    {
                        File.Delete(targetFilePath);
                        foreach (DownloadEngine eng in DownloadEngine.GetEngineList())
                        {
                            string engineFilePath = Path.Combine(eng.saveDirectory, eng.fileName);
                            if (eng.state == Status.success && engineFilePath == targetFilePath)
                            {
                                UpdateDownloadPanel(eng.id, Languages.Current["downloadStateReplaced"], "0", true, Status.deleted);
                                Database.DB_Download_Update(eng.dbid, "INFOS", Collectif.HTMLEntities(Languages.Current["downloadStateReplaced"]));
                            }
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(saveDirectory);
                }
                string finalFilePath = Path.Combine(saveDirectory, saveFilename);
                if (Directory.Exists(saveDirectory))
                {
                    if (File.Exists(finalFilePath))
                    {
                        File.Delete(finalFilePath);
                    }
                    File.Move(imageTempsAssemblagePath, finalFilePath);
                }
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    Message.NoReturnBoxAsync(Languages.GetWithArguments("downloadMessageErrorTileAssembly", saveFilename), Languages.Current["dialogTitleOperationFailed"]);
                }, null);
            }
        }
    }
}
