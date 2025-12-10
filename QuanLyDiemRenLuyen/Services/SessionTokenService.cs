using System;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service quản lý session token với mã hóa AES
    /// </summary>
    public class SessionTokenService
    {
        /// <summary>
        /// Thời gian sống mặc định của token (giờ)
        /// </summary>
        public int DefaultExpiryHours { get; set; } = 24;

        /// <summary>
        /// Tạo session token mới cho user
        /// </summary>
        public string CreateSessionToken(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "BEGIN :result := PKG_SESSION_TOKEN.CREATE_SESSION_TOKEN(:p_user_id); END;";
                    
                    var resultParam = cmd.Parameters.Add("result", OracleDbType.Varchar2, 100);
                    resultParam.Direction = System.Data.ParameterDirection.Output;
                    
                    cmd.Parameters.Add("p_user_id", OracleDbType.Varchar2).Value = userId;
                    
                    cmd.ExecuteNonQuery();
                    
                    var result = resultParam.Value;
                    return result == DBNull.Value ? null : result?.ToString();
                }
            }
        }

        /// <summary>
        /// Xác thực session token
        /// </summary>
        public bool ValidateSessionToken(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return false;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT PKG_SESSION_TOKEN.VALIDATE_SESSION_TOKEN(:p_user_id, :p_token) FROM DUAL";
                    cmd.Parameters.Add("p_user_id", OracleDbType.Varchar2).Value = userId;
                    cmd.Parameters.Add("p_token", OracleDbType.Varchar2).Value = token;
                    
                    var result = cmd.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) == 1;
                }
            }
        }

        /// <summary>
        /// Xóa/Vô hiệu hóa session token của user
        /// </summary>
        public void ClearSessionToken(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "BEGIN PKG_SESSION_TOKEN.CLEAR_SESSION_TOKEN(:p_user_id); END;";
                    cmd.Parameters.Add("p_user_id", OracleDbType.Varchar2).Value = userId;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Lấy user ID từ token
        /// </summary>
        public string GetUserByToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT PKG_SESSION_TOKEN.GET_USER_BY_TOKEN(:p_token) FROM DUAL";
                    cmd.Parameters.Add("p_token", OracleDbType.Varchar2).Value = token;
                    
                    var result = cmd.ExecuteScalar();
                    return result == DBNull.Value ? null : result?.ToString();
                }
            }
        }

        #region Convenience Methods

        /// <summary>
        /// Tạo session và trả về thông tin đầy đủ
        /// </summary>
        public SessionInfo CreateSession(string userId)
        {
            var token = CreateSessionToken(userId);
            if (string.IsNullOrEmpty(token))
                return null;

            return new SessionInfo
            {
                Token = token,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(DefaultExpiryHours)
            };
        }

        /// <summary>
        /// Kiểm tra và lấy user từ token
        /// </summary>
        public string ValidateAndGetUser(string token)
        {
            var userId = GetUserByToken(token);
            if (string.IsNullOrEmpty(userId))
                return null;
            
            // Validate the token belongs to this user
            if (ValidateSessionToken(userId, token))
                return userId;
            
            return null;
        }

        #endregion
    }

    /// <summary>
    /// Thông tin session
    /// </summary>
    public class SessionInfo
    {
        public string Token { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}
