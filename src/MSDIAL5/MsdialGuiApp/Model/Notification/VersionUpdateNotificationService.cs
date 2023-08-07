using CompMs.App.Msdial.Properties;
using CompMs.App.Msdial.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;

namespace CompMs.App.Msdial.Model.Notification
{
    /// <summary>
    /// Checks for the latest Version Description Document online, and notify the user when a new version is available.
    /// </summary>
    public static class VersionUpdateNotificationService
    {
        private static void FetchVersionDescriptionDocument(Uri uri, Action<VersionDescriptionDocument> callback)
        {
            var json = FetchVersionDescriptionDocument(uri);
            if (json is null) {
                return;
            }

            var serializer = new DataContractJsonSerializer(typeof(ReleaseInfoDataTransferObject));
            try {
                var vdd = ((ReleaseInfoDataTransferObject)serializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(json)))).ToVersionDescriptionDocument();
                callback?.Invoke(vdd);
            }
            catch (SerializationException) {

            }
        }

        private static string FetchVersionDescriptionDocument(Uri uri)
        {
            try {
                using (var client = new MyWebClient()) {
                    return client.DownloadString(uri);
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
            if (GlobalResources.Instance.Version != vdd.LatestVersion)
            {
                var result = MessageBox.Show(
                    $"A new MS-DIAL is available: {vdd.LatestVersion}\r\nClick 'Yes' if you want to go MS-DIAL website.",
                    "Update notification",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes) {
                    System.Diagnostics.Process.Start("http://prime.psc.riken.jp/compms/index.html");
                }

            }
        }

        public static void CheckForUpdates()
        {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return;
            }
            
            // TODO: Show "Checking for updates" in status bar
            Uri updateUri = new Uri(Resources.VDD_URI);
            FetchVersionDescriptionDocument(updateUri, ShowUpdatePopup);
        }

        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 5 * 1000;
                return w;
            }
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

            public VersionDescriptionDocument ToVersionDescriptionDocument() {
                return new VersionDescriptionDocument
                {
                    LatestVersion = TagName.TrimStart("MSDIAL-v.".ToCharArray()),
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
