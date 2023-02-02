using System;
using System.Net;
using System.Runtime.Serialization.Json;
using System.IO;

/* Author: rfmejia */
namespace Rfx.Riken.OsakaUniv
{
    class VersionUpdateNotifierService
    {
        private VersionDescriptionDocument fetchVersionDescriptionDocument(Uri uri)
        {
            using (var client = new WebClient())
            {
                client.DownloadDataCompleted += (sender, e) =>
                {
                    var serializer = new DataContractJsonSerializer(typeof(VersionDescriptionDocument));
                    var vdd = (VersionDescriptionDocument)serializer.ReadObject(new MemoryStream(e.Result));
                };
                client.DownloadStringAsync(uri);
            }
            return null;
        }
    }
}
