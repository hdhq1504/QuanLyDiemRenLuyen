using System;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace QuanLyDiemRenLuyen.Helpers
{
    /// <summary>
    /// Helper class để kết nối và thực thi queries với Oracle Database
    /// </summary>
    public class OracleDbHelper
    {
        static OracleDbHelper()
        {
            Environment.SetEnvironmentVariable("NLS_LANG", "AMERICAN_AMERICA.WE8MSWIN1252");
        }

        private static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["OracleConnection"]?.ConnectionString
                    ?? throw new InvalidOperationException("Oracle connection string not found in Web.config");
            }
        }

        /// <summary>
        /// Lấy connection string từ cấu hình
        /// </summary>
        public static string GetConnectionString()
        {
            return ConnectionString;
        }

        /// <summary>
        /// Tạo và mở connection mới
        /// </summary>
        public static OracleConnection GetConnection()
        {
            var connection = new OracleConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Thực thi query SELECT và trả về DataTable
        /// </summary>
        public static DataTable ExecuteQuery(string query, params OracleParameter[] parameters)
        {
            using (var connection = GetConnection())
            {
                using (var command = new OracleCommand(query, connection))
                {
                    command.BindByName = true;
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (var adapter = new OracleDataAdapter(command))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
        }

        /// <summary>
        /// Thực thi query INSERT, UPDATE, DELETE và trả về số dòng bị ảnh hưởng
        /// </summary>
        public static int ExecuteNonQuery(string query, params OracleParameter[] parameters)
        {
            using (var connection = GetConnection())
            {
                using (var command = new OracleCommand(query, connection))
                {
                    command.BindByName = true;
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Thực thi query và trả về giá trị đơn (scalar)
        /// </summary>
        public static object ExecuteScalar(string query, params OracleParameter[] parameters)
        {
            using (var connection = GetConnection())
            {
                using (var command = new OracleCommand(query, connection))
                {
                    command.BindByName = true;
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return command.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Tạo parameter cho Oracle command
        /// </summary>
        public static OracleParameter CreateParameter(string name, OracleDbType type, object value)
        {
            return new OracleParameter(name, type) { Value = value ?? DBNull.Value };
        }

        /// <summary>
        /// Tạo parameter với direction
        /// </summary>
        public static OracleParameter CreateParameter(string name, OracleDbType type, ParameterDirection direction, object value = null)
        {
            return new OracleParameter(name, type)
            {
                Direction = direction,
                Value = value ?? DBNull.Value
            };
        }
    }
}

