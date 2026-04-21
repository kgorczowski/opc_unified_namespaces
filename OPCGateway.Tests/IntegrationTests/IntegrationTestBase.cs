using Microsoft.Extensions.Logging;
using Moq;
using Opc.Ua;
using OPCGateway.Controllers;
using OPCGateway.Data.Repositories;
using OPCGateway.OPCServerMock;
using OPCGateway.Services.Connections;
using OPCGateway.Services.Monitoring;

namespace OPCGateway.Tests.IntegrationTests;

public abstract class IntegrationTestBase
{
    protected static MockOpcServer _mockOpcServer;
    protected static string _endpointUrl = "opc.tcp://localhost:4841";
    protected static string _username = "user";
    protected static string _password = "password";
    protected static SecurityMode _securityMode = SecurityMode.Sign;
    protected static SecurityPolicy _securityPolicy = SecurityPolicy.Basic128Rsa15;
    protected static UserTokenType _authentication = UserTokenType.UserName;
    protected static string? _certificatePath = null;
    protected static string? _certificatePassword = null;

    protected IConnectionRepository _repository;
    protected OpcSessionManager _sessionManager;
    protected OpcConnectionManagement _opcConnectionManagement;
    protected string _connectionId;
    protected int _opcNamespace = 2;

    [OneTimeSetUp]
    public async Task BaseOneTimeSetUp()
    {
        var loggerMock = new Mock<ILogger<IOpcSessionManager>>();
        var loggerMock2 = new Mock<ILogger<IOpcConnectionManagement>>();

        _repository = new InMemoryConnectionRepository();
        var opcSessionFactory = new OpcSessionFactory();
        var subscriptionManagerMock = new Mock<ISubscriptionManager>();
        _sessionManager = new OpcSessionManager(subscriptionManagerMock.Object, loggerMock.Object);
        _opcConnectionManagement = new OpcConnectionManagement(_repository, _sessionManager, opcSessionFactory, loggerMock2.Object);

        _mockOpcServer = new MockOpcServer();
        await _mockOpcServer.StartAsync();

        _connectionId = await _opcConnectionManagement.ConnectAsync(
            _endpointUrl,
            _username,
            _password,
            null,
            _securityMode,
            _securityPolicy,
            _authentication,
            _certificatePath,
            _certificatePassword);
    }

    [OneTimeTearDown]
    public void BaseOneTimeTearDown()
    {
        // Disconnect the session if it was created
        if (!string.IsNullOrEmpty(_connectionId))
        {
            _opcConnectionManagement.Disconnect(_connectionId);
        }

        _mockOpcServer.Stop();
    }
}
