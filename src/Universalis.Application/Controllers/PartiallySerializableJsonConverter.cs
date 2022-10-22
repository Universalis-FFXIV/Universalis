using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Universalis.Application.Common;

namespace Universalis.Application.Controllers;

public class PartiallySerializableJsonConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsAssignableTo(typeof(PartiallySerializable));

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        return (JsonConverter)Activator.CreateInstance(typeof(PartiallySerializableJsonConverter<>).MakeGenericType(typeToConvert),
            BindingFlags.Instance | BindingFlags.Public, null, null, null);
    }
}

public class PartiallySerializableJsonConverter<T> : JsonConverter<T> where T : PartiallySerializable {
    public override bool HandleNull => false;

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return JsonSerializer.Deserialize(ref reader, typeToConvert, options) as T;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        foreach (var property in value.GetType().GetProperties()) {
            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
            if (value.SerializableProperties != null && !value.SerializableProperties.Contains(name))
                continue;
            var propValue = property.GetValue(value);
            var ignoreAttrib = property.GetCustomAttribute<JsonIgnoreAttribute>();
            if (ignoreAttrib?.Condition == JsonIgnoreCondition.Always)
                continue;
            if (propValue == null && ignoreAttrib?.Condition == JsonIgnoreCondition.WhenWritingNull)
                continue;
            writer.WritePropertyName(name);
            JsonSerializer.Serialize(writer, propValue, options);
        }

        writer.WriteEndObject();
    }
}
