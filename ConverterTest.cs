using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virinco.WATS.Interface;

namespace Virinco.WATS.Converter.Seica
{
    [TestClass]
    public class ConverterTest : TDM
    {

        [TestMethod]
        public void SetupClient()
        {
            SetupAPI(null, "location", "purpose", true);
            RegisterClient("Your WATS instance url", "username", "password/token");
            InitializeAPI(true);
        }


        [TestMethod]
        public void TestSeicaXMLConverter()
        {

            InitializeAPI(true);
            SeicaXMLConverter converter = new SeicaXMLConverter();
            string fileName = "Data\\testdata_00012343123_passed.xml";
            SetConversionSource(new FileInfo(fileName), converter.ConverterParameters, null);
            using (FileStream file = new FileStream(fileName, FileMode.Open))
            {
                Report uut = converter.ImportReport(this, file);
                Submit(uut);
            }
        }
    }
}
