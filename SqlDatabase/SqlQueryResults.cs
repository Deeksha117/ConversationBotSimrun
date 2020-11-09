using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;

namespace TeamsConversationBot.SqlDatabase
{
    public class SqlQueryResults
    {
        private ISqlConnection sqlConnectionObj;
        public SqlQueryResults(ISqlConnection sqlConnection)
        {
            sqlConnectionObj = sqlConnection;
        }

        public string GetTeamStatsForTodayResult(string teamname)
        {
            string reply = String.Empty;
            var query = $"SELECT {SqlConnectionClass.MemberTable}.MemberName, Distance FROM {SqlConnectionClass.RecordTable} " +
                    $"INNER JOIN {SqlConnectionClass.MemberTable} ON {SqlConnectionClass.RecordTable}.Id = {SqlConnectionClass.MemberTable}.Id " +
                    $"WHERE TeamName='{teamname}' AND DateOfRecord='{DateTime.UtcNow.Date}'" +
                    $" ORDER BY {SqlConnectionClass.MemberTable}.MemberName ASC";

            var result = sqlConnectionObj.GetQueryResult(query);
            return reply;
        }

        private string GetTableText(System.Data.SqlClient.SqlDataReader reader)
        {
            var reply = "<table>";
            while (reader.Read())
            {
                reply += "<tr>";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.GetValue(i);
                    if (val.GetType() == typeof(string))
                    {
                        reply += $"<td>{XmlConvert.DecodeName(val.ToString())}</td>";
                    }
                    else
                    {
                        reply += $"<td>{reader.GetValue(i)}</td>";
                    }
                }
                reply += "</tr>";
            }

            reply += "</table>";
            return reply;
        }
    }
}
