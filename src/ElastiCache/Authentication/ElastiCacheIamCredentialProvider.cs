using Amazon.Runtime;

public sealed class ElastiCacheIamCredentialProvider
    : IElastiCacheIamCredentialProvider,
      IAsyncDisposable
{
    private readonly ElastiCacheIamOptions _options;
    private readonly AWSCredentials _awsCredentials;
    private readonly IElastiCacheIamTokenGenerator _tokenGenerator;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();

    private Task? _refreshLoop;

    private volatile ElastiCacheIamCredentials? _current;

    public string UserName => _options.UserId;

    public string Password =>
        _current?.Password
        ?? throw new InvalidOperationException(
            "The ElastiCache IAM credential provider has not been initialized. " +
            "Call StartAsync() before creating the Redis connection.");

    public ElastiCacheIamCredentialProvider(
        ElastiCacheIamOptions options,
        AWSCredentials awsCredentials,
        IElastiCacheIamTokenGenerator tokenGenerator)
    {
        _options = options;
        _awsCredentials = awsCredentials;
        _tokenGenerator = tokenGenerator;

        _options.Validate();
    }

    public async Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        // We deliberately perform the first token generation synchronously
        // from the caller's perspective. This guarantees that Password is
        // available before StackExchange.Redis starts connecting.
        await RefreshAsync(cancellationToken)
            .ConfigureAwait(false);

        _refreshLoop = RunRefreshLoopAsync(_shutdown.Token);
    }

    public async Task RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            // Another caller may have refreshed while we were waiting.
            if (IsCurrentTokenUsable())
                return;

            var token = await _tokenGenerator.GenerateAsync(
                    _options,
                    _awsCredentials,
                    cancellationToken)
                .ConfigureAwait(false);

            var expiresAt =
                DateTimeOffset.UtcNow.Add(_options.TokenLifetime);

            _current = new ElastiCacheIamCredentials(
                _options.UserId,
                token,
                expiresAt);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool IsCurrentTokenUsable()
    {
        var current = _current;

        if (current is null)
            return false;

        var refreshAt =
            current.ExpiresAt -
            _options.RefreshBeforeExpiry;

        return DateTimeOffset.UtcNow < refreshAt;
    }

    private async Task RunRefreshLoopAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetNextRefreshDelay();

                await Task.Delay(delay, cancellationToken)
                    .ConfigureAwait(false);

                await RefreshAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Important: don't throw out of the background loop.
                //
                // The existing token remains available until its actual
                // expiration. We retry below.
                try
                {
                    await Task.Delay(
                            _options.RefreshRetryDelay,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private TimeSpan GetNextRefreshDelay()
    {
        var current = _current;

        if (current is null)
            return TimeSpan.Zero;

        var refreshAt =
            current.ExpiresAt -
            _options.RefreshBeforeExpiry;

        var delay =
            refreshAt - DateTimeOffset.UtcNow;

        return delay > TimeSpan.Zero
            ? delay
            : TimeSpan.Zero;
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();

        if (_refreshLoop is not null)
        {
            try
            {
                await _refreshLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected.
            }
        }

        _refreshLock.Dispose();
        _shutdown.Dispose();
    }
}