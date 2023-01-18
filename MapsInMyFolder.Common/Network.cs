using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace MapsInMyFolder.Commun
{

    public static class Network
    {
        /// <summary>
        /// Indicates whether any network connection is available
        /// Filter virtual network cards.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if a network connection is available; otherwise, <c>false</c>.
        /// </returns>
        ///
        public static bool IsNetworkAvailable()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                _FastIsNetworkAvailable = false;
                Task.Factory.StartNew(NetworkWatcher);
                return false;
            }

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // discard because of standard reasons
                if ((ni.OperationalStatus != OperationalStatus.Up) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                {
                    continue;
                }

                // discard virtual cards (virtual box, virtual pc, etc.)
                if (ni.Description.Contains("virtual", StringComparison.OrdinalIgnoreCase) ||
                    ni.Name.Contains("virtual", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
                if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                _FastIsNetworkAvailable = true;
                return true;
            }
            Task.Factory.StartNew(NetworkWatcher);
            _FastIsNetworkAvailable = false;
            return false;
        }

        private static bool _FastIsNetworkAvailable = true;
        public static bool FastIsNetworkAvailable()
        {
            return _FastIsNetworkAvailable;
        }

        private static readonly object NetworkWatcherLocker = new object();
        private static bool NetworkWatcherIsRunning;
        public static event EventHandler IsNetworkNowAvailable = delegate { };
        public static async void NetworkWatcher()
        {
            if (!NetworkWatcherIsRunning)
            {
                Task t = Task.Run(() =>
                {
                    lock (NetworkWatcherLocker)
                    {
                        if (!IsNetworkAvailable() && !NetworkWatcherIsRunning)
                        {
                            NetworkWatcherIsRunning = true;
                            do
                            {
                                Thread.Sleep(1000);
                                //Debug.WriteLine("Waiting 100ms, NetworkNotAvailable");
                            } while (!IsNetworkAvailable());
                            NetworkWatcherIsRunning = false;
                            IsNetworkNowAvailable(null, EventArgs.Empty);
                        }
                    }
                });
                await t;
            }
        }
    }
}
