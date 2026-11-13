using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Leaderboard
{
    // Persists the refresh token between launches so the player signs in once.
    // The file is AES-encrypted and HMAC-authenticated with a key derived from
    // the device id: a copied file is useless on another machine and a
    // tampered one is rejected. This is obfuscation-grade protection against
    // casual copying, not a replacement for the OS keychain.
    public static class AuthSessionStore
    {
        [Serializable] public sealed class Data
        {
            public string refreshToken;
            public string email;
        }

        private const int Iterations = 10000;
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Ethereality.auth.session.v1");

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "ranked-session.dat");

        public static void Save(string refreshToken, string email)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(FilePath, Protect(JsonUtility.ToJson(
                    new Data { refreshToken = refreshToken, email = email }), DeviceSecret()));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"AuthSessionStore could not write session: {e.Message}");
            }
        }

        public static Data Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string json = Unprotect(File.ReadAllText(FilePath), DeviceSecret());
                var data = json != null ? JsonUtility.FromJson<Data>(json) : null;
                return data != null && !string.IsNullOrEmpty(data.refreshToken) ? data : null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                || e is ArgumentException)
            {
                return null;
            }
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"AuthSessionStore could not delete session: {e.Message}");
            }
        }

        private static string DeviceSecret() => SystemInfo.deviceUniqueIdentifier ?? "";

        // base64( iv[16] | ciphertext | hmac[32] ), encrypt-then-MAC.
        public static string Protect(string plain, string secret)
        {
            DeriveKeys(secret, out var encKey, out var macKey);
            using (var aes = Aes.Create())
            {
                aes.Key = encKey;
                aes.GenerateIV();
                byte[] cipher;
                using (var enc = aes.CreateEncryptor())
                {
                    var bytes = Encoding.UTF8.GetBytes(plain);
                    cipher = enc.TransformFinalBlock(bytes, 0, bytes.Length);
                }
                var blob = new byte[16 + cipher.Length + 32];
                Buffer.BlockCopy(aes.IV, 0, blob, 0, 16);
                Buffer.BlockCopy(cipher, 0, blob, 16, cipher.Length);
                using (var hmac = new HMACSHA256(macKey))
                {
                    var mac = hmac.ComputeHash(blob, 0, 16 + cipher.Length);
                    Buffer.BlockCopy(mac, 0, blob, 16 + cipher.Length, 32);
                }
                return Convert.ToBase64String(blob);
            }
        }

        // Returns null on a wrong key, corruption or tampering.
        public static string Unprotect(string protectedText, string secret)
        {
            byte[] blob;
            try { blob = Convert.FromBase64String(protectedText); }
            catch (FormatException) { return null; }
            if (blob.Length < 16 + 16 + 32) return null;
            DeriveKeys(secret, out var encKey, out var macKey);
            int cipherLength = blob.Length - 16 - 32;
            using (var hmac = new HMACSHA256(macKey))
            {
                var expected = hmac.ComputeHash(blob, 0, 16 + cipherLength);
                int diff = 0;
                for (int i = 0; i < 32; i++) diff |= expected[i] ^ blob[16 + cipherLength + i];
                if (diff != 0) return null;
            }
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = encKey;
                    var iv = new byte[16];
                    Buffer.BlockCopy(blob, 0, iv, 0, 16);
                    aes.IV = iv;
                    using (var dec = aes.CreateDecryptor())
                        return Encoding.UTF8.GetString(dec.TransformFinalBlock(blob, 16, cipherLength));
                }
            }
            catch (CryptographicException) { return null; }
        }

        private static void DeriveKeys(string secret, out byte[] encKey, out byte[] macKey)
        {
            using (var kdf = new Rfc2898DeriveBytes(secret, Salt, Iterations))
            {
                var material = kdf.GetBytes(64);
                encKey = new byte[32];
                macKey = new byte[32];
                Buffer.BlockCopy(material, 0, encKey, 0, 32);
                Buffer.BlockCopy(material, 32, macKey, 0, 32);
            }
        }
    }
}
