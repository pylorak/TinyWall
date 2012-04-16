using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace PKSoft
{
    internal static class SerializationHelper
    {
        internal static void Serialize<T>(Stream stream, T obj)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            formatter.Serialize(stream, obj);
        }

        internal static T Deserialize<T>(Stream stream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            return (T)formatter.Deserialize(stream);
        }

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
                        //using (StreamReader sr = new StreamReader(cryptoStream))
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

        internal static void SaveToEncryptedXMLFile<T>(T obj, string filepath, string key, string iv)
        {
            // Construct encryptor
            using (AesCryptoServiceProvider symmetricKey = new AesCryptoServiceProvider())
            {
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Key = Encoding.ASCII.GetBytes(key);
                symmetricKey.IV = Encoding.ASCII.GetBytes(iv);

                // Encrypt
                FileStream fs = null;
                try
                {
                    fs = new FileStream(filepath, FileMode.Create, FileAccess.Write);
                    using (CryptoStream cryptoStream = new CryptoStream(fs, symmetricKey.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        fs = null;

                        //Create our own namespaces for the output
                        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                        ns.Add("", "");

                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        serializer.Serialize(cryptoStream, obj, ns);
                    }
                }
                finally
                {
                    if (fs != null)
                        fs.Dispose();
                }
            }

        }

        internal static T LoadFromXMLFile<T>(string filepath)
        {
            using (TextReader reader = new StreamReader(filepath, Encoding.UTF8))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(reader);
            }
        }

        internal static void SaveToXMLFile<T>(T obj, string filepath)
        {
            using (TextWriter writer = new StreamWriter(filepath, false, Encoding.UTF8))
            {
                //Create our own namespaces for the output
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");

                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, obj, ns);
            }
        }

    }
}
