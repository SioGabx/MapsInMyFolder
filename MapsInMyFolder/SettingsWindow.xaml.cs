using System;
using System.Windows;
using CefSharp;
using MapsInMyFolder.MapControl;
using System.Diagnostics;
using ModernWpf;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;

namespace MapsInMyFolder
{
    




    /// <summary>
    /// Logique d'interaction pour SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            this.Title = "MapsInMyFolder - Settings";
            TitleTextBox.Text = "MapsInMyFolder - Settings";
        }

        private void Window_Initialized(object sender, EventArgs e)
        {

        }
    }
}
