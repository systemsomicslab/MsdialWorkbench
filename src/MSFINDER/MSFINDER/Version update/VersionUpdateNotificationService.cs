using System;
using System.Net;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Windows;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

/* Author: rfmejia */
namespace Rfx.Riken.OsakaUniv
{
    internal class MyWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 5 * 1000;
            return w;
        }
    }

    /// <summary>
    /// Checks for the latest Version Description Document online, and notify the user when a new version is available.
    /// </summary>
    public sealed class VersionUpdateNotificationService
    {
        private VersionUpdateNotificationService() { }

        private delegate void VersionUpdateCallback(VersionDescriptionDocument vdd);

        private static void FetchVersionDescriptionDocument(Uri uri, Action<VersionDescriptionDocument> callback) {
            var jsonStream = FetchVersionDescriptionDocument(uri);
            if (jsonStream is null) {
                return;
            }

            var serializer = new DataContractJsonSerializer(typeof(List<ReleaseInfoDataTransferObject>));
            try {
                var releaseInfoDataTransferObjects = (List<ReleaseInfoDataTransferObject>)serializer.ReadObject(jsonStream);
                foreach (var dto in releaseInfoDataTransferObjects) {
                    if (!dto.IsPrerelease && dto.TagName.StartsWith("MSFINDER")) { 
                        var vdd = dto.ToVersionDescriptionDocument();
                        callback?.Invoke(vdd);
                        break;
                    }
                }
            }
            catch (SerializationException) {

            }
        }
        private static Stream FetchVersionDescriptionDocument(Uri uri) {
            try {
                using (var client = new MyWebClient()) {
                    client.Headers.Add(HttpRequestHeader.UserAgent, "Msdial");
                    client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    return client.OpenRead(uri);
                }
            }
            catch (IOException) {

            }
            catch (WebException) {

            }
            return null;
        }

        private static void ShowUpdatePopup(VersionDescriptionDocument vdd)
        {
            if (Rfx.Riken.OsakaUniv.Properties.Resources.VERSION != vdd.LatestVersion) {
                // TODO Show version update information in a popup
                if (MessageBox.Show("A new MS-FINDER is available: " + vdd.LatestVersion + "\r\n" +
                    "Click 'Yes' if you want to go MS-FINDER website.", "Update notification", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
                    System.Diagnostics.Process.Start("https://github.com/systemsomicslab/MsdialWorkbench/releases");
                }

            }
        }

        public static void CheckForUpdates()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;


            // TODO Show "Checking for updates" in status bar
            Uri updateUri = new Uri(Rfx.Riken.OsakaUniv.Properties.Resources.VDD_URI);
            FetchVersionDescriptionDocument(updateUri, ShowUpdatePopup);
        }

        [DataContract]
        class ReleaseInfoDataTransferObject {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }

            [DataMember(Name = "published_at")]
            public string PublishedAt { get; set; }

            [DataMember(Name = "html_url")]
            public string HtmlUrl { get; set; }

            [DataMember(Name = "assets")]
            public List<AssetsDataTransferObject> Assets { get; set; }

            [DataMember(Name = "prerelease")]
            public bool IsPrerelease { get; set; }

            public VersionDescriptionDocument ToVersionDescriptionDocument() {
                return new VersionDescriptionDocument {
                    LatestVersion = TagName.TrimStart("MSFINDER-v".ToCharArray()),
                    DatePublished = PublishedAt,
                    DownloadUri = new Uri(HtmlUrl),
                };
            }
        }

        [DataContract]
        class AssetsDataTransferObject {
            [DataMember(Name = "browser_download_url")]
            public string DownloadUrl { get; set; }
        }

    }
}
