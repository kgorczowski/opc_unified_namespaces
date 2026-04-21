using Microsoft.Extensions.Logging;
using Moq;
using Opc.Ua;
using Opc.Ua.Client;
using OPCGateway.Controllers;
using OPCGateway.Data.Entities;
using OPCGateway.Data.Repositories;
using OPCGateway.Services.Connections;

namespace OPCGateway.Tests.Services.Connections;

[TestFixture]
public class OpcConnectionManagementTests
{
    private Mock<IConnectionRepository> _repositoryMock;
    private Mock<IOpcSessionManager> _sessionManagerMock;
    private OpcConnectionManagement _opcConnectionManagement;
    private string _connectionId;
    private string _endpointUrl;
    private string _username;
    private string _password;
    private SecurityMode _securityMode;
    private SecurityPolicy _securityPolicy;
    private UserTokenType _authentication;
    private string? _certificatePath;
    private string? _certificatePassword;

    [SetUp]
    public void SetUp()
    {
        var loggerMock = new Mock<ILogger<IOpcConnectionManagement>>();
        _repositoryMock = new Mock<IConnectionRepository>();
        _sessionManagerMock = new Mock<IOpcSessionManager>();
        var opcSessionFactory = new OpcSessionFactory();
        _opcConnectionManagement = new OpcConnectionManagement(_repositoryMock.Object, _sessionManagerMock.Object, opcSessionFactory, loggerMock.Object);

        // Mock the static Session.Create method
        SessionFactory.SetCreateSessionFunc((config, endpoint, updateBeforeConnect, sessionName, sessionTimeout, identity, preferredLocales) =>
        {
            return Task.FromResult<Session>(new MockSession(config, endpoint));
        });

        // Initialize common test data
        _connectionId = Guid.NewGuid().ToString();
        _endpointUrl = "opc.tcp://localhost:4840";
        _username = "user";
        _password = "password";
        _securityMode = SecurityMode.Sign;
        _securityPolicy = SecurityPolicy.Basic128Rsa15;
        _authentication = UserTokenType.UserName;
        _certificatePath = null;
        _certificatePassword = null;
    }

    [TearDown]
    public void TearDown()
    {
        // Reset the static Session.Create method to its default behavior
        SessionFactory.ResetCreateSessionFunc();
    }

    [Test]
    public async Task ConnectAsync_SuccessfulConnection_AddsSession()
    {
        // Arrange
        _repositoryMock.Setup(r => r.LoadConnectionParametersAsync(It.IsAny<string>()))
            .ReturnsAsync((ConnectionParameters?)null);

        // Act
        var result = await Connect();

        // Assert
        _sessionManagerMock.Verify(s => s.AddSession(_connectionId, It.IsAny<Session>(), It.IsAny<ConnectionParameters>()), Times.Once);
        Assert.That(result, Is.EqualTo(_connectionId));
    }

    [Test]
    public void ConnectAsync_AuthenticationFailure_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.LoadConnectionParametersAsync(It.IsAny<string>()))
            .ReturnsAsync((ConnectionParameters?)null);

        // Simulate authentication failure
        _sessionManagerMock.Setup(s => s.AddSession(It.IsAny<string>(), It.IsAny<Session>(), It.IsAny<ConnectionParameters>()))
            .Throws(new ServiceResultException(StatusCodes.BadUserAccessDenied));

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(Connect);
    }

    [Test]
    public void ConnectAsync_ConnectionFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.LoadConnectionParametersAsync(It.IsAny<string>()))
            .ReturnsAsync((ConnectionParameters?)null);

        // Simulate connection failure
        _sessionManagerMock.Setup(s => s.AddSession(It.IsAny<string>(), It.IsAny<Session>(), It.IsAny<ConnectionParameters>()))
            .Throws(new ServiceResultException(StatusCodes.BadNotConnected));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(Connect);
    }

    [Test]
    public void Disconnect_SuccessfulDisconnection_RemovesSession()
    {
        // Arrange
        var sessionMock = new Mock<MockSession>(OpcSessionFactory.GetOpcConfig(), new ConfiguredEndpoint(null, new EndpointDescription()));

        _sessionManagerMock.Setup(s => s.GetConnectionStatus(_connectionId))
            .Returns(ConnectionStatus.Connected);
        _sessionManagerMock.Setup(s => s.GetSession(_connectionId))
            .Returns(sessionMock.Object);

        // Act
        _opcConnectionManagement.Disconnect(_connectionId);

        // Assert
        sessionMock.Verify(s => s.Close(), Times.Once);
        _sessionManagerMock.Verify(s => s.RemoveSession(_connectionId), Times.Once);
    }

    [Test]
    public async Task CheckConnection_AlreadyConnected_NoActionTaken()
    {
        // Arrange
        _sessionManagerMock.Setup(s => s.GetConnectionStatus(_connectionId))
            .Returns(ConnectionStatus.Connected);

        // Act
        await _opcConnectionManagement.CheckConnection(_connectionId);

        // Assert
        _sessionManagerMock.Verify(s => s.GetSession(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task CheckConnection_ReconnectsIfNotConnected()
    {
        // Arrange
        _sessionManagerMock.Setup(s => s.GetConnectionStatus(_connectionId))
            .Returns(ConnectionStatus.NotConnected);

        SetupRepositoryWithExistingConnection();

        // Act
        await _opcConnectionManagement.CheckConnection(_connectionId);

        // Assert
        _sessionManagerMock.Verify(s => s.AddSession(_connectionId, It.IsAny<Session>(), It.IsAny<ConnectionParameters>()), Times.Once);
    }

    [Test]
    public async Task ReconnectAsync_SuccessfulReconnection()
    {
        // Arrange
        SetupRepositoryWithExistingConnection();

        _sessionManagerMock.Setup(s => s.GetConnectionStatus(_connectionId))
            .Returns(ConnectionStatus.NotConnected);

        // Act
        var result = await _opcConnectionManagement.ReconnectAsync(_connectionId);

        // Assert
        _sessionManagerMock.Verify(s => s.AddSession(_connectionId, It.IsAny<Session>(), It.IsAny<ConnectionParameters>()), Times.Once);
        Assert.That(result, Is.EqualTo(_connectionId));
    }

    [Test]
    public void ReconnectAsync_ConnectionIdNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.LoadConnectionParametersAsync(It.IsAny<string>()))
            .ReturnsAsync((ConnectionParameters?)null);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _opcConnectionManagement.ReconnectAsync(_connectionId));
    }

    private async Task<string> Connect()
    {
        return await _opcConnectionManagement.ConnectAsync(
                        _endpointUrl,
                        _username,
                        _password,
                        _connectionId,
                        _securityMode,
                        _securityPolicy,
                        _authentication,
                        _certificatePath,
                        _certificatePassword);
    }

    private void SetupRepositoryWithExistingConnection()
    {
        var connectionParameters = new ConnectionParameters
        {
            ConnectionId = _connectionId,
            EndpointUrl = _endpointUrl,
            Username = _username,
            Password = _password,
            SecurityMode = _securityMode,
            SecurityPolicy = _securityPolicy,
            Authentication = _authentication,
            CertificatePath = _certificatePath,
            CertificatePassword = _certificatePassword,
        };

        _repositoryMock.Setup(r => r.LoadConnectionParametersAsync(It.IsAny<string>()))
            .ReturnsAsync(connectionParameters);
    }
}
