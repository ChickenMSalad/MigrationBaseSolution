using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Utilities
{

    public class LargeFileDownloader
    {
        public static async Task DownloadLargeFileAsync(string url, string destinationFilePath)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode(); // Throws an exception if the HTTP status code is not 2xx

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                        using (FileStream fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
                        {
                            byte[] buffer = new byte[81920]; // 80KB buffer
                            int bytesRead;
                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                            }
                        }
                        Console.WriteLine($"File downloaded successfully to: {destinationFilePath}");
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Error downloading file: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An unexpected error occurred: {e.Message}");
                }
            }
        }

        public static async Task Main(string[] args)
        {
            string fileUrl = "http://example.com/largefile.zip"; // Replace with your large file URL
            string localFilePath = "C:\\Temp\\largefile.zip"; // Replace with your desired local path

            await DownloadLargeFileAsync(fileUrl, localFilePath);
        }
    }
}
