namespace OPCGateway.Services.ReadWrite;

public interface IOpcReader
{
    Task<string> ReadDataAsync(string connectionId, int opcNamespace, string nodeId);

    Task<Dictionary<string, string>> ReadMultipleDataAsync(string connectionId, int opcNamespace, IEnumerable<string> nodeIds);
}