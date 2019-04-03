using System;
using System.IO;
using System.Net;
using System.Text;
using OfficeOpenXml;
using Exceling.NDatabase;
using System.Diagnostics;
using System.Net.Security;
using System.IO.Compression;
using System.Collections.Generic;
using Exceling.Functional.FileWork;
using Exceling.NDatabase.UploadData;
using System.Text.RegularExpressions;
using Exceling.NDatabase.CategoryData;
using System.Security.Cryptography.X509Certificates;

namespace Exceling
{
    public class ExcelModule
    {
        private Database database;
        private LogProgram logger;
        public LoaderFile loaderFile;
        private Regex OnlyNumbers = new Regex("^[0-9]+$");
        private Regex ImageProductPattern = new Regex("<meta property=\"og:image\" content=\"(.*)\" />"); 
        private string WorkingDirectory = Directory.GetCurrentDirectory() + "/Files/Xlsx/";
        public Dictionary<string,int> Parameters_Id = new Dictionary<string,int>();
        public Random SetParameterId = new Random();

        public ExcelModule(ref Database Database, ref LogProgram logProgram, ref LoaderFile loaderFile)
        {
            this.database = Database;
            this.logger = logProgram;
            this.loaderFile = loaderFile;
        }
        public void HandleXlsFile(ref FileStruct file)
        {
            DateTime created_At;
            ExcelWorksheet worksheet;
            string xlsxPath;
            string xlsPath = file.Path + file.Name;
            xlsPath = GetExcelFile(ref xlsPath);
            if (!File.Exists(xlsPath))
            {
                loaderFile.DeleteFile(ref file);
                logger.WriteLog("Error in getting file. Delete file, file name=" + file.Name, LogLevel.Exceling);
                return;
            }
            created_At = DefineDatetimeZipfile(file.Path + file.Name);
            xlsxPath = ConvertXlsToXlsx(ref xlsPath);
            worksheet = GetFirstWorksheetExcel(ref xlsxPath);
            if (worksheet == null)
            {
                logger.WriteLog("Can not get First Worksheet Excel from file", LogLevel.Exceling);
                return;
            }
            SortOutProductCells(ref worksheet,ref created_At);
            Upload upload = new Upload(file.Name, created_At);
            database.upload.AddUpload(ref upload);
            logger.WriteLog("Handle xls file", LogLevel.Exceling);
        }
        public bool HandleXlsFile(string Full_Path_Zip, ref string answer)
        {
            string XlsxPath;
            DateTime Created_At;
            ExcelWorksheet worksheet;
            string xls_name = Full_Path_Zip.Substring(Full_Path_Zip.LastIndexOf('/') + 1);
            string XlsPath = GetExcelFile(ref Full_Path_Zip);
            if (!File.Exists(Full_Path_Zip))
            {
                logger.WriteLog("Error in getting file. File path=" + Full_Path_Zip, LogLevel.Error);
                answer = "Download zip file - does not have xls. Error int getting file. Can not get xls path.";
                return false;
            }
            Created_At = DefineDatetimeZipfile(Full_Path_Zip);
            XlsxPath = ConvertXlsToXlsx(ref XlsPath);
            worksheet = GetFirstWorksheetExcel(ref XlsxPath);
            if (worksheet == null)
            {
                logger.WriteLog("Can not get First Excel Worksheet from file", LogLevel.Error);
                answer = "Does not can convert this xls_path= " + XlsPath ;
                return false;
            }
            logger.WriteLog("Start sort out of product cells", LogLevel.Exceling);
            SortOutProductCells(ref worksheet,ref Created_At);
            Upload upload = new Upload(xls_name, Created_At);
            database.upload.AddUpload(ref upload);
            logger.WriteLog("Handle xls file", LogLevel.Exceling);
            return true;
        }
        private void SortOutProductCells(ref ExcelWorksheet worksheet,ref DateTime Created_At)
        {
            double no_price = -1;
            bool checking = false;
            string htmlData = null;
            ProductCell productCell = null;
            Dictionary<int, int> CategoriesStyles = DefineCategoriesStyles(ref worksheet);
            List<ProductCell> products = GetProducts(ref worksheet,ref CategoriesStyles,ref Created_At);
            List<ProductCell> old_products = database.product.SelectProducts();
            foreach (ProductCell new_product in products)
            {
                productCell = new_product;
                if (!database.product.CheckProductExist(ref new_product.product_code))
                {
                    htmlData = GetHTMLData(ref new_product.product_link_1);
                    if (htmlData != null)
                    {
                        new_product.product_url_image = GetImageUrlFromHtml(ref htmlData);
                    }
                    database.product.AddProduct(ref productCell);
                    database.price.AddPriceProduct(ref productCell, ref Created_At);
                }
                else
                {
                    database.product.UpdateCurrentPrice(ref new_product.product_code,ref new_product.product_price);
                    database.price.AddPriceProduct(ref productCell,ref Created_At);
                }
            }
            foreach(ProductCell oldProduct in old_products)
            {
                foreach(ProductCell new_product in products)
                {
                    if (oldProduct.product_code == new_product.product_code)
                    {
                        checking = true;
                    }
                }
                if (checking == false)
                {
                    database.product.UpdateCurrentPrice(ref oldProduct.product_code, ref no_price);
                }
                checking = false;
            }
            logger.WriteLog("Sorted out cells product and inserted all of that to database", LogLevel.Exceling);
        }
        /// <summary>
        /// Gets the excel file.
        /// </summary>
        /// <returns>The excel file.</returns>
        /// <param name="PathZipArchive">Path zip archive.</param>
        private string GetExcelFile(ref string PathZipArchive)
        {
            string PathToUnzipXls = "";
            string PathToSaveOutputFile = WorkingDirectory + ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds)).ToString();
            Directory.CreateDirectory(PathToSaveOutputFile);
            try
            {
                ZipFile.ExtractToDirectory(PathZipArchive, PathToSaveOutputFile);
                PathToUnzipXls = Directory.GetFiles(PathToSaveOutputFile)[0];
                logger.WriteLog("Unzip insert file. File path =" + PathZipArchive, LogLevel.Exceling);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error GetExcelFile(): " + e.Message);
                logger.WriteLog("Can not unzip insert file. File path =" + PathZipArchive, LogLevel.Exceling);
            }
            return PathToUnzipXls;
        }
        /// <summary>
        /// Converts the xls to xlsx.
        /// </summary>
        /// <returns>The xls to xlsx.</returns>
        /// <param name="PathXls">Path xls.</param>
        private string ConvertXlsToXlsx(ref string PathXls)
        {
            int attemp = 10;
            while (!File.Exists(PathXls + "x"))
            {
                Process LibreOffice = new Process();
                LibreOffice.StartInfo.FileName = "libreoffice";
                LibreOffice.StartInfo.WorkingDirectory = WorkingDirectory + PathXls.Substring(WorkingDirectory.Length, PathXls.IndexOf('/', WorkingDirectory.Length) - WorkingDirectory.Length);
                LibreOffice.StartInfo.Arguments = "--headless --convert-to xlsx " + PathXls;
                LibreOffice.Start();
                LibreOffice.WaitForExit();
                if (attemp == 0)
                {
                    break;
                }
                --attemp;
            }
            if (File.Exists(PathXls + "x"))
            {
                logger.WriteLog("Convert xls to xlsx file, path to xls =" + PathXls, LogLevel.Exceling);
                return PathXls + "x";
            }
            else
            {
                logger.WriteLog("Can not convert xls to xlsx file, file doesnt exists ,path to xls=" + PathXls, LogLevel.Exceling);
                return null;
            }
        }
        /// <summary>
        /// Gets the first worksheet excel.
        /// </summary>
        /// <returns>The first worksheet excel.</returns>
        /// <param name="PathExcelFile">Path excel file.</param>
        private ExcelWorksheet GetFirstWorksheetExcel(ref string PathExcelFile)
        {
            if (File.Exists(PathExcelFile))
            {
                FileInfo fileInfo =  new FileInfo(PathExcelFile);
                ExcelPackage excel = new ExcelPackage(fileInfo);
                if (excel.Workbook.Worksheets.Count >= 1)
                {
                    logger.WriteLog("Get first excel worksheet from xlsx", LogLevel.Exceling);
                    return excel.Workbook.Worksheets[1];
                }
                else
                {
                    logger.WriteLog("Can not get first worksheet from xlsx, file is doesnot have 1 woorksheet.", LogLevel.Exceling);
                    return null;
                }
            }
            else
            {
                logger.WriteLog("Can not get first worksheet from xlsx, file is doesnot exist.", LogLevel.Exceling);
                return null;
            }
        }
        /// <summary>
        /// Defines the parameters styles.
        /// </summary>
        /// <returns>The parameters styles.</returns>
        /// <param name="worksheet">Worksheet.</param>
        private Dictionary<int, int> DefineCategoriesStyles(ref ExcelWorksheet worksheet)
        {
            int Row = 1;
            int EmptyCell = 0;
            int parameter = 0;
            Dictionary<int, int> ParametersStyles = new Dictionary<int, int>();
            while (EmptyCell < 1000)
            {
                ExcelRange cell = worksheet.Cells[Row, 1];
                if (cell.Text == "")
                {
                    ++EmptyCell;
                }
                else
                {
                    EmptyCell = 0;
                    Match ContainsNumbers = OnlyNumbers.Match(cell.Text);
                    if (!ContainsNumbers.Success)
                    {
                        if (ParametersStyles.ContainsValue(cell.StyleID) == false)
                        {
                            ParametersStyles.Add(parameter, cell.StyleID);
                            ++parameter;
                        }
                    }
                }
                ++Row;
            }
            ParametersStyles.Remove(0);
            logger.WriteLog("Define categories styles", LogLevel.Exceling);
            return ParametersStyles;
        }
        /// <summary>
        /// Gets the products.
        /// </summary>
        /// <returns>The products.</returns>
        /// <param name="worksheet">Worksheet.</param>
        /// <param name="CategoriesStyles">Parameters styles.</param>
        /// <param name="created_at">Create at.</param>
        private List<ProductCell> GetProducts(ref ExcelWorksheet worksheet,ref Dictionary<int, int> CategoriesStyles, ref DateTime created_at)
        {
            string Created_At_Short = created_at.ToShortDateString();
            int Row = 1;
            int EmptyCell = 0;
            List<ProductCell> products = new List<ProductCell>();
            ProductCell categoryTransort = new ProductCell();
            while (EmptyCell < 1000)
            {
                ExcelRange cell = worksheet.Cells[Row, 1];
                if (cell.Text == "") 
                { 
                    ++EmptyCell; 
                }
                else
                {
                    EmptyCell = 0;
                    Match ContainsNumbers = OnlyNumbers.Match(cell.Text);
                    if (ContainsNumbers.Success)
                    {
                        ProductCell product = new ProductCell();
                        product.category = categoryTransort.category;
                        product.subcat = categoryTransort.subcat;
                        product.second_subcat = categoryTransort.second_subcat;
                        product.product_code = Convert.ToInt32(worksheet.Cells[Row, 1].Text);
                        product.product_vendore_code = worksheet.Cells[Row, 2].Text;
                        product.product_name = worksheet.Cells[Row, 3].Text;
                        string product_price = worksheet.Cells[Row, 4].Text;
                        product.product_price = ConvertSaveString(ref product_price, "double");
                        product.product_order = worksheet.Cells[Row, 5].Text;
                        product.product_link_1 = worksheet.Cells[Row, 6].Text;
                        product.product_link_2 = worksheet.Cells[Row, 7].Text;
                        product.product_comment = worksheet.Cells[Row, 8].Text;
                        product.created_at = Created_At_Short;
                        products.Add(product);
                    }
                    else
                    {
                        categoryTransort = GetProductRegulationParameters(ref cell,ref CategoriesStyles,ref categoryTransort, ref Created_At_Short);
                    }
                }
                ++Row;
            }
            logger.WriteLog("Get products from first worksheet", LogLevel.Exceling);
            return products;
        }
        /// <summary>
        /// Gets the product regulation parameters.
        /// </summary>
        /// <returns>The product regulation parameters.</returns>
        /// <param name="cell">Cell.</param>
        /// <param name="CategoriesStyles">Parameters styles.</param>
        /// <param name="Last_Category">Last parameter.</param>
        private ProductCell GetProductRegulationParameters(ref ExcelRange cell,ref Dictionary<int,int> CategoriesStyles,ref ProductCell Last_Category, ref string created_at)
        {
            CategoryCell category;
            if (CategoriesStyles.ContainsValue(cell.StyleID))
            {
                foreach (KeyValuePair<int,int> parameter in CategoriesStyles)
                {
                    if (parameter.Value == cell.StyleID)
                    {
                        string cell_text = cell.Text;
                        category = database.category.SelectCategoryByName(ref cell_text);
                        if (category == null)
                        {
                            category = new CategoryCell();
                            category.created_at = created_at;
                            category.category_name = cell.Text;
                            category.category_position = (short)parameter.Key;
                            category = database.category.AddCategory(ref category);
                        }
                        switch (parameter.Key)
                        {
                            case 1:
                                Last_Category.category = category.category_id;
                                Last_Category.subcat = -1;
                                Last_Category.second_subcat = -1;
                                break;
                            case 2:
                                Last_Category.subcat = category.category_id;
                                Last_Category.second_subcat = -1;
                                break;
                            case 3:
                                Last_Category.second_subcat = category.category_id;
                                break;
                        }
                    }
                }
            }
            return Last_Category;
        }
        public DateTime DefineDatetimeZipfile(string FileName)
        {
            int unixTime = 0;
            Regex EndFile = new Regex("/mt-(.*).zip");
            Match match = EndFile.Match(FileName);
            if (match.Success)
            {
                int days = Convert.ToInt32(match.Value.Substring(4, 2));
                int months = Convert.ToInt32(match.Value.Substring(7, 2));
                DateTime dateTime = new DateTime(DateTime.Now.Year, months, days);
                return dateTime;
            }
            else
            {
                unixTime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                return DateTime.UtcNow;
            }
        }
        private dynamic ConvertSaveString(ref string value, string type_value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return -1;
            }
            else
            {
                try
                {
                    switch(type_value)
                    {
                        case "double": return Convert.ToDouble(value);
                        case "int": return Convert.ToInt32(value);
                        case "long": return Convert.ToInt64(value);
                        case "short": return Convert.ToInt16(value);
                        default: return -1;
                    }
                }
                catch
                {
                    return -1;
                }
            }
        }
        public string GetHTMLData(ref string product_link_1)
        {
            try
            {
                if (!string.IsNullOrEmpty(product_link_1))
                {
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(AcceptAllCertifications);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(product_link_1);
                    request.Method = "GET";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Stream receiveStream = response.GetResponseStream();
                        StreamReader readStream = null;
                        if (response.CharacterSet == null)
                        {
                            readStream = new StreamReader(receiveStream);
                        }
                        else
                        {
                            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                        }
                        string htmldata = readStream.ReadToEnd();
                        response.Close();
                        readStream.Close();
                        return htmldata;
                    }
                    else
                    {
                        logger.WriteLog("Error response status, responce.StatusCode=" + response.StatusCode.ToString(), LogLevel.Error);
                        return null;
                    }
                }
                else
                {
                    logger.WriteLog("Empty or null string of product_link_1, FindProductImageUrl()", LogLevel.Error);
                    return null;
                }
            }
            catch (Exception e)
            {
                logger.WriteLog("Error function GetHTMLData, message:" + e.Message, LogLevel.Error);
                return null;
            }
        }
        public bool AcceptAllCertifications(object sender, X509Certificate certification, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public string GetImageUrlFromHtml(ref string htmlData)
        {
            string answer;
            if (!string.IsNullOrEmpty(htmlData))
            {
                Match url = ImageProductPattern.Match(htmlData);
                if (url.Success)
                {
                    logger.WriteLog("Get image url from html data", LogLevel.Exceling);
                    answer = url.Value.Remove(0, "<meta property=\"og:image\" content=\"".Length);
                    answer = answer.Remove(answer.Length - "\" />".Length, "\" />".Length);
                    return answer;
                }
                else
                {
                    logger.WriteLog("Can not get image url from data. Did not find by pattern - value, GetImageUrlFromHtml()", LogLevel.Error);
                    return null;
                }
            }
            else
            {
                logger.WriteLog("Empty or null string of htmlData, GetImageUrlFromHtml()", LogLevel.Error);
                return null;
            }
        }
    }
} 