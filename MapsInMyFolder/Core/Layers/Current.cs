using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Layers
{
  

    public partial class Layer : IDisposable
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
                var LayerChangedEventArgs = new LayerChangedEventArgs() { OldLayer = _current, NewLayer = value };
                CurrentLayerChanged?.Invoke(null, LayerChangedEventArgs);
                if (!LayerChangedEventArgs.Cancel)
                {
                    _current = LayerChangedEventArgs.NewLayer;
                }
            }
        }

        public static event LayerChangedEventHandler CurrentLayerChanged;

        public class LayerChangedEventArgs : EventArgs
        {
            public Layer OldLayer { get; set; }
            public Layer NewLayer { get; set; }
            public bool Cancel { get; set; }

            public LayerChangedEventArgs() {}
        }
        public delegate void LayerChangedEventHandler(object sender, LayerChangedEventArgs e);

    }
}
