using Microsoft.VisualStudio.TestTools.UnitTesting;
using MapsInMyFolder.Core.XMLDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MapsInMyFolder.Core.XMLDatabase.Tests
{
    [TestClass()]
    public class ManagerTests
    {
        private Database GetDatabase()
        {
            Database database = new Database();
            var pathDirectory = Directory.GetCurrentDirectory();
            database.Path = Path.Combine(pathDirectory, "XMLTestFile.xml");
            Debug.WriteLine(database.Path);
            return database;
        }


        [TestMethod()]
        public void CreateWriteRead()
        {
            Database database = GetDatabase();
            System.IO.File.Delete(database.Path);
            database.CreateFile();
            Assert.IsTrue(System.IO.File.Exists(database.Path));

            database.Write("cat1/key1", "val1", "desc1");
            database.Write("cat1/key2", "val2");
            database.Write("cat2/key1", "val1", "desc2");
            database.Write("cat3/subcat3/key1", "val1", "desc1");

            database.WriteAttribute("cat2/key1", "attribcat2key1", "attribvalcat2key1");
            var AttribValue = database.ReadAttribute("cat2/key1", "attribcat2key1");
            Assert.AreEqual("attribvalcat2key1", AttribValue);


            Assert.IsNull(database.ReadAttribute("cat2/key1", "shouldnotexist"));
            Assert.IsNull(database.ReadAttribute("cat2/null", "attribcat2key1"));

            Assert.AreEqual("val1", database.Read("cat1/key1"));
            Assert.AreEqual("val1", database.Read("cat2/key1"));
        }

    }
}