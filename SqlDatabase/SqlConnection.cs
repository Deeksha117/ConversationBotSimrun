using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace TeamsConversationBot.SqlDatabase
{
    public class SqlConnectionClass : ISqlConnection
    {
        public SqlConnection sqlConnectionObj { get; }
        public readonly static string MemberTable = "Members";
        public readonly static string RecordTable = "Record";
        public readonly static string TeamsTable = "Teams";
        public readonly static string FailureString = "Could not save record. Try query with right values. Or Contact admin.";

        public SqlConnectionClass(IConfiguration config)
        {
            var connString = config["SQLConnectionString"];
            sqlConnectionObj = new SqlConnection(connString);
            sqlConnectionObj.Open();
        }

        public void Setup()
        {
            var query = "CREATE TABLE Record (" +
                "MemberName varchar(255),"+
                "DateOfRecord date,"+
                "ModifiedTime time,"+
                "Distance float,"+
                "AppUsedToMeasure varchar(255) DEFAULT 'Manual',"+
                "Id varchar(255) NOT NULL," +
                ");";
            ExecuteQuery(query);

            query = "CREATE TABLE Members (" +
                "MemberName varchar(255),"+
                "TeamName varchar(255),"+
                "Id varchar(255) NOT NULL,"+
                "PRIMARY KEY(Id)"+
                ");";
            ExecuteQuery(query);

            query = "CREATE TABLE Teams (TeamName varchar(255),MemberCount int)";
            ExecuteQuery(query);
        }

        public int ExecuteQuery(string query)
        {
            try
            {
                var command = new SqlCommand(query, this.sqlConnectionObj);
                var result = command.ExecuteNonQuery();
                command.Dispose();
                return result;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception:", ex.Message);
                return 0;
            }
        }

        public SqlDataReader GetQueryResult(string query)
        {
            var returnObj = new List<List<string>>();
            try
            {
                var command = new SqlCommand(query, this.sqlConnectionObj);
                var dataReader = command.ExecuteReader();
                command.Dispose();
                return dataReader;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:", ex.Message);
                return null;
            }
        }

        public T GetQueryValue<T>(string query)
        {
            try
            {
                var command = new SqlCommand(query, this.sqlConnectionObj);
                T val = (T)command.ExecuteScalar();
                command.Dispose();
                return val;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:", ex.Message);
                return default(T);
            }
        }
    }
}
