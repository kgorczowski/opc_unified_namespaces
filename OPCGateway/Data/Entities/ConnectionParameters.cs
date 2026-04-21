using Opc.Ua;
using OPCGateway.Controllers;

namespace OPCGateway.Data.Entities;

public class ConnectionParameters
{
    public int Id { get; set; }

    public required string ConnectionId { get; set; }

    public required string EndpointUrl { get; set; }

    public required string? Username { get; set; }

    public required string? Password { get; set; }

    public required SecurityMode? SecurityMode { get; set; }

    public required SecurityPolicy? SecurityPolicy { get; set; }

    public required UserTokenType Authentication { get; set; }

    public required string? CertificatePath { get; set; }

    public required string? CertificatePassword { get; set; }
}
