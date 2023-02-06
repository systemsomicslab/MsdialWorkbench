using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.Classfire {
    [DataContract]
    public class ClassyfireResult {
        [DataMember(Name = "id")] // query id
        public int id { get; set; }
        [DataMember(Name = "label")] // 
        public string label { get; set; }
        [DataMember(Name = "classification_status")] // 
        public string classification_status { get; set; }
        [DataMember(Name = "entities")] // 
        public ClassyfireEntity[] entities { get; set; }
    }

    [DataContract]
    public class ClassyfireEntity {
        [DataMember(Name = "identifier")] // 
        public string identifier { get; set; } //identifier for a query that we post
        [DataMember(Name = "smiles")] // 
        public string smiles { get; set; } //identifier for a query that we post
        [DataMember(Name = "inchikey")] // 
        public string inchikey { get; set; } //identifier for a query that we post
        [DataMember(Name = "kingdom")] // 
        public ClassyfireClass kingdom { get; set; }
        [DataMember(Name = "superclass")] // 
        public ClassyfireClass superclass { get; set; }
        [DataMember(Name = "class")] // 
        public ClassyfireClass nClass { get; set; }
        [DataMember(Name = "subclass")] // 
        public ClassyfireClass subclass { get; set; }
        [DataMember(Name = "direct_parent")] // 
        public ClassyfireClass direct_parent { get; set; }
        [DataMember(Name = "molecular_framework")] // 
        public string molecular_framework { get; set; }
        [DataMember(Name = "report")] // 
        public string report { get; set; }
    }

    [DataContract]
    public class ClassyfireClass {
        [DataMember(Name = "name")] // 
        public string name { get; set; }
        [DataMember(Name = "description")] // 
        public string description { get; set; }
        [DataMember(Name = "chemont_id")] // 
        public string chemont_id { get; set; }
        [DataMember(Name = "url")] // 
        public string url { get; set; }
    }

    [DataContract]
    public class ClassyfireRequest {
        [DataMember(Name = "label")] // 
        public string label { get; set; }
        [DataMember(Name = "query_input")] // 
        public string query_input { get; set; }
        [DataMember(Name = "query_type")] // 
        public string query_type { get; set; }
    }

    [DataContract]
    public class ClassyfireResponse {
        [DataMember(Name = "id")] // 
        public string id { get; set; }
        [DataMember(Name = "label")] // 
        public string label { get; set; }
        [DataMember(Name = "query_input")] // 
        public string query_input { get; set; }
        [DataMember(Name = "query_type")] // 
        public string query_type { get; set; }
    }

    public class ClassfireApi {
        private static string prolog = @"http://classyfire.wishartlab.com";

        public void DownloadClassyfireJson(string entryID, string path) {
            var url = prolog + "/queries/" + entryID + ".json";
            var uri = new Uri(url);

            try {
                new WebClient().DownloadFile(uri, path);
            }
            catch (Exception e) {
                Console.WriteLine("failed: {0}, {1}", url, e);
            }
        }

        public int PostSmilesQuery(string label, string smiles) {

            var result = new ClassyfireEntity();
            var url = prolog + "/queries/";
            var entryID = -1;

            using (var client = new WebClient()) {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Headers[HttpRequestHeader.Accept] = "application/json";
                client.Encoding = Encoding.UTF8;
                //create json client
                var request = new ClassyfireRequest() {
                    label = label,
                    query_input = smiles,
                    query_type = "STRUCTURE"
                };
                var json = JsonConvert.SerializeObject(request, Formatting.Indented);

                try {
                    var res = client.UploadString(url, "POST", json);
                    var response = JsonConvert.DeserializeObject<ClassyfireResponse>(res);
                    if (response.id == null)
                        return -1;
                    else {
                        if (int.TryParse(response.id, out entryID))
                            return entryID;
                        else
                            return -1;
                    }
                }
                catch (WebException ex) {
                    Debug.WriteLine("{0}: {1}", ex.Status, ex.Message);
                }
                catch (System.IO.IOException ex) {
                    System.Console.WriteLine(ex);
                    return -1;
                }
                catch (System.NullReferenceException ex) {
                    System.Console.WriteLine(ex);
                    return -1;
                }
                catch (Newtonsoft.Json.JsonReaderException ex) {
                    System.Console.WriteLine(ex);
                    return -1;
                }
            }
            return -1;
        }


        public ClassyfireEntity ReadClassyfireEntityByInChIKey(string inchikey) {
            var url = prolog + "/entities/" + inchikey + ".json";
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.KeepAlive = false;
            req.Timeout = System.Threading.Timeout.Infinite;
            req.ProtocolVersion = HttpVersion.Version10;

            ClassyfireEntity result = null;
            try {
                using (var res = req.GetResponse()) {
                    using (var sr = new StreamReader(res.GetResponseStream())) {
                        var resString = sr.ReadToEnd();
                        result = JsonConvert.DeserializeObject<ClassyfireEntity>(resString);
                    }
                }
            }
            catch (WebException ex) {
                Console.WriteLine("{0}: {1}", ex.Status, ex.Message);
                return null;
            }
            catch (System.IO.IOException ex) {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (System.NullReferenceException ex) {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException ex) {
                System.Console.WriteLine(ex);
                return null;
            }

            return result;
        }

        public ClassyfireEntity ReadClassyfireEntityByEntryID(string entryID) {
            var url = prolog + "/queries/" + entryID + ".json";
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.KeepAlive = false;
            req.Timeout = System.Threading.Timeout.Infinite;
            req.ProtocolVersion = HttpVersion.Version10;

            ClassyfireEntity result = null;
            try {
                using (var res = req.GetResponse()) {
                    using (var sr = new StreamReader(res.GetResponseStream())) {
                        var resString = sr.ReadToEnd();
                        result = JsonConvert.DeserializeObject<ClassyfireEntity>(resString);
                    }
                }
            }
            catch (WebException ex) {
                Console.WriteLine("{0}: {1}", ex.Status, ex.Message);
                return null;
            }
            catch (System.IO.IOException ex) {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (System.NullReferenceException ex) {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException ex) {
                System.Console.WriteLine(ex);
                return null;
            }

            return result;
        }

        public ClassyfireEntity ReadClassyfireEntityAsSdfByEntryID(string entryID) {
            var url = prolog + "/queries/" + entryID + ".sdf";
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.KeepAlive = false;
            req.Timeout = System.Threading.Timeout.Infinite;
            req.ProtocolVersion = HttpVersion.Version10;

            ClassyfireEntity result = null;
            try {
                using (var res = req.GetResponse()) {
                    using (var sr = new StreamReader(res.GetResponseStream())) {
                        var resString = sr.ReadToEnd();
                        result = readSdfClassyfireEntity(resString);
                    }
                }
            }
            catch (WebException ex) {
                Console.WriteLine("{0}: {1}", ex.Status, ex.Message);
                return null;
            }
            catch (System.IO.IOException ex) {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (System.NullReferenceException ex) {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException ex) {
                System.Console.WriteLine(ex);
                return null;
            }

            return result;
        }

        private ClassyfireEntity readSdfClassyfireEntity(string resString) {

            var entity = new ClassyfireEntity() {
                kingdom = new ClassyfireClass(), 
                superclass = new ClassyfireClass(), 
                nClass = new ClassyfireClass(), 
                subclass = new ClassyfireClass(),
                direct_parent = new ClassyfireClass()
            };

            using (var sr = new StringReader(resString)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var trimedField = line.Trim();
                    switch (trimedField) {
                        case @"> <InChIKey>":
                            entity.inchikey = sr.ReadLine();
                            break;
                        case @"> <Kingdom>":
                            entity.kingdom.name = sr.ReadLine();
                            break;
                        case @"> <Superclass>":
                            entity.superclass.name = sr.ReadLine();
                            break;
                        case @"> <Class>":
                            entity.nClass.name = sr.ReadLine();
                            break;
                        case @"> <Subclass>":
                            entity.subclass.name = sr.ReadLine();
                            break;
                        case @"> <Direct Parent>":
                            entity.direct_parent.name = sr.ReadLine();
                            break;
                    }
                }
            }
            return entity;
        }

        public ClassyfireResult ReadClassyfireResultByEntryID(string entryID) {
            var url = prolog + "/queries/" + entryID + ".json";
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.KeepAlive = false;
            req.Timeout = System.Threading.Timeout.Infinite;
            req.ProtocolVersion = HttpVersion.Version10;

            ClassyfireResult result = null;
            try {
                using (var res = req.GetResponse()) {
                    using (var sr = new StreamReader(res.GetResponseStream())) {
                        var resString = sr.ReadToEnd();
                        result = JsonConvert.DeserializeObject<ClassyfireResult>(resString);
                    }
                }
            }
            catch (WebException ex) {
                Console.WriteLine("{0}: {1}", ex.Status, ex.Message);
                return null;
            }
            catch (System.IO.IOException ex) {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (System.NullReferenceException ex) {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException ex) {
                System.Console.WriteLine(ex);
                return null;
            }

            return result;
        }

        private WebResponse getWebResponse(WebRequest req) {
            WebResponse res = null;

            try {
                res = req.GetResponse();
            }
            catch (WebException ex) {
                Console.WriteLine("{0}: {1}", ex.Status, ex.Message);
                res = null;
            }
            finally {
            }
            return res;
        }
    }
}
