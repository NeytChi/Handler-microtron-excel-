using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using Exceling.NDatabase;
using System.Globalization;
using Exceling.Functional.Mail;
using Exceling.Functional.Tasker;
using System.Collections.Generic;
using Exceling.Functional.FileWork;
using Exceling.NDatabase.PriceData;
using System.Text.RegularExpressions;
using Exceling.NDatabase.CategoryData;

namespace Exceling
{
    public class Worker
    {
        private TaskManager tasker;
        private Database database;
        private LogProgram logger;
        private LoaderFile loaderFile;
        private ExcelModule excelModule;
        private MailF mail;

        public Worker(ref Database callDB, ref LogProgram logprogram) 
        {
            this.database = callDB;
            this.logger = logprogram;
            this.mail = new MailF(database, logger);
            this.tasker = new TaskManager(logger);
            this.loaderFile = new LoaderFile(ref database,ref logger);
            this.excelModule = new ExcelModule(ref database,ref logger,ref loaderFile);
            tasker.EmailMonitoring(this);
        }
        public void HandleDownloadXlsByEmail()
        {
            string answerFromHandle = "";
            List<string> files = mail.ReceivedFileImap(loaderFile.Path_Zip_Save);
            foreach(string file in files)
            {
                string xls_name = file.Substring(file.LastIndexOf('/') + 1);
                if (database.upload.CheckUploadExist(ref xls_name))
                {
                    logger.WriteLog("Program did not handle this archive, because had finded the same file name in database", LogLevel.Exceling);
                    return;
                }
                if (!excelModule.HandleXlsFile(file, ref answerFromHandle))
                {
                    logger.WriteLog("Error xls handle file" + "Answer of algoritm:\r\n" + answerFromHandle, LogLevel.Worker);
                }
                else 
                {
                    logger.WriteLog("Handle excelx file from imap protocol", LogLevel.Worker);
                }
            }
        }
        public string AddXls(ref string request, ref byte[] buffer, ref int bytes, ref Socket handleSocket)
        {
            FileStruct file = loaderFile.AddFile(ref request, ref buffer, ref bytes);
            if (file != null)
            {
                if (database.upload.CheckUploadExist(ref file.Name))
                {
                    logger.WriteLog("Program did not handle this archive, because had finded the same file name in database", LogLevel.Exceling);
                    SendErrorJsonRequest(JsonAnswer(false, "Program did not handle this archive, because had finded the same file name in database"), handleSocket);
                    return null;
                }
                Thread thread = new Thread(() => excelModule.HandleXlsFile(ref file));
                thread.IsBackground = true;
                thread.Start();
                return JsonAnswer(true, "Send xls-file is okay. Server handle info from it");
            }
            else
            {
                SendErrorJsonRequest(JsonAnswer(false, "Can not get file from request"), handleSocket);
                return null;
            }
        }
        public string SelectCategories(ref string request, ref Socket handleSocket)
        {
            int? category_id = ConvertSaveString(FindParamFromRequest(ref request, "category_id"));
            int page = ConvertSaveString(FindParamFromRequest(ref request, "page"));
            if (category_id == -1)
            {
                List<CategoryCell> categories = database.category.SelectCategoriesByPosition(1);
                List<ProductCell> products = database.product.SelectProducts(-1, -1, -1);
                logger.WriteLog("Get general categories.", LogLevel.Worker);
                return JsonCategoriesProducts(categories, products);
            }
            else
            {
                CategoryCell category = database.category.SelectCategoryById(category_id);
                if (category == null)
                {
                    logger.WriteLog("Can not find category with insert id", LogLevel.Error);
                    SendErrorJsonRequest(JsonAnswer(false, "Can not find category with insert id"), handleSocket);
                    return null;
                }
                List<CategoryCell> categories = new List<CategoryCell>();
                List<ProductCell> products = database.product.SelectProductsByCategory(category.category_id, category.category_position);
                List<int> findingCategories = database.product.FindNextStepCategories(ref category.category_id, category.category_position);
                foreach(int findCategory in findingCategories)
                {
                    category = database.category.SelectCategoryById(findCategory);
                    if (category != null)
                    {
                        categories.Add(category);
                    }
                }
                logger.WriteLog("Select categories and products by category_id=" + category_id, LogLevel.Worker);
                return JsonCategoriesProducts(categories, products);
            }
        }
        public string SelectHistoryProduct(ref string request, ref Socket handleSocket)
        {
            int? product_code = ConvertSaveString(FindParamFromRequest(ref request, "product_code"));
            if (product_code == null)
            {
                logger.WriteLog("Can not define product code in request", LogLevel.Error);
                SendErrorJsonRequest(JsonAnswer(false, "Can not define product code in request"), handleSocket);
                return null;
            }
            int time_from = ConvertSaveString(FindParamFromRequest(ref request, "time_from"));
            int time_to = ConvertSaveString(FindParamFromRequest(ref request, "time_to"));
            if (time_to == -1)
            {
                time_to = 2000000000; 
            }
            DateTime date_from = new DateTime(1970, 1, 1, 1, 1, 1, 1).AddSeconds(time_from);
            DateTime date_to = new DateTime(1970, 1, 1, 1, 1, 1, 1).AddSeconds(time_to);
            List<Price> prices = database.price.SelectHistory(ref product_code,ref date_from,ref date_to);
            if (prices.Count == 0)
            {
                logger.WriteLog("Can not get histories by product_code=" + product_code, LogLevel.Error);
                SendErrorJsonRequest(JsonAnswer(false, "Can not get histories by product_code=" + product_code), handleSocket);
                return null;
            }
            logger.WriteLog("Complite got histories by product_code=" + product_code, LogLevel.Worker);
            return JsonTrueObject("prices", prices);
        }
        public string SearchProductByName(ref string request, ref Socket handleSocket)
        {
            List<ProductCell> products = new List<ProductCell>();
            string search_name = WebUtility.UrlDecode(FindParamFromRequest(ref request, "search_name"));
            int page = ConvertSaveString(FindParamFromRequest(ref request, "page"));
            if (search_name != "")
            {
                if (page < 0)
                {
                    products = database.product.SelectProductsWithSearch(ref search_name, 0, 9);
                }
                else
                {
                    products = database.product.SelectProductsWithSearch(ref search_name, page * 10, 9);
                }
                logger.WriteLog("Search product by name request.", LogLevel.Worker);
                return JsonTrueObject("products", products);
            }
            SendErrorJsonRequest(JsonAnswer(false, "Can not get search name from request"), handleSocket);
            return null;
        }
        private void SendJsonRequest(ref string json, ref Socket remoteSocket)
        {
            if (string.IsNullOrEmpty(json))
            {
                logger.WriteLog("Input value is null or empty, function SendJsonRequest()", LogLevel.Error);
                return;
            }
            json = EncodeNonAsciiCharacters(ref json);
            string response = "HTTP/1.1 200\r\n" +
                              "Version: HTTP/1.1\r\n" +
                              "Content-Type: application/json;\r\n" +
                              "Content-Length: " + (json.Length).ToString() +
                              "\r\n\r\n" + json;
            byte[] buffer = Encoding.ASCII.GetBytes(response);
            if (remoteSocket.Connected)
            {
                logger.WriteLog("Return http 200 JSON answer", LogLevel.Worker);
                remoteSocket.Send(buffer);
            }
            else
            {
                logger.WriteLog("Remote socket disconnected, can not send request, function SendJsonRequest()", LogLevel.Error);
                return;
            }
        }
        public string EncodeNonAsciiCharacters(ref string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        public string DecodeEncodedNonAsciiCharacters(ref string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m => {
                    return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
                });
        }
        private dynamic GetJsonFromRequest(ref string request)
        {
            if (string.IsNullOrEmpty(request))
            {
                string json = "";
                int searchIndex = request.IndexOf("application/json", StringComparison.Ordinal);
                if (searchIndex == -1)
                {
                    logger.WriteLog("Can not find \"application/json\" in request.", LogLevel.Error);
                    return null;
                }
                int indexFirstChar = request.IndexOf("{", searchIndex, StringComparison.Ordinal);
                if (indexFirstChar == -1)
                {
                    logger.WriteLog("Can not find start json in request.", LogLevel.Error);
                    return null;
                }
                int indexLastChar = request.LastIndexOf("}", StringComparison.Ordinal);
                if (indexLastChar == -1)
                {
                    logger.WriteLog("Can not find end json in request.", LogLevel.Error);
                    return null;
                }
                try
                {
                    json = request.Substring(indexFirstChar, indexLastChar - indexFirstChar + 1);
                    return JsonConvert.DeserializeObject<dynamic>(json);
                }
                catch (Exception e)
                {
                    logger.WriteLog("Can not define json object in request. Message" + e.Message, LogLevel.Error);
                    return null;
                }
            }
            else
            {
                logger.WriteLog("Insert request is null or empty. Error in function GetJsonFromRequest", LogLevel.Error);
                return null;
            }
        }
        private string FindValueContentDisposition(ref string request, string key)
        {
            string findKey = "Content-Disposition: form-data; name=\"" + key + "\"";
            string boundary = GetBoundaryRequest(request);
            if (string.IsNullOrEmpty(boundary))
            {
                logger.WriteLog("Can not get boundary from request, function FindValueContentDisposition", LogLevel.Error);
                return null;
            }
            boundary = "\r\n--" + boundary;
            if (request.Contains(findKey))
            {
                int searchKey = request.IndexOf(findKey, StringComparison.Ordinal) + findKey.Length + "\r\n\r\n".Length;
                if (searchKey == -1)
                {
                    logger.WriteLog("Can not find content-disposition key from request, function FindValueContentDisposition", LogLevel.Error);
                    return null;
                }
                int transfer = request.IndexOf(boundary, searchKey, StringComparison.Ordinal);
                if (transfer == -1)
                {
                    logger.WriteLog("Can not end boundary from request, function FindValueContentDisposition", LogLevel.Error);
                    return null;
                }
                if (transfer > searchKey)
                {
                    return request.Substring(searchKey, transfer - searchKey);
                }
                else
                {
                    logger.WriteLog("Can not define key value from request, function FindValueContentDisposition", LogLevel.Error);
                    return null;
                }
            }
            else
            {
                logger.WriteLog("Request does not contain find key, function FindValueContentDisposition", LogLevel.Error);
                return null;
            }
        }
        private string FindParamFromRequest(ref string request, string key)
        {
            Regex urlParams = new Regex(@"[\?&](" + key + @"=([^&=#\s]*))", RegexOptions.Multiline);
            Match match = urlParams.Match(request);
            if (match.Success)
            {
                string value = match.Value;
                return value.Substring(key.Length + 2);
            }
            else
            {
                logger.WriteLog("Can not define url parameter from request, function FindParamFromRequest", LogLevel.Error);
                return null;
            }
        }
        private void HttpErrorUrl(ref Socket remoteSocket)
        {
            string response = "";
            string responseBody = "";
            responseBody = string.Format("<HTML><BODY><h1>error url...</h1></BODY></HTML>");
            response = "HTTP/1.1 400 \r\n" +
                    "Version: HTTP/1.1\r\n" +
                    "Content-Type: text/html; charset=utf-8\r\n" +
                    "Content-Length: " + (response.Length + responseBody.Length) +
                    "\r\n\r\n" +
                 responseBody;
            remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            logger.WriteLog("HTTP 400 Error link response", LogLevel.Error);
        }
        public string JsonAnswer(bool success, string message)
        {
            string jsonAnswer = "{\r\n \"success\":" + success.ToString().ToLower() + ",\r\n" +
                " \"message\":\"" + message + "\"\r\n" +
                "}";
            return jsonAnswer;
        }
        public string JsonCategoriesProducts(Object categories, Object products)
        {
            string jsonProducts = JsonConvert.SerializeObject(products);
            string jsonCategories = JsonConvert.SerializeObject(categories);
            string jsonAnswer = "{\r\n" +
                "\"success\":true,\r\n" +
                "\"data\":{\r\n" +
                "\"products\":" + jsonProducts + ",\r\n" +
                "\"categories\":" + jsonCategories + "\r\n}\r\n}";
            return jsonAnswer;
        }
        public string JsonTrueObject(string name_object, Object products)
        {
            string jsonObject = JsonConvert.SerializeObject(products);
            string jsonAnswer = "{\r\n" +
            	"\"success\":true,\r\n" +
                "\"data\":{\r\n" + 
                "\"" + name_object + "\":" + jsonObject + "\r\n}\r\n}";
            return jsonAnswer;
        }
        private int ConvertSaveString(string resouce)
        {
            if (string.IsNullOrEmpty(resouce))
            { 
                return -1; 
            }
            try 
            {

                return Convert.ToInt32(resouce); 
            }
            catch 
            {
                return -1; 
            }
        }
        private void SendErrorJsonRequest(string json, Socket remoteSocket)
        {
            json = EncodeNonAsciiCharacters(ref json);
            string response = "HTTP/1.1 500\r\n" +
                              "Version: HTTP/1.1\r\n" +
                              "Content-Type: application/json\r\n" +
                              "Content-Length: " + (json.Length).ToString() +
                              "\r\n\r\n" +
                              json;
            remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            Debug.WriteLine(response);
            logger.WriteLog("Return http 500 responce with JSON data.", LogLevel.Worker);
        }
        public string GetBoundaryRequest(string request)
        {
            int i = 0;
            bool exist = false;
            string boundary = "";
            string subRequest = "";
            int first = request.IndexOf("boundary=", StringComparison.Ordinal);
            if (first == -1)
            {
                logger.WriteLog("Can not search boundary from request", LogLevel.Error);
                return null;
            }
            first += 9;                                     // boundary=.Length
            if (request.Length > 2500 + first)
            {
                subRequest = request.Substring(first, 2000);
            }
            else
            {
                subRequest = request.Substring(first);
            }
            while (!exist)
            {
                if (subRequest[i] == '\r')
                {
                    exist = true;
                }
                else
                {
                    boundary += subRequest[i];
                    i++;
                }
                if (i > 2000)
                {
                    logger.WriteLog("Can not define end of boundary request", LogLevel.Error);
                    return null;
                }
            }
            return boundary;
        }
        public void ErrorJsonRequest(ref string json, ref Socket remoteSocket)
        {
            if (string.IsNullOrEmpty(json))
            {
                string response = "HTTP/1.1 500\r\n" +
                                  "Version: HTTP/1.1\r\n" +
                                  "Content-Type: application/json\r\n" +
                                  "Access-Control-Allow-Headers: *\r\n" +
                                  "Access-Control-Allow-Origin: *\r\n" +
                                  "Content-Length: " + json.Length.ToString() +
                                  "\r\n\r\n" +
                                  json;
                if (remoteSocket.Connected)
                {
                    remoteSocket.Send(Encoding.ASCII.GetBytes(response));
                }
                Debug.WriteLine(response);
                logger.WriteLog("Return http 500 responce with JSON data.", LogLevel.Worker);
            }
        }
    }
}