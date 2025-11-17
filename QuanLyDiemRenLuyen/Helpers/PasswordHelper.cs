using System;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyDiemRenLuyen.Helpers
{
    /// <summary>
    /// Helper class để xử lý mã hóa mật khẩu
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Tạo salt ngẫu nhiên
        /// </summary>
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Hash mật khẩu với salt
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            
            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException(nameof(salt));

            using (var sha256 = SHA256.Create())
            {
                // Kết hợp password và salt
                string combined = password + salt;
                byte[] bytes = Encoding.UTF8.GetBytes(combined);
                
                // Hash nhiều lần để tăng độ bảo mật
                byte[] hash = sha256.ComputeHash(bytes);
                for (int i = 0; i < 1000; i++)
                {
                    hash = sha256.ComputeHash(hash);
                }
                
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Xác thực mật khẩu
        /// </summary>
        public static bool VerifyPassword(string password, string salt, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(hash))
                return false;

            string computedHash = HashPassword(password, salt);
            return computedHash == hash;
        }
    }
}

