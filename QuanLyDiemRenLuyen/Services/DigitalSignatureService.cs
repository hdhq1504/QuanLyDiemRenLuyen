using System;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service xử lý chữ ký điện tử cho điểm rèn luyện
    /// Sử dụng RSA digital signature với SHA-256
    /// </summary>
    public class DigitalSignatureService
    {
        /// <summary>
        /// Tạo chữ ký điện tử cho score data
        /// </summary>
        /// <param name="scoreData">Chuỗi data cần ký (format: STUDENT_ID=xxx|TERM_ID=xxx|...)</param>
        /// <returns>Chữ ký điện tử (Base64)</returns>
        public static string SignScoreData(string scoreData)
        {
            if (string.IsNullOrWhiteSpace(scoreData))
                throw new ArgumentException("Score data cannot be empty");

            try
            {
                // Lấy private key để ký
                string privateKey = RsaKeyManager.GetSystemPrivateKey();
                
                // Tạo digital signature
                string signature = RsaHelper.Sign(scoreData, privateKey);
                
                return signature;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi tạo chữ ký điện tử: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Verify chữ ký điện tử của score
        /// </summary>
        /// <param name="scoreData">Data gốc</param>
        /// <param name="signature">Chữ ký cần verify</param>
        /// <returns>true nếu signature hợp lệ</returns>
        public static bool VerifyScoreSignature(string scoreData, string signature)
        {
            if (string.IsNullOrWhiteSpace(scoreData) || string.IsNullOrWhiteSpace(signature))
                return false;

            try
            {
                // Lấy public key để verify
                string publicKey = RsaKeyManager.GetSystemPublicKey();
                
                // Verify signature
                bool isValid = RsaHelper.VerifySignature(scoreData, signature, publicKey);
                
                return isValid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Verify signature error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Tạo hash SHA-256 của score data
        /// </summary>
        /// <param name="scoreData">Score data string</param>
        /// <returns>Hash HEX string</returns>
        public static string CreateScoreDataHash(string scoreData)
        {
            if (string.IsNullOrWhiteSpace(scoreData))
                return null;

            try
            {
                string hash = RsaHelper.ComputeSha256Hash(scoreData);
                return hash;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi tạo hash: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Tạo score data string từ các thành phần
        /// Format cố định để đảm bảo hash nhất quán
        /// </summary>
        public static string CreateScoreDataString(
            string studentId, 
            string termId, 
            int totalScore, 
            string classification, 
            string status)
        {
            // Format chuẩn (phải giống với Oracle package)
            string data = $"STUDENT_ID={studentId}|TERM_ID={termId}|TOTAL_SCORE={totalScore}|" +
                         $"CLASSIFICATION={classification ?? ""}|STATUS={status ?? ""}";
            
            return data;
        }

        /// <summary>
        /// Kiểm tra score có bị tamper không (bằng cách so sánh hash)
        /// </summary>
        public static bool IsScoreTampered(string originalHash, string currentData)
        {
            if (string.IsNullOrWhiteSpace(originalHash) || string.IsNullOrWhiteSpace(currentData))
                return false;

            try
            {
                string currentHash = CreateScoreDataHash(currentData);
                return originalHash != currentHash;
            }
            catch
            {
                return true; // Nếu lỗi → coi như tampered
            }
        }

        /// <summary>
        /// Format signature info để hiển thị
        /// </summary>
        public static string FormatSignatureInfo(string signature, string signedBy, DateTime? signedAt)
        {
            if (string.IsNullOrWhiteSpace(signature))
                return "Chưa ký";

            string info = $"Đã ký bởi: {signedBy ?? "Unknown"}\n";
            if (signedAt.HasValue)
            {
                info += $"Thời gian: {signedAt.Value:dd/MM/yyyy HH:mm:ss}\n";
            }
            info += $"Signature: {signature.Substring(0, Math.Min(32, signature.Length))}...";
            
            return info;
        }
    }
}
