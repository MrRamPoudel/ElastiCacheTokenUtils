using Amazon;
using Amazon.Runtime;
using ElastiCache.IAMElastiCacheTokenGenerator;
public sealed class RedisIAMCredentialProvider
{
    private readonly string _cacheName;
    private readonly string _userId;
    private readonly RegionEndpoint _region;
    private readonly AWSCredentials _credentials;

    private string? _token;
    private DateTimeOffset _tokenExpiresAt;

    public RedisIAMCredentialProvider(
        string cacheName,
        string userId,
        RegionEndpoint region,
        AWSCredentials credentials)
    {
        _cacheName = cacheName;
        _userId = userId;
        _region = region;
        _credentials = credentials;
    }

    public async Task<string> GetToken()
    {
        if (_token == null ||
            DateTimeOffset.UtcNow >= _tokenExpiresAt)
        {
            _token = await IAMElastiCacheTokenGenerator.GenerateTokenAsync(
                _cacheName,
                _userId,
                _region,
                _credentials);

            _tokenExpiresAt =
                DateTimeOffset.UtcNow.AddMinutes(15);
        }

        return _token;
    }
}