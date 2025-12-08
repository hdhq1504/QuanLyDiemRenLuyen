using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyDiemRenLuyen.Helpers
{
    /// <summary>
    /// AES-256-CBC encryption helper for symmetric encryption operations.
    /// This class provides methods for encrypting and decrypting data using AES-256.
    /// 
    /// Key format: 32 bytes (256 bits)
    /// IV format: 16 bytes (128 bits) - automatically generated and prepended to ciphertext
    /// Output format: IV (16 bytes) || Ciphertext
    /// </summary>
    public static class AesHelper
    {
        private const int KEY_SIZE_BYTES = 32;  // 256 bits
        private const int IV_SIZE_BYTES = 16;   // 128 bits
        private const int KEY_SIZE_BITS = 256;
        private const int BLOCK_SIZE_BITS = 128;

        #region Key Generation

        /// <summary>
        /// Generates a cryptographically secure random AES-256 key.
        /// </summary>
        /// <returns>32-byte array containing the AES key</returns>
        public static byte[] GenerateKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = KEY_SIZE_BITS;
                aes.GenerateKey();
                return aes.Key;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random initialization vector.
        /// </summary>
        /// <returns>16-byte array containing the IV</returns>
        public static byte[] GenerateIV()
        {
            using (var aes = Aes.Create())
            {
                aes.GenerateIV();
                return aes.IV;
            }
        }

        /// <summary>
        /// Generates a key as a Base64 string for easy storage.
        /// </summary>
        public static string GenerateKeyBase64()
        {
            return Convert.ToBase64String(GenerateKey());
        }

        #endregion

        #region Encryption

        /// <summary>
        /// Encrypts a plaintext string using AES-256-CBC.
        /// The IV is automatically generated and prepended to the ciphertext.
        /// </summary>
        /// <param name="plaintext">The text to encrypt</param>
        /// <param name="key">32-byte AES key</param>
        /// <returns>Base64 encoded string containing IV + ciphertext</returns>
        public static string Encrypt(string plaintext, byte[] key)
        {
            if (string.IsNullOrEmpty(plaintext))
                return null;

            ValidateKey(key);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KEY_SIZE_BITS;
                aes.BlockSize = BLOCK_SIZE_BITS;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    // Write IV first
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
                    {
                        swEncrypt.Write(plaintext);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// Encrypts a plaintext string using a Base64 encoded key.
        /// </summary>
        public static string Encrypt(string plaintext, string keyBase64)
        {
            return Encrypt(plaintext, Convert.FromBase64String(keyBase64));
        }

        /// <summary>
        /// Encrypts raw byte data using AES-256-CBC.
        /// </summary>
        /// <param name="data">The data to encrypt</param>
        /// <param name="key">32-byte AES key</param>
        /// <returns>Byte array containing IV + ciphertext</returns>
        public static byte[] EncryptBytes(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
                return null;

            ValidateKey(key);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KEY_SIZE_BITS;
                aes.BlockSize = BLOCK_SIZE_BITS;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    // Write IV first
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                    }

                    return msEncrypt.ToArray();
                }
            }
        }

        #endregion

        #region Decryption

        /// <summary>
        /// Decrypts a Base64 encoded ciphertext that contains IV + ciphertext.
        /// </summary>
        /// <param name="ciphertext">Base64 encoded IV + ciphertext</param>
        /// <param name="key">32-byte AES key</param>
        /// <returns>Decrypted plaintext string</returns>
        public static string Decrypt(string ciphertext, byte[] key)
        {
            if (string.IsNullOrEmpty(ciphertext))
                return null;

            ValidateKey(key);

            byte[] fullCipher = Convert.FromBase64String(ciphertext);
            
            if (fullCipher.Length < IV_SIZE_BYTES)
                throw new ArgumentException("Ciphertext is too short to contain IV");

            // Extract IV (first 16 bytes)
            byte[] iv = new byte[IV_SIZE_BYTES];
            Array.Copy(fullCipher, 0, iv, 0, IV_SIZE_BYTES);

            // Extract actual ciphertext
            byte[] actualCiphertext = new byte[fullCipher.Length - IV_SIZE_BYTES];
            Array.Copy(fullCipher, IV_SIZE_BYTES, actualCiphertext, 0, actualCiphertext.Length);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KEY_SIZE_BITS;
                aes.BlockSize = BLOCK_SIZE_BITS;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(actualCiphertext))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Decrypts using a Base64 encoded key.
        /// </summary>
        public static string Decrypt(string ciphertext, string keyBase64)
        {
            return Decrypt(ciphertext, Convert.FromBase64String(keyBase64));
        }

        /// <summary>
        /// Decrypts raw byte data.
        /// </summary>
        /// <param name="cipherData">Byte array containing IV + ciphertext</param>
        /// <param name="key">32-byte AES key</param>
        /// <returns>Decrypted byte array</returns>
        public static byte[] DecryptBytes(byte[] cipherData, byte[] key)
        {
            if (cipherData == null || cipherData.Length <= IV_SIZE_BYTES)
                return null;

            ValidateKey(key);

            // Extract IV
            byte[] iv = new byte[IV_SIZE_BYTES];
            Array.Copy(cipherData, 0, iv, 0, IV_SIZE_BYTES);

            // Extract ciphertext
            byte[] actualCiphertext = new byte[cipherData.Length - IV_SIZE_BYTES];
            Array.Copy(cipherData, IV_SIZE_BYTES, actualCiphertext, 0, actualCiphertext.Length);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KEY_SIZE_BITS;
                aes.BlockSize = BLOCK_SIZE_BITS;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream())
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(actualCiphertext, 0, actualCiphertext.Length);
                    }
                    return msDecrypt.ToArray();
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Converts a hex string to byte array (for Oracle RAW compatibility).
        /// </summary>
        public static byte[] HexStringToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return null;

            int length = hex.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Converts byte array to hex string (for Oracle RAW compatibility).
        /// </summary>
        public static string BytesToHexString(byte[] bytes)
        {
            if (bytes == null)
                return null;

            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:X2}", b);
            }
            return hex.ToString();
        }

        /// <summary>
        /// Validates that the key is the correct size for AES-256.
        /// </summary>
        private static void ValidateKey(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), "AES key cannot be null");
            
            if (key.Length != KEY_SIZE_BYTES)
                throw new ArgumentException($"AES-256 key must be exactly {KEY_SIZE_BYTES} bytes, got {key.Length} bytes", nameof(key));
        }

        #endregion

        #region Oracle Integration

        /// <summary>
        /// Encrypts data in a format compatible with Oracle PKG_AES_CRYPTO.
        /// Returns hex string (Oracle RAW format).
        /// </summary>
        public static string EncryptForOracle(string plaintext, byte[] key)
        {
            if (string.IsNullOrEmpty(plaintext))
                return null;

            ValidateKey(key);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KEY_SIZE_BITS;
                aes.BlockSize = BLOCK_SIZE_BITS;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.GenerateIV();

                byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    // Write IV first (same as Oracle format)
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plaintextBytes, 0, plaintextBytes.Length);
                    }

                    return BytesToHexString(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypts data from Oracle PKG_AES_CRYPTO format (hex string).
        /// </summary>
        public static string DecryptFromOracle(string hexCiphertext, byte[] key)
        {
            if (string.IsNullOrEmpty(hexCiphertext))
                return null;

            byte[] cipherData = HexStringToBytes(hexCiphertext);
            return Decrypt(Convert.ToBase64String(cipherData), key);
        }

        #endregion
    }
}
