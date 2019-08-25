using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace TinyWall.Interface.Internal
{
    public static class SerializationHelper
    {
        private static readonly Type[] KnownDataContractTypes =
        {
            typeof(BlockListSettings),
            typeof(ServerProfileConfiguration),
            typeof(ServerConfiguration),

            typeof(ExceptionSubject),
            typeof(GlobalSubject),
            typeof(ExecutableSubject),
            typeof(ServiceSubject),

            typeof(ExceptionPolicy),
            typeof(HardBlockPolicy),
            typeof(UnrestrictedPolicy),
            typeof(TcpUdpPolicy),
            typeof(RuleListPolicy),

            typeof(RuleDef),
            typeof(FirewallExceptionV3),

            typeof(UpdateModule),
            typeof(UpdateDescriptor),
        };

        public static void Serialize<T>(Stream stream, T obj)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            formatter.Context = new StreamingContext(StreamingContextStates.Persistence | StreamingContextStates.CrossMachine);
            formatter.Serialize(stream, obj);
        }

        public static T Deserialize<T>(Stream stream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            formatter.Context = new StreamingContext(StreamingContextStates.Persistence | StreamingContextStates.CrossMachine);
            return (T)formatter.Deserialize(stream);
        }

        public static void SerializeDC<T>(Stream stream, T obj)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T), KnownDataContractTypes);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.CloseOutput = false;
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                serializer.WriteObject(writer, obj);
            }
        }

        public static T DeserializeDC<T>(Stream stream)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T), KnownDataContractTypes);
            return (T)serializer.ReadObject(stream);
        }

        public static T LoadFromEncryptedXMLFile<T>(string filepath, string key, string iv)
        {
            // Construct encryptor
            using (AesCryptoServiceProvider symmetricKey = new AesCryptoServiceProvider())
            {
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Key = Encoding.ASCII.GetBytes(key);
                symmetricKey.IV = Encoding.ASCII.GetBytes(iv);

                // Decrypt
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                using (CryptoStream cryptoStream = new CryptoStream(fs, symmetricKey.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    return DeserializeDC<T>(cryptoStream);
                }
            }
        }

        public static void SaveToEncryptedXMLFile<T>(T obj, string filepath, string key, string iv)
        {
            // Construct encryptor
            using (AesCryptoServiceProvider symmetricKey = new AesCryptoServiceProvider())
            {
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Key = Encoding.ASCII.GetBytes(key);
                symmetricKey.IV = Encoding.ASCII.GetBytes(iv);

                // Encrypt
                using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                using (CryptoStream cryptoStream = new CryptoStream(fs, symmetricKey.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    SerializeDC(cryptoStream, obj);
                }
            }
        }

        public static T LoadFromXMLFile<T>(string filepath)
        {
            using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                return DeserializeDC<T>(stream);
            }
        }

        public static void SaveToXMLFile<T>(T obj, string filepath)
        {
            using (FileStream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                SerializeDC(stream, obj);
            }
        }

    }
}
