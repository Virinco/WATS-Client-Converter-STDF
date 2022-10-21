using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Virinco.WATS.Interface;

namespace STDFConverter
{
    [TestClass]
    public class ConverterTests : TDM
    {
        [TestMethod]
        public void SetupClient()
        {
            SetupAPI("dataDir", "Location", "Purpose", true);
            RegisterClient("Your WATS domain", "Your username (Empty if using ClientToken)", "Insert ClientToken/password here");
            InitializeAPI(true);
        }

        [TestMethod]
        public void STDFConverterTest()
        {
            InitializeAPI(true);
            string fn = @"Path to your file here.";
            STDFConverter converter = new STDFConverter();
            using (FileStream file = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read))
            {                
                SetConversionSource(new FileInfo(fn), converter.ConverterParameters, null);
                Report uut = converter.ImportReport(this, file);
                //Submit(uut); //Uncomment if you wish to submit the report here instead of in the converter. You will also need to comment out the submit in Converter (Line 130).
            }
        }
        [TestMethod]
        public void STDFConverterTestFolder()
        {
            InitializeAPI(true);
            STDFConverter converter = new STDFConverter();
            string[] fileNames = Directory.GetFiles(@"c:\tmp", "*.std"); //This requires a folder under C named tmp
            foreach (string fn in fileNames)
            {
                using (FileStream file = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read))
                {
                    SetConversionSource(new FileInfo(fn), converter.ConverterParameters, null);
                    Report uut = converter.ImportReport(this, file);
                    //Submit(uut); //Uncomment if you wish to submit the reports here instead of in the converter. You will also need to comment out the submit in Converter (Line 130).
                }
            }
        }
    }
}
