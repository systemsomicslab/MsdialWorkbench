using CompMs.App.Msdial.Properties;
using CompMs.App.Msdial.Utility;
using CompMs.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
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
            var jsonStream = FetchVersionDescriptionDocument(uri);
            if (jsonStream is null) {
                return;
            }

            var serializer = new DataContractJsonSerializer(typeof(List<ReleaseInfoDataTransferObject>));
            try {
                var releaseInfoDataTransferObjects = (List<ReleaseInfoDataTransferObject>)serializer.ReadObject(jsonStream);
                foreach (var dto in releaseInfoDataTransferObjects) {
                    if (!dto.IsPrerelease && (dto.TagName?.StartsWith("MSDIAL-v5") ?? false)) {
                        var vdd = dto.ToVersionDescriptionDocument();
                        callback?.Invoke(vdd);
                        break;
                    }
                }
            }
            catch (SerializationException) {

            }
        }

        private static Stream? FetchVersionDescriptionDocument(Uri uri)
        {
            try {
                using var client = new MyWebClient();
                client.Headers.Add(HttpRequestHeader.UserAgent, "Msdial");
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                return client.OpenRead(uri);
            }
            catch (IOException) {

            }
            catch (WebException) {

            }
            return null;
        }

        private static void ShowUpdatePopup(VersionDescriptionDocument vdd)
        {
            // A BUILD NEWER THAN THE RELEASE IS NOT OFFERED AN UPDATE TO IT.
            //
            // The test below this is a string INEQUALITY with no ordering, so anyone running a
            // locally built MS-DIAL -- everyone developing it, and anyone who built the binary they
            // used for a paper -- was told on every start that a new version was available, and the
            // newer thing was the one already running.
            //
            // DatePublished has been parsed out of the release feed since this class was written and
            // read nowhere, so the fix needs no new request and no new field. Same-day still
            // notifies, and a date that cannot be parsed still notifies: the cost of a redundant
            // dialog is one click, and the cost of the opposite error is a user sitting on an old
            // version for a year without knowing.
            if (MsdialBuildIdentity.IsNewerThan(vdd.DatePublished)) {
                return;
            }

            if (GlobalResources.Instance.Version != vdd.LatestVersion)
            {
                var result = MessageBox.Show(
                    $"A new MS-DIAL is available: {vdd.LatestVersion}\r\nClick 'Yes' if you want to go MS-DIAL website.",
                    "Update notification",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes) {
                    System.Diagnostics.Process.Start("https://github.com/systemsomicslab/MsdialWorkbench/releases");
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

        /// <summary>
        /// "MSDIAL-v5.5.241113" becomes "5.5.241113".
        /// </summary>
        /// <remarks>
        /// Was TrimStart("MSDIAL-v".ToCharArray()), which trims ANY of {M,S,D,I,A,L,-,v}
        /// repeatedly rather than the prefix as a unit. It gives the right answer today only
        /// because the selection guard above requires the tag to start "MSDIAL-v5", so the trim
        /// stops on the '5' that is not in the set. A tag whose version part began with any of
        /// those characters -- a suffix scheme, a differently named series -- would have had
        /// characters eaten off the front of its version with no error, and the popup would then
        /// compare a mangled string and fire forever.
        /// </remarks>
        internal static string WithoutTagPrefix(string? tagName) {
            const string prefix = "MSDIAL-v";
            if (string.IsNullOrEmpty(tagName)) {
                return string.Empty;
            }
            return tagName!.StartsWith(prefix, StringComparison.Ordinal)
                ? tagName.Substring(prefix.Length)
                : tagName;
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
            public string? Name { get; set; }

            [DataMember(Name = "tag_name")]
            public string? TagName { get; set; }

            [DataMember(Name = "published_at")]
            public string? PublishedAt { get; set; }

            [DataMember(Name = "html_url")]
            public string? HtmlUrl { get; set; } 

            [DataMember(Name = "assets")]
            public List<AssetsDataTransferObject>? Assets { get; set; }

            [DataMember(Name = "prerelease")]
            public bool IsPrerelease { get; set; }

            public VersionDescriptionDocument ToVersionDescriptionDocument() {
                return new VersionDescriptionDocument
                {
                    LatestVersion = VersionUpdateNotificationService.WithoutTagPrefix(TagName),
                    DatePublished = PublishedAt,
                    DownloadUri = new Uri(HtmlUrl),
                };
            }
        }

        [DataContract]
        class AssetsDataTransferObject {
            [DataMember(Name = "browser_download_url")]
            public string? DownloadUrl { get; set; }
        }

    }
}
