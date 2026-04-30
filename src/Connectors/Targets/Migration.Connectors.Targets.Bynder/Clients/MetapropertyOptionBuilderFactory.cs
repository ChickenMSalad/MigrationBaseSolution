using Bynder.Sdk.Model;
using Bynder.Sdk.Service;
using Bynder.Sdk.Service.Asset;

using Migration.Connectors.Targets.Bynder.Extensions;

using Microsoft.Extensions.Caching.Memory;

namespace Migration.Connectors.Targets.Bynder.Clients;

public class MetapropertyOptionBuilderFactory(IBynderClient bynderClient, IMemoryCache memoryCache)
{
    private readonly IAssetService _assetService = bynderClient.GetAssetService();

    public async Task<IMetapropertyOptionBuilder> CreateBuilder()
    {
        var metapropertyLookup = await CreateMetapropertyLookup().ConfigureAwait(false);
        return new MetapropertyOptionBuilder(metapropertyLookup);
    }

    private async Task<Dictionary<string, Metaproperty>> CreateMetapropertyLookup()
    {
        return (await memoryCache.GetOrCreateAsyncWithLock(nameof(CreateMetapropertyLookup), async cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var metaproperties = await _assetService.GetMetapropertiesAsync().ConfigureAwait(false);
            return metaproperties.ToDictionary(entry => entry.Value.Name, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        }).ConfigureAwait(false)) ?? [];
    }

    private class MetapropertyOptionBuilder(IDictionary<string, Metaproperty> metapropertyLookup) : IMetapropertyOptionBuilder
    {
        private readonly Dictionary<string, IList<string>> _metapropertyOptions = [];

        public IList<string> this[string name]
        {
            get => _metapropertyOptions[name];
            set
            {
                if (!metapropertyLookup.TryGetValue(name, out var metaproperty))
                {
                    throw new BynderException($"Metaproperty '{name}' could not be resolved.");
                }

                _metapropertyOptions[metaproperty.Id] = value;
            }
        }

        public IDictionary<string, IList<string>> ToMetapropertyOptions()
        {
            return _metapropertyOptions;
        }
    }
}
