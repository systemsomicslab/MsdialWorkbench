using edu.ucdavis.fiehnlab.MonaRestApi.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.IO;

namespace edu.ucdavis.fiehnlab.MonaRestApi.client {
    public class BinvestigateClient {
        private readonly string CLASSIFICATION_PATH = "rest/bin/classifications";

        private WebClient client = new WebClient();

        public BinvestigateClient(string bvHost) {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            this.client.BaseAddress = Path.Combine("https://", bvHost);
        }

        public List<ClassificationsResponse> getClassification() {
            client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=UTF-8";
            client.Headers[HttpRequestHeader.Accept] = "application/json";

            try {
                var response = client.DownloadString(CLASSIFICATION_PATH);
                return JArray.Parse(response).ToObject<List<ClassificationsResponse>>();
            } catch (JsonReaderException ex) {
                Debug.WriteLine("error: " + ex.Message);
                Debug.WriteLineIf(ex.Data.Count > 0, "data: (" + ex.Data.Count + ")");
                foreach (var d in ex.Data) {
                    Debug.WriteLine("\t" + d);
                }
                return new List<ClassificationsResponse>();
            } catch (WebException ex) {
                Debug.WriteLine("client: " + client.BaseAddress + CLASSIFICATION_PATH);
                Debug.WriteLine("Error message: " + ex.Message);
                Debug.WriteLine("Error data: " + ex.Data);
                return new List<ClassificationsResponse>();
            }
        }

    }
}
