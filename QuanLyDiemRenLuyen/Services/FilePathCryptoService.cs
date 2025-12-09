using System;
using Oracle.ManagedDataAccess.Client;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service xử lý mã hóa đường dẫn file cho PROOFS và FEEDBACK_ATTACHMENTS.
    /// Sử dụng Oracle PKG_FILE_PATH_CRYPTO backend.
    /// </summary>
    public class FilePathCryptoService
    {
        #region Proofs

        /// <summary>
        /// Mã hóa và lưu đường dẫn minh chứng
        /// </summary>
        public void EncryptProofPath(string proofId, string storedPath)
        {
            if (string.IsNullOrEmpty(proofId) || string.IsNullOrEmpty(storedPath))
                return;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "BEGIN PKG_FILE_PATH_CRYPTO.ENCRYPT_PROOF_PATH(:p_proof_id, :p_stored_path); END;";
                    cmd.Parameters.Add("p_proof_id", OracleDbType.Varchar2).Value = proofId;
                    cmd.Parameters.Add("p_stored_path", OracleDbType.Varchar2).Value = storedPath;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Lấy đường dẫn minh chứng đã giải mã
        /// </summary>
        public string GetProofPath(string proofId)
        {
            if (string.IsNullOrEmpty(proofId))
                return null;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT PKG_FILE_PATH_CRYPTO.GET_PROOF_PATH(:p_proof_id) FROM DUAL";
                    cmd.Parameters.Add("p_proof_id", OracleDbType.Varchar2).Value = proofId;
                    
                    var result = cmd.ExecuteScalar();
                    return result == DBNull.Value ? null : result?.ToString();
                }
            }
        }

        /// <summary>
        /// Mã hóa tất cả đường dẫn minh chứng chưa được mã hóa
        /// </summary>
        public void EncryptAllProofPaths()
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "BEGIN PKG_FILE_PATH_CRYPTO.ENCRYPT_ALL_PROOF_PATHS; END;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Feedback Attachments

        /// <summary>
        /// Mã hóa và lưu đường dẫn tệp đính kèm phản hồi
        /// </summary>
        public void EncryptAttachmentPath(string attachmentId, string storedPath)
        {
            if (string.IsNullOrEmpty(attachmentId) || string.IsNullOrEmpty(storedPath))
                return;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "BEGIN PKG_FILE_PATH_CRYPTO.ENCRYPT_ATTACHMENT_PATH(:p_attachment_id, :p_stored_path); END;";
                    cmd.Parameters.Add("p_attachment_id", OracleDbType.Varchar2).Value = attachmentId;
                    cmd.Parameters.Add("p_stored_path", OracleDbType.Varchar2).Value = storedPath;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Lấy đường dẫn tệp đính kèm đã giải mã
        /// </summary>
        public string GetAttachmentPath(string attachmentId)
        {
            if (string.IsNullOrEmpty(attachmentId))
                return null;

            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT PKG_FILE_PATH_CRYPTO.GET_ATTACHMENT_PATH(:p_attachment_id) FROM DUAL";
                    cmd.Parameters.Add("p_attachment_id", OracleDbType.Varchar2).Value = attachmentId;
                    
                    var result = cmd.ExecuteScalar();
                    return result == DBNull.Value ? null : result?.ToString();
                }
            }
        }

        /// <summary>
        /// Mã hóa tất cả đường dẫn tệp đính kèm chưa được mã hóa
        /// </summary>
        public void EncryptAllAttachmentPaths()
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "BEGIN PKG_FILE_PATH_CRYPTO.ENCRYPT_ALL_ATTACHMENT_PATHS; END;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Mã hóa hàng loạt tất cả đường dẫn file
        /// </summary>
        public void EncryptAllFilePaths()
        {
            EncryptAllProofPaths();
            EncryptAllAttachmentPaths();
        }

        /// <summary>
        /// Lấy đường dẫn file an toàn (trả về null nếu lỗi)
        /// </summary>
        public string GetPathSafe(string id, string type)
        {
            try
            {
                switch (type?.ToUpper())
                {
                    case "PROOF":
                        return GetProofPath(id);
                    case "ATTACHMENT":
                    case "FEEDBACK_ATTACHMENT":
                        return GetAttachmentPath(id);
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
