using OPCGateway.Services.ReadWrite;

namespace OPCGateway.Tests.IntegrationTests;

[NonParallelizable]
[TestFixture]
public class OpcReaderIntegrationTests : IntegrationTestBase
{
    private OpcReader _opcReader;

    [SetUp]
    public void SetUp()
    {
        _opcReader = new OpcReader(_opcConnectionManagement);
    }

    [Test]
    public async Task ReadDataAsync_Success()
    {
        // Arrange
        var nodeId = "SomeDynamicNodeId";

        // Act
        var result = await _opcReader.ReadDataAsync(_connectionId, _opcNamespace, nodeId);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task ReadMultipleDataAsync_Success()
    {
        // Arrange
        var nodeIds = new List<string> { "SomeDynamicNodeId", "NonExistentNodeId" };

        // Act
        var result = await _opcReader.ReadMultipleDataAsync(_connectionId, _opcNamespace, nodeIds);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result["SomeDynamicNodeId"], Is.Not.Empty);
        Assert.That(result["NonExistentNodeId"], Is.EqualTo("Error"));
    }

    [Test]
    public async Task ReadDataAsync_InvalidNodeId_ThrowsException()
    {
        // Arrange
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
        var opcNamespace = 2;
        var invalidNodeId = "NonExistentNodeId";

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await _opcReader.ReadDataAsync(_connectionId, opcNamespace, invalidNodeId));
    }
}
