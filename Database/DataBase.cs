
using MySql.Data.MySqlClient;

namespace Webtracking.Database
{
    public class DataBase
    {
        private static MySqlConnection _connection = null;
        private static MySqlConnection _subconnection = null;

        public static MySqlConnection Connection
        {
            get
            {
                if ((_connection != null) && (_connection.State == System.Data.ConnectionState.Open))
                    return _connection;
                else
                {
                    _connection = new MySqlConnection(Middleware.GlobalSetting.GetSettings().ConnectionString);
                    _connection.Open();
                    return _connection;
                }
            }
        }
        public static MySqlConnection subConnection
        {
            get
            {
                if ((_subconnection != null) && (_subconnection.State == System.Data.ConnectionState.Open))
                    return _subconnection;
                else
                {
                    _subconnection = new MySqlConnection(Middleware.GlobalSetting.GetSettings().ConnectionString);
                    _subconnection.Open();
                    return _subconnection;
                }
            }
        }
    }
}
