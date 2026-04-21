using Microsoft.AspNetCore.Mvc;
using OPCGateway.Services.Implementation;
using OPCGateway.Services.ReadWrite;

namespace OPCGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController(IOpcReader opcReader, IOpcWriter opcWriter) : ControllerBase
{
    [HttpGet("read/{connectionId}/{opcNamespace}/{nodeId}")]
    public async Task<IActionResult> ReadData(string connectionId, int opcNamespace, string nodeId)
    {
        var data = await opcReader.ReadDataAsync(connectionId, opcNamespace, nodeId);
        return Ok(new { Data = data });
    }

    [HttpPost("readMultiple/{connectionId}/{opcNamespace}")]
    public async Task<IActionResult> ReadMultipleData(string connectionId, int opcNamespace, [FromBody] IEnumerable<string> nodeIds)
    {
        var data = await opcReader.ReadMultipleDataAsync(connectionId, opcNamespace, nodeIds);
        return Ok(new { Data = data });
    }

    [HttpPost("write/{connectionId}/{opcNamespace}")]
    public async Task<IActionResult> WriteDataAsync(string connectionId, int opcNamespace, [FromBody] WriteDataRequest request)
    {
        if (!Enum.TryParse<OpcType>(request.ValueType, true, out var valueType))
        {
            return BadRequest($"Invalid value type: {request.ValueType}");
        }

        await opcWriter.WriteDataAsync(connectionId, opcNamespace, request.NodeId, request.Value, valueType);
        return Ok("Data written successfully.");
    }

    [HttpPost("writeMultiple/{connectionId}/{opcNamespace}")]
    public async Task<IActionResult> WriteMultipleDataAsync(string connectionId, int opcNamespace, [FromBody] WriteMultipleDataRequest request)
    {
        var nodeValues = request.NodeValues.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                if (!Enum.TryParse<OpcType>(kvp.Value.ValueType, true, out var valueType))
                {
                    throw new ArgumentException($"Invalid value type: {kvp.Value.ValueType}");
                }

                return (kvp.Value.Value, valueType);
            });

        await opcWriter.WriteMultipleDataAsync(connectionId, opcNamespace, nodeValues);
        return Ok("Data written successfully.");
    }
}
