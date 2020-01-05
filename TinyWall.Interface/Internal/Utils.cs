using System;
using System.Globalization;
using System.Text;

namespace TinyWall.Interface.Internal
{
    public static class Utils
    {
        private static readonly Random _rng = new Random();

        public static string HexEncode(byte[] binstr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte oct in binstr)
                sb.Append(oct.ToString(@"X2", CultureInfo.InvariantCulture));

            return sb.ToString();
        }

        public static string ExecutablePath { get; } = System.Reflection.Assembly.GetEntryAssembly().Location;

        public static string RandomString(int length)
        {
            const string chars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[_rng.Next(chars.Length)];
            }
            return new string(buffer);
        }
    }
}
