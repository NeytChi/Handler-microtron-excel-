using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Exceling.NDatabase.PriceData
{
    public class PriceStorage : Storage
    {
        public string table_nameP = "excel_prices";
        public string tableP = "CREATE TABLE IF NOT EXISTS excel_prices" +
        "(" +
            "price_id bigint AUTO_INCREMENT," +
            "product_code int NOT NULL," +
            "price double," +
            "ordering varchar(256)," +
            "created_at varchar(16)," +
            "created_at_D DATETIME," + 
            "PRIMARY KEY (price_id)," +
            "FOREIGN KEY (product_code) REFERENCES excel_products(product_code)" +
        ");";
        private string InsertPrice = "INSERT INTO excel_prices(product_code, price, ordering, created_at, created_at_D) " +
            "VALUES(@product_code, @price, @ordering, @created_at, @created_at_D);";

        public PriceStorage(ref MySqlConnection connection, ref object locker)
        {
            table_name = table_nameP;
            table = tableP;
            this.connection = connection;
            this.locker = locker;
        }
        public void AddPriceProduct(ref ProductCell product, ref DateTime created_at_D)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(InsertPrice, connection))
                {
                    commandSQL.Parameters.AddWithValue("@product_code", product.product_code);
                    commandSQL.Parameters.AddWithValue("@price", product.product_price);
                    commandSQL.Parameters.AddWithValue("@ordering", product.product_order);
                    commandSQL.Parameters.AddWithValue("@created_at", product.created_at);
                    commandSQL.Parameters.AddWithValue("@created_at_D", created_at_D);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public List<Price> SelectHistory(ref int? product_code)
        {
            List<Price> prices = new List<Price>();
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_prices WHERE product_code='" + product_code + "';", connection))
            {
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        while (readerMassive.Read())
                        {
                            Price price = new Price();
                            price.price_id = readerMassive.GetInt64(0);
                            price.code_product = readerMassive.GetInt32(1);
                            price.price = (readerMassive.IsDBNull(2)) ? -1 : readerMassive.GetDouble(2);
                            price.price_ordering = (readerMassive.IsDBNull(3)) ? "" : readerMassive.GetString(3);
                            price.created_at = readerMassive.GetString(4);
                            prices.Add(price);
                        }
                        commandSQL.Dispose();
                        return prices;
                    }
                }
            }
        }
        public List<Price> SelectHistory(ref int? product_code, ref DateTime time_from, ref DateTime time_to)
        {
            List<Price> prices = new List<Price>();
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_prices WHERE product_code=@product_code AND created_at_D>@time_from AND created_at_D<@time_to;", connection))
            {
                commandSQL.Parameters.AddWithValue("@product_code", product_code);
                commandSQL.Parameters.AddWithValue("@time_from", time_from);
                commandSQL.Parameters.AddWithValue("@time_to", time_to);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        while (readerMassive.Read())
                        {
                            Price price = new Price();
                            price.price_id = readerMassive.GetInt64(0);
                            price.code_product = readerMassive.GetInt32(1);
                            price.price = readerMassive.GetDouble(2);
                            price.price_ordering = readerMassive.GetString(3);
                            price.created_at = readerMassive.GetString(4);
                            prices.Add(price);
                        }
                        commandSQL.Dispose();
                        return prices;
                    }
                }
            }
        }
    }
}
