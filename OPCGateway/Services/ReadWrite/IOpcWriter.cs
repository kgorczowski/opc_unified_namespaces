using OPCGateway.Services.Implementation;

namespace OPCGateway.Services.ReadWrite;

public interface IOpcWriter
{
    Task WriteDataAsync(string connectionId, int opcNamespace, string nodeId, object value, OpcType valueType);

    Task WriteMultipleDataAsync(string connectionId, int opcNamespace, Dictionary<string, (object Value, OpcType ValueType)> nodeValues);
}