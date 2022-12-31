using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Commun
{
    public static class Curent
    {
        public static Layers Layer = Layers.Empty();

        public static class Selection
        {
            public static double NO_Latitude;
            public static double NO_Longitude;
            public static double SE_Latitude;
            public static double SE_Longitude;
        }
    }
}
