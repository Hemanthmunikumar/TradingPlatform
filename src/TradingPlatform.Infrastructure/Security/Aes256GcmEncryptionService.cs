using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using TradingPlatform.Application.Common;

namespace TradingPlatform.Infrastructure.Security;

/// <summary>
/// Implements authenticated AES-256-GCM encryption with 12-byte nonces and 16-byte authentication tags.
/// </summary>
public class Aes256GcmEncryptionService : ICredentialEncryptionService
{
    private readonly byte[] _key;

    public Aes256GcmEncryptionService(IConfiguration configuration)
    {
        var rawKey = configuration["Security:MasterEncryptionKey"]
            ?? configuration["APP_ENCRYPTION_MASTER_KEY"]
            ?? "TradingPlatformDefaultMasterEncryptionKeyMustBe32Bytes!";

        // Ensure key is strictly 256-bit (32 bytes) via SHA-256 derivation
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize); // 12 bytes
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes
        byte[] cipherBytes = new byte[plainBytes.Length];

        using var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Format: {nonce_b64}:{tag_b64}:{cipher_b64}
        return $"{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(cipherBytes)}";
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        var parts = cipherText.Split(':');
        if (parts.Length != 3)
            throw new CryptographicException("Invalid ciphertext format for AES-256-GCM.");

        byte[] nonce = Convert.FromBase64String(parts[0]);
        byte[] tag = Convert.FromBase64String(parts[1]);
        byte[] cipherBytes = Convert.FromBase64String(parts[2]);
        byte[] plainBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
