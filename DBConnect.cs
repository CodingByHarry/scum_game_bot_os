using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SCUMBot
{
    class DBConnect
    {
        public string connectionString = "SERVER=host;DATABASE=scumbot_development;UID=username;PASSWORD=password;";
        private MySqlConnection connection;

        public DBConnect()
        {
            Initialize();
        }

        private void Initialize()
        {
            connection = new MySqlConnection(connectionString);
        }

        /*
        public string[] get_order()
        {
            string[] data = { “ERROR”, "" };

            if (this.OpenConnection() == true)
            {
                try { 
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT id, userId, shopPackageId FROM orders WHERE delivered = false ORDER BY createdAt LIMIT 1";

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        data = { reader.GetString(0), reader.GetString(1), reader.GetString(2) };
                        //Debug.WriteLine($"ID {reader.GetString(0)}");
                        //Debug.WriteLine($"UserID {reader.GetString(1)}");
                        //Debug.WriteLine($"ShopPackageID {reader.GetString(2)}");
                    }
                }
                finally
                {
                    CloseConnection();
                }
            }

            return data;
        }
        */

        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        //Cannot connect to server.
                        break;

                    case 1045:
                        //Invalid username/password
                        break;
                }
                return false;
            }
        }

        private bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                //MessageBox.Show(ex.Message);
                return false;
            }
        }
    }
}
