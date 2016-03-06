using System;
using System.Security.Cryptography;
using System.Text;

namespace TinyWall
{
    public static class Pbkdf2
    {
        public static string GetHash(string text, string salt, int iterations, int numBytes)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
            Rfc2898DeriveBytes hasher = new Rfc2898DeriveBytes(text, saltBytes, iterations);
            return Convert.ToBase64String(hasher.GetBytes(numBytes));
        }
        public static string GetHashForStorage(string text, string salt, int iterations, int numBytes)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
            Rfc2898DeriveBytes hasher = new Rfc2898DeriveBytes(text, saltBytes, iterations);
            string hash = Convert.ToBase64String(hasher.GetBytes(numBytes));
            return string.Format("Rfc2898;{0};{1};{2};{3}", salt, iterations, numBytes, hash);
        }
        public static bool CompareHash(string storedHash, string text)
        {
            string[] elems = storedHash.Split(';');
            string algo = elems[0];
            string salt = elems[1];
            int iterations = int.Parse(elems[2]);
            int numBytes = int.Parse(elems[3]);
            string hash = elems[4];

            string verificationHash = GetHashForStorage(text, salt, iterations, numBytes);
            return verificationHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
