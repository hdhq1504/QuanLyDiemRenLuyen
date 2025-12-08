using System;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service xử lý mã hóa lai (Hybrid Encryption) kết hợp RSA và AES.
    /// - RSA: Mã hóa khóa AES ngẫu nhiên
    /// - AES: Mã hóa dữ liệu lớn
    /// 
    /// Phù hợp cho: Feedback dài, hồ sơ sinh viên xuất, mô tả hoạt động.
    /// Uses Oracle PKG_HYBRID_CRYPTO backend.
    /// </summary>
    public static class HybridCryptoService
    {
        #region Core Hybrid Encryption

        /// <summary>
        /// Mã hóa dữ liệu lớn sử dụng hybrid encryption (RSA + AES)
        /// </summary>
        /// <param name="data">Dữ liệu cần mã hóa</param>
        /// <returns>Chuỗi mã hóa (RSA_KEY::AES_DATA)</returns>
        public static string EncryptLargeData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_HYBRID_CRYPTO.HYBRID_ENCRYPT(:data) FROM DUAL";
                        cmd.Parameters.Add("data", OracleDbType.Clob).Value = data;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa dữ liệu: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã dữ liệu lớn đã được mã hóa hybrid
        /// </summary>
        /// <param name="encryptedData">Chuỗi mã hóa</param>
        /// <returns>Dữ liệu gốc</returns>
        public static string DecryptLargeData(string encryptedData)
        {
            if (string.IsNullOrEmpty(encryptedData))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_HYBRID_CRYPTO.HYBRID_DECRYPT(:encrypted) FROM DUAL";
                        cmd.Parameters.Add("encrypted", OracleDbType.Clob).Value = encryptedData;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt error: " + ex.Message);
                return "[Decryption Error]";
            }
        }

        /// <summary>
        /// Kiểm tra xem dữ liệu có được mã hóa hybrid không
        /// </summary>
        public static bool IsHybridEncrypted(string data)
        {
            if (string.IsNullOrEmpty(data))
                return false;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_HYBRID_CRYPTO.IS_HYBRID_ENCRYPTED(:data) FROM DUAL";
                        cmd.Parameters.Add("data", OracleDbType.Clob).Value = data;
                        
                        var result = cmd.ExecuteScalar();
                        return result != null && Convert.ToInt32(result) == 1;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Feature 1: Feedback Full Content Encryption

        /// <summary>
        /// Mã hóa nội dung feedback dài (> 200 ký tự nên dùng hybrid)
        /// </summary>
        public static string EncryptFeedbackContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_HYBRID_CRYPTO.ENCRYPT_FEEDBACK_CONTENT(:content) FROM DUAL";
                        cmd.Parameters.Add("content", OracleDbType.Clob).Value = content;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi mã hóa feedback: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã nội dung feedback
        /// </summary>
        public static string DecryptFeedbackContent(string encryptedContent)
        {
            if (string.IsNullOrEmpty(encryptedContent))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_HYBRID_CRYPTO.DECRYPT_FEEDBACK_CONTENT(:encrypted) FROM DUAL";
                        cmd.Parameters.Add("encrypted", OracleDbType.Clob).Value = encryptedContent;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt feedback error: " + ex.Message);
                return "[Decryption Error]";
            }
        }

        #endregion

        #region Feature 2: Student Profile Export Encryption

        /// <summary>
        /// Mã hóa hồ sơ sinh viên đầy đủ (JSON format)
        /// </summary>
        public static string EncryptStudentProfile(string profileJson)
        {
            if (string.IsNullOrEmpty(profileJson))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_HYBRID_CRYPTO.ENCRYPT_STUDENT_PROFILE(:profile) FROM DUAL";
                        cmd.Parameters.Add("profile", OracleDbType.Clob).Value = profileJson;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi mã hóa hồ sơ sinh viên: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã hồ sơ sinh viên
        /// </summary>
        public static string DecryptStudentProfile(string encryptedProfile)
        {
            if (string.IsNullOrEmpty(encryptedProfile))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_HYBRID_CRYPTO.DECRYPT_STUDENT_PROFILE(:encrypted) FROM DUAL";
                        cmd.Parameters.Add("encrypted", OracleDbType.Clob).Value = encryptedProfile;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt profile error: " + ex.Message);
                return "[Decryption Error]";
            }
        }

        /// <summary>
        /// Tạo JSON hồ sơ sinh viên từ các thông tin
        /// </summary>
        public static string CreateStudentProfileJson(
            int studentId,
            string studentCode,
            string fullName,
            string phone,
            string address,
            string idCard,
            string email,
            string className)
        {
            // Simple JSON creation (use Newtonsoft.Json in production)
            return $@"{{
                ""studentId"": {studentId},
                ""studentCode"": ""{EscapeJson(studentCode)}"",
                ""fullName"": ""{EscapeJson(fullName)}"",
                ""phone"": ""{EscapeJson(phone)}"",
                ""address"": ""{EscapeJson(address)}"",
                ""idCard"": ""{EscapeJson(idCard)}"",
                ""email"": ""{EscapeJson(email)}"",
                ""className"": ""{EscapeJson(className)}"",
                ""exportDate"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}""
            }}";
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        #endregion

        #region Feature 3: Activity Description Encryption

        /// <summary>
        /// Mã hóa mô tả hoạt động dài
        /// </summary>
        public static string EncryptActivityDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_HYBRID_CRYPTO.ENCRYPT_ACTIVITY_DESCRIPTION(:desc) FROM DUAL";
                        cmd.Parameters.Add("desc", OracleDbType.Clob).Value = description;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi mã hóa mô tả hoạt động: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã mô tả hoạt động
        /// </summary>
        public static string DecryptActivityDescription(string encryptedDescription)
        {
            if (string.IsNullOrEmpty(encryptedDescription))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_HYBRID_CRYPTO.DECRYPT_ACTIVITY_DESCRIPTION(:encrypted) FROM DUAL";
                        cmd.Parameters.Add("encrypted", OracleDbType.Clob).Value = encryptedDescription;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt description error: " + ex.Message);
                return "[Decryption Error]";
            }
        }

        /// <summary>
        /// Lưu hoạt động với mô tả được mã hóa
        /// </summary>
        public static void StoreActivityWithEncryptedDescription(int activityId, string description)
        {
            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            UPDATE ACTIVITIES 
                            SET DESCRIPTION_ENCRYPTED = PKG_HYBRID_CRYPTO.ENCRYPT_ACTIVITY_DESCRIPTION(:desc),
                                ENCRYPTION_TYPE = 'HYBRID'
                            WHERE ACTIVITY_ID = :activity_id";
                        
                        cmd.Parameters.Add("desc", OracleDbType.Clob).Value = description;
                        cmd.Parameters.Add("activity_id", OracleDbType.Int32).Value = activityId;
                        
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi lưu hoạt động: " + ex.Message, ex);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Ước tính kích thước sau khi mã hóa
        /// </summary>
        public static int EstimateEncryptedSize(int plaintextLength)
        {
            // RSA key ~344 chars + AES overhead ~2.4x + separator
            return 346 + (int)Math.Ceiling(plaintextLength * 2.4);
        }

        /// <summary>
        /// Kiểm tra xem có nên dùng hybrid encryption không
        /// </summary>
        public static bool ShouldUseHybridEncryption(string data)
        {
            if (string.IsNullOrEmpty(data))
                return false;

            // RSA 2048-bit can only encrypt ~214 bytes directly
            // Use hybrid for anything > 200 chars
            return data.Length > 200;
        }

        /// <summary>
        /// Tự động chọn phương thức mã hóa phù hợp
        /// </summary>
        public static string SmartEncrypt(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            if (ShouldUseHybridEncryption(data))
            {
                return EncryptLargeData(data);
            }
            else
            {
                // Use RSA for short data
                return SensitiveDataService.EncryptStudentPhone(data);
            }
        }

        #endregion
    }
}
