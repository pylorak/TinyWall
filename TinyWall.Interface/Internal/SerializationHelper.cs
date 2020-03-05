using System;
using System.IO;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace TinyWall.Interface.Internal
{
    public static class SerializationHelper
    {
        private static readonly Type[] KnownDataContractTypes =
        {
            typeof(TwMessage),
            typeof(MessageType),
            typeof(ServerState),
            typeof(FirewallMode),
            typeof(FirewallLogEntry),
            typeof(List<FirewallLogEntry>),

            typeof(BlockListSettings),
            typeof(ServerProfileConfiguration),
            typeof(ServerConfiguration),

            typeof(ExceptionSubject),
            typeof(GlobalSubject),
            typeof(ExecutableSubject),
            typeof(ServiceSubject),
            typeof(AppContainerSubject),

            typeof(ExceptionPolicy),
            typeof(HardBlockPolicy),
            typeof(UnrestrictedPolicy),
            typeof(TcpUdpPolicy),
            typeof(RuleListPolicy),

            typeof(RuleDef),
            typeof(List<RuleDef>),
            typeof(FirewallExceptionV3),

            typeof(UpdateModule),
            typeof(UpdateDescriptor),
        };

        public static void SerializeToPipe<T>(Stream pipe, T obj)
        {
            string xml;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.CloseOutput = false;
                settings.Indent = true;
                settings.Encoding = Encoding.UTF8;
                using (XmlWriter writer = XmlWriter.Create(memoryStream, settings))
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(T), KnownDataContractTypes);
                    serializer.WriteObject(writer, obj);
                }

                memoryStream.Position = 0;
                using (StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8))
                {
                    xml = reader.ReadToEnd();
                }
            }

            BinaryWriter bw = new BinaryWriter(pipe);
            bw.Write(xml);
            bw.Flush();
        }

        public static T DeserializeFromPipe<T>(Stream pipe)
        {
            BinaryReader br = new BinaryReader(pipe);
            string xml = br.ReadString();

            using (Stream stream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(xml);
                writer.Flush();
                stream.Position = 0;
                DataContractSerializer serializer = new DataContractSerializer(typeof(T), KnownDataContractTypes);
                return (T)serializer.ReadObject(stream);
            }
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
