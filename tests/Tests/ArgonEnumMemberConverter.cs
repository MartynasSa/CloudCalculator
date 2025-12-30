using System.Runtime.Serialization;
using Argon;

namespace Tests;

public class ArgonEnumMemberConverter : Argon.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsEnum;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            string? enumText = reader.Value?.ToString();
            if (string.IsNullOrEmpty(enumText))
                return null;

            foreach (var field in objectType.GetFields())
            {
                var attribute = field.GetCustomAttributes(typeof(EnumMemberAttribute), false)
                    .Cast<EnumMemberAttribute>()
                    .FirstOrDefault();
                    
                if (attribute != null && attribute.Value == enumText)
                    return Enum.Parse(objectType, field.Name);
            }
            
            return Enum.Parse(objectType, enumText, true);
        }
        
        return null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var type = value.GetType();
        var field = type.GetField(value.ToString()!);
        
        if (field != null)
        {
            var attribute = field.GetCustomAttributes(typeof(EnumMemberAttribute), false)
                .Cast<EnumMemberAttribute>()
                .FirstOrDefault();
                
            if (attribute != null && !string.IsNullOrEmpty(attribute.Value))
            {
                writer.WriteValue(attribute.Value);
                return;
            }
        }
        
        writer.WriteValue(value.ToString());
    }
}
