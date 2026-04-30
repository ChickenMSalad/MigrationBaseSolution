using CloudinaryDotNet;
using Microsoft.Extensions.Options;
using Migration.Connectors.Targets.Cloudinary.Configuration;

namespace Migration.Connectors.Targets.Cloudinary.Services;

public sealed class CloudinaryClientFactory(IOptions<CloudinaryOptions> options)
{
    public CloudinaryDotNet.Cloudinary Create()
    {
        var value = options.Value;
        value.Validate();

        var account = new Account(value.CloudName, value.ApiKey, value.ApiSecret);
        var cloudinary = new CloudinaryDotNet.Cloudinary(account);
        cloudinary.Api.Secure = value.Secure;
        return cloudinary;
    }
}
