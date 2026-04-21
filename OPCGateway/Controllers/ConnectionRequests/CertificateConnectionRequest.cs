namespace OPCGateway.Controllers.ConnectionRequests;

public class CertificateConnectionRequest : BaseConnectionRequest
{
    public required string CertificatePath { get; set; }

    public string? CertificatePassword { get; set; }
}