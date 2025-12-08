using System;
using System.Collections.Generic;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service xử lý mã hóa feedback nhạy cảm
    /// Sử dụng Oracle PKG_FEEDBACK_ENCRYPTION với crypto4ora backend
    /// 
    /// UPDATE: Now uses Oracle database packages for encryption.
    /// Private keys never leave the database, improving security.
    /// </summary>
    public class EncryptedFeedbackService
    {
        /// <summary>
        /// Mã hóa nội dung feedback using Oracle PKG_FEEDBACK_ENCRYPTION
        /// </summary>
        public static string EncryptFeedbackContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_FEEDBACK_ENCRYPTION.ENCRYPT_CONTENT(:content) FROM DUAL";
                        cmd.Parameters.Add("content", OracleDbType.Varchar2).Value = content;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa feedback: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã nội dung feedback using Oracle PKG_FEEDBACK_ENCRYPTION
        /// </summary>
        public static string DecryptFeedbackContent(string encryptedContent)
        {
            if (string.IsNullOrWhiteSpace(encryptedContent))
                return null;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_FEEDBACK_ENCRYPTION.DECRYPT_CONTENT(:encrypted) FROM DUAL";
                        cmd.Parameters.Add("encrypted", OracleDbType.Clob).Value = encryptedContent;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt feedback error: " + ex.Message);
                return "[Encrypted - Cannot Decrypt]";
            }
        }

        /// <summary>
        /// Mã hóa response của feedback
        /// </summary>
        public static string EncryptFeedbackResponse(string response)
        {
            return EncryptFeedbackContent(response);
        }

        /// <summary>
        /// Giải mã response của feedback
        /// </summary>
        public static string DecryptFeedbackResponse(string encryptedResponse)
        {
            return DecryptFeedbackContent(encryptedResponse);
        }

        /// <summary>
        /// Store encrypted feedback directly using Oracle procedure
        /// </summary>
        public static int StoreEncryptedFeedback(int studentId, int termId, string content)
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
                                v_feedback_id NUMBER;
                            BEGIN
                                PKG_FEEDBACK_ENCRYPTION.STORE_ENCRYPTED_FEEDBACK(
                                    :student_id, :term_id, :content, v_feedback_id);
                                :feedback_id := v_feedback_id;
                            END;";
                        
                        cmd.Parameters.Add("student_id", OracleDbType.Int32).Value = studentId;
                        cmd.Parameters.Add("term_id", OracleDbType.Int32).Value = termId;
                        cmd.Parameters.Add("content", OracleDbType.Varchar2).Value = content;
                        cmd.Parameters.Add("feedback_id", OracleDbType.Int32).Direction = System.Data.ParameterDirection.Output;
                        
                        cmd.ExecuteNonQuery();
                        
                        return Convert.ToInt32(cmd.Parameters["feedback_id"].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lưu feedback: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Get decrypted feedback content using Oracle function
        /// </summary>
        public static string GetFeedbackContent(int feedbackId)
        {
            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_FEEDBACK_ENCRYPTION.GET_FEEDBACK_CONTENT(:feedback_id) FROM DUAL";
                        cmd.Parameters.Add("feedback_id", OracleDbType.Int32).Value = feedbackId;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Get feedback error: " + ex.Message);
                return "[Error retrieving feedback]";
            }
        }

        /// <summary>
        /// Parse JSON array allowed readers
        /// Format: ["USER1", "USER2", "USER3"]
        /// </summary>
        public static List<string> ParseAllowedReaders(string allowedReadersJson)
        {
            if (string.IsNullOrWhiteSpace(allowedReadersJson) || allowedReadersJson == "[]")
                return new List<string>();

            try
            {
                // Simple JSON array parsing
                string cleaned = allowedReadersJson.Trim('[', ']');
                var readers = cleaned.Split(',')
                    .Select(r => r.Trim().Trim('"'))
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .ToList();
                
                return readers;
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Convert list to JSON array
        /// </summary>
        public static string ConvertToAllowedReadersJson(List<string> readers)
        {
            if (readers == null || readers.Count == 0)
                return "[]";

            var quotedReaders = readers.Select(r => $"\"{r}\"");
            return "[" + string.Join(",", quotedReaders) + "]";
        }

        /// <summary>
        /// Kiểm tra user có quyền đọc không
        /// </summary>
        public static bool CanUserReadFeedback(string userId, List<string> allowedReaders, string feedbackOwnerId)
        {
            // Owner luôn được phép đọc
            if (userId == feedbackOwnerId)
                return true;

            // Kiểm tra trong allowed list
            if (allowedReaders != null && allowedReaders.Contains(userId))
                return true;

            return false;
        }

        /// <summary>
        /// Tạo preview cho encrypted content (hiển thị 1 phần)
        /// </summary>
        public static string CreateEncryptedPreview(string encryptedContent)
        {
            if (string.IsNullOrWhiteSpace(encryptedContent))
                return "[No Content]";

            int previewLength = Math.Min(40, encryptedContent.Length);
            return "🔒 " + encryptedContent.Substring(0, previewLength) + "...";
        }
    }
}
