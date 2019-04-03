using System;
using System.IO;
using System.Text;
using Exceling.NDatabase;
using System.Text.RegularExpressions;

namespace Exceling.Functional.FileWork
{
    public class LoaderFile
    {
        public readonly string Current_Directory = Directory.GetCurrentDirectory();
        public readonly string Path_Zip_Save = "";
        private Database database;
        private LogProgram logger;
        private Random random = new Random();
        public string pathToFiles = "/Files/Xlsx/";
        private Regex ContentDisposition = new Regex("Content-Disposition: form-data;" +
                                                     " name=\"(.*)\"; filename=\"(.*)\"\r\n" +
                                                     "Content-Type: (.*)\r\n\r\n",RegexOptions.Compiled);
        public LoaderFile(ref Database callDB,ref LogProgram logProgram)
        {
            this.database = callDB;
            this.logger = logProgram;
            Config config = new Config();
            pathToFiles = config.currentDirectory + pathToFiles;
            Path_Zip_Save = Current_Directory + "/Files/";
        }
        public FileStruct AddFile(ref string request,ref byte[] buffer,ref int bytes)
        {
            if (CheckHeadersFileRequest(ref request))
            {
                byte[] binFile = GetBinaryRequest(request, buffer, bytes);
                if (binFile == null)
                {
                    return null;
                }
                else
                {
                    FileStruct file = GetFileStructRequest(ref request);
                    file.Name = GetFileName(request);
                    CreateFileBinary(file.Path + file.Name,ref binFile);
                    database.AddFile(file);
                    return file;
                }
            }
            else { return null; }
        }
        public FileStruct GetFileStructRequest(ref string request) 
        {
            string subContentType = GetContentType(request.Substring(request.IndexOf("boundary=", StringComparison.Ordinal)));
            FileStruct file = new FileStruct
            {
                Type = IdentifyFileType(subContentType),
                ID = random.Next(1, 999999999),
                Name = GenerateIdName(),
                Extention = IdentifyFileExtention(subContentType)
            };
            Directory.CreateDirectory(pathToFiles);
            file.Path = pathToFiles;
            return file;
        }
        public bool CheckHeadersFileRequest(ref string request)
        {
            if (request.Contains("Content-Type: multipart/form-data") || request.Contains("content-type: multipart/form-data"))
            {
                if (request.Contains("boundary="))
                {
                    if (request.Contains("Connection: keep-alive") || request.Contains("connection: keep-alive"))
                    {
                        return true;
                    }
                    else
                    {
                        logger.WriteLog("Can not find (connection: keep-alive) in request, function CheckHeadersFileRequest", LogLevel.Error);
                        return false;
                    }
                }
                else
                {
                    logger.WriteLog("Can not find (boundary=) in request, function CheckHeadersFileRequest", LogLevel.Error);
                    return false;
                }
            }
            else
            {
                logger.WriteLog("Can not find (Content-Type: multipart/form-data) in request, function CheckHeadersFileRequest", LogLevel.Error);
                return false;
            }
        }
        public byte[] GetBinaryRequest(string request, byte[] buffer, int bytes)
        {
            string boundary = GetBoundary(ref request);
            string endBoundary = "--" + boundary;
            string subContentType = GetContentType(request.Substring(request.IndexOf(boundary, StringComparison.Ordinal)));
            if (subContentType == "") 
            { 
                return null; 
            }
            int startBinaryFile = request.IndexOf(subContentType, StringComparison.Ordinal) + subContentType.Length + "\r\n\r\n".Length;
            string fileWithLastBoundary = Encoding.ASCII.GetString(buffer, bytes - 100, 100);
            if (fileWithLastBoundary == "") 
            { 
                return null; 
            }
            if (!fileWithLastBoundary.Contains(endBoundary)) 
            {
                return null; 
            }
            byte[] binRequestPart = Encoding.ASCII.GetBytes(request.Substring(0, startBinaryFile));
            byte[] binBoundaryLast = Encoding.ASCII.GetBytes(endBoundary);
            int fileLength = bytes - binRequestPart.Length - binBoundaryLast.Length;
            byte[] binFile = new byte[fileLength];
            Array.Copy(buffer, binRequestPart.Length, binFile, 0, fileLength);
            return binFile;
        }
        public string GenerateIdName()
        {
            int firstArg = random.Next(100000000, 999999999);
            int secondArg = random.Next(100000, 999999);
            string id = firstArg.ToString() + secondArg.ToString();
            return id;
        }
        public string IdentifyFileExtention(string extention)
        {
            if (extention.Contains("image"))
            {
                return extention.Substring(extention.IndexOf("image/", StringComparison.Ordinal) + "image/".Length);
            }
            else if (extention.Contains("video"))
            {
                return extention.Substring(extention.IndexOf("video/", StringComparison.Ordinal) + "video/".Length);
            }
            else if (extention.Contains("audio"))
            {
                return extention.Substring(extention.IndexOf("audio/", StringComparison.Ordinal) + "audio/".Length);
            }
            else if (extention.Contains("application"))
            {
                return extention.Substring(extention.IndexOf("application/", StringComparison.Ordinal) + "application/".Length);
            }
            else return "";
        }
        public string IdentifyFileType(string extention)
        {
            if (extention.Contains("image"))
            {
                return "image";
            }
            else if (extention.Contains("video"))
            {
                return "video";
            }
            else if (extention.Contains("audio"))
            {
                return "audio";
            }
            else if (extention.Contains("application"))
            {
                return "application";
            }
            else return "";
        }
        public string GetFileName(string substring)
        {
            int first, last;
            first = (substring.IndexOf("filename=\"", StringComparison.Ordinal)) + "filename=\"".Length;
            last = substring.IndexOf("\"", first, StringComparison.Ordinal);
            string filename = substring.Substring(first, (last - first));
            return filename;
        }
        public string GetContentType(string substring)
        {
            bool exist = false;
            string contentType = "";
            int i = 0;
            int first = (substring.IndexOf("Content-Type: ", StringComparison.Ordinal)) + "Content-Type: ".Length;
            if (first == -1) { return ""; }
            string subRequest = substring.Substring(first);
            while (!exist)
            {
                if (subRequest[i] == '\r')
                {
                    exist = true;
                }
                else
                {
                    contentType += subRequest[i];
                    i++;
                }
                if (i > 2000) { return ""; }
            }
            return contentType;
        }
        public string GetBoundary(ref string request)
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
        public bool CreateFileBinary(string FullPathToSave, ref byte[] byteArray)
        {
            try
            {
                using (Stream fileStream = new FileStream(FullPathToSave, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(byteArray, 0, byteArray.Length);
                    fileStream.Close();
                }
                logger.WriteLog("Get file from request.", LogLevel.FileWork);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exeception caught in createFileFromByte: {0}", e.Message);
                logger.WriteLog("Exeception caught in createFileFromByte:" + e.Message, LogLevel.Error);
                return false;
            }
        }
        public string SearchPathToFile(string nameFile, string startSearchFolder)
        {
            string findPathFile = "";
            string pathCurrent = startSearchFolder;
            string[] files = Directory.GetFiles(pathCurrent);
            foreach (string file in files)
            {
                if (file == pathCurrent + "/" + nameFile) { return file; }
            }
            string[] folders = Directory.GetDirectories(pathCurrent);
            foreach (string folder in folders)
            {
                FileAttributes attr = File.GetAttributes(folder);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    findPathFile = SearchPathToFile(nameFile, folder);
                }
            }
            return findPathFile;
        }
        public bool DeleteFile(ref FileStruct file)
        {
            File.Delete(file.Path + file.Name);
            database.DeleteFileByID(file.ID);
            logger.WriteLog("Delete file id=" + file.ID, LogLevel.FileWork);
            return true;
        }
    }
}