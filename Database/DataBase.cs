
using MySql.Data.MySqlClient;

namespace Webtracking.Database
{
    public class DataBase
    {
        public static MySqlConnection Connection
        {
            get
            {
                MySqlConnection _connection = new MySqlConnection(Middleware.GlobalSetting.GetSettings().ConnectionString);
                _connection.Open();
                return _connection;
            }
        }
    }
}
