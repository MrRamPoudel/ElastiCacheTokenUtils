using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Signing;

namespace ElastiCache.IAMElastiCacheTokenGenerator
{
    public class IAMElastiCacheTokenGenerator
    {
        private const string Action = "connect";
        private const string ServiceName = "elasticache";
        private const string UserParameter = "User";
        private static readonly TimeSpan TokenExpiry = TimeSpan.FromMinutes(15);

        private readonly AWSCredentials _credentials;
        private readonly RegionEndpoint _region;

    public IAMElastiCacheTokenGenerator(
        AWSCredentials credentials,
        RegionEndpoint region)
    {
        _credentials = credentials;
        _region = region;
    } 
    public async Task<string> GenerateTokenAsync(
        string cacheName,
        string userId,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        var uri = new Uri(
            $"http://{cacheName}/" +
            $"?Action={Uri.EscapeDataString(Action)}" +
            $"&User={Uri.EscapeDataString(userId)}");

        var request = new AWSSigningRequest
        {
            HttpMethod = HttpMethod.Get,
            RequestUri = uri
        };

        var parameters = new AWSSigV4Parameters
        {
            Credentials = _credentials,
            Region = _region,
            Service = ServiceName
        };

        var result = await AWSSigV4Signer.PresignAsync(
            request,
            parameters,
            lifetime,
            cancellationToken);

        var token = result.Uri.ToString();

        if (token.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            token = token["http://".Length..];
        }
        else if (token.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            token = token["https://".Length..];
        }

        return token;
    }
    }
}