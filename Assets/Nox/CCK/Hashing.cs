using System;
using System.Security.Cryptography;

namespace Nox.CCK
{
    public class Hashing
    {

        public static string Hash(string password)
        {
            var salt = new byte[8];
            new RNGCryptoServiceProvider().GetBytes(salt);
            var key = new Rfc2898DeriveBytes(password, salt, 64).GetBytes(64);
            return Convert.ToBase64String(salt) + ':' + Convert.ToBase64String(key);
        }

        public static bool Verify(string password, string hash)
        {
            var parts = hash.Split(':');
            var salt = Convert.FromBase64String(parts[0]);
            var key = new Rfc2898DeriveBytes(password, salt, 64).GetBytes(64);
            return parts[1] == Convert.ToBase64String(key);
        }

        public static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static string HashFile(string path)
        {
            using var sha = SHA256.Create();
            using var stream = System.IO.File.OpenRead(path);
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}