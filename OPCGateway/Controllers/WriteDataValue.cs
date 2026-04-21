namespace OPCGateway.Controllers;

public class WriteDataValue
{
    public required string ValueType { get; set; }

    public required object Value { get; set; }
}
