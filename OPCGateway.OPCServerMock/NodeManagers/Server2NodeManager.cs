using Opc.Ua;
using Opc.Ua.Server;
using OPCGateway.OPCServerMock.MockData;

namespace OPCGateway.OPCServerMock.NodeManagers;

public class Server2NodeManager(IServerInternal server, ApplicationConfiguration configuration) : BaseNodeManager(server, configuration)
{
    protected override FolderState CreateFolder()
    {
        return CreateFolder(null, "Server2", "Server2");
    }

    protected override Dictionary<string, DynamicVariableParameters> GetMockedDynamicValuesDictionary()
    {
        return Server2DynamicReadData.GetOpcParametersMock();
    }

    protected override Dictionary<string, object> GetMockedNonDynamicValuesDictionary()
    {
        return [];
    }
}