using Opc.Ua;
using Opc.Ua.Server;
using OPCGateway.OPCServerMock.MockData;

namespace OPCGateway.OPCServerMock.NodeManagers;

internal class Server1NodeManager(IServerInternal server, ApplicationConfiguration configuration) : BaseNodeManager(server, configuration)
{
    protected override FolderState CreateFolder()
    {
        return CreateFolder(null, "Server1", "Server1");
    }

    protected override Dictionary<string, DynamicVariableParameters> GetMockedDynamicValuesDictionary()
    {
        return NodesParser.GetCreatedDynamicNodes();
    }

    protected override Dictionary<string, object> GetMockedNonDynamicValuesDictionary()
    {
        var parsedNodes = NodesParser.GetCreatedStaticNodes();
        return parsedNodes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
