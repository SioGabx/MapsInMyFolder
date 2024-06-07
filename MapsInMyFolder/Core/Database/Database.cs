namespace MapsInMyFolder.Core.Database
{
    public enum Tables { LAYERS, EDITEDLAYERS, CUSTOMLAYERS, DOWNLOADS }
    public class Database
    {
        public string Path { get; set; }
        public Tables AvailableTables { get; set; }
        public Database() { }
    }
}
