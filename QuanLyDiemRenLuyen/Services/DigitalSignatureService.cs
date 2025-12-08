using System;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service xử lý chữ ký điện tử cho điểm rèn luyện
    /// Sử dụng Oracle PKG_SCORE_SIGNATURE với crypto4ora backend
    /// 
    /// UPDATE: Now uses Oracle database packages for digital signatures.
    /// Private keys never leave the database, improving security.
    /// </summary>
    public class DigitalSignatureService
    {
        /// <summary>
        /// Tạo chữ ký điện tử cho score data using Oracle PKG_SCORE_SIGNATURE
        /// </summary>
        /// <param name="studentId">Student ID</param>
        /// <param name="termId">Term ID</param>
        /// <param name="totalScore">Total score</param>
        /// <param name="classification">Classification</param>
        /// <returns>Chữ ký điện tử (CLOB)</returns>
        public static string SignScoreData(int studentId, int termId, int totalScore, string classification)
        {
            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT PKG_SCORE_SIGNATURE.SIGN_SCORE(
                            :student_id, :term_id, :total_score, :classification) FROM DUAL";
                        cmd.Parameters.Add("student_id", OracleDbType.Int32).Value = studentId;
                        cmd.Parameters.Add("term_id", OracleDbType.Int32).Value = termId;
                        cmd.Parameters.Add("total_score", OracleDbType.Int32).Value = totalScore;
                        cmd.Parameters.Add("classification", OracleDbType.Varchar2).Value = classification ?? "";
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi tạo chữ ký điện tử: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Legacy method for compatibility - creates score data string then signs
        /// </summary>
        public static string SignScoreData(string scoreData)
        {
            if (string.IsNullOrWhiteSpace(scoreData))
                throw new ArgumentException("Score data cannot be empty");

            try
            {
                // Parse the legacy format and use the new Oracle-based signing
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT PKG_CRYPTO4ORA.SIGN_WITH_SYSTEM_KEY(:data) FROM DUAL";
                        cmd.Parameters.Add("data", OracleDbType.Varchar2).Value = scoreData;
                        
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi tạo chữ ký điện tử: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Verify chữ ký điện tử của score using Oracle PKG_SCORE_SIGNATURE
        /// </summary>
        public static bool VerifyScoreSignature(int studentId, int termId, int totalScore, string classification, string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
                return false;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT PKG_SCORE_SIGNATURE.VERIFY_SCORE_SIGNATURE(
                            :student_id, :term_id, :total_score, :classification, :signature) FROM DUAL";
                        cmd.Parameters.Add("student_id", OracleDbType.Int32).Value = studentId;
                        cmd.Parameters.Add("term_id", OracleDbType.Int32).Value = termId;
                        cmd.Parameters.Add("total_score", OracleDbType.Int32).Value = totalScore;
                        cmd.Parameters.Add("classification", OracleDbType.Varchar2).Value = classification ?? "";
                        cmd.Parameters.Add("signature", OracleDbType.Clob).Value = signature;
                        
                        var result = cmd.ExecuteScalar();
                        return result != null && Convert.ToInt32(result) == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Verify signature error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Legacy method for compatibility
        /// </summary>
        public static bool VerifyScoreSignature(string scoreData, string signature)
        {
            if (string.IsNullOrWhiteSpace(scoreData) || string.IsNullOrWhiteSpace(signature))
                return false;

            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT PKG_CRYPTO4ORA.VERIFY_WITH_SYSTEM_KEY(:data, :signature) FROM DUAL";
                        cmd.Parameters.Add("data", OracleDbType.Varchar2).Value = scoreData;
                        cmd.Parameters.Add("signature", OracleDbType.Clob).Value = signature;
                        
                        var result = cmd.ExecuteScalar();
                        return result != null && Convert.ToInt32(result) == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Verify signature error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sign and store signature for a score record directly
        /// </summary>
        public static void SignAndStoreScore(int scoreId)
        {
            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "BEGIN PKG_SCORE_SIGNATURE.SIGN_AND_STORE_SCORE(:score_id); END;";
                        cmd.Parameters.Add("score_id", OracleDbType.Int32).Value = scoreId;
                        
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi ký điểm: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Verify stored signature for a score record
        /// </summary>
        public static bool VerifyStoredSignature(int scoreId)
        {
            try
            {
                using (var conn = new OracleConnection(OracleDbHelper.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PKG_SCORE_SIGNATURE.VERIFY_STORED_SIGNATURE(:score_id) FROM DUAL";
                        cmd.Parameters.Add("score_id", OracleDbType.Int32).Value = scoreId;
                        
                        var result = cmd.ExecuteScalar();
                        return result != null && Convert.ToInt32(result) == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Verify stored signature error: " + ex.Message);
                return false;
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
            // Format chuẩn (must match Oracle package format)
            string data = $"SCORE|{studentId}|{termId}|{totalScore}|{classification ?? ""}";
            return data;
        }

        /// <summary>
        /// Tạo hash SHA256 từ score data string
        /// </summary>
        public static string CreateScoreDataHash(string scoreData)
        {
            if (string.IsNullOrWhiteSpace(scoreData))
                return null;

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(scoreData);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
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
