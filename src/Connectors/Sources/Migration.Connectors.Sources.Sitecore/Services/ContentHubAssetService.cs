using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Stylelabs.M.Sdk.WebClient;
using Stylelabs.M.Sdk.Contracts.Base;


namespace Migration.Connectors.Sources.Sitecore.Services
{
    public class ContentHubAssetService
    {
        private readonly IWebMClient _client;
        private readonly ILogger<ContentHubAssetService> _logger;

        public ContentHubAssetService(IWebMClient client, ILogger<ContentHubAssetService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        /// <summary>
        /// Extracts metadata (properties) for an asset entity.
        /// </summary>
        public Dictionary<string, object> GetAssetMetadata(IEntity assetEntity)
        {
            var metadata = new Dictionary<string, object>();

            foreach (var prop in assetEntity.Properties)
            {
                //metadata[prop.Key] = prop.Value;
            }

            return metadata;
        }

    }

}
