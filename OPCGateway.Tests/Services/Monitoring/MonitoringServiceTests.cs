using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Moq;
using Opc.Ua;
using Opc.Ua.Client;
using OPCGateway.Services.Connections;
using OPCGateway.Services.Monitoring;
using OPCGateway.Tests.Services.Connections;

namespace OPCGateway.Tests.Services.Monitoring;

[TestFixture]
public class MonitoringServiceTests
{
    private Mock<IOpcConnectionManagement> _connectionManagementMock;
    private Mock<ISubscriptionManager> _subscriptionManagerMock;
    private MonitoringService _monitoringService;
    private string _connectionId;
    private int _opcNamespace;
    private List<string> _nodeIds;
    private Mock<MockSession> _sessionMock;
    private Mock<WebSocket> _webSocketMock;

    [SetUp]
    public void SetUp()
    {
        var loggerMock = new Mock<ILogger<IMonitoringService>>();
        _connectionManagementMock = new Mock<IOpcConnectionManagement>();
        _subscriptionManagerMock = new Mock<ISubscriptionManager>();
        _monitoringService = new MonitoringService(_connectionManagementMock.Object, _subscriptionManagerMock.Object, loggerMock.Object);
        _connectionId = Guid.NewGuid().ToString();
        _opcNamespace = 2;
        _nodeIds = ["Node1", "Node2"];
        _sessionMock = new Mock<MockSession>(OpcSessionFactory.GetOpcConfig(), new ConfiguredEndpoint(null, new EndpointDescription()));
        _webSocketMock = new Mock<WebSocket>();

        var readResponse = new ReadResponse
        {
            Results = new[] { new DataValue { StatusCode = StatusCodes.Good, Value = "TestValue" } },
        };

        _sessionMock.Setup(s => s.ReadAsync(It.IsAny<RequestHeader>(), It.IsAny<double>(), It.IsAny<TimestampsToReturn>(), It.IsAny<ReadValueIdCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readResponse);
    }

    [Test]
    public async Task AddClientAsync_Success()
    {
        // Act
        await _monitoringService.AddClientAsync(_webSocketMock.Object);

        // Assert
        Assert.That(_monitoringService.Clients.ContainsKey(_webSocketMock.Object), Is.True, "Client was not added.");
        _webSocketMock.Verify(ws => ws.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce, "Initial data was not sent.");
    }

    [Test]
    public async Task MonitorParametersAsync_Success()
    {
        // Arrange
        var subscriptionMock = new Mock<Subscription>();
        _connectionManagementMock.Setup(cm => cm.CheckConnection(_connectionId)).Returns(Task.CompletedTask);
        _connectionManagementMock.Setup(cm => cm.GetSession(_connectionId)).Returns(_sessionMock.Object);
        _subscriptionManagerMock.Setup(sm => sm.GetOrCreateSubscription(_connectionId, _sessionMock.Object, It.IsAny<int>())).Returns(subscriptionMock.Object);
        _subscriptionManagerMock.Setup(sm => sm.AddSubscriptionAsync(_connectionId, _sessionMock.Object, subscriptionMock.Object)).Returns(Task.CompletedTask);

        // Act
        await _monitoringService.MonitorParametersAsync(_connectionId, _opcNamespace, _nodeIds, 1000);

        // Assert
        _subscriptionManagerMock.Verify(sm => sm.GetOrCreateSubscription(_connectionId, _sessionMock.Object, 1000), Times.Once, "Subscription was not created.");
        _subscriptionManagerMock.Verify(sm => sm.AddSubscriptionAsync(_connectionId, _sessionMock.Object, subscriptionMock.Object), Times.Once, "Subscription was not added.");
    }
}