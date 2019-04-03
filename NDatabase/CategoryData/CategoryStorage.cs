using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Exceling.NDatabase.CategoryData
{
    public class CategoryStorage : Storage
    {
        public string table_nameC = "excel_categories";
        public string tableC = "CREATE TABLE IF NOT EXISTS excel_categories" +
        "(" +
            "category_id int AUTO_INCREMENT," +
            "category_name varchar(256)," +
            "category_position tinyint," +
            "created_at varchar(16)," +
            "PRIMARY KEY(category_id)" +
        ");";
        private string insertCategory = "INSERT INTO excel_categories( category_name, category_position, created_at) " +
            "VALUES( @category_name, @category_position, @created_at);";

        private string selectByName = "SELECT category_name FROM excel_categories WHERE category_name=@category_name;";

        public CategoryStorage(ref MySqlConnection connection, ref object locker)
        {
            this.table_name = table_nameC;
            this.table = tableC;
            this.connection = connection;
            this.locker = locker;
        }
        public CategoryCell AddCategory(ref CategoryCell category)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(insertCategory, connection))
                {
                    commandSQL.Parameters.AddWithValue("@category_name", category.category_name);
                    commandSQL.Parameters.AddWithValue("@category_position", category.category_position);
                    commandSQL.Parameters.AddWithValue("@created_at", category.created_at);
                    commandSQL.ExecuteNonQuery();
                    category.category_id = (int)commandSQL.LastInsertedId;
                    commandSQL.Dispose();
                }
            }
            return category;
        }
        public CategoryCell SelectCategoryByName(ref string category_name)
        {
            CategoryCell category;
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_categories WHERE category_name=@category_name;", connection))
            {
                commandSQL.Parameters.AddWithValue("@category_name", category_name);
                category = SelectCategory(commandSQL);
            }
            return category;
        }
        public CategoryCell SelectCategoryById(int? category_id)
        {
            CategoryCell category;
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_categories WHERE category_id=@category_id;", connection))
            {
                commandSQL.Parameters.AddWithValue("@category_id", category_id);
                category = SelectCategory(commandSQL);
            }
            return category;
        }
        public List<CategoryCell> SelectCategoriesByPosition(int category_position)
        {
            List<CategoryCell> categories;
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM excel_categories WHERE category_position=@category_position;", connection))
            {
                commandSQL.Parameters.AddWithValue("@category_position", category_position);
                categories = SelectCategories(commandSQL);
            }
            return categories;
        }
        private CategoryCell SelectCategory(MySqlCommand commandSQL)
        {
            lock (locker)
            {
                using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                {
                    while (readerMassive.Read())
                    {
                        CategoryCell category = new CategoryCell();
                        category.category_id = readerMassive.GetInt32("category_id");
                        category.category_name = readerMassive.GetString("category_name");
                        category.category_position = readerMassive.GetInt16("category_position");
                        category.created_at = readerMassive.GetString("created_at");
                        commandSQL.Dispose();
                        return category;
                    }
                    return null;
                }
            }
        }
        private List<CategoryCell> SelectCategories(MySqlCommand commandSQL)
        {
            List<CategoryCell> categories = new List<CategoryCell>();
            lock (locker)
            {
                using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                {
                    while (readerMassive.Read())
                    {
                        CategoryCell category = new CategoryCell();
                        category.category_id = readerMassive.GetInt32("category_id");
                        category.category_name = readerMassive.GetString("category_name");
                        category.category_position = readerMassive.GetInt16("category_position");
                        category.created_at = readerMassive.GetString("created_at");
                        categories.Add(category);
                    }
                    commandSQL.Dispose();
                }
            }
            return categories;
        }
    }
}
