using StackExchange.Redis;

public sealed class RedisConnectionFactory
{
    private readonly IElastiCacheIamCredentialProvider _credentials;
    private readonly ElastiCacheIamRedisOptionsProvider _defaults;

    public RedisConnectionFactory(
        IElastiCacheIamCredentialProvider credentials)
    {
        _credentials = credentials;
        _defaults =
            new ElastiCacheIamRedisOptionsProvider(
                credentials);
    }

    public async Task<IConnectionMultiplexer> CreateAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        await _credentials.StartAsync(cancellationToken);

        var options = new ConfigurationOptions
        {
            Ssl = true,
            AbortOnConnectFail = false,
            Defaults = _defaults
        };

        options.EndPoints.Add(endpoint);

        return await ConnectionMultiplexer
            .ConnectAsync(options)
            .ConfigureAwait(false);
    }
}