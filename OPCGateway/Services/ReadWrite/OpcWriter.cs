using System.Text.Json;
using Opc.Ua;
using OPCGateway.Services.Connections;
using OPCGateway.Services.Implementation;
using StatusCodes = Opc.Ua.StatusCodes;

namespace OPCGateway.Services.ReadWrite;

public class OpcWriter(IOpcConnectionManagement connectionManagement) : IOpcWriter
{
    public async Task WriteDataAsync(string connectionId, int opcNamespace, string nodeId, object value, OpcType valueType)
    {
        await connectionManagement.CheckConnection(connectionId);

        var session = connectionManagement.GetSession(connectionId);
        var variantValue = ConvertToVariant(value, valueType);

        var writeValue = new WriteValue
        {
            NodeId = new NodeId(OpcUtilities.GetNodeWithNamespace(opcNamespace, nodeId)),
            AttributeId = Attributes.Value,
            Value = new DataValue(variantValue),
        };

        var writeRequest = new WriteRequest
        {
            NodesToWrite = new[] { writeValue },
        };

        var response = await session.WriteAsync(writeRequest.RequestHeader, writeRequest.NodesToWrite, CancellationToken.None);

        if (response.Results[0] != StatusCodes.Good)
        {
            throw new InvalidOperationException($"Failed to write data to OPC server. ConnectionId: {connectionId}, Namespace: {opcNamespace}, NodeId: {nodeId}, Value: {value}, ValueType: {valueType}");
        }
    }

    public async Task WriteMultipleDataAsync(string connectionId, int opcNamespace, Dictionary<string, (object Value, OpcType ValueType)> nodeValues)
    {
        await connectionManagement.CheckConnection(connectionId);

        var session = connectionManagement.GetSession(connectionId);
        var writeValues = nodeValues.Select(kv => new WriteValue
        {
            NodeId = new NodeId(OpcUtilities.GetNodeWithNamespace(opcNamespace, kv.Key)),
            AttributeId = Attributes.Value,
            Value = new DataValue(ConvertToVariant(kv.Value.Value, kv.Value.ValueType)),
        }).ToArray();

        var writeRequest = new WriteRequest
        {
            NodesToWrite = writeValues,
        };

        var response = await session.WriteAsync(writeRequest.RequestHeader, writeRequest.NodesToWrite, CancellationToken.None);

        for (int i = 0; i < response.Results.Count; i++)
        {
            if (response.Results[i] != StatusCodes.Good)
            {
                throw new InvalidOperationException($"Failed to write data to OPC server for node {nodeValues.ElementAt(i).Key}.");
            }
        }
    }

    private static Variant ConvertToVariant(object value, OpcType valueType)
    {
        try
        {
            var jsonElement = (JsonElement)value;

            return valueType switch
            {
                OpcType.Int16 => new Variant(jsonElement.GetInt16()),
                OpcType.Int32 => new Variant(jsonElement.GetInt32()),
                OpcType.Int64 => new Variant(jsonElement.GetInt64()),
                OpcType.String => new Variant(jsonElement.GetString()),
                OpcType.Double => new Variant(jsonElement.GetDouble()),
                OpcType.Float => new Variant(jsonElement.GetSingle()),
                OpcType.Boolean => new Variant(jsonElement.GetBoolean()),
                OpcType.Byte => new Variant(jsonElement.GetByte()),
                OpcType.Decimal => new Variant(jsonElement.GetDecimal()),
                OpcType.Guid => new Variant(jsonElement.GetGuid()),
                OpcType.DateTime => new Variant(jsonElement.GetDateTime()),
                OpcType.Sbyte => new Variant(jsonElement.GetSByte()),
                OpcType.Ushort => new Variant(jsonElement.GetUInt16()),
                OpcType.Uint => new Variant(jsonElement.GetUInt32()),
                OpcType.Ulong => new Variant(jsonElement.GetUInt64()),
                OpcType.ByteArray => new Variant(jsonElement.GetBytesFromBase64()),
                OpcType.DateTimeOffset => new Variant(jsonElement.GetDateTimeOffset()),
                _ => throw new ArgumentException("Unsupported value type."),
            };
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to convert value to {valueType}.", ex);
        }
    }
}
