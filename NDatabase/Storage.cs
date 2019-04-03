using System;
using MySql.Data.MySqlClient;

namespace Exceling.NDatabase
{
    public class Storage
    {
        public object locker;
        public MySqlConnection connection;
        public string table_name;
        public string table;
    }
}

