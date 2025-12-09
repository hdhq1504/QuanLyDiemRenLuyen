using System;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service quản lý session token với mã hóa AES.
    /// Sử dụng Oracle PKG_SESSION_TOKEN backend.
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
        /// <param name="userId">MAND của user</param>
        /// <param name="expiryHours">Số giờ token hết hạn</param>
        /// <returns>Session token ID</returns>
        public string CreateSessionToken(string userId, int? expiryHours = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT PKG_SESSION_TOKEN.CREATE_SESSION(:p_user_id, :p_expiry_hours) FROM DUAL";
                    cmd.Parameters.Add("p_user_id", OracleDbType.Varchar2).Value = userId;
                    cmd.Parameters.Add("p_expiry_hours", OracleDbType.Int32).Value = expiryHours ?? DefaultExpiryHours;
                    
                    var result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }

        /// <summary>
        /// Xác thực session token
        /// </summary>
        /// <param name="tokenId">Session token ID</param>
        /// <returns>True nếu token hợp lệ</returns>
        public bool ValidateSessionToken(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
                return false;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT PKG_SESSION_TOKEN.VALIDATE_SESSION(:p_token_id) FROM DUAL";
                    cmd.Parameters.Add("p_token_id", OracleDbType.Varchar2).Value = tokenId;
                    
                    var result = cmd.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) == 1;
                }
            }
        }

        /// <summary>
        /// Lấy user ID từ session token
        /// </summary>
        public string GetUserIdFromToken(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
                return null;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT PKG_SESSION_TOKEN.GET_USER_FROM_SESSION(:p_token_id) FROM DUAL";
                    cmd.Parameters.Add("p_token_id", OracleDbType.Varchar2).Value = tokenId;
                    
                    var result = cmd.ExecuteScalar();
                    return result == DBNull.Value ? null : result?.ToString();
                }
            }
        }

        /// <summary>
        /// Vô hiệu hóa session token
        /// </summary>
        public void InvalidateToken(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
                return;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "BEGIN PKG_SESSION_TOKEN.INVALIDATE_SESSION(:p_token_id); END;";
                    cmd.Parameters.Add("p_token_id", OracleDbType.Varchar2).Value = tokenId;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Làm mới session token (gia hạn thời gian hết hạn)
        /// </summary>
        public bool RefreshToken(string tokenId, int? newExpiryHours = null)
        {
            if (string.IsNullOrEmpty(tokenId))
                return false;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT PKG_SESSION_TOKEN.REFRESH_SESSION(:p_token_id, :p_expiry_hours) FROM DUAL";
                    cmd.Parameters.Add("p_token_id", OracleDbType.Varchar2).Value = tokenId;
                    cmd.Parameters.Add("p_expiry_hours", OracleDbType.Int32).Value = newExpiryHours ?? DefaultExpiryHours;
                    
                    var result = cmd.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) == 1;
                }
            }
        }

        /// <summary>
        /// Vô hiệu hóa tất cả session của user
        /// </summary>
        public void InvalidateAllUserSessions(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "BEGIN PKG_SESSION_TOKEN.INVALIDATE_USER_SESSIONS(:p_user_id); END;";
                    cmd.Parameters.Add("p_user_id", OracleDbType.Varchar2).Value = userId;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Dọn dẹp session đã hết hạn
        /// </summary>
        public void CleanupExpiredSessions()
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "BEGIN PKG_SESSION_TOKEN.CLEANUP_EXPIRED; END;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #region Convenience Methods

        /// <summary>
        /// Tạo session và trả về thông tin đầy đủ
        /// </summary>
        public SessionInfo CreateSession(string userId, int? expiryHours = null)
        {
            var tokenId = CreateSessionToken(userId, expiryHours);
            if (string.IsNullOrEmpty(tokenId))
                return null;

            return new SessionInfo
            {
                TokenId = tokenId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expiryHours ?? DefaultExpiryHours)
            };
        }

        /// <summary>
        /// Kiểm tra và lấy user từ token
        /// </summary>
        public string ValidateAndGetUser(string tokenId)
        {
            if (!ValidateSessionToken(tokenId))
                return null;

            return GetUserIdFromToken(tokenId);
        }

        #endregion
    }

    /// <summary>
    /// Thông tin session
    /// </summary>
    public class SessionInfo
    {
        public string TokenId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}
