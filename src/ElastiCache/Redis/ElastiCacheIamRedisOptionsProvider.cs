using StackExchange.Redis.Configuration;

public sealed class ElastiCacheIamRedisOptionsProvider
    : DefaultOptionsProvider
{
    private readonly IElastiCacheIamCredentialProvider _credentials;

    public ElastiCacheIamRedisOptionsProvider(
        IElastiCacheIamCredentialProvider credentials)
    {
        _credentials = credentials;
    }

    public override string? User =>
        _credentials.UserName;

    public override string? Password =>
        _credentials.Password;
}