using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Exceling.NDatabase.UploadData
{
    public class UploadStorage : Storage
    {
        public string table_nameU = "excel_uploads";
        public string tableU = "CREATE TABLE IF NOT EXISTS excel_uploads" +
    	"(" +
    	    "upload_id int AUTO_INCREMENT," +
    	    "upload_name varchar(30)," +
    	    "created_at int," +
    	    "PRIMARY KEY (upload_id)" +
    	");";
        private string insertUpload = "INSERT INTO excel_uploads(upload_name, created_at) " +
            "VALUES(@upload_name, @created_at)";
            
        private string checkUpload = "SELECT * FROM excel_uploads WHERE upload_name=@upload_name;";

        public UploadStorage(ref MySqlConnection connection, ref object locker)
        {
            this.connection = connection;
            this.locker = locker;
            this.table_name = table_nameU;
            this.table = tableU;
        }
        public Upload AddUpload(ref Upload upload)
        {
            lock(locker)
            {
                if (upload.upload_name != null)
                {
                    if (upload.upload_name.Length > 30)
                    {
                        upload.upload_name = upload.upload_name.Substring(0, 30);
                    }
                }
                using (MySqlCommand command = new MySqlCommand(insertUpload, connection))
                {
                    command.Parameters.AddWithValue("@upload_name", upload.upload_name);
                    command.Parameters.AddWithValue("@created_at", upload.created_at);
                    command.ExecuteNonQuery();
                    upload.upload_id  = (int)command.LastInsertedId;
                    command.Dispose();
                }
            }
            return upload;
        }
        public List<Upload> GetListUploads()
        {
            List<Upload> uploads = new List<Upload>();
            lock (locker)
            {
                using (MySqlCommand command = new MySqlCommand(insertUpload, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            Upload upload = new Upload();
                            upload.upload_id = reader.GetInt32(0);
                            upload.upload_name = reader.GetString(1);
                            upload.created_at = reader.GetInt32(2);
                            uploads.Add(upload);
                        }
                    }
                }
            }
            return uploads;
        }
        public bool CheckUploadExist(ref string upload_name)
        {
            lock (locker)
            {
                using (MySqlCommand command = new MySqlCommand(checkUpload, connection))
                {
                    command.Parameters.AddWithValue("@upload_name", upload_name);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
    }
}
