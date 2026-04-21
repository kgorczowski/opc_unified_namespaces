using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using OPCGateway.Services.Monitoring;

namespace OPCGateway.Tests.IntegrationTests;

[NonParallelizable]
[TestFixture]
public class MonitoringServiceIntegrationTests : IntegrationTestBase
{
    private SubscriptionManager _subscriptionManager;
    private MonitoringService _monitoringService;
    private Mock<WebSocket> _webSocketMock;
    private List<string> _capturedMessages;

    [SetUp]
    public async Task SetUp()
    {
        var loggerMock = new Mock<ILogger<IMonitoringService>>();
        _subscriptionManager = new SubscriptionManager();
        _monitoringService = new MonitoringService(_opcConnectionManagement, _subscriptionManager, loggerMock.Object);

        _webSocketMock = new Mock<WebSocket>();
        _capturedMessages = [];

        _webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);

        // Setup WebSocket mock to capture messages
        _webSocketMock.Setup(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            It.IsAny<WebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
                      .Callback<ArraySegment<byte>, WebSocketMessageType, bool, CancellationToken>((buffer, _, _, _) =>
                      {
                          var message = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count);
                          _capturedMessages.Add(message);
                      })
                      .Returns(Task.CompletedTask);

        // Create a new connection for each test
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

    [TearDown]
    public void TearDown()
    {
        _opcConnectionManagement.Disconnect(_connectionId);
    }

    [Test]
    public async Task MonitoringService_ShouldReceiveUpdates_FromMockOpcServer()
    {
        // Arrange
        await _monitoringService.AddClientAsync(_webSocketMock.Object);
        var nodeIds = new List<string> { "SomeDynamicNodeId", "SecondDynamicNodeId" };

        // Act
        await _monitoringService.MonitorParametersAsync(_connectionId, _opcNamespace, nodeIds, 300);

        // Wait for messages to be received
        await Task.Delay(2000);

        // Filter messages that are valid JSON and contain the "NodeId" key
        var receivedNodeIds = _capturedMessages.Select(message =>
        {
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                if (data != null && data.ContainsKey("NodeId"))
                {
                    return data["NodeId"]?.ToString();
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parse error: {ex.Message}");
            }

            return null;
        })
        .Where(nodeId => nodeId != null)
        .ToList();

        // Assert
        Assert.That(receivedNodeIds, Is.Not.Empty, "No valid NodeId messages were received from the MonitoringService.");

        Assert.That(receivedNodeIds, Contains.Item($"ns={_opcNamespace};s=SomeDynamicNodeId"), $"Did not receive updates for SomeDynamicNodeId. Received NodeIds: {string.Join(", ", receivedNodeIds)}");
        Assert.That(receivedNodeIds, Contains.Item($"ns={_opcNamespace};s=SecondDynamicNodeId"), $"Did not receive updates for SecondDynamicNodeId. Received NodeIds: {string.Join(", ", receivedNodeIds)}");
    }

    [Test]
    public async Task MonitoringService_ShouldStopMonitoringParameters_Correctly()
    {
        // Arrange
        await _monitoringService.AddClientAsync(_webSocketMock.Object);
        var nodeIds = new List<string> { "SomeDynamicNodeId", "SecondDynamicNodeId" };
        await _monitoringService.MonitorParametersAsync(_connectionId, _opcNamespace, nodeIds, 300);
        await Task.Delay(1000);

        // Act
        await _monitoringService.StopMonitoringParametersAsync(_connectionId, _opcNamespace, nodeIds);
        _capturedMessages.Clear();

        // Wait for messages to be received
        await Task.Delay(2000);

        // Assert
        Assert.That(_capturedMessages, Is.Empty, "Messages were received after stopping monitoring.");
    }
}
