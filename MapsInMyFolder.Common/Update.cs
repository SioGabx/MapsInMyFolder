using ModernWpf.Controls;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace MapsInMyFolder.Commun
{
    public static class Update
    {
        public static Version AssemblyVersion = Assembly.GetEntryAssembly().GetName().Version;
        public static bool IsUpdateAvailable = false;
        public static RootObject UpdateRelease = null;
        public static Asset UpdateFileAsset = null;
        public static event EventHandler NewUpdateFoundEvent = delegate { };
        public static string GetActualVersionFormatedString()
        {
            Version version = Update.AssemblyVersion;
            return String.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        public static string GetActualProductVersionFormatedString()
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            return fileVersionInfo.ProductVersion;
        }

        public async static Task<bool> CheckIfNewerVersionAvailableOnGithub()
        {
            if (!Network.IsNetworkAvailable()) { return false; }
            return await Task.Run(() =>
            {
                System.Threading.Thread.Sleep(5000);
                (RootObject Release, Asset FileAsset) = GetGithubAssets.GetReleaseAssetsFromGithub(new Uri(Settings.github_repository_url).PathAndQuery, "MapsInMyFolder.Setup.msi");
                if (Release is null)
                {
                    return false;
                }
                string GithubVersion = Collectif.FilterDigitOnly(Release.Tag_name, new System.Collections.Generic.List<char>() { '.' }, false, false);

                //Debug.WriteLine("LatestGithubRelease : " + FileAsset);
                //Debug.WriteLine("Download URL : " + FileAsset.Browser_download_url);
                //Debug.WriteLine("tag_name " + Release.Tag_name);
                //Debug.WriteLine("GetActualProductVersionFormatedString " + GetActualProductVersionFormatedString());
                bool IsNewerVersionAvailable = CompareVersion(GithubVersion);
                if (IsNewerVersionAvailable)
                {
                    UpdateRelease = Release;
                    UpdateFileAsset = FileAsset;
                    IsUpdateAvailable = true;
                    NewUpdateFoundEvent(null, EventArgs.Empty);
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        public static bool CompareVersion(string GithubVersion)
        {
            string[] SplittedGithubVersion = GithubVersion.Split('.');

            int versionMajor = 0;
            int versionMinor = 0;
            int versionBuild = 0;

            if (SplittedGithubVersion.Length >= 3)
            {
                versionMajor = Convert.ToInt32(SplittedGithubVersion[0]);
                versionMinor = Convert.ToInt32(SplittedGithubVersion[1]);
                versionBuild = Convert.ToInt32(SplittedGithubVersion[2]);
            }

            Version AssemblyVersion = Update.AssemblyVersion;
            XMLParser.Cache.Write("LastUpdateCheck", DateTime.Now.Ticks.ToString());
            //Debug.WriteLine("Version actuelle : " + GetActualVersionFormatedString());
            //Debug.WriteLine("Version github : " + GithubVersion);

            //Debug.WriteLine("versionMajor :" + (AssemblyVersion.Major < versionMajor));
            //Debug.WriteLine("versionMinor :" + (AssemblyVersion.Major <= versionMajor && AssemblyVersion.Minor < versionMinor));
            //Debug.WriteLine("versionBuild :" + (AssemblyVersion.Major <= versionMajor && AssemblyVersion.Minor <= versionMinor && AssemblyVersion.Build < versionBuild));
            if (GetActualVersionFormatedString() == GithubVersion)
            {
                return false;
            }

            if (
            (AssemblyVersion.Major < versionMajor) ||
            (AssemblyVersion.Major <= versionMajor && AssemblyVersion.Minor < versionMinor) ||
            (AssemblyVersion.Major <= versionMajor && AssemblyVersion.Minor <= versionMinor && AssemblyVersion.Build < versionBuild))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async void StartUpdating()
        {
            var dialog = Message.SetContentDialog($"Une nouvelle version de l'application est disponible {UpdateRelease.Tag_name}. Voullez-vous télécharger et installer cette mise à jour ?\n\nNotes de publication :\n{UpdateRelease.Body}", "Confirmer", MessageDialogButton.YesNo);
            ContentDialogResult result2 = ContentDialogResult.None;
            try
            {
                result2 = await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            if (result2 != ContentDialogResult.Primary)
            {
                return;
            }

            Debug.WriteLine("La nouvelle version " + UpdateRelease.Tag_name + " va être installée");

            var downloadFileUrl = new Uri(UpdateFileAsset.Browser_download_url);
            string UpdateFilePath = System.IO.Path.Combine(Settings.temp_folder, UpdateFileAsset.Id + UpdateFileAsset.Name);

            string NotificationMsg = $"Téléchargement en cours de '{UpdateFileAsset.Name}' depuis {downloadFileUrl.Host}...";
            NProgress UpdateNotification = new NProgress(NotificationMsg, "MapsInMyFolder", null, 0, false) { };
            UpdateNotification.Register();

            Collectif.HttpClientDownloadWithProgress client = new Collectif.HttpClientDownloadWithProgress(downloadFileUrl.ToString(), UpdateFilePath);
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateNotification.Text(NotificationMsg + $" {progressPercentage}% - {Collectif.FormatBytes(totalBytesDownloaded)}/{Collectif.FormatBytes((long)totalFileSize)}");
                    UpdateNotification.SetProgress((double)progressPercentage);
                });
            };
            await client.StartDownload();
            
            UpdateNotification.Text("Installation...");
            UpdateNotification.SendUpdate();
            Application.Current.Shutdown();
            Process.Start(new ProcessStartInfo(UpdateFilePath) { UseShellExecute = true });
        }
    }
}
