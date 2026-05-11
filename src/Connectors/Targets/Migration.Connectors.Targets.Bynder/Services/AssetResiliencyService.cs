using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Connectors.Targets.Bynder.Extensions;
﻿using Bynder.Sdk.Model;
using Bynder.Sdk.Query.Upload;
using Bynder.Sdk.Service;
using Bynder.Sdk.Service.Asset;

using Polly;
using Polly.Retry;

namespace Migration.Connectors.Targets.Bynder.Services;

public class AssetResiliencyService(IBynderClient bynderClient)
{
    private static ResiliencePipeline CreateUploadPipeline()
    {
        var retryOptions = new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(3),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = args => args.Outcome switch
            {
                { Exception: HttpRequestException } => PredicateResult.True(),
                { Exception: BynderException } => PredicateResult.True(),
                _ => PredicateResult.False()
            }
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .Build();
    }

    private readonly IAssetService _assetService = bynderClient.GetAssetService();
    private readonly ResiliencePipeline _fileUploadPipeline = CreateUploadPipeline();

    public async Task<SaveMediaResponse> UploadFileAsync(MemoryStream stream, UploadQuery uploadQuery)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable for upload.", nameof(stream));
        }

        return await _fileUploadPipeline.ExecuteAsync(async _ =>
        {
            // Tricky: Since the Bynder SDK disposes the source stream, create a non-disposing proxy to support retries.
            await using var streamProxy = new NonDisposingStreamProxy(stream);
            streamProxy.Position = 0; // Ensure the stream is always at the beginning before retry.

            var saveMediaResponse = await _assetService.UploadFileAsync(streamProxy, uploadQuery).ConfigureAwait(false);

            if (!saveMediaResponse.IsSuccessful)
            {
                throw new BynderException($"An unexpected error occured uploading image '{uploadQuery.Name}'.");
            }

            return saveMediaResponse;
        }).ConfigureAwait(false);
    }

    public async Task<SaveMediaResponse> UploadFileAsync(Stream stream, UploadQuery uploadQuery)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable for upload.", nameof(stream));
        }

        return await _fileUploadPipeline.ExecuteAsync(async _ =>
        {
            // Tricky: Since the Bynder SDK disposes the source stream, create a non-disposing proxy to support retries.
            await using var streamProxy = new NonDisposingStreamProxy(stream);
            streamProxy.Position = 0; // Ensure the stream is always at the beginning before retry.

            var saveMediaResponse = await _assetService.UploadFileAsync(streamProxy, uploadQuery).ConfigureAwait(false);

            if (!saveMediaResponse.IsSuccessful)
            {
                throw new BynderException($"An unexpected error occured uploading image '{uploadQuery.Name}'.");
            }

            return saveMediaResponse;
        }).ConfigureAwait(false);
    }
}
