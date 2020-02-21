using System;
using System.Net;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Windows;
using System.Text;

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

        private static void FetchVersionDescriptionDocument(Uri uri, VersionUpdateCallback callback)
        {
            try {
                using (var client = new MyWebClient()) {
                    var json = client.DownloadString(uri);
                    if (json == null) return;

                    var serializer = new DataContractJsonSerializer(typeof(VersionDescriptionDocument));
                    var vdd = (VersionDescriptionDocument)serializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                    callback(vdd);
                }
            }
            catch (System.IO.IOException ex) {

            }
            catch (System.Net.WebException ex) {

            }
            catch (System.Runtime.Serialization.SerializationException ex) {

            }
        }

        private static void ShowUpdatePopup(VersionDescriptionDocument vdd)
        {
            if (Rfx.Riken.OsakaUniv.Properties.Resources.VERSION != vdd.LatestVersion)
            {
                // TODO Show version update information in a popup
                if (MessageBox.Show("A new MS-DIAL is available: " + vdd.LatestVersion + "\r\n" +
                    "Click 'Yes' if you want to go MS-DIAL website.", "Update notification", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
                        System.Diagnostics.Process.Start("http://prime.psc.riken.jp/Metabolomics_Software/index.html");
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
    }
}
