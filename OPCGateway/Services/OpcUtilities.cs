using Opc.Ua;
using OPCGateway.Controllers;

namespace OPCGateway.Services;

public static class OpcUtilities
{
    public static string GetNodeWithNamespace(int opcNamespace, string nodeId)
    {
        return $"ns={opcNamespace};s={nodeId}";
    }

    public static string ConvertSecurityPolicy(SecurityPolicy securityPolicy)
    {
        return securityPolicy switch
        {
            SecurityPolicy.None => SecurityPolicies.None,
            SecurityPolicy.Basic128Rsa15 => SecurityPolicies.Basic128Rsa15,
            SecurityPolicy.Basic256 => SecurityPolicies.Basic256,
            SecurityPolicy.Basic256Sha256 => SecurityPolicies.Basic256Sha256,
            SecurityPolicy.Aes128_Sha256_RsaOaep => SecurityPolicies.Aes128_Sha256_RsaOaep,
            SecurityPolicy.Aes256_Sha256_RsaPss => SecurityPolicies.Aes256_Sha256_RsaPss,
            _ => throw new ArgumentException($"Invalid security policy: {securityPolicy}"),
        };
    }

    public static MessageSecurityMode ConvertSecurityMode(SecurityMode securityMode)
    {
        return securityMode switch
        {
            SecurityMode.None => MessageSecurityMode.None,
            SecurityMode.Sign => MessageSecurityMode.Sign,
            SecurityMode.SignAndEncrypt => MessageSecurityMode.SignAndEncrypt,
            _ => throw new ArgumentException($"Invalid security mode: {securityMode}"),
        };
    }
}
