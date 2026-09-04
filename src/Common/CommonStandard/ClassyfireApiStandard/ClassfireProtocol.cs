using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;

namespace CompMs.Common.ClassyfireApiStandard
{
    [DataContract]
    public class ClassyfireResult
    {
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
    public class ClassyfireEntity
    {
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
    public class ClassyfireClass
    {
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
    public class ClassyfireRequest
    {
        [DataMember(Name = "label")] // 
        public string label { get; set; }
        [DataMember(Name = "query_input")] // 
        public string query_input { get; set; }
        [DataMember(Name = "query_type")] // 
        public string query_type { get; set; }
    }

    [DataContract]
    public class ClassyfireResponse
    {
        [DataMember(Name = "id")] // 
        public string id { get; set; }
        [DataMember(Name = "label")] // 
        public string label { get; set; }
        [DataMember(Name = "query_input")] // 
        public string query_input { get; set; }
        [DataMember(Name = "query_type")] // 
        public string query_type { get; set; }
    }

    public class ClassfireApi
    {
        private static string prolog = @"http://classyfire.wishartlab.com";
        private static readonly HttpClient HttpClient = new HttpClient();

        public void DownloadClassyfireJson(string entryID, string path)
        {
            var url = prolog + "/queries/" + entryID + ".json";
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
            catch (Exception e)
            {
                Console.WriteLine("failed: {0}, {1}", url, e);
            }
        }

        public int PostSmilesQuery(string label, string smiles)
        {

            var result = new ClassyfireEntity();
            var url = prolog + "/queries/";
            var entryID = -1;

            using (var content = new StringContent(JsonConvert.SerializeObject(new ClassyfireRequest()
            {
                label = label,
                query_input = smiles,
                query_type = "STRUCTURE"
            }, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                try
                {
                    using (var response = HttpClient.PostAsync(url, content).GetAwaiter().GetResult())
                    {
                        response.EnsureSuccessStatusCode();
                        var res = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        var classyfireResponse = JsonConvert.DeserializeObject<ClassyfireResponse>(res);
                        if (classyfireResponse.id == null)
                            return -1;
                        else
                        {
                            if (int.TryParse(classyfireResponse.id, out entryID))
                                return entryID;
                            else
                                return -1;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                catch (System.IO.IOException ex)
                {
                    System.Console.WriteLine(ex);
                    return -1;
                }
                catch (System.NullReferenceException ex)
                {
                    System.Console.WriteLine(ex);
                    return -1;
                }
                catch (Newtonsoft.Json.JsonReaderException ex)
                {
                    System.Console.WriteLine(ex);
                    return -1;
                }
            }
            return -1;
        }


        public ClassyfireEntity ReadClassyfireEntityByInChIKey(string inchikey)
        {
            var url = prolog + "/entities/" + inchikey + ".json";
            ClassyfireEntity result = null;
            try
            {
                using (var res = getWebResponse(url))
                {
                    using (var sr = new StreamReader(res.Content.ReadAsStreamAsync().GetAwaiter().GetResult()))
                    {
                        var resString = sr.ReadToEnd();
                        result = JsonConvert.DeserializeObject<ClassyfireEntity>(resString);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("{0}: {1}", ex.HResult, ex.Message);
                return null;
            }
            catch (System.IO.IOException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (System.NullReferenceException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }

            return result;
        }

        public ClassyfireEntity ReadClassyfireEntityByEntryID(string entryID)
        {
            var url = prolog + "/queries/" + entryID + ".json";
            ClassyfireEntity result = null;
            try
            {
                using (var res = getWebResponse(url))
                {
                    using (var sr = new StreamReader(res.Content.ReadAsStreamAsync().GetAwaiter().GetResult()))
                    {
                        var resString = sr.ReadToEnd();
                        result = JsonConvert.DeserializeObject<ClassyfireEntity>(resString);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("{0}: {1}", ex.HResult, ex.Message);
                return null;
            }
            catch (System.IO.IOException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (System.NullReferenceException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }

            return result;
        }

        public ClassyfireEntity ReadClassyfireEntityAsSdfByEntryID(string entryID)
        {
            var url = prolog + "/queries/" + entryID + ".sdf";
            ClassyfireEntity result = null;
            try
            {
                using (var res = getWebResponse(url))
                {
                    using (var sr = new StreamReader(res.Content.ReadAsStreamAsync().GetAwaiter().GetResult()))
                    {
                        var resString = sr.ReadToEnd();
                        result = readSdfClassyfireEntity(resString);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("{0}: {1}", ex.HResult, ex.Message);
                return null;
            }
            catch (System.IO.IOException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (System.NullReferenceException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }

            return result;
        }

        private ClassyfireEntity readSdfClassyfireEntity(string resString)
        {

            var entity = new ClassyfireEntity()
            {
                kingdom = new ClassyfireClass(),
                superclass = new ClassyfireClass(),
                nClass = new ClassyfireClass(),
                subclass = new ClassyfireClass(),
                direct_parent = new ClassyfireClass()
            };

            using (var sr = new StringReader(resString))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var trimedField = line.Trim();
                    switch (trimedField)
                    {
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

        public ClassyfireResult ReadClassyfireResultByEntryID(string entryID)
        {
            var url = prolog + "/queries/" + entryID + ".json";
            ClassyfireResult result = null;
            try
            {
                using (var res = getWebResponse(url))
                {
                    using (var sr = new StreamReader(res.Content.ReadAsStreamAsync().GetAwaiter().GetResult()))
                    {
                        var resString = sr.ReadToEnd();
                        result = JsonConvert.DeserializeObject<ClassyfireResult>(resString);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("{0}: {1}", ex.HResult, ex.Message);
                return null;
            }
            catch (System.IO.IOException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (System.NullReferenceException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                System.Console.WriteLine(ex);
                return null;
            }

            return result;
        }

        private HttpResponseMessage getWebResponse(string url)
        {
            HttpResponseMessage res = null;

            try
            {
                res = HttpClient.GetAsync(url).GetAwaiter().GetResult();
                res.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("{0}: {1}", ex.HResult, ex.Message);
                res = null;
            }
            finally
            {
            }
            return res;
        }
    }
}
