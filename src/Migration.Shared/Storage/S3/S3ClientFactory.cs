using Amazon;
using Amazon.Runtime;
using Amazon.S3;

namespace Migration.Shared.Storage.S3;

public sealed class S3ClientFactory : IS3ClientFactory
{
    public IAmazonS3 Create(S3ClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.AccessKeyId))
        {
            throw new InvalidOperationException("S3 access key id is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            throw new InvalidOperationException("S3 secret access key is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Region) && string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            throw new InvalidOperationException("S3 region is required unless a service URL is provided.");
        }

        AWSCredentials credentials = string.IsNullOrWhiteSpace(options.SessionToken)
            ? new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey)
            : new SessionAWSCredentials(options.AccessKeyId, options.SecretAccessKey, options.SessionToken);

        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        return new AmazonS3Client(credentials, config);
    }
}
