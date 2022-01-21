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
using pylorak.Utilities;

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
        WriteIndented = true
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
    [JsonSerializable(typeof(UpdateDescriptor))]
    [JsonSerializable(typeof(ConfigContainer))]
    [JsonSerializable(typeof(DatabaseClasses.SubjectIdentity))]
    [JsonSerializable(typeof(DatabaseClasses.Application))]
    [JsonSerializable(typeof(DatabaseClasses.AppDatabase))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }

    public static class SerializationHelper
    {
        public static byte[] Serialize<T>(T obj) where T : ISerializable<T>
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj, obj.GetJsonTypeInfo());
        }

        public static void Serialize<T>(Stream stream, T obj) where T : ISerializable<T>
        {
            JsonSerializer.Serialize(stream, obj, obj.GetJsonTypeInfo());
        }

        public static T Deserialize<T>(byte[] utf8bytes, T defInstance) where T : ISerializable<T>
        {
            return JsonSerializer.Deserialize(utf8bytes, defInstance.GetJsonTypeInfo()) ?? throw new NullResultExceptions(nameof(JsonSerializer.Deserialize));
        }

        public static T Deserialize<T>(Stream stream, T defInstance) where T : ISerializable<T>
        {
            return JsonSerializer.Deserialize(stream, defInstance.GetJsonTypeInfo()) ?? throw new NullResultExceptions(nameof(JsonSerializer.Deserialize));
        }

        public static void SerializeToPipe<T>(PipeStream pipe, T obj) where T : ISerializable<T>
        {
            // Pipe might be message-based, so we want to make sure the whole serialized object
            // gets written to the pipe in a single write. To ensure this, we serialize to a
            // byte-array first.

            var utf8Bytes = Serialize(obj);
            //string dbg = System.Text.Encoding.UTF8.GetString(utf8Bytes);
            pipe.Write(utf8Bytes, 0, utf8Bytes.Length);
            pipe.Flush();
        }

        public static T DeserializeFromPipe<T>(PipeStream pipe, int timeout_ms, T defInstance) where T : ISerializable<T>
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
            return Deserialize(memoryStream, defInstance);
        }

        public static T DeserializeFromFile<T>(string filepath, T defInstance, bool readOnlySource = false) where  T : ISerializable<T>
        {
            try
            {
                using var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                return Deserialize(stream, defInstance);
            }
            catch
            {
                // Try loading from old serialization format, and save in new format if allowed
                var xmlPath = filepath.EndsWith(".json") ? Path.ChangeExtension(filepath, ".xml") : filepath;
                var ret = LoadFromXMLFile<T>(xmlPath);
                if (!readOnlySource) SerializeToFile(ret, filepath);
                return ret;
            }
        }

        public static void SerializeToFile<T>(T obj, string filepath) where T : ISerializable<T>
        {
            using var fileUpdater = new AtomicFileUpdater(filepath);
            using (var stream = new FileStream(fileUpdater.TemporaryFilePath, FileMode.Create, FileAccess.Write))
            {
                Serialize(stream, obj);
            }
            fileUpdater.Commit();
        }

        public static T DeserializeFromEncryptedFile<T>(string filepath, string key, string iv, T defInst) where T : ISerializable<T>
        {
            try
            {
                // Construct encryptor
                using var symmetricKey = new AesCryptoServiceProvider();
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Key = Encoding.ASCII.GetBytes(key);
                symmetricKey.IV = Encoding.ASCII.GetBytes(iv);

                // Decrypt
                using var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                using var cryptoStream = new CryptoStream(fs, symmetricKey.CreateDecryptor(), CryptoStreamMode.Read);
                return Deserialize<T>(cryptoStream, defInst);
            }
            catch
            {
                // Try loading from old serialization format, and save in new format if allowed
                var xmlPath = filepath.EndsWith(".json") ? Path.ChangeExtension(filepath, ".xml") : filepath;
                var ret = LoadFromEncryptedXMLFile<T>(xmlPath, key, iv);
                SerializeToEncryptedFile(ret, filepath, key, iv);
                return ret;
            }
        }

        public static void SerializeToEncryptedFile<T>(T obj, string filePath, string key, string iv) where T : ISerializable<T>
        {
            // Construct encryptor
            using var symmetricKey = new AesCryptoServiceProvider();
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Key = Encoding.ASCII.GetBytes(key);
            symmetricKey.IV = Encoding.ASCII.GetBytes(iv);

            // Encrypt
            using var fileUpdater = new AtomicFileUpdater(filePath);
            using (var fs = new FileStream(fileUpdater.TemporaryFilePath, FileMode.Create, FileAccess.Write))
            {
                using var cryptoStream = new CryptoStream(fs, symmetricKey.CreateEncryptor(), CryptoStreamMode.Write);
                Serialize(cryptoStream, obj);
            }
            fileUpdater.Commit();
        }

        [Obsolete]
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
        
        [Obsolete]
        public static T DeserializeDC<T>(Stream stream)
        {
            var serializer = new DataContractSerializer(typeof(T), KnownDataContractTypes);
            return ((T?)serializer.ReadObject(stream)) ?? throw new NullResultExceptions("DataContractSerializer.ReadObject()");
        }

        [Obsolete]
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

        [Obsolete]
        public static T LoadFromXMLFile<T>(string filepath)
        {
            using var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            return DeserializeDC<T>(stream);
        }
    }
}
