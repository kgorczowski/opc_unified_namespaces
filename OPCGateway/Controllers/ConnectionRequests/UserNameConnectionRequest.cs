namespace OPCGateway.Controllers.ConnectionRequests;

public class UserNameConnectionRequest : BaseConnectionRequest
{
    public required string Username { get; set; }

    public required string Password { get; set; }
}