namespace OPCGateway.Controllers;

public class WriteDataRequest
{
    public required string NodeId { get; set; }

    public required object Value { get; set; }

    public required string ValueType { get; set; }
}
