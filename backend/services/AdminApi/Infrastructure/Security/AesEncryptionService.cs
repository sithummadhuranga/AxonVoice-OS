using System.Security.Cryptography;
using System.Text;

namespace AdminApi.Infrastructure.Security;

/// <summary>
/// AES-256-CBC encryption service for tenant webhook auth tokens.
/// Tokens are encrypted before DB storage and decrypted on read — never plaintext at rest.
///
/// Security: Key and IV loaded from environment variables. Min key length: 32 bytes (256-bit).
/// FIPS-compliant implementation using System.Security.Cryptography.Aes.
/// </summary>
public sealed class AesEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration config)
    {
        var keyHex = config["Security:AesKey"]
            ?? throw new InvalidOperationException(
                "Security:AesKey not configured. Set AES_ENCRYPTION_KEY env var.");
        var ivHex = config["Security:AesIv"]
            ?? throw new InvalidOperationException(
                "Security:AesIv not configured. Set AES_ENCRYPTION_IV env var.");

        _key = Convert.FromHexString(keyHex);
        _iv = Convert.FromHexString(ivHex);

        if (_key.Length != 32)
            throw new InvalidOperationException(
                $"AES key must be 32 bytes (64 hex chars) for AES-256. Got {_key.Length} bytes.");

        if (_iv.Length != 16)
            throw new InvalidOperationException(
                $"AES IV must be 16 bytes (32 hex chars). Got {_iv.Length} bytes.");
    }

    /// <summary>Encrypts plaintext using AES-256-CBC and returns a Base64-encoded ciphertext.</summary>
    public string Encrypt(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        return Convert.ToBase64String(ciphertextBytes);
    }

    /// <summary>Decrypts a Base64-encoded AES-256-CBC ciphertext back to plaintext.</summary>
    public string Decrypt(string ciphertext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        var plaintextBytes = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
