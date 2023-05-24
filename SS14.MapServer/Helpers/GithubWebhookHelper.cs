using System.Security.Cryptography;
using System.Text;

namespace SS14.MapServer.Helpers;


/// <summary>
/// 
/// </summary>
/// <remarks>
/// Taken from the following Stackoverflow answer: https://stackoverflow.com/a/67111680
/// </remarks>
public static class GithubWebhookHelper
{
    private const string ShaPrefix = "sha256=";
    
    public static async Task<bool> VerifyWebhook(HttpRequest request, IConfiguration configuration)
    {
        var secret = configuration["Github:AppWebhookSecret"];
        if (string.IsNullOrEmpty(secret))
            throw new Exception("Webkook secret not set");

        var headers = request.Headers;
        
        if (!headers.TryGetValue("X-GitHub-Event", out var eventName)
            || !headers.TryGetValue("X-Hub-Signature-256", out var prefixedSignature)
            || !headers.TryGetValue("X-GitHub-Delivery", out var delivery))
        {
            return false;
        }

        var payload = await RetrievePayload(request);

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        string prefixedSignatureString = prefixedSignature;
        if (prefixedSignatureString.StartsWith(ShaPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var signature = prefixedSignatureString.Substring(ShaPrefix.Length);
            var secretBytes = Encoding.ASCII.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var sha = new HMACSHA256(secretBytes);
            var hash = sha.ComputeHash(payloadBytes);

            var hashString = ToHexString(hash);

            if (hashString.Equals(signature))
                return true;
        }

        return false;
    }
    
    public static string ToHexString(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            builder.AppendFormat("{0:x2}", b);
        }

        return builder.ToString();
    }

    public async static Task<string> RetrievePayload(HttpRequest request)
    {
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}