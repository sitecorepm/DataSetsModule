using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Sitecore.SharedSource.Dataset.DataAccess
{
    public class SqlServerDatabase
    {
        private string _cxnstr = string.Empty;
        public delegate void CommandParameterSetup(SqlCommand cmd);

        public SqlServerDatabase(string vcConnectionStringName)
        {
            _cxnstr = ConfigurationManager.ConnectionStrings[vcConnectionStringName].ConnectionString;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_cxnstr);
        }

        public DataTable SelectDataTable(string sql)
        {
            DataTable dt = new DataTable();

            using (SqlConnection cn = GetConnection())
            {
                SqlDataAdapter da = new SqlDataAdapter(sql, cn);
                da.Fill(dt);
            }

            return dt;
        }

        public IDataReader ExecuteStoredProc(string procName, CommandParameterSetup setup)
        {
			var cn = GetConnection();
			cn.Open();
			var cmd  = new SqlCommand(procName, cn){CommandType = CommandType.StoredProcedure};
            setup(cmd);
			return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public IDataReader ExecuteSql(string sql)
        {
            var cn = GetConnection();
            cn.Open();
            var cmd = new SqlCommand(sql, cn);
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public int ExecuteStoredProcNonQuery(string procName, CommandParameterSetup setup)
        {
            var recordsAffected = 0;
            using (var cn = GetConnection())
            {
                cn.Open();
                var cmd = new SqlCommand(procName, cn) { CommandType = CommandType.StoredProcedure };
                setup(cmd);
                recordsAffected = cmd.ExecuteNonQuery();
            }
            return recordsAffected;
        }

        public T ExecuteStoredProcScalar<T>(string procName, CommandParameterSetup setup)
        {
            T result = default(T);
            using (var cn = GetConnection())
            {
                cn.Open();
                var cmd = new SqlCommand(procName, cn) { CommandType = CommandType.StoredProcedure };
                setup(cmd);
                result = (T)cmd.ExecuteScalar();
            }
            return result;
        }
    }
}
