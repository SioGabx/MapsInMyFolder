using Microsoft.VisualStudio.TestTools.UnitTesting;
using MapsInMyFolder.Core.XMLDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Core.Layers;

namespace MapsInMyFolder.Tests.Core.XMLDatabase
{
    [TestClass()]
    public class ManagerTests
    {
        [TestMethod()]
        public void CreateWriteRead()
        {
            Database Db = new Database();
            var PathDirectory = Directory.GetCurrentDirectory();
            Db.Path = Path.Combine(PathDirectory, "XMLTestFile.xml");

            File.Delete(Db.Path);
            Debug.WriteLine("CreateWriteRead : " + Db.Path);
            Db.CreateFile();
            Assert.IsTrue(File.Exists(Db.Path));

            Db.Write("cat1/key1", "val1", "desc1");
            Db.Write("cat1/key2", "val2");
            Db.Write("cat2/key1", "val1", "desc2");
            Db.Write("cat3/subcat3/key1", "val1", "desc1");

            Db.WriteAttribute("cat2/key1", "attribcat2key1", "attribvalcat2key1");
            var AttribValue = Db.ReadAttribute("cat2/key1", "attribcat2key1");
            Assert.AreEqual("attribvalcat2key1", AttribValue);


            Assert.IsNull(Db.ReadAttribute("cat2/key1", "shouldnotexist"));
            Assert.IsNull(Db.ReadAttribute("cat2/null", "attribcat2key1"));

            Assert.AreEqual("val1", Db.Read("cat1/key1"));
            Assert.AreEqual("val1", Db.Read("cat2/key1"));
        }


        [TestMethod()]
        public void TestReadLayer()
        {
            var PathDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Atlas/");
            var XMLLayers = Directory.GetFiles(PathDirectory, "*.xml");
            foreach (var XMLLayer in XMLLayers)
            {
                Database db = new Database
                {
                    Path = XMLLayer
                };

                var layer = new Layer();
                layer.Name = db.Read("Layer/Name");
                layer.Description = db.Read("Layer/Description");
                layer.Tags = db.Read("Layer/Tags");
                layer.Countries = db.Read("Layer/Countries");
                layer.Identifier = db.Read("Layer/Identifier");
                layer.TileUrlSchema = db.Read("Layer/Identifier");
                layer.MinZoom = int.Parse(db.Read("Layer/MinZoom"));
                layer.MaxZoom = int.Parse(db.Read("Layer/MaxZoom"));
                layer.TileSavingFormat = Enum.Parse<SavingFormat>(db.Read("Layer/TileSavingFormat"), true);
                layer.TileSize = int.Parse(db.Read("Layer/TileSize"));
                layer.Area = db.Read("Layer/Area");
                layer.Style = db.Read("Layer/Style");
                layer.Script = db.Read("Layer/Script");
                layer.BackColor = db.Read("Layer/BackColor").ConvertHexValueToSolidColorBrush();
                layer.UserAgent = db.Read("Layer/UserAgent");
                layer.ProviderName = db.Read("Layer/ProviderName");
                layer.ProviderUrl = db.Read("Layer/ProviderUrl");
                layer.IsFavorite = bool.Parse(db.Read("Settings/IsFavorite"));
                layer.Visibility = Enum.Parse<Display>(db.Read("Settings/Visibility"), true);
                layer.IsAtScale = bool.Parse(db.Read("Settings/IsAtScale"));
                layer.ShowTileBorder = bool.Parse(db.Read("Settings/ShowTileBorder"));
                layer.ShowTileLocation = bool.Parse(db.Read("Settings/ShowTileLocation"));

                Debug.WriteLine(layer);
            }
        }

    }
}