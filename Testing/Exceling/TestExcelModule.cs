/*using System;
using Exceling.NDatabase;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit;
using NUnit.Framework;
using Exceling.Functional.FileWork;
using OfficeOpenXml;

namespace Exceling.Testing.Exceling
{
    [TestFixture()]
    public class TestExcelModule
    {
        static Database database = new Database();
        static LogProgram logProgram = new LogProgram(database);
        static LoaderFile loaderFile = new LoaderFile(database, logProgram);
        ExcelModule excelModule = new ExcelModule(database, logProgram, loaderFile);

        [Test]
        public void GetHTMLData()
        {
            string url3 = excelModule.GetHTMLData("hhtttpsss:///1234.23");
            Assert.AreEqual(url3, null);
            string url1 = excelModule.GetHTMLData("https://www.microtron.ua/usb-3-0-flash-drive-128gb-kingston-datatraveler-se9-g2-32-6mbps-dtse9g2-128gb/p115999");
            Assert.AreNotEqual(url1, null);
            string url2 = excelModule.GetHTMLData(null);
            Assert.AreEqual(url2, null);

        }
        [Test]
        public void GetImageUrlFromHtml()
        {
            string html = excelModule.GetHTMLData("https://www.microtron.ua/usb-3-0-flash-drive-128gb-kingston-datatraveler-se9-g2-32-6mbps-dtse9g2-128gb/p115999");
            string answer = excelModule.GetImageUrlFromHtml(html);
            Assert.AreEqual(answer, "https://microtron.ua/media/5c52a2834dd6567ac96c1575/usb-3-0-flash-drive-128gb-kingston-datatraveler-se9-g2-32-6mbps-dtse9g2-128gb-115999.jpg");
            string answer2 = excelModule.GetImageUrlFromHtml(null);
            Assert.AreEqual(answer2, null);
            string answer3 = excelModule.GetImageUrlFromHtml("Hello world!");
            Assert.AreNotEqual(answer3, null);
        }
    }
}

        [Test]
        public void ConvertXlsToXlsx()
        {
            /*string pathXls = excelModule.GetExcelFile(Directory.GetCurrentDirectory() + "/m.zip");
            Assert.AreEqual(pathXls.Substring(pathXls.Length - "/mt-08_01.xls".Length), "/mt-08_01.xls");
            string pathXlsx = excelModule.ConvertXlsToXlsx(pathXls);
            Assert.AreEqual(pathXlsx.Substring(pathXlsx.Length - "/mt-08_01.xlsx".Length), "/mt-08_01.xlsx");
        }
        [Test]
        public void GetExcelFile()
        {
            /*string pathXls = excelModule.GetExcelFile(Directory.GetCurrentDirectory() + "/m.zip");
            Assert.AreEqual(pathXls.Substring(pathXls.Length - "/mt-08_01.xls".Length), "/mt-08_01.xls");
        }
        [Test]
        public void GetFirstWorksheetExcel()
        {
            /*string pathXls = excelModule.GetExcelFile(Directory.GetCurrentDirectory() + "/m.zip");
            string pathXlsx = excelModule.ConvertXlsToXlsx(pathXls);
            ExcelWorksheet worksheet = excelModule.GetFirstWorksheetExcel(pathXlsx);
            Assert.AreNotEqual(worksheet, null);
        }
        [Test]
        public void DefineParametersStyles()
        {
            /*string pathXls = excelModule.GetExcelFile(Directory.GetCurrentDirectory() + "/m.zip");
            string pathXlsx = excelModule.ConvertXlsToXlsx(pathXls);
            ExcelWorksheet worksheet = excelModule.GetFirstWorksheetExcel(pathXlsx);
            Dictionary<int,int> ParamsStyle = excelModule.DefineParametersStyles(worksheet);
            foreach(KeyValuePair<int,int> pair in ParamsStyle)
            {
                Console.WriteLine("Key:" + pair.Key + "\tValue:" + pair.Value);
            }
        }
        [Test]
        public void GetProducts()
        {
            /*string pathXls = excelModule.GetExcelFile(Directory.GetCurrentDirectory() + "/m.zip");
            string pathXlsx = excelModule.ConvertXlsToXlsx(pathXls);
            ExcelWorksheet worksheet = excelModule.GetFirstWorksheetExcel(pathXlsx);
            Dictionary<int, int> ParamsStyle = excelModule.DefineParametersStyles(worksheet);
            List<ProductCell> products = excelModule.GetProducts(worksheet, ParamsStyle);
            foreach(ProductCell product in products)
            {
                Console.WriteLine("Код:" + product.Code + "/tProduct Name:" + product.Product_Name);
            }
        }
        [Test]
        public void DefineDatetimeByFileName()
        {
            string pathXls = Directory.GetCurrentDirectory() + "/mt-20_03.zip";
            Console.WriteLine(excelModule.DefineDatetimeZipfile(pathXls));
        }
    }
}*/
