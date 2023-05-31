using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SSD_Assignment___Banking_Application
{
    public class EncryptData
    {
        private static readonly int saltSize = 16;
        private static readonly int keySize = 32;
        private static readonly int iterations = 1000;

        public static string EncryptString(string password, string plainText)
        {
            byte[] salt = new byte[saltSize];
            new RNGCryptoServiceProvider().GetBytes(salt);
            var key = new Rfc2898DeriveBytes(password, salt, iterations).GetBytes(keySize);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = key;
                aes.GenerateIV();

                using (var memoryStream = new MemoryStream())
                {
                    // Prepend salt to the cipher text
                    memoryStream.Write(salt, 0, saltSize);
                    // Prepend IV to the cipher text
                    memoryStream.Write(aes.IV, 0, aes.IV.Length);

                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(plainText);
                    }

                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        public static string DecryptString(string password, string cipherText)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            var salt = new byte[saltSize];
            var iv = new byte[16];
            var actualCipherText = new byte[fullCipher.Length - saltSize - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, salt, 0, saltSize);
            Buffer.BlockCopy(fullCipher, saltSize, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, saltSize + iv.Length, actualCipherText, 0, actualCipherText.Length);

            var key = new Rfc2898DeriveBytes(password, salt, iterations).GetBytes(keySize);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = key;
                aes.IV = iv;

                using (var memoryStream = new MemoryStream(actualCipherText))
                using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var streamReader = new StreamReader(cryptoStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
