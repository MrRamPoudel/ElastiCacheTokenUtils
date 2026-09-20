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
        Task<string>>? _handler;

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
        _handler = handler;
    }

    public int CallCount => Volatile.Read(ref _callCount);

    public Task<string> GenerateAsync(
        string cacheName,
        string userId,
        RegionEndpoint region,
        bool isServerless,
        CancellationToken cancellationToken = default)
    {
        var callCount = Interlocked.Increment(ref _callCount);

        return _handler?.Invoke(
            cacheName,
            userId,
            region,
            isServerless,
            cancellationToken)
            ?? Task.FromResult($"token-{callCount}");
    }

    public Task<string> GenerateAsync(ElastiCacheIamOptions options, AWSCredentials credentials, CancellationToken cancellationToken = default)
    {
        return GenerateAsync(
            options.CacheName,
            options.UserId,
            options.Region,
            options.IsServerless,
            cancellationToken);
    }
}
