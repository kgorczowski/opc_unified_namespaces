namespace OPCGateway.Controllers;

public enum SecurityPolicy
{
    Auto,
    None,
    Basic128Rsa15,
    Basic256,
    Basic256Sha256,
    Aes128_Sha256_RsaOaep,
    Aes256_Sha256_RsaPss,
}