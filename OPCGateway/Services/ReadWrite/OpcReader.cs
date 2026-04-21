using Opc.Ua;
using OPCGateway.Services.Connections;
using StatusCodes = Opc.Ua.StatusCodes;

namespace OPCGateway.Services.ReadWrite;

public class OpcReader(IOpcConnectionManagement connectionManagement) : IOpcReader
{
    public async Task<string> ReadDataAsync(string connectionId, int opcNamespace, string nodeId)
    {
        await connectionManagement.CheckConnection(connectionId);

        var session = connectionManagement.GetSession(connectionId);
        var readValueId = CreateReadValueId(OpcUtilities.GetNodeWithNamespace(opcNamespace, nodeId));
        var readRequest = CreateReadRequest([readValueId]);

        var response = await session.ReadAsync(readRequest.RequestHeader, readRequest.MaxAge, readRequest.TimestampsToReturn, readRequest.NodesToRead, CancellationToken.None);

        if (response.Results[0].StatusCode != StatusCodes.Good || response.Results[0].Value == null)
        {
            throw new Exception("Failed to read data from OPC server.");
        }

        return response.Results[0].Value.ToString() ?? string.Empty;
    }

    public async Task<Dictionary<string, string>> ReadMultipleDataAsync(string connectionId, int opcNamespace, IEnumerable<string> nodeIds)
    {
        await connectionManagement.CheckConnection(connectionId);

        var session = connectionManagement.GetSession(connectionId);
        var readValueIds = nodeIds.Select(nodeId => CreateReadValueId(OpcUtilities.GetNodeWithNamespace(opcNamespace, nodeId))).ToArray();
        var readRequest = CreateReadRequest(readValueIds);

        var response = await session.ReadAsync(readRequest.RequestHeader, readRequest.MaxAge, readRequest.TimestampsToReturn, readRequest.NodesToRead, CancellationToken.None);

        var result = new Dictionary<string, string>();
        for (int i = 0; i < response.Results.Count; i++)
        {
            var statusCode = response.Results[i].StatusCode;
            var value = response.Results[i].Value?.ToString() ?? string.Empty;
            result[nodeIds.ElementAt(i)] = statusCode == StatusCodes.Good ? value : "Error";
        }

        return result;
    }

    private static ReadValueId CreateReadValueId(string nodeId)
    {
        return new ReadValueId
        {
            NodeId = new NodeId(nodeId),
            AttributeId = Attributes.Value,
        };
    }

    private static ReadRequest CreateReadRequest(ReadValueId[] readValueIds)
    {
        return new ReadRequest
        {
            NodesToRead = readValueIds,
            MaxAge = 0,
            TimestampsToReturn = TimestampsToReturn.Both,
        };
    }
}