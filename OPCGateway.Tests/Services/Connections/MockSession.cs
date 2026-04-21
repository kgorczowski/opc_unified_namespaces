using Moq;
using Opc.Ua;
using Opc.Ua.Client;

namespace OPCGateway.Tests.Services.Connections;

public class MockSession(ApplicationConfiguration configuration, ConfiguredEndpoint endpoint) :
    Session(new Mock<ITransportChannel>().Object, configuration, endpoint, null)
{
}