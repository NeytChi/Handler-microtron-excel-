using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Exceling.NDatabase.ProductData
{
    public class ProductStorage : Storage
    {
        public string table_nameP = "excel_products";
        public string tableP = "CREATE TABLE IF NOT EXISTS excel_products" +
        "(" +
            "product_id int AUTO_INCREMENT," +
            "category_id int," +
            "subcat_id int," +
            "second_subcat_id int," +
            "product_code int NOT NULL UNIQUE," +
            "product_vendore_code varchar(256)," +
            "product_name text(1256)," +
            "product_price double," +
            "product_ordering varchar(256)," +
            "product_link_1 varchar(256)," +
            "product_link_2 varchar(256)," +
            "product_comment varchar(256)," +
            "created_at varchar(16)," +
            "product_url_image text(500)," +
            "PRIMARY KEY (product_id)" +
        ");";

        private string InsertProduct = "INSERT INTO excel_products(category_id, subcat_id, second_subcat_id, product_code, product_vendore_code, product_name, product_price, product_ordering, product_link_1, product_link_2, product_comment, created_at, product_url_image) " +
            "VALUES( @category_id, @subcat_id, @second_subcat_id, @product_code, @product_vendore_code, @product_name, @product_price, @product_ordering, @product_link_1, @product_link_2, @product_comment, @created_at, @product_url_image);";

        private string CheckProductCode = "SELECT product_code FROM excel_products WHERE product_code=@product_code;";

        private string UpdatePriceById = "UPDATE excel_products SET product_price=@product_price WHERE product_code=@product_code;";


        public ProductStorage(ref MySqlConnection connection, ref object locker)
        {
            table = tableP;
            table_name = table_nameP;
            this.connection = connection;
            this.locker = locker;
        }
        public void AddProduct(ref ProductCell product)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(InsertProduct, connection))
                {
                    commandSQL.Parameters.AddWithValue("@category_id", product.category);
                    commandSQL.Parameters.AddWithValue("@subcat_id", product.subcat);
                    commandSQL.Parameters.AddWithValue("@second_subcat_id", product.second_subcat);
                    commandSQL.Parameters.AddWithValue("@product_code", product.product_code);
                    commandSQL.Parameters.AddWithValue("@product_vendore_code", product.product_vendore_code);
                    commandSQL.Parameters.AddWithValue("@product_name", product.product_name);
                    commandSQL.Parameters.AddWithValue("@product_price", product.product_price);
                    commandSQL.Parameters.AddWithValue("@product_ordering", product.product_order);
                    commandSQL.Parameters.AddWithValue("@product_link_1", product.product_link_1);
                    commandSQL.Parameters.AddWithValue("@product_link_2", product.product_link_2);
                    commandSQL.Parameters.AddWithValue("@product_comment", product.product_comment);
                    commandSQL.Parameters.AddWithValue("@created_at", product.created_at);
                    commandSQL.Parameters.AddWithValue("@product_url_image", product.product_url_image);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public List<ProductCell> SelectProducts()
        {
            List<ProductCell> products;
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_products;", connection))
            {
                products = GetMassiveProducts(commandSQL);
            }
            return products;
        }
        public List<ProductCell> SelectProductsWithSearch(ref string searchName, int since, int count)
        {
            List<ProductCell> products;
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_products" +
            	" WHERE product_code LIKE '%" + searchName + "%' OR product_vendore_code LIKE '%" + searchName + "%' OR product_name LIKE '%" + searchName + "%'" +
            	" ORDER BY product_id DESC LIMIT " + since + ", " + count + ";", connection))
            {
                products = GetMassiveProducts(commandSQL);
            }
            return products;
        }
        public List<ProductCell> SelectProducts(int category, int subcat, int second_subcat)
        {
            List<ProductCell> products;
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_products WHERE category_id=@category_id AND subcat_id=@subcat_id AND second_subcat_id=@second_subcat_id;", connection))
            {
                commandSQL.Parameters.AddWithValue("@category_id", category);
                commandSQL.Parameters.AddWithValue("@subcat_id", subcat);
                commandSQL.Parameters.AddWithValue("@second_subcat_id", second_subcat);
                products = GetMassiveProducts(commandSQL);
            }
            return products;
        }
        public List<ProductCell> SelectProductsByCategory(int category, int subcat, int second_subcat)
        {
            List<ProductCell> products;
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_products WHERE category_id=@category_id AND subcat_id=@subcat_id AND second_subcat_id=@second_subcat_id;", connection))
            {
                commandSQL.Parameters.AddWithValue("@category_id", category);
                commandSQL.Parameters.AddWithValue("@subcat_id", subcat);
                commandSQL.Parameters.AddWithValue("@second_subcat_id", second_subcat);
                products = GetMassiveProducts(commandSQL);
            }
            return products;
        }
        public List<ProductCell> SelectProductsByCategory(int category, int position)
        {
            List<ProductCell> products = new List<ProductCell>();
            if (position == 1)
            {
                using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_products WHERE category_id=@category_id AND subcat_id=@subcat_id AND second_subcat_id=@second_subcat_id;", connection))
                {
                    commandSQL.Parameters.AddWithValue("@category_id", category);
                    commandSQL.Parameters.AddWithValue("@subcat_id", -1);
                    commandSQL.Parameters.AddWithValue("@second_subcat_id", -1);
                    products = GetMassiveProducts(commandSQL);
                }
            }
            if (position == 2)
            {
                using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_products WHERE subcat_id=@subcat_id AND second_subcat_id=@second_subcat_id;", connection))
                {
                    commandSQL.Parameters.AddWithValue("@subcat_id", category);
                    commandSQL.Parameters.AddWithValue("@second_subcat_id", -1);
                    products = GetMassiveProducts(commandSQL);
                }
            }    
            if (position == 3)
            {
                using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_products WHERE second_subcat_id=@second_subcat_id;", connection))
                {
                    commandSQL.Parameters.AddWithValue("@second_subcat_id", category);
                    products = GetMassiveProducts(commandSQL);
                }
            }
            return products;
        }
        public List<int> FindNextStepCategories(ref int category_id, int position)
        {
            int categories_id;
            List<int> findingCategories = new List<int>();
            if (position == 1)
            {
                using (MySqlCommand commandSQL = new MySqlCommand("SELECT subcat_id FROM excel_products WHERE category_id=@category_id;", connection))
                {
                    commandSQL.Parameters.AddWithValue("@category_id", category_id);
                    lock (locker)
                    {
                        using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                        {
                            while (readerMassive.Read())
                            {
                                categories_id = readerMassive.GetInt32("subcat_id");
                                if (!findingCategories.Contains(categories_id))
                                {
                                    findingCategories.Add(categories_id);
                                }
                            }
                        }
                    }
                }
            }
            else if (position == 2)
            {
                using (MySqlCommand commandSQL = new MySqlCommand("SELECT second_subcat_id FROM excel_products WHERE subcat_id=@subcat_id;", connection))
                {
                    commandSQL.Parameters.AddWithValue("@subcat_id", category_id);
                    lock (locker)
                    {
                        using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                        {
                            while (readerMassive.Read())
                            {
                                categories_id = readerMassive.GetInt32("second_subcat_id");
                                if (!findingCategories.Contains(categories_id))
                                {
                                    findingCategories.Add(categories_id);
                                }
                            }
                        }
                    }
                }
            }
            else { }
            return findingCategories;
        }
        private List<ProductCell> GetMassiveProducts(MySqlCommand commandSQL)
        {
            List<ProductCell> products = new List<ProductCell>();
            lock (locker)
            {
                using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                {
                    while (readerMassive.Read())
                    {
                        ProductCell product = new ProductCell();
                        product.product_id = (readerMassive.IsDBNull(0)) ? -1 : readerMassive.GetInt32(0);
                        product.category = (readerMassive.IsDBNull(1)) ? -1 : readerMassive.GetInt32(1);
                        product.subcat = (readerMassive.IsDBNull(2)) ? -1 : readerMassive.GetInt32(2);
                        product.second_subcat = (readerMassive.IsDBNull(3)) ? -1 : readerMassive.GetInt32(3);
                        product.product_code = readerMassive.GetInt32(4);
                        product.product_vendore_code = (readerMassive.IsDBNull(5)) ? "" : readerMassive.GetString(5);
                        product.product_name = (readerMassive.IsDBNull(6)) ? "" : readerMassive.GetString(6);
                        product.product_price = (readerMassive.IsDBNull(7)) ? -1 : readerMassive.GetDouble(7);
                        product.product_order = (readerMassive.IsDBNull(8)) ? "" : readerMassive.GetString(8);
                        product.product_link_1 = (readerMassive.IsDBNull(9)) ? "" : readerMassive.GetString(9);
                        product.product_link_2 = (readerMassive.IsDBNull(10)) ? "" : readerMassive.GetString(10);
                        product.product_comment = (readerMassive.IsDBNull(11)) ? "" : readerMassive.GetString(11);
                        product.created_at = readerMassive.GetString(12);
                        product.product_url_image = (readerMassive.IsDBNull(13)) ? "" : readerMassive.GetString(13);
                        products.Add(product);
                    }
                    commandSQL.Dispose();
                    return products;
                }
            }
        }
        public void UpdateCurrentPrice(ref int product_code, ref double current_price)
        {
            lock (locker)
            {
                using (MySqlCommand command = new MySqlCommand(UpdatePriceById, connection))
                {
                    command.Parameters.AddWithValue("@product_code", product_code);
                    command.Parameters.AddWithValue("@product_price", current_price);
                    command.ExecuteReader();
                    command.Dispose();
                }
            }
        }
        public bool CheckProductExist(ref int product_code)
        {
            lock (locker)
            {
                using (MySqlCommand command = new MySqlCommand(CheckProductCode, connection))
                {
                    command.Parameters.AddWithValue("@product_code", product_code);
                    using (MySqlDataReader dataReader = command.ExecuteReader())
                    {
                        if (dataReader.Read())
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
