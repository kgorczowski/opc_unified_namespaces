using Moq;
using Opc.Ua;
using OPCGateway.Services.Connections;
using OPCGateway.Services.ReadWrite;
using OPCGateway.Tests.Services.Connections;

namespace OPCGateway.Tests.Services.ReadWrite;

[TestFixture]
public class OpcReaderTests
{
    private Mock<IOpcConnectionManagement> _connectionManagementMock;
    private OpcReader _opcReader;
    private string _connectionId;
    private int _opcNamespace;
    private string _nodeId;
    private Mock<MockSession> _sessionMock;

    [SetUp]
    public void SetUp()
    {
        _connectionManagementMock = new Mock<IOpcConnectionManagement>();
        _opcReader = new OpcReader(_connectionManagementMock.Object);
        _connectionId = Guid.NewGuid().ToString();
        _opcNamespace = 2;
        _nodeId = "TestNodeId";

        _sessionMock = new Mock<MockSession>(OpcSessionFactory.GetOpcConfig(), new ConfiguredEndpoint(null, new EndpointDescription()));
    }

    [Test]
    public async Task ReadDataAsync_Success()
    {
        // Arrange
        var readResponse = new ReadResponse
        {
            Results = new[] { new DataValue { StatusCode = StatusCodes.Good, Value = "TestValue" } },
        };

        _connectionManagementMock.Setup(cm => cm.CheckConnection(_connectionId)).Returns(Task.CompletedTask);
        _connectionManagementMock.Setup(cm => cm.GetSession(_connectionId)).Returns(_sessionMock.Object);
        _sessionMock.Setup(s => s.ReadAsync(It.IsAny<RequestHeader>(), It.IsAny<double>(), It.IsAny<TimestampsToReturn>(), It.IsAny<ReadValueIdCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readResponse);

        // Act
        var result = await _opcReader.ReadDataAsync(_connectionId, _opcNamespace, _nodeId);

        // Assert
        Assert.That(result, Is.EqualTo("TestValue"));
    }

    [Test]
    public void ReadDataAsync_Failure()
    {
        // Arrange
        var readResponse = new ReadResponse
        {
            Results = new[] { new DataValue { StatusCode = StatusCodes.Bad, Value = null } },
        };

        _connectionManagementMock.Setup(cm => cm.CheckConnection(_connectionId)).Returns(Task.CompletedTask);
        _connectionManagementMock.Setup(cm => cm.GetSession(_connectionId)).Returns(_sessionMock.Object);
        _sessionMock.Setup(s => s.ReadAsync(It.IsAny<RequestHeader>(), It.IsAny<double>(), It.IsAny<TimestampsToReturn>(), It.IsAny<ReadValueIdCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readResponse);

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await _opcReader.ReadDataAsync(_connectionId, _opcNamespace, _nodeId));
    }

    [Test]
    public async Task ReadMultipleDataAsync_Success()
    {
        // Arrange
        var nodeIds = new List<string> { "Node1", "Node2" };
        var readResponse = new ReadResponse
        {
            Results = new[]
            {
                new DataValue { StatusCode = StatusCodes.Good, Value = "Value1" },
                new DataValue { StatusCode = StatusCodes.Good, Value = "Value2" },
            },
        };

        _connectionManagementMock.Setup(cm => cm.CheckConnection(_connectionId)).Returns(Task.CompletedTask);
        _connectionManagementMock.Setup(cm => cm.GetSession(_connectionId)).Returns(_sessionMock.Object);
        _sessionMock.Setup(s => s.ReadAsync(It.IsAny<RequestHeader>(), It.IsAny<double>(), It.IsAny<TimestampsToReturn>(), It.IsAny<ReadValueIdCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readResponse);

        // Act
        var result = await _opcReader.ReadMultipleDataAsync(_connectionId, _opcNamespace, nodeIds);

        // Assert
        Assert.That(result["Node1"], Is.EqualTo("Value1"));
        Assert.That(result["Node2"], Is.EqualTo("Value2"));
    }

    [Test]
    public async Task ReadMultipleDataAsync_Failure()
    {
        // Arrange
        var nodeIds = new List<string> { "Node1", "Node2" };
        var readResponse = new ReadResponse
        {
            Results = new[]
            {
                new DataValue { StatusCode = StatusCodes.Bad, Value = null },
                new DataValue { StatusCode = StatusCodes.Good, Value = "Value2" },
            },
        };

        _connectionManagementMock.Setup(cm => cm.CheckConnection(_connectionId)).Returns(Task.CompletedTask);
        _connectionManagementMock.Setup(cm => cm.GetSession(_connectionId)).Returns(_sessionMock.Object);
        _sessionMock.Setup(s => s.ReadAsync(It.IsAny<RequestHeader>(), It.IsAny<double>(), It.IsAny<TimestampsToReturn>(), It.IsAny<ReadValueIdCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readResponse);

        // Act
        var result = await _opcReader.ReadMultipleDataAsync(_connectionId, _opcNamespace, nodeIds);

        // Assert
        Assert.That(result["Node1"], Is.EqualTo("Error"));
        Assert.That(result["Node2"], Is.EqualTo("Value2"));
    }
}
