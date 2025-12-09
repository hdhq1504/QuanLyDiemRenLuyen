using System;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service quản lý VPD Context và OLS Session Labels
    /// Sử dụng để thiết lập security context khi người dùng đăng nhập
    /// </summary>
    public class SecurityContextService
    {
        /// <summary>
        /// Thiết lập VPD Context cho phiên làm việc
        /// Gọi PKG_VPD_CONTEXT.SET_USER_CONTEXT trong Oracle
        /// </summary>
        /// <param name="userId">Mã người dùng (MAND)</param>
        /// <param name="roleName">Vai trò (STUDENT, LECTURER, ADMIN)</param>
        /// <param name="clientId">Session ID từ ứng dụng</param>
        /// <returns>True nếu thành công</returns>
        public static bool SetVpdContext(string userId, string roleName, string clientId)
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OracleConnection"].ConnectionString;
                using (var connection = new OracleConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"BEGIN 
                            PKG_VPD_CONTEXT.SET_USER_CONTEXT(
                                p_user_id => :userId, 
                                p_role => :roleName, 
                                p_client_id => :clientId
                            ); 
                        END;";
                        cmd.Parameters.Add(new OracleParameter("userId", userId ?? (object)DBNull.Value));
                        cmd.Parameters.Add(new OracleParameter("roleName", roleName ?? (object)DBNull.Value));
                        cmd.Parameters.Add(new OracleParameter("clientId", clientId ?? (object)DBNull.Value));
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetVpdContext Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa VPD Context khi người dùng đăng xuất
        /// </summary>
        /// <returns>True nếu thành công</returns>
        public static bool ClearVpdContext()
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OracleConnection"].ConnectionString;
                using (var connection = new OracleConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "BEGIN PKG_VPD_CONTEXT.CLEAR_USER_CONTEXT; END;";
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearVpdContext Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Thiết lập OLS Session Label cho phiên làm việc
        /// Gọi SA_SESSION.SET_LABEL trong Oracle Label Security
        /// </summary>
        /// <param name="roleName">Vai trò người dùng</param>
        /// <returns>True nếu thành công</returns>
        public static bool SetOlsSessionLabel(string roleName)
        {
            try
            {
                // Xác định label theo role
                string label = GetOlsLabelForRole(roleName);
                
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OracleConnection"].ConnectionString;
                using (var connection = new OracleConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"BEGIN 
                            SA_SESSION.SET_LABEL('OLS_DRL_POLICY', :label); 
                        END;";
                        cmd.Parameters.Add(new OracleParameter("label", label));
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                // OLS có thể chưa được cài đặt - không fail ứng dụng
                System.Diagnostics.Debug.WriteLine($"SetOlsSessionLabel Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xác định OLS label dựa trên role
        /// </summary>
        private static string GetOlsLabelForRole(string roleName)
        {
            switch (roleName?.ToUpper())
            {
                case "ADMIN":
                    // Admin có quyền cao nhất: CONFIDENTIAL, tất cả compartments, University level
                    return "CONF:FB,EV,AU:UNI";
                case "LECTURER":
                    // Lecturer: INTERNAL, Feedback + Evidence compartments, Department level
                    return "INT:FB,EV:DEPT";
                case "STUDENT":
                default:
                    // Student: PUBLIC, Class level
                    return "PUB::CLS";
            }
        }

        /// <summary>
        /// Thiết lập Audit Context cho các thao tác cần ghi justification
        /// </summary>
        /// <param name="userId">Mã người dùng</param>
        /// <param name="justification">Lý do thao tác (tùy chọn)</param>
        /// <param name="clientIp">IP của client</param>
        /// <returns>True nếu thành công</returns>
        public static bool SetAuditContext(string userId, string justification = null, string clientIp = null)
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OracleConnection"].ConnectionString;
                using (var connection = new OracleConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"BEGIN 
                            PKG_AUDIT_CONTEXT.SET_CONTEXT(
                                p_user_id => :userId, 
                                p_justification => :justification, 
                                p_client_ip => :clientIp
                            ); 
                        END;";
                        cmd.Parameters.Add(new OracleParameter("userId", userId ?? (object)DBNull.Value));
                        cmd.Parameters.Add(new OracleParameter("justification", justification ?? (object)DBNull.Value));
                        cmd.Parameters.Add(new OracleParameter("clientIp", clientIp ?? (object)DBNull.Value));
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetAuditContext Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa Audit Context
        /// </summary>
        public static bool ClearAuditContext()
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OracleConnection"].ConnectionString;
                using (var connection = new OracleConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "BEGIN PKG_AUDIT_CONTEXT.CLEAR_CONTEXT; END;";
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearAuditContext Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Thiết lập tất cả security contexts khi đăng nhập
        /// </summary>
        public static void SetAllSecurityContexts(string userId, string roleName, string sessionId, string clientIp)
        {
            // Set VPD Context - quan trọng nhất cho row-level security
            SetVpdContext(userId, roleName, sessionId);
            
            // Set OLS Session Label - optional, phụ thuộc vào việc OLS đã được cấu hình
            SetOlsSessionLabel(roleName);
            
            // Set Audit Context - cho trigger audit
            SetAuditContext(userId, "User login", clientIp);
        }

        /// <summary>
        /// Xóa tất cả security contexts khi đăng xuất
        /// </summary>
        public static void ClearAllSecurityContexts()
        {
            ClearVpdContext();
            ClearAuditContext();
            // OLS context tự động reset khi session kết thúc
        }
    }
}
