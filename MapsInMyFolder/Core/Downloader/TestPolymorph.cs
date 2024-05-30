using MapsInMyFolder.Core.Layers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MapsInMyFolder.Core.Downloader
{
    public class TestPolymorphisme
    {
        public static TestPolymorphisme GetFromFormat(Format format)
        {
            Type ClassType = typeof(TestPolymorphisme);
            switch (format)
            {
                case Format.png:
                    ClassType = typeof(TilePNG);
                    break;
                case Format.jpeg:
                    ClassType = typeof(TileJPEG);
                    break;
                case Format.pbf:
                    throw new NotImplementedException();
                    break;
            }
            return (TestPolymorphisme)Activator.CreateInstance(ClassType, null);
        }
        public virtual void GetFormat()
        {
            Debug.WriteLine("Non");
        }
    }

    public class TilePNG : TestPolymorphisme
    {
        public override void GetFormat()
        {
            Debug.WriteLine("PNG");
        }
    }

    public class TileJPEG : TestPolymorphisme
    {
        public override void GetFormat()
        {
            Debug.WriteLine("JPEG");
        }
    }
}
