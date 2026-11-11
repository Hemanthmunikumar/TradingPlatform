using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TradingPlatform.Infrastructure.Security;

namespace TradingPlatform.Tests.Security;

public class Aes256GcmEncryptionTests
{
    private readonly Aes256GcmEncryptionService _encryptionService;

    public Aes256GcmEncryptionTests()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Security:MasterEncryptionKey"] = "SuperSecretMasterKeyForTestingAesGcmEncryption123!"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        _encryptionService = new Aes256GcmEncryptionService(config);
    }

    [Fact]
    public void Encrypt_ShouldProduceThreePartFormattedString()
    {
        // Act
        var encrypted = _encryptionService.Encrypt("my_broker_secret_token_xyz");

        // Assert
        encrypted.Should().NotBeNullOrWhiteSpace();
        var parts = encrypted.Split(':');
        parts.Should().HaveCount(3, "Format must be {nonce_b64}:{tag_b64}:{ciphertext_b64}");
    }

    [Fact]
    public void Encrypt_And_Decrypt_ShouldRoundTripAccurately()
    {
        // Arrange
        const string originalSecret = "zerodha_api_key_778899:secret_123456";

        // Act
        var cipher = _encryptionService.Encrypt(originalSecret);
        var decrypted = _encryptionService.Decrypt(cipher);

        // Assert
        decrypted.Should().Be(originalSecret);
    }

    [Fact]
    public void Encrypt_SameTextTwice_ShouldProduceDifferentNoncesAndCiphertext()
    {
        // Arrange
        const string secret = "upstox_oauth_refresh_token_abc";

        // Act
        var cipher1 = _encryptionService.Encrypt(secret);
        var cipher2 = _encryptionService.Encrypt(secret);

        // Assert
        cipher1.Should().NotBe(cipher2, "Each encryption must use a unique cryptographically random 12-byte nonce");
        _encryptionService.Decrypt(cipher1).Should().Be(secret);
        _encryptionService.Decrypt(cipher2).Should().Be(secret);
    }

    [Fact]
    public void Decrypt_WithTamperedTag_ShouldThrowCryptographicException()
    {
        // Arrange
        var cipher = _encryptionService.Encrypt("sensitive_api_secret");
        var parts = cipher.Split(':');

        // Corrupt the tag byte
        byte[] tag = Convert.FromBase64String(parts[1]);
        tag[0] ^= 0xFF; // flip bits
        var tamperedCipher = $"{parts[0]}:{Convert.ToBase64String(tag)}:{parts[2]}";

        // Act & Assert
        var act = () => _encryptionService.Decrypt(tamperedCipher);
        act.Should().Throw<CryptographicException>("GCM authentication tag mismatch must be detected and rejected");
    }

    [Fact]
    public void Encrypt_EmptyString_ShouldReturnEmptyString()
    {
        _encryptionService.Encrypt(string.Empty).Should().BeEmpty();
        _encryptionService.Decrypt(string.Empty).Should().BeEmpty();
    }
}
