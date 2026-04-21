using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace OPCGateway.Data;

public class PasswordEncryptor
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public PasswordEncryptor(IOptions<EncryptionSettings> options)
    {
        var settings = options.Value;

        if (string.IsNullOrEmpty(settings.Key) || string.IsNullOrEmpty(settings.IV))
        {
            throw new InvalidOperationException("Encryption key and IV must be provided in the configuration.");
        }

        _key = Encoding.UTF8.GetBytes(settings.Key);
        _iv = Encoding.UTF8.GetBytes(settings.IV);

        if (_key.Length != 32 || _iv.Length != 16)
        {
            throw new InvalidOperationException("Invalid key or IV length. Key must be 32 bytes and IV must be 16 bytes.");
        }
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        var buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}