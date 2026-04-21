using Opc.Ua;
using Opc.Ua.Server;
using OPCGateway.OPCServerMock.NodeManagers;

namespace OPCGateway.OPCServerMock.Servers;

internal class Server2WithAuthentication : BaseServerWithAuthentication
{
    protected override INodeManager CreateCustomNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        return new Server2NodeManager(server, configuration);
    }

    protected override string GetProductName()
    {
        return "Server 2 Mock";
    }
}
