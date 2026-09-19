using ElastiCache.IAMElastiCacheTokenGenerator;
using StackExchange.Redis.Configuration;

public sealed class ElastiCacheIamOptionsProvider : DefaultOptionsProvider
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    // Refresh ahead of time to gurantee unexpired token
    private static readonly TimeSpan RefreshAfter = TimeSpan.FromMinutes(13);

    private readonly IAMElastiCacheTokenGenerator _tokenGenerator;
    private readonly string _cacheName;
    private readonly string _userId;

    private readonly object _sync = new();
    private string? _token;
    private DateTimeOffset _refreshAt;

    public ElastiCacheIamOptionsProvider(string cacheName, string userId, IAMElastiCacheTokenGenerator tokenGenerator) {

        _cacheName = cacheName;
        _userId = userId;
        _tokenGenerator = tokenGenerator;    
    }

    public override string User => _userId;

    public override string? Password
    {
        get
        {
            return GetToken();
        }
    }

    private string GetToken()
    {
        lock (_sync)
        {
            if (_token is not null && DateTimeOffset.UtcNow < _refreshAt)
            {
                return _token;
            }

            _token = _tokenGenerator.GenerateTokenAsync(
                    _cacheName,
                    _userId,
                    TokenLifetime)
                .GetAwaiter()
                .GetResult();

            _refreshAt =
                DateTimeOffset.UtcNow.Add(RefreshAfter);

            return _token;
        }
    }
}