using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;


namespace PKSoft
{
    internal static class Hasher
    {
        internal static byte[] HashStream(Stream stream)
        {
            using (SHA256Cng hasher = new SHA256Cng())
            {
                return hasher.ComputeHash(stream);
            }
        }

        internal static byte[] HashFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return HashStream(fs);
            }
        }

        internal static string HashString(string text)
        {
            using (SHA256Cng hasher = new SHA256Cng())
            {
                return Utils.HexEncode(hasher.ComputeHash(Encoding.UTF8.GetBytes(text)));
            }
        }
    }
}
