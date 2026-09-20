using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Signing;

public sealed class ElastiCacheIamTokenGenerator : IElastiCacheIamTokenGenerator
{
    private const string ServiceName = "elasticache";
    private const string Action = "connect";
    private const string ServerlessResourceType = "ServerlessCache";

    public async Task<string> GenerateAsync(
        ElastiCacheIamOptions options,
        AWSCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(credentials);

        options.Validate();

        var query =
            $"Action={Uri.EscapeDataString(Action)}" +
            $"&User={Uri.EscapeDataString(options.UserId)}";

        if (options.IsServerless)
        {
            query +=
                $"&ResourceType={Uri.EscapeDataString(ServerlessResourceType)}";
        }

        // AWS intentionally uses http:// for the SigV4 token request.
        // The actual Redis connection must use TLS.
        var requestUri = new Uri(
            $"http://{options.CacheName}/?{query}");

        var request = new AWSSigningRequest
        {
            HttpMethod = HttpMethod.Get,
            RequestUri = requestUri
        };

        var parameters = new AWSSigV4Parameters
        {
            Credentials = credentials,
            Region = options.Region,
            Service = ServiceName
        };

        var result = await AWSSigV4Signer.PresignAsync(
            request,
            parameters,
            options.TokenLifetime,
            cancellationToken)
            .ConfigureAwait(false);

        return StripScheme(result.Uri);
    }

    private static string StripScheme(Uri uri)
    {
        const string Http = "http://";
        const string Https = "https://";

        var value = uri.ToString();

        if (value.StartsWith(
                Http,
                StringComparison.OrdinalIgnoreCase))
        {
            return value[Http.Length..];
        }

        if (value.StartsWith(
                Https,
                StringComparison.OrdinalIgnoreCase))
        {
            return value[Https.Length..];
        }

        throw new InvalidOperationException(
            $"Unexpected presigned URI scheme: {uri.Scheme}");
    }
}