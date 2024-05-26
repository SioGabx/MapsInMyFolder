using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Database
{
    public enum Tables { LAYERS, EDITEDLAYERS, CUSTOMLAYERS, DOWNLOADS}
    public class Database
    {
        public string Path { get; set; }
        public Tables AvailableTables { get; set; }
        public Database() { }
    }
}
