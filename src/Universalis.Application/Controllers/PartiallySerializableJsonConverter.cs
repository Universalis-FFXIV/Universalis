using System;
using System.Collections.Generic;
using System.Linq;
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
    // Store the type information of T statically once to avoid recomputing it thousands of times per second.
    // This also relieves GC pressure significantly. Once this is upgraded to .NET 7, this should be refactored
    // into a code generation-based converter to avoid this reflection altogether.
    private static readonly PropertyInfo[] Properties = typeof(T).GetProperties();

    // ReSharper disable once StaticMemberInGenericType
    private static readonly IReadOnlyDictionary<string, string> JsonPropertyNames = Properties
        .ToDictionary(prop => prop.Name,
            prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name);
    
    // ReSharper disable once StaticMemberInGenericType
    private static readonly IReadOnlyDictionary<string, JsonIgnoreAttribute> JsonIgnores = Properties
        .ToDictionary(prop => prop.Name,
            prop => prop.GetCustomAttribute<JsonIgnoreAttribute>());
    
    public override bool HandleNull => false;

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return JsonSerializer.Deserialize(ref reader, typeToConvert, options) as T;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        foreach (var property in Properties) {
            var name = JsonPropertyNames[property.Name];
            if (value.SerializableProperties != null && !value.SerializableProperties.Contains(name))
                continue;
            var propValue = property.GetValue(value);
            var ignoreAttrib = JsonIgnores[property.Name];
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
