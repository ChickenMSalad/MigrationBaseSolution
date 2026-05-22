using Amazon.S3;

namespace Migration.Shared.Storage.S3;

public interface IS3ClientFactory
{
    IAmazonS3 Create(S3ClientOptions options);
}
