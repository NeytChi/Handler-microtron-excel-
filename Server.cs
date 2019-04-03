using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using Exceling.NDatabase;
using System.Text.RegularExpressions;

namespace Exceling
{
    public class Server
    {
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private Database database = new Database();
        public Worker worker;
        private int Port = 8021;
        public string Host = "127.0.0.1";
        private IPEndPoint iPEndPoint;
        public LogProgram logger;
        private Random random = new Random();
        private readonly string[] methods = { "GET", "POST", "OPTIONS" };

        public Server()
        {
            logger.stateLogging = false;
            Console.WriteLine("Server run without initiation socket listen.");
        }
        public Server(int Port, string IP)
        {
            this.Host = IP;
            this.Port = Port;
            iPEndPoint = new IPEndPoint(IPAddress.Parse(Host), Port);
            logger = new LogProgram(ref database);
            worker = new Worker(ref database,ref logger);
            Console.WriteLine("Server run./ Host:port = " + Host + ":" + Port);
            logger.WriteLog("Server run. Host:port = " + Host + ":" + Port, LogLevel.Server);
        }
        public void InitListenSocket()
        {
            socket.Bind(iPEndPoint);
            socket.Listen(1000);
            while (true)
            {
                Socket handleSocket = socket.Accept();
                Thread thread = new Thread(() => ReceivedSocketData(ref handleSocket))
                {
                    IsBackground = true
                };
                thread.Start();
            }
        }
        private void ReceivedSocketData(ref Socket handleSocket)
        {
            try
            {
                byte[] buffer = new byte[1096];
                int bytes = 0;
                string request = "";
                int ContentLength = 0;
                for (; ; )
                {
                    if (buffer.Length < bytes + 300)
                    {
                        Array.Resize(ref buffer, bytes + 2000);
                    }
                    else
                    {
                        bytes += handleSocket.Receive(buffer, bytes, 60, SocketFlags.None);
                    }
                    if (bytes > 500 && bytes < 1000 && buffer.Length == 1096)
                    {
                        request = Encoding.ASCII.GetString(buffer, 0, bytes);
                        if (request.Contains("content-length:") || request.Contains("Content-Length:"))
                        {
                            ContentLength = GetRequestContentLenght(ref request);
                            if (ContentLength > 0 && ContentLength < 210000000)
                            {
                                Array.Resize(ref buffer, ContentLength + bytes);
                            }
                            else if (ContentLength > 210000000) handleSocket.Close();
                        }
                    }
                    if (handleSocket.Available == 0 && bytes >= ContentLength) { break; }
                    if (handleSocket.Available == 0 && bytes < ContentLength)
                    {
                        if ((handleSocket.Poll(10000, SelectMode.SelectRead) && (handleSocket.Available == 0)) || !handleSocket.Connected)
                        {
                            handleSocket.Close();
                            logger.WriteLog("Remote socket was disconnected.", LogLevel.Server);
                            break;
                        }
                    }
                }
                if (handleSocket.Connected)
                {
                    if (bytes < 210000000)
                    {
                        request = Encoding.ASCII.GetString(buffer, 0, bytes);
                        IdentifyRequestMethod(ref request,ref handleSocket,ref buffer,ref bytes);
                    }
                    else
                    {
                        HttpIternalServerError(ref handleSocket);
                    }
                }
                if (handleSocket.Connected) { handleSocket.Close(); }
            }
            catch (Exception e)
            {
                logger.WriteLog("Error in function ReceivedSocketData, message:" + e.Message, LogLevel.Error);
            }
        }
        private void IdentifyRequestMethod(ref string ReceivedRequest, ref Socket handleSocket, ref byte[] ReceivedBuffer, ref int ReceivedBytes)
        {
            Debug.WriteLine("Request:");
            Debug.WriteLine(ReceivedRequest);
            string url = GetMethodRequest(ref ReceivedRequest);
            switch (url)
            {
                case "GET":
                    HandleGetRequest(ReceivedRequest, handleSocket);
                    break;
                case "POST":
                    HandlePostRequest(ReceivedRequest, handleSocket, ReceivedBuffer, ReceivedBytes);
                    break;
                default:
                    HttpErrorUrl(ref handleSocket);
                    break;
            }
        }
        public void HandleGetRequest(string ReceivedRequest, Socket handleSocket)
        {
            string answer = null;
            switch (FindURLRequest(ReceivedRequest, "GET").ToLower())
            {
                case "selectcategory": answer = worker.SelectCategories(ref ReceivedRequest, ref handleSocket);
                    SendJsonRequest(ref answer, ref handleSocket);
                    break;
                case "searchproducts": answer = worker.SearchProductByName(ref ReceivedRequest, ref handleSocket);
                    SendJsonRequest(ref answer, ref handleSocket);
                    break;
                case "selecthistory": answer = worker.SelectHistoryProduct(ref ReceivedRequest, ref handleSocket);
                    SendJsonRequest(ref answer, ref handleSocket);
                    break;
                default: HttpErrorUrl(ref handleSocket);
                    break;
            }
        }
        public void HandlePostRequest(string ReceivedRequest, Socket handleSocket, byte[] buffer, int bytes)
        {
            string answer = null;
            switch (FindURLRequest(ReceivedRequest, "POST").ToLower())
            {
                case "addxls": answer = worker.AddXls(ref ReceivedRequest,ref buffer, ref bytes,ref handleSocket);
                    SendJsonRequest(ref answer, ref handleSocket);
                    break;
                default: HttpErrorUrl(ref handleSocket);
                    break;
            }
        }
        /// <summary>
        /// Finds the URL in request.
        /// </summary>
        /// <returns>The URLR equest.</returns>
        /// <param name="request">Request.</param>
        /// <param name="method">Method.</param>
        private string FindURLRequest(string request, string method)
        {
            string url = GetBetween(ref request, method + " ", " HTTP/1.1");
            if (url == null)
            {
                return null;
            }
            int questionUrl = url.IndexOf('?', 1);
            if (questionUrl == -1)
            {
                url = url.Substring(1);
                if (url[url.Length - 1] != '/')
                {
                    return url.ToLower();                                       // handle this pattern url -> /Log || /Log/Level
                }
                else
                {
                    return url.Remove(url.Length - 1).ToLower();                // handle this pattern url -> /Log/ || /Log/Level/
                }
            }
            else
            {
                if (url[questionUrl - 1] == '/')                                // handle this pattern url -> /LogInfo/Account/?id=1111 -> /LogInfo/Account/
                {
                    return url.Substring(1, questionUrl - 2).ToLower();         // handle this pattern url -> Log/Account - return
                }
                else
                {
                    logger.WriteLog("Can not define pattern of url, function FindURLRequest()", LogLevel.Error);
                    return "";                                                  // Don't handle this pattern url -> /LogInfo?id=1111 and /LogInfo/Account?id=9999 
                }
            }
        }
        public string GetBetween(ref string source, string start, string end)
        {
            if (!string.IsNullOrEmpty(source))
            {
                if (source.Contains(start) && source.Contains(end))
                {
                    int Start = source.IndexOf(start, 0, StringComparison.Ordinal) + start.Length;
                    if (Start == -1)
                    {
                        logger.WriteLog("Can not find start of source, function GetBetween()", LogLevel.Error);
                        return null;
                    }
                    int End = source.IndexOf(end, Start, StringComparison.Ordinal);
                    if (End == -1)
                    {
                        logger.WriteLog("Can not find end of source, function GetBetween()", LogLevel.Error);
                        return null;
                    }
                    if (End > Start)
                    {
                        return source.Substring(Start, End - Start);
                    }
                    else
                    {
                        logger.WriteLog("Can not get between, because end > start, function GetBetween()", LogLevel.Error);
                        return null;
                    }
                }
                else
                {
                    logger.WriteLog("Source does not contains search values, function GetBetween()", LogLevel.Error);
                    return null;
                }
            }
            else
            {
                logger.WriteLog("Source is null or empty, function GetBetween()", LogLevel.Error);
                return null;
            }
        }
        /// <summary>
        /// Finds the parameter from request. If success = false return empty string.
        /// </summary>
        /// <returns>The parameter from request.</returns>
        /// <param name="request">Request.</param>
        /// <param name="key">Key.</param>
        private string FindParamFromRequest(ref string request, string key)
        {
            string patternKeyValue = @"[\?&](" + key + @"=([^&=#\s]*))";
            Regex urlParams = new Regex(patternKeyValue, RegexOptions.Multiline);
            Match match = urlParams.Match(request);
            if (match.Success)
            {
                string value = match.Value;
                logger.WriteLog("Define parameter from request", LogLevel.Server);
                return value.Substring(key.Length + 2);
            }
            else
            {
                logger.WriteLog("Can not define parameter from request, match->success == false", LogLevel.Error);
                return null;
            }
        }
        private void SendJsonRequest(ref string json,ref Socket remoteSocket)
        {
            if (string.IsNullOrEmpty(json))
            {
                logger.WriteLog("Can not send json request, input value is null or empty", LogLevel.Error);
                return;
            }
            json = EncodeNonAsciiCharacters(ref json);
            string response = "HTTP/1.1 200\r\n" +
                              "Version: HTTP/1.1\r\n" +
                              "Content-Type: application/json\r\n" +
                              "Content-Length: " + (json.Length).ToString() +
                              "\r\n\r\n" +
                              json;
            if (remoteSocket.Connected)
            {
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
                Debug.WriteLine(response);
                logger.WriteLog("Return http 200 JSON answer", LogLevel.Worker);
            }
            else
            {
                logger.WriteLog("Can not send json request, remote socket was disconnected", LogLevel.Error);
                return;
            }
        }
        private void HttpIternalServerError(ref Socket remoteSocket)
        {
            string responseBody = "<HTML>" +
                                 "<BODY>" +
                                 "<h1> 500 Internal Server Error...</h1>" +
                                 "</BODY>" +
                                 "</HTML>";
            string response = "HTTP/1.1 500 \r\n" +
                       "Version: HTTP/1.1\r\n" +
                       "Content-Type: text/html; charset=utf-8\r\n" +
                       "Content-Length: " + (responseBody.Length + responseBody.Length) +
                       "\r\n\r\n" +
                       responseBody;
            if (remoteSocket.Connected)
            {
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            }
            logger.WriteLog("HTTP 500 Error link response", LogLevel.Error);
        }
        private void HttpErrorUrl(ref Socket remoteSocket)
        {
            string response = "";
            string responseBody = "";
            responseBody = "<HTML><BODY>error url to connect...</BODY></HTML>";
            response = "HTTP/1.1 400 \r\n" +
                       "Version: HTTP/1.1\r\n" +
                       "Content-Type: text/html; charset=utf-8\r\n" +
                       "Content-Length: " + (response.Length + responseBody.Length) +
                       "\r\n\r\n" +
                       responseBody;
            if (remoteSocket.Connected)
            {
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            }
            logger.WriteLog("HTTP 400 Error link response", LogLevel.Error);
        }
        private int ConvertSaveString(string resouce)
        {
            if (string.IsNullOrEmpty(resouce))
            {
                logger.WriteLog("Can not convert save string, input value is null or empty", LogLevel.Error);
                return -1;
            }
            try
            {
                return Convert.ToInt32(resouce);
            }
            catch
            {
                logger.WriteLog("Can not convert save string, funciton convert was disabled", LogLevel.Error);
                return -1;
            }
        }
        /// <summary>
        /// Gets value "content lenght" from request.
        /// </summary>
        /// <returns>The request content lenght.</returns>
        /// <param name="request">Picie of request.</param>
        public int GetRequestContentLenght(ref string request)
        {
            Regex contentlength = new Regex("ength: [0-9]*\r\n", RegexOptions.Compiled);
            Match resultContentLength = contentlength.Match(request);
            if (resultContentLength.Success)
            {
                return Convert.ToInt32(resultContentLength.Value.Substring("ength: ".Length)) + resultContentLength.Index + resultContentLength.Length;
            }
            else
            {
                logger.WriteLog("Can not get request content length, function GetRequestContentLength()", LogLevel.Error);
                return 0;
            }
        }
        public string GetMethodRequest(ref string request)
        {
            if (!string.IsNullOrEmpty(request))
            {
                if (request.Length < 20)
                {
                    logger.WriteLog("Request length < 20 chars, can not get method of request", LogLevel.Error);
                    return null;
                }
                string requestMethod = request.Substring(0, 20);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (requestMethod.Contains(methods[i]))
                    {
                        int start = request.IndexOf(methods[i], 0, StringComparison.Ordinal);
                        return request.Substring(start, methods[i].Length);
                    }
                }
                logger.WriteLog("Can not define method request, server !Contains methods of request ", LogLevel.Error);
                return null;
            }
            else
            {
                logger.WriteLog("input_value == null or empty, function GetMethodRequest()", LogLevel.Error);
                return null;
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
    }
}
