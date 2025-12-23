using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Reflection;

namespace Application.Models.Enums;

public class EnumMemberTypeConverter<T> : TypeConverter where T : struct, Enum
{
    private static readonly Dictionary<string, T> _stringToEnum = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<T, string> _enumToString = new();

    static EnumMemberTypeConverter()
    {
        var enumType = typeof(T);
        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var enumValue = (T)field.GetValue(null)!;
            var enumMemberAttr = field.GetCustomAttribute<EnumMemberAttribute>();
            var stringValue = enumMemberAttr?.Value ?? field.Name;
            
            _stringToEnum[stringValue] = enumValue;
            _enumToString[enumValue] = stringValue;
            
            // Also allow numeric values
            _stringToEnum[((int)(object)enumValue).ToString()] = enumValue;
        }
    }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            if (_stringToEnum.TryGetValue(stringValue, out var enumValue))
            {
                return enumValue;
            }
        }
        
        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is T enumValue)
        {
            if (_enumToString.TryGetValue(enumValue, out var stringValue))
            {
                return stringValue;
            }
        }
        
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
