using Amazon;
using Amazon.Runtime;
using Xunit;

namespace ElastiCache.Tests.Authentication;

public sealed class ElastiCacheIamCredentialProviderTests
{
    private static ElastiCacheIamOptions CreateOptions(
        TimeSpan? tokenLifetime = null,
        TimeSpan? refreshBeforeExpiry = null)
    {
        return new ElastiCacheIamOptions
        {
            CacheName = "my-cache",
            UserId = "my-user",
            Region = RegionEndpoint.USEast1,
            IsServerless = false,
            TokenLifetime = tokenLifetime ?? TimeSpan.FromMinutes(15),
            RefreshBeforeExpiry =
                refreshBeforeExpiry ?? TimeSpan.FromMinutes(3),
            RefreshRetryDelay = TimeSpan.FromMilliseconds(25)
        };
    }

  private  BasicAWSCredentials credentials = new BasicAWSCredentials(
        "AKIDEXAMPLE",
        "secret");
        
    [Fact]
    public async Task StartAsync_GeneratesInitialToken()
    {
        var generator = new TestTokenGenerator();
        var provider = new ElastiCacheIamCredentialProvider(
            CreateOptions(),
            credentials,
            generator);

        await provider.StartAsync();

        Assert.Equal("token-1", provider.Password);
        Assert.Equal("my-user", provider.UserName);
        Assert.Equal(1, generator.CallCount);

        await provider.DisposeAsync();
    }

    [Fact]
    public async Task Password_ReturnsCachedToken()
    {
        var generator = new TestTokenGenerator();
        var provider = new ElastiCacheIamCredentialProvider(
            CreateOptions(),
            credentials,
            generator);

        await provider.StartAsync();

        var first = provider.Password;
        var second = provider.Password;
        var third = provider.Password;

        Assert.Equal("token-1", first);
        Assert.Equal(first, second);
        Assert.Equal(first, third);

        Assert.Equal(1, generator.CallCount);

        await provider.DisposeAsync();
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenIsFresh_PreservesCachedToken()
    {
        var generator = new TestTokenGenerator();
        var provider = new ElastiCacheIamCredentialProvider(
            CreateOptions(),
            credentials,
            generator);

        await provider.StartAsync();

        Assert.Equal("token-1", provider.Password);

        await provider.RefreshAsync();

        Assert.Equal("token-1", provider.Password);
        Assert.Equal(1, generator.CallCount);

        await provider.DisposeAsync();
    }

    [Fact]
    public async Task ConcurrentRefresh_IsSingleFlight()
    {
        var callStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var releaseCall = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var calls = 0;

        var generator = new TestTokenGenerator(
            async (_, _, _, _, _) =>
            {
                var call = Interlocked.Increment(ref calls);

                if (call == 1)
                {
                    return "initial-token";
                }

                callStarted.SetResult();

                await releaseCall.Task;

                return "refreshed-token";
            });

        var provider = new ElastiCacheIamCredentialProvider(
            CreateOptions(),
            credentials,
            generator);

        await provider.StartAsync();

        var refreshTasks = Enumerable.Range(0, 20)
            .Select(_ => provider.RefreshAsync())
            .ToArray();

        await callStarted.Task;

        releaseCall.SetResult();

        await Task.WhenAll(refreshTasks);

        Assert.Equal(
            "refreshed-token",
            provider.Password);

        Assert.Equal(
            2,
            generator.CallCount);

        await provider.DisposeAsync();
    }

    [Fact]
    public async Task FailedRefresh_PreservesExistingToken()
    {
        var attempts = 0;

        var generator = new TestTokenGenerator(
            (_, _, _, _, _) =>
            {
                var attempt = Interlocked.Increment(ref attempts);

                if (attempt == 1)
                {
                    return Task.FromResult("valid-token");
                }

                throw new InvalidOperationException(
                    "Token generation failed.");
            });

        var provider = new ElastiCacheIamCredentialProvider(
            CreateOptions(),
            credentials,
            generator);

        await provider.StartAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.RefreshAsync());

        Assert.Equal(
            "valid-token",
            provider.Password);


        await provider.DisposeAsync();
    }

    [Fact]
    public async Task FailedInitialGeneration_DoesNotExposePassword()
    {
        var generator = new TestTokenGenerator(
            (_, _, _, _, _) =>
                throw new InvalidOperationException(
                    "Token generation failed."));

        var provider = new ElastiCacheIamCredentialProvider(
            CreateOptions(),
            credentials,
            generator);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.StartAsync());

        Assert.Throws<InvalidOperationException>(
            () => _ = provider.Password);

        await provider.DisposeAsync();
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenIsStillFresh_DoesNotRegenerate()
    {
        var generator = new TestTokenGenerator();

        var provider = new ElastiCacheIamCredentialProvider(
            CreateOptions(
                tokenLifetime: TimeSpan.FromMinutes(15),
                refreshBeforeExpiry: TimeSpan.FromMinutes(3)),
            credentials,
            generator);

        await provider.StartAsync();

        await provider.RefreshAsync();

        Assert.Equal(
            1,
            generator.CallCount);

        Assert.Equal(
            "token-1",
            provider.Password);

        await provider.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMoreThanOnce()
    {
        var generator = new TestTokenGenerator();

        var provider = new ElastiCacheIamCredentialProvider(
            CreateOptions(),
            credentials,
            generator);

        await provider.StartAsync();

        await provider.DisposeAsync();
        await provider.DisposeAsync();
    }
}
