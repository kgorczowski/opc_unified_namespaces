using System.Text.Json;
using Moq;
using Opc.Ua;
using OPCGateway.Services.Connections;
using OPCGateway.Services.Implementation;
using OPCGateway.Services.ReadWrite;
using OPCGateway.Tests.Services.Connections;

namespace OPCGateway.Tests.Services.ReadWrite;

[TestFixture]
public class OpcWriterTests
{
    private Mock<IOpcConnectionManagement> _connectionManagementMock;
    private OpcWriter _opcWriter;
    private string _connectionId;
    private int _opcNamespace;
    private string _nodeId;
    private Mock<MockSession> _sessionMock;

    [SetUp]
    public void SetUp()
    {
        _connectionManagementMock = new Mock<IOpcConnectionManagement>();
        _opcWriter = new OpcWriter(_connectionManagementMock.Object);
        _connectionId = Guid.NewGuid().ToString();
        _opcNamespace = 2;
        _nodeId = "TestNodeId";

        _sessionMock = new Mock<MockSession>(OpcSessionFactory.GetOpcConfig(), new ConfiguredEndpoint(null, new EndpointDescription()));
    }

    [Test]
    public async Task WriteDataAsync_Success()
    {
        // Arrange
        var writeResponse = new WriteResponse
        {
            Results = new StatusCodeCollection { StatusCodes.Good },
        };

        _connectionManagementMock.Setup(cm => cm.CheckConnection(_connectionId)).Returns(Task.CompletedTask);
        _connectionManagementMock.Setup(cm => cm.GetSession(_connectionId)).Returns(_sessionMock.Object);
        _sessionMock.Setup(s => s.WriteAsync(It.IsAny<RequestHeader>(), It.IsAny<WriteValueCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(writeResponse);

        // Act
        await _opcWriter.WriteDataAsync(_connectionId, _opcNamespace, _nodeId, JsonDocument.Parse("\"TestValue\"").RootElement, OpcType.String);

        // Assert
        _sessionMock.Verify(s => s.WriteAsync(It.IsAny<RequestHeader>(), It.IsAny<WriteValueCollection>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void WriteDataAsync_Failure()
    {
        // Arrange
        var writeResponse = new WriteResponse
        {
            Results = new StatusCodeCollection { StatusCodes.Bad },
        };

        _connectionManagementMock.Setup(cm => cm.CheckConnection(_connectionId)).Returns(Task.CompletedTask);
        _connectionManagementMock.Setup(cm => cm.GetSession(_connectionId)).Returns(_sessionMock.Object);
        _sessionMock.Setup(s => s.WriteAsync(It.IsAny<RequestHeader>(), It.IsAny<WriteValueCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(writeResponse);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _opcWriter.WriteDataAsync(_connectionId, _opcNamespace, _nodeId, JsonDocument.Parse("\"TestValue\"").RootElement, OpcType.String));
    }

    [Test]
    public async Task WriteMultipleDataAsync_Success()
    {
        // Arrange
        var nodeValues = new Dictionary<string, (object value, OpcType valueType)>
        {
            { "Node1", (JsonDocument.Parse("\"Value1\"").RootElement, OpcType.String) },
            { "Node2", (JsonDocument.Parse("\"Value2\"").RootElement, OpcType.String) },
        };
        var writeResponse = new WriteResponse
        {
            Results = new StatusCodeCollection { StatusCodes.Good, StatusCodes.Good },
        };

        _connectionManagementMock.Setup(cm => cm.CheckConnection(_connectionId)).Returns(Task.CompletedTask);
        _connectionManagementMock.Setup(cm => cm.GetSession(_connectionId)).Returns(_sessionMock.Object);
        _sessionMock.Setup(s => s.WriteAsync(It.IsAny<RequestHeader>(), It.IsAny<WriteValueCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(writeResponse);

        // Act
        await _opcWriter.WriteMultipleDataAsync(_connectionId, _opcNamespace, nodeValues);

        // Assert
        _sessionMock.Verify(s => s.WriteAsync(It.IsAny<RequestHeader>(), It.IsAny<WriteValueCollection>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void WriteMultipleDataAsync_Failure()
    {
        // Arrange
        var nodeValues = new Dictionary<string, (object value, OpcType valueType)>
        {
            { "Node1", (JsonDocument.Parse("\"Value1\"").RootElement, OpcType.String) },
            { "Node2", (JsonDocument.Parse("\"Value2\"").RootElement, OpcType.String) },
        };
        var writeResponse = new WriteResponse
        {
            Results = [StatusCodes.Bad, StatusCodes.Good],
        };

        _connectionManagementMock.Setup(cm => cm.CheckConnection(_connectionId)).Returns(Task.CompletedTask);
        _connectionManagementMock.Setup(cm => cm.GetSession(_connectionId)).Returns(_sessionMock.Object);
        _sessionMock.Setup(s => s.WriteAsync(It.IsAny<RequestHeader>(), It.IsAny<WriteValueCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(writeResponse);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _opcWriter.WriteMultipleDataAsync(_connectionId, _opcNamespace, nodeValues));
    }
}

