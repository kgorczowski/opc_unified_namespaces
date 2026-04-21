namespace OPCGateway.Services.Monitoring;

public static class ValueTypeHelper
{
    public static (string ValueType, string ValueString) GetValueTypeAndString(object? value)
    {
        string valueType;
        string valueString;

        switch (value)
        {
            case int intValue:
                valueType = "int";
                valueString = intValue.ToString();
                break;
            case double doubleValue:
                valueType = "double";
                valueString = doubleValue.ToString("F8");
                break;
            case string stringValue:
                valueType = "string";
                valueString = stringValue;
                break;
            case bool boolValue:
                valueType = "bool";
                valueString = boolValue.ToString();
                break;
            case short shortValue:
                valueType = "int16";
                valueString = shortValue.ToString();
                break;
            case long longValue:
                valueType = "int64";
                valueString = longValue.ToString();
                break;
            case float floatValue:
                valueType = "float";
                valueString = floatValue.ToString("F2");
                break;
            case byte byteValue:
                valueType = "byte";
                valueString = byteValue.ToString();
                break;
            case decimal decimalValue:
                valueType = "decimal";
                valueString = decimalValue.ToString("F2");
                break;
            case Guid guidValue:
                valueType = "guid";
                valueString = guidValue.ToString();
                break;
            case DateTime dateTimeValue:
                valueType = "datetime";
                valueString = dateTimeValue.ToString("o");
                break;
            case sbyte sbyteValue:
                valueType = "sbyte";
                valueString = sbyteValue.ToString();
                break;
            case ushort ushortValue:
                valueType = "ushort";
                valueString = ushortValue.ToString();
                break;
            case uint uintValue:
                valueType = "uint";
                valueString = uintValue.ToString();
                break;
            case ulong ulongValue:
                valueType = "ulong";
                valueString = ulongValue.ToString();
                break;
            case byte[] byteArrayValue:
                valueType = "bytearray";
                valueString = Convert.ToBase64String(byteArrayValue);
                break;
            case DateTimeOffset dateTimeOffsetValue:
                valueType = "datetimeoffset";
                valueString = dateTimeOffsetValue.ToString("o");
                break;
            default:
                valueType = "unknown";
                valueString = value?.ToString() ?? "null";
                break;
        }

        return (valueType, valueString);
    }
}