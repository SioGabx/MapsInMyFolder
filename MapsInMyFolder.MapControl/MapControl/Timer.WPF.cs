// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;

using System.Windows;
using System.Windows.Threading;


namespace MapsInMyFolder.MapControl
{
    internal static class Timer
    {
        public static DispatcherTimer CreateTimer(this DependencyObject obj, TimeSpan interval)
        {
            var timer = new DispatcherTimer
            {
                Interval = interval
            };

            return timer;
        }

        public static void Run(this DispatcherTimer timer, bool restart = false)
        {
            if (restart)
            {
                timer.Stop();
            }

            if (!timer.IsEnabled)
            {
                timer.Start();
            }
        }
    }
}
