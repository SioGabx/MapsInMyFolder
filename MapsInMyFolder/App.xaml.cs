using CefSharp;
using CefSharp.SchemeHandler;
using MapsInMyFolder.Commun;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
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
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
                //Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

                var settings = new CefSharp.Wpf.CefSettings();
                string BrowserSubprocessPathPath = System.IO.Path.GetFullPath("CefSharp.BrowserSubprocess.exe");
                if (File.Exists(BrowserSubprocessPathPath))
                {
                    settings.BrowserSubprocessPath = System.IO.Path.GetFullPath("CefSharp.BrowserSubprocess.exe");
                }
                settings.CachePath = Commun.Settings.temp_folder;

                if (!Directory.Exists(Commun.Settings.working_folder))
                {
                    try
                    {
                        Directory.CreateDirectory(Commun.Settings.working_folder);
                    }
                    catch (Exception)
                    {
                        Commun.Settings.working_folder = Commun.Settings.temp_folder;
                    }

                    if (!HasWritePermission(Commun.Settings.working_folder))
                    {
                        Commun.Settings.working_folder = Commun.Settings.temp_folder;
                    }

                    try
                    {
                        Directory.CreateDirectory(Commun.Settings.working_folder);
                    }
                    catch (Exception)
                    {
                        DebugMode.WriteLine("Erreur creation du dossier working_folder : " + Commun.Settings.working_folder);
                    }
                }

                settings.UserDataPath = Commun.Settings.temp_folder;
                settings.LogFile = Path.Combine(Commun.Settings.temp_folder, "internalBrowserLogs.log");
                settings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = "localfolder",
                    DomainName = "cefsharp",
                    SchemeHandlerFactory = new FolderSchemeHandlerFactory(
              rootFolder: Commun.Settings.working_folder,
              hostName: "cefsharp",
              defaultPage: "index.html" // will default to index.html
          )
                });

                if (!Cef.IsInitialized)
                {
                    //Perform dependency check to make sure all relevant resources are in our output directory.
                    //Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
                    Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Erreur de lancement : " + ex.Message + "\n\n StackTrace :  \n" + ex.StackTrace + "\n\n InnerException :  \n" + ex.InnerException);
                //MessageBox.Show(ex.ToString(), "Erreur de lancement", MessageBoxButton.OK, MessageBoxImage.Error);
                Message.NoReturnBoxAsync(ex.ToString(), "Erreur");
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
                e.Handled = true;
                System.IO.File.WriteAllText(Path.Combine(Commun.Settings.working_folder,"crash.log"), e.Exception.Message + Environment.NewLine + e.Exception.StackTrace + Environment.NewLine + Environment.NewLine + "Error string \n" + e.Exception.ToString(), System.Text.Encoding.UTF8);
                DebugMode.WriteLine(e.Exception.ToString());
                MessageBox.Show("Une erreur innatendu s'est produite, l'application va devoir se fermer. \n" + e.Exception.ToString(), "Erreur fatale");
                //Message.NoReturnBoxAsync("Une erreur innatendu s'est produite, l'application va devoir se fermer", "Erreur");
            }
            catch (Exception ex)
            {
                DebugMode.WriteLine(ex.ToString());
            }
            finally
            {
                //Collectif.RestartApplication();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            System.IO.File.WriteAllText(ex.Message + Environment.NewLine + ex.StackTrace, "log.txt");
            //En cas d'erreur CEFSHARP.CORE.RUNTIME introuvable alors intaller vc_redist.x64 (https://aka.ms/vs/17/release/vc_redist.x64.exe)
        }
    }
}
