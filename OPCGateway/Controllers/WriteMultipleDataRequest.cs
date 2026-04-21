namespace OPCGateway.Controllers;

public class WriteMultipleDataRequest
{
    public required Dictionary<string, WriteDataValue> NodeValues { get; set; }
}
