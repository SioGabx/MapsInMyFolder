using System;

namespace MapsInMyFolder.Core.Layers
{


    public partial class Layer
    {
        private static Layer _current;

        public static Layer Current
        {
            get
            {
                return _current;
            }
            set
            {
                if (_current == value)
                {
                    return;
                }
                var LayerChangedEventArgs = new LayerChangedEventArgs(_current, value);
                CurrentLayerChanging?.Invoke(null, LayerChangedEventArgs);
                if (!LayerChangedEventArgs.Cancel)
                {
                    _current = LayerChangedEventArgs.NewLayer;
                }
                CurrentLayerChanged?.Invoke(null, LayerChangedEventArgs);
            }
        }

        public static event EventHandler<LayerChangedEventArgs> CurrentLayerChanging;
        public static event EventHandler<LayerChangedEventArgs> CurrentLayerChanged;

        public class LayerChangedEventArgs : EventArgs
        {
            public Layer OldLayer { get; }
            public Layer NewLayer { get; set; }
            public bool Cancel { get; set; }

            public LayerChangedEventArgs(Layer OldLayer, Layer NewLayer)
            {
                this.OldLayer = OldLayer;
                this.NewLayer = NewLayer;
            }
        }
        public delegate void LayerChangedEventHandler(object sender, LayerChangedEventArgs e);

    }
}
