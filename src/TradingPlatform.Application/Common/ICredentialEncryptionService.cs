namespace TradingPlatform.Application.Common;

/// <summary>
/// Authenticated encryption service for securing broker API keys, secrets, and OAuth tokens at rest.
/// </summary>
public interface ICredentialEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
