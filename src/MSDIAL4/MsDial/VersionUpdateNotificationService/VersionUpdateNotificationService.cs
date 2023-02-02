using System;
using System.Net;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Windows;

/* Author: rfmejia */
namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Checks for the latest Version Description Document online, and notify the user when a new version is available.
    /// </summary>
    public sealed class VersionUpdateNotificationService
    {
        private VersionUpdateNotificationService() { }

        private delegate void VersionUpdateCallback(VersionDescriptionDocument vdd);

        private static void FetchVersionDescriptionDocument(Uri uri, VersionUpdateCallback callback)
        {
            using (var client = new WebClient())
            {
                client.DownloadDataCompleted += (sender, e) =>
                {
                    var serializer = new DataContractJsonSerializer(typeof(VersionDescriptionDocument));
                    var vdd = (VersionDescriptionDocument)serializer.ReadObject(new MemoryStream(e.Result));
                    callback(vdd);
                };
                client.DownloadStringAsync(uri);
            }
        }

        private static void ShowUpdatePopup(VersionDescriptionDocument vdd)
        {
            if (Rfx.Riken.OsakaUniv.Properties.Resources.VERSION != vdd.LatestVersion)
            {
                // TODO Show version update information in a popup
                MessageBox.Show("A new MS-DIAL is available. " + vdd.LatestVersion + "\r\n" +
                    "Download link: " + vdd.DownloadUri, "Message", MessageBoxButton.OK, MessageBoxImage.Information);

            }
        }

        public static void CheckForUpdates()
        {
            // TODO Show "Checking for updates" in status bar
            Uri updateUri = new Uri(Rfx.Riken.OsakaUniv.Properties.Resources.VDD_URI);
            FetchVersionDescriptionDocument(updateUri, ShowUpdatePopup);
        }
    }
}
