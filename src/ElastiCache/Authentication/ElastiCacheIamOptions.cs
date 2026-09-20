using Amazon;

public sealed record ElastiCacheIamOptions
{
    public required string CacheName { get; init; }

    public required string UserId { get; init; }

    public required RegionEndpoint Region { get; init; }

    /// <summary>
    /// True for ElastiCache Serverless Cache.
    /// False for a replication group / node-based cache.
    /// </summary>
    public bool IsServerless { get; init; }

    /// <summary>
    /// IAM tokens have a maximum lifetime of 15 minutes.
    /// </summary>
    public TimeSpan TokenLifetime { get; init; }
        = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Generate a replacement token this long before expiration.
    /// </summary>
    public TimeSpan RefreshBeforeExpiry { get; init; }
        = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Retry background refreshes using this delay.
    /// </summary>
    public TimeSpan RefreshRetryDelay { get; init; }
        = TimeSpan.FromSeconds(10);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CacheName))
            throw new ArgumentException(
                "CacheName is required.", nameof(CacheName));

        if (string.IsNullOrWhiteSpace(UserId))
            throw new ArgumentException(
                "UserId is required.", nameof(UserId));

        if (TokenLifetime <= TimeSpan.Zero ||
            TokenLifetime > TimeSpan.FromMinutes(15))
        {
            throw new ArgumentOutOfRangeException(
                nameof(TokenLifetime),
                "ElastiCache IAM tokens cannot exceed 15 minutes.");
        }

        if (RefreshBeforeExpiry <= TimeSpan.Zero ||
            RefreshBeforeExpiry >= TokenLifetime)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RefreshBeforeExpiry));
        }
    }
}