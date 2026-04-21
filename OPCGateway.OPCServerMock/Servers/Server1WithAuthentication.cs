using Opc.Ua;
using Opc.Ua.Server;
using OPCGateway.OPCServerMock.NodeManagers;

namespace OPCGateway.OPCServerMock.Servers;

internal class Server1WithAuthentication : BaseServerWithAuthentication
{
    protected override INodeManager CreateCustomNodeManager(
        IServerInternal server,
        ApplicationConfiguration configuration)
    {
        return new Server1NodeManager(server, configuration);
    }

    protected override string GetProductName()
    {
        return "Server Mock";
    }
}