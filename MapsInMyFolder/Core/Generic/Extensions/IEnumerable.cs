using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class IEnumerableExtensions
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> collection)
        {
            var OCollection = new ObservableCollection<T>();
            foreach (var item in collection)
            {
                OCollection.Add(item);
            }
            return OCollection;
        }


    }
}
