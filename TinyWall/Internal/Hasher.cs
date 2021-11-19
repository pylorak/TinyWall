using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace pylorak.TinyWall
{
    public static class Hasher
    {
        public static string HashStream(Stream stream)
        {
            using SHA256Cng hasher = new();
            return Utils.HexEncode(hasher.ComputeHash(stream));
        }

        public static string HashFile(string filePath)
        {
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            return HashStream(fs);
        }

        public static string HashString(string text)
        {
            using SHA256Cng hasher = new();
            return Utils.HexEncode(hasher.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        public static string HashFileSha1(string filePath)
        {
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            using SHA1Cng hasher = new();
            return Utils.HexEncode(hasher.ComputeHash(fs));
        }
    }
}
