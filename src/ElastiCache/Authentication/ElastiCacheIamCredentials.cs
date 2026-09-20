public sealed record ElastiCacheIamCredentials(
    string UserName,
    string Password,
    DateTimeOffset ExpiresAt);