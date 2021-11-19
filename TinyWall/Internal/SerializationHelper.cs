using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace pylorak.TinyWall
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

        public static void SerializeToPipe<T>(PipeStream pipe, T obj)
        {
            using var memoryStream = new MemoryStream();

            var settings = new XmlWriterSettings();
            settings.CloseOutput = false;
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;
            using (var writer = XmlWriter.Create(memoryStream, settings))
            {
                var serializer = new DataContractSerializer(typeof(T), KnownDataContractTypes);
                serializer.WriteObject(writer, obj);
            }

            memoryStream.Position = 0;
            var buf = memoryStream.ToArray();
            pipe.Write(buf, 0, buf.Length);
            pipe.Flush();
        }

        public static bool DeserializeFromPipe<T>(PipeStream pipe, int timeout_ms, ref T result)
        {
            bool pipeClosed = false;

            using (var memoryStream = new MemoryStream())
            using (var readDone = new System.Threading.AutoResetEvent(false))
            {
                var buf = new byte[4 * 1024];
                do
                {
                    int len = 0;
                    var res = pipe.BeginRead(buf, 0, buf.Length, delegate (IAsyncResult r)
                    {
                        try
                        {
                            len = pipe.EndRead(r);
                            if (len == 0)
                                pipeClosed = true;
                            readDone.Set();
                        }
                        catch { }
                    }, null);

                    if (!readDone.WaitOne(timeout_ms))
                        throw new TimeoutException("Timeout while waiting for answer from service.");

                    if (pipeClosed)
                        return false;

                    memoryStream.Write(buf, 0, len);
                    timeout_ms = 1000;
                } while (!pipe.IsMessageComplete);

                memoryStream.Flush();
                memoryStream.Position = 0;

                var settings = new XmlReaderSettings();
                settings.IgnoreComments = true;
                settings.IgnoreProcessingInstructions = true;
                settings.IgnoreWhitespace = true;

                using var reader = XmlReader.Create(memoryStream, settings);
                var serializer = new DataContractSerializer(typeof(T), KnownDataContractTypes);
                result = (T)serializer.ReadObject(reader);
            }

            return true;
        }


        public static void SerializeDC<T>(Stream stream, T obj)
        {
            var serializer = new DataContractSerializer(typeof(T), KnownDataContractTypes);
            var settings = new XmlWriterSettings();
            settings.CloseOutput = false;
            settings.Indent = true;
            using XmlWriter writer = XmlWriter.Create(stream, settings);
            serializer.WriteObject(writer, obj);
        }

        public static T DeserializeDC<T>(Stream stream)
        {
            var serializer = new DataContractSerializer(typeof(T), KnownDataContractTypes);
            return (T)serializer.ReadObject(stream);
        }

        public static T LoadFromEncryptedXMLFile<T>(string filepath, string key, string iv)
        {
            // Construct encryptor
            using var symmetricKey = new AesCryptoServiceProvider();
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Key = Encoding.ASCII.GetBytes(key);
            symmetricKey.IV = Encoding.ASCII.GetBytes(iv);

            // Decrypt
            using var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            using var cryptoStream = new CryptoStream(fs, symmetricKey.CreateDecryptor(), CryptoStreamMode.Read);
            return DeserializeDC<T>(cryptoStream);
        }

        public static void SaveToEncryptedXMLFile<T>(T obj, string filepath, string key, string iv)
        {
            // Construct encryptor
            using var symmetricKey = new AesCryptoServiceProvider();
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Key = Encoding.ASCII.GetBytes(key);
            symmetricKey.IV = Encoding.ASCII.GetBytes(iv);

            // Encrypt
            using var fs = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            using var cryptoStream = new CryptoStream(fs, symmetricKey.CreateEncryptor(), CryptoStreamMode.Write);
            SerializeDC(cryptoStream, obj);
        }

        public static T LoadFromXMLFile<T>(string filepath)
        {
            using var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            return DeserializeDC<T>(stream);
        }

        public static void SaveToXMLFile<T>(T obj, string filepath)
        {
            using var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            SerializeDC(stream, obj);
        }

    }
}
