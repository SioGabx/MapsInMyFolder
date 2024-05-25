using System;
using System.Diagnostics;
using System.IO;
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
