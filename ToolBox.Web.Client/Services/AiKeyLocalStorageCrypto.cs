using System.Security.Cryptography;
using System.Text;

namespace ToolBox.Web.Client.Services;

/// <summary>
/// Web 端 API Key 本地加密：AES-GCM，密钥由应用盐 + 站点 origin 派生。
/// 防 DevTools 直接读明文；无法防御 XSS 或拿到源码后的逆向。
/// </summary>
internal static class AiKeyLocalStorageCrypto
{
    internal const string EncryptedPrefix = "v1:";

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const string KeyMaterialPrefix = "ToolBox.AiKey.v1|";

    public static string Encrypt(string plaintext, string originContext)
    {
        var key = DeriveKey(originContext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, payload, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, payload, NonceSize + cipherBytes.Length, TagSize);

        return EncryptedPrefix + Convert.ToBase64String(payload);
    }

    public static bool TryDecrypt(string stored, string originContext, out string? plaintext)
    {
        plaintext = null;
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
            return false;

        try
        {
            var payload = Convert.FromBase64String(stored[EncryptedPrefix.Length..]);
            if (payload.Length <= NonceSize + TagSize)
                return false;

            var nonce = payload.AsSpan(0, NonceSize);
            var tag = payload.AsSpan(payload.Length - TagSize, TagSize);
            var cipherBytes = payload.AsSpan(NonceSize, payload.Length - NonceSize - TagSize);

            var plainBytes = new byte[cipherBytes.Length];
            using var aes = new AesGcm(DeriveKey(originContext), TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            plaintext = Encoding.UTF8.GetString(plainBytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] DeriveKey(string originContext) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(KeyMaterialPrefix + originContext));
}
