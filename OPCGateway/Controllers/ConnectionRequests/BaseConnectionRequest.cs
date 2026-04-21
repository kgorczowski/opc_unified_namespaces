namespace OPCGateway.Controllers.ConnectionRequests;

public class BaseConnectionRequest
{
    public required string EndpointUrl { get; set; }

    public SecurityMode? SecurityMode { get; set; }

    public SecurityPolicy? SecurityPolicy { get; set; }
}