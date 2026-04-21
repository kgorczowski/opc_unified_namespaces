using System.Text.Json;
using OPCGateway.Services.Implementation;
using OPCGateway.Services.ReadWrite;

namespace OPCGateway.Tests.IntegrationTests;

[NonParallelizable]
[TestFixture]
public class OpcWriterIntegrationTests : IntegrationTestBase
{
    private OpcWriter _opcWriter;
    private OpcReader _opcReader;

    [SetUp]
    public void SetUp()
    {
        _opcWriter = new OpcWriter(_opcConnectionManagement);
        _opcReader = new OpcReader(_opcConnectionManagement);
    }

    [Test]
    public async Task WriteDataAsync_Success()
    {
        // Arrange
        var nodeId = "SomeWriteNodeId";

        var value = JsonDocument.Parse("20.5").RootElement;
        var valueType = OpcType.Float;

        // Act
        await _opcWriter.WriteDataAsync(_connectionId, _opcNamespace, nodeId, value, valueType);

        // Assert
        var readValue = await _opcReader.ReadDataAsync(_connectionId, _opcNamespace, nodeId);
        Assert.That(readValue, Is.EqualTo("20,5"));
    }

    [Test]
    public async Task WriteMultipleDataAsync_Success()
    {
        // Arrange
        var nodeValues = new Dictionary<string, (object Value, OpcType ValueType)>
        {
            { "SomeWriteNodeId", (JsonDocument.Parse("20.9").RootElement, OpcType.Float) },
            { "SecondWriteNodeId", (JsonDocument.Parse("100").RootElement, OpcType.Int32) },
        };

        // Act
        await _opcWriter.WriteMultipleDataAsync(_connectionId, _opcNamespace, nodeValues);

        // Assert
        var readValue1 = await _opcReader.ReadDataAsync(_connectionId, _opcNamespace, "SomeWriteNodeId");
        var readValue2 = await _opcReader.ReadDataAsync(_connectionId, _opcNamespace, "SecondWriteNodeId");
        Assert.That(readValue1, Is.EqualTo("20,9"));
        Assert.That(readValue2, Is.EqualTo("100"));
    }


}
