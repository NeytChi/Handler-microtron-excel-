using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Exceling.NDatabase.LogData
{
    public class LogStorage : Storage
    {
        public string table_nameL = "excel_logs";
        public string tableL = "CREATE TABLE IF NOT EXISTS excel_logs" +
        "(" +
            "log varchar(2000)," +
            "user_computer varchar(255)," +
            "seconds tinyint," +
            "minutes tinyint," +
            "hours tinyint," +
            "day tinyint," +
            "month tinyint," +
            "year int," +
            "level varchar(20)" +
        ");";

        public LogStorage(ref MySqlConnection connection, ref object locker)
        {
            table_name = table_nameL;
            table = tableL;
            this.connection = connection;
            this.locker = locker;
        }
        public void AddLogs(ref Log loger)
        {
            string command = string.Format("INSERT INTO excel_logs( log, user_computer, seconds, minutes, hours, day, month, year, level) " +
                "VALUES( '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}');",
            loger.log, loger.user_computer, loger.seconds, loger.minutes, loger.hours, loger.day, loger.month, loger.year, loger.level);
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(command, connection))
                {
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public List<Log> SelectLogs()
        {
            List<Log> logs = new List<Log>();
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_logs;", connection))
            {
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        while (readerMassive.Read())
                        {
                            Log logi = new Log
                            {
                                log = readerMassive.GetString("log"),
                                user_computer = readerMassive.GetString("user_computer"),
                                seconds = readerMassive.GetInt16("seconds"),
                                minutes = readerMassive.GetInt16("minutes"),
                                hours = readerMassive.GetInt16("hours"),
                                day = readerMassive.GetInt16("day"),
                                month = readerMassive.GetInt16("month"),
                                year = readerMassive.GetInt32("year"),
                                level = readerMassive.GetString("level")
                            };
                            logs.Add(logi);
                        }
                        commandSQL.Dispose();
                        return logs;
                    }
                }
            }
        }
    }
}
