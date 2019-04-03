using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Exceling.NDatabase;

namespace Exceling
{
    public enum LogLevel { Error, Server, Worker, Tasker, Exceling , FileWork, Mail }

    public class LogProgram
    {
		private string CurrentDirectory = "";
        private string PathLogsDirectory = "/Files/logs/";
        private string fileName = "log";
        private string Full_Path_File = "";
        private string user_computer = Environment.UserName + " " + Environment.MachineName;

        private Database database;
        public bool stateLogging = true;
		private FileStream FileWriter;
		private FileInfo FileLogExist;
        /// <summary>
        /// Initializes a new instance of the LogProgram class. Creating directory and file to logging. 
        /// The input parameter CallDB is needed for logs to be written to the database.
        /// </summary>
        public LogProgram()
        {
            this.database = new Database();
            CurrentDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(CurrentDirectory + PathLogsDirectory);
            Full_Path_File = CurrentDirectory + PathLogsDirectory + fileName;
            FileLogExist = new FileInfo(Full_Path_File);
            FileWriter = new FileStream(Full_Path_File, FileMode.Append, FileAccess.Write);
        }
        /// <summary>
        /// Initializes a new instance of the LogProgram class. Creating directory and file to logging. 
        /// The input parameter CallDB is needed for logs to be written to the database.
        /// </summary>
        /// <param name="db">Database.</param>
		public LogProgram(ref Database db) 
		{
			this.database = db;
            CurrentDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(CurrentDirectory + PathLogsDirectory);
            Full_Path_File = CurrentDirectory + PathLogsDirectory + fileName;
			FileLogExist = new FileInfo(Full_Path_File);
            FileWriter = new FileStream(Full_Path_File, FileMode.Append, FileAccess.Write);
        }
        /// <summary>
        /// Logging.
        /// </summary>
        /// <param name="logCmd">Log cmd. Is a recorded log.</param>
        /// <param name="level">This is the type of log level</param>
        public void WriteLog(string logCmd, LogLevel level)
        {
            if (string.IsNullOrEmpty(logCmd))
            {
                WriteLog("Insert log is null or empty", LogLevel.Error);
                return;
            }
            if (logCmd.Length > 2000)
            {
                logCmd = logCmd.Substring(0, 2000);
            }
            if (stateLogging == true)
            {
                DateTime localDate = DateTime.Now;
                Log loger = new Log
                {
                    log = logCmd,
                    user_computer = user_computer,
                    seconds = (short)localDate.Second,
                    minutes = (short)localDate.Minute,
                    hours = (short)localDate.Hour,
                    day = (short)localDate.Day,
                    month = (short)localDate.Month,
                    year = localDate.Year,
                    level = SetLevelLog(level)
                };
                if (CheckExistLogFile())
                {
                    Write(loger);
                }
            }
            else 
            { 
                Debug.WriteLine(logCmd);
            }
        }
        public bool CheckExistLogFile() 
		{
			if (FileLogExist.Exists) 
			{
				return true;
			}
			else 
			{
				FileStream fs = File.Create(Full_Path_File);
                fs.Close();
                return true;
			}
		}
        /// <summary>
        /// Write log info to txt file.
        /// </summary>
        /// <param name="loger">Loger.</param>
		private void Write(Log loger)
        {
            byte[] array = Encoding.ASCII.GetBytes
            (
                "T/D: " + loger.hours + ":" + loger.minutes + ":" + loger.seconds + "__"
                + loger.day + ":" + loger.month + ":" + loger.year
                + "; " + "User_comp.: " + loger.user_computer + "; " +
                "Log: " + loger.log + "; Level: " + loger.level + ";" + "\r\n"
            );
            FileWriter.Write(array, 0, array.Length);
            FileWriter.Flush();
            database.log.AddLogs(ref loger);
            Debug.WriteLine(loger.log);
		}
        /// <summary>
        /// Return logs wrote this day.
        /// </summary>
        /// <returns>The string logs.</returns>
        public string ReadStringLogs()
        {
			CheckExistLogFile();
            if (FileWriter != null)
            {
                FileWriter.Close();
            }
            using (FileStream fileStream = File.OpenRead(Full_Path_File))
			{
				byte[] array = new byte[fileStream.Length];
				fileStream.Read(array, 0, array.Length);
				string textFromFile = Encoding.Default.GetString(array);
				fileStream.Close();
                FileWriter = new FileStream(Full_Path_File, FileMode.Append, FileAccess.Write);
                return textFromFile;
			}
        }
        /// <summary>
        /// Reads logs from database.
        /// </summary>
        /// <returns>The logs database.</returns>
        public void ReadConsoleLogsDatabase()
        {
            List<Log> logs = database.log.SelectLogs();
            foreach (Log log in logs)
            {
                Debug.WriteLine("Log: " + log.log + ";" + "Data: " + log.year + ":" + log.month + ":" + log.day + ";" +
                "Time: " + log.hours + ":" + log.minutes + ":" + log.seconds + ";" + "Type: " + log.level + ";");
            }
        }
        /// <summary>
        /// Get list of byte (logs) from database.
        /// </summary>
        public byte[] ReadMassiveLogs()
        {
            List<byte> mass = new List<byte>();
            List<Log> logs = database.log.SelectLogs();
            foreach (Log log in logs)
            {
                mass.AddRange(Encoding.ASCII.GetBytes("Log: " + log.log + ";" + "Data: " + log.year + ":" + log.month + ":" + log.day + ";" +
                "Time: " + log.hours + ":" + log.minutes + ":" + log.seconds + ";" + "Type: " + log.level + ";<br>"));                
            }
            return mass.ToArray();
        }
        /// <summary>
        /// Sorts the logs to output html.
        /// </summary>
        /// <returns>The logs to output html.</returns>
        /// <param name="massLogs">Mass logs.</param>
        public string sortLogsToOutputHTML(string massLogs) 
		{
            int lastFindLog = 0;
			int logLength = "Log: ".Length;
			while (true)
			{
				if (lastFindLog >= massLogs.Length - 150) { break; }
				else 
				{
					lastFindLog = massLogs.IndexOf("Log:", lastFindLog, StringComparison.Ordinal); 
                    massLogs = massLogs.Insert(lastFindLog, "<br>");
					lastFindLog += logLength;
				}
			}
			return massLogs;	
		}
        private string SetLevelLog(LogLevel level)
        {
            switch(level)
            {
                case LogLevel.Error: return "error";
                case LogLevel.Server: return "server";
                case LogLevel.Worker: return "worker"; 
                case LogLevel.Tasker: return "tasker";
                case LogLevel.Exceling : return "exceling";
                case LogLevel.FileWork: return "filework";
                case LogLevel.Mail: return "mail";
                default: return "error";
            }
        }
        public void SetUpNewLogFile()
        {
            string answer;
            DateTime time = DateTime.Now;
            string renameFullpath = CurrentDirectory + PathLogsDirectory + time.Month + "." + time.Day + "." + time.Year;
            FileWriter.Close();
            if (Directory.Exists(Path.GetDirectoryName(CurrentDirectory + PathLogsDirectory)))
            {
                if (File.Exists(Full_Path_File))
                {
                    if (!File.Exists(renameFullpath))
                    {
                        File.Move(Full_Path_File, renameFullpath);
                        FileStream creating = File.Create(Full_Path_File);
                        creating.Close();
                        FileWriter = new FileStream(Full_Path_File, FileMode.Append, FileAccess.Write);
                        FileLogExist = new FileInfo(Full_Path_File);
                        WriteLog("Set new file for logs.", LogLevel.FileWork);
                        return;
                    }
                    else
                    {
                        answer = "Dont set new file, because has the same another.";
                    }

                }
                else
                {
                    answer = "Log file is not exists.";
                }
            }
            else
            {
                answer = "Dont set new file, because has not create set directory.";
            }
            FileWriter = new FileStream(Full_Path_File, FileMode.Append, FileAccess.Write);
            FileLogExist = new FileInfo(Full_Path_File);
            WriteLog(answer, LogLevel.Error);
        }
    }
	public class Log 
	{
        public string log;
        public string user_computer;
        public short seconds;
		public short minutes;
        public short hours;
        public short day;
        public short month;
        public int year;
        public string level;
    }
}
