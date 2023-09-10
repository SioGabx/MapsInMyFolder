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
            Version version = AssemblyVersion;
            string formatedVersionNumbers = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            return formatedVersionNumbers;
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
            var dialog = Message.SetContentDialog(Languages.GetWithArguments("updateMessageNewVersionAvailable", UpdateRelease.Tag_name, UpdateRelease.Body), Languages.Current["dialogTitleOperationConfirm"], MessageDialogButton.YesNo);
            ContentDialogResult result = ContentDialogResult.None;
            try
            {
                result = await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var downloadFileUrl = new Uri(UpdateFileAsset.Browser_download_url);
            string UpdateFilePath = System.IO.Path.Combine(Settings.temp_folder, UpdateFileAsset.Id + UpdateFileAsset.Name);

            string NotificationMsg = Languages.GetWithArguments("updateNotificationStartDownloading", UpdateFileAsset.Name, downloadFileUrl.Host);
            NProgress UpdateNotification = new NProgress(NotificationMsg, "MapsInMyFolder", "MainPage", null, 0, false) { };
            UpdateNotification.Register();

            Collectif.HttpClientDownloadWithProgress client = new Collectif.HttpClientDownloadWithProgress(downloadFileUrl.ToString(), UpdateFilePath);
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateNotification.Text(NotificationMsg + $" {progressPercentage?.ToString("0.00")}% - {Collectif.FormatBytes(totalBytesDownloaded)}/{Collectif.FormatBytes((long)totalFileSize)}");
                    UpdateNotification.SetProgress((double)progressPercentage);
                });
            };
            await client.StartDownload();
            UpdateNotification.Remove();

            var dialog2 = Message.SetContentDialog(Languages.Current["updateMessageStartUpdateProcess"], Languages.Current["dialogTitleOperationConfirm"], MessageDialogButton.YesCancel);
            ContentDialogResult result2 = ContentDialogResult.None;
            try
            {
                result2 = await dialog2.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            if (result2 == ContentDialogResult.Primary)
            {
                ApplyUpdate();
            }
            else {
                NText UpdateDownloadedNotification = new NText(Languages.Current["updateNotificationStartUpdateProcess"], "MapsInMyFolder", "MainPage", ApplyUpdate, true);
                UpdateDownloadedNotification.Register();
            }
        }

        public static void ApplyUpdate()
        {
            NText UpdateNotification = new NText(Languages.Current["updateNotificationStartInstalling"], "MapsInMyFolder", "MainPage", null, true);
            UpdateNotification.Register(); 

            string UpdateFilePath = System.IO.Path.Combine(Settings.temp_folder, UpdateFileAsset.Id + UpdateFileAsset.Name);
            Collectif.StartApplication(UpdateFilePath, TimeSpan.FromSeconds(3));
            Application.Current.Shutdown();
        }

    }
}
