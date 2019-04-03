using System;
using System.IO;
using System.Data;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using Exceling.NDatabase.LogData;
using Exceling.NDatabase.PriceData;
using Exceling.Functional.FileWork;
using Exceling.NDatabase.UploadData;
using Exceling.NDatabase.ProductData;
using Exceling.NDatabase.CategoryData;

namespace Exceling.NDatabase
{
    public class Database
    {
        public object locker = new object();
        public string defaultNameDB = "excel";
        public MySqlConnectionStringBuilder connectionstring = new MySqlConnectionStringBuilder();
        public MySqlConnection connection;

        public LogStorage log;
        public UploadStorage upload;
        public CategoryStorage category;
        public PriceStorage price;
        public ProductStorage product;

        public List<Storage> storages = new List<Storage>();

        string files_Create = "CREATE TABLE IF NOT EXISTS excel_files" +
        "(" +
            "id int NOT NULL AUTO_INCREMENT," +
            "uid int," +
            "path varchar(255)," +
            "name varchar(100)," +
            "type varchar(20)," +
            "extention varchar(100)," +
            "PRIMARY KEY(id)" +
        ");";
        public Database()
        {
            Console.WriteLine("MySQL connection...");
            string conf_database = GetConfigDatabase();
            GetJsonConfig(conf_database);
            connection = new MySqlConnection(connectionstring.ToString());
            connection.Open();
            SetMainStorages();
            CheckingAllTables();
            Console.WriteLine("MySQL connected.");
        }
        private void SetMainStorages()
        {
            upload = new UploadStorage(ref connection, ref locker);
            category = new CategoryStorage(ref connection, ref locker);
            price = new PriceStorage(ref connection, ref locker);
            product = new ProductStorage(ref connection, ref locker);
            log = new LogStorage(ref connection, ref locker);
            storages.Add(log);
            storages.Add(product);
            storages.Add(upload);
            storages.Add(price);
            storages.Add(category);
            CheckTableExists(files_Create);
        }
        public bool GetJsonConfig(string Json)
        {
            connectionstring.SslMode = MySqlSslMode.None;
            connectionstring.ConnectionReset = true;
            connectionstring.CharacterSet = "UTF8";
            if (Json == "")
            {
                connectionstring.Server = "localhost";
                connectionstring.Database = "databasename";
                connectionstring.UserID = "root";
                connectionstring.Password = "root";
                defaultNameDB = "databasename";
            }
            else
            {
                var configJson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(Json);
                connectionstring.Server = configJson["Server"].ToString();
                connectionstring.Database = configJson["Database"].ToString();
                connectionstring.UserID = configJson["UserID"].ToString();
                connectionstring.Password = configJson["Password"].ToString();
                defaultNameDB = configJson["Database"].ToString();
            }
            Debug.WriteLine("Database configurations:\r\n{0}", connectionstring);
            return true;
        }
        public string GetConfigDatabase()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "/database.conf"))
            {
                using (var fstream = File.OpenRead("database.conf"))
                {
                    byte[] array = new byte[fstream.Length];
                    fstream.Read(array, 0, array.Length);
                    string textFromFile = System.Text.Encoding.Default.GetString(array);
                    fstream.Close();
                    return textFromFile;
                }
            }
            else
            {
                Console.WriteLine("Function getConfigInfoDB() doesn't get database configuration information. Server DB starting with default configuration.");
                return string.Empty;
            }
        }
        public bool CheckingAllTables()
        {
            bool checking = true;
            CheckDatabaseExists();
            foreach (Storage storage in storages)
            {
                if (!CheckTableExists(storage.table))
                {
                    checking = false;
                    Console.WriteLine("The table=" + storage.table_name + " didn't create.");
                }
            }
            CheckTableExists(files_Create);
            Console.WriteLine("The specified tables created.");
            return checking;
        }
        private bool CheckTableExists(string sqlCreateCommand)
        {
            try
            {
                using (MySqlCommand command = new MySqlCommand(sqlCreateCommand, connection))
                {
                    command.ExecuteNonQuery();
                    command.Dispose();
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error function CheckTableExists().\r\n\r\n{0}\r\n\r\nMessage:\r\n\r\n{1}\r\n", e.Message, sqlCreateCommand);
                return false;
            }
        }
        public void DropTables()
        {
            for (int i = storages.Count; i > 0; i--)
            {
                Storage storage = storages[i - 1];
                Console.WriteLine(storage.table_name);
                string command = string.Format("DROP TABLE {0};", storage.table_name);
                lock (locker)
                {
                    using (MySqlCommand commandSQL = new MySqlCommand(command, connection))
                    {
                        commandSQL.ExecuteNonQuery();
                        commandSQL.Dispose();
                    }
                }
            }
        }
        public bool CheckDatabaseExists()
        {
            if (connection.State == ConnectionState.Open)
            {
                using (MySqlCommand command = new MySqlCommand($"SELECT ('{defaultNameDB}');", connection))
                {
                    if (command.ExecuteScalar() != DBNull.Value)
                    {
                        string creatingDB = "CREATE DATABASE IF NOT EXISTS " + defaultNameDB + ";";
                        using (MySqlCommand commandSQL = new MySqlCommand(creatingDB, connection))
                        {
                            command.ExecuteNonQuery();
                            command.Dispose();
                        }
                    }
                    Console.WriteLine("Database->" + defaultNameDB + " exists.");
                    return true;
                }
            }
            Console.WriteLine("Connnection state is not open");
            return false;
        }
        public void AddFile(FileStruct file)
        {
           string command = string.Format("INSERT INTO excel_files(id, uid, path, name, type, extention)" +
            "VALUES('{0}', @uid, '{1}', '{2}', '{3}', '{4}');",
           file.ID, file.Path, file.Name, file.Type, file.Extention);
           lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(command, connection))
                {
                    if (file.UID == -1)
                    {
                        commandSQL.Parameters.AddWithValue("@uid", null);
                    }
                    else
                    {
                        commandSQL.Parameters.AddWithValue("@uid", file.UID);
                    }
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public bool DeleteFileByID(int id)
        {
            string delete = string.Format("DELETE FROM excel_files WHERE id='{0}';", id);
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(delete, connection))
                {
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                    return true;
                }
            }
        }
    }
}
