using System;
using System.Collections.Generic;
using System.Linq;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service xử lý mã hóa feedback nhạy cảm
    /// Sử dụng RSA encryption với access control
    /// </summary>
    public class EncryptedFeedbackService
    {
        /// <summary>
        /// Mã hóa nội dung feedback
        /// </summary>
        public static string EncryptFeedbackContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                string publicKey = RsaKeyManager.GetSystemPublicKey();
                
                // Xử lý content dài (nếu > 200 chars thì dùng multi-block)
                if (content.Length > 200)
                {
                    return EncryptLongText(content, publicKey);
                }
                
                string encrypted = RsaHelper.Encrypt(content, publicKey);
                return encrypted;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa feedback: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã nội dung feedback
        /// </summary>
        public static string DecryptFeedbackContent(string encryptedContent)
        {
            if (string.IsNullOrWhiteSpace(encryptedContent))
                return null;

            try
            {
                string privateKey = RsaKeyManager.GetSystemPrivateKey();
                
                // Kiểm tra multi-block
                if (encryptedContent.StartsWith("[MULTI]"))
                {
                    return DecryptLongText(encryptedContent, privateKey);
                }
                
                string decrypted = RsaHelper.Decrypt(encryptedContent, privateKey);
                return decrypted;
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
        /// Mã hóa văn bản dài (chia thành nhiều blocks)
        /// </summary>
        private static string EncryptLongText(string text, string publicKey)
        {
            const int chunkSize = 200;
            var chunks = new List<string>();
            
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                string chunk = text.Substring(i, Math.Min(chunkSize, text.Length - i));
                string encryptedChunk = RsaHelper.Encrypt(chunk, publicKey);
                chunks.Add(encryptedChunk);
            }
            
            // Format: [MULTI]chunk1|chunk2|chunk3...
            return "[MULTI]" + string.Join("|", chunks);
        }

        /// <summary>
        /// Giải mã văn bản dài (từ nhiều blocks)
        /// </summary>
        private static string DecryptLongText(string encryptedText, string privateKey)
        {
            // Remove [MULTI] prefix
            string data = encryptedText.Substring(7);
            
            // Split blocks
            string[] chunks = data.Split('|');
            var decryptedChunks = new List<string>();
            
            foreach (var chunk in chunks)
            {
                string decrypted = RsaHelper.Decrypt(chunk, privateKey);
                decryptedChunks.Add(decrypted);
            }
            
            return string.Join("", decryptedChunks);
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
                // Simple JSON array parsing (trong production nên dùng JSON.NET)
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
