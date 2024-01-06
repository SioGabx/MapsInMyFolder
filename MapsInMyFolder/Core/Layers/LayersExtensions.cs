using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder
{
    public static class LayersExtensions
    {
        public static Layers GetLayerById(this IEnumerable<Layers> LayerList, int id)
        {
            return LayerList.Where(layer => layer.Id == id).FirstOrDefault(null as Layers);
        } 
        
        public static bool HasTransparency(this Layers layers)
        {
            string[] listOfAllFormatsAcceptedWithTransparency = new string[] { "png" };
            if (!string.IsNullOrWhiteSpace(layers.TilesFormat) && listOfAllFormatsAcceptedWithTransparency.Contains(layers.TilesFormat))
            {
                return true;
            }
            return false;
        }
    }
}
