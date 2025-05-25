using System;
using System.Security.Cryptography;
using System.Text;

namespace ReminderApp
{
    public static class PasswordHasher
    {
        // Generate a 128-bit salt using a secure PRNG
        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        // Hash password with PBKDF2-SHA256 (10000 iterations)
        public static string HashPassword(string password)
        {
            byte[] salt = GenerateSalt();
            byte[] hash = PBKDF2(password, salt, 10000, 20);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        // iverify ang password against the stored hash
        public static bool VerifyPassword(string password, string storedHash)
        {
            string[] parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] hash = Convert.FromBase64String(parts[1]);
            byte[] testHash = PBKDF2(password, salt, 10000, hash.Length);
            return SlowEquals(hash, testHash);
        }

        private static byte[] PBKDF2(string password, byte[] salt, int iterations, int outputBytes)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return pbkdf2.GetBytes(outputBytes);
            }
        }

        // Compare byte arrays in constant time to prevent timing attacks
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
    }
}