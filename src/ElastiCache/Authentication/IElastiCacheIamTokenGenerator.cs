using Amazon.Runtime;
public interface IElastiCacheIamTokenGenerator
{
    Task<string> GenerateAsync(
        ElastiCacheIamOptions options,
        AWSCredentials credentials,
        CancellationToken cancellationToken = default);
}