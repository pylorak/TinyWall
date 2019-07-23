using System.Xml.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

namespace PKSoft.Deprecated
{
    [Obsolete]
    internal static class SerializationHelper
    {
        internal static T LoadFromEncryptedXMLFile<T>(string filepath, string key, string iv)
        {
            // Construct encryptor
            using (AesCryptoServiceProvider symmetricKey = new AesCryptoServiceProvider())
            {
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Key = Encoding.ASCII.GetBytes(key);
                symmetricKey.IV = Encoding.ASCII.GetBytes(iv);

                // Decrypt
                FileStream fs = null;
                try
                {
                    fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                    using (CryptoStream cryptoStream = new CryptoStream(fs, symmetricKey.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        fs = null;

                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        {
                            return (T)serializer.Deserialize(cryptoStream);
                        }
                    }
                }
                finally
                {
                    if (fs != null)
                        fs.Dispose();
                }
            }
        }

        public static T LoadFromXMLFile<T>(string filepath)
        {
            using (TextReader reader = new StreamReader(filepath, Encoding.UTF8))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(reader);
            }
        }

    }
}
