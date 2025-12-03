using System;
using QuanLyDiemRenLuyen.Helpers;

namespace QuanLyDiemRenLuyen.Services
{
    /// <summary>
    /// Service xử lý mã hóa/giải mã dữ liệu nhạy cảm của sinh viên
    /// Sử dụng RSA encryption với system key
    /// </summary>
    public class SensitiveDataService
    {
        /// <summary>
        /// Mã hóa số điện thoại
        /// </summary>
        public static string EncryptStudentPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            try
            {
                // Lấy public key từ hệ thống
                string publicKey = RsaKeyManager.GetSystemPublicKey();
                
                // Mã hóa
                string encrypted = RsaHelper.Encrypt(phone.Trim(), publicKey);
                
                // Update key usage
                RsaKeyManager.UpdateKeyUsage();
                
                return encrypted;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa số điện thoại: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã số điện thoại
        /// </summary>
        public static string DecryptStudentPhone(string encryptedPhone)
        {
            if (string.IsNullOrWhiteSpace(encryptedPhone))
                return null;

            try
            {
                // Lấy private key từ hệ thống
                string privateKey = RsaKeyManager.GetSystemPrivateKey();
                
                // Giải mã
                string decrypted = RsaHelper.Decrypt(encryptedPhone, privateKey);
                
                return decrypted;
            }
            catch (Exception ex)
            {
                // Log error nhưng không expose chi tiết
                System.Diagnostics.Debug.WriteLine("Decrypt phone error: " + ex.Message);
                return "[Encrypted - Cannot Decrypt]";
            }
        }

        /// <summary>
        /// Mã hóa địa chỉ
        /// </summary>
        public static string EncryptStudentAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            try
            {
                string publicKey = RsaKeyManager.GetSystemPublicKey();
                
                // Nếu address quá dài, cần split và encrypt từng phần
                // RSA 2048-bit chỉ encrypt được ~214 bytes
                if (address.Length > 200)
                {
                    return EncryptLongText(address, publicKey);
                }
                
                string encrypted = RsaHelper.Encrypt(address.Trim(), publicKey);
                RsaKeyManager.UpdateKeyUsage();
                
                return encrypted;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa địa chỉ: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã địa chỉ
        /// </summary>
        public static string DecryptStudentAddress(string encryptedAddress)
        {
            if (string.IsNullOrWhiteSpace(encryptedAddress))
                return null;

            try
            {
                string privateKey = RsaKeyManager.GetSystemPrivateKey();
                
                // Check if this is multi-part encrypted data
                if (encryptedAddress.StartsWith("[MULTI]"))
                {
                    return DecryptLongText(encryptedAddress, privateKey);
                }
                
                string decrypted = RsaHelper.Decrypt(encryptedAddress, privateKey);
                return decrypted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt address error: " + ex.Message);
                return "[Encrypted - Cannot Decrypt]";
            }
        }

        /// <summary>
        /// Mã hóa số CMND/CCCD
        /// </summary>
        public static string EncryptIdCard(string idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard))
                return null;

            try
            {
                string publicKey = RsaKeyManager.GetSystemPublicKey();
                string encrypted = RsaHelper.Encrypt(idCard.Trim(), publicKey);
                
                RsaKeyManager.UpdateKeyUsage();
                
                return encrypted;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi mã hóa số CMND/CCCD: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Giải mã số CMND/CCCD
        /// </summary>
        public static string DecryptIdCard(string encryptedIdCard)
        {
            if (string.IsNullOrWhiteSpace(encryptedIdCard))
                return null;

            try
            {
                string privateKey = RsaKeyManager.GetSystemPrivateKey();
                string decrypted = RsaHelper.Decrypt(encryptedIdCard, privateKey);
                
                return decrypted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Decrypt ID card error: " + ex.Message);
                return "[Encrypted - Cannot Decrypt]";
            }
        }

        /// <summary>
        /// Mã hóa text dài bằng cách split thành nhiều blocks
        /// Format: [MULTI]block1|block2|block3
        /// </summary>
        private static string EncryptLongText(string text, string publicKey)
        {
            const int chunkSize = 180; // An toàn với 2048-bit RSA
            var chunks = new System.Collections.Generic.List<string>();
            
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                int length = Math.Min(chunkSize, text.Length - i);
                string chunk = text.Substring(i, length);
                string encryptedChunk = RsaHelper.Encrypt(chunk, publicKey);
                chunks.Add(encryptedChunk);
            }
            
            return "[MULTI]" + string.Join("|", chunks);
        }

        /// <summary>
        /// Giải mã text dài đã được split thành nhiều blocks
        /// </summary>
        private static string DecryptLongText(string encryptedText, string privateKey)
        {
            // Remove [MULTI] prefix
            string data = encryptedText.Substring(7);
            string[] chunks = data.Split('|');
            
            var decryptedChunks = new System.Collections.Generic.List<string>();
            foreach (string chunk in chunks)
            {
                string decrypted = RsaHelper.Decrypt(chunk, privateKey);
                decryptedChunks.Add(decrypted);
            }
            
            return string.Join("", decryptedChunks);
        }

        /// <summary>
        /// Mask sensitive data để hiển thị (ví dụ: 012****789)
        /// </summary>
        public static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "";

            if (phone.Length <= 6)
                return new string('*', phone.Length);

            // Show first 3 and last 3 digits
            string first = phone.Substring(0, 3);
            string last = phone.Substring(phone.Length - 3);
            string middle = new string('*', phone.Length - 6);

            return first + middle + last;
        }

        /// <summary>
        /// Mask ID card number
        /// </summary>
        public static string MaskIdCard(string idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard))
                return "";

            if (idCard.Length <= 4)
                return new string('*', idCard.Length);

            // Show last 4 digits only
            string masked = new string('*', idCard.Length - 4);
            string last = idCard.Substring(idCard.Length - 4);

            return masked + last;
        }
    }
}
