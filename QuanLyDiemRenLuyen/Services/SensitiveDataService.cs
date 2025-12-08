using System;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service xử lý mã hóa/giải mã dữ liệu nhạy cảm của sinh viên
    /// Sử dụng Oracle PKG_STUDENT_ENCRYPTION (crypto4ora backend)
    /// 
    /// UPDATE: Now uses Oracle database packages for encryption instead of C# RSA.
    /// This ensures encryption keys never leave the database, improving security.
    /// </summary>
    public class SensitiveDataService
    {
        #region Phone Encryption

        /// <summary>
        /// Mã hóa số điện thoại using Oracle PKG_STUDENT_ENCRYPTION
        /// </summary>
        public static string EncryptStudentPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            try
            {
                // Call Oracle package for encryption
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_STUDENT_ENCRYPTION.ENCRYPT_PHONE(:phone) FROM DUAL";
                        cmd.Parameters.Add("phone", OracleDbType.Varchar2).Value = phone.Trim();
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa số điện thoại: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã số điện thoại using Oracle PKG_STUDENT_ENCRYPTION
        /// </summary>
        public static string DecryptStudentPhone(string encryptedPhone)
        {
            if (string.IsNullOrWhiteSpace(encryptedPhone))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_STUDENT_ENCRYPTION.DECRYPT_PHONE(:encrypted) FROM DUAL";
                        cmd.Parameters.Add("encrypted", OracleDbType.Clob).Value = encryptedPhone;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt phone error: " + ex.Message);
                return "[Encrypted - Cannot Decrypt]";
            }
        }

        #endregion

        #region Address Encryption

        /// <summary>
        /// Mã hóa địa chỉ using Oracle PKG_STUDENT_ENCRYPTION
        /// </summary>
        public static string EncryptStudentAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_STUDENT_ENCRYPTION.ENCRYPT_ADDRESS(:address) FROM DUAL";
                        cmd.Parameters.Add("address", OracleDbType.Varchar2).Value = address.Trim();
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa địa chỉ: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã địa chỉ using Oracle PKG_STUDENT_ENCRYPTION
        /// </summary>
        public static string DecryptStudentAddress(string encryptedAddress)
        {
            if (string.IsNullOrWhiteSpace(encryptedAddress))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_STUDENT_ENCRYPTION.DECRYPT_ADDRESS(:encrypted) FROM DUAL";
                        cmd.Parameters.Add("encrypted", OracleDbType.Clob).Value = encryptedAddress;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt address error: " + ex.Message);
                return "[Encrypted - Cannot Decrypt]";
            }
        }

        #endregion

        #region ID Card Encryption

        /// <summary>
        /// Mã hóa số CMND/CCCD using Oracle PKG_STUDENT_ENCRYPTION
        /// </summary>
        public static string EncryptIdCard(string idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_STUDENT_ENCRYPTION.ENCRYPT_ID_CARD(:idcard) FROM DUAL";
                        cmd.Parameters.Add("idcard", OracleDbType.Varchar2).Value = idCard.Trim();
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa số CMND/CCCD: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã số CMND/CCCD using Oracle PKG_STUDENT_ENCRYPTION
        /// </summary>
        public static string DecryptIdCard(string encryptedIdCard)
        {
            if (string.IsNullOrWhiteSpace(encryptedIdCard))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_STUDENT_ENCRYPTION.DECRYPT_ID_CARD(:encrypted) FROM DUAL";
                        cmd.Parameters.Add("encrypted", OracleDbType.Clob).Value = encryptedIdCard;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt ID card error: " + ex.Message);
                return "[Encrypted - Cannot Decrypt]";
            }
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Encrypt and store all sensitive data for a student
        /// </summary>
        public static void EncryptAndStoreStudentData(int studentId, string phone, string address, string idCard)
        {
            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "BEGIN PKG_STUDENT_ENCRYPTION.ENCRYPT_STUDENT_DATA(:sid, :phone, :address, :idcard); END;";
                        cmd.Parameters.Add("sid", OracleDbType.Int32).Value = studentId;
                        cmd.Parameters.Add("phone", OracleDbType.Varchar2).Value = (object)phone ?? DBNull.Value;
                        cmd.Parameters.Add("address", OracleDbType.Varchar2).Value = (object)address ?? DBNull.Value;
                        cmd.Parameters.Add("idcard", OracleDbType.Varchar2).Value = (object)idCard ?? DBNull.Value;
                        
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa dữ liệu sinh viên: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Get all decrypted sensitive data for a student
        /// </summary>
        public static (string Phone, string Address, string IdCard) GetDecryptedStudentData(int studentId)
        {
            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            DECLARE
                                v_phone VARCHAR2(50);
                                v_address VARCHAR2(500);
                                v_idcard VARCHAR2(50);
                            BEGIN
                                PKG_STUDENT_ENCRYPTION.GET_STUDENT_SENSITIVE_DATA(:sid, v_phone, v_address, v_idcard);
                                :phone := v_phone;
                                :address := v_address;
                                :idcard := v_idcard;
                            END;";
                        
                        cmd.Parameters.Add("sid", OracleDbType.Int32).Value = studentId;
                        cmd.Parameters.Add("phone", OracleDbType.Varchar2, 50).Direction = System.Data.ParameterDirection.Output;
                        cmd.Parameters.Add("address", OracleDbType.Varchar2, 500).Direction = System.Data.ParameterDirection.Output;
                        cmd.Parameters.Add("idcard", OracleDbType.Varchar2, 50).Direction = System.Data.ParameterDirection.Output;
                        
                        cmd.ExecuteNonQuery();
                        
                        return (
                            cmd.Parameters["phone"].Value?.ToString(),
                            cmd.Parameters["address"].Value?.ToString(),
                            cmd.Parameters["idcard"].Value?.ToString()
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Get student data error: " + ex.Message);
                return (null, null, null);
            }
        }

        #endregion

        #region Masking Utilities

        /// <summary>
        /// Mask sensitive data để hiển thị (ví dụ: 012****789)
        /// </summary>
        public static string MaskPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 6)
                return phone;

            // Giữ 3 số đầu và 3 số cuối, còn lại thay bằng *
            int visibleStart = 3;
            int visibleEnd = 3;
            int maskLength = phone.Length - visibleStart - visibleEnd;

            if (maskLength <= 0)
                return phone;

            return phone.Substring(0, visibleStart) 
                   + new string('*', maskLength) 
                   + phone.Substring(phone.Length - visibleEnd);
        }

        /// <summary>
        /// Mask ID card number
        /// </summary>
        public static string MaskIdCard(string idCard)
        {
            if (string.IsNullOrEmpty(idCard) || idCard.Length < 6)
                return idCard;

            // Giữ 4 số cuối
            int visibleEnd = 4;
            int maskLength = idCard.Length - visibleEnd;

            return new string('*', maskLength) + idCard.Substring(idCard.Length - visibleEnd);
        }

        /// <summary>
        /// Mask address - show only district/city
        /// </summary>
        public static string MaskAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return address;

            // Show only last part (typically district/city)
            int lastComma = address.LastIndexOf(',');
            if (lastComma > 0 && lastComma < address.Length - 1)
            {
                return "***" + address.Substring(lastComma);
            }

            // If no comma, mask first half
            int halfLength = address.Length / 2;
            return new string('*', halfLength) + address.Substring(halfLength);
        }

        #endregion
    }
}
