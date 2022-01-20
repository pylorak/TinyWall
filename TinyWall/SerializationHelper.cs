using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace pylorak.TinyWall
{
    public interface ISerializable<T>
    {
        public JsonTypeInfo<T> GetJsonTypeInfo();
    }

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        GenerationMode = JsonSourceGenerationMode.Default,
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        IncludeFields = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        WriteIndented = false
        )]
    [JsonSerializable(typeof(TwMessage))]
    [JsonSerializable(typeof(TwMessageGetSettings))]
    [JsonSerializable(typeof(TwMessagePutSettings))]
    [JsonSerializable(typeof(TwMessageComError))]
    [JsonSerializable(typeof(TwMessageError))]
    [JsonSerializable(typeof(TwMessageLocked))]
    [JsonSerializable(typeof(TwMessageGetProcessPath))]
    [JsonSerializable(typeof(TwMessageReadFwLog))]
    [JsonSerializable(typeof(TwMessageIsLocked))]
    [JsonSerializable(typeof(TwMessageUnlock))]
    [JsonSerializable(typeof(TwMessageModeSwitch))]
    [JsonSerializable(typeof(TwMessageSetPassword))]
    [JsonSerializable(typeof(TwMessageSimple))]
    [JsonSerializable(typeof(TwMessageAddTempException))]
    [JsonSerializable(typeof(GlobalSubject))]
    [JsonSerializable(typeof(AppContainerSubject))]
    [JsonSerializable(typeof(ExecutableSubject))]
    [JsonSerializable(typeof(ServiceSubject))]
    [JsonSerializable(typeof(HardBlockPolicy))]
    [JsonSerializable(typeof(UnrestrictedPolicy))]
    [JsonSerializable(typeof(TcpUdpPolicy))]
    [JsonSerializable(typeof(RuleListPolicy))]
    [JsonSerializable(typeof(FirewallExceptionV3))]
    [JsonSerializable(typeof(ServerConfiguration))]
    [JsonSerializable(typeof(ControllerSettings))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }

    public static class SerializationHelper
    {
        public static byte[] Serialize<T>(T obj) where T : ISerializable<T>
        {
            return JsonSerializer.SerializeToUtf8Bytes<T>(obj, obj.GetJsonTypeInfo());
        }

        public static T? Deserialize<T>(byte[] utf8bytes, T defInstance) where T : ISerializable<T>
        {
            return JsonSerializer.Deserialize<T>(utf8bytes, defInstance.GetJsonTypeInfo());
        }

        public static T? Deserialize<T>(Stream stream, T defInstance) where T : ISerializable<T>
        {
            return JsonSerializer.Deserialize<T>(stream, defInstance.GetJsonTypeInfo());
        }

        private static readonly Type[] KnownDataContractTypes =
        {
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

        public static void SerializeToPipe<T>(PipeStream pipe, T obj) where T : ISerializable<T>
        {
            var utf8Bytes = Serialize<T>(obj);
            //string dbg = System.Text.Encoding.UTF8.GetString(utf8Bytes);
            pipe.Write(utf8Bytes, 0, utf8Bytes.Length);
            pipe.Flush();
        }

        public static T? DeserializeFromPipe<T>(PipeStream pipe, int timeout_ms, T defInstance) where T : ISerializable<T>
        {
            bool pipeClosed = false;
            var buf = new byte[4 * 1024];

            using var memoryStream = new MemoryStream();
            using var readDone = new System.Threading.AutoResetEvent(false);

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
                    throw new IOException("Pipe closed.");

                memoryStream.Write(buf, 0, len);
                timeout_ms = 1000;
            } while (!pipe.IsMessageComplete);

            memoryStream.Flush();
            memoryStream.Position = 0;

            //string dbg = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            return Deserialize<T>(memoryStream, defInstance);
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
