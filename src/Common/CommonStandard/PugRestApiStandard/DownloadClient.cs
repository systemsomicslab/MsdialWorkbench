using System;
using System.IO;
using System.Net.Http;

namespace CompMs.Common.PugRestApiStandard
{
    public sealed class DownloadClient
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static void DownloadSdf(string url, string path)
        {
            var uri = new Uri(url);

            try
            {
                using (var response = HttpClient.GetAsync(uri).GetAwaiter().GetResult())
                {
                    response.EnsureSuccessStatusCode();
                    using (var input = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var output = File.Create(path))
                    {
                        input.CopyTo(output);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("failed: {0}, {1}", url, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("failed: {0}, {1}", url, e.Message);
            }
        }
    }
}
