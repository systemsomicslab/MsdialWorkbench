using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class DownloadClient
    {
        public static void DownloadSdf(string url, string path)
        {
            var uri = new Uri(url);

            try
            {
                new WebClient().DownloadFile(uri, path);
            }
            catch (IOException e) {
                Console.WriteLine("failed: {0}, {1}", url, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("failed: {0}, {1}", url, e.Message);
            }

            //var client = new WebClient();
            //var uri = new Uri(url);

            //client.DownloadProgressChanged += client_DownloadProgressChanged;
            //client.DownloadFileCompleted += client_DownloadFileCompleted;

            //client.DownloadFileAsync(uri, path);
        }

        private static void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
                Console.WriteLine("Canceled");
            else if (e.Error != null)
                Console.WriteLine("Error:{0}", e.Error.Message);
            else
                Console.WriteLine("Download complete");
        }

        private static void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.WriteLine("{0}% ({1}byte/{2}byte) downloaded", e.ProgressPercentage, e.TotalBytesToReceive, e.BytesReceived);
        }
    }
}
