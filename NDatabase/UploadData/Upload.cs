using System;

namespace Exceling.NDatabase.UploadData
{
    public class Upload
    {
        public int upload_id;
        public string upload_name;
        public int created_at;
        public Upload()
        { }
        public Upload(string name, DateTime time)
        {
            this.upload_name = name;
            this.created_at = (int)(time.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds; ;
        }
    }
}
