using Microsoft.Extensions.Logging;
using Moq;
using Opc.Ua;
using OPCGateway.Controllers;
using OPCGateway.Data.Repositories;
using OPCGateway.OPCServerMock;
using OPCGateway.Services.Connections;
using OPCGateway.Services.Monitoring;

namespace OPCGateway.Tests.IntegrationTests;

[NonParallelizable]
[TestFixture]
public class OpcConnectionManagementIntegrationTests
{
    private IConnectionRepository _repository;
    private OpcSessionManager _sessionManager;
    private OpcConnectionManagement _opcConnectionManagement;
    private MockOpcServer _mockOpcServer;
    private string _endpointUrl;
    private string _username;
    private string _password;
    private string _connectionId;
    private SecurityMode _securityMode;
    private SecurityPolicy _securityPolicy;
    private UserTokenType _authentication;
    private string? _certificatePath;
    private string? _certificatePassword;

    [SetUp]
    public async Task SetUp()
    {
        var loggerMock = new Mock<ILogger<IOpcSessionManager>>();
        var loggerMock2 = new Mock<ILogger<IOpcConnectionManagement>>();

        _repository = new InMemoryConnectionRepository();
        var opcSessionFactory = new OpcSessionFactory();
        var subscriptionManagerMock = new Mock<ISubscriptionManager>();
        _sessionManager = new OpcSessionManager(subscriptionManagerMock.Object, loggerMock.Object);
        _opcConnectionManagement = new OpcConnectionManagement(_repository, _sessionManager, opcSessionFactory, loggerMock2.Object);
        _mockOpcServer = new MockOpcServer();

        _endpointUrl = "opc.tcp://localhost:4841"; // Use the mock server's endpoint
        _username = "user";
        _password = "password";
        _securityMode = SecurityMode.Sign;
        _securityPolicy = SecurityPolicy.Basic128Rsa15;
        _authentication = UserTokenType.UserName;
        _certificatePath = null;
        _certificatePassword = null;

        // Start the mock OPC server
        await _mockOpcServer.StartAsync();
    }

    [TearDown]
    public void TearDown()
    {
        // Disconnect the session if it was created
        if (!string.IsNullOrEmpty(_connectionId))
        {
            _opcConnectionManagement.Disconnect(_connectionId);
            _connectionId = null;
        }

        // Stop the mock OPC server
        _mockOpcServer.Stop();
    }

    [Test]
    public async Task ConnectAndDisconnect_SuccessfulLifecycle()
    {
        // Arrange

        // Act
        _connectionId = await ConnectWithUsername();
        var session = _opcConnectionManagement.GetSession(_connectionId);

        // Assert
        Assert.NotNull(session);
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.Connected));

        // Disconnect
        _opcConnectionManagement.Disconnect(_connectionId);
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.NotConnected));
    }

    [Test]
    public async Task ServerStopped_ClientDetectsDisconnection()
    {
        // Arrange

        // Act
        _connectionId = await ConnectWithUsername();
        var session = _opcConnectionManagement.GetSession(_connectionId);

        // Stop the mock OPC server
        _mockOpcServer.Stop();

        // Wait a moment to allow the client to detect the disconnection
        await Task.Delay(2500);

        // Assert that the client detects the disconnection
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.Reconnecting));
    }

    [Test]
    public async Task Reconnect_AfterServerRestart_Successful()
    {
        // Arrange
        _connectionId = await ConnectWithUsername();
        var session = _opcConnectionManagement.GetSession(_connectionId);

        // Stop the mock OPC server to simulate disconnection
        _mockOpcServer.Stop();

        await Task.Delay(2500);

        // Restart the mock OPC server to allow reconnection
        await _mockOpcServer.StartAsync();

        await Task.Delay(5000);

        // Assert
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.Connected));
    }

    [Test]
    public async Task ConnectWithAnonymousAuthentication_Successful()
    {
        // Arrange
        _authentication = UserTokenType.Anonymous;

        // Act
        _connectionId = await _opcConnectionManagement.ConnectAsync(
            _endpointUrl,
            null,
            null,
            null,
            _securityMode,
            _securityPolicy,
            _authentication,
            _certificatePath,
            _certificatePassword);
        var session = _opcConnectionManagement.GetSession(_connectionId);

        // Assert
        Assert.NotNull(session);
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.Connected));

        // Disconnect
        _opcConnectionManagement.Disconnect(_connectionId);
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.NotConnected));
    }

    [Test]
    public async Task ConnectWithCertificateAuthentication_Successful()
    {
        // Arrange
        _authentication = UserTokenType.Certificate;

        // Act
        _connectionId = await _opcConnectionManagement.ConnectAsync(
            _endpointUrl,
            null,
            null,
            null,
            _securityMode,
            _securityPolicy,
            _authentication,
            null,
            null);
        var session = _opcConnectionManagement.GetSession(_connectionId);

        // Assert
        Assert.NotNull(session);
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.Connected));

        // Disconnect
        _opcConnectionManagement.Disconnect(_connectionId);
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.NotConnected));
    }

    [Test]
    public async Task ConnectWithAutoSecuritySettings_Successful()
    {
        // Arrange
        _authentication = UserTokenType.Anonymous;

        // Act
        _connectionId = await _opcConnectionManagement.ConnectAsync(
            _endpointUrl,
            null,
            null,
            null,
            null,
            null,
            _authentication,
            _certificatePath,
            _certificatePassword);
        var session = _opcConnectionManagement.GetSession(_connectionId);

        // Assert
        Assert.NotNull(session);
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.Connected));

        // Disconnect
        _opcConnectionManagement.Disconnect(_connectionId);
        Assert.That(_opcConnectionManagement.GetConnectionStatus(_connectionId), Is.EqualTo(ConnectionStatus.NotConnected));
    }

    private async Task<string> ConnectWithUsername()
    {
        return await _opcConnectionManagement.ConnectAsync(
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
}
