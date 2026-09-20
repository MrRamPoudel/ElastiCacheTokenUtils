using Amazon;
using Amazon.Runtime;

namespace ElastiCache.Tests.Authentication;

internal sealed class TestTokenGenerator : IElastiCacheIamTokenGenerator
{
    private readonly Func<
        string,
        string,
        RegionEndpoint,
        bool,
        CancellationToken,
        Task<string>> _handler;

    private int _callCount;

    public TestTokenGenerator(
        Func<
            string,
            string,
            RegionEndpoint,
            bool,
            CancellationToken,
            Task<string>>? handler = null)
    {
        _handler = handler ??
            ((_, _, _, _, _) =>
                Task.FromResult($"token-{Interlocked.Increment(ref _callCount)}"));
    }

    public int CallCount => Volatile.Read(ref _callCount);

    public Task<string> GenerateAsync(
        string cacheName,
        string userId,
        RegionEndpoint region,
        bool isServerless,
        CancellationToken cancellationToken = default)
    {
        return _handler(
            cacheName,
            userId,
            region,
            isServerless,
            cancellationToken);
    }

    public Task<string> GenerateAsync(ElastiCacheIamOptions options, AWSCredentials credentials, CancellationToken cancellationToken = default)
    {
        return _handler(
            options.CacheName,
            options.UserId,
            options.Region,
            options.IsServerless,
            cancellationToken);
    }
}
