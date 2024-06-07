using System;

namespace MapsInMyFolder.Core.Layers
{
    public partial class Layer : IDisposable
    {
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Cleanup();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Cleanup()
        {
            Tiles?.Dispose();
        }

    }
}
