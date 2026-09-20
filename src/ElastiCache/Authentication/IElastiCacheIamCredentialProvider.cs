public interface IElastiCacheIamCredentialProvider
{
    string UserName { get; }

    string Password { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}