using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace QuanLyDiemRenLuyen.Helpers
{
    /// <summary>
    /// Service quản lý RSA keys trong hệ thống
    /// Xử lý initialization, retrieval, và rotation của encryption keys
    /// </summary>
    public class RsaKeyManager
    {
        private const string SYSTEM_KEY_NAME = "SYSTEM_MAIN_KEY";
        
        /// <summary>
        /// Khởi tạo hệ thống với RSA key pair mới nếu chưa có
        /// Được gọi khi application start lần đầu
        /// </summary>
        public static void InitializeSystemKey()
        {
            try
            {
                // Kiểm tra xem đã có key thật chưa (không phải PLACEHOLDER)
                if (IsSystemKeyInitialized())
                {
                    System.Diagnostics.Debug.WriteLine("✓ System RSA key đã tồn tại và hoạt động.");
                    return;
                }

                // Generate new key pair
                System.Diagnostics.Debug.WriteLine("Đang tạo RSA key pair mới cho hệ thống...");
                var keyPair = RsaHelper.GenerateRsaKeyPair();

                // Kiểm tra xem có key PLACEHOLDER không
                bool hasPlaceholder = CheckPlaceholderKeyExists();

                if (hasPlaceholder)
                {
                    // UPDATE existing PLACEHOLDER key
                    System.Diagnostics.Debug.WriteLine("Đang cập nhật PLACEHOLDER key với key thật...");
                    
                    string updateQuery = @"
                        UPDATE ENCRYPTION_KEYS
                        SET PUBLIC_KEY = :PublicKey,
                            PRIVATE_KEY = :PrivateKey,
                            CREATED_AT = SYSTIMESTAMP,
                            CREATED_BY = :CreatedBy,
                            DESCRIPTION = :Description
                        WHERE KEY_NAME = :KeyName";

                    var updateParams = new[]
                    {
                        OracleDbHelper.CreateParameter("PublicKey", OracleDbType.Clob, keyPair.PublicKey),
                        OracleDbHelper.CreateParameter("PrivateKey", OracleDbType.Clob, keyPair.PrivateKey),
                        OracleDbHelper.CreateParameter("CreatedBy", OracleDbType.Varchar2, "SYSTEM_AUTO"),
                        OracleDbHelper.CreateParameter("Description", OracleDbType.Varchar2, 
                            "Main system RSA key pair (2048-bit) auto-generated on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                        OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME)
                    };

                    OracleDbHelper.ExecuteNonQuery(updateQuery, updateParams);
                    System.Diagnostics.Debug.WriteLine("✓ Đã cập nhật key thành công!");
                }
                else
                {
                    // INSERT new key
                    string insertQuery = @"
                        BEGIN
                            PKG_RSA_CRYPTO.CREATE_KEY(
                                p_key_name => :KeyName,
                                p_public_key => :PublicKey,
                                p_private_key => :PrivateKey,
                                p_created_by => :CreatedBy,
                                p_description => :Description
                            );
                        END;";

                    var insertParams = new[]
                    {
                        OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME),
                        OracleDbHelper.CreateParameter("PublicKey", OracleDbType.Clob, keyPair.PublicKey),
                        OracleDbHelper.CreateParameter("PrivateKey", OracleDbType.Clob, keyPair.PrivateKey),
                        OracleDbHelper.CreateParameter("CreatedBy", OracleDbType.Varchar2, "SYSTEM_AUTO"),
                        OracleDbHelper.CreateParameter("Description", OracleDbType.Varchar2, 
                            "Main system RSA key pair (2048-bit) generated on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    };

                    OracleDbHelper.ExecuteNonQuery(insertQuery, insertParams);
                    System.Diagnostics.Debug.WriteLine("✓ Đã tạo key mới thành công!");
                }

                System.Diagnostics.Debug.WriteLine("✓ System RSA key đã sẵn sàng!");
                System.Diagnostics.Debug.WriteLine("  - Key Name: " + SYSTEM_KEY_NAME);
                System.Diagnostics.Debug.WriteLine("  - Key Size: " + keyPair.KeySize + " bits");
                System.Diagnostics.Debug.WriteLine("  - Fingerprint: " + keyPair.GetPublicKeyFingerprint().Substring(0, 16) + "...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Lỗi khi khởi tạo system key: " + ex.Message);
                // Không throw exception để app vẫn chạy được
                // throw new Exception("Không thể khởi tạo RSA encryption keys: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Kiểm tra xem system key đã được khởi tạo chưa
        /// </summary>
        public static bool IsSystemKeyInitialized()
        {
            try
            {
                string query = @"
                    SELECT COUNT(*) 
                    FROM ENCRYPTION_KEYS 
                    WHERE KEY_NAME = :KeyName 
                    AND IS_ACTIVE = 1
                    AND PUBLIC_KEY NOT LIKE '%PLACEHOLDER%'";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME)
                };

                object result = OracleDbHelper.ExecuteScalar(query, parameters);
                return Convert.ToInt32(result) > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra xem có key PLACEHOLDER tồn tại không
        /// </summary>
        private static bool CheckPlaceholderKeyExists()
        {
            try
            {
                string query = @"
                    SELECT COUNT(*) 
                    FROM ENCRYPTION_KEYS 
                    WHERE KEY_NAME = :KeyName 
                    AND PUBLIC_KEY LIKE '%PLACEHOLDER%'";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME)
                };

                object result = OracleDbHelper.ExecuteScalar(query, parameters);
                return Convert.ToInt32(result) > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy public key của hệ thống (dùng để mã hóa)
        /// </summary>
        public static string GetSystemPublicKey()
        {
            try
            {
                string query = @"
                    SELECT PUBLIC_KEY 
                    FROM ENCRYPTION_KEYS 
                    WHERE KEY_NAME = :KeyName 
                    AND IS_ACTIVE = 1";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME)
                };

                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);
                
                if (dt.Rows.Count == 0)
                {
                    throw new Exception("System RSA key không tồn tại. Vui lòng khởi tạo hệ thống.");
                }

                return dt.Rows[0]["PUBLIC_KEY"].ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy system public key: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Lấy private key của hệ thống (dùng để giải mã)
        /// CHỈ DÙNG KHI THỰC SỰ CẦN THIẾT
        /// </summary>
        public static string GetSystemPrivateKey()
        {
            try
            {
                string query = @"
                    SELECT PRIVATE_KEY 
                    FROM ENCRYPTION_KEYS 
                    WHERE KEY_NAME = :KeyName 
                    AND IS_ACTIVE = 1";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME)
                };

                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);
                
                if (dt.Rows.Count == 0)
                {
                    throw new Exception("System RSA key không tồn tại.");
                }

                // Log access to private key for security audit
                LogPrivateKeyAccess();

                return dt.Rows[0]["PRIVATE_KEY"].ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy system private key: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Log việc truy cập private key (security audit)
        /// </summary>
        private static void LogPrivateKeyAccess()
        {
            try
            {
                string query = @"
                    INSERT INTO AUDIT_TRAIL (WHO, ACTION, EVENT_AT_UTC)
                    VALUES (:Who, :Action, SYS_EXTRACT_UTC(SYSTIMESTAMP))";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("Who", OracleDbType.Varchar2, "SYSTEM_APP"),
                    OracleDbHelper.CreateParameter("Action", OracleDbType.Varchar2, 
                        "PRIVATE_KEY_ACCESS: Application accessed system private key")
                };

                OracleDbHelper.ExecuteNonQuery(query, parameters);
            }
            catch
            {
                // Không throw exception nếu log fail
                System.Diagnostics.Debug.WriteLine("Warning: Could not log private key access");
            }
        }

        /// <summary>
        /// Update last used timestamp cho key
        /// </summary>
        public static void UpdateKeyUsage()
        {
            try
            {
                string query = @"
                    BEGIN
                        PKG_RSA_CRYPTO.UPDATE_KEY_USAGE(:KeyName);
                    END;";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME)
                };

                OracleDbHelper.ExecuteNonQuery(query, parameters);
            }
            catch
            {
                // Silent fail - not critical
            }
        }

        /// <summary>
        /// Rotate system key - tạo key mới và vô hiệu hóa key cũ
        /// Chỉ nên dùng khi cần thiết (ví dụ: key bị compromise)
        /// </summary>
        public static void RotateSystemKey(string reason)
        {
            try
            {
                // 1. Vô hiệu hóa key cũ
                string deactivateQuery = @"
                    BEGIN
                        PKG_RSA_CRYPTO.DEACTIVATE_KEY(:KeyName);
                    END;";

                OracleDbHelper.ExecuteNonQuery(deactivateQuery, new[]
                {
                    OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME)
                });

                // 2. Tạo key mới
                InitializeSystemKey();

                // 3. Log rotation
                string logQuery = @"
                    INSERT INTO AUDIT_TRAIL (WHO, ACTION, EVENT_AT_UTC)
                    VALUES (:Who, :Action, SYS_EXTRACT_UTC(SYSTIMESTAMP))";

                OracleDbHelper.ExecuteNonQuery(logQuery, new[]
                {
                    OracleDbHelper.CreateParameter("Who", OracleDbType.Varchar2, "SYSTEM_APP"),
                    OracleDbHelper.CreateParameter("Action", OracleDbType.Varchar2, 
                        "KEY_ROTATION: " + reason)
                });

                System.Diagnostics.Debug.WriteLine("✓ System key rotation completed successfully!");
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi rotate system key: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Lấy ID của encryption key hiện tại
        /// </summary>
        public static string GetCurrentEncryptionKeyId()
        {
            try
            {
                string query = @"SELECT ID FROM ENCRYPTION_KEYS 
                                WHERE KEY_NAME = :KeyName 
                                AND IS_ACTIVE = 1";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME)
                };

                object result = OracleDbHelper.ExecuteScalar(query, parameters);
                return result != null ? result.ToString() : null;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy encryption key ID: " + ex.Message, ex);
            }
        }


        /// <summary>
        /// Lấy thông tin về system key hiện tại
        /// </summary>
        public static KeyInfo GetSystemKeyInfo()
        {
            try
            {
                string query = @"
                    SELECT 
                        KEY_NAME,
                        KEY_SIZE,
                        ALGORITHM,
                        CREATED_AT,
                        LAST_USED_AT,
                        IS_ACTIVE,
                        DESCRIPTION
                    FROM ENCRYPTION_KEYS
                    WHERE KEY_NAME = :KeyName";

                var parameters = new[]
                {
                    OracleDbHelper.CreateParameter("KeyName", OracleDbType.Varchar2, SYSTEM_KEY_NAME)
                };

                DataTable dt = OracleDbHelper.ExecuteQuery(query, parameters);
                
                if (dt.Rows.Count == 0)
                {
                    return null;
                }

                DataRow row = dt.Rows[0];
                return new KeyInfo
                {
                    KeyName = row["KEY_NAME"].ToString(),
                    KeySize = Convert.ToInt32(row["KEY_SIZE"]),
                    Algorithm = row["ALGORITHM"].ToString(),
                    CreatedAt = Convert.ToDateTime(row["CREATED_AT"]),
                    LastUsedAt = row["LAST_USED_AT"] != DBNull.Value ? 
                        Convert.ToDateTime(row["LAST_USED_AT"]) : (DateTime?)null,
                    IsActive = Convert.ToInt32(row["IS_ACTIVE"]) == 1,
                    Description = row["DESCRIPTION"].ToString()
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy key info: " + ex.Message, ex);
            }
        }
    }

    /// <summary>
    /// Class chứa thông tin về encryption key
    /// </summary>
    public class KeyInfo
    {
        public string KeyName { get; set; }
        public int KeySize { get; set; }
        public string Algorithm { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
    }
}
