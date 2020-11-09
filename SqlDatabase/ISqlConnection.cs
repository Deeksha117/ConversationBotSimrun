using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace TeamsConversationBot.SqlDatabase
{
    public interface ISqlConnection
    {
        public int ExecuteQuery(string query);

        public SqlDataReader GetQueryResult(string query);

        public T GetQueryValue<T>(string query);

    }
}
