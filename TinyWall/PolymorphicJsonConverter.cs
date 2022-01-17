using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace pylorak.TinyWall
{
    public abstract class PolymorphicJsonConverter<T> : JsonConverter<T>
    {
        public abstract string DiscriminatorPropertyName { get; }
        public abstract T? DeserializeDerived(ref Utf8JsonReader reader, int discriminator);
        public abstract void SerializeDerived(Utf8JsonWriter writer, T value, JsonSerializerOptions options);

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader readerClone = reader;

            if (readerClone.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            while (true)
            {
                readerClone.Read();
                if (readerClone.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var propertyName = readerClone.GetString();
                if (propertyName != DiscriminatorPropertyName)
                    readerClone.Skip();
                else
                    break;
            }

            readerClone.Read();
            if (readerClone.TokenType != JsonTokenType.Number)
                throw new JsonException();

            return DeserializeDerived(ref reader, readerClone.GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            SerializeDerived(writer, value, options);
        }
    }
}
