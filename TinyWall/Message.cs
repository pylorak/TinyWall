using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace pylorak.TinyWall
{
    public class TwMessageConverter : PolymorphicJsonConverter<TwMessage>
    {
        public override string DiscriminatorPropertyName => "Type";

        public override TwMessage? DeserializeDerived(ref Utf8JsonReader reader, int discriminator)
        {
            var ret = (MessageType)discriminator switch
            {
                MessageType.GET_SETTINGS => (TwMessage?)JsonSerializer.Deserialize<TwMessageGetSettings>(ref reader, SourceGenerationContext.Default.TwMessageGetSettings),
                MessageType.PUT_SETTINGS => (TwMessage?)JsonSerializer.Deserialize<TwMessagePutSettings>(ref reader, SourceGenerationContext.Default.TwMessagePutSettings),
                MessageType.COM_ERROR => (TwMessage?)JsonSerializer.Deserialize<TwMessageComError>(ref reader, SourceGenerationContext.Default.TwMessageComError),
                MessageType.RESPONSE_ERROR => (TwMessage?)JsonSerializer.Deserialize<TwMessageError>(ref reader, SourceGenerationContext.Default.TwMessageError),
                MessageType.RESPONSE_LOCKED => (TwMessage?)JsonSerializer.Deserialize<TwMessageLocked>(ref reader, SourceGenerationContext.Default.TwMessageLocked),
                MessageType.GET_PROCESS_PATH => (TwMessage?)JsonSerializer.Deserialize<TwMessageGetProcessPath>(ref reader, SourceGenerationContext.Default.TwMessageGetProcessPath),
                MessageType.READ_FW_LOG => (TwMessage?)JsonSerializer.Deserialize<TwMessageReadFwLog>(ref reader, SourceGenerationContext.Default.TwMessageReadFwLog),
                MessageType.IS_LOCKED => (TwMessage?)JsonSerializer.Deserialize<TwMessageIsLocked>(ref reader, SourceGenerationContext.Default.TwMessageIsLocked),
                MessageType.UNLOCK => (TwMessage?)JsonSerializer.Deserialize<TwMessageUnlock>(ref reader, SourceGenerationContext.Default.TwMessageUnlock),
                MessageType.MODE_SWITCH => (TwMessage?)JsonSerializer.Deserialize<TwMessageModeSwitch>(ref reader, SourceGenerationContext.Default.TwMessageModeSwitch),
                MessageType.SET_PASSPHRASE => (TwMessage?)JsonSerializer.Deserialize<TwMessageSetPassword>(ref reader, SourceGenerationContext.Default.TwMessageSetPassword),
                MessageType.REINIT => (TwMessage?)JsonSerializer.Deserialize<TwMessageSimple>(ref reader, SourceGenerationContext.Default.TwMessageSimple),
                MessageType.LOCK => (TwMessage?)JsonSerializer.Deserialize<TwMessageSimple>(ref reader, SourceGenerationContext.Default.TwMessageSimple),
                MessageType.STOP_SERVICE => (TwMessage?)JsonSerializer.Deserialize<TwMessageSimple>(ref reader, SourceGenerationContext.Default.TwMessageSimple),
                MessageType.MINUTE_TIMER => (TwMessage?)JsonSerializer.Deserialize<TwMessageSimple>(ref reader, SourceGenerationContext.Default.TwMessageSimple),
                MessageType.ADD_TEMPORARY_EXCEPTION => (TwMessage?)JsonSerializer.Deserialize<TwMessageAddTempException>(ref reader, SourceGenerationContext.Default.TwMessageAddTempException),
                _ => throw new JsonException($"Tried to deserialize unsupported type with discriminator {(MessageType)discriminator}."),
            };
            return ret;
        }

        public override void SerializeDerived(Utf8JsonWriter writer, TwMessage value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case TwMessageGetSettings typedVal:
                    JsonSerializer.Serialize<TwMessageGetSettings>(writer, typedVal, SourceGenerationContext.Default.TwMessageGetSettings); break;
                case TwMessagePutSettings typedVal:
                    JsonSerializer.Serialize<TwMessagePutSettings>(writer, typedVal, SourceGenerationContext.Default.TwMessagePutSettings); break;
                case TwMessageComError typedVal:
                    JsonSerializer.Serialize<TwMessageComError>(writer, typedVal, SourceGenerationContext.Default.TwMessageComError); break;
                case TwMessageError typedVal:
                    JsonSerializer.Serialize<TwMessageError>(writer, typedVal, SourceGenerationContext.Default.TwMessageError); break;
                case TwMessageLocked typedVal:
                    JsonSerializer.Serialize<TwMessageLocked>(writer, typedVal, SourceGenerationContext.Default.TwMessageLocked); break;
                case TwMessageGetProcessPath typedVal:
                    JsonSerializer.Serialize<TwMessageGetProcessPath>(writer, typedVal, SourceGenerationContext.Default.TwMessageGetProcessPath); break;
                case TwMessageReadFwLog typedVal:
                    JsonSerializer.Serialize<TwMessageReadFwLog>(writer, typedVal, SourceGenerationContext.Default.TwMessageReadFwLog); break;
                case TwMessageIsLocked typedVal:
                    JsonSerializer.Serialize<TwMessageIsLocked>(writer, typedVal, SourceGenerationContext.Default.TwMessageIsLocked); break;
                case TwMessageUnlock typedVal:
                    JsonSerializer.Serialize<TwMessageUnlock>(writer, typedVal, SourceGenerationContext.Default.TwMessageUnlock); break;
                case TwMessageModeSwitch typedVal:
                    JsonSerializer.Serialize<TwMessageModeSwitch>(writer, typedVal, SourceGenerationContext.Default.TwMessageModeSwitch); break;
                case TwMessageSetPassword typedVal:
                    JsonSerializer.Serialize<TwMessageSetPassword>(writer, typedVal, SourceGenerationContext.Default.TwMessageSetPassword); break;
                case TwMessageSimple typedVal:
                    JsonSerializer.Serialize<TwMessageSimple>(writer, typedVal, SourceGenerationContext.Default.TwMessageSimple); break;
                case TwMessageAddTempException typedVal:
                    JsonSerializer.Serialize<TwMessageAddTempException>(writer, typedVal, SourceGenerationContext.Default.TwMessageAddTempException); break;
                default:
                    throw new JsonException($"Tried to serialize unsupported type {value.GetType()}.");
            };
        }
    }

    [JsonConverter(typeof(TwMessageConverter))]
    public abstract record TwMessage : ISerializable<TwMessage>
    {
        [JsonPropertyOrder(-1)]
        public MessageType Type { get; }

        [JsonConstructor]
        public TwMessage(MessageType type)
        {
            Type = type;
        }

        public JsonTypeInfo<TwMessage> GetJsonTypeInfo()
        {
            return SourceGenerationContext.Default.TwMessage;
        }
    }

    public record TwMessageComError : TwMessage
    {
        public static TwMessageComError Instance { get; } = new TwMessageComError();

        [JsonConstructor]
        public TwMessageComError() :
            base(MessageType.COM_ERROR)
        { }
    }

    public record TwMessageError : TwMessage
    {
        public static TwMessageError Instance { get; } = new TwMessageError();

        [JsonConstructor]
        public TwMessageError() :
            base(MessageType.RESPONSE_ERROR)
        { }
    }

    public record TwMessageLocked : TwMessage
    {
        public static TwMessageLocked Instance { get; } = new TwMessageLocked();

        [JsonConstructor]
        public TwMessageLocked() :
            base(MessageType.RESPONSE_LOCKED)
        { }
    }

    public record TwMessageGetSettings : TwMessage
    {
        public Guid Changeset { get; }
        public ServerConfiguration? Config { get; }
        public ServerState? State { get; }

        [JsonConstructor]
        public TwMessageGetSettings(Guid changeset, ServerConfiguration? config, ServerState? state) :
            base(MessageType.GET_SETTINGS)
        {
            Changeset = changeset;
            Config = config;
            State = state;
        }

        public static TwMessageGetSettings NewRequest(Guid clientChangeset)
        {
            return new TwMessageGetSettings(clientChangeset, null, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessageGetSettings NewResponse(Guid serverChangeset, ServerConfiguration config, ServerState state)
        {
            return new TwMessageGetSettings(serverChangeset, config, state);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessageGetSettings NewResponse(Guid serverChangeset)
        {
            return new TwMessageGetSettings(serverChangeset, null, null);
        }
    }

    public record TwMessagePutSettings : TwMessage
    {
        public Guid Changeset { get; }
        public ServerConfiguration Config { get; }
        public ServerState? State { get; }
        public bool Warning { get; }

        [JsonConstructor]
        public TwMessagePutSettings(Guid changeset, ServerConfiguration config, ServerState? state, bool warning) :
            base(MessageType.PUT_SETTINGS)
        {
            Changeset = changeset;
            Config = config;
            State = state;
            Warning = warning;
        }

        public static TwMessagePutSettings NewRequest(Guid clientChangeset, ServerConfiguration config)
        {
            return new TwMessagePutSettings(clientChangeset, config, null, false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessagePutSettings NewResponse(Guid serverChangeset, ServerConfiguration config, ServerState? state, bool warning)
        {
            return new TwMessagePutSettings(serverChangeset, config, state, warning);
        }
    }

    public record TwMessageGetProcessPath : TwMessage
    {
        public uint Pid { get; }
        public string Path { get; }

        [JsonConstructor]
        public TwMessageGetProcessPath(uint pid, string path) :
            base(MessageType.GET_PROCESS_PATH)
        {
            Pid = pid;
            Path = path;
        }

        public static TwMessageGetProcessPath NewRequest(uint pid)
        {
            return new TwMessageGetProcessPath(pid, string.Empty);
        }
        public TwMessageGetProcessPath NewResponse(string path)
        {
            return new TwMessageGetProcessPath(Pid, path);
        }
    }

    public record TwMessageReadFwLog : TwMessage
    {
        public FirewallLogEntry[] Entries { get; }

        [JsonConstructor]
        public TwMessageReadFwLog(FirewallLogEntry[] entries) :
            base(MessageType.READ_FW_LOG)
        {
            Entries = entries;
        }

        public static TwMessageReadFwLog NewRequest()
        {
            return new TwMessageReadFwLog(Array.Empty<FirewallLogEntry>());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessageReadFwLog NewResponse(FirewallLogEntry[] entries)
        {
            return new TwMessageReadFwLog(entries);
        }
    }

    public record TwMessageIsLocked : TwMessage
    {
        public bool LockedStatus { get; }

        [JsonConstructor]
        public TwMessageIsLocked(bool lockedStatus) :
            base(MessageType.IS_LOCKED)
        {
            LockedStatus = lockedStatus;
        }

        public static TwMessageIsLocked NewRequest()
        {
            return new TwMessageIsLocked(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessageIsLocked NewResponse(bool lockedStatus)
        {
            return new TwMessageIsLocked(lockedStatus);
        }
    }

    public record TwMessageUnlock : TwMessage
    {
        public string Password { get; }

        [JsonConstructor]
        public TwMessageUnlock(string password) :
            base(MessageType.UNLOCK)
        {
            Password = password;
        }

        public static TwMessageUnlock NewRequest(string password)
        {
            return new TwMessageUnlock(password);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessageUnlock NewResponse()
        {
            return new TwMessageUnlock(string.Empty);
        }
    }

    public record TwMessageModeSwitch : TwMessage
    {
        public FirewallMode Mode { get; }

        [JsonConstructor]
        public TwMessageModeSwitch(FirewallMode mode) :
            base(MessageType.MODE_SWITCH)
        {
            Mode = mode;
        }

        public static TwMessageModeSwitch NewRequest(FirewallMode mode)
        {
            return new TwMessageModeSwitch(mode);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessageModeSwitch NewResponse(FirewallMode mode)
        {
            return new TwMessageModeSwitch(mode);
        }
    }

    public record TwMessageSetPassword : TwMessage
    {
        public string Password { get; }

        [JsonConstructor]
        public TwMessageSetPassword(string password) :
            base(MessageType.SET_PASSPHRASE)
        {
            Password = password;
        }

        public static TwMessageSetPassword NewRequest(string pwd)
        {
            return new TwMessageSetPassword(pwd);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessageSetPassword NewResponse()
        {
            return new TwMessageSetPassword(string.Empty);
        }
    }

    public record TwMessageSimple : TwMessage
    {
        [JsonConstructor]
        public TwMessageSimple(MessageType type) :
            base(type)
        { }

        public static TwMessageSimple NewRequest(MessageType type)
        {
            return new TwMessageSimple(type);
        }

        public TwMessageSimple NewResponse()
        {
            return new TwMessageSimple(Type);
        }
    }

    public record TwMessageAddTempException : TwMessage
    {
        public FirewallExceptionV3[] Exceptions { get; }

        [JsonConstructor]
        public TwMessageAddTempException(FirewallExceptionV3[] exceptions) :
            base(MessageType.ADD_TEMPORARY_EXCEPTION)
        {
            Exceptions = exceptions;
        }

        public static TwMessageAddTempException NewRequest(FirewallExceptionV3[] exceptions)
        {
            return new TwMessageAddTempException(exceptions);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessageAddTempException NewResponse()
        {
            return new TwMessageAddTempException(Array.Empty<FirewallExceptionV3>());
        }
    }

    public record TwMessageDisplayPowerEvent : TwMessage
    {
        public bool PowerOn { get; }

        [JsonConstructor]
        public TwMessageDisplayPowerEvent(bool powerOn) :
            base(MessageType.DISPLAY_POWER_EVENT)
        {
            PowerOn = powerOn;
        }

        public static TwMessageDisplayPowerEvent NewRequest(bool powerOn)
        {
            return new TwMessageDisplayPowerEvent(powerOn);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public TwMessageDisplayPowerEvent NewResponse(bool powerOn)
        {
            return new TwMessageDisplayPowerEvent(powerOn);
        }
    }
}
