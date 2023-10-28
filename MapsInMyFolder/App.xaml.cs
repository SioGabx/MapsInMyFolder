using CefSharp;
using CefSharp.SchemeHandler;
using MapsInMyFolder.Commun;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;

namespace MapsInMyFolder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool HasWritePermission(string tempfolderpath)
        {
            try
            {
                if (!Directory.Exists(tempfolderpath))
                {
                    Directory.CreateDirectory(tempfolderpath);
                }
                using (FileStream fs = File.Create(
             Path.Combine(tempfolderpath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose)) { }
            }
            catch (System.UnauthorizedAccessException)
            {
                return false;
            }

            return true;
        }

        public App()
        {
            try
            {
                Settings.LoadSetting();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en");

                var settings = new CefSharp.Wpf.CefSettings();
                string BrowserSubprocessPathPath = Path.GetFullPath("CefSharp.BrowserSubprocess.exe");
                if (File.Exists(BrowserSubprocessPathPath))
                {
                    settings.BrowserSubprocessPath = Path.GetFullPath("CefSharp.BrowserSubprocess.exe");
                }
                string cefsharpTempFolderPath = Path.Combine(Settings.temp_folder, "panel");
                if (Settings.nettoyer_cache_browser_au_demarrage)
                {
                    try
                    {
                        if (Directory.Exists(cefsharpTempFolderPath))
                        {
                            Directory.Delete(cefsharpTempFolderPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to clear the cache." + cefsharpTempFolderPath + "\n" + ex.Message);
                    }
                }
                string layersTempFolderPath = Path.Combine(Settings.temp_folder, "layers");
                if (Settings.nettoyer_cache_layers_au_demarrage)
                {
                    try
                    {
                        if (Directory.Exists(layersTempFolderPath))
                        {
                            Directory.Delete(layersTempFolderPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to clear the cache." + layersTempFolderPath + "\n" + ex.Message);
                    }
                }
                Directory.CreateDirectory(cefsharpTempFolderPath);
                settings.CachePath = cefsharpTempFolderPath;
                if (!Directory.Exists(Settings.working_folder))
                {
                    try
                    {
                        Directory.CreateDirectory(Settings.working_folder);
                    }
                    catch (Exception)
                    {
                        Settings.working_folder = Settings.temp_folder;
                    }

                    if (!HasWritePermission(Settings.working_folder))
                    {
                        Settings.working_folder = Settings.temp_folder;
                    }

                    try
                    {
                        Directory.CreateDirectory(Settings.working_folder);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }

                settings.PersistSessionCookies = false;
                settings.PersistUserPreferences = false;

                settings.RootCachePath = cefsharpTempFolderPath;
                settings.LogFile = Path.Combine(cefsharpTempFolderPath, "internalBrowserLogs.log");
                settings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = "localfolder",
                    DomainName = "cefsharp",
                    SchemeHandlerFactory = new FolderSchemeHandlerFactory(
                        rootFolder: Settings.working_folder,
                        hostName: "cefsharp",
                        defaultPage: "index.html" // will default to index.html
                    )
                });

                //settings.RegisterScheme(new CefCustomScheme
                //{
                //    SchemeName = "mapsinmyfolder",
                //    DomainName = "get",
                //    SchemeHandlerFactory = new CustomSchemeLoadFromApplicationHandlerFactory()
                //});



                settings.CefCommandLineArgs.Add("ignore-certificate-errors"); //cf https://stackoverflow.com/a/35564187/9947331
                //CefSharpSettings.ConcurrentTaskExecution = true;
                if (!Cef.IsInitialized)
                {
                    //Perform dependency check to make sure all relevant resources are in our output directory.
                    //Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
                    Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
                }
            }
            catch (Exception ex)
            {
                Message.NoReturnBoxAsync(ex.ToString(), "Error");
                Collectif.RestartApplication();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Debug.WriteLine(e.Exception.ToString());
                e.Handled = true;
                string Separator = "\n----------------------------------------------------------";
                Separator += Separator + Separator;
                File.AppendAllText(
                    Path.Combine(Settings.working_folder, "crash.log"), Separator +
                    "\nException.Message :\n" + e.Exception.Message +
                    "\nException.StackTrace :\n" + e.Exception.StackTrace +
                    "\n\nException.String :\n" + e.Exception.ToString(), System.Text.Encoding.UTF8);

                MessageBox.Show("An error occurred, the application is now unstable. It is strongly recommended to restart the application!\n\n" + e.Exception.Message, "Fatal error.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                //Collectif.RestartApplication();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            Debug.WriteLine(ex.ToString());
            File.WriteAllText(ex.Message + Environment.NewLine + ex.StackTrace, "log.txt");
            //En cas d'erreur CEFSHARP.CORE.RUNTIME introuvable alors intaller vc_redist.x64 (https://aka.ms/vs/17/release/vc_redist.x64.exe)
        }
    }
}
