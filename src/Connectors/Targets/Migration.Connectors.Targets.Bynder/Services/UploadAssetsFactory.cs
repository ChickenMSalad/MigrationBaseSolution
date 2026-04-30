using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bynder.Sdk.Model;
using Bynder.Sdk.Query.Upload;

namespace Migration.Connectors.Targets.Bynder.Services
{
    public class UploadAssetsFactory
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10); // Limit to 10 concurrent uploads


        public async Task<List<SaveMediaResponse>> UploadAllAsync(
               List<(MemoryStream stream, UploadQuery query)> uploads,
               AssetResiliencyService ars)
        {
            var tasks = new List<Task<SaveMediaResponse>>();

            foreach (var (stream, query) in uploads)
            {
                await _semaphore.WaitAsync();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        // Actual upload
                        return await ars.UploadFileAsync(stream, query);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });

                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }


        public async Task UploadFilesAsync(List<(MemoryStream stream, UploadQuery query)> files, AssetResiliencyService ars)
        {
            var tasks = new List<Task>();

            foreach (var file in files)
            {
                tasks.Add(UploadWithLimitAsync(file.stream, file.query, ars));
            }

            await Task.WhenAll(tasks);
        }

        private async Task UploadWithLimitAsync(MemoryStream stream, UploadQuery query, AssetResiliencyService ars)
        {
            await _semaphore.WaitAsync();
            try
            {
                var response = await ars.UploadFileAsync(stream, query);
                
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
