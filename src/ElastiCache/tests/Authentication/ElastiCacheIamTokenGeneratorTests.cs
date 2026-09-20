using Amazon;
using Amazon.Runtime;
using System.Net;
using Xunit;

namespace ElastiCache.Tests.Authentication;

public sealed class ElastiCacheIamTokenGeneratorTests
{
    private  BasicAWSCredentials credentials = new BasicAWSCredentials(
        "AKIDEXAMPLE",
        "secret");

    
    private ElastiCacheIamOptions options = new ElastiCacheIamOptions{
        CacheName = "my-cache",
        UserId= "my-user",
        Region= RegionEndpoint.USEast1,
        IsServerless = false
    };
    [Fact]
    public async Task GenerateAsync_ReturnsPresignedToken()
    {

        var generator = new ElastiCacheIamTokenGenerator();

        var token = await generator.GenerateAsync(options, credentials);

        Assert.False(string.IsNullOrWhiteSpace(token));

        // The Redis password is the URI without the scheme.
        Assert.DoesNotContain("http://", token);
        Assert.DoesNotContain("https://", token);

        Assert.Contains("my-cache/", token);
        Assert.Contains("Action=connect", token);
        Assert.Contains("User=my-user", token);
        Assert.Contains("X-Amz-", token);
    }

    [Fact]
    public async Task GenerateAsync_Serverless_IncludesResourceType()
    {

        var generator = new ElastiCacheIamTokenGenerator();

        var serverlessOptions = new ElastiCacheIamOptions{CacheName="my-serverless-cache", UserId="my-user", Region=RegionEndpoint.USEast1, IsServerless=true};
        var token = await generator.GenerateAsync(
            serverlessOptions, credentials);

        Assert.Contains(
            "ResourceType=ServerlessCache",
            token);
    }

    [Fact]
    public async Task GenerateAsync_NonServerless_DoesNotIncludeResourceType()
    {
        var credentials = new BasicAWSCredentials(
            "AKIDEXAMPLE",
            "secret");

        var generator = new ElastiCacheIamTokenGenerator();

        var token = await generator.GenerateAsync(new ElastiCacheIamOptions{
            CacheName= "my-replication-group",
            UserId= "my-user",
            Region= RegionEndpoint.USEast1,
            IsServerless= false}, credentials);

        Assert.DoesNotContain(
            "ResourceType=ServerlessCache",
            token);
    }

    [Fact]
    public async Task GenerateAsync_UsesExpectedUser()
    {

        var generator = new ElastiCacheIamTokenGenerator();

        var token = await generator.GenerateAsync(new ElastiCacheIamOptions{
            CacheName= "my-cache",
            UserId= "redis-user",
            Region= RegionEndpoint.USEast1,
            IsServerless= false}, credentials);

        Assert.Contains(
            "User=redis-user",
            token);
    }

    [Fact]
    public async Task GenerateAsync_UsesSessionCredentials()
    {
        var credentials = new SessionAWSCredentials(
            "AKIDEXAMPLE",
            "secret",
            "session-token");

        var generator = new ElastiCacheIamTokenGenerator();

        var token = await generator.GenerateAsync(new ElastiCacheIamOptions{
            CacheName= "my-cache",
            UserId= "my-user",
            Region= RegionEndpoint.USEast1,
            IsServerless= false}, credentials);

        Assert.Contains(
            "X-Amz-Security-Token=",
            token);
    }
}
