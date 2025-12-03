using System;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyDiemRenLuyen.Helpers
{
    /// <summary>
    /// Helper class cho RSA asymmetric encryption/decryption và digital signatures
    /// Sử dụng RSA 2048-bit với OAEP padding và SHA-256 hashing
    /// </summary>
    public static class RsaHelper
    {
        // Key size chuẩn: 2048 bits
        private const int KeySize = 2048;

        /// <summary>
        /// Tạo cặp khóa RSA mới (Public Key và Private Key)
        /// </summary>
        /// <returns>Object chứa public key và private key ở định dạng XML</returns>
        public static RsaKeyPair GenerateRsaKeyPair()
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider(KeySize))
                {
                    // Đảm bảo key được tạo mới
                    rsa.PersistKeyInCsp = false;

                    var keyPair = new RsaKeyPair
                    {
                        PublicKey = rsa.ToXmlString(false),  // Chỉ public key
                        PrivateKey = rsa.ToXmlString(true),  // Cả public và private key
                        KeySize = KeySize,
                        CreatedAt = DateTime.UtcNow
                    };

                    return keyPair;
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Lỗi khi tạo RSA key pair: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Mã hóa dữ liệu bằng RSA public key
        /// </summary>
        /// <param name="plainText">Văn bản cần mã hóa</param>
        /// <param name="publicKeyXml">Public key ở định dạng XML</param>
        /// <returns>Chuỗi Base64 của dữ liệu đã mã hóa</returns>
        public static string Encrypt(string plainText, string publicKeyXml)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            if (string.IsNullOrEmpty(publicKeyXml))
                throw new ArgumentNullException(nameof(publicKeyXml));

            try
            {
                using (var rsa = new RSACryptoServiceProvider(KeySize))
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(publicKeyXml);

                    // Convert text to bytes
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                    // Mã hóa với OAEP padding (an toàn hơn PKCS#1 v1.5)
                    byte[] encryptedBytes = rsa.Encrypt(plainBytes, true);

                    // Trả về Base64 string
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
            catch (CryptographicException ex)
            {
                // RSA có giới hạn kích thước dữ liệu
                // Với 2048-bit key: max ~214 bytes cho OAEP
                throw new CryptographicException(
                    "Lỗi mã hóa RSA. Dữ liệu có thể quá lớn (max ~214 bytes với 2048-bit key): " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Lỗi không xác định khi mã hóa: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã dữ liệu bằng RSA private key
        /// </summary>
        /// <param name="cipherText">Chuỗi Base64 của dữ liệu đã mã hóa</param>
        /// <param name="privateKeyXml">Private key ở định dạng XML</param>
        /// <returns>Văn bản gốc đã giải mã</returns>
        public static string Decrypt(string cipherText, string privateKeyXml)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            if (string.IsNullOrEmpty(privateKeyXml))
                throw new ArgumentNullException(nameof(privateKeyXml));

            try
            {
                using (var rsa = new RSACryptoServiceProvider(KeySize))
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(privateKeyXml);

                    // Convert Base64 to bytes
                    byte[] encryptedBytes = Convert.FromBase64String(cipherText);

                    // Giải mã
                    byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, true);

                    // Convert bytes to text
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("Dữ liệu mã hóa không hợp lệ (không phải Base64): " + ex.Message, ex);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Lỗi giải mã RSA. Key có thể không đúng: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Lỗi không xác định khi giải mã: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Tạo chữ ký số cho dữ liệu bằng private key
        /// Sử dụng SHA-256 hash algorithm
        /// </summary>
        /// <param name="data">Dữ liệu cần ký</param>
        /// <param name="privateKeyXml">Private key để ký</param>
        /// <returns>Chữ ký ở dạng Base64 string</returns>
        public static string Sign(string data, string privateKeyXml)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrEmpty(privateKeyXml))
                throw new ArgumentNullException(nameof(privateKeyXml));

            try
            {
                using (var rsa = new RSACryptoServiceProvider(KeySize))
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(privateKeyXml);

                    // Convert data to bytes
                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                    // Tạo chữ ký với SHA-256
                    byte[] signatureBytes = rsa.SignData(dataBytes, CryptoConfig.MapNameToOID("SHA256"));

                    return Convert.ToBase64String(signatureBytes);
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Lỗi khi tạo chữ ký số: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Xác thực chữ ký số bằng public key
        /// </summary>
        /// <param name="data">Dữ liệu gốc</param>
        /// <param name="signature">Chữ ký cần xác thực (Base64 string)</param>
        /// <param name="publicKeyXml">Public key để xác thực</param>
        /// <returns>true nếu chữ ký hợp lệ, false nếu không</returns>
        public static bool VerifySignature(string data, string signature, string publicKeyXml)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrEmpty(signature))
                return false;

            if (string.IsNullOrEmpty(publicKeyXml))
                throw new ArgumentNullException(nameof(publicKeyXml));

            try
            {
                using (var rsa = new RSACryptoServiceProvider(KeySize))
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(publicKeyXml);

                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    byte[] signatureBytes = Convert.FromBase64String(signature);

                    return rsa.VerifyData(dataBytes, CryptoConfig.MapNameToOID("SHA256"), signatureBytes);
                }
            }
            catch (FormatException)
            {
                // Signature không phải Base64 hợp lệ
                return false;
            }
            catch (CryptographicException)
            {
                // Lỗi xác thực
                return false;
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Lỗi không xác định khi xác thực chữ ký: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Tính SHA-256 hash của dữ liệu
        /// Dùng để tạo fingerprint hoặc verify data integrity
        /// </summary>
        /// <param name="data">Dữ liệu cần hash</param>
        /// <returns>Hash ở dạng hex string</returns>
        public static string ComputeSha256Hash(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            try
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    byte[] hashBytes = sha256.ComputeHash(dataBytes);

                    // Convert to hex string
                    var sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Lỗi khi tính SHA-256 hash: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Kiểm tra xem một cặp public/private key có khớp nhau không
        /// </summary>
        public static bool ValidateKeyPair(string publicKeyXml, string privateKeyXml)
        {
            try
            {
                // Test bằng cách encrypt/decrypt một message
                string testMessage = "RSA_KEY_VALIDATION_TEST_" + Guid.NewGuid();
                string encrypted = Encrypt(testMessage, publicKeyXml);
                string decrypted = Decrypt(encrypted, privateKeyXml);

                return testMessage == decrypted;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Class đại diện cho một cặp RSA key pair
    /// </summary>
    public class RsaKeyPair
    {
        /// <summary>
        /// Public Key ở định dạng XML
        /// Dùng để mã hóa và xác thực chữ ký
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Private Key ở định dạng XML
        /// Dùng để giải mã và tạo chữ ký
        /// PHẢI BẢO MẬT TUYỆT ĐỐI
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Kích thước key (bits)
        /// </summary>
        public int KeySize { get; set; }

        /// <summary>
        /// Thời điểm tạo key
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Tạo fingerprint (SHA-256 hash) của public key
        /// Dùng để identify key
        /// </summary>
        public string GetPublicKeyFingerprint()
        {
            return RsaHelper.ComputeSha256Hash(PublicKey);
        }
    }
}
