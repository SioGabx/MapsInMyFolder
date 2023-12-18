using MapsInMyFolder.Commun;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapsInMyFolder.UserControls
{
    /// <summary>
    /// Logique d'interaction pour ClickableLabel.xaml
    /// </summary>
    public partial class ClickableLabel : UserControl
    {
        public ClickableLabel()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ContentValueProperty = DependencyProperty.Register(
            nameof(ContentValue), typeof(object),
            typeof(ClickableLabel),
     new PropertyMetadata(OnContentValueChanged));

        public object ContentValue
        {
            get { return GetValue(ContentValueProperty); }
            set { SetValue(ContentValueProperty, value); }
        }

        private static void OnContentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var clickableLabel = (ClickableLabel)d;
            clickableLabel.ClickableLbl.Content = e.NewValue?.ToString();
        }

        public string ContentValueStringFormat
        {
            get { return ClickableLbl.ContentStringFormat; }
            set { ClickableLbl.ContentStringFormat = value; }
        }

        public new bool IsEnabled
        {
            get { return ClickableLbl.IsEnabled; }
            set { ClickableLbl.IsEnabled = value; }
        }
        public new double Opacity
        {
            get { return ClickableLbl.Opacity; }
            set { ClickableLbl.Opacity = value; }
        }

        private bool isMouseDown = false;
        public event RoutedEventHandler Click;

        public void ClickableLbl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
        }

        public void ClickableLbl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isMouseDown)
            {
                ClickableLbl.Cursor = Cursors.Arrow;
                Click?.Invoke(this, e);
            }

            isMouseDown = false;
        }

        public void ClickableLbl_MouseLeave(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
            ClickableLbl.Foreground = Collectif.HexValueToSolidColorBrush("#888989");
        }

        public void ClickableLbl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (ClickableLbl.IsEnabled)
            {
                ClickableLbl.Cursor = Cursors.Hand;
                ClickableLbl.Foreground = Collectif.HexValueToSolidColorBrush("#b4b4b4");
            }
            else
            {
                ClickableLbl.Cursor = Cursors.Arrow;
            }
        }

        private void ClickableLbl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Wtf ?");
        }
    }
}
