using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Signing;

namespace ElastiCache.IAMElastiCacheTokenGenerator
{
    public class IAMElastiCacheTokenGenerator
    {
        private const string Action = "connect";
        private const string ServiceName = "elasticache";
        private const string UserParameter = "User";
        private static readonly TimeSpan TokenExpiry = TimeSpan.FromMinutes(15);

        public static async Task<string> GenerateTokenAsync(
            string cacheName,
            string userId,
            RegionEndpoint regionEndpoint,
            AWSCredentials awsCredentials)
        {
            var uri = new Uri($"http://{cacheName}/?Action={Action}&{UserParameter}={Uri.EscapeDataString(userId)}");

            var request = new AWSSigningRequest
            {
                HttpMethod = HttpMethod.Get,
                RequestUri = uri
            };


            var parameters = new AWSSigV4Parameters()
            {
                Credentials = awsCredentials,
                Region = regionEndpoint,
                Service = ServiceName
            };

            var result = await AWSSigV4Signer.PresignAsync(
                request,
                parameters,
                TokenExpiry,
                CancellationToken.None);

            return result.Uri.ToString()
                .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
                .Replace("https://", "", StringComparison.OrdinalIgnoreCase);
        }
    }
}